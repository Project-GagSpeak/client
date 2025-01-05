using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Profile;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Dto.User;
using ImGuiNET;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.Components.Popup;

internal class ReportPopupHandler : IPopupHandler
{
    private readonly MainHub _apiHubMain;
    private readonly PairManager _pairs;
    private readonly CosmeticService _cosmetics;
    private readonly KinkPlateService _kinkPlates;
    private readonly UiSharedService _uiShared;
    private UserData _reportedKinkster = new("BlankKinkster");
    private string _reportedDisplayName = "Kinkster-XXX";
    private string _reportReason = DefaultReportReason;

    private const string DefaultReportReason = "Describe why you are reporting this Kinkster here...";

    public ReportPopupHandler(MainHub apiHubMain, PairManager pairs,
        CosmeticService cosmetics, KinkPlateService kinkplates, 
        UiSharedService uiShared)
    {
        _apiHubMain = apiHubMain;
        _pairs = pairs;
        _cosmetics = cosmetics;
        _kinkPlates = kinkplates;
        _uiShared = uiShared;
    }

    public Vector2 PopupSize => new(800, 450);
    public bool ShowClosed => false;
    public bool CloseHovered { get; set; } = false;
    public Vector2? WindowPadding => Vector2.Zero;
    public float? WindowRounding => 35f;

