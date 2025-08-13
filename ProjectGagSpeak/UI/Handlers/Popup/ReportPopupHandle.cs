using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using Dalamud.Bindings.ImGui;

namespace GagSpeak.Gui.Components;

internal class ReportPopupHandler : IPopupHandler
{
    private readonly MainHub _hub;
    private readonly KinksterManager _pairs;
    private readonly CosmeticService _cosmetics;
    private readonly KinkPlateService _kinkPlates;

    private UserData _reportedKinkster = new("BlankKinkster");
    private string _reportedDisplayName = "Kinkster-XXX";
    private string _reportReason = DefaultReportReason;

    private const string DefaultReportReason = "Describe why you are reporting this Kinkster here...";

    public ReportPopupHandler(MainHub hub, KinksterManager pairs,
        CosmeticService cosmetics, KinkPlateService kinkplates)
    {
        _hub = hub;
        _pairs = pairs;
        _cosmetics = cosmetics;
        _kinkPlates = kinkplates;

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
            drawList.AddDalamudImageRounded(plateBG, rectMin, PlateSize, 30f);

        // draw out the border on top of that.
        if (_cosmetics.TryGetBorder(ProfileComponent.Plate, ProfileStyleBorder.Default, out var plateBorder))
            drawList.AddDalamudImageRounded(plateBorder, rectMin, PlateSize, 20f);

        var pfpPos = rectMin + Vector2.One * 16f;
        drawList.AddDalamudImageRounded(pfpWrap, pfpPos, Vector2.One * 192, 96f, "The Image being Reported");

        // draw out the border for the profile picture
        if (_cosmetics.TryGetBorder(ProfileComponent.ProfilePicture, ProfileStyleBorder.Default, out var pfpBorder))
            drawList.AddDalamudImageRounded(pfpBorder, rectMin + Vector2.One* 12f, Vector2.One * 200, 96f);


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
            drawList.AddDalamudImageRounded(descBorder, rectMin + new Vector2(220, 12), new Vector2(250, 200), 2f);

        ImGui.SetCursorScreenPos(rectMin + new Vector2(235, 24));
        var desc = KinkPlate.KinkPlateInfo.Description;
        DrawLimitedDescription(desc, ImGuiColors.DalamudWhite, new Vector2(230, 185));
        CkGui.AttachToolTip("The Description being Reported");

        // Beside it we should draw out the rules.
        ImGui.SetCursorScreenPos(rectMin + new Vector2(475, 15));

        using (ImRaii.Group())
        {
            CkGui.ColorText("Only Report Pictures if they are:", ImGuiColors.ParsedGold);
            CkGui.TextWrapped("- Harassing another player. Directly or Indirectly.");
            CkGui.TextWrapped("- Impersonating another player.");
            CkGui.TextWrapped("- Displays Content that displays NFSL content.");
            ImGui.Spacing();
            CkGui.ColorText("Only Report Descriptions if they are:", ImGuiColors.ParsedGold);
            CkGui.TextWrapped("- Harassing another player. Directly or Indirectly.");
            CkGui.TextWrapped("- Used to share topics that dont belong here.");
            ImGui.Spacing();
            CkGui.ColorTextWrapped("Miss-use of reporting will result in your account being Timed out.", ImGuiColors.DalamudRed);
        }

        // Draw the gold line split.
        var reportBoxSize = new Vector2(250 + 192 + ImGui.GetStyle().ItemSpacing.X);
        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.AchievementLineSplit], rectMin + new Vector2(15, 220), new Vector2(770, 6));

        ImGui.SetCursorScreenPos(rectMin + new Vector2(15, 235));
        ImGui.InputTextMultiline("##reportReason", ref _reportReason, 500, new Vector2(reportBoxSize.X, 200));

        // draw out the title text for this mark.
        ImGui.SetCursorScreenPos(rectMin + new Vector2(475, 235));
        using (ImRaii.Group())
        {
            using (UiFontService.GagspeakFont.Push())
            {
                CkGui.ColorTextWrapped("We will analyze reports with care. Cordy has been a victum " +
                "of manipulation and abuse multiple times, and will do her best to ensure her team does not allow " +
                "predators to exploit this reporting system on you.", ImGuiColors.DalamudWhite2);
            }

            ImGui.Spacing();

            CkGui.FontText("Report " + _reportedDisplayName + "?", UiFontService.GagspeakTitleFont, ImGuiColors.ParsedGold);
            if (CkGui.IconTextButton(FAI.ExclamationTriangle, "Report Kinkster", 
                disabled: _reportReason.IsNullOrWhitespace() || string.Equals(_reportReason, DefaultReportReason, StringComparison.OrdinalIgnoreCase)))
            {
                ImGui.CloseCurrentPopup();
                var reason = _reportReason;
                _ = _hub.UserReportKinkPlate(new(_reportedKinkster, reason));
            }
        }
    }

    private void DrawLimitedDescription(string desc, Vector4 color, Vector2 size)
    {
        // Calculate the line height and determine the max lines based on available height
        var lineHeight = ImGui.CalcTextSize("A").Y;
        var maxLines = (int)(size.Y / lineHeight);

        var currentLines = 1;
        var lineWidth = size.X; // Max width for each line
        var words = desc.Split(' '); // Split text by words
        var newDescText = "";
        var currentLine = "";

        foreach (var word in words)
        {
            // Try adding the current word to the line
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testLineWidth = ImGui.CalcTextSize(testLine).X;

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

        CkGui.ColorTextWrapped(newDescText.TrimEnd(), color);
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
