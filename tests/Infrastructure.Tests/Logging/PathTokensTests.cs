using GridironFrontOffice.Infrastructure.Logging;

namespace GridironFrontOffice.Infrastructure.Tests.Logging;

public sealed class PathTokensTests
{
    private static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [Fact]
    public void LocalAppData_becomes_a_token()
    {
        var path = Path.Combine(LocalAppData, "GridironFrontOffice", "logs");

        PathTokens.Tokenize($"Opened {path}").ShouldBe(@"Opened %LOCALAPPDATA%\GridironFrontOffice\logs");
    }

    [Fact]
    public void Temp_wins_over_LocalAppData_because_it_is_more_specific()
    {
        var path = Path.Combine(Path.GetTempPath(), "x.gfo");

        PathTokens.Tokenize(path).ShouldBe(@"%TEMP%\x.gfo");
    }

    [Fact]
    public void Matching_ignores_case_and_leaves_no_trace_of_the_user_folder()
    {
        var text = $"{UserProfile.ToUpperInvariant()}\\Documents\\notes.txt";

        var tokenized = PathTokens.Tokenize(text);

        tokenized.ShouldBe(@"%USERPROFILE%\Documents\notes.txt");
        tokenized.ShouldNotContain(Path.GetFileName(UserProfile), Case.Insensitive);
    }

    [Fact]
    public void Other_paths_are_untouched() =>
        PathTokens.Tokenize(@"D:\Programming\GridironFrontOffice\src\Core\Team.cs").ShouldBe(@"D:\Programming\GridironFrontOffice\src\Core\Team.cs");
}
