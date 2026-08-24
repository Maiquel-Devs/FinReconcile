using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using FinReconcile.Data;
using FinReconcile.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração de Serviços e Hospedagem
ConfigureWebHost(builder.WebHost);
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// 2. Middlewares e Pipeline de Requisição HTTP
app.UseSecurityHeaders();
app.UseRequestLocalization(GetLocalizationOptions());

// 3. Inicialização e Migração do Banco de Dados
await app.ApplyDatabaseMigrationsAsync();

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


#region Local Configuration Methods

void ConfigureWebHost(ConfigureWebHostBuilder webHost)
{
    // Oculta o header Server: Kestrel por segurança
    webHost.ConfigureKestrel(options => options.AddServerHeader = false);
}

void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Configuração do EF Core com resiliência de conexão
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null)));

    // Injeção de Dependências (DI)
    services.AddScoped<IReconciliationService, ReconciliationService>();
    services.AddControllersWithViews();
}

RequestLocalizationOptions GetLocalizationOptions()
{
    // Força cultura pt-BR no container Linux
    var defaultCulture = new CultureInfo("pt-BR");
    
    return new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture(defaultCulture),
        SupportedCultures = [defaultCulture],
        SupportedUICultures = [defaultCulture]
    };
}

#endregion

#region Extension Methods

public static class WebApplicationExtensions
{
    /// <summary>
    /// Adiciona headers de segurança globais nas respostas HTTP.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            await next();
        });
    }

    /// <summary>
    /// Executa as migrações pendentes e insere dados iniciais (Seed) de forma resiliente.
    /// Especialmente útil para ambientes Docker onde o SQL Server pode demorar a iniciar.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        var retries = 10;
        
        while (retries > 0)
        {
            try
            {
                await context.Database.MigrateAsync();
                DbInitializer.Seed(context);
                
                logger.LogInformation("SQL Server pronto e migrado com sucesso.");
                break;
            }
            catch (Exception ex)
            {
                retries--;
                logger.LogWarning(ex, "Aguardando inicialização do SQL Server... Tentativas restantes: {Retries}", retries);
                
                if (retries == 0) throw;
                
                // Melhoria: Usando Task.Delay em vez de Thread.Sleep para não bloquear a thread inicial
                await Task.Delay(3000);
            }
        }
    }
}

#endregion