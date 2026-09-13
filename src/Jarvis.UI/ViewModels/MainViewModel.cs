using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Jarvis.UI.Commands;
using Jarvis.UI.Views;

namespace Jarvis.UI.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string _inputText = string.Empty;
    private string _statusText = "Idle";
    private IBrush _statusColor = Brushes.LimeGreen;
    private string _micLabel = "◉ MIC OFF";
    private bool _voiceOn;
    private Action? _scrollToBottom;

    public ObservableCollection<ChatBubble> Messages { get; } = new();
    public ObservableCollection<string> ToolNames { get; } = new();
    public ObservableCollection<string> UpgradeLog { get; } = new();

    public string InputText
    {
        get => _inputText;
        set { _inputText = value; OnPropertyChanged(); ((RelayCommand)SendCommand).RaiseCanExecuteChanged(); }
    }

    public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
    public IBrush StatusColor { get => _statusColor; set { _statusColor = value; OnPropertyChanged(); } }
    public string MicLabel { get => _micLabel; set { _micLabel = value; OnPropertyChanged(); } }
    public string ProviderName => App.Orchestrator?.ProviderName ?? "—";
    public bool VisionOn => App.Config?.Vision.AttachScreenToEveryMessage == true;
    public string VisionStatusText => VisionOn
        ? "👁 VISION: ON — screen shared every message"
        : "👁 VISION: OFF";
    public IBrush VisionStatusColor => VisionOn ? Brushes.Gold : Brushes.Gray;
    public bool HasUpgrades => UpgradeLog.Count > 0;
    public string SystemSummary =>
        $"OS: {Environment.OSVersion}\n" +
        $"User: {Environment.UserName}\n" +
        $"Cores: {Environment.ProcessorCount}\n" +
        $".NET: {Environment.Version}";

    public ICommand SendCommand { get; }
    public ICommand ToggleVoiceCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    public MainViewModel()
    {
        SendCommand = new RelayCommand(async _ => await SendAsync(), _ => !string.IsNullOrWhiteSpace(InputText));
        ToggleVoiceCommand = new RelayCommand(_ => ToggleVoice());
        OpenSettingsCommand = new RelayCommand(async _ => await OpenSettingsAsync());

        if (App.Orchestrator != null)
            foreach (var t in App.Orchestrator.AvailableTools)
                ToolNames.Add("▸ " + t.Name);
        RefreshUpgradeLog();

        // Initial greeting
        Messages.Add(new ChatBubble("JARVIS", "All systems online. How may I assist you, sir?", true));
        try { App.Tts?.Speak("All systems online. How may I assist you, sir?"); } catch { }

        if (App.Stt != null)
        {
            App.Stt.RecordingStarted += () => SetStatus("Listening… (click mic again to stop)", Brushes.Cyan);
            App.Stt.TranscriptionFailed += async msg =>
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Messages.Add(new ChatBubble("JARVIS", msg, true) { IsError = true });
                    SetStatus("Idle", Brushes.LimeGreen);
                    _voiceOn = false;
                    MicLabel = "◉ MIC OFF";
                });
            };
            App.Stt.CommandRecognized += async text =>
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _voiceOn = false;
                    MicLabel = "◉ MIC OFF";
                    InputText = text;
                });
                await SendAsync();
            };
        }
    }

    public void AttachScrollToBottom(Action scroll) => _scrollToBottom = scroll;

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;
        var text = InputText.Trim();
        InputText = string.Empty;

        Messages.Add(new ChatBubble("YOU", text, false));
        _scrollToBottom?.Invoke();

        string? screenshot = null;
        if (App.Config?.Vision.AttachScreenToEveryMessage == true)
        {
            SetStatus("Looking at your screen…", Brushes.Cyan);
            try { screenshot = App.Automation?.CaptureScreenPngBase64(); }
            catch { /* best effort — proceed without vision this turn */ }
        }
        SetStatus("Thinking…", Brushes.Orange);

        try
        {
            var reply = await App.Orchestrator.AskAsync(text, screenshot);
            Messages.Add(new ChatBubble("JARVIS", reply, true));
            _scrollToBottom?.Invoke();
            try { App.Tts?.Speak(reply); } catch { }
            SetStatus("Idle", Brushes.LimeGreen);

            // refresh tool list (self-upgrade may have added some)
            ToolNames.Clear();
            foreach (var t in App.Orchestrator.AvailableTools)
                ToolNames.Add("▸ " + t.Name);
            RefreshUpgradeLog();
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatBubble("ERROR", ex.Message, true) { IsError = true });
            SetStatus("Error", Brushes.OrangeRed);
        }
    }

    private void RefreshUpgradeLog()
    {
        UpgradeLog.Clear();
        if (App.UpgradeEngine != null)
            foreach (var record in App.UpgradeEngine.History.Reverse())
                UpgradeLog.Add($"{record.At:HH:mm:ss}  {record.ToolName}");
        OnPropertyChanged(nameof(HasUpgrades));
    }

    private async Task OpenSettingsAsync()
    {
        var win = new SettingsWindow(App.Config);
        var owner = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner != null)
            await win.ShowDialog(owner);
        else
            win.Show();
        OnPropertyChanged(nameof(VisionOn));
        OnPropertyChanged(nameof(VisionStatusText));
        OnPropertyChanged(nameof(VisionStatusColor));
    }

    private void ToggleVoice()
    {
        if (App.Stt == null) return;

        if (!App.Stt.IsAvailable)
        {
            Messages.Add(new ChatBubble("JARVIS",
                "Voice input needs whisper.cpp and a model configured in Settings (Whisper model path), plus `arecord` or `parecord`. See README.",
                true) { IsError = true });
            return;
        }

        _voiceOn = !_voiceOn;
        if (_voiceOn) { App.Stt.StartListening(); MicLabel = "◉ RECORDING…"; SetStatus("Listening…", Brushes.Cyan); }
        else          { App.Stt.StopListening();  MicLabel = "◉ MIC OFF"; SetStatus("Transcribing…", Brushes.Orange); }
    }

    private void SetStatus(string text, IBrush color)
    {
        StatusText = text;
        StatusColor = color;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class ChatBubble
{
    public string Role { get; }
    public string Content { get; }
    public bool IsJarvis { get; }
    public bool IsError { get; set; }

    public ChatBubble(string role, string content, bool isJarvis)
    {
        Role = role;
        Content = content;
        IsJarvis = isJarvis;
    }

    public HorizontalAlignment Align => IsJarvis ? HorizontalAlignment.Left : HorizontalAlignment.Right;
    public IBrush Background => IsJarvis
        ? new SolidColorBrush(Color.FromArgb(40, 0x22, 0xD3, 0xEE))
        : new SolidColorBrush(Color.FromArgb(40, 0xFF, 0xB3, 0x47));
    public IBrush Border => IsError
        ? Brushes.OrangeRed
        : IsJarvis ? Brushes.DeepSkyBlue : new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x47));
    public IBrush RoleColor => IsError
        ? Brushes.OrangeRed
        : IsJarvis ? Brushes.DeepSkyBlue : new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x47));
}
