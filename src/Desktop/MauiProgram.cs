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

		var app = builder.Build();

		// Setup Global exception handling
		var logger = app.Services.GetService<ILogger<App>>();

		if (logger == null)
		{
			Console.WriteLine("Warning: Logger service not found. Global exception handling will not log exceptions.");
		}
		else
		{
			ConfigureGlobalExceptionHandling(logger);
		}

		return app;
	}


	private static void ConfigureGlobalExceptionHandling(ILogger logger)
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
		{
			var ex = e.ExceptionObject as Exception;
			if (ex != null)
			{
				// Log the exception using your logging framework
				logger?.LogError(ex, "Unhandled exception occurred");

				// Optionally, show a user-friendly message or perform other actions
			}
		};
	}
}
