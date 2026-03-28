using CyberpunkGenerator.Models;
using CyberpunkGenerator.Data;

namespace CyberpunkGenerator.Economy
{
    /// <summary>
    /// Single source of truth for all business type name strings.
    /// Referenced by both CreateBusiness and GetBusinessToFulfillNeed so a
    /// rename is one edit in one place.
    /// </summary>
    public static class BusinessTypes
    {
        // Housing
        public const string PenthouseSpire = "Penthouse Spire";
        public const string CorporateArcology = "Corporate Arcology";
        public const string MegaBlockApartments = "Mega-Block Apartments";
        public const string CoffinMotelSlum = "Coffin-Motel Slum";

        // Corporate & Science
        public const string MegaCorpHeadquarters = "Mega-Corp Headquarters";
        public const string OrbitalResearchFacility = "Orbital Research Facility";
        public const string MedicalTechPlant = "Medical Tech Plant";

        // Food
        public const string AutomatedFoodFactory = "Automated Food Factory";
        public const string SyntheticMeatVat = "Synthetic Meat Vat";
        public const string FoodMarket = "Food Market";
        public const string GourmetMarket = "Gourmet Market";
        public const string SyntheticDistillery = "Synthetic Distillery";
        public const string DiveBar = "Dive Bar";

        // Clothing & Furniture
        public const string TextileMill = "Textile Mill";
        public const string DesignerStudio = "Designer Studio";
        public const string FurnitureFactory = "Furniture Factory";
        public const string ArtisanWoodworker = "Artisan Woodworker";
        public const string ClothingStore = "Clothing Store";
        public const string ClothingBoutique = "Clothing Boutique";
        public const string Furnishings = "Furnishings";
        public const string InteriorDesign = "Interior Design";

        // Utilities & Raw Materials
        public const string FusionPowerPlant = "Fusion Power Plant";
        public const string AutomatedMine = "Automated Mine";
        public const string PetroChemPlant = "Petro-Chem Plant";
        public const string BioCottonFarm = "Bio-Cotton Farm";
        public const string HydroponicWoodFarm = "Hydroponic Wood Farm";

        // Heavy Manufacturing
        public const string AutomatedFactory = "Automated Factory";
        public const string AutoPlant = "Auto Plant";
        public const string AutoDealership = "Auto Dealership";
        public const string MunitionsPlant = "Munitions Plant";

        // Tech & Entertainment
        public const string TerminalFactory = "Terminal Factory";
        public const string HoloScreenPlant = "Holo-Screen Plant";
        public const string SimRealStudio = "SimReal Studio";
        public const string TechBazaar = "Tech Bazaar";
        public const string SimRealParlor = "SimReal Parlor";

        // Augmentations & Biotech
        public const string StreetChromeFoundry = "Street-Chrome Foundry";
        public const string HighTechCyberLab = "High-Tech Cyber Lab";
        public const string RipperdocClinic = "Ripperdoc Clinic";
        public const string ChromeBoutique = "Chrome Boutique";
        public const string GeneTailoringClinic = "Gene-Tailoring Clinic";

        // Services
        public const string CorpMedClinic = "Corp-Med Clinic";
        public const string CorpSecPrecinct = "Corp-Sec Precinct";

        // Unknown
        public const string Unknown = "Unknown, Unexpected Business";
    }

    public static class EconomyBlueprints
    {
        private static MarketGood Retail(GoodType type) => new MarketGood(type, GoodState.Retail);
        private static MarketGood Wholesale(GoodType type) => new MarketGood(type, GoodState.Wholesale);

        // The central source of truth for physical space requirements
        public static readonly Dictionary<PopSocioeconomicClass, int> SqmPerPerson = new()
        {
            { PopSocioeconomicClass.Capitalist, 200 },
            { PopSocioeconomicClass.WhiteCollar, 100 },
            { PopSocioeconomicClass.BlueCollar, 50 },
            { PopSocioeconomicClass.Destitute, 25 }
        };

