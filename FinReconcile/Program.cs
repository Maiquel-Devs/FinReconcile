using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using FinReconcile.Data;
using FinReconcile.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração de Servidor Web e Injeção de Dependências
ConfigureWebHost(builder.WebHost);
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// 2. Middlewares de Segurança e Localização
app.UseSecurityHeaders();
app.UseRequestLocalization(GetLocalizationOptions());

// 3. Inicialização Resiliente do Banco de Dados (EF Core Migrations & Seed)
await app.ApplyDatabaseMigrationsAsync();

if (!app.Environment.IsDevelopment())
{
    // Força transporte estrito HTTPS em ambientes de produção
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
    // Hardening: Oculta o header 'Server: Kestrel' para não divulgar a tecnologia do servidor
    webHost.ConfigureKestrel(options => options.AddServerHeader = false);
}

void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Configuração do EF Core com estratégia de retentativas para resiliência de rede
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null)));

    // Injeção de Dependências (DI) dos serviços de conciliação e controllers
    services.AddScoped<IReconciliationService, ReconciliationService>();
    services.AddControllersWithViews();
}

RequestLocalizationOptions GetLocalizationOptions()
{
    // Padroniza a cultura monetária e de data para o padrão brasileiro (R$ e dd/MM/yyyy)
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
    /// Injeta cabeçalhos HTTP globais para proteção contra Clickjacking, MIME-sniffing, XSS e vazamento de dados.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            // Impede renderização da aplicação dentro de iframes externos (Mitigação de Clickjacking)
            context.Response.Headers.Append("X-Frame-Options", "DENY");

            // Impede o navegador de tentar adivinhar o MIME type dos arquivos (Mitigação de MIME Sniffing)
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // Controla a quantidade de informações de rota enviadas no cabeçalho Referer
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Desativa permissões de hardware desnecessárias para a plataforma
            context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

            // Content Security Policy (CSP): Restringe a execução de scripts e carregamento de fontes apenas às origens confiáveis do projeto
            var cspPolicy = "default-src 'self'; " +
                            "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
                            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
                            "img-src 'self' data:; " +
                            "script-src 'self';";

            context.Response.Headers.Append("Content-Security-Policy", cspPolicy);

            await next();
        });
    }

    /// <summary>
    /// Executa as migrações pendentes e insere dados iniciais (Seed) de forma resiliente.
    /// Projetado para aguardar a inicialização completa do container do SQL Server no Docker.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        const int maxRetries = 10;
        var retries = maxRetries;
        
        while (retries > 0)
        {
            try
            {
                await context.Database.MigrateAsync();
                DbInitializer.Seed(context);
                
                logger.LogInformation("SQL Server conectado e migrado com sucesso.");
                break;
            }
            catch (Exception ex)
            {
                retries--;
                logger.LogWarning(ex, "Aguardando inicialização do SQL Server... Tentativas restantes: {Retries}", retries);
                
                if (retries == 0)
                {
                    logger.LogCritical("Não foi possível conectar ao SQL Server após {MaxRetries} tentativas.", maxRetries);
                    throw;
                }
                
                // Aguarda de forma não-bloqueante antes da próxima tentativa
                await Task.Delay(3000);
            }
        }
    }
}

#endregion