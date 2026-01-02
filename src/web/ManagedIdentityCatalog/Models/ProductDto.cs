namespace ManagedIdentityCatalog.Models;

public sealed record ProductDto(
    int ProductId,
    string Name,
    string? Color,
    decimal ListPrice,
    string? CategoryName);
