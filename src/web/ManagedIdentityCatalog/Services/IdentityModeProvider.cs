using Microsoft.Extensions.Configuration;

namespace ManagedIdentityCatalog.Services;

public sealed class IdentityModeProvider
{
    private readonly IConfiguration _configuration;

    public IdentityModeProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetModeLabel()
    {
        var userAssignedClientId = _configuration["ManagedIdentity:UserAssignedClientId"];
        return string.IsNullOrWhiteSpace(userAssignedClientId)
            ? "System-assigned"
            : "User-assigned";
    }
}
