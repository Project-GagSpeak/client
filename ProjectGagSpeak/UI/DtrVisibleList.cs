using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using CkCommons;

namespace GagSpeak.Gui.Components;

// So sloppy right now lol.
internal class DtrVisibleWindow : WindowMediatorSubscriberBase
{
    private readonly DtrBarService _service;
    private bool ThemePushed = false;
    public DtrVisibleWindow(ILogger<DtrVisibleWindow> logger, GagspeakMediator mediator,
        DtrBarService dtrService) : base(logger, mediator, "##DtrLinker")
    {
        _service = dtrService;

        Flags = WFlags.NoCollapse | WFlags.NoTitleBar | WFlags.NoResize | WFlags.NoScrollbar;
    }

    private nint _selectedAddr = nint.Zero;
    private Vector2 _lastPos = Vector2.Zero;
    public override void OnOpen() => _lastPos = ImGui.GetMousePos();

    protected override void PreDrawInternal() 
    {
        var posX = _lastPos.X - 100;
        var posY = _lastPos.Y + ImGui.GetFrameHeight();
        ImGui.SetNextWindowPos(new Vector2(posX, posY));

        Flags |= WFlags.NoMove;

        var cnt = DtrBarService.NonKinksters.Count > 10 ? 10+2 : DtrBarService.NonKinksters.Count+2;
        var size = new Vector2(200f, (ImGui.GetTextLineHeightWithSpacing() * cnt) - ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y + ImGuiHelpers.GlobalScale);

        ImGui.SetNextWindowSize(size);

        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
            ThemePushed = true;
        }
    }
    protected override void DrawInternal()
    {
        // close window if its not focused.
        if (!ImGui.IsWindowFocused())
            IsOpen = false;

        if (DtrBarService.NonKinksters.Count is 0)
            return;

        // draw a list of tree nodes, for each player. When they are selected, display the map coordinates of them.
        var displayed = DtrBarService.NonKinksters.Take(10).ToList();
        var remaining = DtrBarService.NonKinksters.Count - displayed.Count;
        bool anyNonKinksters = DtrBarService.NonKinksters.Count is not 0;
        unsafe
        {
            foreach (var addr in displayed)
            {
                var character = (Character*)addr;
                var text = $"{character->NameString}  {character->GetWorld()}";
                if (ImGui.Selectable(text, _selectedAddr == addr))
                {
                    _selectedAddr = addr;
                    _service.LocatePlayer(character);
                }
            }
        }
        if (remaining > 0)
        {
            CkGui.ColorTextCentered($"And {remaining} more...", ImGuiColors.ParsedPink);
        }
    }
    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }
}
