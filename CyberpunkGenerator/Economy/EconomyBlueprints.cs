using CyberpunkGenerator.Models;
using CyberpunkGenerator.Data;
using System.Collections.Generic;

namespace CyberpunkGenerator.Economy
{
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
                    { Retail(GoodType.SimRealSets), 10f },
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
                    { Retail(GoodType.HoloScreens), 10f },
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
                    { Retail(GoodType.PersonalTerminals), 5f },
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
                // --- HOUSING (Outputs 10,000 units to fill 1 block) ---
                // ==========================================
                case "Penthouse Spire":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.Capitalist;
                    b.Outputs.Add(Retail(GoodType.LuxuryHousing), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 50f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 50);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 50);
                    break;

                case "Corporate Arcology":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.ComfortableHousing), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 20f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 30);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 70);
                    break;

                case "Mega-Block Apartments":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.BasicHousing), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 200f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 10);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 90);
                    break;

                case "Coffin-Motel Slum":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.Destitute;
                    b.Outputs.Add(Retail(GoodType.SlumHousing), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 50f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 20);
                    break;

                // ==========================================
                // --- CORPORATE & SCIENCE OVERHEAD ---
                // ==========================================
                case "Mega-Corp Headquarters":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.Capitalist;
                    b.Outputs.Add(Wholesale(GoodType.CorporateServices), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.LuxuryFurniture), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Data), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1000f);
                    b.RequiredLabor.Add(JobRole.CapCommercial, 150);
                    b.RequiredLabor.Add(JobRole.CapScience, 50);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 500);
                    break;

                case "Orbital Research Facility":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.Data), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.PersonalTerminals), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1500f);
                    b.RequiredLabor.Add(JobRole.CapScience, 20);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 400);
                    b.RequiredLabor.Add(JobRole.BlueCollarScience, 100);
                    break;

                case "Medical Tech Plant":
                    b.ZoneType = BusinessZoneType.Industrial;
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
                case "Automated Food Factory":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.BasicFood), 200000f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 5000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 20);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 300);
                    break;

                case "Synthetic Meat Vat":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.LuxuryFood), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.Data), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 2000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 50);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 100);
                    break;

                case "Food Market":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.BasicFood), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.BasicFood), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 100f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 5);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 45);
                    break;

                case "Gourmet Market":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.LuxuryFood), 2500f);
                    b.InputGoods.Add(Wholesale(GoodType.LuxuryFood), 2500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 100f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 15);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 20);
                    break;

                case "Synthetic Distillery":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.Liquor), 50000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 5000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 200);
                    break;

                case "Dive Bar":
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
                case "Textile Mill":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.Clothes), 100000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 5000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 500);
                    break;

                case "Designer Studio":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.LuxuryClothes), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.NaturalFabric), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 50);
                    break;

                case "Furniture Factory":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.Furniture), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 4000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 300);
                    break;

                case "Artisan Woodworker":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.LuxuryFurniture), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.Wood), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 500f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 20);
                    break;

                case "Clothing Store":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.Clothes), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Clothes), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 50f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 15);
                    break;

                case "Clothing Boutique":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.LuxuryClothes), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.LuxuryClothes), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 5f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 50f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 10);
                    b.RequiredLabor.Add(JobRole.CapCommercial, 1);
                    break;

                case "Furnishings":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.Furniture), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.Furniture), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 50f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 10);
                    break;

                case "Interior Design":
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
                case "Fusion Power Plant":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.Electricity), 20000f);
                    b.Outputs.Add(Retail(GoodType.Electricity), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 4000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 100);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 20);
                    break;

                case "Automated Mine":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.RawMaterials), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 400);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 25);
                    break;

                case "Petro-Chem Plant":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.SyntheticMaterials), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 2500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1500f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 300);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 25);
                    break;

                case "Bio-Cotton Farm":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.NaturalFabric), 2500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 200);
                    break;

                case "Hydroponic Wood Farm":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.Wood), 1500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 2000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 150);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 10);
                    break;

                // ==========================================
                // --- HEAVY MANUFACTURING ---
                // ==========================================
                case "Automated Factory":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.ManufacturedGoods), 10000f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 4000f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 3000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 800);
                    break;

                case "Auto Plant":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.Automobiles), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 1500f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 1000);
                    break;

                case "Auto Dealership":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.Automobiles), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Automobiles), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 50f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 15);
                    break;

                case "Munitions Plant":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.Weapons), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 2000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 500);
                    break;

                // ==========================================
                // --- TECH & ENTERTAINMENT ---
                // ==========================================
                case "Terminal Factory":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.PersonalTerminals), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 1500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 2000f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 600);
                    break;

                case "Holo-Screen Plant":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.HoloScreens), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 2500f);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 400);
                    b.RequiredLabor.Add(JobRole.WhiteCollarIndustrial, 50);
                    break;

                case "SimReal Studio":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.SimRealSets), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.ManufacturedGoods), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.Data), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 3000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 150);
                    break;

                case "Tech Bazaar":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.PersonalTerminals), 500f);
                    b.Outputs.Add(Retail(GoodType.HoloScreens), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.PersonalTerminals), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.HoloScreens), 200f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 100f);
                    b.RequiredLabor.Add(JobRole.BlueCollarCommercial, 20);
                    break;

                case "SimReal Parlor":
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
                case "Street-Chrome Foundry":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.BasicCybernetics), 5000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 2000f);
                    b.InputGoods.Add(Wholesale(GoodType.RawMaterials), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 4000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 50);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 500);
                    break;

                case "High-Tech Cyber Lab":
                    b.ZoneType = BusinessZoneType.Industrial;
                    b.Outputs.Add(Wholesale(GoodType.LuxuryCybernetics), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.Data), 1000f);
                    b.InputGoods.Add(Wholesale(GoodType.SyntheticMaterials), 1500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 5000f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 200);
                    b.RequiredLabor.Add(JobRole.BlueCollarIndustrial, 200);
                    break;

                case "Ripperdoc Clinic":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.BlueCollar;
                    b.Outputs.Add(Retail(GoodType.BasicCybernetics), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.BasicCybernetics), 500f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 150f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 5);
                    b.RequiredLabor.Add(JobRole.BlueCollarScience, 10);
                    break;

                case "Chrome Boutique":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.LuxuryCybernetics), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.LuxuryCybernetics), 100f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 20f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 200f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarCommercial, 10);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 5);
                    break;

                case "Gene-Tailoring Clinic":
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
                case "Corp-Med Clinic":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.TargetClass = PopSocioeconomicClass.WhiteCollar;
                    b.Outputs.Add(Retail(GoodType.MedicalCare), 250f); // Tuned for throughput
                    b.InputGoods.Add(Wholesale(GoodType.MedicalEquipment), 50f);
                    b.InputGoods.Add(Wholesale(GoodType.CorporateServices), 10f);
                    b.InputGoods.Add(Wholesale(GoodType.Electricity), 100f);
                    b.RequiredLabor.Add(JobRole.WhiteCollarScience, 15);
                    b.RequiredLabor.Add(JobRole.BlueCollarScience, 35);
                    break;

                case "Corp-Sec Precinct":
                    b.ZoneType = BusinessZoneType.Commercial;
                    b.Outputs.Add(Retail(GoodType.Security), 500f); // High coverage area
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
                // Tech, Corporate & Science Overhead
                { Type: GoodType.Data, State: GoodState.Wholesale } => "Orbital Research Facility",
                { Type: GoodType.CorporateServices, State: GoodState.Wholesale } => "Mega-Corp Headquarters",
                { Type: GoodType.MedicalEquipment, State: GoodState.Wholesale } => "Medical Tech Plant",

                // Biotech & Augmentations
                { Type: GoodType.BasicCybernetics, State: GoodState.Wholesale } => "Street-Chrome Foundry",
                { Type: GoodType.BasicCybernetics, State: GoodState.Retail } => "Ripperdoc Clinic",
                { Type: GoodType.LuxuryCybernetics, State: GoodState.Wholesale } => "High-Tech Cyber Lab",
                { Type: GoodType.LuxuryCybernetics, State: GoodState.Retail } => "Chrome Boutique",
                { Type: GoodType.GeneticTailoring, State: GoodState.Retail } => "Gene-Tailoring Clinic",

                // Housing
                { Type: GoodType.LuxuryHousing } => "Penthouse Spire",
                { Type: GoodType.ComfortableHousing } => "Corporate Arcology",
                { Type: GoodType.BasicHousing } => "Mega-Block Apartments",
                { Type: GoodType.SlumHousing } => "Coffin-Motel Slum",

                // Utilities & Raw Materials
                { Type: GoodType.Electricity } => "Fusion Power Plant",
                { Type: GoodType.RawMaterials } => "Automated Mine",
                { Type: GoodType.SyntheticMaterials } => "Petro-Chem Plant",
                { Type: GoodType.NaturalFabric } => "Bio-Cotton Farm",
                { Type: GoodType.Wood } => "Hydroponic Wood Farm",
                { Type: GoodType.ManufacturedGoods } => "Automated Factory",
                { Type: GoodType.Weapons } => "Munitions Plant",

                // Wholesale
                { Type: GoodType.BasicFood, State: GoodState.Wholesale } => "Automated Food Factory",
                { Type: GoodType.LuxuryFood, State: GoodState.Wholesale } => "Synthetic Meat Vat",
                { Type: GoodType.Clothes, State: GoodState.Wholesale } => "Textile Mill",
                { Type: GoodType.LuxuryClothes, State: GoodState.Wholesale } => "Designer Studio",
                { Type: GoodType.Furniture, State: GoodState.Wholesale } => "Furniture Factory",
                { Type: GoodType.LuxuryFurniture, State: GoodState.Wholesale } => "Artisan Woodworker",
                { Type: GoodType.Automobiles, State: GoodState.Wholesale } => "Auto Plant",
                { Type: GoodType.Liquor, State: GoodState.Wholesale } => "Synthetic Distillery",
                { Type: GoodType.PersonalTerminals, State: GoodState.Wholesale } => "Terminal Factory",
                { Type: GoodType.HoloScreens, State: GoodState.Wholesale } => "Holo-Screen Plant",
                { Type: GoodType.SimRealSets, State: GoodState.Wholesale } => "SimReal Studio",

                // Retail
                { Type: GoodType.BasicFood, State: GoodState.Retail } => "Food Market",
                { Type: GoodType.LuxuryFood, State: GoodState.Retail } => "Gourmet Market",
                { Type: GoodType.Clothes, State: GoodState.Retail } => "Clothing Store",
                { Type: GoodType.LuxuryClothes, State: GoodState.Retail } => "Clothing Boutique",
                { Type: GoodType.Furniture, State: GoodState.Retail } => "Furnishings",
                { Type: GoodType.LuxuryFurniture, State: GoodState.Retail } => "Interior Design",
                { Type: GoodType.Automobiles, State: GoodState.Retail } => "Auto Dealership",
                { Type: GoodType.Liquor, State: GoodState.Retail } => "Dive Bar",
                { Type: GoodType.PersonalTerminals, State: GoodState.Retail } => "Tech Bazaar",
                { Type: GoodType.HoloScreens, State: GoodState.Retail } => "Tech Bazaar",
                { Type: GoodType.SimRealSets, State: GoodState.Retail } => "SimReal Parlor",

                // Services
                { Type: GoodType.MedicalCare } => "Corporate Hospital",
                { Type: GoodType.Security } => "Corp-Sec Precinct",

                _ => "Automated Food Factory"
            };
        }
    }
}