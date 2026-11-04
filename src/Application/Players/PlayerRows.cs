using GridironFrontOffice.Core.IDs;
using GridironFrontOffice.Core.Players;

namespace GridironFrontOffice.Application.Players;

/// <summary>
/// A generated player as his Players row (F-03 §5), for every writer of new players: the league's creation (WP5) now,
/// each year's draft class later. The caller passes the IDs and the status, since only it knows the save's teams and
/// colleges: a free agent has no team, a prospect no team and no draft.
/// </summary>
public static class PlayerRows
{
	public static PlayerSnapshot ToSnapshot(this Player p)
	{
		ArgumentNullException.ThrowIfNull(p);

		var attributes = new Dictionary<PlayerAttribute, double>
		{
			// Physical
			[PlayerAttribute.Speed] = p.Speed,
			[PlayerAttribute.Acceleration] = p.Acceleration,
			[PlayerAttribute.Agility] = p.Agility,
			[PlayerAttribute.Strength] = p.Strength,
			[PlayerAttribute.Stamina] = p.Stamina,
			[PlayerAttribute.Toughness] = p.Toughness,

			// Mental
			[PlayerAttribute.Awareness] = p.Awareness,
			[PlayerAttribute.Discipline] = p.Discipline,

			// Passing
			[PlayerAttribute.ArmStrength] = p.ArmStrength,
			[PlayerAttribute.ShortAccuracy] = p.ShortAccuracy,
			[PlayerAttribute.MediumAccuracy] = p.MediumAccuracy,
			[PlayerAttribute.DeepAccuracy] = p.DeepAccuracy,
			[PlayerAttribute.ThrowOnTheRun] = p.ThrowOnTheRun,
			[PlayerAttribute.PocketPresence] = p.PocketPresence,
			[PlayerAttribute.DecisionMaking] = p.DecisionMaking,

			// Ball carrying
			[PlayerAttribute.BallSecurity] = p.BallSecurity,
			[PlayerAttribute.Vision] = p.Vision,
			[PlayerAttribute.Elusiveness] = p.Elusiveness,
			[PlayerAttribute.BreakTackle] = p.BreakTackle,

			// Receiving
			[PlayerAttribute.Hands] = p.Hands,
			[PlayerAttribute.RouteRunning] = p.RouteRunning,
			[PlayerAttribute.Release] = p.Release,
			[PlayerAttribute.ContestedCatch] = p.ContestedCatch,

			// Blocking
			[PlayerAttribute.PassBlock] = p.PassBlock,
			[PlayerAttribute.RunBlock] = p.RunBlock,
			[PlayerAttribute.LeadBlock] = p.LeadBlock,

			// Defense
			[PlayerAttribute.PassRushPower] = p.PassRushPower,
			[PlayerAttribute.PassRushFinesse] = p.PassRushFinesse,
			[PlayerAttribute.BlockShedding] = p.BlockShedding,
			[PlayerAttribute.Tackling] = p.Tackling,
			[PlayerAttribute.Pursuit] = p.Pursuit,
			[PlayerAttribute.HitPower] = p.HitPower,
			[PlayerAttribute.ManCoverage] = p.ManCoverage,
			[PlayerAttribute.ZoneCoverage] = p.ZoneCoverage,
			[PlayerAttribute.Press] = p.Press,
			[PlayerAttribute.BallSkills] = p.BallSkills,

			// Special teams
			[PlayerAttribute.KickPower] = p.KickPower,
			[PlayerAttribute.KickAccuracy] = p.KickAccuracy,
			[PlayerAttribute.PuntPower] = p.PuntPower,
			[PlayerAttribute.PuntAccuracy] = p.PuntAccuracy,
			[PlayerAttribute.Return] = p.Return,
			[PlayerAttribute.SnapAccuracy] = p.SnapAccuracy,
		};

		var personality = new Dictionary<PersonalityTrait, double>
		{
			[PersonalityTrait.WorkEthic] = p.WorkEthic,
			[PersonalityTrait.Coachability] = p.Coachability,
			[PersonalityTrait.Greed] = p.Greed,
			[PersonalityTrait.Loyalty] = p.Loyalty,
			[PersonalityTrait.Competitiveness] = p.Competitiveness,
			[PersonalityTrait.Ego] = p.Ego,
		};

		return new PlayerSnapshot(p.PlayerID, p.TeamID, p.Position, attributes, p.Potential, personality);
	}

