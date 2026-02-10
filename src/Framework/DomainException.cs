namespace GridironFrontOffice.Framework;

public class DomainException : Exception
{
	private readonly string _message;
	private readonly string _code;
	private readonly bool _isFatal;
	private readonly bool _retryable;
	public Dictionary<string, object> Details;

	public DomainException(string message, string code = "DOMAIN_ERROR", bool isFatal = false, bool retryable = false)
		: base(message)
	{
		_message = message;
		_code = code;
		_isFatal = isFatal;
		_retryable = retryable;
	}
}