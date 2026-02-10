using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace GridironFrontOffice.Desktop;

public partial class App : Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage()) { Title = "Gridiron Front Office" };
	}
}
