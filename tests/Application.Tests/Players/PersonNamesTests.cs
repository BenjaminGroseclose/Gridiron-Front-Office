using GridironFrontOffice.Application.Players;
using GridironFrontOffice.Core.Random;

namespace GridironFrontOffice.Application.Tests.Players;

/// <summary>
/// Names (M1 WP4 guide §4.2, D-078 #3): a famous name or one already taken is re-rolled, however common its parts.
/// Full names compare without case or accents.
/// </summary>
public sealed class PersonNamesTests
{
	[Fact]
	public void A_famous_name_is_rerolled_however_common_it_is()
	{
		var names = new PersonNames(Names(("Tom", 1)), Names(("Brady", 1_000), ("Smith", 1)), ["Tom Brady"]);
		var random = new GfoRandom(1);

		Enumerable.Range(0, 50).Select(_ => names.Pick(random, Taken())).Distinct().ShouldBe([("Tom", "Smith")]);
	}

	[Theory]
	[InlineData("Tom", "Brädy")]
	[InlineData("TOM", "BRADY")]
	public void The_blocklist_ignores_case_and_accents(string first, string last)
	{
		var names = new PersonNames(Names((first, 1)), Names((last, 1_000), ("Smith", 1)), ["Tom Brady"]);

		names.Pick(new GfoRandom(2), Taken()).ShouldBe((first, "Smith"));
	}

	[Fact]
	public void A_name_already_taken_is_rerolled()
	{
		var names = new PersonNames(Names(("Tom", 1)), Names(("Jones", 1_000), ("Smith", 1)), []);

		names.Pick(new GfoRandom(3), Taken("tom jones")).ShouldBe(("Tom", "Smith"));
	}

	[Fact]
	public void An_empty_taken_set_lets_a_name_repeat()
	{
		// Only teammates must differ: the free agents pick with an empty set (WP4 guide §4.3, step 9).
		var names = new PersonNames(Names(("Tom", 1)), Names(("Smith", 1)), []);
		var random = new GfoRandom(4);

		names.Pick(random, Taken()).ShouldBe(("Tom", "Smith"));
		names.Pick(random, Taken()).ShouldBe(("Tom", "Smith"));
	}

	private static NameList Names(params (string Name, int Weight)[] names) =>
		new("test", [.. names.Select(n => new WeightedName(n.Name, n.Weight))]);

	private static HashSet<string> Taken(params string[] names) => new(names, PersonNames.FullNames);
}
