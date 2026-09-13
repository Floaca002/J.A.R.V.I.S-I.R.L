using Avalonia.Controls;
using Avalonia.Interactivity;
using Jarvis.Core.Config;
using Jarvis.Voice;

namespace Jarvis.UI.Views;

public partial class SettingsWindow : Window
{
    private readonly JarvisConfig _config;

    public SettingsWindow()
    {
        InitializeComponent();
        _config = new JarvisConfig();
    }

    public SettingsWindow(JarvisConfig config) : this()
    {
        _config = config;

        ProviderCombo.SelectedIndex = _config.AI.DefaultProvider switch
        {
            var p when p.Equals("Ollama", StringComparison.OrdinalIgnoreCase) => 1,
            var p when p.Equals("Anthropic", StringComparison.OrdinalIgnoreCase) => 2,
            _ => 0
        };
        GroqKeyBox.Text = _config.AI.Groq.ApiKey;
        GroqModelBox.Text = _config.AI.Groq.Model;
        OllamaModelBox.Text = _config.AI.Ollama.Model;
        AnthropicKeyBox.Text = _config.AI.Anthropic.ApiKey;
        AnthropicModelBox.Text = _config.AI.Anthropic.Model;

        VisionAttachScreenBox.IsChecked = _config.Vision.AttachScreenToEveryMessage;

        VoiceEnabledBox.IsChecked = _config.Voice.Enabled;

        foreach (var voice in TextToSpeech.CommonEspeakVoices)
            EspeakVoiceCombo.Items.Add(voice);
        if (!string.IsNullOrWhiteSpace(_config.Voice.EspeakVoice) && !EspeakVoiceCombo.Items.Contains(_config.Voice.EspeakVoice))
            EspeakVoiceCombo.Items.Add(_config.Voice.EspeakVoice);
        EspeakVoiceCombo.SelectedItem = _config.Voice.EspeakVoice;
        if (EspeakVoiceCombo.SelectedItem == null && EspeakVoiceCombo.Items.Count > 0)
            EspeakVoiceCombo.SelectedIndex = 0;

        PiperModelPathBox.Text = _config.Voice.PiperModelPath;
        WhisperModelPathBox.Text = _config.Voice.WhisperModelPath;

        ConfirmCommandsBox.IsChecked = _config.Security.RequireConfirmationForCommands;
        ConfirmWritesBox.IsChecked = _config.Security.RequireConfirmationForFileWrites;
        AllowSelfUpgradeBox.IsChecked = _config.Security.AllowSelfUpgrade;

        GitHubRepoBox.Text = _config.GitHub.Repo;
        GitHubTokenBox.Text = _config.GitHub.Token;
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();

    private void OnPreviewVoice(object? sender, RoutedEventArgs e)
    {
        var previewCfg = new VoiceConfig
        {
            EspeakVoice = EspeakVoiceCombo.SelectedItem as string ?? "en-gb+m3",
            PiperModelPath = PiperModelPathBox.Text ?? string.Empty,
            Rate = _config.Voice.Rate,
            Volume = _config.Voice.Volume
        };
        Task.Run(() =>
        {
            try { TextToSpeech.Preview(previewCfg, "All systems online. How may I assist you, sir?"); }
            catch { /* preview is best-effort */ }
        });
    }

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        _config.AI.DefaultProvider = (ProviderCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "Groq";
        _config.AI.Groq.ApiKey = GroqKeyBox.Text ?? string.Empty;
        _config.AI.Groq.Model = (GroqModelBox.Text ?? string.Empty).Trim();
        _config.AI.Ollama.Model = (OllamaModelBox.Text ?? string.Empty).Trim();
        _config.AI.Anthropic.ApiKey = AnthropicKeyBox.Text ?? string.Empty;
        _config.AI.Anthropic.Model = (AnthropicModelBox.Text ?? string.Empty).Trim();

        _config.Vision.AttachScreenToEveryMessage = VisionAttachScreenBox.IsChecked ?? false;

        _config.Voice.Enabled = VoiceEnabledBox.IsChecked ?? true;
        _config.Voice.EspeakVoice = EspeakVoiceCombo.SelectedItem as string ?? _config.Voice.EspeakVoice;
        _config.Voice.PiperModelPath = (PiperModelPathBox.Text ?? string.Empty).Trim();
        _config.Voice.WhisperModelPath = (WhisperModelPathBox.Text ?? string.Empty).Trim();

        _config.Security.RequireConfirmationForCommands = ConfirmCommandsBox.IsChecked ?? true;
        _config.Security.RequireConfirmationForFileWrites = ConfirmWritesBox.IsChecked ?? true;
        _config.Security.AllowSelfUpgrade = AllowSelfUpgradeBox.IsChecked ?? true;

        _config.GitHub.Repo = (GitHubRepoBox.Text ?? string.Empty).Trim();
        _config.GitHub.Token = GitHubTokenBox.Text ?? string.Empty;

        try
        {
            var secretsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JarvisIRL", "secrets.json");
            Directory.CreateDirectory(Path.GetDirectoryName(secretsPath)!);
            var json = System.Text.Json.JsonSerializer.Serialize(_config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(secretsPath, json);

            await MessageDialog.ShowAsync("J.A.R.V.I.S", "Settings saved. Restart Jarvis for changes to take effect.");
            Close();
        }
        catch (Exception ex)
        {
            await MessageDialog.ShowAsync("J.A.R.V.I.S", $"Failed to save settings: {ex.Message}");
        }
    }
}
