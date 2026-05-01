using Halto.Api.Extensions;
using Halto.Api.Middleware;
using Halto.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["Azure:ApplicationInsights:ConnectionString"];
});

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Halto.Api")
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(
        builder.Configuration["Azure:ApplicationInsights:ConnectionString"],
        TelemetryConverter.Traces,
        restrictedToMinimumLevel: LogEventLevel.Warning)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSwagger();

// Read allowed origins from config or env var (comma-separated)
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();

builder.Services.AddCors(opt =>
{
    // "Open" policy for local dev and Swagger testing
    opt.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

    // Restrictive policy for production — only the known frontend origin
    opt.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        else
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); // fallback
    });
});

// Only force the local URL when not running in Azure App Service
if (string.IsNullOrEmpty(builder.Configuration["WEBSITE_INSTANCE_ID"]))
{
    builder.WebHost.UseUrls("http://localhost:5005");
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HaltoDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database schema...");
        await DbSchemaInitializer.EnsureSchemaAsync(db, logger);

        logger.LogInformation("Seeding database...");
        await DbSeeder.SeedAsync(db, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed.");
    }
}

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Halto API v1");
    c.RoutePrefix = "swagger";
});

// Use the restrictive policy in production, open during development
var corsPolicy = app.Environment.IsDevelopment() ? "AllowAll" : "AllowFrontend";
app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Log.Information("Halto API started");

app.Run();
