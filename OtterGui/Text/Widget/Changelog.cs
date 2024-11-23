using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Text.Widget;

public enum ChangeLogDisplayType
{
    New,
    HighlightOnly,
    Never,
}

public sealed class Changelog : Window
{
    private readonly unsafe struct Utf8Literal
    {
        private readonly byte* _data;
        private readonly int   _length;

        public Utf8Literal(ReadOnlySpan<byte> span)
        {
            fixed (byte* ptr = span)
            {
                _data   = ptr;
                _length = span.Length;
            }
        }

        public ReadOnlySpan<byte> Get()
            => new(_data, _length);

        public int Length
            => _length;

        public static implicit operator ReadOnlySpan<byte>(in Utf8Literal lit)
            => lit.Get();

        public override string ToString()
            => Encoding.UTF8.GetString(Get());
    }

    public static ReadOnlySpan<byte> ToName(ChangeLogDisplayType type)
    {
        return type switch
        {
            ChangeLogDisplayType.New           => "Show New Changelogs (Recommended)"u8,
            ChangeLogDisplayType.HighlightOnly => "Only Show Important Changelogs"u8,
            ChangeLogDisplayType.Never         => "Never Show Changelogs (Dangerous)"u8,
            _                                  => ""u8,
        };
    }

    public const int FreshInstallVersion = int.MaxValue;

    public const uint DefaultHeaderColor    = 0xFF60D0D0;
    public const uint DefaultImportantColor = 0xFF6060FF;
    public const uint DefaultHighlightColor = 0xFFFF9090;

    private readonly Func<(int, ChangeLogDisplayType)> _getConfig;
    private readonly Action<int, ChangeLogDisplayType> _setConfig;

    private readonly List<(Utf8Literal Title, List<Entry> Entries, bool HasHighlight)> _entries = [];

    private          int                  _lastVersion;
    private          ChangeLogDisplayType _displayType;
    private readonly TerminatedByteString _headerName;

    public uint HeaderColor { get; set; } = DefaultHeaderColor;
    public bool ForceOpen   { get; set; }

    public Changelog(string label, Func<(int, ChangeLogDisplayType)> getConfig, Action<int, ChangeLogDisplayType> setConfig)
        : base(label, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize, true)
    {
        _headerName         = new TerminatedByteString(label.Split().FirstOrDefault(string.Empty));
        _getConfig          = getConfig;
        _setConfig          = setConfig;
        Position            = null;
        RespectCloseHotkey  = false;
        ShowCloseButton     = false;
        DisableWindowSounds = true;
    }

    public override void PreOpenCheck()
    {
        (_lastVersion, _displayType) = _getConfig();
        if (ForceOpen)
        {
            IsOpen = true;
            return;
        }

        if (_lastVersion == FreshInstallVersion)
        {
            IsOpen = false;
            _setConfig(_entries.Count, _displayType);
            return;
        }

        switch (_displayType)
        {
            case ChangeLogDisplayType.New:
                IsOpen = _lastVersion < _entries.Count;
                break;
            case ChangeLogDisplayType.HighlightOnly:
                IsOpen = _entries.Skip(_lastVersion).Any(t => t.HasHighlight);
                if (!IsOpen && _lastVersion < _entries.Count)
                    _setConfig(_entries.Count, ChangeLogDisplayType.Never);
                break;
            case ChangeLogDisplayType.Never:
                IsOpen = false;
                if (_lastVersion < _entries.Count)
                    _setConfig(_entries.Count, ChangeLogDisplayType.Never);
                break;
        }
    }

    public override void PreDraw()
    {
        Size = new Vector2(Math.Min(ImGui.GetMainViewport().Size.X / ImGuiHelpers.GlobalScale / 2, 800),
            ImGui.GetMainViewport().Size.Y / ImGuiHelpers.GlobalScale / 2);
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport((ImGui.GetMainViewport().Size - Size.Value * ImGuiHelpers.GlobalScale) / 2,
            ImGuiCond.Appearing);
    }

    public override void Draw()
    {
        DrawEntries();
        var pos = Size!.Value.X * ImGuiHelpers.GlobalScale / 3;
        ImGui.SetCursorPosX(pos);
        DrawDisplayTypeCombo(pos);
        ImGui.SetCursorPosX(pos);
        DrawUnderstoodButton(pos);
    }

