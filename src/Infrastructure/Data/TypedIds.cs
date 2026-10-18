using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using GridironFrontOffice.Core.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GridironFrontOffice.Infrastructure.Data;

/// <summary>
/// The strongly typed ID convention (D-015, proven in Spike S0a): one converter for every ID type, plus the
/// AUTOINCREMENT fix EF's SQLite provider doesn't apply to converted keys.
/// </summary>
internal static class TypedIds
{
    /// <summary>Every <c>readonly record struct XxxId(int Value)</c> in the assembly.</summary>
    public static IReadOnlyList<Type> DeclaredIn(Assembly assembly) =>
        [.. assembly.GetTypes().Where(t => t.IsValueType && typeof(IStronglyTypedId).IsAssignableFrom(t))];

    public static void ApplyConversions(ModelConfigurationBuilder configurationBuilder, IEnumerable<Type> idTypes)
    {
        foreach (var idType in idTypes)
        {
            configurationBuilder.Properties(idType).HaveConversion(typeof(IdConverter<>).MakeGenericType(idType));
        }
    }

    /// <summary>
    /// EF's SQLite convention adds AUTOINCREMENT only to plain integer keys. A converted key gets a bare
    /// INTEGER PRIMARY KEY, and SQLite may then reuse the highest ID after a delete, while IDs are referenced from
    /// history. So every generated typed-ID key gets it explicitly. EF Core 10 ignores the old raw
    /// "Sqlite:Autoincrement" annotation; it has to be the value-generation strategy API. Call it last in
    /// OnModelCreating.
    /// </summary>
    public static void UseAutoincrementForGeneratedKeys(ModelBuilder modelBuilder)
    {
        var keys = modelBuilder.Model.GetEntityTypes()
            .Select(entity => (Entity: entity, Key: entity.FindPrimaryKey()))
            .Where(x => x.Key is { Properties.Count: 1 })
            .Select(x => (x.Entity, Property: x.Key!.Properties[0]))
            .Where(x => typeof(IStronglyTypedId).IsAssignableFrom(x.Property.ClrType) && x.Property.ValueGenerated == ValueGenerated.OnAdd)
            .ToList();

        foreach (var (entity, property) in keys)
        {
            modelBuilder.Entity(entity.ClrType).Property(property.Name).UseAutoincrement();
        }
    }
}

/// <summary>TId ⇄ int for any strongly typed ID.</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated by EF Core from TypedIds.ApplyConversions (typeof).")]
internal sealed class IdConverter<TId>() : ValueConverter<TId, int>(ToProvider, FromProvider)
    where TId : struct, IStronglyTypedId
{
    private static readonly Expression<Func<TId, int>> ToProvider = id => id.Value;
    private static readonly Expression<Func<int, TId>> FromProvider = BuildFromProvider();

    // The ID is a record struct with a single (int Value) constructor. Built as an expression tree because expression
    // trees can't call static abstract interface members.
    private static Expression<Func<int, TId>> BuildFromProvider()
    {
        var value = Expression.Parameter(typeof(int), "value");
        var constructor = typeof(TId).GetConstructor([typeof(int)])
            ?? throw new InvalidOperationException($"{typeof(TId).Name} needs a constructor that takes an int.");
        return Expression.Lambda<Func<int, TId>>(Expression.New(constructor, value), value);
    }
}
