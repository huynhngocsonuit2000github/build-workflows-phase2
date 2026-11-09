using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TaskLite.Api.Data;
using TaskLite.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=tasklite.db";
builder.Services.AddDbContext<TaskLiteDbContext>(options =>
    options.UseSqlite(connectionString));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4300"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .WithMethods("GET", "POST", "PATCH");
    });
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskLiteDbContext>();
    await db.Database.MigrateAsync();

    var shouldSeed = app.Environment.IsDevelopment() ||
                     app.Configuration.GetValue<bool>("Seed:DemoData");
    if (shouldSeed)
    {
        await DevSeeder.SeedAsync(db);
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapProjectEndpoints();
app.MapTaskEndpoints();

app.Run();

public partial class Program;
