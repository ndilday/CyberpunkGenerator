using CyberpunkGenerator.Data;

namespace CyberpunkGenerator.Models
{
    public enum PlacementType
    {
        Industrial = 1,
        Commercial = 2,
        Residential = 3
    }

    public interface IZoneable
    {
        // Used for the Primary Sort (Affluence)
        PopSocioeconomicClass? TargetClass { get; }

        // Used for the Secondary Sort (Res > Com > Ind)
        PlacementType PlacementType { get; }

        // Used for the Tertiary Sort (Stable random tie-breaker)
        int PlacementSeed { get; }
    }
}