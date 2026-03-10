using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace CyberpunkGenerator.Models
{
    public class Neighborhood
    {
        public string Name { get; set; } // e.g., "The Neon Slums", "Aethelred Corporate Plaza"
        public int Grit { get; set; } // 1-10 scale of danger and decay
        public int TechLevel { get; set; } // 1-10 scale of available technology
        public List<CityBlock> Blocks { get; set; }
        public List<Pop> Pops { get; set; }

        public Neighborhood()
        {
            Name = "";
            Blocks = new List<CityBlock>();
            Pops = new List<Pop>();
        }

        public override string ToString()
        {
            return $"{Name} (Grit: {Grit}, Tech: {TechLevel})";
        }
    }
}
