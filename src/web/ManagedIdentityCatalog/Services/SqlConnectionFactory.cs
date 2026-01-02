using Azure.Core;
using Azure.Identity;
using ManagedIdentityCatalog.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ManagedIdentityCatalog.Services;

public sealed class SqlConnectionFactory
{
    private static readonly string[] SqlScopes = ["https://database.windows.net//.default"];

    private readonly SqlOptions _sqlOptions;
    private readonly IConfiguration _configuration;

    public SqlConnectionFactory(IOptions<SqlOptions> sqlOptions, IConfiguration configuration)
    {
        _sqlOptions = sqlOptions.Value;
        _configuration = configuration;
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var userAssignedClientId = _configuration["ManagedIdentity:UserAssignedClientId"];

        if (!string.IsNullOrWhiteSpace(userAssignedClientId))
        {
            return await CreateOpenConnectionUsingUserAssignedManagedIdentityAsync(userAssignedClientId, cancellationToken);
        }

        return await CreateOpenConnectionUsingSystemAssignedManagedIdentityAsync(cancellationToken);
    }

    // System-assigned Managed Identity (default demo path)
    private async Task<SqlConnection> CreateOpenConnectionUsingSystemAssignedManagedIdentityAsync(CancellationToken cancellationToken)
    {
        var credential = new ManagedIdentityCredential();
        return await CreateOpenConnectionAsync(credential, cancellationToken);
    }

    // User-assigned Managed Identity (instructor conversion path)
    private async Task<SqlConnection> CreateOpenConnectionUsingUserAssignedManagedIdentityAsync(string clientId, CancellationToken cancellationToken)
    {
        //When using a user-assigned managed identity, we need to specify the client ID
        //because there may be multiple identities assigned to the resource.
        
        var credential = new ManagedIdentityCredential(clientId);
        return await CreateOpenConnectionAsync(credential, cancellationToken);
    }

    private async Task<SqlConnection> CreateOpenConnectionAsync(TokenCredential credential, CancellationToken cancellationToken)
    {
        var token = await credential.GetTokenAsync(new TokenRequestContext(SqlScopes), cancellationToken);

        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = _sqlOptions.Server,
            InitialCatalog = _sqlOptions.Database,
            Encrypt = true,
            TrustServerCertificate = false,
            ConnectTimeout = 30,
        }.ConnectionString;

        var connection = new SqlConnection(connectionString)
        {
            AccessToken = token.Token
        };

        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
