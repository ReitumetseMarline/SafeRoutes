using SafeRoute.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<CrimeDataService>();
builder.Services.AddScoped<RouteService>();

// Named HttpClient with User-Agent required by Nominatim ToS
builder.Services.AddHttpClient("maps", c =>
{
    c.DefaultRequestHeaders.Add("User-Agent", "SafeRoute/1.0 (AI for Good Challenge)");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
