var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddOptions<ManagedIdentityCatalog.Options.SqlOptions>()
    .Bind(builder.Configuration.GetSection(ManagedIdentityCatalog.Options.SqlOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Server), "Sql:Server is required")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Database), "Sql:Database is required");

builder.Services.AddSingleton<ManagedIdentityCatalog.Services.SqlConnectionFactory>();
builder.Services.AddSingleton<ManagedIdentityCatalog.Services.CatalogRepository>();
builder.Services.AddSingleton<ManagedIdentityCatalog.Services.IdentityModeProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
