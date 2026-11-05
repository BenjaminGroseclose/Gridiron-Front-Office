using System.Globalization;
using Bunit;
using GridironFrontOffice.Application.Leagues;
using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.UI.Components.Theming;
using GridironFrontOffice.UI.Football;
using GridironFrontOffice.UI.Tests.TestDoubles;

namespace GridironFrontOffice.UI.Tests.Football;

/// <summary>
/// M1 WP7 slice 2 (guide §4): the team crest (F-02 §4.4). The field in the primary; the stripe and a rim in the
/// secondary, clipped to the field; the monogram in the accent, or the on-color below 3:1 on the primary, with a halo
/// in the primary. An image named for its team, unless it's decorative.
/// </summary>
public sealed class TeamMarkTests : BunitContext
{
	// New York Excelsiors' colors: an accent that holds (4.70:1 on the primary), so the monogram keeps it
	private const string Primary = "#A6192E";
	private const string Secondary = "#2B2E34";
	private const string Accent = "#C9CDD2";

	public static TheoryData<CrestShape, CrestStripe> EveryPair()
	{
		var pairs = new TheoryData<CrestShape, CrestStripe>();
		foreach (var shape in Enum.GetValues<CrestShape>())
		{
			foreach (var stripe in Enum.GetValues<CrestStripe>())
			{
				pairs.Add(shape, stripe);
			}
		}

		return pairs;
	}

	[Theory]
	[MemberData(nameof(EveryPair))]
	public void Every_shape_and_stripe_draws_its_field_stripe_rim_and_monogram(CrestShape shape, CrestStripe stripe)
	{
		var cut = Render<TeamMark>(p => p.Add(m => m.Team, Team(shape, stripe)));
		var field = CrestGeometry.Field(shape);

		var svg = cut.Find("svg");
		svg.GetAttribute("data-shape").ShouldBe(shape.ToString());
		svg.GetAttribute("data-stripe").ShouldBe(stripe.ToString());

		// The field, the clip path and the rim are one outline; the stripe and the rim are drawn inside the clip
		var clip = cut.Find("[id^='gfo-crest-']");
		clip.QuerySelector("path")!.GetAttribute("d").ShouldBe(field);
		var group = cut.Find("g[clip-path]");
		group.GetAttribute("clip-path").ShouldBe($"url(#{clip.GetAttribute("id")})");

		var fieldPath = group.QuerySelector("path.field")!;
		(fieldPath.GetAttribute("d"), fieldPath.GetAttribute("fill")).ShouldBe((field, Primary));

		var stripePath = group.QuerySelector("path.stripe");
		if (stripe == CrestStripe.None)
		{
			stripePath.ShouldBeNull();
		}
		else
		{
			stripePath.ShouldNotBeNull();
			(stripePath.GetAttribute("d"), stripePath.GetAttribute("fill")).ShouldBe((CrestGeometry.Stripe(stripe), Secondary));
		}

		var rim = group.QuerySelector("path.rim")!;
		rim.GetAttribute("d").ShouldBe(field);
		(rim.GetAttribute("fill"), rim.GetAttribute("stroke"), rim.GetAttribute("stroke-width")).ShouldBe(("none", Secondary, "8"));
		string[] layers = stripe == CrestStripe.None ? ["field", "rim"] : ["field", "stripe", "rim"];
		group.Children.Select(c => c.ClassList[0]).ShouldBe(layers);

		// The monogram sits outside the clip, on top, with its halo in the field's color under each letter
		var monogram = cut.Find("svg > text.monogram");
		monogram.TextContent.ShouldBe("NY");
		monogram.GetAttribute("fill").ShouldBe(Accent);
		(monogram.GetAttribute("stroke"), monogram.GetAttribute("stroke-width")).ShouldBe((Primary, "7"));
		monogram.GetAttribute("paint-order").ShouldBe("stroke");
	}

