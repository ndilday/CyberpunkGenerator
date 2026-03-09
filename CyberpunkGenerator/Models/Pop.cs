using CyberpunkGenerator.Data;

namespace CyberpunkGenerator.Models
{
    public class Pop
    {
        public string Name { get; set; }
        public int Size { get; set; }

        // The two new axes defining the pop
        public PopSocioeconomicClass SocioeconomicClass { get; set; }
        public PopField Field { get; set; }

        public bool IsEmployed { get; set; }
        public List<string> Traits { get; set; } // e.g., "Cyber-Augmented", "Bio-Purist", "Anarchist"

        public override string ToString()
        {
            return $"{Size} {SocioeconomicClass} {Field}s ({Name})";
        }
    }
}
