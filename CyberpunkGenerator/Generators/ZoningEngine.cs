using CyberpunkGenerator.Models;
using CyberpunkGenerator.Data;
using CyberpunkGenerator.Economy;

namespace CyberpunkGenerator.Generators
{
    /// <summary>
    /// Takes the raw lists of Pops and Businesses produced by the economic
    /// simulator and places them on a CityMap grid using a gravity model:
    ///
    ///   • Each entity is attracted toward the centroid of the entities it
    ///     depends on (employers for Pops; customers / input suppliers for Businesses).
    ///   • Entities are placed in gentrification order (most affluent first).
    ///   • A higher-affluence entity may displace a lower-affluence entity that
    ///     occupies a desirable block, pushing the evicted entity back into the
    ///     pending queue.
    /// </summary>
    public class ZoningEngine
    {
        private readonly CityMap _map;

        // Sorted working queue; re-sorted whenever displacements add new items.
        private readonly List<IZoneable> _pendingEntities;

        // All businesses already placed — used for gravity lookups.
        private readonly List<(Business business, CityBlock block)> _placedBusinesses = new();

        private static readonly Random _rng = new();

        private int _nextBlockId = 0; // tracks for logging

        public ZoningEngine(List<Pop> allPops, List<Business> allBusinesses)
        {
            _map = new CityMap();
            _pendingEntities = new List<IZoneable>();
            _pendingEntities.AddRange(allPops);
            _pendingEntities.AddRange(allBusinesses);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public entry point
        // ─────────────────────────────────────────────────────────────────────

        public CityMap GenerateMap()
        {
            SortPendingEntities();

            // Seed: pull the first Mega-Corp Headquarters out of the pending queue
            // and place it at (0,0) before the loop starts. Using the actual instance
            // from CitySimulator (rather than creating a new one) means its
            // RequiredLabor was already counted by the economy loop, and we don't
            // end up with a duplicate HQ from when that entry reaches the front of
            // the queue in the main loop.
            var seedHQ = _pendingEntities
                .OfType<Business>()
                .FirstOrDefault(b => b.BusinessType == BusinessTypes.MegaCorpHeadquarters)
                ?? throw new InvalidOperationException(
                    "No Mega-Corp Headquarters found in pending entities. " +
                    "CitySimulator must generate one before ZoningEngine runs.");

            _pendingEntities.Remove(seedHQ);

            var seedBlock = _map.CreateBlock(0, 0, BlockType.Office);
            seedBlock.TryAddBusiness(seedHQ);
            _placedBusinesses.Add((seedHQ, seedBlock));
            Console.WriteLine("[Seed] Mega-Corp Headquarters placed at (0,0).");

            Console.WriteLine($"Starting Zoning Engine with {_pendingEntities.Count} entities to place.");

            int pass = 0;
            while (_pendingEntities.Count > 0)
            {
                pass++;
                var entity = _pendingEntities[0];
                _pendingEntities.RemoveAt(0);

                PlaceEntity(entity);

                // Re-sort if displacement events added items back — only needed
                // when the list was modified during PlaceEntity, which is flagged
                // by displacements having been recorded.
                if (_displacementsThisPlacement > 0)
                    SortPendingEntities();

                _displacementsThisPlacement = 0;
            }

            Console.WriteLine($"\nZoning complete. {_map.AllBlocks.Count} blocks placed in {pass} passes.");
            return _map;
        }

        // Counter reset each placement cycle so we know if a re-sort is needed.
        private int _displacementsThisPlacement;

        // ─────────────────────────────────────────────────────────────────────
        // Core placement logic
        // ─────────────────────────────────────────────────────────────────────

        private void PlaceEntity(IZoneable entity)
        {
            // 1. Calculate the gravity target for this entity.
            (float gx, float gy) = CalculateGravityTarget(entity);
            int targetX = (int)Math.Round(gx);
            int targetY = (int)Math.Round(gy);

            // 2. Walk outward from the target in BFS order, scoring every
            //    coordinate as either:
            //      a) an existing block that can directly accept the entity, or
            //      b) an empty space where a new block can be created.
            //    Also track the closest occupied block whose class is strictly
            //    lower, which is a displacement candidate.

            CityBlock? bestDirectFit = null;       // existing block, no displacement needed
            (int x, int y)? bestEmptyCoord = null; // empty coord for a brand-new block
            CityBlock? bestDisplaceable = null;     // block we could gentrify into
            int bestDistance = int.MaxValue;

            // BFS expanding rings until we have found at least one candidate
            // and have exhausted the current distance shell.
            var visited = new HashSet<(int, int)>();
            var queue = new Queue<(int x, int y, int dist)>();
            queue.Enqueue((targetX, targetY, 0));
            visited.Add((targetX, targetY));

            int cutoffDistance = int.MaxValue; // stop searching once we exceed the best found

            while (queue.Count > 0)
            {
                var (cx, cy, dist) = queue.Dequeue();

                // Prune: no point going farther than what we already have.
                if (dist > cutoffDistance) break;

                var existingBlock = _map.GetBlockAt(cx, cy);

                if (existingBlock != null)
                {
                    // Can this block directly absorb the entity?
                    if (CanAccept(existingBlock, entity))
                    {
                        if (dist < bestDistance || (dist == bestDistance && _rng.Next(2) == 0))
                        {
                            bestDirectFit = existingBlock;
                            bestEmptyCoord = null;
                            bestDistance = dist;
                            cutoffDistance = dist; // no need to look farther
                        }
                    }
                    // Is it a displacement candidate (lower class, same placement type)?
                    else if (bestDirectFit == null && CanDisplace(existingBlock, entity))
                    {
                        if (dist < bestDistance || (dist == bestDistance && _rng.Next(2) == 0))
                        {
                            bestDisplaceable = existingBlock;
                            bestDistance = dist;
                        }
                    }
                }
                else
                {
                    // Empty coordinate — a new block could go here.
                    // Only consider it if at least one occupied neighbor exists
                    // (city must stay contiguous).
                    if (HasPlacedNeighbor(cx, cy))
                    {
                        if (bestDirectFit == null &&
                            (dist < bestDistance || (dist == bestDistance && _rng.Next(2) == 0)))
                        {
                            bestEmptyCoord = (cx, cy);
                            bestDistance = dist;
                            cutoffDistance = dist; // don't look farther for empties either
                        }
                    }
                }

                // Expand neighbours if still within range.
                foreach (var (nx, ny) in Cardinals(cx, cy))
                {
                    if (!visited.Contains((nx, ny)) && dist + 1 <= cutoffDistance + 1)
                    {
                        visited.Add((nx, ny));
                        queue.Enqueue((nx, ny, dist + 1));
                    }
                }
            }

            // 3. Execute the best option found.
            if (bestDirectFit != null)
            {
                CommitToBlock(bestDirectFit, entity);
            }
            else if (bestEmptyCoord.HasValue)
            {
                var newBlock = CreateBlockForEntity(bestEmptyCoord.Value.x, bestEmptyCoord.Value.y, entity);
                CommitToBlock(newBlock, entity);
            }
            else if (bestDisplaceable != null)
            {
                DisplaceAndOccupy(bestDisplaceable, entity);
            }
            else
            {
                // Last resort: expand the map at the first available adjacent empty.
                var fallback = FindAnyAdjacentEmpty();
                var newBlock = CreateBlockForEntity(fallback.x, fallback.y, entity);
                CommitToBlock(newBlock, entity);
                Console.WriteLine($"  [Fallback] {DescribeEntity(entity)} placed at ({fallback.x},{fallback.y}).");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Gravity: calculate the ideal map position for an entity
        // ─────────────────────────────────────────────────────────────────────

        private (float x, float y) CalculateGravityTarget(IZoneable entity)
        {
            switch (entity)
            {
                case Pop pop:
                    return GravityForPop(pop);

                case Business biz when biz.PlacementType == PlacementType.Commercial:
                    return GravityForCommercialBusiness(biz);

                case Business biz when biz.PlacementType == PlacementType.Industrial:
                    return GravityForIndustrialBusiness(biz);

                default:
                    return (0f, 0f);
            }
        }

        /// <summary>
        /// Pops want to live near their employers: businesses whose
        /// RequiredLabor includes the pop's JobRole.
        /// </summary>
        private (float x, float y) GravityForPop(Pop pop)
        {
            var popRole = new JobRole(pop.SocioeconomicClass, pop.Field);

            var employers = _placedBusinesses
                .Where(pb => pb.business.RequiredLabor.ContainsKey(popRole))
                .ToList();

            return employers.Count > 0
                ? Centroid(employers.Select(pb => (pb.block.X, pb.block.Y)))
                : (0f, 0f);
        }

        /// <summary>
        /// Commercial businesses want to be near their target customers.
        /// </summary>
        private (float x, float y) GravityForCommercialBusiness(Business biz)
        {
            if (!biz.TargetClass.HasValue) return (0f, 0f);

            var customerBlocks = _map.AllBlocks
                .Where(b => b.SocioeconomicLevel == biz.TargetClass.Value && b.Pops.Count > 0)
                .ToList();

            return customerBlocks.Count > 0
                ? Centroid(customerBlocks.Select(b => (b.X, b.Y)))
                : (0f, 0f);
        }

        /// <summary>
        /// Industrial businesses want to be near their downstream consumers
        /// (other businesses that list one of their outputs as an input).
        /// </summary>
        private (float x, float y) GravityForIndustrialBusiness(Business biz)
        {
            var outputGoods = biz.Outputs.Keys.ToHashSet();

            var consumers = _placedBusinesses
                .Where(pb => pb.business.InputGoods.Keys.Any(g => outputGoods.Contains(g)))
                .ToList();

            return consumers.Count > 0
                ? Centroid(consumers.Select(pb => (pb.block.X, pb.block.Y)))
                : (0f, 0f);
        }

        private static (float x, float y) Centroid(IEnumerable<(int x, int y)> points)
        {
            var list = points.ToList();
            return ((float)(list.Average(p => p.x)), (float)(list.Average(p => p.y)));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Displacement
        // ─────────────────────────────────────────────────────────────────────

        private bool CanDisplace(CityBlock block, IZoneable incoming)
        {
            // Industrial blocks are fixed infrastructure — they cannot be gentrified.
            if (block.Type == BlockType.Industrial) return false;

            // The incoming entity must have a defined class to be "more affluent".
            if (!incoming.TargetClass.HasValue) return false;

            // The block must actually be occupied by a lower class.
            if (!block.SocioeconomicLevel.HasValue) return false;

            return GetAffluenceScore(incoming.TargetClass) > GetAffluenceScore(block.SocioeconomicLevel);
        }

        private void DisplaceAndOccupy(CityBlock block, IZoneable incoming)
        {
            Console.WriteLine($"  [Gentrify] {DescribeEntity(incoming)} displaces " +
                              $"{block.SocioeconomicLevel} residents at Block {block.Id} ({block.X},{block.Y}).");

            // Evict all existing pops and businesses back into the pending queue.
            foreach (var evictedPop in block.Pops)
            {
                _pendingEntities.Add(evictedPop);
                _displacementsThisPlacement++;
            }

            foreach (var evictedBiz in block.Businesses)
            {
                _placedBusinesses.RemoveAll(pb => pb.business == evictedBiz);
                _pendingEntities.Add(evictedBiz);
                _displacementsThisPlacement++;
            }

            // Wipe the block's state so the incoming entity can take it over.
            block.Pops.Clear();
            block.Businesses.Clear();

            // Reset the socioeconomic lock — CityBlock.AddPop / TryAddBusiness
            // will re-lock it to the new class on first insertion.
            ResetBlockClass(block);

            CommitToBlock(block, incoming);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Block creation and commitment helpers
        // ─────────────────────────────────────────────────────────────────────

        private CityBlock CreateBlockForEntity(int x, int y, IZoneable entity)
        {
            var blockType = entity switch
            {
                Business b when b.ZoneType == BusinessZoneType.Industrial => BlockType.Industrial,
                Business b when b.IsWholeBlock => BlockType.Office,
                _ => BlockType.MixedUse
            };

            return _map.CreateBlock(x, y, blockType);
        }

        private void CommitToBlock(CityBlock block, IZoneable entity)
        {
            switch (entity)
            {
                case Pop pop:
                    // The pop may be too large to fit in one block — split if needed.
                    int capacity = block.CalculateCapacityForClass(pop.SocioeconomicClass);
                    if (capacity <= 0)
                    {
                        // Block is full for this class; re-queue and a new block will form.
                        _pendingEntities.Insert(0, pop);
                        return;
                    }

                    if (pop.Size > capacity)
                    {
                        var overflow = pop.Split(capacity);
                        _pendingEntities.Insert(0, overflow); // re-queue the remainder at front
                    }

                    block.AddPop(pop);
                    Console.WriteLine($"  [Pop]     {pop} → Block {block.Id} ({block.X},{block.Y})");
                    break;

                case Business biz:
                    if (!block.TryAddBusiness(biz))
                    {
                        // Block rejected it (wrong zone or full) — re-queue.
                        _pendingEntities.Insert(0, biz);
                        return;
                    }

                    _placedBusinesses.Add((biz, block));
                    Console.WriteLine($"  [Biz]     {biz.BusinessType} → Block {block.Id} ({block.X},{block.Y})");
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Utility helpers
        // ─────────────────────────────────────────────────────────────────────

        private bool CanAccept(CityBlock block, IZoneable entity)
        {
            return entity switch
            {
                Pop pop => block.CalculateCapacityForClass(pop.SocioeconomicClass) > 0,
                Business biz => block.CanFitBusiness(biz),
                _ => false
            };
        }

        private bool HasPlacedNeighbor(int x, int y)
        {
            return Cardinals(x, y).Any(c => _map.GetBlockAt(c.x, c.y) != null);
        }

        private (int x, int y) FindAnyAdjacentEmpty()
        {
            foreach (var block in _map.AllBlocks)
            {
                foreach (var coord in _map.GetAdjacentEmptyCoordinates(block.X, block.Y))
                    return coord;
            }

            // If the map is somehow empty, start at the origin.
            return (0, 0);
        }

        private static IEnumerable<(int x, int y)> Cardinals(int x, int y)
        {
            yield return (x, y + 1);
            yield return (x + 1, y);
            yield return (x, y - 1);
            yield return (x - 1, y);
        }

        private static int GetAffluenceScore(PopSocioeconomicClass? cls) => cls switch
        {
            PopSocioeconomicClass.Capitalist => 4,
            PopSocioeconomicClass.WhiteCollar => 3,
            PopSocioeconomicClass.BlueCollar => 2,
            PopSocioeconomicClass.Destitute => 1,
            _ => 0
        };

        private static string DescribeEntity(IZoneable e) => e switch
        {
            Pop p => $"{p.SocioeconomicClass} Pop ({p.Size})",
            Business b => b.BusinessType ?? "Unknown Business",
            _ => e.ToString() ?? "Unknown"
        };

        /// <summary>
        /// Reflection-free way to reset a block's socioeconomic lock.
        /// CityBlock exposes SocioeconomicLevel with an internal setter, so this
        /// helper lives here in the same assembly (Generators sits in the
        /// CyberpunkGenerator project alongside Models).
        /// </summary>
        private static void ResetBlockClass(CityBlock block)
        {
            // SocioeconomicLevel has an `internal set`, so this is legal from
            // within the CyberpunkGenerator assembly.
            block.SocioeconomicLevel = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Sorting
        // ─────────────────────────────────────────────────────────────────────

        private void SortPendingEntities()
        {
            _pendingEntities.Sort(new GentrificationComparer());
        }

        /// <summary>
        /// Gentrification order:
        ///   1. Affluence descending (Capitalist → Destitute → Industrial/null)
        ///   2. Placement type descending (Residential=3 > Commercial=2 > Industrial=1)
        ///   3. Stable random tie-breaker (assigned at entity creation time)
        /// </summary>
        private class GentrificationComparer : IComparer<IZoneable>
        {
            public int Compare(IZoneable? x, IZoneable? y)
            {
                if (x is null || y is null) return 0;

                int scoreX = GetAffluenceScore(x.TargetClass);
                int scoreY = GetAffluenceScore(y.TargetClass);
                if (scoreX != scoreY)
                    return scoreY.CompareTo(scoreX); // highest first

                if (x.PlacementType != y.PlacementType)
                    return y.PlacementType.CompareTo(x.PlacementType); // highest enum value first

                return x.PlacementSeed.CompareTo(y.PlacementSeed);
            }
        }
    }
}