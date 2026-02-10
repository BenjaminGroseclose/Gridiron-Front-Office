using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using GridironFrontOffice.Persistence.Injection;
using GridironFrontOffice.Application.Injection;
using GridironFrontOffice.UI.Injection;

namespace GridironFrontOffice.Desktop;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddMudServices();

		PersistenceInjection.Configure(builder.Services);
		ApplicationInjection.Configure(builder.Services);
		UIInjection.Configure(builder.Services);

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
		builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

		return builder.Build();
	}
}
