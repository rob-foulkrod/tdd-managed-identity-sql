namespace ManagedIdentityCatalog.Models;

public sealed record PriceAnalysisDto(
    string Category,
    int ProductCount,
    decimal AvgPrice,
    decimal MinPrice,
    decimal MaxPrice,
    decimal? AvgMarginPercent);
