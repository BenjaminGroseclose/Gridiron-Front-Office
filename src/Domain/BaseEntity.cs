namespace GridironFrontOffice.Domain;

public class BaseEntity
{
	public DateTimeOffset CreateDate { get; set; }
	public DateTimeOffset UpdateDate { get; set; }

	public virtual int ID
	{
		get
		{
			throw new NotImplementedException();
		}
	}
}