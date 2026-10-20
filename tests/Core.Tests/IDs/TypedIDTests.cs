using System.Runtime.CompilerServices;
using GridironFrontOffice.Core.IDs;

namespace GridironFrontOffice.Core.Tests.IDs;

/// <summary>D-015: every typed ID has the shape the database convention (Infrastructure's TypedIDs) relies on.</summary>
public sealed class TypedIDTests
{
    /// <summary>Every typed ID in Core, found the same way the database convention finds them.</summary>
    public static TheoryData<Type> IDTypes { get; } = new(typeof(IStronglyTypedID).Assembly.GetTypes()
        .Where(t => t.IsValueType && typeof(IStronglyTypedID).IsAssignableFrom(t)));

    [Fact]
    public void The_M1_IDs_are_all_there()
    {
        IDTypes.Select(row => row.Data.Name).Order(StringComparer.Ordinal).ShouldBe(
            ["CollegeID", "ConferenceID", "ContractID", "DivisionID", "PlayerID", "StadiumID", "TeamID"]);
    }

    [Theory]
    [MemberData(nameof(IDTypes))]
    public void Every_typed_ID_has_the_shape_the_database_convention_needs(Type id)
    {
        id.Name.ShouldEndWith("ID");
        id.GetConstructor([typeof(int)]).ShouldNotBeNull("the converter builds IDs through an (int) constructor");
        id.IsDefined(typeof(IsReadOnlyAttribute), inherit: false).ShouldBeTrue("an ID is a readonly record struct");
    }

    [Fact]
    public void An_ID_compares_by_value_and_its_default_means_not_saved_yet()
    {
        new TeamID(7).ShouldBe(new TeamID(7));
        new TeamID(7).ShouldNotBe(new TeamID(8));
        default(TeamID).Value.ShouldBe(0, "0 means not saved yet; SQLite's IDs start at 1");
    }
}
