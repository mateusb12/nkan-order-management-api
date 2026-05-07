using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Features.Messaging;
using OrderManagement.Features.Orders.Services;
using OrderManagement.Shared.Data;
using OrderManagement.Shared.Middleware;
using OrderManagement.Shared.OpenApi;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddScoped<OrderService>();

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();

builder.Services.AddDbContext<Database>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.DocumentFilter<DatabaseSchemaDocumentFilter>();
});
builder.Services.AddOpenApi();

var app = builder.Build();

await ApplyDatabaseMigrationsAsync(app);

app.MapOpenApi();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();

app.MapGet("/health", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");

    await using var connection = new SqlConnection(connectionString);

    await connection.OpenAsync();

    await using var command = new SqlCommand("SELECT @@VERSION", connection);

    var result = await command.ExecuteScalarAsync();

    return Results.Ok(new
    {
        Connected = true,
        SqlServerVersion = result?.ToString()
    });
});

app.Run();

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<Database>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseMigration");

    const int maxRetries = 10;

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            return;
        }
        catch (SqlException ex) when (attempt < maxRetries)
        {
            logger.LogWarning(
                ex,
                "SQL Server is not ready yet. Retrying migration attempt {Attempt}/{MaxRetries}...",
                attempt,
                maxRetries);

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
