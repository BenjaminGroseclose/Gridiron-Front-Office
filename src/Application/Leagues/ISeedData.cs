namespace GridironFrontOffice.Application.Leagues;

/// <summary>The built-in league data, loaded once (D-035). Generators read only this; nothing hard-codes a list.</summary>
public interface ISeedData
{
    TeamSet TeamSet { get; }
    CollegeList Colleges { get; }
}
