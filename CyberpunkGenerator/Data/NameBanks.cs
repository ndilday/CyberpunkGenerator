
namespace CyberpunkGenerator.Data
{
    public static class NameBanks
    {
        public static readonly Random Rng = new Random();

        // ── City Names ───────────────────────────────────────────────────────
        public static readonly List<string> CityNames = new()
        {
            "Neo-Kyoto", "Terminus City", "Aethelburg", "Cinderfall", "Metropolis Prime",
            "Vantablack", "Shenzhen Omega", "New Carthage", "Port Leviathan", "Ashgate",
            "Voss Station", "Ironhaven", "Neon Babel", "Sector Null", "Crucible City",
            "Verdansk Ultra", "New Meridian", "Chromopolis", "The Sprawl", "Nexus Prime",
        };

        // ── Neighborhood Names ───────────────────────────────────────────────
        public static readonly List<string> NeighborhoodAdjectives = new()
        {
            // Tech / Cyber
            "Neon", "Chrome", "Holo", "Data", "Pixel", "Static", "Binary", "Neural",
            "Pulse", "Glitch", "Signal", "Cipher", "Vector", "Fractal",
            // Decay / Grit
            "Rusty", "Corroded", "Smog", "Ash", "Slick", "Gutter", "Cinder", "Crumbling",
            "Wrecked", "Salvage", "Tar", "Bleak", "Rotting", "Soot",
            // Corporate Gloss
            "Glimmering", "Apex", "Zenith", "Prism", "Gilded", "Lucent", "Sterling",
            "Radiant", "Polished", "Obsidian",
        };

        public static readonly List<string> NeighborhoodNouns = new()
        {
            // Urban geography
            "Sprawl", "District", "Warren", "Quarter", "Zone", "Sector", "Block",
            "Corridor", "Strip", "Flats", "Hollow", "Reach", "Basin", "Fringe",
            // Structures
            "Plaza", "Yard", "Market", "Arcade", "Terminal", "Hub", "Depot",
            "Spire", "Stack", "Column", "Lattice", "Rig",
        };

        // ── Gang Names ───────────────────────────────────────────────────────
        public static readonly List<string> GangNouns = new()
        {
            // Classic menace
            "Skulls", "Serpents", "Vultures", "Ghosts", "Jackals", "Ronin", "Reapers",
            "Blades", "Wraiths", "Shrikes", "Crows", "Vipers", "Wolves", "Mongrels",
            // Cyber-flavored
            "Glitches", "Proxies", "Nulls", "Voids", "Vectors", "Daemons", "Bytecrawlers",
            "Razors", "Synthetics", "Fragments", "Loops", "Executors",
        };

        public static readonly List<string> GangAdjectives = new()
        {
            // Material / Tech
            "Chrome", "Data", "Holo", "Static", "Rusted", "Fried", "Spliced",
            "Burned", "Shattered", "Cracked", "Severed", "Wired", "Welded",
            // Attitude
            "Voodoo", "Hollow", "Cursed", "Rabid", "Savage", "Silent", "Blind",
            "Lost", "Broken", "Feral", "Numb",
        };

        public static readonly List<string> GangSpecialties = new()
        {
            "Smuggling",
            "Netrunning",
            "Muscle for Hire",
            "Black Market Tech",
            "Drug Trafficking",
            "Ripperdoc Extortion",
            "Corporate Espionage",
            "Illegal Braindance",
            "Arms Running",
            "Identity Theft",
            "Cyberware Chopping",
            "Protection Rackets",
            "Organ Harvesting",
            "Data Brokering",
            "Vehicle Jacking",
        };

        // ── Business Flavor Names ────────────────────────────────────────────
        // Used to give generic businesses more evocative names beyond "Generic X".
        // Key: BusinessType string → list of possible name prefixes or full names.
        public static readonly Dictionary<string, List<string>> BusinessFlavorNames = new()
        {
            ["Ripperdoc Clinic"] = new() { "Stitcher's", "Doc Splice", "The Body Shop", "Hack & Heal", "Chrome & Sutures" },
            ["Dive Bar"] = new() { "The Rusted Nail", "Last Round", "Static & Rye", "The Leaky Valve", "Blackout Lounge", "Null Space Bar" },
            ["Food Market"] = new() { "Protein Row", "The Synth Mart", "Calorie Stack", "Block 9 Eats", "The Vat & Shelf" },
            ["Gourmet Market"] = new() { "Epicure Upload", "The Curated Plate", "Apex Provisions", "Zenith Pantry" },
            ["Clothing Store"] = new() { "Threadbare", "The Rack", "Worn Goods", "Budget Chrome" },
            ["Clothing Boutique"] = new() { "Apex Atelier", "Lucent Threads", "Prism Couture", "Gilded Seam" },
            ["Tech Bazaar"] = new() { "Scrap & Socket", "The Component", "Dead Drop Tech", "Fused Circuits" },
            ["SimReal Parlor"] = new() { "Deep Immersion", "Null Space Parlor", "The Experience", "Braindance Deluxe" },
            ["Auto Dealership"] = new() { "Apex Motors", "Chromeline Auto", "Velocity Showroom" },
            ["Corp-Med Clinic"] = new() { "MedSystem Pro", "Apex Health", "CorpCare Annex", "Vitagen Clinic" },
            ["Corp-Sec Precinct"] = new() { "Sector Control", "Aegis Station", "Vector Security Hub" },
            ["Chrome Boutique"] = new() { "Apex Chrome", "Lucent Implants", "The Enhancement Suite", "Prism Cybernetics" },
            ["Gene-Tailoring Clinic"] = new() { "Helix Studio", "Apex Genetics", "Sequence Spa", "The Bespoke Body" },
            ["Furnishings"] = new() { "Block Furnishings", "The Slab Store", "Utility Home" },
            ["Interior Design"] = new() { "Apex Interiors", "The Curated Space", "Prism Design" },
        };

        // ── Helpers ──────────────────────────────────────────────────────────
        public static T GetRandom<T>(this IList<T> list)
        {
            return list[Rng.Next(list.Count)];
        }

        /// <summary>
        /// Returns a flavor name for a given business type if one exists,
        /// otherwise falls back to "Generic {businessType}".
        /// </summary>
        public static string GetBusinessName(string businessType)
        {
            if (BusinessFlavorNames.TryGetValue(businessType, out var names))
                return names.GetRandom();
            return $"Generic {businessType}";
        }
    }
}