    private void DrawEntries()
    {
        using var child = ImUtf8.Child("Entries"u8, new Vector2(-1, -ImGui.GetFrameHeight() * 3));
        if (!child)
            return;

        var i = 0;
        foreach (var ((name, list, hasHighlight), idx) in _entries.WithIndex().Reverse())
        {
            if (name.Length == 0)
                continue;

            using var id    = ImUtf8.PushId(i++);
            using var color = ImRaii.PushColor(ImGuiCol.Text, HeaderColor);
            var       flags = ImGuiTreeNodeFlags.NoTreePushOnOpen;


            // Do open the newest entry if it is the only new entry, if it has highlights or if no highlights are required
            var isOpen = idx == _entries.Count - 1
                ? idx == _lastVersion || _displayType != ChangeLogDisplayType.HighlightOnly || hasHighlight
                // Automatically open all entries that have not been seen, if they have highlights or do not require highlights
                : idx >= _lastVersion && (hasHighlight || _displayType != ChangeLogDisplayType.HighlightOnly);

            if (isOpen)
                flags |= ImGuiTreeNodeFlags.DefaultOpen;

            var tree = ImUtf8.TreeNode(name.Get(), flags);
            CopyToClipboard(_headerName, name, list);
            color.Pop();
            if (!tree)
                continue;

            foreach (var entry in list)
                entry.Draw();
        }
    }

    private void DrawDisplayTypeCombo(float width)
    {
        ImGui.SetNextItemWidth(width);
        using var combo = ImUtf8.Combo("##DisplayType"u8, ToName(_displayType));
        if (!combo)
            return;

        foreach (var type in Enum.GetValues<ChangeLogDisplayType>())
        {
            if (ImUtf8.Selectable(ToName(type)))
                _setConfig(_lastVersion, type);
        }
    }

    private void DrawUnderstoodButton(float width)
    {
        if (!ImUtf8.Button("Understood"u8, new Vector2(width, 0)))
            return;

        if (_lastVersion != _entries.Count)
            _setConfig(_entries.Count, _displayType);
        ForceOpen = false;
    }

    /// <summary> Only call with literals. </summary>
    public Changelog NextVersion(ReadOnlySpan<byte> title)
    {
        _entries.Add((new Utf8Literal(title), [], false));
        return this;
    }

    public Changelog RegisterImportant(ReadOnlySpan<byte> text, ushort level = 0, uint color = DefaultImportantColor)
    {
        var lastEntry = _entries.Last();
        lastEntry.Entries.Add(new Entry(text, color, level));
        _entries[^1] = lastEntry with { HasHighlight = true };
        return this;
    }

    public Changelog RegisterEntry(ReadOnlySpan<byte> text, ushort level = 0)
    {
        _entries.Last().Entries.Add(new Entry(text, 0, level));
        return this;
    }

    public Changelog RegisterHighlight(ReadOnlySpan<byte> text, ushort level = 0, uint color = DefaultHighlightColor)
    {
        _entries.Last().Entries.Add(new Entry(text, color, level));
        return this;
    }

    private readonly struct Entry(ReadOnlySpan<byte> text, uint color = 0, ushort subText = 0)
    {
        public readonly Utf8Literal Text    = new(text);
        public readonly uint        Color   = color;
        public readonly ushort      SubText = subText;

        public void Draw()
        {
            using var tab   = ImRaii.PushIndent(1 + SubText);
            using var color = ImRaii.PushColor(ImGuiCol.Text, Color, Color != 0);
            ImGui.Bullet();
            ImGui.PushTextWrapPos();
            ImUtf8.Text(Text.Get());
            ImGui.PopTextWrapPos();
        }

        public void Append(StringBuilder sb)
        {
            for (var i = 0; i < SubText; ++i)
                sb.Append("  ");

            sb.Append("- ");
            if (Color != 0)
                sb.Append("**");

            sb.Append(Text);
            if (Color != 0)
                sb.Append("**");

            sb.Append('\n');
        }
    }

    [Conditional("DEBUG")]
    private static void CopyToClipboard(TerminatedByteString label, Utf8Literal name, List<Entry> entries)
    {
        try
        {
            if (!ImGui.IsItemClicked(ImGuiMouseButton.Right))
                return;

            var sb = new StringBuilder(1024 * 64);
            if (label.Length > 0)
                sb.Append("# ").Append(label).Append('\n');

            sb.Append("## ")
                .Append(name)
                .Append(" notes, Update <t:")
                .Append(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                .Append(">\n");

            foreach (var entry in entries)
                entry.Append(sb);

            ImGui.SetClipboardText(sb.ToString());
        }
        catch
        {
            // ignored
        }
    }
}
