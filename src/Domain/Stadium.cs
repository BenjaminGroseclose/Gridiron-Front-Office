namespace GridironFrontOffice.Domain;

public class Stadium : BaseEntity
{
	public int StadiumID { get; set; }
	public string? Name { get; set; }

	/// <summary>
	/// The seating capacity of the stadium.
	/// </summary>
	public int Capacity { get; set; }

	/// <summary>
	/// The location of the stadium. eg "Detroit, MI"
	/// </summary>
	public string? Location { get; set; }

	public override int ID => StadiumID;
}