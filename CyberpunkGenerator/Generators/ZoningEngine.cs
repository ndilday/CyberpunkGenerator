using CyberpunkGenerator.Models;
using CyberpunkGenerator.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkGenerator.Generators
{
    public class ZoningEngine
    {
        private readonly CityMap _map;
        private readonly List<IZoneable> _pendingEntities;

        public ZoningEngine(List<Pop> allPops, List<Business> allBusinesses)
        {
            _map = new CityMap();
            _pendingEntities = new List<IZoneable>();

            _pendingEntities.AddRange(allPops);
            _pendingEntities.AddRange(allBusinesses);
        }

        public CityMap GenerateMap()
        {
            // 1. Place the Seed Block at (0,0)
            // The first entity in the sorted list will dictate the lock-type of the seed block
            SortPendingEntities();

            Console.WriteLine($"Starting Zoning Engine. {_pendingEntities.Count} entities to place.");

            // 2. The Placement Loop (Phase C & D will go inside here)
            while (_pendingEntities.Any())
            {
                // Pop the top priority item
                var entityToPlace = _pendingEntities[0];
                _pendingEntities.RemoveAt(0);

                // TODO: Calculate "Gravity" (Phase C)
                // TODO: Attempt Placement or Displacement (Phase C & D)

                // (Temporary placeholder to prevent infinite loops if tested right now)
                // Console.WriteLine($"Placed: {entityToPlace}"); 
            }

            return _map;
        }

        // The gentrification sorting logic
        private void SortPendingEntities()
        {
            _pendingEntities.Sort(new GentrificationComparer());
        }

        // Custom comparer to enforce your sorting rules
        private class GentrificationComparer : IComparer<IZoneable>
        {
            public int Compare(IZoneable x, IZoneable y)
            {
                // 1. Primary Sort: Affluence (Descending)
                int classX = GetAffluenceScore(x.TargetClass);
                int classY = GetAffluenceScore(y.TargetClass);
                if (classX != classY)
                    return classY.CompareTo(classX); // Highest score first

                // 2. Secondary Sort: Type (Residential > Commercial > Industrial)
                if (x.PlacementType != y.PlacementType)
                    return y.PlacementType.CompareTo(x.PlacementType); // Enum is ordered 1, 2, 3. Highest first.

                // 3. Tertiary Sort: Random Seed
                return x.PlacementSeed.CompareTo(y.PlacementSeed);
            }

            private int GetAffluenceScore(PopSocioeconomicClass? targetClass)
            {
                // Null represents Industrial/Classless entities, which go to the very bottom
                if (!targetClass.HasValue) return 0;

                return targetClass.Value switch
                {
                    PopSocioeconomicClass.Capitalist => 4,
                    PopSocioeconomicClass.WhiteCollar => 3,
                    PopSocioeconomicClass.BlueCollar => 2,
                    PopSocioeconomicClass.Destitute => 1,
                    _ => 0
                };
            }
        }
    }
}