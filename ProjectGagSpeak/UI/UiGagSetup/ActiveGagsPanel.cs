using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.StateManagers;
using GagSpeak.UI.Components.Combos;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.UiGagSetup;

public class ActiveGagsPanel : DisposableMediatorSubscriberBase
{
    private readonly ClientData _gagData; // for grabbing lock data
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly AppearanceManager _appearance;
    private readonly TutorialService _guides;
    private readonly UiSharedService _uiShared;

    public ActiveGagsPanel(ILogger<ActiveGagsPanel> logger, GagspeakMediator mediator, ClientData gagData,
        ClientConfigurationManager clientConfigs, AppearanceManager appearance,
        TutorialService guides, UiSharedService uiShared) : base(logger, mediator)
    {
        _gagData = gagData;
        _clientConfigs = clientConfigs;
        _appearance = appearance;
        _guides = guides;
        _uiShared = uiShared;

        _gagItemCombos = new GagComboClient[3]
        {
            new GagComboClient(0, mediator, gagData, clientConfigs, appearance, uiShared, logger, "ClientGagSlot0"),
            new GagComboClient(1, mediator, gagData, clientConfigs, appearance, uiShared, logger, "ClientGagSlot1"),
            new GagComboClient(2, mediator, gagData, clientConfigs, appearance, uiShared, logger, "ClientGagSlot2")
        };
        _gagPadlockCombos = new PadlockGagsClient[3]
        {
            new PadlockGagsClient(0, gagData, appearance, logger, uiShared, "ClientPadlock0"),
            new PadlockGagsClient(1, gagData, appearance, logger, uiShared, "ClientPadlock1"),
            new PadlockGagsClient(2, gagData, appearance, logger, uiShared, "ClientPadlock2")
        };
    }
    private static readonly string[] Labels = { "Inner Gag", "Central Gag", "Outer Gag" };
    private string GetGagTypePath(int index) => $"GagImages\\{_gagData.AppearanceData!.GagSlots[index].GagType}.png" ?? $"ItemMouth\\None.png";
    private string GetGagPadlockPath(int index) => $"PadlockImages\\{_gagData.AppearanceData!.GagSlots[index].Padlock.ToPadlock()}.png" ?? $"Padlocks\\None.png";

    private GagComboClient[] _gagItemCombos = new GagComboClient[3];
    private PadlockGagsClient[] _gagPadlockCombos = new PadlockGagsClient[3];

    // Draw the active gags tab
    public void DrawActiveGagsPanel(Vector2 winPos, Vector2 winSize)
    {
        Vector2 bigTextSize = new Vector2(0, 0);

        using (_uiShared.GagspeakLabelFont.Push()) bigTextSize = ImGui.CalcTextSize("HeightDummy");

        var region = ImGui.GetContentRegionAvail();

        var gagSlots = _gagData.AppearanceData?.GagSlots ?? new GagSlot[3];
        // draw the active gags panel.
        for (var idx = 0; idx < 3; idx++)
        {
            DrawGagSlotHeader(idx, bigTextSize);
            if (idx is 0) _guides.OpenTutorial(TutorialType.Gags, StepsActiveGags.LayersInfo, winPos, winSize);

            using (ImRaii.Group())
            {
                DrawImage(GetGagTypePath(idx));
                ImGui.SameLine();

                // Dictate where this group is drawn.
                var GroupCursorY = ImGui.GetCursorPosY();
                using (ImRaii.Group())
                {
                    if (!GsPadlockEx.IsTwoRowLock(_gagPadlockCombos[idx].SelectedLock))
                        ImGui.SetCursorPosY(GroupCursorY + ImGui.GetFrameHeight() / 2);

                    // Draw out the gag lock information, then the padlock information under it.
                    _gagItemCombos[idx].DrawCombo("##GagCombo" + idx, "Select Gag to Apply", 250f, 1.2f, ImGui.GetTextLineHeightWithSpacing());
                    // The Gag Group
                    if (idx is 0)
                    {
                        _guides.OpenTutorial(TutorialType.Gags, StepsActiveGags.EquippingGags, winPos, winSize);
                        if (gagSlots[0].Padlock.ToPadlock() is Padlocks.None && gagSlots[0].GagType.ToGagType() is not GagType.None)
                            _guides.OpenTutorial(TutorialType.Gags, StepsActiveGags.RemovingGags, winPos, winSize);
                    }

                    // then the lock group.
                    _gagPadlockCombos[idx].DrawPadlockComboSection(250f, "Select Padlock to Apply", "Lock/Unlock");
                    if (idx is 0)
                    {
                        if (gagSlots[0].GagType.ToGagType() is not GagType.None)
                        {
                            _guides.OpenTutorial(TutorialType.Gags, StepsActiveGags.SelectingPadlocks, winPos, winSize);
                            _guides.OpenTutorial(TutorialType.Gags, StepsActiveGags.PadlockTypes, winPos, winSize);
                        }
                        if (_gagPadlockCombos[idx].SelectedLock is not Padlocks.None)
                            _guides.OpenTutorial(TutorialType.Gags, StepsActiveGags.LockingPadlocks, winPos, winSize);
                        if (gagSlots[0].Padlock.ToPadlock() is not Padlocks.None)
                            _guides.OpenTutorial(TutorialType.Gags, StepsActiveGags.UnlockingPadlocks, winPos, winSize);
                    }
                }
                if (gagSlots[idx].IsLocked())
                {
                    ImGui.SameLine();
                    DrawImage(GetGagPadlockPath(idx));
                }
            }
        }
    }

