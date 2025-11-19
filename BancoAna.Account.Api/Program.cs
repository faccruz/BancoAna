using BancoAna.Account.Api.Services;
using BancoAna.Account.Application.Interfaces;
using BancoAna.Account.Application.Services;
using BancoAna.Account.Infrastructure;
using BancoAna.Account.Infrastructure.Persistence;
using BancoAna.Account.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt:Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "bancoana";

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT auth
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;    
})
.AddJwtBearer(options =>
{
    // Garantir chave não nula
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
        throw new InvalidOperationException("Jwt:Key não configurada em appsettings.json");

    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BancoAnaAPI";
    var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

// DI: Db factory (Infrastructure), repository (Infrastructure), jwt service (Api)
var connString = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=bancoana_account.db";
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqliteConnectionFactory(connString));
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<IJwtService, JwtService>();

// ler tarifa como decimal (invariant culture)
var tarifaStr = builder.Configuration["Tarifa:Valor"] ?? "0";
if (!decimal.TryParse(tarifaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var tarifaValor))
{
    tarifaValor = 0m;
}

// registrar TransferService (observação: TransferService espera IAccountRepository + tarifa)
builder.Services.AddScoped<TransferService>(sp =>
{
    var repo = sp.GetRequiredService<BancoAna.Account.Application.Interfaces.IAccountRepository>();
    return new TransferService(repo, tarifaValor);
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "BancoAna.Account.Api", Version = "v1" });

    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Insira o token JWT desta forma: Bearer {token}",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
        }
    };

    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>()   }
    });
});

var app = builder.Build();

// Ensure DB
using (var scope = app.Services.CreateScope())
{
    var dbInit = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    dbInit.EnsureCreated();
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }