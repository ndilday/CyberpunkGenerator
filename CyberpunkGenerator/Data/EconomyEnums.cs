namespace CyberpunkGenerator.Data
{
    public enum PopField { Commercial, Industrial, Military, Science }
    public enum PopSocioeconomicClass { Capitalist, WhiteCollar, BlueCollar, Destitute }

    // Cleaned up: No more "Bulk" prefixes
    public enum GoodType
    {
        SimRealSets,
        HoloScreens,
        PersonalTerminals,
        BasicFood,
        LuxuryFood,
        Liquor,
        CheapEntertainment,
        Clothes,
        LuxuryClothes,
        Furniture,
        LuxuryFurniture,
        Automobiles,
        MedicalCare,
        Security,
        Logistics,
        Electricity,
        SyntheticMaterials,
        NaturalFabric,
        Wood,
        ManufacturedGoods,
        RawMaterials,
        Weapons
    }

    public enum GoodState
    {
        Wholesale, // Fresh from the factory, used by businesses
        Retail     // Sold in a storefront, demanded by pops
    }

    // The new composite key for our economy
    public record MarketGood(GoodType Type, GoodState State)
    {
        public override string ToString() => $"{State} {Type}";
    }

    public record JobRole(PopSocioeconomicClass Class, PopField Field)
    {
        public static JobRole CapCommercial => new(PopSocioeconomicClass.Capitalist, PopField.Commercial);
        public static JobRole CapScience => new(PopSocioeconomicClass.Capitalist, PopField.Science);
        public static JobRole CapMilitary => new(PopSocioeconomicClass.Capitalist, PopField.Military);
        public static JobRole CapIndustrial => new(PopSocioeconomicClass.Capitalist, PopField.Industrial);

        public static JobRole WhiteCollarCommercial => new(PopSocioeconomicClass.WhiteCollar, PopField.Commercial);
        public static JobRole WhiteCollarScience => new(PopSocioeconomicClass.WhiteCollar, PopField.Science);
        public static JobRole WhiteCollarIndustrial => new(PopSocioeconomicClass.WhiteCollar, PopField.Industrial);
        public static JobRole WhiteCollarMilitary => new(PopSocioeconomicClass.WhiteCollar, PopField.Military);

        public static JobRole BlueCollarIndustrial => new(PopSocioeconomicClass.BlueCollar, PopField.Industrial);
        public static JobRole BlueCollarScience => new(PopSocioeconomicClass.BlueCollar, PopField.Science);
        public static JobRole BlueCollarMilitary => new(PopSocioeconomicClass.BlueCollar, PopField.Military);
        public static JobRole BlueCollarCommercial => new(PopSocioeconomicClass.BlueCollar, PopField.Commercial);

        public override string ToString() => $"{Class} {Field}";
    }
}