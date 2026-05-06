using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

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
})
.WithOpenApi();

app.Run();
