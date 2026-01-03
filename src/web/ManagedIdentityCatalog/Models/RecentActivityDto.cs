namespace ManagedIdentityCatalog.Models;

public sealed record RecentActivityDto(
    int ProductId,
    string Name,
    DateTime OrderDate,
    int QtySold,
    decimal Revenue);
