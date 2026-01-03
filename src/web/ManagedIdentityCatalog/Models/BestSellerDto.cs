namespace ManagedIdentityCatalog.Models;

public sealed record BestSellerDto(
    int ProductId,
    string Name,
    int TotalUnitsSold,
    decimal TotalRevenue);
