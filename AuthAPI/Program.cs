using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthService.Infrastructure.Data;
using AuthService.Application.Mappings;
using System.Text.Json.Serialization; // Para manejar las referencias cíclicas en JSON

var builder = WebApplication.CreateBuilder(args);

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        builder => builder
            .WithOrigins("http://localhost:8087", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});
;

// Configurar DbContext y conexión a SQL Server
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthServiceDb"),
    sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("AuthService.Infrastructure");
        sqlOptions.EnableRetryOnFailure(); // Habilita reintentos automáticos en caso de error transitorio
    }));

// Configurar AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// **Registrar HttpClient**
builder.Services.AddHttpClient(); // Agrega HttpClient para IHttpClientFactory

// Configurar JWT Authentication (cargado desde appsettings.json)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], // Tomado desde appsettings.json
        ValidAudience = builder.Configuration["Jwt:Audience"], // Tomado desde appsettings.json
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])), // Clave secreta desde appsettings.json
        ClockSkew = TimeSpan.Zero // Eliminar la diferencia de tiempo por defecto
    };
});

// Configurar Swagger para la documentación de la API sin requerir autenticación
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuthService API",
        Version = "v1",
        Description = "API para la gestión de autenticación, roles y permisos.",
        Contact = new OpenApiContact
        {
            Name = "Tu Nombre",
            Email = "tuemail@correo.com",
            Url = new Uri("http://localhost:8087/api/AuthAPI")
        }
    });
});

// Configurar controladores y habilitar el manejo de referencias cíclicas, evitar $values y usar camelCase
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Evitar ciclos de referencia
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

    // Evitar que se incluyan campos como $values en colecciones
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.WriteIndented = true; // Opcional, para mejor legibilidad
});

var app = builder.Build();


app.UseCors("AllowLocalhost");

// Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    try
    {
        dbContext.Database.Migrate();
        Console.WriteLine("Migraciones aplicadas con éxito.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al aplicar migraciones: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthService API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
