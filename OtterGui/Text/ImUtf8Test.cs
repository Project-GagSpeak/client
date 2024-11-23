using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace OtterGui.Text;

public class ImUtf8TestWindow : Window
{
    public class BetterStopWatch(long dataPoints = 100)
    {
        private readonly Stopwatch _watch = new();

        public void Start()
            => _watch.Restart();

        private          long   _totalElapsed;
        private          int    _totalMeasurements;
        private          int    _lastSlot;
        private readonly long[] _elapsed = new long[dataPoints];

        public void Measure()
        {
            var elapsed = _watch.ElapsedTicks;
            _elapsed[_lastSlot++] =  elapsed;
            _totalElapsed         += elapsed;
            ++_totalMeasurements;

            if (_lastSlot == _elapsed.Length)
                _lastSlot = 0;
        }

        private double RollingAverage()
        {
            var sum = _totalMeasurements >= _elapsed.Length
                ? _elapsed.Sum()
                : _elapsed.Take(_lastSlot).Sum();
            return (double)sum / TimeSpan.TicksPerMillisecond / Math.Min(_elapsed.Length, _totalMeasurements);
        }

        public void Draw(string pre)
        {
            ImUtf8.Text(
                $"[{pre}] Total Ticks: {_totalElapsed}, Total Frames: {_totalMeasurements}, Total Average: {_totalElapsed / (double)TimeSpan.TicksPerMillisecond / _totalMeasurements:F4}, Rolling Average: {RollingAverage():F4}");
        }
    }

    private readonly BetterStopWatch _watchImGui     = new();
    private readonly BetterStopWatch _watchUtf8      = new();
    private readonly BetterStopWatch _watchUtf16     = new();
    private readonly BetterStopWatch _watchFormatted = new();

    public ImUtf8TestWindow()
        : base("ImUtf8Test", ImGuiWindowFlags.AlwaysAutoResize)
        => IsOpen = true;

    private string _input = string.Empty;

    private bool _enableText = true;
    private bool _enableButton = true;
    private bool _enableInputText = true;

    public override void Draw()
    {
        ImUtf8.Checkbox("Enable Text"u8,       ref _enableText);
        ImUtf8.Checkbox("Enable Text Input"u8, ref _enableInputText);
        ImUtf8.Checkbox("Enable Button"u8,     ref _enableButton);

        _watchImGui.Start();
        if (_enableText)
            ImGui.TextUnformatted(
                "Test Text of some considerable length that I can only imagine to be as long as text will ever be in this world.");
        if (_enableButton)
            ImGui.Button("This is a button.##imgui");
        if (_enableInputText)
            ImGui.InputTextWithHint("Input##imgui", "Hint...", ref _input, 1024);
        _watchImGui.Measure();

        _watchUtf8.Start();
        if (_enableText)
            ImUtf8.Text("Test Text of some considerable length that I can only imagine to be as long as text will ever be in this world."u8);
        if (_enableButton)
            ImUtf8.Button("This is a button.##u8"u8);
        if (_enableInputText)
            ImUtf8.InputText("Input##u8"u8, ref _input, "Hint..."u8);
        _watchUtf8.Measure();

        _watchUtf16.Start();
        if (_enableText)
            ImUtf8.Text("Test Text of some considerable length that I can only imagine to be as long as text will ever be in this world.");
        if (_enableButton)
            ImUtf8.Button("This is a button.##u16");
        if (_enableInputText)
            ImUtf8.InputText("Input##u16", ref _input, "Hint...");
        _watchUtf16.Measure();

        _watchFormatted.Start();
        if (_enableText)
            ImUtf8.Text($"Test Text of some considerable length that I can only imagine to be as long as text will ever be in this world{'.'}");
        if (_enableButton)
            ImUtf8.Button($"This is a button.##{"formatted"u8}");
        if (_enableInputText)
            ImUtf8.InputText($"Input##{"formatted"u8}", ref _input, $"Hint..{'.'}");
        _watchFormatted.Measure();

        _watchImGui.Draw("ImGui");
        _watchUtf8.Draw("Utf8");
        _watchUtf16.Draw("Utf16");
        _watchFormatted.Draw("Format");
    }
}

public class ImUtf8Test
{
    public ImUtf8Test()
        => _utf8Clicked = new TestStruct(this, 0);

    private readonly int _randomNumber = Random.Shared.Next();

    private bool _utf8SelectableSelected;
    private bool _utf16SelectableSelected;
    private bool _formattedSelectableSelected;

    private TestStruct _utf8Clicked;
    private int        _utf16Clicked;
    private int        _formattedClicked;

    private int _testStructSpanFormatted;
    private int _testStructUtf8Formatted;
    private int _testStructToStringedFormat;
    private int _testStructToStringed;