	[Theory]
	[InlineData(CrestShape.Shield, "N")]
	[InlineData(CrestShape.Shield, "NY")]
	[InlineData(CrestShape.Roundel, "CIN")]
	[InlineData(CrestShape.Hexagon, "BUF")]
	[InlineData(CrestShape.Diamond, "A")]
	[InlineData(CrestShape.Diamond, "NO")]
	[InlineData(CrestShape.Banner, "KC")]
	public void The_monogram_is_sized_for_its_letters_and_centered_for_its_shape(CrestShape shape, string letters)
	{
		var monogram = Render<TeamMark>(p => p.Add(m => m.Team, Team(shape, monogram: letters))).Find("text.monogram");

		monogram.TextContent.ShouldBe(letters);
		monogram.GetAttribute("font-size").ShouldBe(Number(CrestGeometry.MonogramSize(shape, letters.Length)));
		monogram.GetAttribute("y").ShouldBe(Number(CrestGeometry.MonogramY(shape)));
		(monogram.GetAttribute("x"), monogram.GetAttribute("text-anchor")).ShouldBe(("50", "middle"));
		monogram.ClassList.ShouldContain("gfo-display");
	}

	[Fact]
	public void It_is_an_image_named_for_its_team()
	{
		var svg = Render<TeamMark>(p => p.Add(m => m.Team, Team())).Find("svg");

		svg.GetAttribute("role").ShouldBe("img");
		svg.GetAttribute("aria-label").ShouldBe("New York Excelsiors");
		svg.HasAttribute("aria-hidden").ShouldBeFalse();
		svg.GetAttribute("focusable").ShouldBe("false");
		svg.GetAttribute("viewBox").ShouldBe("0 0 100 100");
	}

	[Fact]
	public void A_decorative_crest_is_hidden_and_has_no_name()
	{
		var svg = Render<TeamMark>(p => p.Add(m => m.Team, Team()).Add(m => m.Decorative, true)).Find("svg");

		svg.GetAttribute("aria-hidden").ShouldBe("true");
		svg.HasAttribute("role").ShouldBeFalse();
		svg.HasAttribute("aria-label").ShouldBeFalse();
	}

	[Theory]
	[InlineData(16)]
	[InlineData(96)]
	public void Size_sets_the_width_and_the_height(int size)
	{
		var svg = Render<TeamMark>(p => p.Add(m => m.Team, Team()).Add(m => m.Size, size)).Find("svg");

		(svg.GetAttribute("width"), svg.GetAttribute("height")).ShouldBe((Number(size), Number(size)));
	}

	[Fact]
	public void It_is_32_px_by_default_and_passes_its_class_and_attributes_through()
	{
		var svg = Render<TeamMark>(p => p
			.Add(m => m.Team, Team())
			.Add(m => m.Class, "crest")
			.AddUnmatched("data-testid", "mark")).Find("svg");

		(svg.GetAttribute("width"), svg.GetAttribute("height")).ShouldBe(("32", "32"));
		svg.ClassList.ShouldContain("gfo-team-mark");
		svg.ClassList.ShouldContain("crest");
		svg.GetAttribute("data-testid").ShouldBe("mark");
	}

	[Fact]
	public void Each_crest_owns_its_clip_path()
	{
		// 32 crests on the League page: a shared id would clip every stripe to the first crest's shape
		var first = Render<TeamMark>(p => p.Add(m => m.Team, Team(CrestShape.Shield)));
		var second = Render<TeamMark>(p => p.Add(m => m.Team, Team(CrestShape.Diamond)));

		var (firstID, secondID) = (ClipID(first), ClipID(second));
		firstID.ShouldNotBe(secondID);
		first.Find("g[clip-path]").GetAttribute("clip-path").ShouldBe($"url(#{firstID})");
		second.Find("g[clip-path]").GetAttribute("clip-path").ShouldBe($"url(#{secondID})");
	}

