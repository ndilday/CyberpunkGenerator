using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberpunkGenerator.Models
{
    public class CityBlock
    {
        public int Id { get; set; }
        public List<Business> Businesses { get; set; }

        public CityBlock(int id)
        {
            Id = id;
            Businesses = new List<Business>();
        }

        public override string ToString()
        {
            return $"Block {Id}";
        }
    }
}
