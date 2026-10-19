using Bunit;
using GridironFrontOffice.UI.Components.DataGrid;
using GridironFrontOffice.UI.Components.Tests.TestDoubles;
using GridironFrontOffice.UI.Components.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.UI.Components.Tests.DataGrid;

/// <summary>
/// UI_DESIGN_SYSTEM.md §5.3, the S0b verdict as GFO behavior: GFO's column rules, the compact sort header, selection,
/// Enter and Space, the density row height, and the empty frame. Virtualization is off here (bUnit has no layout); the
/// gallery page covers it in a real browser.
/// </summary>
public sealed class DataGridTests : GfoTestContext
{
    private static readonly Row[] Rows = [new(1, "Okafor", 26, 84), new(2, "Mensah", 31, 91), new(3, "Ashby", 23, 77)];

    [Fact]
    public void Columns_keep_their_width_as_their_minimum_and_numbers_align_right()
    {
        var cut = RenderGrid();

        var table = cut.Find("table");
        table.GetAttribute("aria-label").ShouldBe("Players");
        table.GetAttribute("style")!.ShouldContain("grid-template-columns: 200px 64px 72px");
        var headers = cut.FindAll("th");
        headers[1].GetAttribute("col-justify").ShouldBe("end");
        headers[1].ClassList.ShouldContain("gfo-num");
        headers[0].GetAttribute("col-justify").ShouldBe("start");
    }

    [Fact]
    public void Every_sortable_column_gets_the_compact_one_button_header()
    {
        var cut = RenderGrid();

        cut.FindAll("th button.gfo-th").Select(b => b.TextContent.Trim()).ShouldBe(["Player", "Age", "OVR"]);
    }

    [Fact]
    public void Numbers_sort_high_to_low_first_and_a_second_click_flips_them()
    {
        var cut = RenderGrid();

        Header(cut, "Age").Click();
        Names(cut).ShouldBe(["Mensah", "Okafor", "Ashby"]);
        Header(cut, "Age").GetAttribute("data-sorted").ShouldBe("descending");

        Header(cut, "Age").Click();
        Names(cut).ShouldBe(["Ashby", "Okafor", "Mensah"]);
        Header(cut, "Age").GetAttribute("data-sorted").ShouldBe("ascending");
    }

    [Fact]
    public void Text_sorts_A_to_Z_first_and_template_columns_sort_by_their_SortBy()
    {
        var cut = RenderGrid();

        Header(cut, "Player").Click();
        Names(cut).ShouldBe(["Ashby", "Mensah", "Okafor"]);

        Header(cut, "OVR").Click();
        Names(cut).ShouldBe(["Mensah", "Okafor", "Ashby"]);
        Header(cut, "Player").HasAttribute("data-sorted").ShouldBeFalse("Only the current sort column is marked.");
    }

    [Fact]
    public void Clicking_a_row_selects_it_and_paints_it_selected()
    {
        Row? selected = null;
        var cut = RenderGrid(selectedChanged: r => selected = r);

        BodyRow(cut, "Mensah").QuerySelector("td")!.Click();

        selected.ShouldBe(Rows[1]);
        cut.FindAll("tr.gfo-row-selected").ShouldHaveSingleItem().TextContent.ShouldContain("Mensah");
    }

    [Fact]
    public async Task Enter_opens_and_Space_selects_the_focused_row()
    {
        Row? selected = null, opened = null;
        var cut = RenderGrid(selectedChanged: r => selected = r, activated: r => opened = r);

        await BodyRow(cut, "Ashby").QuerySelector("td")!.FocusAsync(new Microsoft.AspNetCore.Components.Web.FocusEventArgs());
        await cut.InvokeAsync(() => cut.Instance.OnKeyAsync(" "));
        await cut.InvokeAsync(() => cut.Instance.OnKeyAsync("Enter"));

        selected.ShouldBe(Rows[2]);
        opened.ShouldBe(Rows[2]);
    }

    [Fact]
    public async Task Keys_before_any_row_has_focus_do_nothing()
    {
        Row? opened = null;
        var cut = RenderGrid(activated: r => opened = r);

        await cut.InvokeAsync(() => cut.Instance.OnKeyAsync("Enter"));

        opened.ShouldBeNull();
    }

    [Fact]
    public void A_double_click_opens_the_row()
    {
        Row? opened = null;
        var cut = RenderGrid(activated: r => opened = r);

        BodyRow(cut, "Okafor").QuerySelector("td")!.DoubleClick();

        opened.ShouldBe(Rows[0]);
    }

    [Theory]
    [InlineData(GfoDensity.Comfortable, 46)]
    [InlineData(GfoDensity.Compact, 36)]
    public async Task The_row_height_follows_the_density_setting(GfoDensity density, float rowHeight)
    {
        await Theme.ApplyAsync(new GfoAppearance(Density: density));

        var cut = RenderGrid();

        cut.FindComponent<FluentDataGrid<Row>>().Instance.ItemSize.ShouldBe(rowHeight);
    }

