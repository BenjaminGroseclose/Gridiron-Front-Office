using Bunit;
using GridironFrontOffice.UI.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GridironFrontOffice.UI.Tests.Diagnostics;

public sealed class ClientDiagnosticsTests : BunitContext
{
    private readonly ListLoggerProvider _logs = new();

    public ClientDiagnosticsTests() => Services.AddLogging(logging => logging.AddProvider(_logs));

    [Fact]
    public void It_attaches_to_the_page_script_after_the_first_render()
    {
        var attach = JSInterop.SetupVoid("gfoDiagnostics.attach", _ => true);

        Render<ClientDiagnostics>();

        attach.VerifyInvoke("gfoDiagnostics.attach");
    }

    [Fact]
    public void A_JavaScript_error_is_logged_as_an_error_under_the_UI_Js_category()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<ClientDiagnostics>();

        cut.Instance.LogClientError("error", "x is undefined", "https://0.0.0.1/app.js", 12, 7, "at f (app.js:12:7)");

        var entry = _logs.Entries.ShouldHaveSingleItem();
        entry.Category.ShouldBe("GridironFrontOffice.UI.Js");
        entry.Level.ShouldBe(LogLevel.Error);
        entry.EventID.ShouldBe(8001);
        entry.Message.ShouldBe("JavaScript error: x is undefined (https://0.0.0.1/app.js:12:7)\nat f (app.js:12:7)");
    }

    [Fact]
    public void A_flood_is_cut_off_after_the_limit_with_one_warning()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<ClientDiagnostics>();

        for (var i = 0; i < ClientDiagnostics.Limit + 25; i++)
        {
            cut.Instance.LogClientError("error", $"failure {i}", "", 0, 0, "");
        }

        _logs.Entries.Count(e => e.Level == LogLevel.Error).ShouldBe(ClientDiagnostics.Limit);
        _logs.Entries.Where(e => e.Level == LogLevel.Warning).ShouldHaveSingleItem().EventID.ShouldBe(8002);
    }

    [Fact]
    public void A_host_page_without_the_script_is_reported_once()
    {
        JSInterop.SetupVoid("gfoDiagnostics.attach", _ => true).SetException(new Microsoft.JSInterop.JSException("gfoDiagnostics is not defined"));

        Render<ClientDiagnostics>();

        _logs.Entries.ShouldHaveSingleItem().EventID.ShouldBe(8003);
    }
}
