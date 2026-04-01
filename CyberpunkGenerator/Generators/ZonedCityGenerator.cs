using CyberpunkGenerator.Data;
using CyberpunkGenerator.Models;

namespace CyberpunkGenerator.Generators
{
    /// <summary>
    /// Top-level orchestrator. Runs the economic simulator, feeds the results
    /// into the ZoningEngine, then groups the placed blocks into named
    /// Neighborhoods to produce a finished City.
    /// </summary>
    public class ZonedCityGenerator
    {
        private static readonly Random _rng = new(1);

        public City Generate()
        {
            // ── Phase A: Economic simulation ─────────────────────────────────
            Console.WriteLine("=== Phase A: Economic Simulation ===");
            var simulator = new CitySimulator();
            var (allPops, allBusinesses) = simulator.GenerateOrganicCity();

            // ── Phase B: Spatial zoning ──────────────────────────────────────
            Console.WriteLine("\n=== Phase B: Spatial Zoning ===");
            var zoningEngine = new ZoningEngine(allPops, allBusinesses);
            var cityMap = zoningEngine.GenerateMap();

            // ── Phase B.5: Transportation point calculation ──────────────────
            // Contract and Patronage links were formed during Phase B as each
            // entity was placed. Now that all entities are on the map, sum up
            // the transportation points across all links.
            var (popTP, freightTP) = zoningEngine.CalculateTransportationPoints();
            Console.WriteLine($"\n=== Transportation Points ===");
            Console.WriteLine($"  Pop transport:     {popTP:N0} TP");
            Console.WriteLine($"  Freight transport: {freightTP:N0} TP");
            Console.WriteLine($"  Total:             {popTP + freightTP:N0} TP");

            // ── Phase C: Group blocks → Neighborhoods ────────────────────────
            Console.WriteLine("\n=== Phase C: Neighborhood Formation ===");
            var neighborhoods = BuildNeighborhoods(cityMap);

            // ── Phase D: Assign gangs ────────────────────────────────────────
            Console.WriteLine("\n=== Phase D: Gang Assignment ===");
            var gangs = AssignGangs(neighborhoods, cityMap);

            // ── Assemble City ────────────────────────────────────────────────
            var city = new City
            {
                Name = NameBanks.CityNames.GetRandom(),
                Neighborhoods = neighborhoods,
                Gangs = gangs,
                PopTransportationPoints = popTP,
                FreightTransportationPoints = freightTP,
            };

            Console.WriteLine($"\nCity \"{city.Name}\" generated: " +
                              $"{neighborhoods.Count} neighborhoods, {gangs.Count} gangs.");
            return city;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Neighborhood formation
        // ─────────────────────────────────────────────────────────────────────

        private static List<Neighborhood> BuildNeighborhoods(CityMap cityMap)
        {
            var allBlocks = cityMap.AllBlocks.ToList();
            var visited = new HashSet<int>();
            var neighborhoods = new List<Neighborhood>();

            foreach (var block in allBlocks)
            {
                if (visited.Contains(block.Id)) continue;
                if (block.Type == BlockType.Industrial) continue;

                var island = new List<CityBlock>();
                var queue = new Queue<CityBlock>();
                queue.Enqueue(block);
                visited.Add(block.Id);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    island.Add(current);

                    foreach (var neighbor in GetCardinalNeighbors(cityMap, current))
                    {
                        if (visited.Contains(neighbor.Id)) continue;
                        if (neighbor.Type == BlockType.Industrial) continue;
                        if (neighbor.SocioeconomicLevel != current.SocioeconomicLevel) continue;

                        visited.Add(neighbor.Id);
                        queue.Enqueue(neighbor);
                    }
                }

                var hood = CreateNeighborhood(island);
                neighborhoods.Add(hood);
            }

            foreach (var block in allBlocks.Where(b => b.Type == BlockType.Industrial && !visited.Contains(b.Id)))
            {
                visited.Add(block.Id);
                var nearest = neighborhoods
                    .OrderBy(n => n.Blocks
                        .Min(b => cityMap.GetDistance(b.X, b.Y, block.X, block.Y)))
                    .FirstOrDefault();

                if (nearest != null)
                    nearest.Blocks.Add(block);
                else
                {
                    var solo = new Neighborhood
                    {
                        Name = "Industrial Wasteland",
                        Grit = 8,
                        TechLevel = 4,
                        Blocks = new List<CityBlock> { block }
                    };
                    neighborhoods.Add(solo);
                }
            }

            Console.WriteLine($"  Formed {neighborhoods.Count} neighborhoods.");
            return neighborhoods;
        }

