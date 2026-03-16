using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using GridironFrontOffice.Persistence.Injection;
using GridironFrontOffice.Application.Injection;
using GridironFrontOffice.UI.Injection;
using Serilog;

namespace GridironFrontOffice.Desktop;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var logPath = Path.Combine(FileSystem.AppDataDirectory, "logs", "gfo-.log");

		Log.Logger = new LoggerConfiguration()
#if DEBUG
			.MinimumLevel.Debug()
			.WriteTo.Debug()
#else
			.MinimumLevel.Warning()
#endif
			.WriteTo.File(
				logPath,
				rollingInterval: RollingInterval.Day,
				retainedFileCountLimit: 7,
				outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddMudServices();

		builder.Logging.ClearProviders();
		builder.Logging.AddSerilog(dispose: true);

		PersistenceInjection.Configure(builder.Services);
		ApplicationInjection.Configure(builder.Services);
		UIInjection.Configure(builder.Services);

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
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

	private static void ConfigureGlobalExceptionHandling(Microsoft.Extensions.Logging.ILogger logger)
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
		{
			var ex = e.ExceptionObject as Exception;
			if (ex != null)
			{
				logger?.LogError(ex, "Unhandled exception occurred");
				Log.CloseAndFlush(); // ensure logs are written before process exits
			}
		};

		TaskScheduler.UnobservedTaskException += (sender, e) =>
		{
			logger.LogError(e.Exception, "Unobserved task exception");
			e.SetObserved();
		};
	}
}
