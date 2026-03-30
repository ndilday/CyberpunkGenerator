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
    ///   • Pops are additionally attracted toward high-desirability coordinates as
    ///     recorded in the per-class desirability heatmap, which accumulates amenity
    ///     contributions from every placed business.
    ///   • Commercial businesses are additionally attracted toward high residential
    ///     density coordinates for their target class.
    ///   • Residential placement scoring applies a height penalty: each projected
    ///     floor above ground adds HeightPenaltyPerFloor effective distance units,
    ///     creating organic density gradients without a hard tower height cap.
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

        // ── Desirability heatmap ─────────────────────────────────────────────
        // Keyed by socioeconomic class, then by grid coordinate.
        // Accumulates signed amenity contributions from every placed business,
        // spread outward using inverse-distance decay up to AmenityWriteRadius.
        // Queried in O(1) during pop placement scoring.
        private readonly Dictionary<PopSocioeconomicClass, Dictionary<(int x, int y), float>>
            _desirabilityMap = new();

        // ── Population density map ───────────────────────────────────────────
        // Keyed by socioeconomic class, then by grid coordinate.
        // Tracks total resident headcount per class per cell.
        // Queried by commercial business gravity to seek dense customer zones.
        private readonly Dictionary<PopSocioeconomicClass, Dictionary<(int x, int y), int>>
            _populationDensityMap = new();

        private static readonly Random _rng = new();

        public ZoningEngine(List<Pop> allPops, List<Business> allBusinesses)
        {
            _map = new CityMap();
            _pendingEntities = new List<IZoneable>();
            _pendingEntities.AddRange(allPops);
            _pendingEntities.AddRange(allBusinesses);

            // Initialise desirability and density map buckets for all classes.
            foreach (PopSocioeconomicClass cls in Enum.GetValues(typeof(PopSocioeconomicClass)))
            {
                _desirabilityMap[cls] = new Dictionary<(int, int), float>();
                _populationDensityMap[cls] = new Dictionary<(int, int), int>();
            }
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
            UpdateDesirabilityMap(seedHQ, 0, 0);
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
            //
            //    For residential pops, the effective distance is augmented by
            //    a height penalty: ProjectedFloorCount * HeightPenaltyPerFloor.
            //    This makes tall blocks less attractive than nearby empty land,
            //    producing organic density gradients without a hard height cap.

            CityBlock? bestDirectFit = null;
            (int x, int y)? bestEmptyCoord = null;
            CityBlock? bestDisplaceable = null;
            float bestEffectiveDistance = float.MaxValue;

            var visited = new HashSet<(int, int)>();
            var queue = new Queue<(int x, int y, int dist)>();
            queue.Enqueue((targetX, targetY, 0));
            visited.Add((targetX, targetY));

            // Cutoff in terms of raw Manhattan distance. We stop BFS expansion
            // once the raw distance exceeds the best effective distance found
            // so far (conservative: a block at raw distance d+1 with zero height
            // penalty can never beat an effective distance of d).
            float cutoffEffective = float.MaxValue;

            while (queue.Count > 0)
            {
                var (cx, cy, dist) = queue.Dequeue();

                // Prune: raw distance already exceeds best effective distance found.
                if (dist > cutoffEffective) break;

                var existingBlock = _map.GetBlockAt(cx, cy);

                // Compute effective distance for this coordinate.
                float effectiveDist = ComputeEffectiveDistance(entity, existingBlock, cx, cy, dist);

                if (existingBlock != null)
                {
                    if (CanAccept(existingBlock, entity))
                    {
                        if (effectiveDist < bestEffectiveDistance ||
                            (effectiveDist == bestEffectiveDistance && _rng.Next(2) == 0))
                        {
                            bestDirectFit = existingBlock;
                            bestEmptyCoord = null;
                            bestEffectiveDistance = effectiveDist;
                            cutoffEffective = effectiveDist;
                        }
                    }
                    else if (bestDirectFit == null && CanDisplace(existingBlock, entity))
                    {
                        if (effectiveDist < bestEffectiveDistance ||
                            (effectiveDist == bestEffectiveDistance && _rng.Next(2) == 0))
                        {
                            bestDisplaceable = existingBlock;
                            bestEffectiveDistance = effectiveDist;
                        }
                    }
                }
                else
                {
                    // Empty coordinate — a new block could go here.
                    if (HasPlacedNeighbor(cx, cy))
                    {
                        // For empty coords the effective distance equals raw distance
                        // (no height penalty on an empty block).
                        if (bestDirectFit == null &&
                            (dist < bestEffectiveDistance ||
                             (dist == bestEffectiveDistance && _rng.Next(2) == 0)))
                        {
                            bestEmptyCoord = (cx, cy);
                            bestEffectiveDistance = dist;
                            cutoffEffective = dist;
                        }
                    }
                }

                foreach (var (nx, ny) in Cardinals(cx, cy))
                {
                    if (!visited.Contains((nx, ny)) && dist + 1 <= cutoffEffective + 1)
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
                var fallback = FindAnyAdjacentEmpty();
                var newBlock = CreateBlockForEntity(fallback.x, fallback.y, entity);
                CommitToBlock(newBlock, entity);
                Console.WriteLine($"  [Fallback] {DescribeEntity(entity)} placed at ({fallback.x},{fallback.y}).");
            }
        }

        /// <summary>
        /// Returns the effective distance for scoring a candidate coordinate.
        /// For residential pops, adds a height penalty based on the projected
        /// floor count if this pop were placed here.
        /// For all other entity types, returns the raw Manhattan distance.
        /// </summary>
        private float ComputeEffectiveDistance(
            IZoneable entity,
            CityBlock? existingBlock,
            int cx, int cy,
            int manhattanDist)
        {
            if (entity is not Pop pop) return manhattanDist;

            // Determine the sqm this pop (or its floor-capped split) would add.
            int floorCapacity = pop.SocioeconomicClass switch
            {
                var c => (int)(EconomyBlueprints.FloorHeightSqm / EconomyBlueprints.SqmPerPerson[c])
            };
            int placementSize = Math.Min(pop.Size, floorCapacity);
            int additionalSqm = placementSize * EconomyBlueprints.SqmPerPerson[pop.SocioeconomicClass];

            int projectedFloor = existingBlock != null
                ? existingBlock.ProjectedFloorCount(additionalSqm)
                : 0; // empty block: ground floor, no penalty

            float heightPenalty = projectedFloor * EconomyBlueprints.HeightPenaltyPerFloor;
            return manhattanDist + heightPenalty;
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
        /// Pops want to live near two things, blended by weight:
        ///   1. Their employers (businesses whose RequiredLabor includes their role).
        ///   2. High-desirability coordinates for their class (from the heatmap).
        ///
        /// The amenity contribution is read directly from the precomputed
        /// desirability map: O(1) lookup, no radius search required.
        /// </summary>
        private (float x, float y) GravityForPop(Pop pop)
        {
            var popRole = new JobRole(pop.SocioeconomicClass, pop.Field);

            // ── Component 1: employer centroid ───────────────────────────────
            var employers = _placedBusinesses
                .Where(pb => pb.business.RequiredLabor.ContainsKey(popRole))
                .ToList();

            (float ex, float ey) = employers.Count > 0
                ? Centroid(employers.Select(pb => (pb.block.X, pb.block.Y)))
                : (0f, 0f);

            // ── Component 2: amenity-weighted centroid ───────────────────────
            // Find all coordinates with a non-zero desirability score for this
            // class, weight each by its score (skip negative — we want to be
            // attracted toward positive amenities, not repelled from this step),
            // and compute a weighted centroid.
            (float ax, float ay) = AmenityCentroidForClass(pop.SocioeconomicClass);

            // ── Blend ────────────────────────────────────────────────────────
            // If amenity data is absent (early generation), fall back entirely
            // to employer gravity.
            bool hasAmenitySignal = ax != 0f || ay != 0f;

            if (!hasAmenitySignal)
                return (ex, ey);

            float w = EconomyBlueprints.AmenityGravityWeight;
            return (
                ex * (1f - w) + ax * w,
                ey * (1f - w) + ay * w
            );
        }

        /// <summary>
        /// Commercial businesses want to be near their target customers.
        /// This is a blend of:
        ///   1. Centroid of blocks where the target class lives.
        ///   2. Centroid weighted by population density of the target class,
        ///      so businesses prefer the busiest corners over sparse outskirts.
        /// </summary>
        private (float x, float y) GravityForCommercialBusiness(Business biz)
        {
            if (!biz.TargetClass.HasValue) return (0f, 0f);
            var cls = biz.TargetClass.Value;

            // ── Component 1: target class block centroid ─────────────────────
            var customerBlocks = _map.AllBlocks
                .Where(b => b.SocioeconomicLevel == cls && b.Pops.Count > 0)
                .ToList();

            (float cx, float cy) = customerBlocks.Count > 0
                ? Centroid(customerBlocks.Select(b => (b.X, b.Y)))
                : (0f, 0f);

            // ── Component 2: density-weighted centroid ───────────────────────
            (float dx, float dy) = DensityCentroidForClass(cls);

            bool hasDensitySignal = dx != 0f || dy != 0f;

            if (!hasDensitySignal)
                return (cx, cy);

            float w = EconomyBlueprints.DensityGravityWeight;
            return (
                cx * (1f - w) + dx * w,
                cy * (1f - w) + dy * w
            );
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

        // ─────────────────────────────────────────────────────────────────────
        // Heatmap updates
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called after a business is committed to a block. Spreads the
        /// business's amenity contributions outward up to AmenityWriteRadius
        /// using inverse-distance decay, updating the desirability map for
        /// every affected coordinate and class.
        /// </summary>
        private void UpdateDesirabilityMap(Business biz, int originX, int originY)
        {
            if (biz.BusinessType == null) return;

            for (int dx = -EconomyBlueprints.AmenityWriteRadius;
                     dx <= EconomyBlueprints.AmenityWriteRadius; dx++)
            {
                for (int dy = -EconomyBlueprints.AmenityWriteRadius;
                         dy <= EconomyBlueprints.AmenityWriteRadius; dy++)
                {
                    int manhattanDist = Math.Abs(dx) + Math.Abs(dy);
                    if (manhattanDist > EconomyBlueprints.AmenityWriteRadius) continue;

                    var coord = (originX + dx, originY + dy);

                    foreach (PopSocioeconomicClass cls in Enum.GetValues(typeof(PopSocioeconomicClass)))
                    {
                        float contribution = EconomyBlueprints.GetAmenityContribution(
                            biz.BusinessType, cls, manhattanDist);

                        if (contribution == 0f) continue;

                        if (!_desirabilityMap[cls].ContainsKey(coord))
                            _desirabilityMap[cls][coord] = 0f;

                        _desirabilityMap[cls][coord] += contribution;
                    }
                }
            }
        }

        /// <summary>
        /// Called after a business is evicted during displacement. Subtracts its
        /// previously written amenity contributions from the desirability map.
        /// Minor floating-point drift over many displacement cycles is acceptable.
        /// </summary>
        private void RemoveFromDesirabilityMap(Business biz, int originX, int originY)
        {
            if (biz.BusinessType == null) return;

            for (int dx = -EconomyBlueprints.AmenityWriteRadius;
                     dx <= EconomyBlueprints.AmenityWriteRadius; dx++)
            {
                for (int dy = -EconomyBlueprints.AmenityWriteRadius;
                         dy <= EconomyBlueprints.AmenityWriteRadius; dy++)
                {
                    int manhattanDist = Math.Abs(dx) + Math.Abs(dy);
                    if (manhattanDist > EconomyBlueprints.AmenityWriteRadius) continue;

                    var coord = (originX + dx, originY + dy);

                    foreach (PopSocioeconomicClass cls in Enum.GetValues(typeof(PopSocioeconomicClass)))
                    {
                        float contribution = EconomyBlueprints.GetAmenityContribution(
                            biz.BusinessType, cls, manhattanDist);

                        if (contribution == 0f) continue;

                        if (_desirabilityMap[cls].ContainsKey(coord))
                            _desirabilityMap[cls][coord] -= contribution;
                    }
                }
            }
        }

        /// <summary>
        /// Called after a pop is committed to a block. Increments the
        /// population density map for that class at that coordinate.
        /// </summary>
        private void UpdatePopulationDensityMap(Pop pop, int x, int y)
        {
            var cls = pop.SocioeconomicClass;
            var coord = (x, y);

            if (!_populationDensityMap[cls].ContainsKey(coord))
                _populationDensityMap[cls][coord] = 0;

            _populationDensityMap[cls][coord] += pop.Size;
        }

        /// <summary>
        /// Called when a pop is evicted during displacement. Decrements the
        /// population density map for that class at that coordinate.
        /// </summary>
        private void RemoveFromPopulationDensityMap(Pop pop, int x, int y)
        {
            var cls = pop.SocioeconomicClass;
            var coord = (x, y);

            if (_populationDensityMap[cls].ContainsKey(coord))
                _populationDensityMap[cls][coord] =
                    Math.Max(0, _populationDensityMap[cls][coord] - pop.Size);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Heatmap centroid helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a weighted centroid of all coordinates with positive
        /// desirability for the given class. Coordinates with negative or zero
        /// desirability are excluded — we want attraction toward amenities,
        /// not repulsion away from nuisances, in the gravity target calculation.
        /// (Repulsion is handled implicitly: pops avoid being placed near
        /// industrial zones because those zones poison the desirability scores
        /// of nearby coordinates, making those coordinates lose BFS contests
        /// against cleaner alternatives.)
        /// </summary>
        private (float x, float y) AmenityCentroidForClass(PopSocioeconomicClass cls)
        {
            var map = _desirabilityMap[cls];
            if (map.Count == 0) return (0f, 0f);

            float totalWeight = 0f;
            float wx = 0f, wy = 0f;

            foreach (var (coord, score) in map)
            {
                if (score <= 0f) continue;
                wx += coord.x * score;
                wy += coord.y * score;
                totalWeight += score;
            }

            return totalWeight > 0f
                ? (wx / totalWeight, wy / totalWeight)
                : (0f, 0f);
        }

        /// <summary>
        /// Returns a weighted centroid of all coordinates where the given class
        /// has residential population, weighted by headcount.
        /// </summary>
        private (float x, float y) DensityCentroidForClass(PopSocioeconomicClass cls)
        {
            var map = _populationDensityMap[cls];
            if (map.Count == 0) return (0f, 0f);

            float totalWeight = 0f;
            float wx = 0f, wy = 0f;

            foreach (var (coord, count) in map)
            {
                if (count <= 0) continue;
                wx += coord.x * count;
                wy += coord.y * count;
                totalWeight += count;
            }

            return totalWeight > 0f
                ? (wx / totalWeight, wy / totalWeight)
                : (0f, 0f);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Displacement
        // ─────────────────────────────────────────────────────────────────────

        private bool CanDisplace(CityBlock block, IZoneable incoming)
        {
            if (block.Type == BlockType.Industrial) return false;
            if (!incoming.TargetClass.HasValue) return false;
            if (!block.SocioeconomicLevel.HasValue) return false;

            return GetAffluenceScore(incoming.TargetClass) > GetAffluenceScore(block.SocioeconomicLevel);
        }

        private void DisplaceAndOccupy(CityBlock block, IZoneable incoming)
        {
            Console.WriteLine($"  [Gentrify] {DescribeEntity(incoming)} displaces " +
                              $"{block.SocioeconomicLevel} residents at Block {block.Id} ({block.X},{block.Y}).");

            // Evict pops — update density map before re-queuing.
            foreach (var evictedPop in block.Pops)
            {
                RemoveFromPopulationDensityMap(evictedPop, block.X, block.Y);
                _pendingEntities.Add(evictedPop);
                _displacementsThisPlacement++;
            }

            // Evict businesses — update desirability map before re-queuing.
            foreach (var evictedBiz in block.Businesses)
            {
                RemoveFromDesirabilityMap(evictedBiz, block.X, block.Y);
                _placedBusinesses.RemoveAll(pb => pb.business == evictedBiz);
                _pendingEntities.Add(evictedBiz);
                _displacementsThisPlacement++;
            }

            block.Pops.Clear();
            block.Businesses.Clear();
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
                    int capacity = block.CalculateCapacityForClass(pop.SocioeconomicClass);
                    if (capacity <= 0)
                    {
                        _pendingEntities.Insert(0, pop);
                        return;
                    }

                    if (pop.Size > capacity)
                    {
                        var overflow = pop.Split(capacity);
                        _pendingEntities.Insert(0, overflow);
                    }

                    block.AddPop(pop);
                    UpdatePopulationDensityMap(pop, block.X, block.Y);
                    Console.WriteLine($"  [Pop]     {pop} → Block {block.Id} ({block.X},{block.Y})");
                    break;

                case Business biz:
                    if (!block.TryAddBusiness(biz))
                    {
                        _pendingEntities.Insert(0, biz);
                        return;
                    }

                    _placedBusinesses.Add((biz, block));
                    UpdateDesirabilityMap(biz, block.X, block.Y);
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

        private static void ResetBlockClass(CityBlock block)
        {
            block.SocioeconomicLevel = null;
        }

        private static (float x, float y) Centroid(IEnumerable<(int x, int y)> points)
        {
            var list = points.ToList();
            return ((float)list.Average(p => p.x), (float)list.Average(p => p.y));
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
                    return scoreY.CompareTo(scoreX);

                if (x.PlacementType != y.PlacementType)
                    return y.PlacementType.CompareTo(x.PlacementType);

                return x.PlacementSeed.CompareTo(y.PlacementSeed);
            }
        }
    }
}