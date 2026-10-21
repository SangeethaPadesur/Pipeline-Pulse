using Microsoft.EntityFrameworkCore;
using PipelinePulse.Data;
using PipelinePulse.Repositories;
using PipelinePulse.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
builder.Services.AddDbContext<PipelineDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<IHealthRepository, HealthRepository>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<IPipelineRepository, PipelineRepository>();
builder.Services.AddScoped<IPipelineService, PipelineService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Read Swagger settings from configuration
var swaggerSection = builder.Configuration.GetSection("Swagger");
var swaggerEnabled = swaggerSection.GetValue<bool>("Enabled", true);
var swaggerOnlyInDevelopment = swaggerSection.GetValue<bool>("OnlyInDevelopment", true);
var swaggerRoutePrefix = swaggerSection.GetValue<string>("RoutePrefix", "swagger");
var swaggerEndpoint = swaggerSection.GetValue<string>("Endpoint", "/openapi/v1.json");
var swaggerTitle = swaggerSection.GetValue<string>("Title", "PipelinePulse API v1");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PipelineDbContext>();
    await dbContext.Database.MigrateAsync();
    await PipelineDbSeeder.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (swaggerEnabled && (!swaggerOnlyInDevelopment || app.Environment.IsDevelopment()))
{
    // Add the generated OpenAPI document and connect Swagger UI to it
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(swaggerEndpoint, swaggerTitle);
        options.RoutePrefix = swaggerRoutePrefix ?? "swagger"; // Access it via e.g. http://localhost:5084/swagger
    });
}

app.UseAuthorization();

app.MapControllers();

app.Run();
