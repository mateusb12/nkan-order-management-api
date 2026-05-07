using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.OpenApi;
using OrderManagement.Shared.Data;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderManagement.Shared.OpenApi;

public sealed class DatabaseSchemaDocumentFilter(IServiceScopeFactory scopeFactory) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Database>();

        swaggerDoc.Components ??= new OpenApiComponents();
        swaggerDoc.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        foreach (var entityType in db.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();

            if (string.IsNullOrWhiteSpace(tableName))
            {
                continue;
            }

            var schemaName = entityType.GetSchema();
            var storeObject = StoreObjectIdentifier.Table(tableName, schemaName);
            var openApiSchemaName = $"DatabaseTable_{SanitizeSchemaName(tableName)}";

            var primaryKeyProperties = entityType.FindPrimaryKey()
                ?.Properties
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? [];

            var foreignKeyProperties = entityType
                .GetForeignKeys()
                .SelectMany(fk => fk.Properties.Select(property => new
                {
                    PropertyName = property.Name,
                    PrincipalTable = fk.PrincipalEntityType.GetTableName(),
                    PrincipalColumns = fk.PrincipalKey.Properties
                        .Select(p => p.Name)
                        .ToArray()
                }))
                .GroupBy(x => x.PropertyName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase);

            var tableSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = false,
                Description = BuildTableDescription(entityType, tableName, schemaName),
                Properties = new Dictionary<string, IOpenApiSchema>(),
                Required = new HashSet<string>()
            };

            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject) ?? property.Name;
                var columnSchema = BuildColumnSchema(property, storeObject);

                var descriptionParts = new List<string>
                {
                    $"SQL: {property.GetColumnType(storeObject)}",
                    $"CLR: {GetFriendlyTypeName(property.ClrType)}",
                    property.IsNullable ? "nullable" : "not null"
                };

                if (primaryKeyProperties.Contains(property.Name))
                {
                    descriptionParts.Add("PK");
                }

                if (foreignKeyProperties.TryGetValue(property.Name, out var foreignKey))
                {
                    descriptionParts.Add(
                        $"FK -> {foreignKey.PrincipalTable}({string.Join(", ", foreignKey.PrincipalColumns)})");
                }

                var maxLength = property.GetMaxLength();
                if (maxLength is not null)
                {
                    descriptionParts.Add($"max length: {maxLength}");
                }

                var precision = property.GetPrecision();
                var scale = property.GetScale();

                if (precision is not null)
                {
                    descriptionParts.Add(scale is null
                        ? $"precision: {precision}"
                        : $"precision: {precision}, scale: {scale}");
                }

                columnSchema.Description = string.Join(" | ", descriptionParts);

                tableSchema.Properties[columnName] = columnSchema;

                if (!property.IsNullable)
                {
                    tableSchema.Required.Add(columnName);
                }
            }

            swaggerDoc.Components.Schemas[openApiSchemaName] = tableSchema;
        }
    }

    private static string BuildTableDescription(
        IEntityType entityType,
        string tableName,
        string? schemaName)
    {
        var fullTableName = string.IsNullOrWhiteSpace(schemaName)
            ? tableName
            : $"{schemaName}.{tableName}";

        return $"Tabela SQL efetiva no banco {fullTableName} | Entidade: {entityType.ClrType.FullName}";
    }

    private static OpenApiSchema BuildColumnSchema(
        IProperty property,
        StoreObjectIdentifier storeObject)
    {
        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        var sqlType = property.GetColumnType(storeObject).ToLowerInvariant();

        if (clrType.IsEnum)
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Format = "int32"
            };
        }

        if (clrType == typeof(string))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String };
        }

        if (clrType == typeof(int))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };
        }

        if (clrType == typeof(long))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" };
        }

        if (clrType == typeof(short))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };
        }

        if (clrType == typeof(decimal))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Number, Format = "decimal" };
        }

        if (clrType == typeof(double))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" };
        }

        if (clrType == typeof(float))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Number, Format = "float" };
        }

        if (clrType == typeof(bool))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Boolean };
        }

        if (clrType == typeof(DateTime) || clrType == typeof(DateTimeOffset))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
        }

        if (clrType == typeof(Guid))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
        }

        if (clrType == typeof(byte[]) || sqlType.Contains("binary"))
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "byte" };
        }

        return new OpenApiSchema { Type = JsonSchemaType.String };
    }

    private static string GetFriendlyTypeName(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);

        if (underlyingType is not null)
        {
            return $"{underlyingType.Name}?";
        }

        return type.Name;
    }

    private static string SanitizeSchemaName(string value)
    {
        var chars = value
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray();

        return new string(chars);
    }
}
