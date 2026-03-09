using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberpunkGenerator.Models
{
    public class Gang
    {
        public string Name { get; set; } // e.g., "The Chrome Skulls", "Voodoo Bytes"
        public string Specialty { get; set; } // "Smuggling", "Netrunning", "Muscle"
        public int Power { get; set; } // 1-10 scale
        public List<Neighborhood> ControlledTerritory { get; set; }

        public Gang()
        {
            ControlledTerritory = new List<Neighborhood>();
        }

        public override string ToString()
        {
            return $"{Name} ({Specialty}, Power: {Power})";
        }
    }
}
