using CyberpunkGenerator.Data;
using CyberpunkGenerator.Economy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkGenerator.Models
{
    public enum BlockType { Industrial, MixedUse }

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
        public const int MaxIndustrialBusinesses = 1;

        public CityBlock(int id, BlockType type, int x, int y)
        {
            Id = id;
            Type = type;
            X = x;
            Y = y;
        }

        public bool CanFitBusiness(Business b)
        {
            if (Type == BlockType.Industrial)
                return b.ZoneType == BusinessZoneType.Industrial && Businesses.Count < MaxIndustrialBusinesses;

            if (Type == BlockType.MixedUse)
            {
                if (b.ZoneType != BusinessZoneType.Commercial || Businesses.Count >= MaxCommercialBusinesses)
                    return false;

                if (SocioeconomicLevel.HasValue && b.TargetClass.HasValue && SocioeconomicLevel.Value != b.TargetClass.Value)
                    return false;

                return true;
            }

            return false;
        }

        public bool TryAddBusiness(Business b)
        {
            if (!CanFitBusiness(b)) return false;

            if (Type == BlockType.MixedUse && !SocioeconomicLevel.HasValue && b.TargetClass.HasValue)
                SocioeconomicLevel = b.TargetClass.Value;

            Businesses.Add(b);
            return true;
        }

        public int GetUsedResidentialSqm() => Pops.Sum(p => p.RequiredSqm);

        public int GetRemainingResidentialSqm()
        {
            if (Type == BlockType.Industrial) return 0;
            return UsableResidentialSqm - GetUsedResidentialSqm();
        }

        public int CalculateCapacityForClass(PopSocioeconomicClass popClass)
        {
            if (Type == BlockType.Industrial) return 0;
            if (SocioeconomicLevel.HasValue && SocioeconomicLevel.Value != popClass) return 0;

            int remainingSqm = GetRemainingResidentialSqm();
            if (remainingSqm <= 0) return 0;

            // Pull the required sqm directly from the blueprints
            int sqmPerPerson = EconomyBlueprints.SqmPerPerson[popClass];

            return remainingSqm / sqmPerPerson;
        }

        public void AddPop(Pop pop)
        {
            if (Type == BlockType.Industrial)
                throw new InvalidOperationException("Cannot add pops to an industrial block.");

            if (SocioeconomicLevel.HasValue && SocioeconomicLevel.Value != pop.SocioeconomicClass)
                throw new InvalidOperationException($"Block is locked to {SocioeconomicLevel.Value}, cannot add {pop.SocioeconomicClass}.");

            if (pop.RequiredSqm > GetRemainingResidentialSqm())
                throw new InvalidOperationException("Pop exceeds block residential capacity. Split it first.");

            if (!SocioeconomicLevel.HasValue)
                SocioeconomicLevel = pop.SocioeconomicClass;

            Pops.Add(pop);
        }

        public override string ToString()
        {
            string bizCount = Businesses.Count.ToString();
            string maxBiz = Type == BlockType.MixedUse ? MaxCommercialBusinesses.ToString() : MaxIndustrialBusinesses.ToString();
            string classLabel = SocioeconomicLevel.HasValue ? $"[{SocioeconomicLevel.Value}]" : "[Empty]";
            int popCount = Pops.Sum(p => p.Size);

            return $"Block {Id} at ({X},{Y}) ({Type}) {classLabel} | Businesses: {bizCount}/{maxBiz} | Pops: {popCount}";
        }
    }
}