    [Fact]
    public async Task A_density_change_re_renders_the_grid()
    {
        var cut = RenderGrid();

        await cut.InvokeAsync(() => Theme.ApplyAsync(new GfoAppearance(Density: GfoDensity.Compact)));

        await cut.WaitForAssertionAsync(() => cut.FindComponent<FluentDataGrid<Row>>().Instance.ItemSize.ShouldBe(36));
    }

    [Fact]
    public void With_no_rows_the_empty_content_fills_the_frame_instead_of_the_table()
    {
        var cut = Render<DataGrid<Row>>(p => p
            .Add(g => g.Items, Array.Empty<Row>())
            .Add(g => g.Label, "Players")
            .Add(g => g.EmptyContent, b => b.AddMarkupContent(0, "<p class=\"none\">No players yet</p>"))
            .Add(g => g.ChildContent, Columns));

        cut.Find(".gfo-grid-empty .none").TextContent.ShouldBe("No players yet");
        cut.FindAll("table").ShouldBeEmpty();
    }

    [Fact]
    public void A_column_without_a_width_is_a_mistake_caught_at_once()
    {
        DataGridColumnRules.RequireWidth("Age", "64px").ShouldBe("64px");
        Should.Throw<InvalidOperationException>(() => DataGridColumnRules.RequireWidth("Player", null))
            .Message.ShouldBe("DataGrid column 'Player' needs a Width, such as 72px.");

        // In a real grid the render fails outright (Fluent then trips over the broken column too).
        Should.Throw<Exception>(() => Render<DataGrid<Row>>(p => p
            .Add(g => g.Items, Rows)
            .Add(g => g.Label, "Players")
            .Add(g => g.Virtualize, false)
            .Add(g => g.ChildContent, b =>
            {
                b.OpenComponent<DataGridColumn<Row, string>>(0);
                b.AddComponentParameter(1, nameof(DataGridColumn<Row, string>.Title), "Player");
                b.AddComponentParameter(2, nameof(DataGridColumn<Row, string>.Property), (System.Linq.Expressions.Expression<Func<Row, string>>)(r => r.Name));
                b.CloseComponent();
            })));
    }

    private static readonly GridSort<Row> ByOverall = GridSort<Row>.ByAscending(r => r.Overall);

    private static readonly RenderFragment Columns = b =>
    {
        b.OpenComponent<DataGridColumn<Row, string>>(0);
        b.AddComponentParameter(1, "Title", "Player");
        b.AddComponentParameter(2, "Property", (System.Linq.Expressions.Expression<Func<Row, string>>)(r => r.Name));
        b.AddComponentParameter(3, "Width", "200px");
        b.AddComponentParameter(4, "Pinned", true);
        b.CloseComponent();

        b.OpenComponent<DataGridColumn<Row, int>>(10);
        b.AddComponentParameter(11, "Title", "Age");
        b.AddComponentParameter(12, "Property", (System.Linq.Expressions.Expression<Func<Row, int>>)(r => r.Age));
        b.AddComponentParameter(13, "Width", "64px");
        b.AddComponentParameter(14, "Numeric", true);
        b.CloseComponent();

        b.OpenComponent<DataGridTemplateColumn<Row>>(20);
        b.AddComponentParameter(21, "Title", "OVR");
        b.AddComponentParameter(22, "Width", "72px");
        b.AddComponentParameter(23, "Numeric", true);
        b.AddComponentParameter(24, "SortBy", ByOverall);
        b.AddComponentParameter(25, "ChildContent", (RenderFragment<Row>)(row => c => c.AddContent(0, row.Overall)));
        b.CloseComponent();
    };

    private IRenderedComponent<DataGrid<Row>> RenderGrid(Action<Row?>? selectedChanged = null, Action<Row>? activated = null) =>
        Render<DataGrid<Row>>(p => p
            .Add(g => g.Items, Rows)
            .Add(g => g.Label, "Players")
            .Add(g => g.ItemKey, r => r.ID)
            .Add(g => g.Virtualize, false)
            .Add(g => g.SelectedItemChanged, EventCallback.Factory.Create<Row?>(this, r => selectedChanged?.Invoke(r)))
            .Add(g => g.OnItemActivated, EventCallback.Factory.Create<Row>(this, r => activated?.Invoke(r)))
            .Add(g => g.ChildContent, Columns));

    private static AngleSharp.Dom.IElement Header(IRenderedComponent<DataGrid<Row>> cut, string title) =>
        cut.FindAll("th button.gfo-th").Single(b => b.TextContent.Trim() == title);

    private static AngleSharp.Dom.IElement BodyRow(IRenderedComponent<DataGrid<Row>> cut, string name) =>
        cut.FindAll("tbody tr").Single(r => r.QuerySelector("td")?.TextContent.Trim() == name);

    private static List<string> Names(IRenderedComponent<DataGrid<Row>> cut) =>
        [.. cut.FindAll("tbody tr").Select(r => r.QuerySelector("td")?.TextContent.Trim()).OfType<string>()];

    public sealed record Row(int ID, string Name, int Age, int Overall);
}
