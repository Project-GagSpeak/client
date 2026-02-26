using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using OtterGui.Text;
using TerraFX.Interop.Windows;

namespace GagSpeak.Gui.Components;

internal class ReportPopupHandler : IPopupHandler
{
    private const string DEFAULT_REASON = "Describe your report here...";

    private readonly MainHub _hub;
    private readonly GlobalChatLog _globalChat;
    private readonly KinksterManager _pairs;
    private readonly KinkPlateService _kinkPlates;

    private UserData _reportedUser = new("BlankKinkster");
    private string _reportedDisplayName = "Kinkster-XXX";
    private ReportKind _reportType = ReportKind.Profile;
    private string _reportReason = DEFAULT_REASON;
    private string _compressedChatData = string.Empty;

    public ReportPopupHandler(MainHub hub, GlobalChatLog globalChat,
        KinksterManager pairs, KinkPlateService kinkplates)
    {
        _hub = hub;
        _globalChat = globalChat;
        _pairs = pairs;
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
        var size = rectMax - rectMin;
        var frameH = ImUtf8.FrameHeight;
        var outerPadding = Vector2.One * 12f;
        var borderSize = Vector2.One * 8;
        var pfpBorderPos = rectMin + outerPadding;
        var pfpBorderSize = Vector2.One * 200;
        var pfpPos = rectMin + Vector2.One * 16f;
        var pfpSize = Vector2.One * 192;
        var descPos = pfpBorderPos + new Vector2(0, pfpBorderSize.Y + outerPadding.Y);
        var descSize = pfpBorderSize with { Y = size.Y - outerPadding.Y * 3 - pfpBorderSize.Y }; 

        // grab our profile image and draw the baseline.
        var kinkPlate = _kinkPlates.GetKinkPlate(_reportedUser);
        var pfpWrap = kinkPlate.GetProfileOrDefault();

        // draw out the background for the window.
        if (CosmeticService.TryGetBackground(PlateElement.Plate, KinkPlateBG.Default, out var plateBG))
            drawList.AddDalamudImageRounded(plateBG, rectMin, size, 30f);

        // draw out the border on top of that.
        if (CosmeticService.TryGetBorder(PlateElement.Plate, KinkPlateBorder.Default, out var plateBorder))
            drawList.AddDalamudImageRounded(plateBorder, rectMin, size, 20f);


        // Draw out the left group.
        using (ImRaii.Group())
        {
            drawList.AddDalamudImageRounded(pfpWrap, pfpPos, pfpSize, 96f, "The Image being Reported");
            // draw out the border for the profile picture
            if (CosmeticService.TryGetBorder(PlateElement.Avatar, KinkPlateBorder.Default, out var pfpBorder))
                drawList.AddDalamudImageRounded(pfpBorder, rectMin + Vector2.One * 12f, Vector2.One * 200, 96f);


            // Close Button
            var btnSize = Vector2.One * 20;
            var btnPos = rectMin + Vector2.One * 16;
            var closeButtonColor = CloseHovered ? uint.MaxValue : GsCol.VibrantPink.Uint();
            drawList.AddLine(btnPos, btnPos + btnSize, closeButtonColor, 3);
            drawList.AddLine(new Vector2(btnPos.X + btnSize.X, btnPos.Y), new Vector2(btnPos.X, btnPos.Y + btnSize.Y), closeButtonColor, 3);
            ImGui.SetCursorScreenPos(btnPos);
            if (ImGui.InvisibleButton($"CloseButton##KinkPlateClose" + _reportedDisplayName, btnSize))
                ImGui.CloseCurrentPopup();
            CloseHovered = ImGui.IsItemHovered();

            // Below draw out the description.
            if (CosmeticService.TryGetBorder(PlateElement.DescriptionLight, KinkPlateBorder.Default, out var descBorder))
                drawList.AddDalamudImageRounded(descBorder, descPos, descSize, 2f);
            // The text for it.
            ImGui.SetCursorScreenPos(descPos + borderSize);
            var desc = kinkPlate.Info.Description;
            DrawLimitedDescription(desc, ImGuiColors.DalamudWhite, new Vector2(230, 185));
            CkGui.AttachToolTip("The Description being Reported");

            ImGui.SetCursorScreenPos(pfpBorderPos);
            ImGui.Dummy((descPos + descSize) - pfpBorderPos);
        }

        var reportBoxPos = pfpBorderPos with { X = pfpBorderPos.X + pfpBorderSize.X + ImUtf8.ItemSpacing.X };
        ImGui.SetCursorScreenPos(reportBoxPos);
        ImGui.Dummy(new Vector2(ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - outerPadding.Y));
        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.Border));
        ImGui.SameLine();

        using var rightChild = CkRaii.Child("ReportPlateRight", ImGui.GetContentRegionAvail() - outerPadding);
        using (ImRaii.Group())
        {
            using (var __ = CkRaii.Child("ReportBox", new(ImGui.GetContentRegionAvail().X, pfpBorderSize.Y)))
            {
                ImGui.InputTextMultiline("##reportReason", ref _reportReason, 500, new Vector2(__.InnerRegion.X / 2, __.InnerRegion.Y));

                ImGui.SameLine();
                // Optimize this later.
                if (_reportType is ReportKind.Profile)
                {
                    using (ImRaii.Group())
                    {
                        CkGui.ColorText("Profiles are reportable if they:", ImGuiColors.ParsedGold);
                        CkGui.TextWrapped("- Harass another player. Directly or Indirectly.");
                        CkGui.TextWrapped("- Impersonating another player.");
                        CkGui.TextWrapped("- Displays NSFW content without being marked for NSFW.");
                        CkGui.TextWrapped("- Used to share topics that dont belong here.");
                        ImGui.Spacing();
                        CkGui.ColorTextWrapped("Miss-use of reporting will result in your account being timed out.", ImGuiColors.DalamudRed);
                    }
                }
                else
                {
                    using (ImRaii.Group())
                    {
                        CkGui.ColorText("Chat Messages are reportable if they:", ImGuiColors.ParsedGold);
                        CkGui.TextWrapped("- Harass another player. Directly or Indirectly");
                        CkGui.TextWrapped("- Impersonating another player");
                        CkGui.TextWrapped("- Promoting / Initating Toxicity towards others");
                        ImGui.Spacing();
                        CkGui.ColorTextWrapped("Miss-use of reporting will result in your account being timed out.", ImGuiColors.DalamudRed);
                    }
                }
            }

            // Draw the gold line split.
            var reportBoxSize = new Vector2(250 + 192 + ImGui.GetStyle().ItemSpacing.X);
            drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.AchievementLineSplit], rectMin + new Vector2(15, 235), new Vector2(770, 6));

            ImGui.SetCursorScreenPos(rectMin + new Vector2(15, 235));
            CkGui.FontTextWrapped("We will analyze reports with care. Cordy has been a victum " +
                "of manipulation and abuse multiple times, and will do her best to ensure her team does not allow " +
                "predators to exploit this reporting system on you.", Fonts.Default150Percent);

            using var font = Fonts.UidFont.Push();
            // Get the center of this screen.
            var disableButton = _reportReason.IsNullOrWhitespace() || string.Equals(_reportReason, DEFAULT_REASON, StringComparison.OrdinalIgnoreCase);
            var buttonSize = ImGuiHelpers.GetButtonSize($"Report {_reportedDisplayName} To CK");
            var buttonOffset = (ImGui.GetContentRegionAvail() - buttonSize) / 2;

            ImGui.SetCursorPos(ImGui.GetCursorPos() + buttonOffset);
            using (ImRaii.Disabled(disableButton))
            {
                if (ImGui.Button($"Report {_reportedDisplayName} To CK"))
                {
                    ImGui.CloseCurrentPopup();
                    var reason = _reportReason;
                    switch (_reportType)
                    {
                        case ReportKind.Profile:
                            _ = _hub.UserReportProfile(new(_reportedUser, reason));
                            break;
                        case ReportKind.Chat:
                            // Otherwise, create the dto to send.
                            _ = _hub.UserReportChat(new(_reportedUser, reason, _compressedChatData));
                            break;
                        default:
                            // For all other cases, do nothing.
                            break;
                    }
                }
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

    public void Open(OpenReportUIMessage msg)
    {
        _reportedUser = msg.UserToReport;
        _reportedDisplayName = _pairs.DirectPairs.Any(x => x.UserData.UID == _reportedUser.UID)
            ? _reportedUser.AliasOrUID
            : "Kinkster-" + _reportedUser.UID.Substring(_reportedUser.UID.Length - 4);
        _reportReason = DEFAULT_REASON;
        if (msg.Kind is ReportKind.Chat)
            _compressedChatData = _globalChat.GetRecentChatForReport();
    }
}
