using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Librarium.Json;
using XVTwiddle.Diagnostics;
using XVTwiddle.Converters;
using XVTwiddle.Json;
using XVTwiddle.ObjectModel;
using XVTwiddle.Windows;
using ReactiveUI;
using Serilog;
using Splat;

namespace XVTwiddle
{
    /// <summary>
    /// Houses global application data and the entry point of the application.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Gets the metadata for the application.
        /// </summary>
        public static Meta Metadata { get; } = new Meta();

        /// <summary>
        /// Gets the currently loaded preferences for the user.
        /// </summary>
        public static Preferences Preferences { get; set; } = null!;

        /// <summary>
        /// When the application ends from anywhere, the log needs to be closed and flushed.
        /// </summary>
        [Log("XVTwiddle will now flush the logger and shut down.")]
        private static void OnExit(object? sender, EventArgs args) => Log.CloseAndFlush();

        /// <summary>
        /// Serves as the application entry point.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="args">
        /// Contains the arguments pertaining to the application startup event.
        /// </param>
        private void AppStartup(object? sender, StartupEventArgs args)
        {
            DefineDebugFlag();

            InitializeLogging();

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnExit);

            EnterProgram().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Enters the program.
        /// </summary>
        [Log("Starting...", "XVTwiddle is now running.", "An unhandled exception has occurred.")]
        private static async Task EnterProgram()
        {
            UpdateManager.Initialize();

            await InitializePreferences();

            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());

            Locator.CurrentMutable.RegisterConstant(new TextToFlowDocumentConverter(),
                typeof(IBindingTypeConverter));

            new MainWindow().Show();
        }

        /// <summary>
        /// Loads the Preferences from disk or creates default preferences if none exists.
        /// </summary>
        [Log("Initializing Preferences...", "Preferences initialization complete.")]
        private static async Task InitializePreferences()
        {
            if (!Directory.Exists(Metadata.AppDataDirectory))
            {
                Directory.CreateDirectory(Metadata.AppDataDirectory);
            }

            Preferences = JFile.Load<PreferencesFile>(Metadata.AppDataDirectory, "preferences.json").CreateModel();
            Preferences.Save();
            await Preferences.Load();
        }

        /// <summary>
        /// Initializes the logging manager and logs the welcome messages.
        /// </summary>
        private static void InitializeLogging()
        {
            LogManager.Initialize();

            Log.Information("Welcome to XVTwiddle Version {Version}.", Metadata.Version);

            if (Metadata.IsDebug)
            {
                Log.Warning("This assembly is running in DEBUG mode.");
            }
        }

        /// <summary>
        /// Defines the flag that determines whether or not the program is running in debug mode.
        /// </summary>
        private static void DefineDebugFlag()
        {
#if DEBUG
            Metadata.IsDebug = true;
#endif
        }
    }
}