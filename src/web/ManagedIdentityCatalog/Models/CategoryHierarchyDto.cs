namespace ManagedIdentityCatalog.Models;

public sealed record CategoryHierarchyDto(
    int CategoryId,
    string Name,
    int? ParentCategoryId,
    string CategoryPath,
    int Level,
    int ProductCount);
