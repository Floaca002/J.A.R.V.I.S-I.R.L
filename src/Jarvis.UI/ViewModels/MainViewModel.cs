using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Jarvis.UI.Commands;

namespace Jarvis.UI.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string _inputText = string.Empty;
    private string _statusText = "Idle";
    private Brush _statusColor = Brushes.LimeGreen;
    private string _micLabel = "◉ MIC OFF";
    private bool _voiceOn;
    private Action? _scrollToBottom;

    public ObservableCollection<ChatBubble> Messages { get; } = new();
    public ObservableCollection<string> ToolNames { get; } = new();

    public string InputText
    {
        get => _inputText;
        set { _inputText = value; OnPropertyChanged(); ((RelayCommand)SendCommand).RaiseCanExecuteChanged(); }
    }

    public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
    public Brush StatusColor { get => _statusColor; set { _statusColor = value; OnPropertyChanged(); } }
    public string MicLabel { get => _micLabel; set { _micLabel = value; OnPropertyChanged(); } }
    public string ProviderName => App.Orchestrator?.ProviderName ?? "—";
    public string SystemSummary =>
        $"OS: {Environment.OSVersion}\n" +
        $"User: {Environment.UserName}\n" +
        $"Cores: {Environment.ProcessorCount}\n" +
        $".NET: {Environment.Version}";

    public ICommand SendCommand { get; }
    public ICommand ToggleVoiceCommand { get; }

    public MainViewModel()
    {
        SendCommand = new RelayCommand(async _ => await SendAsync(), _ => !string.IsNullOrWhiteSpace(InputText));
        ToggleVoiceCommand = new RelayCommand(_ => ToggleVoice());

        if (App.Orchestrator != null)
            foreach (var t in App.Orchestrator.AvailableTools)
                ToolNames.Add("▸ " + t.Name);

        // Initial greeting
        Messages.Add(new ChatBubble("JARVIS", "All systems online. How may I assist you, sir?", true));
        try { App.Tts?.Speak("All systems online. How may I assist you, sir?"); } catch { }

        if (App.Stt != null)
        {
            App.Stt.WakeWordDetected += () => SetStatus("Listening…", Brushes.Cyan);
            App.Stt.CommandRecognized += async text =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
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
        SetStatus("Thinking…", Brushes.Orange);

        try
        {
            var reply = await App.Orchestrator.AskAsync(text);
            Messages.Add(new ChatBubble("JARVIS", reply, true));
            _scrollToBottom?.Invoke();
            try { App.Tts?.Speak(reply); } catch { }
            SetStatus("Idle", Brushes.LimeGreen);

            // refresh tool list (self-upgrade may have added some)
            ToolNames.Clear();
            foreach (var t in App.Orchestrator.AvailableTools)
                ToolNames.Add("▸ " + t.Name);
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatBubble("ERROR", ex.Message, true) { IsError = true });
            SetStatus("Error", Brushes.OrangeRed);
        }
    }

    private void ToggleVoice()
    {
        if (App.Stt == null) return;
        _voiceOn = !_voiceOn;
        if (_voiceOn) { App.Stt.StartListening(); MicLabel = "◉ MIC ON"; SetStatus("Wake-word armed", Brushes.Cyan); }
        else          { App.Stt.StopListening();  MicLabel = "◉ MIC OFF"; SetStatus("Idle", Brushes.LimeGreen); }
    }

    private void SetStatus(string text, Brush color)
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
    public Brush Background => IsJarvis
        ? new SolidColorBrush(Color.FromArgb(40, 0x22, 0xD3, 0xEE))
        : new SolidColorBrush(Color.FromArgb(40, 0xFF, 0xB3, 0x47));
    public Brush Border => IsError
        ? Brushes.OrangeRed
        : IsJarvis ? Brushes.DeepSkyBlue : (Brush)new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x47));
    public Brush RoleColor => IsError
        ? Brushes.OrangeRed
        : IsJarvis ? Brushes.DeepSkyBlue : (Brush)new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x47));
}
