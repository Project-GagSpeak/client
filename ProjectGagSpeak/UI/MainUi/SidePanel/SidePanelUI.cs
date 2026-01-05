using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;

namespace GagSpeak.Gui.MainWindow;

// We could ideally have this continuously running but never drawing much
// if anything at all while not expected.
// It would allow us to process the logic in the draw-loop like we want.
public class SidePanelUI : WindowMediatorSubscriberBase
{
    private readonly SidePanelPair _kinksterInfoPanel;
    private readonly SidePanelService _service;

    public SidePanelUI(ILogger<SidePanelUI> logger, GagspeakMediator mediator,
        SidePanelPair kinksterInfoPanel, SidePanelService service)
        : base(logger, mediator, "##GSInteractionsUI")
    {
        _kinksterInfoPanel = kinksterInfoPanel;
        _service = service;

        Flags = WFlags.NoCollapse | WFlags.NoTitleBar | WFlags.NoScrollbar | WFlags.NoResize;
    }

    /// <summary>
    ///     Internal logic performed every draw frame regardless of if the window is open or not. <para />
    ///     Lets us Open/Close the window based on logic in the service using minimal computation.
    /// </summary>
    public override void PreOpenCheck()
    {
        IsOpen = _service.CanDraw;
    }
    protected override void PreDrawInternal()
    {
        // Magic that makes the sticky pair window move with the main UI.
        var position = MainUI.LastPos;
        position.X += MainUI.LastSize.X;
        position.Y += ImGui.GetFrameHeightWithSpacing();
        ImGui.SetNextWindowPos(position);
        Flags |= WFlags.NoMove;

        float fixedWidth = _service.DisplayWidth;
        float fixedHeight = MainUI.LastSize.Y - ImGui.GetFrameHeightWithSpacing() * 2;
        
        this.SetBoundaries(new(fixedWidth, fixedHeight), new(fixedWidth, fixedHeight));
    }

    protected override void PostDrawInternal()
    { }

    // If this runs, it is assumed that for this frame the data is valid for drawing.
    protected override void DrawInternal()
    {
        // If there is no mode to draw, do not draw.
        if (_service.DisplayMode is SidePanelMode.None)
            return;

        // Display the correct mode.
        switch (_service.DisplayCache)
        {
            case KinksterInfoCache ic:
                DrawInteractions(ic);
                return;
            case ResponseCache irc when irc.Mode is SidePanelMode.IncomingRequests:
                DrawIncomingRequests(irc);
                return;
            case ResponseCache prc when prc.Mode is SidePanelMode.PendingRequests:
                DrawPendingRequests(prc);
                return;
        }
    }

    private void DrawInteractions(KinksterInfoCache ic)
    {
        using var _ = CkRaii.Child("InteractionsUI", ImGui.GetContentRegionAvail(), wFlags: WFlags.NoScrollbar);
        var width = _.InnerRegion.X;
        var dispName = ic.DisplayName;

        if (ic.Kinkster is not { } kinkster)
            return;

        // Draw the contents based on the type of tab we are on currently.
        switch (ic.CurrentTab)
        {
            case InteractionsTab.PermsForKinkster: _kinksterInfoPanel.DrawClientPermissions(ic, kinkster, dispName, width); break;
            case InteractionsTab.KinkstersPerms: _kinksterInfoPanel.DrawKinksterPermissions(ic, kinkster, dispName, width); break;
            case InteractionsTab.Interactions: _kinksterInfoPanel.DrawInteractions(ic, kinkster, dispName, width);          break;
        }
    }


    private void DrawIncomingRequests(ResponseCache irc)
    {
        using var _ = CkRaii.Child("RequestResponder", ImGui.GetContentRegionAvail(), wFlags: WFlags.NoScrollbar);
        var width = _.InnerRegion.X;

        CkGui.FontTextCentered("Request Responder", UiFontService.Default150Percent);

        CkGui.FramedIconText(FAI.ObjectGroup);
        CkGui.TextFrameAlignedInline("Bulk Selector Area");
        CkGui.TextFrameAligned($"There are currently: {irc.Selected.Count} selected requests.");
    }

    private void DrawPendingRequests(ResponseCache prc)
    {
        using var _ = CkRaii.Child("PendingRequests", ImGui.GetContentRegionAvail(), wFlags: WFlags.NoScrollbar);
        var width = _.InnerRegion.X;

        CkGui.FontTextCentered("Pending Requests", UiFontService.Default150Percent);

        CkGui.FramedIconText(FAI.ObjectGroup);
        CkGui.TextFrameAlignedInline("Bulk Selector Area");
        CkGui.TextFrameAligned($"There are currently: {prc.Selected.Count} selected requests.");
    }
}
