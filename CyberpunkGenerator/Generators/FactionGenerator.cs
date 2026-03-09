using CyberpunkGenerator.Data;
using CyberpunkGenerator.Models;
using System.Linq;

namespace CyberpunkGenerator.Generators
{
    public class FactionGenerator
    {
        private readonly Random _rng = NameBanks.Rng;

        public void GenerateGangsAndTerritory(City city)
        {
            int numGangs = _rng.Next(2, 5);
            for (int i = 0; i < numGangs; i++)
            {
                var gang = new Gang
                {
                    Name = $"The {NameBanks.GangAdjectives.GetRandom()} {NameBanks.GangNouns.GetRandom()}",
                    Specialty = NameBanks.GangSpecialties.GetRandom(),
                    Power = _rng.Next(3, 9)
                };
                city.Gangs.Add(gang);
            }

            // Assign territory based on Grit
            // Get all neighborhoods not controlled by a gang, sorted by grittiest first
            var uncontrolledNeighborhoods = city.Neighborhoods
                .Where(n => !city.Gangs.Any(g => g.ControlledTerritory.Contains(n)))
                .OrderByDescending(n => n.Grit)
                .ToList();

            foreach (var gang in city.Gangs.OrderByDescending(g => g.Power))
            {
                // Powerful gangs get more/better territory
                int territoriesToClaim = gang.Power / 3; // Simple logic: 1 territory per 3 power

                for (int i = 0; i < territoriesToClaim && uncontrolledNeighborhoods.Any(); i++)
                {
                    var territory = uncontrolledNeighborhoods.First();
                    gang.ControlledTerritory.Add(territory);
                    uncontrolledNeighborhoods.Remove(territory);
                }
            }
        }
    }
}