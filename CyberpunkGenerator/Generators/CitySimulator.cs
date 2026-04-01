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
        /// Runs the organic economic expansion loop, then seeds transportation
        /// businesses heuristically based on city scale, and returns the full
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

            // ── THE EXPANSION LOOP ───────────────────────────────────────────
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
                        float remainingDeficit = deficit.Value;
                        while (remainingDeficit > 0)
                        {
                            string businessType = EconomyBlueprints.GetBusinessToFulfillNeed(deficit.Key);
                            var newBusiness = EconomyBlueprints.CreateBusiness(businessType);
                            _allBusinesses.Add(newBusiness);
                            Console.WriteLine($"  [Built] {newBusiness.BusinessType} to fulfill {deficit.Key}");
                            remainingDeficit -= newBusiness.Outputs.GetValueOrDefault(deficit.Key, 0);
                        }
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

            // ── TRANSPORTATION SEEDING ───────────────────────────────────────
            // Now that the economy is stable, heuristically estimate how many
            // transportation businesses the city will need and add them to the
            // pool before ZoningEngine runs. This ensures transit depots and
            // distribution hubs are placed alongside the city they serve rather
            // than in a secondary pass on the outskirts.
            //
            // These are estimates — the ZoningEngine's contract/patronage link
            // formation will determine actual utilization. A small top-up pass
            // in ZonedCityGenerator handles any remaining unmet transport demand.
            SeedTransportationBusinesses();

            return (_allPops, _allBusinesses);
        }

        /// <summary>
        /// Heuristically estimates transportation business need based on city
        /// scale and seeds the business and labor pools accordingly.
        ///
        /// TransitDepots: one per TransitDepotPopsPerDepot total population,
        /// rounded up. Transit serves all classes but is sized against total pop
        /// since every class generates passenger mobility demand.
        ///
        /// DistributionHubs: one per DistributionHubsPerIndustrialBusiness
        /// industrial businesses, rounded up. Industrial businesses generate
        /// the bulk of freight demand through their input goods requirements.
        /// </summary>
        private void SeedTransportationBusinesses()
        {
            int totalPop = _allPops.Sum(p => p.Size);
            int industrialBizCount = _allBusinesses.Count(
                b => b.ZoneType == BusinessZoneType.Industrial);

            int transitDepotCount = (int)Math.Ceiling(
                (double)totalPop / EconomyConstants.TransitDepotPopsPerDepot);

            int distributionHubCount = (int)Math.Ceiling(
                (double)industrialBizCount / EconomyConstants.DistributionHubsPerIndustrialBusiness);

            Console.WriteLine($"\n=== Transportation Seeding ===");
            Console.WriteLine($"  Population: {totalPop:N0} → {transitDepotCount} Transit Depots");
            Console.WriteLine($"  Industrial businesses: {industrialBizCount} → {distributionHubCount} Distribution Hubs");

            for (int i = 0; i < transitDepotCount; i++)
            {
                var depot = EconomyBlueprints.CreateBusiness(BusinessTypes.TransitDepot);
                _allBusinesses.Add(depot);
            }

            for (int i = 0; i < distributionHubCount; i++)
            {
                var hub = EconomyBlueprints.CreateBusiness(BusinessTypes.DistributionHub);
                _allBusinesses.Add(hub);
            }

            // Transportation businesses require workers. Run a single targeted
            // labor pass to cover their RequiredLabor before handing off to
            // the ZoningEngine. We don't need a full goods deficit re-check
            // here — transportation businesses consume Automobiles and
            // Electricity, both of which are already produced in surplus by
            // this point in the simulation.
            var transportLaborDeficits = CalculateLaborDeficits();
            foreach (var laborNeed in transportLaborDeficits)
            {
                if (laborNeed.Value > 0)
                {
                    AddPops(laborNeed.Key.Class, laborNeed.Key.Field, laborNeed.Value);
                    Console.WriteLine($"  [Transport Labor] {laborNeed.Value} " +
                                      $"{laborNeed.Key.Class} {laborNeed.Key.Field} workers arrived.");
                }
            }

            Console.WriteLine($"  Transportation seeding complete. " +
                              $"Total businesses: {_allBusinesses.Count}, " +
                              $"Total population: {_allPops.Sum(p => p.Size):N0}");
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
                    if (d > s)
                    {
                        deficits[marketGood] = d - s;
                    }
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