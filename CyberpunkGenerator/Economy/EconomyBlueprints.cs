using CyberpunkGenerator.Data;
using CyberpunkGenerator.Models;

namespace CyberpunkGenerator.Economy
{
    public static class EconomyBlueprints
    {
        // Needs are primarily driven by Socioeconomic Class
        public static Dictionary<PopSocioeconomicClass, Dictionary<GoodType, float>> PopNeeds = new()
        {
            {
                PopSocioeconomicClass.Capitalist, new Dictionary<GoodType, float> {
                    { GoodType.LuxuryFood, 5f },
                    { GoodType.HighTechToys, 5f }
                }
            },
            {
                PopSocioeconomicClass.WhiteCollar, new Dictionary<GoodType, float> {
                    { GoodType.LuxuryFood, 2f },
                    { GoodType.BasicFood, 3f },
                    { GoodType.CheapEntertainment, 3f },
                    { GoodType.HighTechToys, 2f }
                }
            },
            {
                PopSocioeconomicClass.BlueCollar, new Dictionary<GoodType, float> {
                    { GoodType.BasicFood, 5f },
                    { GoodType.CheapEntertainment, 3f },
                    { GoodType.Liquor, 2f }
                }
            },
            {
                PopSocioeconomicClass.Destitute, new Dictionary<GoodType, float> {
                    { GoodType.BasicFood, 3f }, // Barely surviving
                    { GoodType.Liquor, 2f }
                }
            }
        };

        public static Business CreateBusiness(string type)
        {
            var b = new Business { BusinessType = type, Name = $"Generic {type}" };

            switch (type)
            {
                case "Luxury Cyber-Boutique":
                    b.Outputs.Add(GoodType.HighTechToys, 10f);
                    b.InputGoods.Add(GoodType.ManufacturedGoods, 10f);

                    // Needs WhiteCollar Commercial (Salespeople) and Capitalist Commercial (Owner/Exec)
                    b.RequiredLabor.Add(new JobRole(PopSocioeconomicClass.WhiteCollar, PopField.Commercial), 20);
                    b.RequiredLabor.Add(new JobRole(PopSocioeconomicClass.Capitalist, PopField.Commercial), 2);
                    break;

                case "Automated Factory":
                    b.Outputs.Add(GoodType.ManufacturedGoods, 50f);
                    b.InputGoods.Add(GoodType.RawMaterials, 50f);

                    // Needs BlueCollar Industrial (Machinists) and WhiteCollar Industrial (Engineers/Foremen)
                    b.RequiredLabor.Add(new JobRole(PopSocioeconomicClass.BlueCollar, PopField.Industrial), 100);
                    b.RequiredLabor.Add(new JobRole(PopSocioeconomicClass.WhiteCollar, PopField.Industrial), 5);
                    break;

                case "Street Ripperdoc Clinic":
                    b.Outputs.Add(GoodType.MedicalSupplies, 20f); // Providing medical services
                    b.InputGoods.Add(GoodType.ManufacturedGoods, 5f);

                    // Needs WhiteCollar Science (The Doc) and BlueCollar Science (Assistants/Cleaners)
                    b.RequiredLabor.Add(new JobRole(PopSocioeconomicClass.WhiteCollar, PopField.Science), 2);
                    b.RequiredLabor.Add(new JobRole(PopSocioeconomicClass.BlueCollar, PopField.Science), 5);
                    break;

                case "Private Security Firm":
                    b.Outputs.Add(GoodType.Weapons, 20f); // Abstracting "Security/Protection" as a good for now
                    b.InputGoods.Add(GoodType.ManufacturedGoods, 10f);

                    // Needs BlueCollar Military (Grants), WhiteCollar Military (Officers), Capitalist Military (Commanders)
                    b.RequiredLabor.Add(new JobRole(PopSocioeconomicClass.BlueCollar, PopField.Military), 50);
                    b.RequiredLabor.Add(new JobRole(PopSocioeconomicClass.WhiteCollar, PopField.Military), 10);
                    b.RequiredLabor.Add(new JobRole(PopSocioeconomicClass.Capitalist, PopField.Military), 1);
                    break;

                case "Synthetic Protein Farm":
                    b.Outputs.Add(GoodType.BasicFood, 100f);

                    // Needs BlueCollar Industrial (Farmhands)
                    b.RequiredLabor.Add(new JobRole(PopSocioeconomicClass.BlueCollar, PopField.Industrial), 50);
                    break;
            }
            return b;
        }

        public static string GetBusinessToFulfillNeed(GoodType need)
        {
            return need switch
            {
                GoodType.HighTechToys => "Luxury Cyber-Boutique",
                GoodType.ManufacturedGoods => "Automated Factory",
                GoodType.BasicFood => "Synthetic Protein Farm",
                GoodType.MedicalSupplies => "Street Ripperdoc Clinic",
                GoodType.Weapons => "Private Security Firm",
                _ => "Synthetic Protein Farm" // Fallback
            };
        }
    }
}