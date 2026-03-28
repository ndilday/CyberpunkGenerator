
namespace CyberpunkGenerator.Models
{
    public class City
    {
        public string Name { get; set; }
        public List<Neighborhood> Neighborhoods { get; set; }
        public List<Gang> Gangs { get; set; }
        // We can add MegaCorps here later

        public City()
        {
            Name = "";
            Neighborhoods = new List<Neighborhood>();
            Gangs = new List<Gang>();
        }
    }
}
