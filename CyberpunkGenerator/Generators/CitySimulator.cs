using CyberpunkGenerator.Data;
using CyberpunkGenerator.Economy;
using CyberpunkGenerator.Models;

namespace CyberpunkGenerator.Generators
{
    public class CitySimulator
    {
        private List<Pop> _allPops = new();
        private List<Business> _allBusinesses = new();

        public void GenerateOrganicCity()
        {
            // 1. SEED THE CITY with a Megacorp Core (Capitalist + Commercial/Science)
            AddPops(PopSocioeconomicClass.Capitalist, PopField.Commercial, 250);
            AddPops(PopSocioeconomicClass.Capitalist, PopField.Science, 250); // High-end R&D execs

            bool needsMet = false;
            int loopSafeguard = 0;

            // 2. THE EXPANSION LOOP
            while (!needsMet && loopSafeguard < 100)
            {
                loopSafeguard++;
                needsMet = true;

                // A. Goods Deficits
                var deficits = CalculateGoodsDeficits();
                foreach (var deficit in deficits)
                {
                    if (deficit.Value > 0)
                    {
                        needsMet = false;
                        string businessType = EconomyBlueprints.GetBusinessToFulfillNeed(deficit.Key);
                        var newBusiness = EconomyBlueprints.CreateBusiness(businessType);
                        _allBusinesses.Add(newBusiness);

                        Console.WriteLine($"[Built] {newBusiness.BusinessType} to fulfill {deficit.Key}");
                    }
                }

                // B. Labor Deficits (Using the new JobRole record)
                var laborDeficits = CalculateLaborDeficits();
                foreach (var laborNeed in laborDeficits)
                {
                    if (laborNeed.Value > 0)
                    {
                        needsMet = false;

                        // laborNeed.Key is our JobRole (e.g., WhiteCollar Commercial)
                        // laborNeed.Value is the number of people needed
                        AddPops(laborNeed.Key.Class, laborNeed.Key.Field, laborNeed.Value);

                        Console.WriteLine($"[Immigration] {laborNeed.Value} {laborNeed.Key.Class} {laborNeed.Key.Field} workers arrived.");
                    }
                }
            }

            Console.WriteLine($"\nCity generation stabilized after {loopSafeguard} passes.");
            Console.WriteLine($"Total Population: {_allPops.Sum(p => p.Size)}");
            Console.WriteLine($"Total Businesses: {_allBusinesses.Count}");
        }

        private void AddPops(PopSocioeconomicClass popClass, PopField field, int size)
        {
            _allPops.Add(new Pop
            {
                SocioeconomicClass = popClass,
                Field = field,
                Size = size,
                Name = $"{popClass} {field} Pop"
            });
        }

        private Dictionary<GoodType, float> CalculateGoodsDeficits()
        {
            var demand = new Dictionary<GoodType, float>();
            var supply = new Dictionary<GoodType, float>();

            foreach (var pop in _allPops)
            {
                // Look up needs based on SocioeconomicClass
                if (EconomyBlueprints.PopNeeds.TryGetValue(pop.SocioeconomicClass, out var needs))
                {
                    foreach (var need in needs)
                    {
                        if (!demand.ContainsKey(need.Key)) demand[need.Key] = 0;
                        demand[need.Key] += need.Value * (pop.Size / 100f);
                    }
                }
            }

            foreach (var biz in _allBusinesses)
            {
                foreach (var input in biz.InputGoods)
                {
                    if (!demand.ContainsKey(input.Key)) demand[input.Key] = 0;
                    demand[input.Key] += input.Value;
                }
                foreach (var output in biz.Outputs)
                {
                    if (!supply.ContainsKey(output.Key)) supply[output.Key] = 0;
                    supply[output.Key] += output.Value;
                }
            }

            var deficits = new Dictionary<GoodType, float>();
            foreach (GoodType good in Enum.GetValues(typeof(GoodType)))
            {
                float d = demand.ContainsKey(good) ? demand[good] : 0;
                float s = supply.ContainsKey(good) ? supply[good] : 0;
                if (d > s) deficits[good] = d - s;
            }
            return deficits;
        }

        private Dictionary<JobRole, int> CalculateLaborDeficits()
        {
            var demand = new Dictionary<JobRole, int>();
            var supply = new Dictionary<JobRole, int>();

            foreach (var biz in _allBusinesses)
            {
                foreach (var labor in biz.RequiredLabor)
                {
                    if (!demand.ContainsKey(labor.Key)) demand[labor.Key] = 0;
                    demand[labor.Key] += labor.Value;
                }
            }

            foreach (var pop in _allPops)
            {
                // We create a JobRole on the fly to check against the demand
                var popRole = new JobRole(pop.SocioeconomicClass, pop.Field);

                if (!supply.ContainsKey(popRole)) supply[popRole] = 0;
                supply[popRole] += pop.Size;
            }

            var deficits = new Dictionary<JobRole, int>();

            // Iterate over the keys in demand to find what's missing
            foreach (var role in demand.Keys)
            {
                int d = demand[role];
                int s = supply.ContainsKey(role) ? supply[role] : 0;
                if (d > s) deficits[role] = d - s;
            }

            return deficits;
        }
    }
}