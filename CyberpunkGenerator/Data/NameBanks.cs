using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberpunkGenerator.Data
{
    public static class NameBanks
    {
        public static readonly Random Rng = new Random();

        public static readonly List<string> CityNames = new List<string> { "Neo-Kyoto", "Terminus City", "Aethelburg", "Cinderfall", "Metropolis Prime" };

        public static readonly List<string> NeighborhoodAdjectives = new List<string> { "Neon", "Chrome", "Rusty", "Glimmering", "Data", "Holo", "Sector" };
        public static readonly List<string> NeighborhoodNouns = new List<string> { "Sprawl", "District", "Plaza", "Warren", "Core", "Yard", "Market" };

        public static readonly List<string> GangNouns = new List<string> { "Skulls", "Serpents", "Vultures", "Ghosts", "Jokers", "Ronin" };
        public static readonly List<string> GangAdjectives = new List<string> { "Chrome", "Data", "Gutter", "Holo", "Voodoo" };
        public static readonly List<string> GangSpecialties = new List<string> { "Smuggling", "Netrunning", "Muscle for Hire", "Black Market Tech", "Drug Trafficking" };

        public static readonly List<string> BusinessTypes = new List<string> { "Ripperdoc", "Noodle Stand", "Arms Dealer", "Dive Bar", "Black Market", "Joy-Toy Booth", "Corporate Office" };
        // We can add more specific name lists for each business type later.

        public static readonly List<string> PopTraits = new List<string> { "Cyber-Augmented", "Bio-Purist", "Corporate Loyalist", "Anarchist", "Tech-Scavenger", "Disenfranchised" };

        // A helper method to get a random item from a list
        public static T GetRandom<T>(this IList<T> list)
        {
            return list[Rng.Next(list.Count)];
        }
    }
}
