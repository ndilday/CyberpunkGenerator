using CyberpunkGenerator.Data;

namespace CyberpunkGenerator.Models
{
    public enum BusinessZoneType { Industrial, Commercial }

    public class Business : IZoneable
    {
        private static readonly Random _rng = new Random();

        public string? Name { get; set; }
        public string? BusinessType { get; set; }

        public BusinessZoneType ZoneType { get; set; }
        public PopSocioeconomicClass? TargetClass { get; set; }

        /// <summary>
        /// True for businesses that occupy an entire block exclusively:
        ///   - All Industrial buildings (factories, plants, mines)
        ///   - Large Commercial offices (Corp HQs, research facilities)
        /// False for retail/service businesses that share a MixedUse block.
        /// </summary>
        public bool IsWholeBlock { get; set; }

        // --- IZoneable Implementation ---
        public PlacementType PlacementType => ZoneType == BusinessZoneType.Commercial
            ? PlacementType.Commercial
            : PlacementType.Industrial;

        public int PlacementSeed { get; } = _rng.Next();
        // --------------------------------

        public Dictionary<MarketGood, float> Outputs { get; set; } = new();
        public Dictionary<MarketGood, float> InputGoods { get; set; } = new();
        public Dictionary<JobRole, int> RequiredLabor { get; set; } = new();

        public List<Pop> Employees { get; set; } = new();

        // ── Capacity reservation tracking ────────────────────────────────────
        //
        // Tracks how many units of each output good have been reserved by
        // contracts or patronage links. Keyed by MarketGood to mirror Outputs.
        // A good is considered at capacity when ReservedOutput[good] >= Outputs[good].
        private readonly Dictionary<MarketGood, float> _reservedOutput = new();

        /// <summary>
        /// Returns the total reserved quantity for a given output good.
        /// Returns 0 if no reservations exist for that good.
        /// </summary>
        public float GetReservedOutput(MarketGood good) =>
            _reservedOutput.GetValueOrDefault(good, 0f);

        /// <summary>
        /// Returns the remaining available capacity for a given output good.
        /// Returns 0 if the good is not produced by this business.
        /// </summary>
        public float GetRemainingCapacity(MarketGood good)
        {
            if (!Outputs.TryGetValue(good, out float total)) return 0f;
            return Math.Max(0f, total - GetReservedOutput(good));
        }

        /// <summary>
        /// Returns the utilization ratio [0.0, 1.0] for a given output good.
        /// Returns 1.0 (fully utilized) if the good is not produced here.
        /// </summary>
        public float GetUtilization(MarketGood good)
        {
            if (!Outputs.TryGetValue(good, out float total)) return 1f;
            if (total <= 0f) return 1f;
            return Math.Clamp(GetReservedOutput(good) / total, 0f, 1f);
        }

        /// <summary>
        /// Returns the amenity multiplier for this business based on its
        /// utilization of the given output good, using a piecewise threshold:
        ///   - Below AmenityCapacityFullCreditThreshold: full credit (1.0)
        ///   - Between threshold and 100%: linear decay to 0.0
        ///   - At 100%: no credit (0.0)
        /// Uses the most-utilized output good as the representative utilization.
        /// </summary>
        public float GetAmenityCapacityMultiplier()
        {
            if (Outputs.Count == 0) return 1f;

            // Use the highest utilization across all output goods as the
            // representative figure — a business bottlenecked on any output
            // should be considered congested.
            float maxUtilization = Outputs.Keys.Max(g => GetUtilization(g));

            const float threshold = EconomyConstants.AmenityCapacityFullCreditThreshold;

            if (maxUtilization <= threshold) return 1f;
            if (maxUtilization >= 1f) return 0f;

            // Linear decay from threshold → 1.0 maps to multiplier 1.0 → 0.0
            return 1f - (maxUtilization - threshold) / (1f - threshold);
        }

        /// <summary>
        /// Reserves up to <paramref name="quantity"/> units of <paramref name="good"/>
        /// and returns the amount actually reserved (capped at remaining capacity).
        /// </summary>
        public float ReserveCapacity(MarketGood good, float quantity)
        {
            float available = GetRemainingCapacity(good);
            float reserved = Math.Min(available, quantity);
            if (reserved <= 0f) return 0f;

            if (!_reservedOutput.ContainsKey(good))
                _reservedOutput[good] = 0f;

            _reservedOutput[good] += reserved;
            return reserved;
        }

        /// <summary>
        /// Releases a previously reserved quantity of <paramref name="good"/>.
        /// Called when a contract or patronage link is dissolved (e.g., displacement).
        /// </summary>
        public void ReleaseCapacity(MarketGood good, float quantity)
        {
            if (!_reservedOutput.ContainsKey(good)) return;
            _reservedOutput[good] = Math.Max(0f, _reservedOutput[good] - quantity);
        }

        // ── Bidirectional relationship tracking ──────────────────────────────

        /// <summary>
        /// Contracts where this business is the consumer (receiving goods).
        /// </summary>
        public List<Contract> InboundContracts { get; } = new();

        /// <summary>
        /// Contracts where this business is the supplier (providing goods).
        /// </summary>
        public List<Contract> OutboundContracts { get; } = new();

        /// <summary>
        /// Patronage links where this business is the supplier (serving pops).
        /// </summary>
        public List<Patronage> InboundPatronage { get; } = new();

        /// <summary>
        /// Releases all inbound and outbound contracts and patronage links,
        /// freeing reserved capacity on all counterparties.
        /// Called when this business is evicted during displacement.
        /// </summary>
        public void ReleaseAllLinks()
        {
            // Release capacity this business has reserved on its suppliers.
            foreach (var contract in InboundContracts)
            {
                contract.Supplier.ReleaseCapacity(contract.Good, contract.Quantity);
                contract.Supplier.OutboundContracts.Remove(contract);
            }
            InboundContracts.Clear();

            // Release capacity this business has promised to its consumers.
            foreach (var contract in OutboundContracts)
            {
                contract.Consumer.ReleaseCapacity(contract.Good, contract.Quantity);
                contract.Consumer.InboundContracts.Remove(contract);
            }
            OutboundContracts.Clear();

            // Release capacity this business has promised to its patron pops.
            foreach (var patronage in InboundPatronage)
            {
                patronage.Consumer.OutboundPatronage.Remove(patronage);
            }
            InboundPatronage.Clear();

            // Reset all reserved output since all links are gone.
            _reservedOutput.Clear();
        }

        public override string ToString() => $"{Name} ({BusinessType})";
    }

    /// <summary>
    /// Shared numeric constants referenced by both Business and ZoningEngine
    /// to avoid a circular dependency on EconomyBlueprints.
    /// </summary>
    public static class EconomyConstants
    {
        /// <summary>
        /// Utilization threshold below which a business provides full amenity
        /// credit. Above this threshold, amenity contribution decays linearly
        /// to zero at 100% utilization.
        /// </summary>
        public const float AmenityCapacityFullCreditThreshold = 0.75f;

        /// <summary>
        /// Multiplier applied to the estimated transportation demand per
        /// BlueCollar pop when heuristically estimating TransitDepot need.
        /// One depot per this many BlueCollar pops.
        /// </summary>
        public const int TransitDepotPopsPerDepot = 2000;

        /// <summary>
        /// One DistributionHub per this many industrial businesses.
        /// Used for heuristic spawning in CitySimulator.
        /// </summary>
        public const int DistributionHubsPerIndustrialBusiness = 4;
    }
}