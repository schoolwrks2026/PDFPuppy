using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using PdfUtility.App.ViewModels;
using PdfUtility.App.Views;
using PdfUtility.Core.Interfaces;
using PdfUtility.Services.Implementations;

namespace PdfUtility.App
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Setup local folder for logging
            var logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PDFUtility",
                "Logs"
            );
            Directory.CreateDirectory(logFolder);

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logFolder, "log-.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("PDF Utility application is starting up...");

            // Setup Dependency Injection Container
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                // Wire up Notification Service callbacks with desktop UI elements
                var notificationService = ServiceProvider.GetRequiredService<INotificationService>() as NotificationService;
                if (notificationService != null)
                {
                    notificationService.OnNotificationRaised += (type, title, message) =>
                    {
                        var icon = type == "Error" ? MessageBoxImage.Error :
                                   type == "Warning" ? MessageBoxImage.Warning :
                                   MessageBoxImage.Information;

                        MessageBox.Show(message, title, MessageBoxButton.OK, icon);
                    };

                    notificationService.ConfirmationHandler = (title, message) =>
                    {
                        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                        return result == MessageBoxResult.Yes;
                    };
                }

                // Retrieve and show MainWindow
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application failed to start MainWindow.");
                MessageBox.Show($"Failed to initialize PDF Utility: {ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Core Services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IHistoryService, HistoryService>();
            services.AddSingleton<IPdfMergeService, PdfMergeService>();
            services.AddSingleton<IFileConverterService, FileConverterService>();
            services.AddSingleton<IPdfSplitService, PdfSplitService>();
            services.AddSingleton<IPdfWatermarkService, PdfWatermarkService>();
            services.AddSingleton<IPdfCompressService, PdfCompressService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<INotificationService, NotificationService>();

            // ViewModels
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<ConvertViewModel>();
            services.AddSingleton<MergeViewModel>();
            services.AddSingleton<SplitViewModel>();
            services.AddSingleton<WatermarkViewModel>();
            services.AddSingleton<CompressViewModel>();
            services.AddSingleton<HistoryViewModel>();
            services.AddSingleton<SettingsViewModel>();

            // Views
            services.AddSingleton<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("PDF Utility application shutting down.");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
