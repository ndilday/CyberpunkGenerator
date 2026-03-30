using CyberpunkGenerator.Data;
using CyberpunkGenerator.Economy;

namespace CyberpunkGenerator.Models
{
    /// <summary>
    /// Industrial : whole-block, not residentially desirable (factories, plants)
    /// MixedUse   : shared block, residentially desirable (apartments, shops)
    /// Office     : whole-block, residentially desirable (corp HQs, research facilities)
    ///              — exclusive to one business like Industrial, but located in affluent
    ///              neighborhoods and counted as a liveable zone for neighborhood formation.
    /// </summary>
    public enum BlockType { Industrial, MixedUse, Office }

    public class CityBlock
    {
        public int Id { get; set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public BlockType Type { get; set; }

        public PopSocioeconomicClass? SocioeconomicLevel { get; internal set; }

        public List<Business> Businesses { get; private set; } = new();
        public List<Pop> Pops { get; private set; } = new();

        public const int MaxCommercialBusinesses = 10;
        public const int MaxWholeBlockBusinesses = 1;  // Industrial and Office both allow exactly one

        public CityBlock(int id, BlockType type, int x, int y)
        {
            Id = id;
            Type = type;
            X = x;
            Y = y;
        }

        // ── Residential capacity ─────────────────────────────────────────────

        public int GetUsedResidentialSqm() => Pops.Sum(p => p.RequiredSqm);

        /// <summary>
        /// Returns the projected floor count if <paramref name="additionalSqm"/>
        /// of residential space were added to this block.
        ///
        /// Floor 1 is ground level (0 to FloorHeightSqm sqm occupied).
        /// Integer division means a block that has used exactly one floor's worth
        /// of sqm projects to floor 1 before the next resident is added,
        /// floor 2 once that resident would push it past the threshold, etc.
        ///
        /// Used by the ZoningEngine to compute a height penalty during BFS
        /// scoring: a block that would become a 10-storey tower is treated as
        /// (10 * HeightPenaltyPerFloor) map units farther away than it physically is.
        /// </summary>
        public int ProjectedFloorCount(int additionalSqm)
        {
            return (int)((GetUsedResidentialSqm() + additionalSqm) / EconomyBlueprints.FloorHeightSqm);
        }

        public int GetRemainingResidentialSqm()
        {
            // Industrial blocks have no residential space.
            // Office blocks are whole-block businesses — no room for residents.
            if (Type is BlockType.Industrial or BlockType.Office) return 0;
            return int.MaxValue - GetUsedResidentialSqm(); // unbounded — towers grow as tall as needed
        }

        /// <summary>
        /// Returns how many residents of <paramref name="popClass"/> can be
        /// placed on this block in a single placement pass: one floor's worth.
        ///
        /// Capping at one floor per pass keeps the height accumulation granular
        /// so that the ZoningEngine's BFS always re-evaluates height penalties
        /// before committing another floor of population to the same block.
        /// Returns 0 if the block cannot accept this class at all.
        /// </summary>
        public int CalculateCapacityForClass(PopSocioeconomicClass popClass)
        {
            if (Type is BlockType.Industrial or BlockType.Office) return 0;
            if (SocioeconomicLevel.HasValue && SocioeconomicLevel.Value != popClass) return 0;

            // One floor's worth at a time so the ZoningEngine reassesses height
            // penalty before placing the next tranche of residents.
            int sqmPerPerson = EconomyBlueprints.SqmPerPerson[popClass];
            return (int)(EconomyBlueprints.FloorHeightSqm / sqmPerPerson);
        }

        // ── Business placement ───────────────────────────────────────────────

        public bool CanFitBusiness(Business b)
        {
            switch (Type)
            {
                case BlockType.Industrial:
                    // Only whole-block industrial businesses; one per block.
                    return b.ZoneType == BusinessZoneType.Industrial
                        && b.IsWholeBlock
                        && Businesses.Count < MaxWholeBlockBusinesses;

                case BlockType.Office:
                    // Only whole-block commercial businesses; one per block.
                    // Class-lock applies the same way as MixedUse.
                    if (b.ZoneType != BusinessZoneType.Commercial || !b.IsWholeBlock)
                        return false;
                    if (Businesses.Count >= MaxWholeBlockBusinesses)
                        return false;
                    if (SocioeconomicLevel.HasValue && b.TargetClass.HasValue
                        && SocioeconomicLevel.Value != b.TargetClass.Value)
                        return false;
                    return true;

                case BlockType.MixedUse:
                    // Only shared commercial businesses; up to MaxCommercialBusinesses.
                    if (b.ZoneType != BusinessZoneType.Commercial || b.IsWholeBlock)
                        return false;
                    if (Businesses.Count >= MaxCommercialBusinesses)
                        return false;
                    if (SocioeconomicLevel.HasValue && b.TargetClass.HasValue
                        && SocioeconomicLevel.Value != b.TargetClass.Value)
                        return false;
                    return true;

                default:
                    return false;
            }
        }

        public bool TryAddBusiness(Business b)
        {
            if (!CanFitBusiness(b)) return false;

            // Lock the block's class on first commercial placement.
            if (Type is BlockType.MixedUse or BlockType.Office
                && !SocioeconomicLevel.HasValue && b.TargetClass.HasValue)
            {
                SocioeconomicLevel = b.TargetClass.Value;
            }

            Businesses.Add(b);
            return true;
        }

        // ── Pop placement ────────────────────────────────────────────────────

        public void AddPop(Pop pop)
        {
            if (Type is BlockType.Industrial or BlockType.Office)
                throw new InvalidOperationException(
                    $"Cannot add pops to a {Type} block.");

            if (SocioeconomicLevel.HasValue && SocioeconomicLevel.Value != pop.SocioeconomicClass)
                throw new InvalidOperationException(
                    $"Block is locked to {SocioeconomicLevel.Value}, cannot add {pop.SocioeconomicClass}.");

            if (!SocioeconomicLevel.HasValue)
                SocioeconomicLevel = pop.SocioeconomicClass;

            Pops.Add(pop);
        }

        // ── Diagnostics ──────────────────────────────────────────────────────

        public override string ToString()
        {
            int maxBiz = Type == BlockType.MixedUse ? MaxCommercialBusinesses : MaxWholeBlockBusinesses;
            string classLabel = SocioeconomicLevel.HasValue ? $"[{SocioeconomicLevel.Value}]" : "[Empty]";
            int popCount = Pops.Sum(p => p.Size);
            int floors = ProjectedFloorCount(0);

            return $"Block {Id} at ({X},{Y}) ({Type}) {classLabel} | " +
                   $"Businesses: {Businesses.Count}/{maxBiz} | Pops: {popCount} | Floors: {floors}";
        }
    }
}