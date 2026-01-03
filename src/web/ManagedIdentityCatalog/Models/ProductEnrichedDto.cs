namespace ManagedIdentityCatalog.Models;

public sealed record ProductEnrichedDto(
    int ProductId,
    string Name,
    string? Color,
    string? Size,
    decimal ListPrice,
    decimal? StandardCost,
    string? CategoryName,
    // Computed fields
    decimal? MarginPercent,
    string LifecycleStatus,
    int DaysInCatalog,
    int TotalUnitsSold,
    decimal TotalRevenue,
    decimal? Weight);
