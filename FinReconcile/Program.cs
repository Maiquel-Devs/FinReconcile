using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using FinReconcile.Data;
using FinReconcile.Services;

var builder = WebApplication.CreateBuilder(args);

// Oculta o header Server: Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// Força cultura pt-BR no container Linux
var defaultCulture = new CultureInfo("pt-BR");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

// Configuração do EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<IReconciliationService, ReconciliationService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Injeção de Security Headers globais
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseRequestLocalization(localizationOptions);

// Migrações automáticas e Seed Data resiliente
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    var retries = 10;
    while (retries > 0)
    {
        try
        {
            context.Database.Migrate();
            DbInitializer.Seed(context);
            logger.LogInformation("SQL Server pronto e migrado com sucesso.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning(ex, "Aguardando SQL Server... Tentativas restantes: {Retries}", retries);
            if (retries == 0) throw;
            Thread.Sleep(3000);
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Transactions}/{action=Index}/{id?}");

app.Run();