	public static Player ToPlayer(this GeneratedPlayer p, PlayerID id, TeamID? team, PlayerStatus status, CollegeID college,
		TeamID? draftTeam)
	{
		ArgumentNullException.ThrowIfNull(p);

		return new Player
		{
			PlayerID = id,
			TeamID = team,
			Status = status,
			Position = p.Position,
			FirstName = p.FirstName,
			LastName = p.LastName,
			BirthDate = p.BirthDate,
			CollegeID = college,
			HometownName = p.Hometown.Name,
			HometownState = p.Hometown.State,
			Experience = p.Experience,
			DraftSeason = p.Draft?.Season,
			DraftRound = p.Draft?.Round,
			DraftPick = p.Draft?.Pick,
			DraftTeamID = draftTeam,
			JerseyNumber = p.JerseyNumber,

			// Measurables
			Height = p.Measurables[Measurable.Height],
			Weight = p.Measurables[Measurable.Weight],
			Forty = p.Measurables[Measurable.Forty],
			TenSplit = p.Measurables[Measurable.TenSplit],
			ThreeCone = p.Measurables[Measurable.ThreeCone],
			Shuttle = p.Measurables[Measurable.Shuttle],
			Bench = p.Measurables[Measurable.Bench],
			Vertical = p.Measurables[Measurable.Vertical],
			BroadJump = p.Measurables[Measurable.BroadJump],
			ArmLength = p.Measurables[Measurable.ArmLength],
			HandSize = p.Measurables[Measurable.HandSize],

			// Physical
			Speed = p.Ratings[PlayerAttribute.Speed],
			Acceleration = p.Ratings[PlayerAttribute.Acceleration],
			Agility = p.Ratings[PlayerAttribute.Agility],
			Strength = p.Ratings[PlayerAttribute.Strength],
			Stamina = p.Ratings[PlayerAttribute.Stamina],
			Toughness = p.Ratings[PlayerAttribute.Toughness],

			// Mental
			Awareness = p.Ratings[PlayerAttribute.Awareness],
			Discipline = p.Ratings[PlayerAttribute.Discipline],

			// Passing
			ArmStrength = p.Ratings[PlayerAttribute.ArmStrength],
			ShortAccuracy = p.Ratings[PlayerAttribute.ShortAccuracy],
			MediumAccuracy = p.Ratings[PlayerAttribute.MediumAccuracy],
			DeepAccuracy = p.Ratings[PlayerAttribute.DeepAccuracy],
			ThrowOnTheRun = p.Ratings[PlayerAttribute.ThrowOnTheRun],
			PocketPresence = p.Ratings[PlayerAttribute.PocketPresence],
			DecisionMaking = p.Ratings[PlayerAttribute.DecisionMaking],

			// Ball carrying
			BallSecurity = p.Ratings[PlayerAttribute.BallSecurity],
			Vision = p.Ratings[PlayerAttribute.Vision],
			Elusiveness = p.Ratings[PlayerAttribute.Elusiveness],
			BreakTackle = p.Ratings[PlayerAttribute.BreakTackle],

			// Receiving
			Hands = p.Ratings[PlayerAttribute.Hands],
			RouteRunning = p.Ratings[PlayerAttribute.RouteRunning],
			Release = p.Ratings[PlayerAttribute.Release],
			ContestedCatch = p.Ratings[PlayerAttribute.ContestedCatch],

			// Blocking
			PassBlock = p.Ratings[PlayerAttribute.PassBlock],
			RunBlock = p.Ratings[PlayerAttribute.RunBlock],
			LeadBlock = p.Ratings[PlayerAttribute.LeadBlock],

			// Defense
			PassRushPower = p.Ratings[PlayerAttribute.PassRushPower],
			PassRushFinesse = p.Ratings[PlayerAttribute.PassRushFinesse],
			BlockShedding = p.Ratings[PlayerAttribute.BlockShedding],
			Tackling = p.Ratings[PlayerAttribute.Tackling],
			Pursuit = p.Ratings[PlayerAttribute.Pursuit],
			HitPower = p.Ratings[PlayerAttribute.HitPower],
			ManCoverage = p.Ratings[PlayerAttribute.ManCoverage],
			ZoneCoverage = p.Ratings[PlayerAttribute.ZoneCoverage],
			Press = p.Ratings[PlayerAttribute.Press],
			BallSkills = p.Ratings[PlayerAttribute.BallSkills],

			// Special teams
			KickPower = p.Ratings[PlayerAttribute.KickPower],
			KickAccuracy = p.Ratings[PlayerAttribute.KickAccuracy],
			PuntPower = p.Ratings[PlayerAttribute.PuntPower],
			PuntAccuracy = p.Ratings[PlayerAttribute.PuntAccuracy],
			Return = p.Ratings[PlayerAttribute.Return],
			SnapAccuracy = p.Ratings[PlayerAttribute.SnapAccuracy],

			// Hidden
			Potential = p.Hidden.Potential,
			DevelopmentCurve = p.Hidden.DevelopmentCurve,
			BustRisk = p.Hidden.BustRisk,
			PeakAgeOffset = p.Hidden.PeakAgeOffset,
			InjuryProneness = p.Hidden.InjuryProneness,

			// Personality
			WorkEthic = p.Personality[PersonalityTrait.WorkEthic],
			Coachability = p.Personality[PersonalityTrait.Coachability],
			Greed = p.Personality[PersonalityTrait.Greed],
			Loyalty = p.Personality[PersonalityTrait.Loyalty],
			Competitiveness = p.Personality[PersonalityTrait.Competitiveness],
			Ego = p.Personality[PersonalityTrait.Ego],
		};
	}
}
