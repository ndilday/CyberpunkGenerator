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
    ///     pending queue with all its links released.
    ///   • As each entity is placed, Contract and Patronage links are formed
    ///     greedily by proximity, reserving capacity on supplier businesses.
    ///     Displacement releases all links on the evicted entity; links on its
    ///     counterparties are also released, freeing their capacity for
    ///     re-assignment when those entities are next placed.
    ///   • Amenity contributions are scaled by the supplier's remaining capacity
    ///     using a piecewise threshold (full credit below the threshold, linear
    ///     decay to zero above it).
    ///   • Transportation points are calculated as a post-placement derived metric
    ///     by summing quantity * distance across all formed links.
    /// </summary>
    public class ZoningEngine
    {
        private readonly CityMap _map;

        // Sorted working queue; re-sorted whenever displacements add new items.
        private readonly List<IZoneable> _pendingEntities;

        // All businesses already placed — used for gravity lookups and link formation.
        private readonly List<(Business business, CityBlock block)> _placedBusinesses = new();

        // All pops already placed — used for link formation lookups.
        private readonly List<(Pop pop, CityBlock block)> _placedPops = new();

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
            // and place it at (0,0) before the loop starts.
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
            FormContractLinks(seedHQ, seedBlock);
            Console.WriteLine("[Seed] Mega-Corp Headquarters placed at (0,0).");

            Console.WriteLine($"Starting Zoning Engine with {_pendingEntities.Count} entities to place.");

            int pass = 0;
            while (_pendingEntities.Count > 0)
            {
                pass++;
                var entity = _pendingEntities[0];
                _pendingEntities.RemoveAt(0);

                PlaceEntity(entity);

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
            (float gx, float gy) = CalculateGravityTarget(entity);
            int targetX = (int)Math.Round(gx);
            int targetY = (int)Math.Round(gy);

            CityBlock? bestDirectFit = null;
            (int x, int y)? bestEmptyCoord = null;
            CityBlock? bestDisplaceable = null;
            float bestEffectiveDistance = float.MaxValue;

            var visited = new HashSet<(int, int)>();
            var queue = new Queue<(int x, int y, int dist)>();
            queue.Enqueue((targetX, targetY, 0));
            visited.Add((targetX, targetY));

            float cutoffEffective = float.MaxValue;

            while (queue.Count > 0)
            {
                var (cx, cy, dist) = queue.Dequeue();

                if (dist > cutoffEffective) break;

                var existingBlock = _map.GetBlockAt(cx, cy);
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
                    if (HasPlacedNeighbor(cx, cy))
                    {
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

            int floorCapacity = pop.SocioeconomicClass switch
            {
                var c => (int)(EconomyBlueprints.FloorHeightSqm / EconomyBlueprints.SqmPerPerson[c])
            };
            int placementSize = Math.Min(pop.Size, floorCapacity);
            int additionalSqm = placementSize * EconomyBlueprints.SqmPerPerson[pop.SocioeconomicClass];

            int projectedFloor = existingBlock != null
                ? existingBlock.ProjectedFloorCount(additionalSqm)
                : 0;

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

        private (float x, float y) GravityForPop(Pop pop)
        {
            var popRole = new JobRole(pop.SocioeconomicClass, pop.Field);

            var employers = _placedBusinesses
                .Where(pb => pb.business.RequiredLabor.ContainsKey(popRole))
                .ToList();

            (float ex, float ey) = employers.Count > 0
                ? Centroid(employers.Select(pb => (pb.block.X, pb.block.Y)))
                : (0f, 0f);

            (float ax, float ay) = AmenityCentroidForClass(pop.SocioeconomicClass);

            bool hasAmenitySignal = ax != 0f || ay != 0f;

            if (!hasAmenitySignal)
                return (ex, ey);

            float w = EconomyBlueprints.AmenityGravityWeight;
            return (
                ex * (1f - w) + ax * w,
                ey * (1f - w) + ay * w
            );
        }

        private (float x, float y) GravityForCommercialBusiness(Business biz)
        {
            if (!biz.TargetClass.HasValue) return (0f, 0f);
            var cls = biz.TargetClass.Value;

            var customerBlocks = _map.AllBlocks
                .Where(b => b.SocioeconomicLevel == cls && b.Pops.Count > 0)
                .ToList();

            (float cx, float cy) = customerBlocks.Count > 0
                ? Centroid(customerBlocks.Select(b => (b.X, b.Y)))
                : (0f, 0f);

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
        // Contract and Patronage link formation
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Forms Contract links for a newly placed business. For each of its
        /// InputGoods (excluding Electricity and other freight-excluded goods),
        /// scans placed businesses that supply that good, sorted by distance,
        /// and greedily reserves capacity until the need is fully met or all
        /// suppliers are exhausted.
        ///
        /// A single input need may be split across multiple suppliers if no
        /// single business has sufficient remaining capacity.
        /// </summary>
        private void FormContractLinks(Business consumer, CityBlock consumerBlock)
        {
            foreach (var (inputGood, requiredQty) in consumer.InputGoods)
            {
                // Electricity is infrastructure — no freight contracts.
                if (EconomyBlueprints.FreightExcludedGoodTypes.Contains(inputGood.Type))
                    continue;

                float remaining = requiredQty;

                // Find all placed businesses that produce this good, sorted by
                // ascending distance so the nearest supplier is preferred.
                var suppliers = _placedBusinesses
                    .Where(pb => pb.business.Outputs.ContainsKey(inputGood)
                                 && pb.business.GetRemainingCapacity(inputGood) > 0)
                    .OrderBy(pb => _map.GetDistance(consumerBlock.X, consumerBlock.Y, pb.block.X, pb.block.Y))
                    .ToList();

                foreach (var (supplier, supplierBlock) in suppliers)
                {
                    if (remaining <= 0f) break;

                    float reserved = supplier.ReserveCapacity(inputGood, remaining);
                    if (reserved <= 0f) continue;

                    int distance = _map.GetDistance(
                        consumerBlock.X, consumerBlock.Y,
                        supplierBlock.X, supplierBlock.Y);

                    var contract = new Contract(consumer, supplier, inputGood, reserved)
                    {
                        Distance = distance
                    };

                    consumer.InboundContracts.Add(contract);
                    supplier.OutboundContracts.Add(contract);

                    remaining -= reserved;
                }

                if (remaining > 0f)
                {
                    Console.WriteLine($"  [Unmet Contract] {consumer.BusinessType} needs " +
                                      $"{remaining:N0} more {inputGood} — no supplier capacity available.");
                }
            }
        }

        /// <summary>
        /// Forms Patronage links for a newly placed pop. For each of the pop's
        /// class needs (excluding housing goods, which generate no transport cost),
        /// scans placed businesses that supply that good at retail, sorted by
        /// distance, and greedily reserves capacity until the need is fully met
        /// or all suppliers are exhausted.
        ///
        /// Need quantity is scaled by pop size (needs are defined per 100 people).
        /// A single need may be split across multiple suppliers.
        /// </summary>
        private void FormPatronageLinks(Pop pop, CityBlock popBlock)
        {
            if (!EconomyBlueprints.PopNeeds.TryGetValue(pop.SocioeconomicClass, out var needs))
                return;

            foreach (var (need, quantityPer100) in needs)
            {
                // Housing is always distance 0 — no transport cost, skip.
                if (EconomyBlueprints.HousingGoodTypes.Contains(need.Type))
                    continue;

                float totalNeeded = quantityPer100 * (pop.Size / 100f);
                float remaining = totalNeeded;

                // Find all placed businesses that supply this retail good,
                // sorted by ascending distance so the nearest supplier is preferred.
                var suppliers = _placedBusinesses
                    .Where(pb => pb.business.Outputs.ContainsKey(need)
                                 && pb.business.GetRemainingCapacity(need) > 0)
                    .OrderBy(pb => _map.GetDistance(popBlock.X, popBlock.Y, pb.block.X, pb.block.Y))
                    .ToList();

                foreach (var (supplier, supplierBlock) in suppliers)
                {
                    if (remaining <= 0f) break;

                    float reserved = supplier.ReserveCapacity(need, remaining);
                    if (reserved <= 0f) continue;

                    int distance = _map.GetDistance(
                        popBlock.X, popBlock.Y,
                        supplierBlock.X, supplierBlock.Y);

                    var patronage = new Patronage(pop, supplier, need, reserved)
                    {
                        Distance = distance
                    };

                    pop.OutboundPatronage.Add(patronage);
                    supplier.InboundPatronage.Add(patronage);

                    remaining -= reserved;
                }

                if (remaining > 0f)
                {
                    Console.WriteLine($"  [Unmet Patronage] {pop} needs " +
                                      $"{remaining:N0} more {need} — no supplier capacity available.");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Transportation point calculation
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Calculates city-wide transportation points by summing across all
        /// formed Contract and Patronage links. Returns pop transport points
        /// and freight transport points separately.
        ///
        /// Called by ZonedCityGenerator after GenerateMap() completes.
        /// </summary>
        public (float popTransport, float freightTransport) CalculateTransportationPoints()
        {
            float popTP = 0f;
            float freightTP = 0f;

            // Pop transport: sum all patronage link transportation points.
            foreach (var (pop, _) in _placedPops)
            {
                foreach (var patronage in pop.OutboundPatronage)
                    popTP += patronage.TransportationPoints;
            }

            // Freight transport: sum all contract transportation points.
            // Use outbound contracts to avoid double-counting (each contract
            // is registered on both consumer and supplier).
            foreach (var (biz, _) in _placedBusinesses)
            {
                foreach (var contract in biz.OutboundContracts)
                    freightTP += contract.TransportationPoints;
            }

            return (popTP, freightTP);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Heatmap updates
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called after a business is committed to a block. Spreads the
        /// business's amenity contributions outward up to AmenityWriteRadius
        /// using inverse-distance decay, scaled by the business's current
        /// capacity utilization multiplier. Updates the desirability map for
        /// every affected coordinate and class.
        /// </summary>
        private void UpdateDesirabilityMap(Business biz, int originX, int originY)
        {
            if (biz.BusinessType == null) return;

            float capacityMultiplier = biz.GetAmenityCapacityMultiplier();

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
                            biz.BusinessType, cls, manhattanDist, capacityMultiplier);

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
        /// Uses the same capacity multiplier that was active when the contributions
        /// were written — minor drift is acceptable.
        /// </summary>
        private void RemoveFromDesirabilityMap(Business biz, int originX, int originY)
        {
            if (biz.BusinessType == null) return;

            float capacityMultiplier = biz.GetAmenityCapacityMultiplier();

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
                            biz.BusinessType, cls, manhattanDist, capacityMultiplier);

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

            // Evict pops — release links, update density map, re-queue.
            foreach (var evictedPop in block.Pops)
            {
                evictedPop.ReleaseAllLinks();
                RemoveFromPopulationDensityMap(evictedPop, block.X, block.Y);
                _placedPops.RemoveAll(pp => pp.pop == evictedPop);
                _pendingEntities.Add(evictedPop);
                _displacementsThisPlacement++;
            }

            // Evict businesses — release links, update desirability map, re-queue.
            foreach (var evictedBiz in block.Businesses)
            {
                evictedBiz.ReleaseAllLinks();
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
                    _placedPops.Add((pop, block));
                    UpdatePopulationDensityMap(pop, block.X, block.Y);
                    FormPatronageLinks(pop, block);
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
                    FormContractLinks(biz, block);
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