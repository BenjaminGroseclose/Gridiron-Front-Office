namespace GridironFrontOffice.Domain;

public class Conference : BaseEntity
{
	public int ConferenceID { get; set; }
	public string Name { get; set; }
	public string Abbreviation { get; set; }

	public override int ID => ConferenceID;
}