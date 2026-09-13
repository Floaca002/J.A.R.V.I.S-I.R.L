using System.IO;
using System.Windows;
using Jarvis.Core.AI;
using Jarvis.Core.Commands;
using Jarvis.Core.Config;
using Jarvis.Core.Memory;
using Jarvis.Core.Security;
using Jarvis.Core.Tools.Builtin;
using Jarvis.SelfUpgrade;
using Jarvis.SystemControl;
using Jarvis.UI.Services;
using Jarvis.UI.Tools;
using Jarvis.Voice;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Jarvis.UI;

public partial class App : Application
{
    public static AIOrchestrator Orchestrator { get; private set; } = null!;
    public static TextToSpeech Tts { get; private set; } = null!;
    public static SpeechToText Stt { get; private set; } = null!;
    public static JarvisConfig Config { get; private set; } = null!;
    public static SelfUpgradeEngine UpgradeEngine { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JarvisIRL");
        Directory.CreateDirectory(Path.Combine(dataDir, "logs"));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(dataDir, "logs", "jarvis-.log"), rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

        Config = LoadConfig();

        // Allow env vars to override secrets.
        if (string.IsNullOrWhiteSpace(Config.AI.Groq.ApiKey))
            Config.AI.Groq.ApiKey = Environment.GetEnvironmentVariable("JARVIS_GROQ_API_KEY") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Config.GitHub.Token))
            Config.GitHub.Token = Environment.GetEnvironmentVariable("JARVIS_GITHUB_TOKEN") ?? string.Empty;

        // Build system-control services.
        var fs = new FileSystemController();
        var apps = new AppController();
        var shell = new ShellExecutor();
        var automation = new AutomationController();
        var clipboard = new ClipboardController();
        IConfirmationService confirmation = new WpfConfirmationService();

        var dispatcher = new ToolDispatcher();
        dispatcher.Register(new ReadFileTool(fs));
        dispatcher.Register(new WriteFileTool(fs, Config.Security, confirmation));
        dispatcher.Register(new ListDirectoryTool(fs));
        dispatcher.Register(new CreateDirectoryTool(fs));
        dispatcher.Register(new DeletePathTool(fs, Config.Security, confirmation));
        dispatcher.Register(new OpenAppTool(apps));
        dispatcher.Register(new CloseAppTool(apps));
        dispatcher.Register(new ListProcessesTool(apps));
        dispatcher.Register(new OpenUrlTool(apps));
        dispatcher.Register(new RunShellTool(shell, Config.Security, confirmation));
        dispatcher.Register(new ScreenshotTool(automation));
        dispatcher.Register(new AdjustVolumeTool(automation));
        dispatcher.Register(new WindowControlTool(automation));
        dispatcher.Register(new TypeTextTool(automation));
        dispatcher.Register(new MouseClickTool(automation));
        dispatcher.Register(new GetClipboardTool(clipboard));
        dispatcher.Register(new SetClipboardTool(clipboard));

        var upgradeEngine = new SelfUpgradeEngine(dispatcher);
        UpgradeEngine = upgradeEngine;
        if (Config.Security.AllowSelfUpgrade)
        {
            dispatcher.Register(new UpgradeSelfTool(upgradeEngine, Config.Security, confirmation));
            dispatcher.Register(new RevertLastUpgradeTool(upgradeEngine));
        }

        var githubUpdater = new GitHubUpdater(Config.GitHub);
        dispatcher.Register(new CheckForUpdatesTool(githubUpdater));

        var memory = new ConversationMemory(Config.Memory);

        IAIProvider provider = Config.AI.DefaultProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
            ? new OllamaProvider(Config.AI.Ollama)
            : new GroqProvider(Config.AI.Groq);

        Orchestrator = new AIOrchestrator(provider, dispatcher, memory);

        Tts = new TextToSpeech(Config.Voice);
        Stt = new SpeechToText(Config.Voice);

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled UI exception");
            MessageBox.Show(args.Exception.Message, "Jarvis", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }

    private static JarvisConfig LoadConfig()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JarvisIRL", "secrets.json"), optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "JARVIS_");
        var cfg = new JarvisConfig();
        builder.Build().Bind(cfg);
        return cfg;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Tts?.Dispose();
        Stt?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
