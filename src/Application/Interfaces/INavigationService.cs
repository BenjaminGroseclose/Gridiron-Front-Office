namespace GridironFrontOffice.Application.Interfaces;

public interface INavigationService
{
	void NavigateTo(string route, Dictionary<string, object>? queryParameters = null);
	void NavigateBack();
	bool CanNavigateBack { get; }
}
