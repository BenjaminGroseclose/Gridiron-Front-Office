namespace GridironFrontOffice.Domain;

public class Division : BaseEntity
{
	public int DivisionID { get; set; }
	public string? Name { get; set; }
	public int ConferenceID { get; set; }

	public override int ID => DivisionID;
}