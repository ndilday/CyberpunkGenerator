using CyberpunkGenerator.Models;
using CyberpunkGenerator.Data;

namespace CyberpunkGenerator.Economy
{
    public static class EconomyBlueprints
    {
        // Helper methods to keep the dictionary definitions clean
        private static MarketGood Retail(GoodType type) => new MarketGood(type, GoodState.Retail);
        private static MarketGood Wholesale(GoodType type) => new MarketGood(type, GoodState.Wholesale);

        public static Dictionary<PopSocioeconomicClass, Dictionary<MarketGood, float>> PopNeeds = new()
        {
            {
                PopSocioeconomicClass.Capitalist, new Dictionary<MarketGood, float> {
                    { Retail(GoodType.LuxuryFood), 5f },
                    { Retail(GoodType.LuxuryClothes), 3f },
                    { Retail(GoodType.LuxuryFurniture), 2f },
                    { Retail(GoodType.SimRealSets), 4f },
                    { Retail(GoodType.Automobiles), 2f },
                    { Retail(GoodType.MedicalCare), 5f },
                    { Retail(GoodType.Security), 5f }
                }
            },
            {
                PopSocioeconomicClass.WhiteCollar, new Dictionary<MarketGood, float> {
                    { Retail(GoodType.BasicFood), 2f },
                    { Retail(GoodType.LuxuryFood), 1f },
                    { Retail(GoodType.Clothes), 3f },
                    { Retail(GoodType.Furniture), 2f },
                    { Retail(GoodType.HoloScreens), 3f },
                    { Retail(GoodType.MedicalCare), 3f },
                    { Retail(GoodType.Security), 3f },
                    { Retail(GoodType.Automobiles), 1f }
                }
            },
            {
                PopSocioeconomicClass.BlueCollar, new Dictionary<MarketGood, float> {
                    { Retail(GoodType.BasicFood), 4f },
                    { Retail(GoodType.Liquor), 2f },
                    { Retail(GoodType.Clothes), 2f },
                    { Retail(GoodType.Furniture), 1f },
                    { Retail(GoodType.PersonalTerminals), 2f },
                    { Retail(GoodType.MedicalCare), 1f },
                    { Retail(GoodType.Security), 1f }
                }
            },
            {
                PopSocioeconomicClass.Destitute, new Dictionary<MarketGood, float> {
                    { Retail(GoodType.BasicFood), 1f },
                    { Retail(GoodType.Liquor), 2f },
                    { Retail(GoodType.CheapEntertainment), 1f }
                }
            }
        };

        public static Business CreateBusiness(string type)
        {
            var b = new Business { BusinessType = type, Name = $"Generic {type}" };

            // We default to generating Wholesale goods for industries, and Retail for storefronts
            switch (type)
            {
                // --- UTILITIES & RAW MATERIALS ---
                case "Fusion Power Plant":
                    b.Outputs.Add(Wholesale(GoodType.Electricity), 500f);
                    b.Outputs.Add(Retail(GoodType.Electricity), 500f); // Power plants output direct to consumers and biz
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 20f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 50);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 10);
                    break;

                case "Automated Mine":
                    b.Outputs.Add(Wholesale(GoodType.RawMaterials), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 20f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 80);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 5);
                    break;

