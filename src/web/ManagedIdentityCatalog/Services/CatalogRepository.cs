using ManagedIdentityCatalog.Models;
using Microsoft.Data.SqlClient;

namespace ManagedIdentityCatalog.Services;

public sealed class CatalogRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public CatalogRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            select
                ProductCategoryID,
                Name
            from SalesLT.ProductCategory
            order by Name;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var results = new List<ProductCategoryDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ProductCategoryDto(
                ProductCategoryId: reader.GetInt32(0),
                Name: reader.GetString(1)));
        }

        return results;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(int? categoryId, string? search, int take, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // Keep query simple and demo-friendly: filter by category and name prefix/contains.
        var sql = """
            select top (@take)
                p.ProductID,
                p.Name,
                p.Color,
                p.ListPrice,
                c.Name as CategoryName
            from SalesLT.Product p
            left join SalesLT.ProductCategory c on c.ProductCategoryID = p.ProductCategoryID
            where ( @categoryId is null or p.ProductCategoryID = @categoryId )
              and ( @search is null or p.Name like '%' + @search + '%' )
            order by p.Name;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@take", take);
        cmd.Parameters.AddWithValue("@categoryId", categoryId.HasValue ? categoryId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : search);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var results = new List<ProductDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ProductDto(
                ProductId: reader.GetInt32(0),
                Name: reader.GetString(1),
                Color: reader.IsDBNull(2) ? null : reader.GetString(2),
                ListPrice: reader.GetDecimal(3),
                CategoryName: reader.IsDBNull(4) ? null : reader.GetString(4)));
        }

        return results;
    }
}
