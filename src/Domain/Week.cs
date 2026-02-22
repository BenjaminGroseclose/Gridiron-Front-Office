namespace GridironFrontOffice.Domain;

public class Week : BaseEntity
{
	public int WeekID { get; set; }

	public int SeasonID { get; set; }

	/// <summary>
	/// The name of the week. eg "Week 1", "Preseason Week 2", "Playoffs Round 1"
	/// </summary>
	public string Name { get; set; }

	public IEnumerable<Game> Games { get; set; }

	public override int ID
	{
		get => WeekID;
	}
}