        // Needs are strictly defined per 100 people per day.
        public static Dictionary<PopSocioeconomicClass, Dictionary<MarketGood, float>> PopNeeds = new()
        {
            {
                PopSocioeconomicClass.Capitalist, new Dictionary<MarketGood, float> {
                    { Retail(GoodType.LuxuryHousing), 200f },
                    { Retail(GoodType.LuxuryFood), 200f },
                    { Retail(GoodType.LuxuryClothes), 30f },
                    { Retail(GoodType.LuxuryFurniture), 2f },
                    { Retail(GoodType.PersonalTerminals), 2f },
                    { Retail(GoodType.HoloScreens), 1f },
                    { Retail(GoodType.SimRealSets), 1f },
                    { Retail(GoodType.Automobiles), 5f },
                    { Retail(GoodType.MedicalCare), 2f },
                    { Retail(GoodType.Security), 5f },
                    { Retail(GoodType.LuxuryCybernetics), 5f },
                    { Retail(GoodType.GeneticTailoring), 2f }
                }
            },
            {
                PopSocioeconomicClass.WhiteCollar, new Dictionary<MarketGood, float> {
                    { Retail(GoodType.ComfortableHousing), 100f },
                    { Retail(GoodType.BasicFood), 200f },
                    { Retail(GoodType.LuxuryFood), 100f },
                    { Retail(GoodType.Clothes), 20f },
                    { Retail(GoodType.LuxuryClothes), 5f },
                    { Retail(GoodType.Furniture), 0.5f },
                    { Retail(GoodType.LuxuryFurniture), 0.5f },
                    { Retail(GoodType.PersonalTerminals), 1f },
                    { Retail(GoodType.HoloScreens), 0.5f },
                    { Retail(GoodType.SimRealSets), 0.2f },
                    { Retail(GoodType.MedicalCare), 1f },
                    { Retail(GoodType.Security), 2f },
                    { Retail(GoodType.Automobiles), 2f },
                    { Retail(GoodType.BasicCybernetics), 5f }
                }
            },
            {
                PopSocioeconomicClass.BlueCollar, new Dictionary<MarketGood, float> {
                    { Retail(GoodType.BasicHousing), 50f },
                    { Retail(GoodType.BasicFood), 200f },
                    { Retail(GoodType.Liquor), 100f },
                    { Retail(GoodType.Clothes), 10f },
                    { Retail(GoodType.Furniture), 0.5f },
                    { Retail(GoodType.PersonalTerminals), 0.5f },
                    { Retail(GoodType.MedicalCare), 0.5f },
                    { Retail(GoodType.Security), 0.5f },
                    { Retail(GoodType.BasicCybernetics), 2f }
                }
            },
            {
                PopSocioeconomicClass.Destitute, new Dictionary<MarketGood, float> {
                    { Retail(GoodType.SlumHousing), 25f },
                    { Retail(GoodType.BasicFood), 100f },
                    { Retail(GoodType.Clothes), 2f },
                    { Retail(GoodType.Liquor), 50f }
                }
            }
        };

