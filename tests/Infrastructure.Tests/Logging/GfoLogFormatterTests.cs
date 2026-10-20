using GridironFrontOffice.Infrastructure.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace GridironFrontOffice.Infrastructure.Tests.Logging;

public sealed class GfoLogFormatterTests
{
    [Fact]
    public void A_plain_event_is_one_line_with_level_and_source()
    {
        var output = Render(log => log.ForContext("SourceContext", "GridironFrontOffice.Desktop.App").Information("Window shown"));

        var lines = Lines(output);
        lines.Length.ShouldBe(1);
        lines[0].ShouldMatch(@"^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d\.\d{3} [+-]\d\d:\d\d \[INF\] \[GridironFrontOffice\.Desktop\.App\] Window shown$");
    }

    [Fact]
    public void Message_values_render_as_literals_and_are_not_repeated()
    {
        var output = Render(log => log.Information("Draft pick {Overall} by {Team}", 7, "WOL"));

        Lines(output).ShouldBe([Lines(output)[0]]);
        output.ShouldContain("Draft pick 7 by WOL");
        output.ShouldNotContain("\"WOL\"");
    }

    [Fact]
    public void Context_properties_get_a_second_line_but_the_banner_ones_do_not()
    {
        string output;
        using (LogContext.PushProperty("SaveID", 12))
        using (LogContext.PushProperty("Phase", "Draft"))
        {
            output = Render(log => log.Information("Advanced"));
        }

        var lines = Lines(output);
        lines.Length.ShouldBe(2);
        lines[1].ShouldStartWith("    {");
        lines[1].ShouldContain("\"SaveID\":12");
        lines[1].ShouldContain("\"Phase\":\"Draft\"");
        output.ShouldNotContain("AppVersion");
        output.ShouldNotContain("SessionID");
    }

    [Fact]
    public void Exceptions_follow_with_their_stack_trace()
    {
        var output = Render(log => log.Error(Thrown(), "Command failed"));

        output.ShouldContain("[ERR]");
        output.ShouldContain("System.InvalidOperationException: boom");
        output.ShouldContain(nameof(Thrown));
    }

    [Fact]
    public void User_folders_are_tokenized_on_the_way_out()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GridironFrontOffice", "Saves", "a.gfo");

        var output = Render(log => log.Warning("Couldn't open {Path}", path));

        output.ShouldContain(@"Couldn't open %LOCALAPPDATA%\GridironFrontOffice\Saves\a.gfo");
    }

    private static string Render(Action<ILogger> write)
    {
        using var writer = new StringWriter();
        using (var logger = new LoggerConfiguration()
                   .Enrich.FromLogContext()
                   .Enrich.WithProperty("AppVersion", "0.0.1+abc1234")
                   .Enrich.WithProperty("SessionID", Guid.Empty)
                   .WriteTo.Sink(new FormattingSink(new GfoLogFormatter(), writer))
                   .CreateLogger())
        {
            write(logger);
        }

        return writer.ToString();
    }

    private static string[] Lines(string output) =>
        output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    private sealed class FormattingSink(ITextFormatter formatter, TextWriter writer) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => formatter.Format(logEvent, writer);
    }

    private static InvalidOperationException Thrown()
    {
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
    }
}
