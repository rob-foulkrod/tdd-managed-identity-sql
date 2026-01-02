using ManagedIdentityCatalog.Models;
using ManagedIdentityCatalog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManagedIdentityCatalog.Pages.Products;

public sealed class IndexModel : PageModel
{
    private readonly CatalogRepository _repository;

    public IndexModel(CatalogRepository repository)
    {
        _repository = repository;
    }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Q { get; set; }

    public int Take { get; } = 50;

    public List<ProductCategoryDto> Categories { get; private set; } = [];

    public List<ProductDto> Products { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGet(CancellationToken cancellationToken)
    {
        try
        {
            Categories = (await _repository.GetCategoriesAsync(cancellationToken)).ToList();
            Products = (await _repository.GetProductsAsync(CategoryId, Q, Take, cancellationToken)).ToList();
        }
        catch (Exception ex)
        {
            // Keep errors trainer-friendly: show a short hint instead of a stack trace.
            ErrorMessage = "Unable to query Azure SQL. If this is the first run, make sure the database has a user for the app's Managed Identity and that Azure AD admin is configured on the SQL server.";

            // Still log full exception for troubleshooting.
            HttpContext.RequestServices
                .GetRequiredService<ILogger<IndexModel>>()
                .LogError(ex, "Failed to load products");
        }
    }
}