        public static Business CreateBusiness(string type)
        {
            var b = new Business { BusinessType = type, Name = $"Generic {type}" };

            switch (type)
            {
                // ==========================================
                // --- HOUSING ---
                // ==========================================
                case BusinessTypes.PenthouseSpire:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.Capitalist;
                    b.Outputs.Add(Retail(GoodType.LuxuryHousing), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 50f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 50);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 50);
                    break;

                case BusinessTypes.CorporateArcology:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.ComfortableHousing), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 20f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 30);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 70);
                    break;

                case BusinessTypes.MegaBlockApartments:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.BasicHousing), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 200f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 10);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 90);
                    break;

                case BusinessTypes.CoffinMotelSlum:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.Destitute;
                    b.Outputs.Add(Retail(GoodType.SlumHousing), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 50f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 20);
                    break;

                // ==========================================
                // --- CORPORATE & SCIENCE OVERHEAD ---
                // ==========================================
                case BusinessTypes.MegaCorpHeadquarters:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.IsWholeBlock = true;
                    b.TargetClass = PopSocioeconomicClass.Capitalist;
                    b.Outputs.Add(Wholesale(GoodType.CorporateServices), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.LuxuryFurniture), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Data), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1000f);
                    b.RequiredLabor.Add(JobRole.CapCommercial, 150);
                    b.RequiredLabor.Add(JobRole.CapScience, 50);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 500);
                    break;

                case BusinessTypes.OrbitalResearchFacility:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.Data), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.PersonalTerminals), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1500f);
                    b.RequiredLabor.Add(JobRole.CapScience, 20);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 400);
                    b.RequiredLabor.Add(JobRole.BlueCollarScience, 100);
                    break;

                case BusinessTypes.MedicalTechPlant:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.MedicalEquipment), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 3000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 100);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 400);
                    break;

                // ==========================================
                // --- FOOD SUPPLY CHAIN ---
                // ==========================================
                case BusinessTypes.AutomatedFoodFactory:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.BasicFood), 200000f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 5000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 20);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 300);
                    break;

                case BusinessTypes.SyntheticMeatVat:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.LuxuryFood), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.Data), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 2000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 50);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 100);
                    break;

                case BusinessTypes.FoodMarket:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.BasicFood), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.BasicFood), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 100f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 5);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 45);
                    break;

                case BusinessTypes.GourmetMarket:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.LuxuryFood), 2500f);
                    b.InputGoods.Add(Wholesale(GoodType.LuxuryFood), 2500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 100f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 15);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 20);
                    break;

                case BusinessTypes.SyntheticDistillery:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.Liquor), 50000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 5000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 200);
                    break;

                case BusinessTypes.DiveBar:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.Liquor), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Liquor), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 50f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 15);
                    break;

                // ==========================================
                // --- CLOTHING & FURNITURE ---
                // ==========================================
                case BusinessTypes.TextileMill:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.Clothes), 100000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 5000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 500);
                    break;

                case BusinessTypes.DesignerStudio:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.LuxuryClothes), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.NaturalFabric), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 50);
                    break;

                case BusinessTypes.FurnitureFactory:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.Furniture), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 4000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 300);
                    break;

                case BusinessTypes.ArtisanWoodworker:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.LuxuryFurniture), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.Wood), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 500f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 20);
                    break;

                case BusinessTypes.ClothingStore:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.Clothes), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Clothes), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 50f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 15);
                    break;

                case BusinessTypes.ClothingBoutique:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.LuxuryClothes), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.LuxuryClothes), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 5f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 50f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 10);
                    b.RequiredLabor.Add(JobRole.CapCommercial, 1);
                    break;

                case BusinessTypes.Furnishings:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.Furniture), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.Furniture), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 50f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 10);
                    break;

                case BusinessTypes.InteriorDesign:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.Capitalist;
                    b.Outputs.Add(Retail(GoodType.LuxuryFurniture), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.LuxuryFurniture), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 5f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 20f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 5);
                    b.RequiredLabor.Add(JobRole.CapCommercial, 1);
                    break;

                // ==========================================
                // --- UTILITIES & RAW MATERIALS ---
                // ==========================================
                case BusinessTypes.FusionPowerPlant:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.Electricity), 20000f);
                    b.Outputs.Add(Retail(GoodType.Electricity), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 4000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 100);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 20);
                    break;

                case BusinessTypes.AutomatedMine:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.RawMaterials), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 400);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 25);
                    break;

                case BusinessTypes.PetroChemPlant:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.SyntheticMaterials), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 2500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1500f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 300);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 25);
                    break;

                case BusinessTypes.BioCottonFarm:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.NaturalFabric), 2500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 200);
                    break;

                case BusinessTypes.HydroponicWoodFarm:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.Wood), 1500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 2000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 150);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 10);
                    break;

                // ==========================================
                // --- HEAVY MANUFACTURING ---
                // ==========================================
                case BusinessTypes.AutomatedFactory:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.ManufacturedGoods), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 4000f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 3000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 800);
                    break;

                case BusinessTypes.AutoPlant:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.Automobiles), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1500f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 1000);
                    break;

                case BusinessTypes.AutoDealership:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.Automobiles), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Automobiles), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 50f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 15);
                    break;

                case BusinessTypes.MunitionsPlant:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.Weapons), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 2000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 500);
                    break;

                // ==========================================
                // --- TECH & ENTERTAINMENT ---
                // ==========================================
                case BusinessTypes.TerminalFactory:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.PersonalTerminals), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 1500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 2000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 600);
                    break;

                case BusinessTypes.HoloScreenPlant:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.HoloScreens), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 2500f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 400);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 50);
                    break;

                case BusinessTypes.SimRealStudio:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.SimRealSets), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.Data), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 3000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 150);
                    break;

                case BusinessTypes.TechBazaar:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.PersonalTerminals), 500f);
                    b.Outputs.Add(Retail(GoodType.HoloScreens), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.PersonalTerminals), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.HoloScreens), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 100f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 20);
                    break;

                case BusinessTypes.SimRealParlor:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.Capitalist;
                    b.Outputs.Add(Retail(GoodType.SimRealSets), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.SimRealSets), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 150f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 10);
                    break;

                // ==========================================
                // --- AUGMENTATIONS & BIOTECH ---
                // ==========================================
                case BusinessTypes.StreetChromeFoundry:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.BasicCybernetics), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 4000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 50);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 500);
                    break;

                case BusinessTypes.HighTechCyberLab:
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.IsWholeBlock = true;
                    b.Outputs.Add(Wholesale(GoodType.LuxuryCybernetics), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Data), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 1500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 5000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 200);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 200);
                    break;

                case BusinessTypes.RipperdocClinic:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.BasicCybernetics), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.BasicCybernetics), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 150f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 5);
                    b.RequiredLabor.Add(JobRole.BlueCollarScience, 10);
                    break;

                case BusinessTypes.ChromeBoutique:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.LuxuryCybernetics), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.LuxuryCybernetics), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 20f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 200f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 10);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 5);
                    break;

                case BusinessTypes.GeneTailoringClinic:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.Capitalist;
                    b.Outputs.Add(Retail(GoodType.GeneticTailoring), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.MedicalEquipment), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.Data), 150f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 300f);
                    b.RequiredLabor.Add(JobRole.CapScience, 2);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 15);
                    break;

                // ==========================================
                // --- SERVICES ---
                // ==========================================
                case BusinessTypes.CorpMedClinic:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.MedicalCare), 250f);
                    b.InputGoods.Add(Wholesale(GoodType.MedicalEquipment), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 10f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 100f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 15);
                    b.RequiredLabor.Add(JobRole.BlueCollarScience, 35);
                    break;

                case BusinessTypes.CorpSecPrecinct:
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.Outputs.Add(Retail(GoodType.Security), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.Weapons), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Automobiles), 10f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 20f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 150f);
                    b.RequiredLabor.Add(JobRole.BlueCollarMilitary, 80);
                    b.RequiredLabor.Add(JobRole.WhiteCollarMilitary, 20);
                    break;
            }
            return b;
        }

        public static string GetBusinessToFulfillNeed(MarketGood need)
        {
            return need switch
            {
                // Corporate & Science Overhead
                { Type: GoodType.Data, State: GoodState.Wholesale } => BusinessTypes.OrbitalResearchFacility,
                { Type: GoodType.CorporateServices, State: GoodState.Wholesale } => BusinessTypes.MegaCorpHeadquarters,
                { Type: GoodType.MedicalEquipment, State: GoodState.Wholesale } => BusinessTypes.MedicalTechPlant,

                // Biotech & Augmentations
                { Type: GoodType.BasicCybernetics, State: GoodState.Wholesale } => BusinessTypes.StreetChromeFoundry,
                { Type: GoodType.BasicCybernetics, State: GoodState.Retail } => BusinessTypes.RipperdocClinic,
                { Type: GoodType.LuxuryCybernetics, State: GoodState.Wholesale } => BusinessTypes.HighTechCyberLab,
                { Type: GoodType.LuxuryCybernetics, State: GoodState.Retail } => BusinessTypes.ChromeBoutique,
                { Type: GoodType.GeneticTailoring, State: GoodState.Retail } => BusinessTypes.GeneTailoringClinic,

                // Housing
                { Type: GoodType.LuxuryHousing } => BusinessTypes.PenthouseSpire,
                { Type: GoodType.ComfortableHousing } => BusinessTypes.CorporateArcology,
                { Type: GoodType.BasicHousing } => BusinessTypes.MegaBlockApartments,
                { Type: GoodType.SlumHousing } => BusinessTypes.CoffinMotelSlum,

                // Utilities & Raw Materials
                { Type: GoodType.Electricity } => BusinessTypes.FusionPowerPlant,
                { Type: GoodType.RawMaterials } => BusinessTypes.AutomatedMine,
                { Type: GoodType.SyntheticMaterials } => BusinessTypes.PetroChemPlant,
                { Type: GoodType.NaturalFabric } => BusinessTypes.BioCottonFarm,
                { Type: GoodType.Wood } => BusinessTypes.HydroponicWoodFarm,
                { Type: GoodType.ManufacturedGoods } => BusinessTypes.AutomatedFactory,
                { Type: GoodType.Weapons } => BusinessTypes.MunitionsPlant,

                // Wholesale
                { Type: GoodType.BasicFood, State: GoodState.Wholesale } => BusinessTypes.AutomatedFoodFactory,
                { Type: GoodType.LuxuryFood, State: GoodState.Wholesale } => BusinessTypes.SyntheticMeatVat,
                { Type: GoodType.Clothes, State: GoodState.Wholesale } => BusinessTypes.TextileMill,
                { Type: GoodType.LuxuryClothes, State: GoodState.Wholesale } => BusinessTypes.DesignerStudio,
                { Type: GoodType.Furniture, State: GoodState.Wholesale } => BusinessTypes.FurnitureFactory,
                { Type: GoodType.LuxuryFurniture, State: GoodState.Wholesale } => BusinessTypes.ArtisanWoodworker,
                { Type: GoodType.Automobiles, State: GoodState.Wholesale } => BusinessTypes.AutoPlant,
                { Type: GoodType.Liquor, State: GoodState.Wholesale } => BusinessTypes.SyntheticDistillery,
                { Type: GoodType.PersonalTerminals, State: GoodState.Wholesale } => BusinessTypes.TerminalFactory,
                { Type: GoodType.HoloScreens, State: GoodState.Wholesale } => BusinessTypes.HoloScreenPlant,
                { Type: GoodType.SimRealSets, State: GoodState.Wholesale } => BusinessTypes.SimRealStudio,

                // Retail
                { Type: GoodType.BasicFood, State: GoodState.Retail } => BusinessTypes.FoodMarket,
                { Type: GoodType.LuxuryFood, State: GoodState.Retail } => BusinessTypes.GourmetMarket,
                { Type: GoodType.Clothes, State: GoodState.Retail } => BusinessTypes.ClothingStore,
                { Type: GoodType.LuxuryClothes, State: GoodState.Retail } => BusinessTypes.ClothingBoutique,
                { Type: GoodType.Furniture, State: GoodState.Retail } => BusinessTypes.Furnishings,
                { Type: GoodType.LuxuryFurniture, State: GoodState.Retail } => BusinessTypes.InteriorDesign,
                { Type: GoodType.Automobiles, State: GoodState.Retail } => BusinessTypes.AutoDealership,
                { Type: GoodType.Liquor, State: GoodState.Retail } => BusinessTypes.DiveBar,
                { Type: GoodType.PersonalTerminals, State: GoodState.Retail } => BusinessTypes.TechBazaar,
                { Type: GoodType.HoloScreens, State: GoodState.Retail } => BusinessTypes.TechBazaar,
                { Type: GoodType.SimRealSets, State: GoodState.Retail } => BusinessTypes.SimRealParlor,

                // Services
                { Type: GoodType.MedicalCare } => BusinessTypes.CorpMedClinic,
                { Type: GoodType.Security } => BusinessTypes.CorpSecPrecinct,

                _ => BusinessTypes.Unknown
            };
        }
    }
}