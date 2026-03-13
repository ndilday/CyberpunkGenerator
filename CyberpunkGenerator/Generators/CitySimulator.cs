using CyberpunkGenerator.Data;
using CyberpunkGenerator.Economy;
using CyberpunkGenerator.Models;

namespace CyberpunkGenerator.Generators
{
    public class CitySimulator
    {
        private List<Pop> _allPops = new();
        private List<Business> _allBusinesses = new();

        /// <summary>
        /// Runs the organic economic expansion loop and returns the full
        /// population and business lists for the ZoningEngine to consume.
        /// </summary>
        public (List<Pop> Pops, List<Business> Businesses) GenerateOrganicCity()
        {
            // Seed with a Mega-Corp Headquarters. Adding it to _allBusinesses before
            // the expansion loop means its RequiredLabor (Capitalists, WhiteCollar)
            // drives the first immigration wave organically. ZoningEngine will pull
            // this exact instance out of the returned list and place it at (0,0).
            _allBusinesses.Add(EconomyBlueprints.CreateBusiness(BusinessTypes.MegaCorpHeadquarters));

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

                        Console.WriteLine($"  [Built] {newBusiness.BusinessType} to fulfill {deficit.Key}");
                    }
                }

                // B. Labor Deficits
                var laborDeficits = CalculateLaborDeficits();
                foreach (var laborNeed in laborDeficits)
                {
                    if (laborNeed.Value > 0)
                    {
                        needsMet = false;
                        AddPops(laborNeed.Key.Class, laborNeed.Key.Field, laborNeed.Value);

                        Console.WriteLine($"  [Immigration] {laborNeed.Value} " +
                                          $"{laborNeed.Key.Class} {laborNeed.Key.Field} workers arrived.");
                    }
                }
            }

            Console.WriteLine($"\nEconomy stabilized after {loopSafeguard} passes.");
            Console.WriteLine($"Total Population: {_allPops.Sum(p => p.Size)}");
            Console.WriteLine($"Total Businesses: {_allBusinesses.Count}");

            return (_allPops, _allBusinesses);
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

        private Dictionary<MarketGood, float> CalculateGoodsDeficits()
        {
            var demand = new Dictionary<MarketGood, float>();
            var supply = new Dictionary<MarketGood, float>();

            foreach (var pop in _allPops)
            {
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

            var deficits = new Dictionary<MarketGood, float>();

            foreach (GoodType goodType in Enum.GetValues(typeof(GoodType)))
            {
                foreach (GoodState state in Enum.GetValues(typeof(GoodState)))
                {
                    var marketGood = new MarketGood(goodType, state);
                    float d = demand.GetValueOrDefault(marketGood);
                    float s = supply.GetValueOrDefault(marketGood);
                    if (d > s) deficits[marketGood] = d - s;
                }
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
                var popRole = new JobRole(pop.SocioeconomicClass, pop.Field);
                if (!supply.ContainsKey(popRole)) supply[popRole] = 0;
                supply[popRole] += pop.Size;
            }

            var deficits = new Dictionary<JobRole, int>();

            foreach (var role in demand.Keys)
            {
                int d = demand[role];
                int s = supply.GetValueOrDefault(role);
                if (d > s) deficits[role] = d - s;
            }

            return deficits;
        }
    }
}