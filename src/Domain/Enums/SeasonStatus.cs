namespace GridironFrontOffice.Domain.Enums;


/// <summary>
/// The status of a season.
/// Flow: NotStarted -> OffSeason -> PreSeason -> InSeason -> PostSeason -> Completed -> Archived
/// </summary>
public enum SeasonStatus
{
	NotStarted = 0,
	OffSeason = 1,
	PreSeason = 2,
	InSeason = 3,
	PostSeason = 4,
	Completed = 5,
	Archived = 6
}