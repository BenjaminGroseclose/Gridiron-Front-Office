namespace GridironFrontOffice.Domain;

public class BaseEntity
{
	public int ID { get; set; }
	public DateTimeOffset CreateDate { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset? UpdateDate { get; set; } = null;
}