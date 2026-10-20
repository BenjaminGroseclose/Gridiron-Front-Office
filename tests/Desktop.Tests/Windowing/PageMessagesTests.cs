using GridironFrontOffice.Desktop.Windowing;

namespace GridironFrontOffice.Desktop.Tests.Windowing;

public sealed class PageMessagesTests
{
    [Fact]
    public void Drag_regions_arrive_as_bars_with_their_holes()
    {
        const string json = """{"gfo":"drag","regions":[{"bar":[0,0,1440,40],"holes":[[1100,4,120,32],[1230,4,32,32]]}]}""";

        var message = PageMessages.Parse(json).ShouldBeOfType<DragRegionsMessage>();

        var region = message.Regions.ShouldHaveSingleItem();
        region.Bar.ShouldBe(new CssRect(0, 0, 1440, 40));
        region.Holes.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(500, 20, true)]      // the bar's empty space
    [InlineData(1150, 20, false)]    // a button in the bar
    [InlineData(500, 60, false)]     // below the bar
    [InlineData(1440, 20, false)]    // right and bottom edges are outside
    public void Only_the_bars_empty_space_is_a_caption(double x, double y, bool caption)
    {
        var region = new DragRegion(new CssRect(0, 0, 1440, 40), [new CssRect(1100, 4, 120, 32)]);

        region.IsCaption(x, y).ShouldBe(caption);
    }

    [Theory]
    [InlineData("dark", true)]
    [InlineData("light", false)]
    public void The_theme_mode_arrives(string mode, bool dark)
    {
        PageMessages.Parse($$"""{"gfo":"theme","mode":"{{mode}}"}""").ShouldBe(new ThemeModeMessage(dark));
    }

    [Fact]
    public void A_page_with_no_drag_region_clears_them()
    {
        PageMessages.Parse("""{"gfo":"drag","regions":[]}""").ShouldBeOfType<DragRegionsMessage>().Regions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("__bwv:[\"BeginInvokeDotNet\",\"1\"]")]   // Blazor's own messages share the channel
    [InlineData("{\"kind\":\"drag\"}")]                    // JSON, but not ours
    [InlineData("{\"gfo\":\"resize\"}")]                   // ours, but unknown
    [InlineData("{\"gfo\":\"drag\"}")]                     // missing its regions
    [InlineData("{\"gfo\":\"drag\",\"regions\":[{\"bar\":[0,0],\"holes\":[]}]}")]   // a short rectangle
    [InlineData("{\"gfo\":\"drag\",\"regions\":[{\"bar\":[\"a\",0,1,1],\"holes\":[]}]}")]
    [InlineData("{not json")]
    public void Anything_else_is_ignored(string? message)
    {
        PageMessages.Parse(message).ShouldBeNull();
    }
}
