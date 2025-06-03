using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Achievements;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.CkCommons.Gui.Profile;

public class KinkPlateEditorUI : WindowMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly KinkPlateService _KinkPlateManager;
    private readonly CosmeticService _cosmetics;

    public KinkPlateEditorUI(
        ILogger<KinkPlateEditorUI> logger,
        GagspeakMediator mediator,
        MainHub hub,
        KinkPlateService KinkPlateManager,
        CosmeticService cosmetics) : base(logger, mediator, "KinkPlate Editor###KP_EditorUI")
    {
        _hub = hub;
        _KinkPlateManager = KinkPlateManager;
        _cosmetics = cosmetics;

        Flags = WFlags.NoScrollbar | WFlags.NoResize;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(500, 400),
            MaximumSize = new Vector2(500, 400),
        };

        Size = new(400, 600);
        IsOpen = false;

        Mediator.Subscribe<MainHubDisconnectedMessage>(this, (_) => IsOpen = false);
    }

    private Vector2 RectMin = Vector2.Zero;
    private Vector2 RectMax = Vector2.Zero;
    private ProfileComponent SelectedComponent = ProfileComponent.Plate;
    private StyleKind SelectedStyle = StyleKind.Background;

    private IEnumerable<StyleKind> StylesForComponent()
        => SelectedComponent switch
        {
            ProfileComponent.Plate => new[]{ StyleKind.Background, StyleKind.Border},
            ProfileComponent.ProfilePicture => new[]{ StyleKind.Border, StyleKind.Overlay},
            ProfileComponent.Description => new[]{ StyleKind.Background, StyleKind.Border, StyleKind.Overlay},
            ProfileComponent.GagSlot => new[]{ StyleKind.Background, StyleKind.Border, StyleKind.Overlay},
            ProfileComponent.Padlock => new[]{ StyleKind.Background, StyleKind.Border, StyleKind.Overlay },
            ProfileComponent.BlockedSlots => new[]{ StyleKind.Background, StyleKind.Border, StyleKind.Overlay},
            ProfileComponent.BlockedSlot => new[]{ StyleKind.Border, StyleKind.Overlay},
            _ => throw new NotImplementedException()
        };

    private IEnumerable<ProfileStyleBG> UnlockedBackgrounds() 
        => AchievementManager.CompletedAchievements
            .Where(x => x.RewardComponent == SelectedComponent && x.RewardStyleType == SelectedStyle)
            .Select(x => (ProfileStyleBG)x.RewardStyleIndex)
            .Distinct();

    private IEnumerable<ProfileStyleBorder> UnlockedBorders()
        => AchievementManager.CompletedAchievements
            .Where(x => x.RewardComponent == SelectedComponent && x.RewardStyleType == SelectedStyle)
            .Select(x => (ProfileStyleBorder)x.RewardStyleIndex)
            .Distinct();

    private IEnumerable<ProfileStyleOverlay> UnlockedOverlays()
        => AchievementManager.CompletedAchievements
            .Where(x => x.RewardComponent == SelectedComponent && x.RewardStyleType == SelectedStyle)
            .Select(x => (ProfileStyleOverlay)x.RewardStyleIndex)
            .Distinct();

    protected override void PreDrawInternal() { }
    protected override void PostDrawInternal() { }

    protected override void DrawInternal()
    {
        var drawList = ImGui.GetWindowDrawList();
        RectMin = drawList.GetClipRectMin();
        RectMax = drawList.GetClipRectMax();
        var contentRegion = RectMax - RectMin;
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        // grab our profile.
        var profile = _KinkPlateManager.GetKinkPlate(new UserData(MainHub.UID));
        var publicRef = profile.KinkPlateInfo.PublicPlate;
        var pos = new Vector2(ImGui.GetCursorScreenPos().X + contentRegion.X - 242, ImGui.GetCursorScreenPos().Y);
        using (ImRaii.Group())
        {
            using (ImRaii.Group())
            {
                if (CkGui.IconTextButton(FAI.FileUpload, "Edit Image", disabled: profile.KinkPlateInfo.Disabled))
                    Mediator.Publish(new UiToggleMessage(typeof(ProfilePictureEditor)));
                CkGui.AttachToolTip(profile.KinkPlateInfo.Disabled
                    ? "You're Profile Customization Access has been Revoked!"
                    : "Import and adjust a new profile picture to your liking!");

                ImUtf8.SameLineInner();
                if (CkGui.IconTextButton(FAI.Save, "Save Changes"))
                    _ = _hub.UserSetKinkPlateContent(new KinkPlateContent(new UserData(MainHub.UID), profile.KinkPlateInfo));
                CkGui.AttachToolTip("Updates your stored profile with latest information");
                
                ImUtf8.SameLineInner();
                if (ImGui.Checkbox("Public", ref publicRef))
                    profile.KinkPlateInfo.PublicPlate = publicRef;
                CkGui.AttachToolTip("If checked, your profile picture and description will become visible\n" +
                    "to others through private rooms and global chat!" +
                    "--SEP--Non-Paired Kinksters still won't be able to see your UID if viewing your KinkPlate");
            }
        }

        var pfpWrap = profile.GetCurrentProfileOrDefault();
        if (pfpWrap != null)
        {
            var currentPosition = ImGui.GetCursorPos();
            drawList.AddImageRounded(pfpWrap.ImGuiHandle, pos, pos + Vector2.One * 232f, Vector2.Zero, Vector2.One, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), 116f);
        }

        using (ImRaii.Group())
        {
            List<(int, string)> items = AchievementManager.CompletedAchievements.Select(x => (x.AchievementId, x.Title)).ToList();
            items.Insert(0, (0, "None"));

            CkGui.ColorText("Select Title", ImGuiColors.ParsedGold);
            CkGui.HelpText("Select a title to display on your KinkPlate!--SEP--Can only select Achievement Titles you've completed!");
            CkGui.DrawComboSearchable("##ProfileSelectTitle", 200f, items, (achievement) => achievement.Item2, true,
                (i) => profile.KinkPlateInfo.ChosenTitleId = i.Item1,
                initialSelectedItem: (profile.KinkPlateInfo.ChosenTitleId, AchievementManager.GetTitleById(profile.KinkPlateInfo.ChosenTitleId)));
        }

        using (ImRaii.Group())
        {
            // Create a dropdown for all the different components of the KinkPlate
            CkGui.ColorText("Select Component", ImGuiColors.ParsedGold);
            CkGui.HelpText("Select the component of the KinkPlate you'd like to customize!");
            if(CkGuiUtils.EnumCombo("##ProfileComponent", 200f, SelectedComponent, out ProfileComponent newComponent))
                SelectedComponent = newComponent;

            // Create a dropdown for all the different styles of the KinkPlate
            CkGui.ColorText("Select Style", ImGuiColors.ParsedGold);
            CkGui.HelpText("Select the Style Kind from the selected component you wish to change the customization of.");
            if(CkGuiUtils.EnumCombo("##ProfileStyleKind", 200f, SelectedStyle, out StyleKind newStyle))
                SelectedStyle = newStyle;

            // grab the reference value for the selected component and style from the profile.kinkplateinfo based on the currently chosen options.
            CkGui.ColorText("Customization for Section", ImGuiColors.ParsedGold);
            if (SelectedStyle is StyleKind.Background)
            {
                CkGui.HelpText("Select the background style for your KinkPlate!--SEP--You will only be able to see cosmetics you've unlocked from Achievements!");
                if(CkGuiUtils.EnumCombo("##ProfileBackgroundStyle", 200f, profile.GetBackground(SelectedComponent), out ProfileStyleBG newBG, UnlockedBackgrounds()))
                    profile.SetBackground(SelectedComponent, newBG);
            }
            else if (SelectedStyle is StyleKind.Border)
            {
                CkGui.HelpText("Select the border style for your KinkPlate!--SEP--You will only be able to see cosmetics you've unlocked from Achievements!");
                if(CkGuiUtils.EnumCombo("##ProfileBorderStyle", 200f, profile.GetBorder(SelectedComponent), out ProfileStyleBorder newBorder, UnlockedBorders()))
                    profile.SetBorder(SelectedComponent, newBorder);
            }
            else if (SelectedStyle is StyleKind.Overlay)
            {
                CkGui.HelpText("Select the overlay style for your KinkPlate!--SEP--You will only be able to see cosmetics you've unlocked from Achievements!");
                if(CkGuiUtils.EnumCombo("##ProfileOverlayStyle", 200f, profile.GetOverlay(SelectedComponent), out ProfileStyleOverlay newOverlay, UnlockedOverlays()))
                    profile.SetOverlay(SelectedComponent, newOverlay);
            }
        }

        // below this, we should draw out the description editor
        ImGui.AlignTextToFramePadding();
        CkGui.ColorText("Description", ImGuiColors.ParsedGold);
        using (ImRaii.Disabled(profile.KinkPlateInfo.Disabled))
        {
            var refText = profile.KinkPlateInfo.Description.IsNullOrEmpty() ? "No Description Set..." : profile.KinkPlateInfo.Description;
            var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing());
            if (ImGui.InputTextMultiline("##pfpDescription", ref refText, 1000, size))
                profile.KinkPlateInfo.Description = refText;
        }
        if (profile.KinkPlateInfo.Disabled)
            CkGui.AttachToolTip("You're Profile Customization Access has been Revoked!" +
                "--SEP--You will not be able to edit your KinkPlate Description!");

        // draw the plate preview buttons.
        var width = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;

        if (CkGui.IconTextButton(FAI.Expand, "Preview KinkPlate™ Light", width, id: MainHub.UID + "KinkPlatePreviewLight"))
            Mediator.Publish(new KinkPlateOpenStandaloneLightMessage(MainHub.PlayerUserData));
        CkGui.AttachToolTip("Preview your Light KinkPlate™ in a standalone window!");

        ImGui.SameLine();
        if (CkGui.IconTextButton(FAI.Expand, "Preview KinkPlate™ Full", width, id: MainHub.UID + "KinkPlatePreviewFull"))
            Mediator.Publish(new UiToggleMessage(typeof(KinkPlatePreviewUI)));
    }
}
