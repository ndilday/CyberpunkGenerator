using CyberpunkGenerator.Data;

namespace CyberpunkGenerator.Models
{
    public class Business
    {
        public string Name { get; set; }
        public string BusinessType { get; set; }

        public Dictionary<MarketGood, float> Outputs { get; set; } = new();
        public Dictionary<MarketGood, float> InputGoods { get; set; } = new();
        public Dictionary<JobRole, int> RequiredLabor { get; set; } = new();

        public List<Pop> Employees { get; set; } = new();

        public override string ToString()
        {
            return $"{Name} ({BusinessType})";
        }
    }
}