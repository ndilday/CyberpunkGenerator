using CyberpunkGenerator.Data;
using CyberpunkGenerator.Models;

namespace CyberpunkGenerator.Generators
{
    public class PopulationGenerator
    {
        private readonly Random _rng = NameBanks.Rng;

        public void GeneratePopsForNeighborhood(Neighborhood neighborhood)
        {
            int totalPopulation = neighborhood.Blocks.Count * _rng.Next(500, 2000); // More blocks = more people
            int popGroups = _rng.Next(2, 5); // 2 to 4 distinct population groups

            for (int i = 0; i < popGroups; i++)
            {
                var pop = new Pop
                {
                    Name = "Unnamed Population Group", // We'll improve this later
                    Size = totalPopulation / popGroups, // Simple even split for now
                };

                // Traits are influenced by the neighborhood
                if (neighborhood.TechLevel > 7 && neighborhood.Grit < 4)
                    pop.Traits.Add("Corporate Loyalist");
                if (neighborhood.Grit > 7)
                    pop.Traits.Add("Disenfranchised");
                if (neighborhood.TechLevel > 6)
                    pop.Traits.Add("Cyber-Augmented");

                // Add one more random trait
                pop.Traits.Add(NameBanks.PopTraits.GetRandom());

                neighborhood.Pops.Add(pop);
            }
        }
    }
}