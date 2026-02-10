namespace GridironFrontOffice.Desktop;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		try
		{
			InitializeComponent();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error initializing MainPage: {ex.Message}");
		}
	}
}
