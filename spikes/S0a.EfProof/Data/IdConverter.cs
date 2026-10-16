using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using GridironFrontOffice.Spikes.S0a.EfProof.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GridironFrontOffice.Spikes.S0a.EfProof.Data;

/// <summary>
/// One converter for every strongly typed ID (D-015): TId ⇄ int. Registered for all ID types at once in
/// LeagueDbContext.ConfigureConventions, so new ID types need no per-entity configuration.
/// </summary>
[SuppressMessage("Performance", "CA1812", Justification = "Instantiated by EF Core from the conventions (typeof).")]
internal sealed class IdConverter<TId>() : ValueConverter<TId, int>(ToProvider, FromProvider)
    where TId : struct, IStronglyTypedId
{
    private static readonly Expression<Func<TId, int>> ToProvider = id => id.Value;
    private static readonly Expression<Func<int, TId>> FromProvider = BuildFromProvider();

    // TId has a single (int Value) constructor (record struct). Built as an expression because
    // expression trees can't call static abstract interface members.
    private static Expression<Func<int, TId>> BuildFromProvider()
    {
        var value = Expression.Parameter(typeof(int), "value");
        var constructor = typeof(TId).GetConstructor([typeof(int)])
            ?? throw new InvalidOperationException($"{typeof(TId).Name} needs a constructor taking int.");
        return Expression.Lambda<Func<int, TId>>(Expression.New(constructor, value), value);
    }
}
