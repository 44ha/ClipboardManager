using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ClipboardManager.App.Services;
using ClipboardManager.App.ViewModels;
using ClipboardManager.App.Views;
using ClipboardManager.Core.Services;
using ClipboardManager.Data.Context;
using ClipboardManager.Data.Repositories;
using ClipboardManager.Data.Services;
using ClipboardManager.Windows.Clipboard;
using ClipboardManager.Windows.Hotkey;

namespace ClipboardManager.App;

/// <summary>
/// Application entry point and host bootstrapper configuring Microsoft.Extensions.Hosting dependency injection.
/// Data files (database, settings, error log) are stored next to the executable
/// so no AppData / admin rights are required.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Store everything in a "Data" sub-folder next to the EXE
        string dataFolder = GetDataFolder();

        try
        {
            string dbPath       = Path.Combine(dataFolder, "clipboard.db");
            string settingsPath = Path.Combine(dataFolder, "settings.json");

            _host = new HostBuilder()
                // Use only console logging – avoids the Windows EventLog provider
                // that CreateDefaultBuilder adds (which needs admin write access).
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                })
                .ConfigureServices((_, services) =>
                {
                    // 1. Storage & Persistence Layer
                    services.AddDbContextFactory<ClipboardDbContext>(options =>
                        options.UseSqlite($"Data Source={dbPath}"));
                    services.AddSingleton<IClipboardHistoryRepository, SqliteClipboardHistoryRepository>();
                    services.AddSingleton<ISettingsService>(_ => new JsonSettingsService(settingsPath));

                    // 2. Win32 Interop Layer
                    services.AddSingleton<IClipboardMonitor, Win32ClipboardMonitor>();
                    services.AddSingleton<IHotkeyService, Win32HotkeyService>();

                    // 3. UI Services
                    services.AddSingleton<IClipboardPaster, WpfClipboardPaster>();
                    services.AddSingleton<IWindowService, WpfWindowService>();

                    // 4. ViewModels
                    services.AddSingleton<HistoryViewModel>();
                    services.AddSingleton<SettingsViewModel>();

                    // 5. Views
                    services.AddTransient<HistoryWindow>();
                    services.AddTransient<SettingsWindow>();
                })
                .Build();

            await _host.StartAsync();

            // Ensure SQLite database schema exists
            var contextFactory = _host.Services.GetRequiredService<IDbContextFactory<ClipboardDbContext>>();
            await using (var context = await contextFactory.CreateDbContextAsync())
            {
                await context.Database.EnsureCreatedAsync();
            }

            // Load persisted settings
            var settingsService = _host.Services.GetRequiredService<ISettingsService>();
            var settings        = await settingsService.LoadSettingsAsync();

            var windowService = _host.Services.GetRequiredService<IWindowService>();

            // Register global hotkey (default Ctrl + Shift + V)
            var hotkeyService = _host.Services.GetRequiredService<IHotkeyService>();
            hotkeyService.Register(settings.Hotkey, out _);
            hotkeyService.HotkeyPressed += (_, _) => windowService.ToggleHistoryWindow();



            // Start clipboard listener
            var monitor = _host.Services.GetRequiredService<IClipboardMonitor>();
            if (settings.IsMonitoringPaused) monitor.Pause();
            monitor.Start();

            // Show main window on startup
            windowService.ShowHistoryWindow();
        }
        catch (Exception ex)
        {
            // Write error log next to the EXE so it's always accessible
            try { File.WriteAllText(Path.Combine(dataFolder, "error.log"), ex.ToString()); }
            catch { /* ignore */ }

            MessageBox.Show(ex.ToString(), "Clipboard Manager Startup Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Returns (and creates if needed) the "Data" folder that lives next to the EXE.
    /// Falls back to the EXE's base directory itself if the sub-folder cannot be created.
    /// </summary>
    private static string GetDataFolder()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Data");
        try
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }
        catch
        {
            // Last resort – use the base directory itself
            return AppContext.BaseDirectory;
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            try
            {
                _host.Services.GetService<IHotkeyService>()?.Dispose();
                _host.Services.GetService<IClipboardMonitor>()?.Dispose();

                await _host.StopAsync();
                _host.Dispose();
            }
            catch
            {
                // Avoid crashing on shutdown
            }
        }

        base.OnExit(e);
    }
}
