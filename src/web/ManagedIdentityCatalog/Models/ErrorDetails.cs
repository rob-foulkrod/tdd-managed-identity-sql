namespace ManagedIdentityCatalog.Models;

/// <summary>
/// Rich error diagnostics for demo/development environments.
/// Captures exception details for developer-friendly display.
/// </summary>
public sealed record ErrorDetails
{
    public required string ExceptionType { get; init; }
    public required string Message { get; init; }
    public string? StackTrace { get; init; }
    public List<InnerExceptionInfo> InnerExceptions { get; init; } = [];
    
    // SQL-specific fields
    public int? SqlErrorNumber { get; init; }
    public int? SqlErrorState { get; init; }
    public int? SqlErrorClass { get; init; }
    public string? SqlErrorMessage { get; init; }
}

public sealed record InnerExceptionInfo
{
    public required string ExceptionType { get; init; }
    public required string Message { get; init; }
}
