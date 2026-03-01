using System.ComponentModel.DataAnnotations.Schema;

namespace GridironFrontOffice.Domain;

public class BaseEntity
{
	public DateTimeOffset CreateDate { get; set; }
	public DateTimeOffset UpdateDate { get; set; }

	[NotMapped]
	public virtual int ID
	{
		get
		{
			throw new NotImplementedException();
		}
	}
}