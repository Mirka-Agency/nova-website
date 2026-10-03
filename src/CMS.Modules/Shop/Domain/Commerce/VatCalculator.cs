namespace CMS.Modules.Shop.Domain.Commerce;

public static class VatCalculator
{
    public static decimal ComputeAmount(decimal taxableBase, decimal percent) =>
        Math.Round(Math.Max(0, taxableBase) * percent / 100m, 2, MidpointRounding.AwayFromZero);
}
