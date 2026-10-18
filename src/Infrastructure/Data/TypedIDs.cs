using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using GridironFrontOffice.Core.IDs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GridironFrontOffice.Infrastructure.Data;

/// <summary>
/// The strongly typed ID convention (D-015, proven in Spike S0a): one converter for every ID type, plus the
/// AUTOINCREMENT fix EF's SQLite provider doesn't apply to converted keys.
/// </summary>
internal static class TypedIDs
{
    /// <summary>Every <c>readonly record struct XxxID(int Value)</c> in the assembly.</summary>
    public static IReadOnlyList<Type> DeclaredIn(Assembly assembly) =>
        [.. assembly.GetTypes().Where(t => t.IsValueType && typeof(IStronglyTypedID).IsAssignableFrom(t))];

    public static void ApplyConversions(ModelConfigurationBuilder configurationBuilder, IEnumerable<Type> idTypes)
    {
        foreach (var idType in idTypes)
        {
            configurationBuilder.Properties(idType).HaveConversion(typeof(IDConverter<>).MakeGenericType(idType));
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
            .Where(x => typeof(IStronglyTypedID).IsAssignableFrom(x.Property.ClrType) && x.Property.ValueGenerated == ValueGenerated.OnAdd)
            .ToList();

        foreach (var (entity, property) in keys)
        {
            modelBuilder.Entity(entity.ClrType).Property(property.Name).UseAutoincrement();
        }
    }
}

/// <summary>TID ⇄ int for any strongly typed ID.</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated by EF Core from TypedIDs.ApplyConversions (typeof).")]
internal sealed class IDConverter<TID>() : ValueConverter<TID, int>(ToProvider, FromProvider)
    where TID : struct, IStronglyTypedID
{
    private static readonly Expression<Func<TID, int>> ToProvider = id => id.Value;
    private static readonly Expression<Func<int, TID>> FromProvider = BuildFromProvider();

    // The ID is a record struct with a single (int Value) constructor. Built as an expression tree because expression
    // trees can't call static abstract interface members.
    private static Expression<Func<int, TID>> BuildFromProvider()
    {
        var value = Expression.Parameter(typeof(int), "value");
        var constructor = typeof(TID).GetConstructor([typeof(int)])
            ?? throw new InvalidOperationException($"{typeof(TID).Name} needs a constructor that takes an int.");
        return Expression.Lambda<Func<int, TID>>(Expression.New(constructor, value), value);
    }
}
