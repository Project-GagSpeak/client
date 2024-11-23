using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using ImGuiNET;
using OtterGui.Log;

namespace OtterGui.Classes;

public class Notification : MessageService.IMessage
{
    public NotificationType NotificationType { get; }

    public string NotificationMessage { get; }

    public uint NotificationDuration { get; }

    private readonly string     _content2;
    private readonly Exception? _ex;

    public Notification(string content, NotificationType type, uint duration = 5000)
    {
        NotificationType     = type;
        NotificationMessage  = content;
        _content2            = string.Empty;
        _ex                  = null;
        NotificationDuration = duration;
    }

    public Notification(Exception ex, string content1, string content2, NotificationType type, uint duration = 5000)
    {
        NotificationType     = type;
        NotificationMessage  = content1;
        _content2            = content2;
        _ex                  = ex;
        NotificationDuration = duration;
    }

    public string LogMessage
        => _ex != null ? $"[{NotificationType}] {_content2}:\n{_ex}" : $"[{NotificationType}] {NotificationMessage}";

    public SeString ChatMessage
        => SeString.Empty;

    public string PrintMessage
        => NotificationMessage;

    public string PrintTooltip
        => _ex?.ToString() ?? string.Empty;
}

public class MessageService(Logger log, IUiBuilder builder, IChatGui chat, INotificationManager notificationManager)
    : IReadOnlyDictionary<DateTime, MessageService.IMessage>
{
    public interface IMessage
    {
        public NotificationType NotificationType
            => NotificationType.None;

        public string Message
            => string.Empty;

        public string NotificationMessage
            => Message;

        public string NotificationTitle
            => NotificationType.ToString();

        public uint NotificationDuration
            => 3000;

        public string LogMessage
            => Message;

        public SeString ChatMessage
            => Message;


        public Logger.LogLevel LogLevel
            => NotificationType switch
            {
                NotificationType.None    => Logger.LogLevel.Verbose,
                NotificationType.Success => Logger.LogLevel.Verbose,
                NotificationType.Warning => Logger.LogLevel.Information,
                NotificationType.Error   => Logger.LogLevel.Error,
                NotificationType.Info    => Logger.LogLevel.Debug,
                _                        => Logger.LogLevel.Verbose,
            };

        public string PrintMessage
            => Message;

        public string PrintTooltip
            => Message;
    }

    public readonly Logger               Log                 = log;
    public readonly IUiBuilder           Builder             = builder;
    public readonly INotificationManager NotificationManager = notificationManager;
    public readonly IChatGui             Chat                = chat;

    private readonly SortedDictionary<DateTime, IMessage> _messages   = [];
    private          DateTime                             _deleteTime = DateTime.MinValue;
    private          Vector2                              _buttonSize;

    public void AddMessage(IMessage message, bool doPrint = true, bool doNotify = true, bool doLog = true, bool doChat = false)
    {
        var time         = DateTime.UtcNow;
        var printMessage = message.PrintMessage;
        if (doPrint && printMessage.Length > 0)
            lock (_messages)
            {
                while (!_messages.TryAdd(time, message))
                    time = time.AddTicks(1);
            }

        var logMessage = message.LogMessage;
        if (doLog && logMessage.Length > 0)
            Log.Message(message.LogLevel, message.LogMessage);

        var notificationMessage = message.NotificationMessage;
        if (doNotify && notificationMessage.Length > 0)
            NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification()
            {
                Content         = message.NotificationMessage,
                Title           = message.NotificationTitle,
                Type            = message.NotificationType,
                Minimized       = false,
                InitialDuration = TimeSpan.FromMilliseconds(message.NotificationDuration),
            });

        var chatMessage = message.ChatMessage;
        if (doChat && chatMessage.Payloads.Count > 0)
            Chat.Print(chatMessage);
    }

    public void Draw()
    {
        _buttonSize = new Vector2(ImGui.GetFrameHeight());
        _deleteTime = DateTime.MinValue;

        using var table = ImRaii.Table("errors", 5, ImGuiTableFlags.RowBg);
        ImGui.TableSetupColumn("##del",   ImGuiTableColumnFlags.WidthFixed, _buttonSize.X);
        ImGui.TableSetupColumn("Time",    ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("00:00:00.0000").X);
        ImGui.TableSetupColumn("##icon",  ImGuiTableColumnFlags.WidthFixed, _buttonSize.X);
        ImGui.TableSetupColumn("##multi", ImGuiTableColumnFlags.WidthFixed, _buttonSize.X);
        ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableHeadersRow();
        ImGui.TableNextRow();


        lock (_messages)
        {
            var height    = ImGui.GetFrameHeightWithSpacing();
            var skips     = ImGuiClip.GetNecessarySkips(height);
            var remainder = ImGuiClip.ClippedDraw(_messages.WithIndex(), skips, PrintMessage, _messages.Count);
            ImGuiClip.DrawEndDummy(remainder, height);

            if (_deleteTime != DateTime.MinValue)
            {
                _messages.Remove(_deleteTime);
                _deleteTime = DateTime.MinValue;
            }
        }
    }

    private void PrintMessage(ValueTuple<KeyValuePair<DateTime, IMessage>, int> message)
    {
        using var id = ImRaii.PushId(message.Item2);
        ImGui.TableNextColumn();
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), _buttonSize, "Remove this from the list.", false, true))
            _deleteTime = message.Item1.Key;

        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(message.Item1.Key.ToLocalTime().ToString("HH:mm:ss.fff"));

        ImGui.TableNextColumn();
        using (var font = ImRaii.PushFont(UiBuilder.IconFont))
        {
            var (icon, color) = message.Item1.Value.NotificationType switch
            {
                NotificationType.None    => (FontAwesomeIcon.None, 0u),
                NotificationType.Success => (FontAwesomeIcon.CheckCircle, 0xFF40FF40),
                NotificationType.Warning => (FontAwesomeIcon.ExclamationCircle, 0xFF40FFFF),
                NotificationType.Error   => (FontAwesomeIcon.TimesCircle, 0xFF4040FF),
                NotificationType.Info    => (FontAwesomeIcon.QuestionCircle, 0xFFFF4040),
                _                        => (FontAwesomeIcon.None, 0u),
            };
            using var c = ImRaii.PushColor(ImGuiCol.Text, color);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(icon.ToIconString());
        }

        var text      = message.Item1.Value.PrintMessage;
        var firstLine = text;
        var newLine   = text.IndexOf('\n');
        var tooltip   = message.Item1.Value.PrintTooltip;

        if (newLine >= 0)
        {
            firstLine = text[..newLine];
            tooltip   = $"{text[(newLine + 1)..]}\n\n{tooltip}";
        }

        ImGui.TableNextColumn();
        if (tooltip.Length > 0)
        {
            ImGui.AlignTextToFramePadding();
            ImGuiComponents.HelpMarker(text);
        }

        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(firstLine);
        ImGuiUtil.HoverTooltip(tooltip);
    }

    public IEnumerator<KeyValuePair<DateTime, IMessage>> GetEnumerator()
        => _messages.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _messages.Count;

    public bool ContainsKey(DateTime key)
        => _messages.ContainsKey(key);

    public bool TryGetValue(DateTime key, [NotNullWhen(true)] out IMessage? value)
        => _messages.TryGetValue(key, out value);

    public IMessage this[DateTime key]
        => _messages[key];

    public IEnumerable<DateTime> Keys
        => _messages.Keys;

    public IEnumerable<IMessage> Values
        => _messages.Values;
}
