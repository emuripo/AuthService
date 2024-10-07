using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using AuthService.Infrastructure.Data;
using AuthService.Application.Mappings;

var builder = WebApplication.CreateBuilder(args);

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000",
        builder => builder.WithOrigins("http://localhost:3000")
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

// Configurar DbContext y conexión a SQL Server
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthServiceDb"),
    sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("AuthService.Infrastructure");
        sqlOptions.EnableRetryOnFailure();
    }));

// Configurar AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Configurar Swagger para la documentación de la API
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

builder.Services.AddControllers();

var app = builder.Build();

// Activar CORS antes de los controladores y otras configuraciones
app.UseCors("AllowLocalhost3000");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    try
    {
        // Aplicar migraciones pendientes para crear o actualizar las tablas
        dbContext.Database.EnsureCreated();
        dbContext.Database.Migrate();
        Console.WriteLine("Migraciones aplicadas con éxito.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al aplicar migraciones: {ex.Message}");
    }
}

// Configurar la app para usar controladores y otras funcionalidades de ASP.NET Core
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthService API v1");
        c.RoutePrefix = string.Empty; // Hacer que Swagger sea la página principal
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