                case "Petro-Chem Plant":
                    b.Outputs.Add(Wholesale(GoodType.SyntheticMaterials), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 30f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 60);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 5);
                    break;

                // --- MANUFACTURING (Outputs Wholesale) ---
                case "Automated Factory":
                    b.Outputs.Add(Wholesale(GoodType.ManufacturedGoods), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 40f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 30f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 80);
                    break;

                case "Auto Plant":
                    b.Outputs.Add(Wholesale(GoodType.Automobiles), 20f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 20f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 10f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 30f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 100);
                    break;

                case "Synthetic Distillery":
                    b.Outputs.Add(Wholesale(GoodType.Liquor), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 10f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 10f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 20);
                    break;

                case "Terminal Factory":
                    b.Outputs.Add(Wholesale(GoodType.PersonalTerminals), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 15f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 20f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 60);
                    break;

                case "SimReal Studio":
                    b.Outputs.Add(Wholesale(GoodType.SimRealSets), 10f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 5f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 30f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 15);
                    break;

                case "Synthetic Protein Farm":
                    b.Outputs.Add(Wholesale(GoodType.BasicFood), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 10f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 40);
                    break;

                case "Textile Mill":
                    b.Outputs.Add(Wholesale(GoodType.Clothes), 60f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 30f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 15f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 50);
                    break;

                case "Furniture Factory":
                    b.Outputs.Add(Wholesale(GoodType.Furniture), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 25f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 15f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 40);
                    break;

                // --- RETAIL STOREFRONTS (Consumes Wholesale, Outputs Retail) ---
                case "Auto Dealership":
                    b.Outputs.Add(Retail(GoodType.Automobiles), 20f);
                    b.InputGoods.Add(Wholesale(GoodType.Automobiles), 20f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 10f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 15);
                    break;

                case "Dive Bar":
                    b.Outputs.Add(Retail(GoodType.Liquor), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Liquor), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 5f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 10);
                    break;

                case "Tech Bazaar": // Sells Terminals and HoloScreens
                    b.Outputs.Add(Retail(GoodType.PersonalTerminals), 50f);
                    b.Outputs.Add(Retail(GoodType.HoloScreens), 30f);
                    b.InputGoods.Add(Wholesale(GoodType.PersonalTerminals), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.HoloScreens), 30f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 10f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 20);
                    break;

                case "SimReal Parlor":
                    b.Outputs.Add(Retail(GoodType.SimRealSets), 10f);
                    b.InputGoods.Add(Wholesale(GoodType.SimRealSets), 10f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 15f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 10);
                    break;

                case "Restaurant":
                    b.Outputs.Add(Retail(GoodType.BasicFood), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.BasicFood), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 10f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 30);
                    break;

                case "Clothing Store":
                    b.Outputs.Add(Retail(GoodType.Clothes), 60f);
                    b.InputGoods.Add(Wholesale(GoodType.Clothes), 60f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 10f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 20);
                    break;

                case "Furnishings":
                    b.Outputs.Add(Retail(GoodType.Furniture), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Furniture), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 10f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 20);
                    break;

                // --- SERVICES (Direct to Retail) ---
                case "Corporate Hospital":
                    b.Outputs.Add(Retail(GoodType.MedicalCare), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 10f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 30f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 30);
                    b.RequiredLabor.Add(JobRole.BlueCollarScience, 20);
                    break;

                case "Corp-Sec Precinct":
                    b.Outputs.Add(Retail(GoodType.Security), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Weapons), 10f); // Weapons are consumed wholesale
                    b.InputGoods.Add(Wholesale(GoodType.Automobiles), 2f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 15f);
                    b.RequiredLabor.Add(JobRole.BlueCollarMilitary, 40);
                    b.RequiredLabor.Add(JobRole.WhiteCollarMilitary, 10);
                    break;

                    // (Note: Stripped out the Luxury duplicates here to keep the snippet brief, but the logic is identical)
            }
            return b;
        }

        public static string GetBusinessToFulfillNeed(MarketGood need)
        {
            // C# 8+ Pattern matching makes this incredibly clean. 
            // We match on both the Type and State of the MarketGood record.
            return need switch
            {
                // Utilities & Raw Materials (Needed Wholesale by industry)
                { Type: GoodType.Electricity } => "Fusion Power Plant", // Fulfills both wholesale and retail
                { Type: GoodType.RawMaterials } => "Automated Mine",
                { Type: GoodType.SyntheticMaterials } => "Petro-Chem Plant",
                { Type: GoodType.ManufacturedGoods } => "Automated Factory",
                { Type: GoodType.Weapons } => "Munitions Plant", // Consumed wholesale by Sec precincts

                // Manufacturing (Outputs Wholesale)
                { Type: GoodType.BasicFood, State: GoodState.Wholesale } => "Synthetic Protein Farm",
                { Type: GoodType.Clothes, State: GoodState.Wholesale } => "Textile Mill",
                { Type: GoodType.Furniture, State: GoodState.Wholesale } => "Furniture Factory",
                { Type: GoodType.Automobiles, State: GoodState.Wholesale } => "Auto Plant",
                { Type: GoodType.Liquor, State: GoodState.Wholesale } => "Synthetic Distillery",
                { Type: GoodType.PersonalTerminals, State: GoodState.Wholesale } => "Terminal Factory",
                { Type: GoodType.HoloScreens, State: GoodState.Wholesale } => "Holo-Screen Plant",
                { Type: GoodType.SimRealSets, State: GoodState.Wholesale } => "SimReal Studio",

                // Retail (Outputs Retail)
                { Type: GoodType.BasicFood, State: GoodState.Retail } => "Restaurant",
                { Type: GoodType.Clothes, State: GoodState.Retail } => "Clothing Store",
                { Type: GoodType.Furniture, State: GoodState.Retail } => "Furnishings",
                { Type: GoodType.Automobiles, State: GoodState.Retail } => "Auto Dealership",
                { Type: GoodType.Liquor, State: GoodState.Retail } => "Dive Bar",
                { Type: GoodType.PersonalTerminals, State: GoodState.Retail } => "Tech Bazaar",
                { Type: GoodType.HoloScreens, State: GoodState.Retail } => "Tech Bazaar",
                { Type: GoodType.SimRealSets, State: GoodState.Retail } => "SimReal Parlor",

                // Services (Outputs Retail)
                { Type: GoodType.MedicalCare } => "Corporate Hospital",
                { Type: GoodType.Security } => "Corp-Sec Precinct",

                _ => "Synthetic Protein Farm" // Fallback to prevent crashes during expansion
            };
        }
    }
}