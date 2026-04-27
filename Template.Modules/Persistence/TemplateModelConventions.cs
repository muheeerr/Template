using System.Linq.Expressions;
using System.Reflection;
using Template.Modules.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Persistence;

public static class TemplateModelConventions
{
    public static void ConfigureModel(ModelBuilder modelBuilder, bool applyPostgresExtensions)
    {
        if (applyPostgresExtensions)
        {
            modelBuilder.HasPostgresExtension("uuid-ossp");
            modelBuilder.HasPostgresExtension("pgcrypto");
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
            {
                continue;
            }

            var clrType = entityType.ClrType;
            if (clrType is not { IsClass: true } || clrType == typeof(object))
            {
                continue;
            }

            var entity = modelBuilder.Entity(clrType);
            var useTenantScopedQueryFilter = typeof(ITenantScopedEntity).IsAssignableFrom(clrType);

            var idProperty = entityType.FindProperty(nameof(BaseEntity.Id));
            if (idProperty != null
                && typeof(BaseEntity).IsAssignableFrom(clrType)
                && !clrType.IsAbstract)
            {
                entity.Property(nameof(BaseEntity.Id))
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("gen_random_uuid()");
            }

            var tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            var isDeletedProperty = entityType.FindProperty(nameof(AuditableEntity.IsDeleted));
            isDeletedProperty?.SetDefaultValue(false);

            var isActiveProperty = clrType.GetProperty("IsActive", BindingFlags.Instance | BindingFlags.Public);
            if (isActiveProperty != null)
            {
                entityType.FindProperty(isActiveProperty.Name)?.SetDefaultValue(true);
            }

            if (!useTenantScopedQueryFilter)
            {
                if (isDeletedProperty != null && isActiveProperty != null)
                {
                    entity.HasQueryFilter(BuildCombinedFilter(clrType, isActiveProperty));
                }
                else if (isDeletedProperty != null)
                {
                    entity.HasQueryFilter(BuildIsDeletedFilter(clrType));
                }
                else if (isActiveProperty != null)
                {
                    entity.HasQueryFilter(BuildIsActiveFilter(clrType, isActiveProperty));
                }
            }

            var createdDateProperty = entityType.FindProperty(nameof(AuditableEntity.CreatedAt));
            if (applyPostgresExtensions)
            {
                createdDateProperty?.SetDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TemplateDbContext).Assembly);
    }

    public static string ToSnakeCase(string input) =>
        string.Concat(
            input.Select((ch, i) =>
                i > 0 && char.IsUpper(ch) ? "_" + ch : ch.ToString()
            )
        ).ToLowerInvariant();

    private static LambdaExpression BuildIsDeletedFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var body = Expression.Equal(
            Expression.Property(parameter, nameof(AuditableEntity.IsDeleted)),
            Expression.Constant(false));
        return Expression.Lambda(body, parameter);
    }

    private static LambdaExpression BuildIsActiveFilter(Type entityType, PropertyInfo isActiveProperty)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var body = Expression.Equal(
            Expression.Property(parameter, isActiveProperty),
            Expression.Constant(true));
        return Expression.Lambda(body, parameter);
    }

    private static LambdaExpression BuildCombinedFilter(Type entityType, PropertyInfo isActiveProperty)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var notDeleted = Expression.Equal(
            Expression.Property(parameter, nameof(AuditableEntity.IsDeleted)),
            Expression.Constant(false));
        var active = Expression.Equal(
            Expression.Property(parameter, isActiveProperty),
            Expression.Constant(true));
        return Expression.Lambda(Expression.AndAlso(notDeleted, active), parameter);
    }
}
