using CyberpunkGenerator.Data;

namespace CyberpunkGenerator.Models
{
    public class Business
    {
        public string Name { get; set; }
        public string BusinessType { get; set; }

        public Dictionary<GoodType, float> Outputs { get; set; } = new();
        public Dictionary<GoodType, float> InputGoods { get; set; } = new();

        // Updated to use our new JobRole record
        public Dictionary<JobRole, int> RequiredLabor { get; set; } = new();

        public List<Pop> Employees { get; set; } = new();

        public override string ToString()
        {
            return $"{Name} ({BusinessType})";
        }
    }
}