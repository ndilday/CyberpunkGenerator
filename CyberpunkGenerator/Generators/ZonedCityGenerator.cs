using CyberpunkGenerator.Data;
using CyberpunkGenerator.Economy;
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

            // ── Phase C: Group blocks → Neighborhoods ────────────────────────
            Console.WriteLine("\n=== Phase C: Neighborhood Formation ===");
            var neighborhoods = BuildNeighborhoods(cityMap);

            // ── Phase D: Assign gangs ────────────────────────────────────────
            Console.WriteLine("\n=== Phase D: Gang Assignment ===");
            var gangs = AssignGangs(neighborhoods);

            // ── Assemble City ────────────────────────────────────────────────
            var city = new City
            {
                Name = NameBanks.CityNames.GetRandom(),
                Neighborhoods = neighborhoods,
                Gangs = gangs
            };

            Console.WriteLine($"\nCity \"{city.Name}\" generated: " +
                              $"{neighborhoods.Count} neighborhoods, {gangs.Count} gangs.");
            return city;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Neighborhood formation
        //
        // Strategy: flood-fill contiguous regions of the same SocioeconomicLevel.
        // Each contiguous island becomes one Neighborhood. Unclassed industrial
        // blocks are merged into whichever adjacent neighborhood is largest.
        // ─────────────────────────────────────────────────────────────────────
        private static List<Neighborhood> BuildNeighborhoods(CityMap cityMap)
        {
            var allBlocks = cityMap.AllBlocks.ToList();
            var visited = new HashSet<int>(); // block IDs
            var neighborhoods = new List<Neighborhood>();

            // Group contiguous same-class MixedUse blocks via BFS
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

            // Attach orphaned industrial blocks to the nearest neighborhood
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
                    // Edge case: only industrial blocks exist (shouldn't happen in a seeded city)
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

            // Grit: inverse of affluence (Capitalist = 1, Destitute = 9)
            int grit = dominantClass switch
            {
                PopSocioeconomicClass.Capitalist => 1,
                PopSocioeconomicClass.WhiteCollar => 3,
                PopSocioeconomicClass.BlueCollar => 6,
                PopSocioeconomicClass.Destitute => 9,
                _ => 5
            };

            // TechLevel: mirrors affluence
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

            // Aggregate pops from all blocks
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
        //
        // Each gang spawns in a neighborhood whose Grit >= 5.
        // Power is proportional to neighborhood population density.
        // Gangs claim contiguous low-affluence territory.
        // ─────────────────────────────────────────────────────────────────────
        private static List<Gang> AssignGangs(List<Neighborhood> neighborhoods)
        {
            var gangs = new List<Gang>();

            var grittyCandidates = neighborhoods
                .Where(n => n.Grit >= 5)
                .OrderByDescending(n => n.Grit)
                .ToList();

            // Roughly one gang per 2-3 gritty neighborhoods, minimum 1
            int gangCount = Math.Max(1, grittyCandidates.Count / 2);

            for (int i = 0; i < gangCount && i < grittyCandidates.Count; i++)
            {
                var homeHood = grittyCandidates[i];

                int totalPop = homeHood.Blocks
                    .SelectMany(b => b.Pops)
                    .Sum(p => p.Size);

                // Power 1-10 scaled against an assumed max pop of 50,000
                int power = Math.Clamp(totalPop / 5000 + 1, 1, 10);

                string adj = NameBanks.GangAdjectives.GetRandom();
                string noun = NameBanks.GangNouns.GetRandom();
                string specialty = NameBanks.GangSpecialties.GetRandom();

                var gang = new Gang
                {
                    Name = $"The {adj} {noun}",
                    Specialty = specialty,
                    Power = power,
                    ControlledTerritory = new List<Neighborhood> { homeHood }
                };

                // Expand gang into adjacent gritty neighborhoods not already claimed
                var claimed = new HashSet<string> { homeHood.Name };
                foreach (var neighbor in grittyCandidates.Skip(i + 1).Take(2))
                {
                    if (!claimed.Contains(neighbor.Name) && neighbor.Grit >= 5)
                    {
                        gang.ControlledTerritory.Add(neighbor);
                        claimed.Add(neighbor.Name);
                    }
                }

                gangs.Add(gang);
                Console.WriteLine($"  Gang spawned: {gang}");
            }

            return gangs;
        }
    }
}