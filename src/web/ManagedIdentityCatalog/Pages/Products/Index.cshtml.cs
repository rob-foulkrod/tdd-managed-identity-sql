using ManagedIdentityCatalog.Models;
using ManagedIdentityCatalog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace ManagedIdentityCatalog.Pages.Products;

public sealed class IndexModel : PageModel
{
    private readonly CatalogRepository _repository;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(CatalogRepository repository, IWebHostEnvironment environment)
    {
        _repository = repository;
        _environment = environment;
    }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? PriceMin { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? PriceMax { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Color { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Size { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "name";

    [BindProperty(SupportsGet = true)]
    public bool ShowDiscontinued { get; set; } = false;

    [BindProperty(SupportsGet = true)]
    public bool ShowAdvanced { get; set; } = false;

    [BindProperty(SupportsGet = true)]
    public QueryMode Mode { get; set; } = QueryMode.Standard;

    public int Take { get; } = 50;

    public List<ProductCategoryDto> Categories { get; private set; } = [];

    public List<ProductDto> Products { get; private set; } = [];
    
    public List<ProductEnrichedDto> ProductsEnriched { get; private set; } = [];
    
    public List<BestSellerDto> BestSellers { get; private set; } = [];
    
    public List<PriceAnalysisDto> PriceAnalysis { get; private set; } = [];
    
    public List<CategoryHierarchyDto> CategoryHierarchy { get; private set; } = [];
    
    public List<RecentActivityDto> RecentActivity { get; private set; } = [];
    
    public List<string> AvailableColors { get; private set; } = [];
    
    public List<string> AvailableSizes { get; private set; } = [];

    public string? ErrorMessage { get; private set; }
    
    public ErrorDetails? ErrorDetailsData { get; private set; }

    public async Task OnGet(CancellationToken cancellationToken)
    {
        Exception? capturedError = null;
        
        // Always load categories, colors, sizes for filters (except in specialized modes)
        if (Mode == QueryMode.Standard)
        {
            try
            {
                Categories = (await _repository.GetCategoriesAsync(cancellationToken)).ToList();
            }
            catch (Exception ex)
            {
                capturedError = ex;
            }
            
            try
            {
                AvailableColors = (await _repository.GetDistinctColorsAsync(cancellationToken)).ToList();
            }
            catch (Exception ex)
            {
                capturedError ??= ex;
            }
            
            try
            {
                AvailableSizes = (await _repository.GetDistinctSizesAsync(cancellationToken)).ToList();
            }
            catch (Exception ex)
            {
                capturedError ??= ex;
            }
        }
        
        // Load data based on active mode
        try
        {
            switch (Mode)
            {
                case QueryMode.BestSellers:
                    BestSellers = (await _repository.GetBestSellersAsync(Take, cancellationToken)).ToList();
                    break;
                    
                case QueryMode.PriceAnalysis:
                    PriceAnalysis = (await _repository.GetPriceAnalysisAsync(cancellationToken)).ToList();
                    break;
                    
                case QueryMode.CategoryHierarchy:
                    CategoryHierarchy = (await _repository.GetCategoryHierarchyAsync(cancellationToken)).ToList();
                    break;
                    
                case QueryMode.RecentActivity:
                    RecentActivity = (await _repository.GetRecentActivityAsync(50, cancellationToken)).ToList();
                    break;
                    
                case QueryMode.Standard:
                default:
                    if (ShowAdvanced)
                    {
                        ProductsEnriched = (await _repository.GetProductsEnrichedAsync(
                            CategoryId, Q, PriceMin, PriceMax, Color, Size, SortBy, ShowDiscontinued, Take, cancellationToken)).ToList();
                    }
                    else
                    {
                        Products = (await _repository.GetProductsAsync(
                            CategoryId, Q, PriceMin, PriceMax, Color, Size, SortBy, ShowDiscontinued, Take, cancellationToken)).ToList();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            capturedError ??= ex;
        }
        
        if (capturedError != null)
        {
            // Keep errors trainer-friendly: show a short hint instead of a stack trace.
            ErrorMessage = "Unable to query Azure SQL. If this is the first run, make sure the database has a user for the app's Managed Identity and that Azure AD admin is configured on the SQL server.";

            // In Development environment, capture detailed error info for demo/debugging
            if (_environment.IsDevelopment())
            {
                ErrorDetailsData = BuildErrorDetails(capturedError);
            }

            // Still log full exception for troubleshooting.
            HttpContext.RequestServices
                .GetRequiredService<ILogger<IndexModel>>()
                .LogError(capturedError, "Failed to load products");
        }
    }

    private static ErrorDetails BuildErrorDetails(Exception ex)
    {
        var innerExceptions = new List<InnerExceptionInfo>();
        var innerEx = ex.InnerException;
        while (innerEx != null)
        {
            innerExceptions.Add(new InnerExceptionInfo
            {
                ExceptionType = innerEx.GetType().FullName ?? innerEx.GetType().Name,
                Message = innerEx.Message
            });
            innerEx = innerEx.InnerException;
        }

        var details = new ErrorDetails
        {
            ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            InnerExceptions = innerExceptions
        };

        // Special handling for SQL exceptions
        if (ex is SqlException sqlEx && sqlEx.Errors.Count > 0)
        {
            var firstError = sqlEx.Errors[0];
            details = new ErrorDetails
            {
                ExceptionType = details.ExceptionType,
                Message = details.Message,
                StackTrace = details.StackTrace,
                InnerExceptions = details.InnerExceptions,
                SqlErrorNumber = firstError.Number,
                SqlErrorState = firstError.State,
                SqlErrorClass = firstError.Class,
                SqlErrorMessage = firstError.Message
            };
        }

        return details;
    }
}