    private string _testInput1 = string.Empty;
    private int    _testInput1Changes;
    private string _testInput2 = string.Empty;
    private int    _testInput2Changes;
    private string _testInput3 = string.Empty;
    private int    _testInput3Changes;


    private string _deactivateInput1 = string.Empty;
    private int    _deactivateInput1Changes;
    private string _deactivateInput2 = string.Empty;
    private int    _deactivateInput2Changes;
    private string _deactivateInput3 = string.Empty;
    private int    _deactivateInput3Changes;

    public void Draw()
    {
        ImUtf8.Text("This is UTF8."u8);
        ImUtf8.Text("This is UTF16.");
        ImUtf8.Text($"This is a formatted random number {_randomNumber}.");

        var utf8Size  = ImUtf8.CalcTextSize("UTF8"u8);
        var utf16Size = ImUtf8.CalcTextSize("UTF16");
        var fullSize  = ImUtf8.CalcTextSize($"UTF8: {utf8Size} UTF16: {utf16Size}", out var formatted);
        ImUtf8.Text(formatted);
        ImGui.SameLine();
        ImUtf8.TextRightAligned($"({fullSize})");

        if (ImUtf8.Selectable("UTF8 Selectable"u8, _utf8SelectableSelected))
            _utf8SelectableSelected = !_utf8SelectableSelected;

        if (ImUtf8.Selectable("UTF16 Selectable", _utf16SelectableSelected))
            _utf16SelectableSelected = !_utf16SelectableSelected;

        if (ImUtf8.Selectable($"Formatted Selectable ({(_formattedSelectableSelected ? "Selected" : "Unselected")})",
                _formattedSelectableSelected))
            _formattedSelectableSelected = !_formattedSelectableSelected;

        if (ImUtf8.Button("UTF8 Button"u8))
            ++_utf8Clicked;
        ImUtf8.HoverTooltip("UTF8 Tooltip"u8);
        ImGui.SameLine();
        ImUtf8.TextFrameAligned($"{_testStructSpanFormatted} Span Formats, {_testStructUtf8Formatted} UTF8 Formats");

        if (ImUtf8.Button("UTF16 Button"))
            ++_utf16Clicked;
        ImUtf8.HoverTooltip("UTF16 Tooltip");
        ImGui.SameLine();
        ImUtf8.TextFrameAligned($"{_testStructToStringed} ToStrings, {_testStructToStringedFormat} Formatted ToStrings");

        if (ImUtf8.Button($"Formatted Button ({_formattedClicked} times clicked)"))
            ++_formattedClicked;
        ImUtf8.HoverTooltip($"Formatted tooltip: Clicked UTF8 {_utf8Clicked} times and UTF16 {_utf16Clicked} times.");

        if (ImUtf8.InputText("UTF8 Label##UTF8"u8, ref _testInput1, "UTF8 Hint..."u8))
            ++_testInput1Changes;

        if (ImUtf8.InputText("UTF16 Label##UTF16", ref _testInput2, "UTF16 Hint..."))
            ++_testInput2Changes;

        if (ImUtf8.InputText($"Formatted Label##{_randomNumber}", ref _testInput3, $"Formatted Hint {_randomNumber}..."))
            ++_testInput3Changes;

        ImUtf8.Text($"{_testInput1Changes} UTF8 Changes, {_testInput2Changes} UTF16 Changes, {_testInput3Changes} Formatted Changes");

        if (ImUtf8.InputTextOnDeactivated("UTF8 Label##UTF8Deactivate"u8, ref _deactivateInput1, "UTF8 Hint..."u8))
            ++_deactivateInput1Changes;

        if (ImUtf8.InputTextOnDeactivated("UTF16 Label##UTF16Deactivate", ref _deactivateInput2, "UTF16 Hint..."))
            ++_deactivateInput2Changes;

        if (ImUtf8.InputTextOnDeactivated($"Formatted Label##{_randomNumber}Deactivate", ref _deactivateInput3))
            ++_deactivateInput3Changes;

        ImUtf8.Text(
            $"{_deactivateInput1Changes} UTF8 Changes, {_deactivateInput2Changes} UTF16 Changes, {_deactivateInput3Changes} Formatted Changes");
    }

    public record struct TestStruct(ImUtf8Test Parent, int Value) : IUtf8SpanFormattable, ISpanFormattable, IIncrementOperators<TestStruct>
    {
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            ++Parent._testStructToStringedFormat;
            return Value.ToString(format, formatProvider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            ++Parent._testStructSpanFormatted;
            return Value.TryFormat(destination, out charsWritten, format, provider);
        }

        public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            ++Parent._testStructUtf8Formatted;
            return Value.TryFormat(destination, out bytesWritten, format, provider);
        }

        public override readonly string ToString()
        {
            ++Parent._testStructToStringed;
            return Value.ToString();
        }

        public static TestStruct operator ++(TestStruct value)
            => value with { Value = value.Value + 1 };
    }
}