    public void DrawContent()
    {
        var drawList = ImGui.GetWindowDrawList();
        var rectMin = drawList.GetClipRectMin();
        var rectMax = drawList.GetClipRectMax();
        var PlateSize = rectMax - rectMin;

        // grab our profile image and draw the baseline.
        var KinkPlate = _kinkPlates.GetKinkPlate(_reportedKinkster);
        var pfpWrap = KinkPlate.GetCurrentProfileOrDefault();

        // draw out the background for the window.
        if (_cosmetics.TryGetBackground(ProfileComponent.Plate, ProfileStyleBG.Default, out var plateBG))
            KinkPlateUI.AddImageRounded(drawList, plateBG, rectMin, PlateSize, 30f);

        // draw out the border on top of that.
        if (_cosmetics.TryGetBorder(ProfileComponent.Plate, ProfileStyleBorder.Default, out var plateBorder))
            KinkPlateUI.AddImageRounded(drawList, plateBorder, rectMin, PlateSize, 20f);

        var pfpPos = rectMin + Vector2.One * 16f;
        KinkPlateUI.AddImageRounded(drawList, pfpWrap, pfpPos, Vector2.One * 192, 96f, true, "The Image being Reported");

        // draw out the border for the profile picture
        if (_cosmetics.TryGetBorder(ProfileComponent.ProfilePicture, ProfileStyleBorder.Default, out var pfpBorder))
            KinkPlateUI.AddImageRounded(drawList, pfpBorder, rectMin + Vector2.One* 12f, Vector2.One * 200, 96f);


        var btnSize = Vector2.One * 20;
        var btnPos = rectMin + Vector2.One * 16;

        var closeButtonColor = CloseHovered ? ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)) : ImGui.GetColorU32(ImGuiColors.ParsedPink);

        drawList.AddLine(btnPos, btnPos + btnSize, closeButtonColor, 3);
        drawList.AddLine(new Vector2(btnPos.X + btnSize.X, btnPos.Y), new Vector2(btnPos.X, btnPos.Y + btnSize.Y), closeButtonColor, 3);

        ImGui.SetCursorScreenPos(btnPos);
        if (ImGui.InvisibleButton($"CloseButton##KinkPlateClose" + _reportedDisplayName, btnSize))
            ImGui.CloseCurrentPopup();

        CloseHovered = ImGui.IsItemHovered();

        // Description Border
        if (_cosmetics.TryGetBorder(ProfileComponent.DescriptionLight, ProfileStyleBorder.Default, out var descBorder))
            KinkPlateUI.AddImageRounded(drawList, descBorder, rectMin + new Vector2(220, 12), new Vector2(250, 200), 2f);

        ImGui.SetCursorScreenPos(rectMin + new Vector2(235, 24));
        var desc = KinkPlate.KinkPlateInfo.Description;
        DrawLimitedDescription(desc, ImGuiColors.DalamudWhite, new Vector2(230, 185));
        UiSharedService.AttachToolTip("The Description being Reported");

        // Beside it we should draw out the rules.
        ImGui.SetCursorScreenPos(rectMin + new Vector2(475, 15));

        using (ImRaii.Group())
        {
            UiSharedService.ColorText("Only Report Pictures if they are:", ImGuiColors.ParsedGold);
            UiSharedService.TextWrapped("- Harassing another player. Directly or Indirectly.");
            UiSharedService.TextWrapped("- Impersonating another player.");
            UiSharedService.TextWrapped("- Displays Content that displays NFSL content.");
            ImGui.Spacing();
            UiSharedService.ColorText("Only Report Descriptions if they are:", ImGuiColors.ParsedGold);
            UiSharedService.TextWrapped("- Harassing another player. Directly or Indirectly.");
            UiSharedService.TextWrapped("- Used to share topics that dont belong here.");
            ImGui.Spacing();
            UiSharedService.ColorTextWrapped("Miss-use of reporting will result in your account being Timed out.", ImGuiColors.DalamudRed);
        }

        // Draw the gold line split.
        var reportBoxSize = new Vector2(250 + 192 + ImGui.GetStyle().ItemSpacing.X);
        KinkPlateUI.AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.AchievementLineSplit],
            rectMin + new Vector2(15, 220), new Vector2(770, 6));

        ImGui.SetCursorScreenPos(rectMin + new Vector2(15, 235));
        ImGui.InputTextMultiline("##reportReason", ref _reportReason, 500, new Vector2(reportBoxSize.X, 200));

        // draw out the title text for this mark.
        ImGui.SetCursorScreenPos(rectMin + new Vector2(475, 235));
        using (ImRaii.Group())
        {
            using (_uiShared.GagspeakFont.Push())
            {
                UiSharedService.ColorTextWrapped("We will analyze reports with care. Cordy has been a victum " +
                "of manipulation and abuse multiple times, and will do her best to ensure her team does not allow " +
                "predators to exploit this reporting system on you.", ImGuiColors.DalamudWhite2);
            }

            ImGui.Spacing();

            _uiShared.GagspeakTitleText("Report " + _reportedDisplayName + "?", ImGuiColors.ParsedGold);
            if (_uiShared.IconTextButton(FontAwesomeIcon.ExclamationTriangle, "Report Kinkster", 
                disabled: _reportReason.IsNullOrWhitespace() || string.Equals(_reportReason, DefaultReportReason, StringComparison.OrdinalIgnoreCase)))
            {
                ImGui.CloseCurrentPopup();
                var reason = _reportReason;
                _ = _apiHubMain.UserReportKinkPlate(new(_reportedKinkster, reason));
            }
        }
    }

    private void DrawLimitedDescription(string desc, Vector4 color, Vector2 size)
    {
        // Calculate the line height and determine the max lines based on available height
        float lineHeight = ImGui.CalcTextSize("A").Y;
        int maxLines = (int)(size.Y / lineHeight);

        int currentLines = 1;
        float lineWidth = size.X; // Max width for each line
        string[] words = desc.Split(' '); // Split text by words
        string newDescText = "";
        string currentLine = "";

        foreach (var word in words)
        {
            // Try adding the current word to the line
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            float testLineWidth = ImGui.CalcTextSize(testLine).X;

            if (testLineWidth > lineWidth)
            {
                // Current word exceeds line width; finalize the current line
                newDescText += currentLine + "\n";
                currentLine = word;
                currentLines++;

                // Check if maxLines is reached and break if so
                if (currentLines >= maxLines)
                    break;
            }
            else
            {
                // Word fits in the current line; accumulate it
                currentLine = testLine;
            }
        }

        // Add any remaining text if we haven’t hit max lines
        if (currentLines < maxLines && !string.IsNullOrEmpty(currentLine))
        {
            newDescText += currentLine;
            currentLines++; // Increment the line count for the final line
        }

        UiSharedService.ColorTextWrapped(newDescText.TrimEnd(), color);
    }

    public void Open(ReportKinkPlateMessage msg)
    {
        _reportedKinkster = msg.KinksterToReport;
        _reportedDisplayName = _pairs.DirectPairs.Any(x => x.UserData.UID == _reportedKinkster.UID)
            ? _reportedKinkster.AliasOrUID
            : "Kinkster-" + _reportedKinkster.UID.Substring(_reportedKinkster.UID.Length - 4);
        _reportReason = DefaultReportReason;
    }
}
