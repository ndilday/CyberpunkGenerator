using CyberpunkGenerator.Data;
using CyberpunkGenerator.Economy;
using System;

namespace CyberpunkGenerator.Models
{
    public class Pop : IZoneable
    {
        private static readonly Random _rng = new Random();

        public string Name { get; set; }
        public int Size { get; set; }
        public PopSocioeconomicClass SocioeconomicClass { get; set; }
        public PopField Field { get; set; }
        public bool IsEmployed { get; set; }

        public PopSocioeconomicClass? TargetClass => SocioeconomicClass;
        public PlacementType PlacementType => PlacementType.Residential;
        public int PlacementSeed { get; } = _rng.Next();

        // Dynamically calculates required space based on the central blueprints
        public int RequiredSqm => Size * EconomyBlueprints.SqmPerPerson[SocioeconomicClass];

        public Pop Split(int sizeToExtract)
        {
            if (sizeToExtract >= Size || sizeToExtract <= 0)
                throw new ArgumentException("Invalid split amount.");

            Size -= sizeToExtract;

            return new Pop
            {
                Name = this.Name,
                SocioeconomicClass = this.SocioeconomicClass,
                Field = this.Field,
                Size = sizeToExtract,
                IsEmployed = this.IsEmployed
            };
        }

        public override string ToString() => $"{Size} {SocioeconomicClass} {Field}s ({Name})";
    }
}