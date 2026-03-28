using CyberpunkGenerator.Generators;
using CyberpunkGenerator.Models;

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var generator = new ZonedCityGenerator();
        var city = generator.Generate();

        Console.WriteLine();
        PrintCityDetails(city);
    }

    public static void PrintCityDetails(City city)
    {
        var totalPop = city.Neighborhoods.SelectMany(n => n.Pops).Sum(p => p.Size);
        var totalBlocks = city.Neighborhoods.Sum(n => n.Blocks.Count);
        var totalBiz = city.Neighborhoods
                               .SelectMany(n => n.Blocks)
                               .SelectMany(b => b.Businesses)
                               .Count();

        Console.WriteLine(new string('═', 60));
        Console.WriteLine($"  {city.Name.ToUpper()}");
        Console.WriteLine($"  {city.Neighborhoods.Count} neighborhoods  •  " +
                          $"{totalBlocks} blocks  •  " +
                          $"{totalPop:N0} citizens  •  " +
                          $"{totalBiz} businesses  •  " +
                          $"{city.Gangs.Count} gangs");
        Console.WriteLine(new string('═', 60));

        // ── Gangs ────────────────────────────────────────────────────────────
        Console.WriteLine("\n┌─ GANGS & TERRITORY ──────────────────────────────────┐");
        foreach (var gang in city.Gangs.OrderByDescending(g => g.Power))
        {
            Console.WriteLine($"│  {gang.Name,-28} │ {gang.Specialty,-22} │ Power {gang.Power,2}/10");
            if (gang.ControlledTerritory.Any())
            {
                var territory = string.Join(", ", gang.ControlledTerritory.Select(n => n.Name));
                Console.WriteLine($"│    Territory: {territory}");
            }
        }
        Console.WriteLine("└──────────────────────────────────────────────────────┘");

        // ── Neighborhoods ────────────────────────────────────────────────────
        Console.WriteLine("\n┌─ NEIGHBORHOODS ──────────────────────────────────────┐");
        foreach (var hood in city.Neighborhoods.OrderBy(n => n.Grit))
        {
            int hoodPop = hood.Pops.Sum(p => p.Size);
            int hoodBiz = hood.Blocks.SelectMany(b => b.Businesses).Count();

            Console.WriteLine($"│");
            Console.WriteLine($"│  ▸ {hood.Name}");
            Console.WriteLine($"│    Grit {hood.Grit}/10  │  Tech {hood.TechLevel}/10  │  " +
                              $"{hood.Blocks.Count} blocks  │  {hoodPop:N0} people  │  {hoodBiz} businesses");

            // Population breakdown
            if (hood.Pops.Any())
            {
                var grouped = hood.Pops
                    .GroupBy(p => p.SocioeconomicClass)
                    .OrderByDescending(g => (int)g.Key);

                Console.WriteLine($"│    Population:");
                foreach (var classGroup in grouped)
                {
                    int classTotal = classGroup.Sum(p => p.Size);
                    var fields = classGroup
                        .GroupBy(p => p.Field)
                        .Select(fg => $"{fg.Sum(p => p.Size):N0} {fg.Key}")
                        .ToList();
                    Console.WriteLine($"│      {classGroup.Key,-14} {classTotal,8:N0}  ({string.Join(", ", fields)})");
                }
            }

            // Businesses — show all unique types in this neighborhood
            var bizTypes = hood.Blocks
                .SelectMany(b => b.Businesses)
                .GroupBy(b => b.BusinessType)
                .OrderBy(g => g.Key)
                .ToList();

            if (bizTypes.Any())
            {
                Console.WriteLine($"│    Businesses:");
                foreach (var bizGroup in bizTypes)
                {
                    string count = bizGroup.Count() > 1 ? $" ×{bizGroup.Count()}" : "";
                    Console.WriteLine($"│      - {bizGroup.Key}{count}");
                }
            }

            // Gang presence
            var presenceGangs = city.Gangs
                .Where(g => g.ControlledTerritory.Any(t => t.Name == hood.Name))
                .ToList();
            if (presenceGangs.Any())
            {
                Console.WriteLine($"│    ⚠  Gang presence: {string.Join(", ", presenceGangs.Select(g => g.Name))}");
            }
        }
        Console.WriteLine("│");
        Console.WriteLine("└──────────────────────────────────────────────────────┘");
        Console.WriteLine();
    }
}