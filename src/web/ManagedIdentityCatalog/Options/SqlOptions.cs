namespace ManagedIdentityCatalog.Options;

public sealed class SqlOptions
{
    public const string SectionName = "Sql";

    public string Server { get; init; } = string.Empty;

    public string Database { get; init; } = string.Empty;
}
