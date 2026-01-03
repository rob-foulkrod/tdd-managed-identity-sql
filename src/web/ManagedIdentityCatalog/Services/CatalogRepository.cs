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

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        int? categoryId, 
        string? search, 
        decimal? priceMin,
        decimal? priceMax,
        string? color,
        string? size,
        string sortBy,
        bool showDiscontinued,
        int take, 
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // Build ORDER BY clause based on sortBy parameter
        var orderByClause = sortBy switch
        {
            "price-asc" => "p.ListPrice ASC, p.Name",
            "price-desc" => "p.ListPrice DESC, p.Name",
            "newest" => "p.SellStartDate DESC, p.Name",
            _ => "p.Name"
        };

        var sql = $"""
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
              and ( @priceMin is null or p.ListPrice >= @priceMin )
              and ( @priceMax is null or p.ListPrice <= @priceMax )
              and ( @color is null or p.Color = @color )
              and ( @size is null or p.Size = @size )
              and ( @showDiscontinued = 1 or p.DiscontinuedDate is null )
            order by {orderByClause};
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@take", take);
        cmd.Parameters.AddWithValue("@categoryId", categoryId.HasValue ? categoryId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : search);
        cmd.Parameters.AddWithValue("@priceMin", priceMin.HasValue ? priceMin.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@priceMax", priceMax.HasValue ? priceMax.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@color", string.IsNullOrWhiteSpace(color) ? DBNull.Value : color);
        cmd.Parameters.AddWithValue("@size", string.IsNullOrWhiteSpace(size) ? DBNull.Value : size);
        cmd.Parameters.AddWithValue("@showDiscontinued", showDiscontinued ? 1 : 0);

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

    public async Task<IReadOnlyList<ProductEnrichedDto>> GetProductsEnrichedAsync(
        int? categoryId, 
        string? search, 
        decimal? priceMin,
        decimal? priceMax,
        string? color,
        string? size,
        string sortBy,
        bool showDiscontinued,
        int take, 
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // Build ORDER BY clause based on sortBy parameter
        var orderByClause = sortBy switch
        {
            "price-asc" => "p.ListPrice ASC, p.Name",
            "price-desc" => "p.ListPrice DESC, p.Name",
            "newest" => "p.SellStartDate DESC, p.Name",
            "bestseller" => "ISNULL(sales.TotalQty, 0) DESC, p.Name",
            _ => "p.Name"
        };

        var sql = $"""
            select top (@take)
                p.ProductID,
                p.Name,
                p.Color,
                p.Size,
                p.ListPrice,
                p.StandardCost,
                c.Name as CategoryName,
                -- Computed: Profit Margin %
                CASE 
                    WHEN p.ListPrice IS NULL OR p.ListPrice = 0 THEN NULL
                    WHEN p.StandardCost IS NULL THEN NULL
                    ELSE ((p.ListPrice - p.StandardCost) / p.ListPrice * 100)
                END as MarginPercent,
                -- Computed: Lifecycle Status
                CASE 
                    WHEN p.DiscontinuedDate IS NOT NULL THEN 'Discontinued'
                    WHEN p.SellEndDate IS NOT NULL THEN 'Phase Out'
                    ELSE 'Active'
                END as LifecycleStatus,
                -- Computed: Days in catalog
                DATEDIFF(day, p.SellStartDate, GETDATE()) as DaysInCatalog,
                -- Aggregated: Sales data
                ISNULL(sales.TotalQty, 0) as TotalUnitsSold,
                ISNULL(sales.TotalRevenue, 0) as TotalRevenue,
                p.Weight
            from SalesLT.Product p
            left join SalesLT.ProductCategory c on c.ProductCategoryID = p.ProductCategoryID
            left join (
                select ProductID, 
                       SUM(OrderQty) as TotalQty,
                       SUM(OrderQty * UnitPrice) as TotalRevenue
                from SalesLT.SalesOrderDetail
                group by ProductID
            ) sales on sales.ProductID = p.ProductID
            where ( @categoryId is null or p.ProductCategoryID = @categoryId )
              and ( @search is null or p.Name like '%' + @search + '%' )
              and ( @priceMin is null or p.ListPrice >= @priceMin )
              and ( @priceMax is null or p.ListPrice <= @priceMax )
              and ( @color is null or p.Color = @color )
              and ( @size is null or p.Size = @size )
              and ( @showDiscontinued = 1 or p.DiscontinuedDate is null )
            order by {orderByClause};
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@take", take);
        cmd.Parameters.AddWithValue("@categoryId", categoryId.HasValue ? categoryId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : search);
        cmd.Parameters.AddWithValue("@priceMin", priceMin.HasValue ? priceMin.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@priceMax", priceMax.HasValue ? priceMax.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@color", string.IsNullOrWhiteSpace(color) ? DBNull.Value : color);
        cmd.Parameters.AddWithValue("@size", string.IsNullOrWhiteSpace(size) ? DBNull.Value : size);
        cmd.Parameters.AddWithValue("@showDiscontinued", showDiscontinued ? 1 : 0);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var results = new List<ProductEnrichedDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ProductEnrichedDto(
                ProductId: reader.GetInt32(0),
                Name: reader.GetString(1),
                Color: reader.IsDBNull(2) ? null : reader.GetString(2),
                Size: reader.IsDBNull(3) ? null : reader.GetString(3),
                ListPrice: reader.GetDecimal(4),
                StandardCost: reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                CategoryName: reader.IsDBNull(6) ? null : reader.GetString(6),
                MarginPercent: reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                LifecycleStatus: reader.GetString(8),
                DaysInCatalog: reader.GetInt32(9),
                TotalUnitsSold: reader.GetInt32(10),
                TotalRevenue: reader.GetDecimal(11),
                Weight: reader.IsDBNull(12) ? null : reader.GetDecimal(12)));
        }

        return results;
    }

    public async Task<IReadOnlyList<string>> GetDistinctColorsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            select distinct Color
            from SalesLT.Product
            where Color is not null
            order by Color;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var results = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }

    public async Task<IReadOnlyList<string>> GetDistinctSizesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
            select distinct Size
            from SalesLT.Product
            where Size is not null
            order by Size;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var results = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }
}
