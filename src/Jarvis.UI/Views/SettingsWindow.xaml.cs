using System.IO;
using System.Text.Json;
using System.Windows;
using Jarvis.Core.Config;

namespace Jarvis.UI.Views;

public partial class SettingsWindow : Window
{
    private readonly JarvisConfig _config;

    public SettingsWindow(JarvisConfig config)
    {
        InitializeComponent();
        _config = config;

        ProviderCombo.SelectedIndex = _config.AI.DefaultProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        GroqKeyBox.Password = _config.AI.Groq.ApiKey;
        GroqModelBox.Text = _config.AI.Groq.Model;
        OllamaModelBox.Text = _config.AI.Ollama.Model;

        VoiceEnabledBox.IsChecked = _config.Voice.Enabled;
        WakeWordBox.Text = _config.Voice.WakeWord;
        VoiceNameBox.Text = _config.Voice.Voice;

        ConfirmCommandsBox.IsChecked = _config.Security.RequireConfirmationForCommands;
        ConfirmWritesBox.IsChecked = _config.Security.RequireConfirmationForFileWrites;
        AllowSelfUpgradeBox.IsChecked = _config.Security.AllowSelfUpgrade;

        GitHubRepoBox.Text = _config.GitHub.Repo;
        GitHubTokenBox.Password = _config.GitHub.Token;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _config.AI.DefaultProvider = ((System.Windows.Controls.ComboBoxItem)ProviderCombo.SelectedItem)?.Content as string ?? "Groq";
        _config.AI.Groq.ApiKey = GroqKeyBox.Password;
        _config.AI.Groq.Model = GroqModelBox.Text.Trim();
        _config.AI.Ollama.Model = OllamaModelBox.Text.Trim();

        _config.Voice.Enabled = VoiceEnabledBox.IsChecked ?? true;
        _config.Voice.WakeWord = WakeWordBox.Text.Trim();
        _config.Voice.Voice = VoiceNameBox.Text.Trim();

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
