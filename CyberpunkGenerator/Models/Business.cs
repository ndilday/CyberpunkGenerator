using CyberpunkGenerator.Data;
using System;
using System.Collections.Generic;

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

        // --- IZoneable Implementation ---
        // Industrial businesses don't have a class, Commercial businesses might.
        public PlacementType PlacementType => ZoneType == BusinessZoneType.Commercial
            ? PlacementType.Commercial
            : PlacementType.Industrial;

        public int PlacementSeed { get; } = _rng.Next();
        // --------------------------------

        public Dictionary<MarketGood, float> Outputs { get; set; } = new();
        public Dictionary<MarketGood, float> InputGoods { get; set; } = new();
        public Dictionary<JobRole, int> RequiredLabor { get; set; } = new();

        public List<Pop> Employees { get; set; } = new();

        public override string ToString() => $"{Name} ({BusinessType})";
    }
}