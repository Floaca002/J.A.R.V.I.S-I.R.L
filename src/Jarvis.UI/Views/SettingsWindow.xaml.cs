using System.IO;
using System.Text.Json;
using System.Windows;
using Jarvis.Core.Config;
using Jarvis.Voice;

namespace Jarvis.UI.Views;

public partial class SettingsWindow : Window
{
    private readonly JarvisConfig _config;

    public SettingsWindow(JarvisConfig config)
    {
        InitializeComponent();
        _config = config;

        ProviderCombo.SelectedIndex = _config.AI.DefaultProvider switch
        {
            var p when p.Equals("Ollama", StringComparison.OrdinalIgnoreCase) => 1,
            var p when p.Equals("Anthropic", StringComparison.OrdinalIgnoreCase) => 2,
            _ => 0
        };
        GroqKeyBox.Password = _config.AI.Groq.ApiKey;
        GroqModelBox.Text = _config.AI.Groq.Model;
        OllamaModelBox.Text = _config.AI.Ollama.Model;
        AnthropicKeyBox.Password = _config.AI.Anthropic.ApiKey;
        AnthropicModelBox.Text = _config.AI.Anthropic.Model;

        VisionAttachScreenBox.IsChecked = _config.Vision.AttachScreenToEveryMessage;

        VoiceEnabledBox.IsChecked = _config.Voice.Enabled;
        WakeWordBox.Text = _config.Voice.WakeWord;

        try
        {
            foreach (var name in TextToSpeech.ListInstalledVoiceNames())
                VoiceNameCombo.Items.Add(name);
        }
        catch { /* voice enumeration is best-effort */ }
        if (!string.IsNullOrWhiteSpace(_config.Voice.Voice) && !VoiceNameCombo.Items.Contains(_config.Voice.Voice))
            VoiceNameCombo.Items.Add(_config.Voice.Voice);
        VoiceNameCombo.SelectedItem = _config.Voice.Voice;
        if (VoiceNameCombo.SelectedItem == null && VoiceNameCombo.Items.Count > 0)
            VoiceNameCombo.SelectedIndex = 0;

        ConfirmCommandsBox.IsChecked = _config.Security.RequireConfirmationForCommands;
        ConfirmWritesBox.IsChecked = _config.Security.RequireConfirmationForFileWrites;
        AllowSelfUpgradeBox.IsChecked = _config.Security.AllowSelfUpgrade;

        GitHubRepoBox.Text = _config.GitHub.Repo;
        GitHubTokenBox.Password = _config.GitHub.Token;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void OnPreviewVoice(object sender, RoutedEventArgs e)
    {
        var voiceName = VoiceNameCombo.SelectedItem as string;
        var rate = _config.Voice.Rate;
        var volume = _config.Voice.Volume;
        Task.Run(() =>
        {
            try { TextToSpeech.Preview(voiceName, rate, volume, "All systems online. How may I assist you, sir?"); }
            catch { /* preview is best-effort */ }
        });
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _config.AI.DefaultProvider = ((System.Windows.Controls.ComboBoxItem)ProviderCombo.SelectedItem)?.Content as string ?? "Groq";
        _config.AI.Groq.ApiKey = GroqKeyBox.Password;
        _config.AI.Groq.Model = GroqModelBox.Text.Trim();
        _config.AI.Ollama.Model = OllamaModelBox.Text.Trim();
        _config.AI.Anthropic.ApiKey = AnthropicKeyBox.Password;
        _config.AI.Anthropic.Model = AnthropicModelBox.Text.Trim();

        _config.Vision.AttachScreenToEveryMessage = VisionAttachScreenBox.IsChecked ?? false;

        _config.Voice.Enabled = VoiceEnabledBox.IsChecked ?? true;
        _config.Voice.WakeWord = WakeWordBox.Text.Trim();
        _config.Voice.Voice = VoiceNameCombo.SelectedItem as string ?? _config.Voice.Voice;

        _config.Security.RequireConfirmationForCommands = ConfirmCommandsBox.IsChecked ?? true;
        _config.Security.RequireConfirmationForFileWrites = ConfirmWritesBox.IsChecked ?? true;
        _config.Security.AllowSelfUpgrade = AllowSelfUpgradeBox.IsChecked ?? true;

        _config.GitHub.Repo = GitHubRepoBox.Text.Trim();
        _config.GitHub.Token = GitHubTokenBox.Password;

        try
        {
            var secretsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JarvisIRL", "secrets.json");
            Directory.CreateDirectory(Path.GetDirectoryName(secretsPath)!);
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(secretsPath, json);

            MessageBox.Show(
                "Settings saved. Restart Jarvis for changes to take effect.",
                "J.A.R.V.I.S", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "J.A.R.V.I.S", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