        private static Neighborhood CreateNeighborhood(List<CityBlock> blocks)
        {
            var dominantClass = blocks
                .Where(b => b.SocioeconomicLevel.HasValue)
                .GroupBy(b => b.SocioeconomicLevel!.Value)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            int grit = dominantClass switch
            {
                PopSocioeconomicClass.Capitalist => 1,
                PopSocioeconomicClass.WhiteCollar => 3,
                PopSocioeconomicClass.BlueCollar => 6,
                PopSocioeconomicClass.Destitute => 9,
                _ => 5
            };

            int tech = dominantClass switch
            {
                PopSocioeconomicClass.Capitalist => 10,
                PopSocioeconomicClass.WhiteCollar => 7,
                PopSocioeconomicClass.BlueCollar => 4,
                PopSocioeconomicClass.Destitute => 2,
                _ => 3
            };

            string adj = NameBanks.NeighborhoodAdjectives.GetRandom();
            string noun = NameBanks.NeighborhoodNouns.GetRandom();

            var hood = new Neighborhood
            {
                Name = $"{adj} {noun}",
                Grit = grit,
                TechLevel = tech,
                Blocks = blocks
            };

            foreach (var pop in blocks.SelectMany(b => b.Pops))
                hood.Pops.Add(pop);

            return hood;
        }

