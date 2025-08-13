using CkCommons.Gui;
using CkCommons.Gui.Utility;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Network;
using Dalamud.Bindings.ImGui;
using Microsoft.IdentityModel.Tokens;
using OtterGui.Text;

namespace GagSpeak.Gui.Profile;

public class KinkPlateEditorUI : WindowMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly KinkPlateService _KinkPlateManager;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;

    public KinkPlateEditorUI(
        ILogger<KinkPlateEditorUI> logger,
        GagspeakMediator mediator,
        MainHub hub,
        KinkPlateService KinkPlateManager,
        CosmeticService cosmetics,
        TutorialService guides) : base(logger, mediator, "KinkPlate Editor###KP_EditorUI")
    {
        _hub = hub;
        _KinkPlateManager = KinkPlateManager;
        _cosmetics = cosmetics;
        _guides = guides;

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
        => ClientAchievements.CompletedAchievements
            .Where(x => x.RewardComponent == SelectedComponent && x.RewardStyleType == SelectedStyle)
            .Select(x => (ProfileStyleBG)x.RewardStyleIndex)
            .Distinct();

    private IEnumerable<ProfileStyleBorder> UnlockedBorders()
        => ClientAchievements.CompletedAchievements
            .Where(x => x.RewardComponent == SelectedComponent && x.RewardStyleType == SelectedStyle)
            .Select(x => (ProfileStyleBorder)x.RewardStyleIndex)
            .Distinct();

    private IEnumerable<ProfileStyleOverlay> UnlockedOverlays()
        => ClientAchievements.CompletedAchievements
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
                _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ProfileEditImage, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
                    () => Mediator.Publish(new UiToggleMessage(typeof(ProfilePictureEditor))));

                ImUtf8.SameLineInner();
                if (CkGui.IconTextButton(FAI.Save, "Save Changes"))
                    _ = _hub.UserSetKinkPlateContent(new KinkPlateInfo(new UserData(MainHub.UID), profile.KinkPlateInfo));
                CkGui.AttachToolTip("Updates your stored profile with latest information");
                _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ProfileSaving, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
                    () => { IsOpen = false; /* save(?) and close the editor window */ });

                ImUtf8.SameLineInner();
                if (ImGui.Checkbox("Public", ref publicRef))
                    profile.KinkPlateInfo.PublicPlate = publicRef;
                CkGui.AttachToolTip("If checked, your profile picture and description will become visible\n" +
                    "to others through private rooms and global chat!" +
                    "--SEP--Non-Paired Kinksters still won't be able to see your UID if viewing your KinkPlate");
                _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ProfilePublicity, ImGui.GetWindowPos(), ImGui.GetWindowSize());
            }
        }

        var pfpWrap = profile.GetCurrentProfileOrDefault();
        if (pfpWrap != null)
        {
            var currentPosition = ImGui.GetCursorPos();
            drawList.AddImageRounded(pfpWrap.Handle, pos, pos + Vector2.One * 232f, Vector2.Zero, Vector2.One, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), 116f);
        }

        using (ImRaii.Group())
        {
            var completed = new SortedList<int, string>(ClientAchievements.CompletedAchievements.ToDictionary(x => x.AchievementId, x => x.Title));
            completed.Add(0, "None"); // Add a default option for no title selected

            CkGui.ColorText("Select Title", ImGuiColors.ParsedGold);
            CkGui.HelpText("Select a title to display on your KinkPlate!--SEP--Can only select Achievement Titles you've completed!");
            if (CkGuiUtils.IntCombo("##TitleSelect", 200f, profile.KinkPlateInfo.ChosenTitleId, out var newTitleId, completed.Keys,
                num => completed.TryGetValue(num, out var title) ? title : "Unknown Title", "Select Title..."))
            {
                profile.KinkPlateInfo.ChosenTitleId = newTitleId;
            }
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.SettingTitles, ImGui.GetWindowPos(), ImGui.GetWindowSize());
        }

        using (ImRaii.Group())
        {
            // Create a dropdown for all the different components of the KinkPlate
            CkGui.ColorText("Select Component", ImGuiColors.ParsedGold);
            CkGui.HelpText("Select the component of the KinkPlate you'd like to customize!");
            if(CkGuiUtils.EnumCombo("##ProfileComponent", 200f, SelectedComponent, out var newComponent))
                SelectedComponent = newComponent;

            // Create a dropdown for all the different styles of the KinkPlate
            CkGui.ColorText("Select Style", ImGuiColors.ParsedGold);
            CkGui.HelpText("Select the Style Kind from the selected component you wish to change the customization of.");
            if(CkGuiUtils.EnumCombo("##ProfileStyleKind", 200f, SelectedStyle, out var newStyle))
                SelectedStyle = newStyle;

            // grab the reference value for the selected component and style from the profile.kinkplateinfo based on the currently chosen options.
            CkGui.ColorText("Customization for Section", ImGuiColors.ParsedGold);
            if (SelectedStyle is StyleKind.Background)
            {
                CkGui.HelpText("Select the background style for your KinkPlate!--SEP--You will only be able to see cosmetics you've unlocked from Achievements!");
                if(CkGuiUtils.EnumCombo("##ProfileBackgroundStyle", 200f, profile.GetBackground(SelectedComponent), out var newBG, UnlockedBackgrounds()))
                    profile.SetBackground(SelectedComponent, newBG);
            }
            else if (SelectedStyle is StyleKind.Border)
            {
                CkGui.HelpText("Select the border style for your KinkPlate!--SEP--You will only be able to see cosmetics you've unlocked from Achievements!");
                if(CkGuiUtils.EnumCombo("##ProfileBorderStyle", 200f, profile.GetBorder(SelectedComponent), out var newBorder, UnlockedBorders()))
                    profile.SetBorder(SelectedComponent, newBorder);
            }
            else if (SelectedStyle is StyleKind.Overlay)
            {
                CkGui.HelpText("Select the overlay style for your KinkPlate!--SEP--You will only be able to see cosmetics you've unlocked from Achievements!");
                if(CkGuiUtils.EnumCombo("##ProfileOverlayStyle", 200f, profile.GetOverlay(SelectedComponent), out var newOverlay, UnlockedOverlays()))
                    profile.SetOverlay(SelectedComponent, newOverlay);
            }
        }
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.CustomizingProfile, ImGui.GetWindowPos(), ImGui.GetWindowSize());

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
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ProfileDescription, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
            () =>
            {
                Mediator.Publish(new KinkPlateOpenStandaloneLightMessage(MainHub.PlayerUserData));
            });
                if (profile.KinkPlateInfo.Disabled)
            CkGui.AttachToolTip("You're Profile Customization Access has been Revoked!" +
                "--SEP--You will not be able to edit your KinkPlate Description!");

        // draw the plate preview buttons.
        var width = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;

        if (CkGui.IconTextButton(FAI.Expand, "Preview KinkPlate™ Light", width, id: MainHub.UID + "KinkPlatePreviewLight"))
            Mediator.Publish(new KinkPlateOpenStandaloneLightMessage(MainHub.PlayerUserData));
        CkGui.AttachToolTip("Preview your Light KinkPlate™ in a standalone window!");
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ProfilePreviewLight, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
            () =>
            {
                // close light kinplate, open full
                Mediator.Publish(new KinkPlateOpenStandaloneLightMessage(MainHub.PlayerUserData));
                Mediator.Publish(new UiToggleMessage(typeof(KinkPlatePreviewUI)));
            }); 


        ImGui.SameLine();
        if (CkGui.IconTextButton(FAI.Expand, "Preview KinkPlate™ Full", width, id: MainHub.UID + "KinkPlatePreviewFull"))
            Mediator.Publish(new UiToggleMessage(typeof(KinkPlatePreviewUI)));
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ProfilePreviewFull, ImGui.GetWindowPos(), ImGui.GetWindowSize(), ()=> {
            Mediator.Publish(new UiToggleMessage(typeof(KinkPlatePreviewUI)));
            Mediator.Publish(new UiToggleMessage(typeof(ProfilePictureEditor)));
         });
    }
}
