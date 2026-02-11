using CkCommons;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;

namespace GagSpeak.Gui.Wardrobe;

public class CollarPanel : DisposableMediatorSubscriberBase
{
    private readonly CollarManager _manager;
    private readonly TutorialService _guides;
    public CollarPanel(ILogger<CollarPanel> logger, GagspeakMediator mediator,
        CollarManager manager, TutorialService guides,
        CollarOverviewTab overview, CollarRequestsIncomingTab incomingRequests, CollarRequestsOutgoingTab outgoingRequests)
        : base(logger, mediator)
    {
        _manager = manager;
        _guides = guides;

        EditorTabs = [overview, incomingRequests, outgoingRequests];

        Mediator.Subscribe<TooltipSetItemToEditorMessage>(this, (msg) =>
        {
            // if the item is applied, and we do not have permission to change it, do not allow this.
            if (!_manager.IsActive || !_manager.SyncedData!.CollaredAccess.HasAny(CollarAccess.GlamMod))
                return;

            // Only set if in editor.
            if (_manager.ItemInEditor is GagSpeakCollar collar)
            {
                collar.Glamour.GameItem = msg.Item;
                Logger.LogDebug($"Set [{msg.Slot}] to [{msg.Item.Name}] on edited collar [{_manager.ItemInEditor.Label}]", LoggerType.Collars);
            }
        });

        Mediator.Subscribe<ThumbnailImageSelected>(this, (msg) =>
        {
            if (msg.Folder is not ImageDataType.Collar)
                return;            
            manager.UpdateThumbnail(msg.FileName);
        });
    }

    public static IFancyTab[] EditorTabs;

    /// <summary> All Content in here is grouped. Can draw either editor or overview left panel. </summary>
    public void DrawContents(CkHeader.QuadDrawRegions regions, float tabMenuWidth, WardrobeTabs tabMenu)
    {
        // This ensures no scrollbar or text clipping occurs via bypassing ImGui's draw and writing to the draw list directly.
        var textPos = regions.TopLeft.Pos + new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        using (UiFontService.GagspeakTitleFont.Push())
            ImGui.GetWindowDrawList().AddText(textPos, uint.MaxValue, "Collar Management");

        // Tab Menu still should be shown here!
        var shiftX = regions.TopRight.SizeX - tabMenuWidth;
        var newSize = regions.TopRight.Size with { X = tabMenuWidth };
        ImGui.SetCursorScreenPos(regions.TopRight.Pos + new Vector2(shiftX, 0));
        using (ImRaii.Child("CollarTR", newSize))
            tabMenu.Draw(newSize);

        // Draw the lower area using the combined bottom region size.
        ImGui.SetCursorScreenPos(regions.BotLeft.Pos);
        using (ImRaii.Child("CollarBotRegion", regions.BotSize, false, WFlags.AlwaysUseWindowPadding))
            DrawTabBarContent();
    }

    private void DrawTabBarContent()
    {
        using var _ = CkRaii.TabBarChild("CollarContents", GsCol.VibrantPink.Uint(), GsCol.VibrantPinkHovered.Uint(), CkCol.CurvedHeader.Uint(),
                LabelFlags.PadInnerChild | LabelFlags.SizeIncludesHeader, out var selected, EditorTabs);
        // Draw the selected tab's contents.
        selected?.DrawContents(_.InnerRegion.X);
    }
}
