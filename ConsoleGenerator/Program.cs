using CyberpunkGenerator.Generators;
using CyberpunkGenerator.Models;

public class Program
{
    public static void Main(string[] args)
    {
        // 1. Initialize the generators
        var citySim = new CitySimulator();

        citySim.GenerateOrganicCity();
    }

    public static void PrintCityDetails(City city)
    {
        Console.WriteLine($"========= Details for {city.Name} =========\n");

        Console.WriteLine("--- GANGS AND TERRITORY ---");
        foreach (var gang in city.Gangs)
        {
            Console.WriteLine($"  {gang}");
            if (gang.ControlledTerritory.Any())
            {
                Console.WriteLine($"    Controls: {string.Join(", ", gang.ControlledTerritory.Select(n => n.Name))}");
            }
        }
        Console.WriteLine("\n--- NEIGHBORHOODS ---");
        foreach (var neighborhood in city.Neighborhoods)
        {
            Console.WriteLine($"> {neighborhood}");

            Console.WriteLine("  Pops:");
            foreach (var pop in neighborhood.Pops)
            {
                Console.WriteLine($"    - {pop}");
            }

            Console.WriteLine("  Sample Businesses (from first block):");
            var firstBlock = neighborhood.Blocks.FirstOrDefault();
            if (firstBlock != null)
            {
                foreach (var biz in firstBlock.Businesses)
                {
                    Console.WriteLine($"    - {biz}");
                }
            }
            Console.WriteLine(); // Add a line break for readability
        }
    }
}