using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;

sealed record Options(
	string Server,
	string Database,
	string RoleName,
	string SchemaName,
	string? SystemAssignedPrincipalName,
	string? UserAssignedPrincipalName);

static class Program
{
	private static readonly string[] SqlScopes = ["https://database.windows.net//.default"];

	public static async Task<int> Main(string[] args)
	{
		try
		{
			var options = ParseArgs(args);
			await RunAsync(options);
			Console.WriteLine("SQL bootstrap completed.");
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
			return 1;
		}
	}

	private static Options ParseArgs(string[] args)
	{
		// Very small parser: --key value
		var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		for (var i = 0; i < args.Length; i++)
		{
			var key = args[i];
			if (!key.StartsWith("--", StringComparison.Ordinal))
			{
				continue;
			}

			if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
			{
				map[key[2..]] = args[i + 1];
				i++;
			}
			else
			{
				map[key[2..]] = "";
			}
		}

		string Require(string name)
		{
			if (!map.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
			{
				throw new ArgumentException($"Missing required argument: --{name}");
			}
			return value;
		}

		map.TryGetValue("systemAssigned", out var systemAssigned);
		map.TryGetValue("userAssigned", out var userAssigned);

		return new Options(
			Server: Require("server"),
			Database: Require("database"),
			RoleName: map.TryGetValue("role", out var role) && !string.IsNullOrWhiteSpace(role) ? role : "catalog_reader",
			SchemaName: map.TryGetValue("schema", out var schema) && !string.IsNullOrWhiteSpace(schema) ? schema : "SalesLT",
			SystemAssignedPrincipalName: string.IsNullOrWhiteSpace(systemAssigned) ? null : systemAssigned,
			UserAssignedPrincipalName: string.IsNullOrWhiteSpace(userAssigned) ? null : userAssigned);
	}

	private static async Task RunAsync(Options options)
	{
		var credential = new AzureCliCredential();
		var token = await credential.GetTokenAsync(new TokenRequestContext(SqlScopes));

		var connectionString = new SqlConnectionStringBuilder
		{
			DataSource = options.Server,
			InitialCatalog = options.Database,
			Encrypt = true,
			TrustServerCertificate = false,
			ConnectTimeout = 30,
		}.ConnectionString;

		await using var connection = new SqlConnection(connectionString)
		{
			AccessToken = token.Token
		};

		await connection.OpenAsync();

		await EnsureRoleAsync(connection, options.RoleName);
		await GrantSelectOnSchemaAsync(connection, options.SchemaName, options.RoleName);

		if (!string.IsNullOrWhiteSpace(options.SystemAssignedPrincipalName))
		{
			await EnsureExternalUserAsync(connection, options.SystemAssignedPrincipalName);
			await EnsureRoleMemberAsync(connection, options.RoleName, options.SystemAssignedPrincipalName);
		}

		if (!string.IsNullOrWhiteSpace(options.UserAssignedPrincipalName))
		{
			await EnsureExternalUserAsync(connection, options.UserAssignedPrincipalName);
			await EnsureRoleMemberAsync(connection, options.RoleName, options.UserAssignedPrincipalName);
		}
	}

	private static async Task EnsureRoleAsync(SqlConnection connection, string roleName)
	{
		const string sql = """
			if not exists (
				select 1
				from sys.database_principals
				where name = @roleName and type = 'R'
			)
			begin
				declare @stmt nvarchar(max) = N'create role ' + quotename(@roleName) + N';';
				exec (@stmt);
			end
			""";

		await using var cmd = new SqlCommand(sql, connection);
		cmd.Parameters.AddWithValue("@roleName", roleName);
		await cmd.ExecuteNonQueryAsync();
	}

	private static async Task GrantSelectOnSchemaAsync(SqlConnection connection, string schemaName, string roleName)
	{
		const string sql = """
			declare @stmt nvarchar(max) =
				N'grant select on schema::' + quotename(@schemaName) + N' to ' + quotename(@roleName) + N';';
			exec (@stmt);
			""";

		await using var cmd = new SqlCommand(sql, connection);
		cmd.Parameters.AddWithValue("@schemaName", schemaName);
		cmd.Parameters.AddWithValue("@roleName", roleName);
		await cmd.ExecuteNonQueryAsync();
	}

	private static async Task EnsureExternalUserAsync(SqlConnection connection, string principalName)
	{
		const string sql = """
			if not exists (
				select 1
				from sys.database_principals
				where name = @principalName
			)
			begin
				declare @stmt nvarchar(max) = N'create user ' + quotename(@principalName) + N' from external provider;';
				exec (@stmt);
			end
			""";

		await using var cmd = new SqlCommand(sql, connection);
		cmd.Parameters.AddWithValue("@principalName", principalName);
		await cmd.ExecuteNonQueryAsync();
	}

	private static async Task EnsureRoleMemberAsync(SqlConnection connection, string roleName, string principalName)
	{
		const string sql = """
			declare @roleId int = database_principal_id(@roleName);
			declare @memberId int = database_principal_id(@principalName);

			if @roleId is null or @memberId is null
				return;

			if not exists (
				select 1
				from sys.database_role_members
				where role_principal_id = @roleId
				  and member_principal_id = @memberId
			)
			begin
				declare @stmt nvarchar(max) = N'alter role ' + quotename(@roleName) + N' add member ' + quotename(@principalName) + N';';
				exec (@stmt);
			end
			""";

		await using var cmd = new SqlCommand(sql, connection);
		cmd.Parameters.AddWithValue("@roleName", roleName);
		cmd.Parameters.AddWithValue("@principalName", principalName);
		await cmd.ExecuteNonQueryAsync();
	}
}
