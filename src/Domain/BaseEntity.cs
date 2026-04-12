using System.ComponentModel.DataAnnotations.Schema;

namespace GridironFrontOffice.Domain;

public class BaseEntity
{
	public DateTimeOffset CreateDate { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset? UpdateDate { get; set; } = null;

	[NotMapped]
	public virtual int ID
	{
		get
		{
			throw new NotImplementedException();
		}
	}
}