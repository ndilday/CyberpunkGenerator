using CyberpunkGenerator.Data;
using CyberpunkGenerator.Models;

namespace CyberpunkGenerator.Generators
{
    public class BusinessGenerator
    {
        private readonly Random _rng = NameBanks.Rng;

        public void PopulateBlock(CityBlock block, Neighborhood neighborhood)
        {
            int numBusinesses = _rng.Next(1, 5); // 1 to 4 businesses per block

            for (int i = 0; i < numBusinesses; i++)
            {
                // Choose a business type based on neighborhood stats
                string businessType;
                if (neighborhood.Grit > 7 && _rng.Next(1, 3) == 1) // 50% chance in high-grit areas
                {
                    businessType = new List<string> { "Ripperdoc", "Black Market", "Dive Bar" }.GetRandom();
                }
                else if (neighborhood.TechLevel > 7 && neighborhood.Grit < 4)
                {
                    businessType = new List<string> { "Corporate Office", "High-end Cybernetics Clinic", "Luxury Goods" }.GetRandom();
                }
                else
                {
                    businessType = NameBanks.BusinessTypes.GetRandom();
                }

                var business = new Business
                {
                    BusinessType = businessType,
                    Name = $"The {NameBanks.NeighborhoodAdjectives.GetRandom()} {businessType}" // Placeholder name
                };

                block.Businesses.Add(business);
            }
        }
    }
}