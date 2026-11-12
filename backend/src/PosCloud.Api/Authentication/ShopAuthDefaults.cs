namespace PosCloud.Api.Authentication;

/// <summary>
/// Mobil ilova mijozi uchun alohida JWT sxemasi. Mijoz endpointlari
/// <c>[Authorize(Policy = ShopAuthDefaults.Policy)]</c> bilan belgilanadi.
/// </summary>
public static class ShopAuthDefaults
{
    public const string Scheme = "Shop";
    public const string Policy = "ShopCustomer";
}