	[Theory]
	[InlineData("GB", "#7A1F1F", "#2E4A3A", "#FFFFFF")]   // 1.06:1, the lowest: white on a dark field
	[InlineData("SF", "#2E6F8E", "#1B1F24", "#FFFFFF")]   // 2.98:1, just under
	[InlineData("TPA", "#E1AD21", "#FFFFFF", "#0B0D12")]  // 2.06:1 on a light field: near-black, as on an accent fill
	[InlineData("PHX", "#B8651D", "#2B2B2B", "#2B2B2B")]  // 3.32:1, just over: the accent stays
	[InlineData("DAL", "#1E1E1E", "#F2EFE6", "#F2EFE6")]  // 14.50:1
	public void The_monogram_keeps_its_accent_from_3_to_1_and_takes_the_on_color_below(
		string team, string primary, string accent, string monogram)
	{
		var text = Render<TeamMark>(p => p.Add(m => m.Team, Team(primary: primary, accent: accent))).Find("text.monogram");

		text.GetAttribute("fill").ShouldBe(monogram, team);
		text.GetAttribute("stroke").ShouldBe(primary, $"{team}: the halo is the field's color");
		TeamMark.MonogramContrast.ShouldBe(3.0);
	}

	[Fact]
	public async Task Every_shipped_team_draws_with_its_own_colors_and_name()
	{
		await using var app = new RealApp();
		var fallbacks = new List<string>();

		foreach (var (definition, index) in app.Data.TeamSet.Teams.Select((t, i) => (t, i)))
		{
			var team = Identity(definition, index);
			var cut = Render<TeamMark>(p => p.Add(m => m.Team, team));

			cut.Find("svg").GetAttribute("aria-label").ShouldBe($"{team.City} {team.Nickname}");
			cut.Find("path.field").GetAttribute("fill").ShouldBe(team.PrimaryColor, team.Abbreviation);
			cut.Find("path.rim").GetAttribute("stroke").ShouldBe(team.SecondaryColor, team.Abbreviation);
			cut.Find("path.stripe").GetAttribute("fill").ShouldBe(team.SecondaryColor, team.Abbreviation);   // every shipped team has one

			var monogram = cut.Find("text.monogram");
			monogram.TextContent.ShouldBe(team.CrestMonogram);
			var fill = monogram.GetAttribute("fill");
			if (fill != team.AccentColor)
			{
				fill.ShouldBe(AccentColors.OnAccent(team.PrimaryColor), team.Abbreviation);
				fallbacks.Add(team.Abbreviation);
			}
		}

		// Guide §8 lists these 15 (each accent below 3:1 on its own primary). A change here is a team-set change.
		fallbacks.ShouldBe(
			["GB", "KC", "CIN", "DEN", "NYS", "WAS", "SEA", "TPA", "JAX", "LAC", "BUF", "LAN", "NSH", "CHA", "SF"],
			ignoreOrder: true);
	}

	[Fact]
	public void No_number_in_the_svg_takes_a_cultures_decimal_comma()
	{
		var (culture, uiCulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

			foreach (var shape in Enum.GetValues<CrestShape>())
			{
				var cut = Render<TeamMark>(p => p.Add(m => m.Team, Team(shape, CrestStripe.Sash, "BUF")).Add(m => m.Size, 20));

				var withComma =
					from element in cut.FindAll("*")
					from attribute in element.Attributes
					where attribute.Value.Contains(',', StringComparison.Ordinal)
					select $"{element.LocalName} {attribute.Name}=\"{attribute.Value}\"";

				withComma.ShouldBeEmpty($"{shape}: an SVG number written with a decimal comma breaks the drawing");
			}
		}
		finally
		{
			(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, uiCulture);
		}
	}

	private static TeamIdentityDto Team(CrestShape shape = CrestShape.Shield, CrestStripe stripe = CrestStripe.TwinBars,
		string monogram = "NY", string primary = Primary, string accent = Accent) =>
		new(new TeamID(1), "NYX", "New York", "Excelsiors", primary, Secondary, accent, shape, monogram, stripe);

	// As LeagueWriter numbers them: in the team set's order, from 1
	private static TeamIdentityDto Identity(TeamDefinition t, int index) => new(new TeamID(index + 1), t.Abbr, t.City,
		t.Nickname, t.Colors.Primary, t.Colors.Secondary, t.Colors.Accent, t.Crest.Shape, t.Crest.Monogram, t.Crest.Stripe);

	private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

	private static string? ClipID(IRenderedComponent<TeamMark> cut) => cut.Find("[id^='gfo-crest-']").GetAttribute("id");
}
