using CyberpunkGenerator.Generators;
using CyberpunkGenerator.Models;

public class Program
{
    public static void Main(string[] args)
    {
        // 1. Initialize the generators
        var citySim = new CitySimulator();
        var cityGen = new CityGenerator();
        var popGen = new PopulationGenerator();
        var bizGen = new BusinessGenerator();
        var factionGen = new FactionGenerator();

        /*// 2. Generate the base city and its structure
        Console.WriteLine("Generating city...");
        City myCity = cityGen.GenerateCity(numNeighborhoods: 5);
        Console.WriteLine($"Welcome to {myCity.Name}!");
        Console.WriteLine("---------------------------------\n");

        // 3. Run the other generators to populate the city
        // Note: We pass the city object around so generators can add to it.
        foreach (var neighborhood in myCity.Neighborhoods)
        {
            // Generate pops for this neighborhood
            popGen.GeneratePopsForNeighborhood(neighborhood);

            // Generate businesses for each block in this neighborhood
            foreach (var block in neighborhood.Blocks)
            {
                bizGen.PopulateBlock(block, neighborhood);
            }
        }

        // 4. Generate factions and assign territory
        factionGen.GenerateGangsAndTerritory(myCity);


        // 5. Print the results
        PrintCityDetails(myCity);*/
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