    private void DrawGagSlotHeader(int slotNumber, Vector2 bigTextSize)
    {
        _uiShared.GagspeakBigText(Labels[slotNumber]);
        if (_gagData.AppearanceData is null)
            return;

        if (_gagData.AppearanceData.GagSlots[slotNumber].Padlock.ToPadlock().IsTimerLock())
        {
            ImGui.SameLine();
            DisplayTimeLeft(
                _gagData.AppearanceData.GagSlots[slotNumber].Timer,
                _gagData.AppearanceData.GagSlots[slotNumber].Padlock.ToPadlock(),
                _gagData.AppearanceData.GagSlots[slotNumber].Assigner,
                yPos: ImGui.GetCursorPosY() + ((bigTextSize.Y - ImGui.GetTextLineHeight()) / 2) + 5f);
        }
    }

    private void DrawImage(string gagTypePath)
    {
        var gagTexture = _uiShared.GetImageFromDirectoryFile(gagTypePath);
        if (gagTexture is { } wrapGag)
            ImGui.Image(wrapGag.ImGuiHandle, new Vector2(80, 80));
        else
            Logger.LogWarning("Failed to render image!");
    }

    private void DisplayTimeLeft(DateTimeOffset endTime, Padlocks padlock, string userWhoSetLock, float yPos)
    {
        var prefixText = userWhoSetLock != MainHub.UID
            ? userWhoSetLock + "'s " : (padlock is Padlocks.MimicPadlock ? "The Devious " : "Self-Applied ");
        var gagText = padlock.ToName() + " has";
        var color = ImGuiColors.ParsedGold;
        switch (padlock)
        {
            case Padlocks.MetalPadlock:
            case Padlocks.CombinationPadlock:
            case Padlocks.PasswordPadlock:
            case Padlocks.FiveMinutesPadlock:
            case Padlocks.TimerPadlock:
            case Padlocks.TimerPasswordPadlock:
                color = ImGuiColors.ParsedGold; break;
            case Padlocks.OwnerPadlock:
            case Padlocks.OwnerTimerPadlock:
                color = ImGuiColors.ParsedPink; break;
            case Padlocks.DevotionalPadlock:
            case Padlocks.DevotionalTimerPadlock:
                color = ImGuiColors.TankBlue; break;
            case Padlocks.MimicPadlock:
                color = ImGuiColors.ParsedGreen; break;
        }
        ImGui.SameLine();
        ImGui.SetCursorPosY(yPos);
        UiSharedService.ColorText(prefixText + gagText, color);
        ImUtf8.SameLineInner();
        ImGui.SetCursorPosY(yPos);
        UiSharedService.ColorText(endTime.ToGsRemainingTimeFancy(), color);
    }
}
