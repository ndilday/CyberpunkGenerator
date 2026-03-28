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

        public const int UsableResidentialSqm = 1_000_000;
        public const int MaxCommercialBusinesses = 10;
        public const int MaxWholeBlockBusinesses = 1;  // Industrial and Office both allow exactly one

        public CityBlock(int id, BlockType type, int x, int y)
        {
            Id = id;
            Type = type;
            X = x;
            Y = y;
        }

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

        public int GetUsedResidentialSqm() => Pops.Sum(p => p.RequiredSqm);

        public int GetRemainingResidentialSqm()
        {
            // Industrial blocks have no residential space.
            // Office blocks are whole-block businesses — no room for residents.
            if (Type is BlockType.Industrial or BlockType.Office) return 0;
            return UsableResidentialSqm - GetUsedResidentialSqm();
        }

        public int CalculateCapacityForClass(PopSocioeconomicClass popClass)
        {
            if (Type is BlockType.Industrial or BlockType.Office) return 0;
            if (SocioeconomicLevel.HasValue && SocioeconomicLevel.Value != popClass) return 0;

            int remainingSqm = GetRemainingResidentialSqm();
            if (remainingSqm <= 0) return 0;

            int sqmPerPerson = EconomyBlueprints.SqmPerPerson[popClass];
            return remainingSqm / sqmPerPerson;
        }

        public void AddPop(Pop pop)
        {
            if (Type is BlockType.Industrial or BlockType.Office)
                throw new InvalidOperationException(
                    $"Cannot add pops to a {Type} block.");

            if (SocioeconomicLevel.HasValue && SocioeconomicLevel.Value != pop.SocioeconomicClass)
                throw new InvalidOperationException(
                    $"Block is locked to {SocioeconomicLevel.Value}, cannot add {pop.SocioeconomicClass}.");

            if (pop.RequiredSqm > GetRemainingResidentialSqm())
                throw new InvalidOperationException(
                    "Pop exceeds block residential capacity. Split it first.");

            if (!SocioeconomicLevel.HasValue)
                SocioeconomicLevel = pop.SocioeconomicClass;

            Pops.Add(pop);
        }

        public override string ToString()
        {
            int maxBiz = Type == BlockType.MixedUse ? MaxCommercialBusinesses : MaxWholeBlockBusinesses;
            string classLabel = SocioeconomicLevel.HasValue ? $"[{SocioeconomicLevel.Value}]" : "[Empty]";
            int popCount = Pops.Sum(p => p.Size);

            return $"Block {Id} at ({X},{Y}) ({Type}) {classLabel} | " +
                   $"Businesses: {Businesses.Count}/{maxBiz} | Pops: {popCount}";
        }
    }
}