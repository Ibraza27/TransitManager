namespace TransitManager.Core.Enums
{
    public enum DiscountType
    {
        Percent = 0, // %
        Amount = 1   // EUR
    }

    public enum DiscountBase
    {
        BaseHT = 0,
        BaseTTC = 1
    }

    public enum DiscountScope
    {
        Total = 0,
        PerLine = 1 
    }
}