        private static IEnumerable<CityBlock> GetCardinalNeighbors(CityMap map, CityBlock block)
        {
            var offsets = new (int dx, int dy)[] { (0, 1), (1, 0), (0, -1), (-1, 0) };
            foreach (var (dx, dy) in offsets)
            {
                var neighbor = map.GetBlockAt(block.X + dx, block.Y + dy);
                if (neighbor != null) yield return neighbor;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Gang assignment
        // ─────────────────────────────────────────────────────────────────────

        private static List<Gang> AssignGangs(List<Neighborhood> neighborhoods, CityMap cityMap)
        {
            var eligible = neighborhoods
                .Where(n => n.Grit >= 5)
                .ToList();

            if (eligible.Count == 0)
            {
                Console.WriteLine("  No gritty neighborhoods found; no gangs spawned.");
                return new List<Gang>();
            }

            var neighborhoodAdjacency = BuildNeighborhoodAdjacency(neighborhoods, cityMap);

            int gangCount = Math.Max(1, eligible.Count / 2);

            int Score(Neighborhood n)
            {
                int pop = n.Blocks.SelectMany(b => b.Pops).Sum(p => p.Size);
                return n.Grit * 3 + pop / 1_000;
            }

            var claimed = new HashSet<string>();
            var gangList = new List<Gang>();
            var gangTerritoryMap = new Dictionary<Gang, HashSet<string>>();

            var candidatesByScore = eligible
                .OrderByDescending(Score)
                .ToList();

            for (int i = 0; i < gangCount; i++)
            {
                var hq = candidatesByScore.FirstOrDefault(n => !claimed.Contains(n.Name));
                if (hq == null) break;

                claimed.Add(hq.Name);

                int pop = hq.Blocks.SelectMany(b => b.Pops).Sum(p => p.Size);
                int power = Math.Clamp(pop / 5_000 + hq.Grit / 2, 1, 10);

                string adj = NameBanks.GangAdjectives.GetRandom();
                string noun = NameBanks.GangNouns.GetRandom();
                string specialty = NameBanks.GangSpecialties.GetRandom();

                var gang = new Gang
                {
                    Name = $"The {adj} {noun}",
                    Specialty = specialty,
                    Power = power,
                    ControlledTerritory = new List<Neighborhood> { hq }
                };

                gangList.Add(gang);
                gangTerritoryMap[gang] = new HashSet<string> { hq.Name };

                Console.WriteLine($"  Gang spawned: {gang} — HQ: {hq.Name}");
            }

            bool expanded = true;
            int expansionRound = 0;
            int maxRounds = neighborhoods.Count;

            while (expanded && expansionRound < maxRounds)
            {
                expanded = false;
                expansionRound++;

                foreach (var gang in gangList.OrderByDescending(g => g.Power))
                {
                    var ownedNames = gangTerritoryMap[gang];

                    Neighborhood? best = null;
                    int bestScore = -1;

                    foreach (var ownedName in ownedNames)
                    {
                        if (!neighborhoodAdjacency.TryGetValue(ownedName, out var adjacentHoods))
                            continue;

                        foreach (var candidate in adjacentHoods)
                        {
                            if (claimed.Contains(candidate.Name)) continue;
                            if (candidate.Grit < 5) continue;

                            int s = Score(candidate);
                            if (s > bestScore)
                            {
                                bestScore = s;
                                best = candidate;
                            }
                        }
                    }

                    if (best != null)
                    {
                        claimed.Add(best.Name);
                        gang.ControlledTerritory.Add(best);
                        ownedNames.Add(best.Name);
                        expanded = true;

                        Console.WriteLine($"  [Expand] {gang.Name} claims {best.Name}");
                    }
                }
            }

            Console.WriteLine("\n  Rivalry check:");
            bool anyRivalry = false;

            for (int a = 0; a < gangList.Count; a++)
            {
                for (int b = a + 1; b < gangList.Count; b++)
                {
                    var gangA = gangList[a];
                    var gangB = gangList[b];

                    bool areRivals = gangA.ControlledTerritory.Any(na =>
                    {
                        if (!neighborhoodAdjacency.TryGetValue(na.Name, out var adj)) return false;
                        return adj.Any(nb => gangB.ControlledTerritory.Any(t => t.Name == nb.Name));
                    });

                    if (areRivals)
                    {
                        bool hotBorder = gangA.ControlledTerritory
                            .Where(na => neighborhoodAdjacency.TryGetValue(na.Name, out var adj2)
                                         && adj2.Any(nb => gangB.ControlledTerritory.Any(t => t.Name == nb.Name)))
                            .Any(na => na.Grit >= 7)
                            ||
                            gangB.ControlledTerritory
                            .Where(nb => neighborhoodAdjacency.TryGetValue(nb.Name, out var adj3)
                                         && adj3.Any(na => gangA.ControlledTerritory.Any(t => t.Name == na.Name)))
                            .Any(nb => nb.Grit >= 7);

                        string intensity = hotBorder ? "HOT RIVALRY" : "Cold rivalry";
                        Console.WriteLine($"    {intensity}: {gangA.Name} ↔ {gangB.Name}");
                        anyRivalry = true;
                    }
                }
            }

            if (!anyRivalry)
                Console.WriteLine("    No rival borders detected.");

            return gangList;
        }

        private static Dictionary<string, List<Neighborhood>> BuildNeighborhoodAdjacency(
            List<Neighborhood> neighborhoods,
            CityMap cityMap)
        {
            var coordToHood = new Dictionary<(int x, int y), Neighborhood>();
            foreach (var hood in neighborhoods)
                foreach (var block in hood.Blocks)
                    coordToHood[(block.X, block.Y)] = hood;

            var adjacency = new Dictionary<string, HashSet<string>>();
            foreach (var hood in neighborhoods)
                adjacency[hood.Name] = new HashSet<string>();

            var cardinalOffsets = new (int dx, int dy)[] { (0, 1), (1, 0), (0, -1), (-1, 0) };

            foreach (var hood in neighborhoods)
            {
                foreach (var block in hood.Blocks)
                {
                    foreach (var (dx, dy) in cardinalOffsets)
                    {
                        var neighborCoord = (block.X + dx, block.Y + dy);
                        if (!coordToHood.TryGetValue(neighborCoord, out var neighborHood)) continue;
                        if (neighborHood.Name == hood.Name) continue;

                        adjacency[hood.Name].Add(neighborHood.Name);
                        adjacency[neighborHood.Name].Add(hood.Name);
                    }
                }
            }

            var result = new Dictionary<string, List<Neighborhood>>();
            var hoodByName = neighborhoods.ToDictionary(n => n.Name);

            foreach (var (name, neighborNames) in adjacency)
                result[name] = neighborNames.Select(n => hoodByName[n]).ToList();

            return result;
        }
    }
}