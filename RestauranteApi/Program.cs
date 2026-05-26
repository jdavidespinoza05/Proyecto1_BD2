/*
Este archivo contiene la configuración principal de arranque de la API del
sistema de restaurantes.

Aquí se registran los servicios que necesita la aplicación para funcionar,
como los controladores, Swagger, autenticación con JWT, autorización, conexión
a bases de datos, Redis, Elasticsearch y los repositorios utilizados por la API.

También se configura la integración con Keycloak. La API recibe tokens JWT y
extrae los roles definidos en Keycloak para convertirlos en claims que .NET
pueda interpretar correctamente con [Authorize]. Esto permite proteger endpoints
según los permisos o roles del usuario autenticado.

Además, se configura Swagger para que permita probar endpoints protegidos usando
tokens Bearer. Esto facilita las pruebas durante el desarrollo, ya que se puede
ingresar el token directamente desde la interfaz de Swagger.

El archivo también incluye la configuración de Redis como sistema de caché y
Elasticsearch como servicio de búsqueda. Redis se utiliza para mejorar el
rendimiento en operaciones frecuentes, mientras que Elasticsearch permite
realizar búsquedas más rápidas y avanzadas dentro del sistema.

Una parte importante de este archivo es el switch de bases de datos. Dependiendo
del valor configurado en DatabaseEngine, la aplicación puede trabajar con
PostgreSQL o con MongoDB. Si se usa PostgreSQL, se registra AppDbContext con
Entity Framework y se cargan los repositorios correspondientes. Si se usa
MongoDB, se registra el cliente de Mongo y se utilizan los repositorios
implementados para esa base documental.

Finalmente, durante el arranque de la aplicación, si el motor seleccionado es
PostgreSQL, se ejecutan automáticamente las migraciones pendientes para crear o
actualizar las tablas necesarias. Después se habilitan los middlewares de
autenticación y autorización, se mapean los controladores y se activa Swagger
cuando el sistema se ejecuta en ambiente de desarrollo.
*/

using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using RestaurantesApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using RestaurantesApi.Repositories; 
using Elastic.Clients.Elasticsearch;
using RestaurantesApi.Services;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer (Ejemplo: 'Bearer 12345abcdef')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience = builder.Configuration["Jwt:Audience"];
        options.RequireHttpsMetadata = false; 

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, 
            ValidateAudience = false,
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role 
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                if (claimsIdentity == null) return Task.CompletedTask;

                var realmAccessClaim = context.Principal?.FindFirst("realm_access")?.Value;

                if (!string.IsNullOrEmpty(realmAccessClaim))
                {
                    try 
                    {
                        using var doc = JsonDocument.Parse(realmAccessClaim);
                        if (doc.RootElement.TryGetProperty("roles", out var roles))
                        {
                            foreach (var role in roles.EnumerateArray())
                            {
                                var roleName = role.GetString();
                                if (!string.IsNullOrEmpty(roleName))
                                {
                                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                }
                            }
                        }
                    }
                    catch 
                    { 
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "RestaurantesApi_"; 
});

var elasticUri = builder.Configuration.GetConnectionString("ElasticConnection") ?? "http://localhost:9200";

var settings = new ElasticsearchClientSettings(new Uri(elasticUri))
    .EnableDebugMode(); 

builder.Services.AddSingleton<ElasticsearchClient>(new ElasticsearchClient(settings));
builder.Services.AddScoped<ISearchService, ElasticSearchService>();

var dbEngine = builder.Configuration["DatabaseEngine"] ?? "Postgres";

if (dbEngine.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IMenuRepository, PostgresMenuRepository>();
    builder.Services.AddScoped<IOrderRepository, PostgresOrderRepository>();
    builder.Services.AddScoped<IReservationRepository, PostgresReservationRepository>();
    builder.Services.AddScoped<IRestaurantRepository, PostgresRestaurantRepository>();
    builder.Services.AddScoped<IUsuarioRepository, PostgresUsuarioRepository>();
}
else if (dbEngine.Equals("Mongo", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<MongoDB.Driver.IMongoClient>(sp => 
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("MongoConnection") ?? "mongodb://localhost:27017";
        return new MongoDB.Driver.MongoClient(connectionString);
    });

    builder.Services.AddScoped(sp => 
    {
        var client = sp.GetRequiredService<MongoDB.Driver.IMongoClient>();
        return client.GetDatabase("reservas_db");
    });

    builder.Services.AddScoped<IMenuRepository, MongoMenuRepository>();
    builder.Services.AddScoped<IOrderRepository, MongoOrderRepository>();
    builder.Services.AddScoped<IReservationRepository, MongoReservationRepository>();
    builder.Services.AddScoped<IRestaurantRepository, MongoRestaurantRepository>();
    builder.Services.AddScoped<IUsuarioRepository, MongoUsuarioRepository>();
}

var app = builder.Build();

if (dbEngine.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            context.Database.Migrate();
            Console.WriteLine("--> Tablas de Postgres creadas con éxito");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Error creando tablas: {ex.Message}");
        }
    }
}

app.UseAuthentication(); 
app.UseAuthorization();  

app.MapControllers();

app.MapGet("/debug-auth", (ClaimsPrincipal user) =>
{
    return user.Claims.Select(c => new { c.Type, c.Value });
}).RequireAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}