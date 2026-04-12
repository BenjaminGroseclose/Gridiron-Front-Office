namespace GridironFrontOffice.Framework;

public class DomainException : Exception
{
	private readonly string _message;
	private readonly string _code;
	public Dictionary<string, object> Details;
	public string Code => _code;
	public override string Message => _message;

	public DomainException(string message, string code = "DOMAIN_ERROR")
		: base(message)
	{
		_message = message;
		_code = code;
	}

	public DomainException(string message, string code, Exception innerException)
		: base(message, innerException)
	{
		_message = message;
		_code = code;
	}
}