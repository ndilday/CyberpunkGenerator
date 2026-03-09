
namespace CyberpunkGenerator.Data
{
    public enum PopField
    {
        Commercial, // sales, interaction
        Industrial, // making goods/software
        Military,   // police, security, paramilitary
        Science     // research, medical
    }

    public enum PopSocioeconomicClass
    {
        Capitalist,
        WhiteCollar,
        BlueCollar,
        Destitute
    }

    public enum GoodType
    {
        HighTechToys,
        LuxuryFood,
        BasicFood,
        Liquor,
        CheapEntertainment,
        ManufacturedGoods,
        RawMaterials,
        Logistics,
        MedicalSupplies, // Added for Science pops
        Weapons          // Added for Military pops
    }

    public record JobRole(PopSocioeconomicClass Class, PopField Field);
}
