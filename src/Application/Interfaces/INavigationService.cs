namespace GridironFrontOffice.Application.Interfaces;

public interface INavigationService
{
	void NavigateTo(string route);
	void NavigateBack();
	bool CanNavigateBack { get; }
}
