using CyberpunkGenerator.Data; // Be sure to import your data
using CyberpunkGenerator.Models;

namespace CyberpunkGenerator.Generators
{
    public class CityGenerator
    {
        private readonly Random _rng = NameBanks.Rng;

        public City GenerateCity(int numNeighborhoods)
        {
            var city = new City { Name = NameBanks.CityNames.GetRandom() };

            for (int i = 0; i < numNeighborhoods; i++)
            {
                var neighborhood = new Neighborhood
                {
                    Name = $"{NameBanks.NeighborhoodAdjectives.GetRandom()} {NameBanks.NeighborhoodNouns.GetRandom()}",
                    // Let's create a relationship: high tech usually means less grit, and vice versa.
                    TechLevel = _rng.Next(1, 11),
                    Grit = _rng.Next(1, 11)
                };

                // Generate a random number of blocks for this neighborhood
                int numBlocks = _rng.Next(5, 21); // 5 to 20 blocks
                for (int j = 0; j < numBlocks; j++)
                {
                    neighborhood.Blocks.Add(new CityBlock(j + 1));
                }

                city.Neighborhoods.Add(neighborhood);
            }

            return city;
        }
    }
}