using System.Globalization;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Formatting.Json;
using Serilog.Parsing;

namespace GridironFrontOffice.Infrastructure.Logging;

/// <summary>
/// The main log's line format (LOGGING.md §3):
/// <code>
/// 2026-10-16 14:03:07.412 -04:00 [INF] [GridironFrontOffice.Desktop.App] Window shown
///     {"SaveId":7,"Phase":"Draft"}          ← only when the event carries properties the message didn't use
/// System.InvalidOperationException: …       ← only when there's an exception
/// </code>
/// AppVersion and SessionId enrich every event but are printed once, in the startup banner. Every line passes
/// through <see cref="PathTokens"/>, so user folders never reach the file.
/// </summary>
public sealed class GfoLogFormatter : ITextFormatter
{
    /// <summary>Properties that are already on the line, or printed once in the banner.</summary>
    private static readonly HashSet<string> NotRepeated = new(StringComparer.Ordinal)
    {
        "SourceContext", "AppVersion", "SessionId", "EventId",
    };

    private static readonly MessageTemplateTextFormatter Line = new(
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}", CultureInfo.InvariantCulture);

    private static readonly JsonValueFormatter Values = new(typeTagName: null);

    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);

        using var buffer = new StringWriter(CultureInfo.InvariantCulture);
        Line.Format(logEvent, buffer);

        var inMessage = logEvent.MessageTemplate.Tokens.OfType<PropertyToken>().Select(t => t.PropertyName).ToHashSet(StringComparer.Ordinal);
        var extra = logEvent.Properties.Where(p => !NotRepeated.Contains(p.Key) && !inMessage.Contains(p.Key)).ToList();
        if (extra.Count > 0)
        {
            buffer.Write("    {");
            for (var i = 0; i < extra.Count; i++)
            {
                if (i > 0)
                {
                    buffer.Write(',');
                }

                JsonValueFormatter.WriteQuotedJsonString(extra[i].Key, buffer);
                buffer.Write(':');
                Values.Format(extra[i].Value, buffer);
            }

            buffer.WriteLine('}');
        }

        if (logEvent.Exception is not null)
        {
            buffer.WriteLine(logEvent.Exception.ToString());
        }

        output.Write(PathTokens.Tokenize(buffer.ToString()));
    }
}
