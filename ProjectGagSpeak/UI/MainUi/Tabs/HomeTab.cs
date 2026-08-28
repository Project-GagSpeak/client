using CkCommons;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Chat;
using GagSpeak.Gui.Profile;
using GagSpeak.Gui.Publications;
using GagSpeak.Gui.Remote;
using GagSpeak.Gui.Toybox;
using GagSpeak.Gui.Wardrobe;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using OtterGui.Text;
using OtterGuiInternal;
using System.Globalization;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace GagSpeak.Gui.MainWindow;

public class HomeTab
{
    private const string NAME_TOOLTIP = "Your Profile's Alias / UID." +
        "--SEP----COL--[L-Click]:--COL-- Copy your UID" +
        "--NL----COL--[CTRL + L-Click]:--COL-- Copy your Alias";

    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly ChatService _chatService;
    private readonly KinkPlateService _kinkplates;
    private readonly TutorialService _guides;

    private bool _editingSafeword = false;
    public HomeTab(GagspeakMediator mediator, MainConfig config, ChatService chat,
        KinkPlateService kinkplates, TutorialService guides)
    {
        _mediator = mediator;
        _config = config;
        _chatService = chat;
        _kinkplates = kinkplates;
        _guides = guides;
    }

    public void DrawSection()
    {
        DrawBackdrop();

        using var _ = ImRaii.Child("homepage", ImGui.GetContentRegionAvail(), false, flags: WFlags.AlwaysUseWindowPadding);

        DrawHeadingLeft();
        ImGui.SameLine();
        DrawHeadingRight();

        // Draw out the selectable options here, just like in our settings options.
        using var s = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, ImGuiHelpers.ScaledVector2(6, 7));
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImUtf8.ItemSpacing.Y);
        using (CkRaii.Child("modules-sidenav", new(ImGui.GetContentRegionAvail().X, -1), wFlags: WFlags.NoScrollbar))
            DrawMenuNav(ImGui.GetContentRegionAvail().X);
    }

    private void DrawHeadingRight()
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        var profile = _kinkplates.GetUserProfile(MainHub.OwnUserData);
        if (profile.GetIconWrapOrDefault() is not { } wrap)
            return;

        var winPtr = ImGuiInternal.GetCurrentWindow();
        if (winPtr.SkipItems)
            return;

        var canEdit = MainHub.Reputation.CanEditProfiles;
        var id = ImGui.GetID("icon-image");
        var style = ImGui.GetStyle();

        var sizeY = CkGui.CalcFontTextSize("A", Fonts.SubtitleFont).Y + CkGui.CalcFontTextSize("A", Fonts.HeaderFont).Y + style.ItemInnerSpacing.Y;
        var drawSize = new Vector2(sizeY);
        var drawRadius = drawSize.X * .5f;
        var drawPos = new Vector2(winPtr.InnerRect.Max.X - drawSize.X - style.WindowPadding.X / 2, winPtr.InnerRect.Min.Y + style.WindowPadding.Y);

        var bb = new ImRect(drawPos, drawPos + drawSize);
        var drawBox = new ImRect(bb.Min + style.FramePadding, bb.Max - style.FramePadding);

        winPtr.DC.CursorPos = drawPos;
        ImGuiInternal.ItemSize(drawSize, style.FramePadding.Y);
        if (!ImGuiP.ItemAdd(bb, id, null))
            return;

        bool hovered = false, active = false;
        var clicked = ImGuiP.ButtonBehavior(bb, id, ref hovered, ref active);
        // Render item
        ImGuiP.RenderNavHighlight(bb, id);
        ImGuiP.RenderFrame(bb.Min, bb.Max, 0);

        winPtr.DrawList.AddDalamudImage(wrap, drawPos, drawSize);
        if (hovered || active)
        {
            var icon = canEdit ? FAI.PenAlt : FAI.Eye;
            var iconSize = CkGui.IconSize(icon);
            var circleCol = active ? 0xBB555555 : hovered ? 0x99444444 : 0xAA333333;
            winPtr.DrawList.AddRectFilled(drawPos, drawPos + drawSize, circleCol);
            var penDrawPos = drawPos + (drawSize - iconSize) * .5f;
            using (Fonts.IconFramedFont.Push())
                winPtr.DrawList.AddText(penDrawPos, ImGui.GetColorU32(hovered ? ImGuiCol.Text : ImGuiCol.TextDisabled), icon.ToIconString());
        }
        winPtr.DrawList.AddRect(drawPos, drawPos + drawSize, 0xFF999999, 2f * ImGuiHelpers.GlobalScale);

        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ProfileEditing, MainUI.LastPos, MainUI.LastSize,
            _ => _mediator.Publish(new UiToggleMessage(typeof(KinkPlateEditorUI), ToggleType.Show)));

        if (clicked)
        {
            if (canEdit)
                _mediator.Publish(new UiToggleMessage(typeof(KinkPlateEditorUI), ToggleType.Show));
            else
                _mediator.Publish(new OpenUserLightProfileMessage(MainHub.OwnUserData));
        }
        if (hovered)
        {
            using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 6f)
                .Push(ImGuiStyleVar.WindowRounding, 4f)
                .Push(ImGuiStyleVar.PopupBorderSize, 1f);
            using var c = ImRaii.PushColor(ImGuiCol.Border, GsCol.VibrantPink.Vec4());
            ImGui.BeginTooltip();
            ImGui.Text("Your Profile Icon.");
            if (canEdit)
                CkGui.ColorText("Click to open editor.", ImGuiColors.DalamudGrey2);
            else
            {
                CkGui.ColorText("Click to preview profile.", ImGuiColors.DalamudGrey2);
                CkGui.ColorText("Your Reputation is preventing edit access.", CkCol.TriStateCross.Vec4());
                var timeout = MainHub.Reputation.ProfileEditTimeout;
                if (timeout > DateTime.UtcNow)
                    CkGui.ColorTextInline($"Timeout expires in {(timeout - DateTime.UtcNow).ToTimeSpanStr()}", ImGuiColors.DalamudGrey, false);
            }
            ImGui.EndTooltip();
        }
    }

    private void DrawBackdrop()
    {
        var winPtr = ImGuiInternal.GetCurrentWindow();
        var style = ImGui.GetStyle();
        // Get clipped rect for the background.
        var startY = winPtr.DC.CursorPos.Y - (style.ItemSpacing.Y * .5f);
        var drawMin = new Vector2(winPtr.InnerRect.Min.X, startY);
        var drawMax = winPtr.InnerRect.Max;
        var drawRegion = drawMax - drawMin;

        winPtr.DrawList.PushClipRect(drawMin, drawMax, false);
        winPtr.DrawList.AddRectFilled(drawMin, drawMax, 0xCC333333, style.WindowRounding, DFlags.RoundCornersBottom);

        // Image Background (Use placeholder for now)
        if (CosmeticService.TryGetBackground(PlateElement.PlateLight, KinkPlateBG.Default, out var wrap) && wrap is { } bgWrap)
        {
            // Ensure it is drawn at the correct scale.
            var drawWidth = drawMax.X - drawMin.X;
            var imgScale = drawWidth / bgWrap.Width;
            var drawHeight = bgWrap.Height * imgScale;
            var imgMax = new Vector2(drawMin.X + drawWidth, drawMin.Y + drawHeight);
            // How far the blur spreads
            var blurRadius = 4f * ImGuiHelpers.GlobalScale;

            // We use a very low alpha hex for the stacked images. 
            uint blurColor = 0x15444444;
            uint centerColor = 0x44444444;
            Span<Vector2> offsets =
            [
                new(-blurRadius, -blurRadius), new(blurRadius, -blurRadius),
                new(-blurRadius,  blurRadius), new(blurRadius,  blurRadius),
                new(0, -blurRadius),           new(0,  blurRadius),
                new(-blurRadius, 0),           new(blurRadius, 0)
            ];

            // Fake blur effect.
            foreach (var offset in offsets)
                winPtr.DrawList.AddImageRounded(bgWrap.Handle, drawMin + offset, imgMax + offset, Vector2.Zero, Vector2.One, blurColor, style.WindowRounding, DFlags.RoundCornersBottom);
            // Image center overlay.
            winPtr.DrawList.AddImageRounded(bgWrap.Handle, drawMin, imgMax, Vector2.Zero, Vector2.One, centerColor, style.WindowRounding, DFlags.RoundCornersBottom);

            // Get bottom fade transition and solid, and draw if nessisary
            var fadeHeight = drawHeight * 0.15f;
            var fadeTopY = imgMax.Y - fadeHeight;
            if (drawMax.Y > fadeTopY)
            {
                var fadeTop = new Vector2(drawMin.X, fadeTopY);
                var fadeBot = new Vector2(drawMax.X, imgMax.Y);
                winPtr.DrawList.AddRectFilledMultiColor(fadeTop, fadeBot, 0, 0, 0xFF000000, 0xFF000000);
                // Then draw out a solid fill for the remainder of the space.
                if (imgMax.Y < drawMax.Y)
                {
                    var solidTop = new Vector2(drawMin.X, imgMax.Y);
                    winPtr.DrawList.AddRectFilled(solidTop, drawMax, 0xFF000000, style.WindowRounding, DFlags.RoundCornersBottom);
                }
            }
        }

        winPtr.RenderCustomResizeGrips();
        winPtr.DrawList.PopClipRect();
    }

    private void DrawHeadingLeft()
    {
        using var _ = ImRaii.Group();

        if (!MainHub.IsConnectionDataSynced)
            return;

        var userData = MainHub.OwnUserData;
        var profile = _kinkplates.GetUserProfile(userData);
        var nameCol = userData.Color.HasValue ? userData.Color.Value : ImGui.GetColorU32(ImGuiCol.Text);
        var uid = MainHub.UID;

        var region = ImGui.GetContentRegionAvail();
        var gScale = ImGuiHelpers.GlobalScale;
        var gapX = 5f * gScale;
        var offset = new Vector2(3f);
        var radius = 2f;

        using (ImRaii.PushColor(ImGuiCol.Text, nameCol, userData.Color.HasValue))
            using (Fonts.SubtitleFont.Push())
                CkGui.TextShadowed(MainHub.AliasOrUID, nameCol, 0xFF000000, offset, radius);
        if (ImGui.IsItemClicked())
            ImGui.SetClipboardText(ImGui.GetIO().KeyCtrl ? userData.Alias : MainHub.UID);
        // Indicate UID can be copied here.
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            CkGui.ToolTipInternal(NAME_TOOLTIP, ImGuiColors.DalamudOrange);
        }
        // Beside it draw the edit button.
        ImUtf8.SameLineInner();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CkGui.CalcFontTextSize("A", Fonts.SubtitleFont).Y - ImUtf8.FrameHeightSpacing));
        if (CkGui.IconButton(FAI.PencilAlt, inPopup: true))
            _mediator.Publish(new UiToggleMessage(typeof(SettingsUi)));
        CkGui.AttachTooltip("Open Alias/Vanity Editor");

        // Below it, draw out the other data
        if (!string.IsNullOrEmpty(userData.VanityName))
        {
            CkGui.TextShadowed(userData.VanityName, 0xFF000000, offset, radius);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                ImGui.SetClipboardText(userData.VanityName);
            // Indicate UID can be copied here.
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                CkGui.ToolTipInternal("Copy VanityName");
            }
            ImGui.SameLine(0, gapX);
            CkGui.TextShadowed("•", 0xFF000000, offset, radius);
            ImGui.SameLine(0, gapX);
        }
        // UID
        CkGui.TextShadowed(userData.UID, 0xFF000000, offset, radius);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            ImGui.SetClipboardText(userData.UID);
        // Indicate UID can be copied here.
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            CkGui.ToolTipInternal("Copy UID");
        }
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ClientUID, MainUI.LastPos, MainUI.LastSize);

        // Creation Date
        var formattedDate = MainHub.OwnUserData.CreatedOn ?? DateTime.MinValue;
        var createdDate = formattedDate != DateTime.MinValue ? formattedDate.ToString("d", CultureInfo.CurrentCulture) : "MM-DD-YYYY";
        using (ImRaii.Group())
        {
            using (ImRaii.PushColor(ImGuiCol.Text, 0xFF888888))
            {
                using (Fonts.IconFramedFont.Push())
                    CkGui.TextShadowed(FAI.Calendar.ToIconString(), offset, radius);
                ImUtf8.SameLineInner();
                CkGui.TextShadowed(createdDate, 0xFF000000, offset, radius);
            }
        }
        CkGui.AttachTooltip("The date your account was made.");

        using (ImRaii.Group())
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGold))
            {
                using (Fonts.IconFramedFont.Push())
                    CkGui.TextShadowed(FAI.Award.ToIconString(), offset, radius);
                ImUtf8.SameLineInner();
                CkGui.TextShadowed($"{ClientAchievements.Completed}/{ClientAchievements.Total}", 0xFF000000, offset, radius);
            }
        }
        CkGui.AttachTooltip("Your current achievement progress.");

        using (ImRaii.Group())
        {
            using (ImRaii.PushColor(ImGuiCol.Text, 0xFF211098))
            {
                var strikes = MainHub.Reputation.WarningStrikes;
                using (Svc.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
                    CkGui.TextShadowed(FAI.ExclamationTriangle.ToIconString(), offset, radius);
                ImUtf8.SameLineInner();
                CkGui.TextShadowed($"{MainHub.Reputation.WarningStrikes} Strikes.", 0xFF000000, offset, radius);
            }
        }
        if (ImGui.IsItemClicked())
            _mediator.Publish(new OpenSettingsUI(6, 0));
        CkGui.AttachTooltip("Reflects current Account Standing.--NL--" +
            "--COL--Too many strikes can lead to restrictions or bans.--COL--", ImGuiColors.ParsedGrey);

        DrawSafewordRow();
        CkGui.AttachTooltip("Your current safeword. Click to edit!");
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.Safewords, MainUI.LastPos, MainUI.LastSize);


        void DrawSafewordRow()
        {
            using var col = ImRaii.PushColor(ImGuiCol.Text, 0xFF211098);
            using (Fonts.IconFramedFont.Push())
                CkGui.TextShadowed(FAI.HandPaper.ToIconString(), offset, radius);
            ImUtf8.SameLineInner();
            using var font = ImRaii.PushFont(UiBuilder.MonoFont);
            if (_editingSafeword)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(region.X * .5f);
                var safeword = _config.Data.Safeword;
                if (ImGui.InputTextWithHint("##safeword", "Set a Safeword..", ref safeword, 35))
                {
                    _config.Data.Safeword = safeword;
                    _config.Save();
                }
                if (ImGui.IsItemDeactivated())
                    _editingSafeword = false;
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    _editingSafeword = false;
                font.Dispose();
                CkGui.AttachTooltip("Enter to save, right-click to cancel.");
            }
            else
            {
                // Display based on if we have a safeword set or not.
                if (string.IsNullOrWhiteSpace(_config.Data.Safeword))
                    CkGui.TextShadowed("Click to set Safeword..", 0xFF000000);
                else
                    CkGui.TextShadowed(_config.Data.Safeword, CkCol.TriStateCross.Uint());
                font.Dispose();
                _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.SettingSafeword, MainUI.LastPos, MainUI.LastSize);
                // Toggle safeword editing.
                if (ImGui.IsItemClicked())
                    _editingSafeword = !_editingSafeword;
            }
        }
    }

    private uint _hidden;
    private uint _pinkFeint;
    private uint _pinkHovered;
    private uint _pinkPressed;
    private uint _pinkActive;

    private void DrawMenuNav(float width)
    {
        _pinkActive = GsCol.VibrantPink.Vec4().WithAlpha(0.25f).ToUint();
        _pinkPressed = GsCol.VibrantPink.Vec4().WithAlpha(0.35f).ToUint();
        _pinkHovered = GsCol.VibrantPink.Vec4().WithAlpha(0.15f).ToUint();
        _pinkFeint = GsCol.VibrantPink.Vec4().WithAlpha(0.05f).ToUint();
        _hidden = GsCol.VibrantPink.Vec4().WithAlpha(0f).ToUint();

        using var c = ImRaii.PushColor(ImGuiCol.Button, 0).Push(ImGuiCol.ButtonHovered, 0x19FFFFFF).Push(ImGuiCol.ButtonActive, 0x33FFFFFF);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0f, 0.5f));

        var winPtr = ImGuiInternal.GetCurrentWindow();
        var style = ImGui.GetStyle();

        ImGui.Spacing();
        ImGui.Spacing();

        // Draw out the heading
        using (Fonts.HeaderFont.Push())
            CkGui.TextShadowed("Modules", 0xFF000000, new Vector2(3f), 2f);
        var labelPos = ImGui.GetItemRectMin();
        var labelSize = ImGui.GetItemRectSize();
        var linePos = new Vector2(labelPos.X, labelPos.Y + labelSize.Y);
        winPtr.DrawList.PathLineTo(linePos);
        linePos.X += winPtr.InnerClipRect.GetSize().X;
        winPtr.DrawList.PathLineTo(linePos);
        winPtr.DrawList.PathStroke(uint.MaxValue);

        var region = ImGui.GetContentRegionAvail();
        var thresholdHeight = CkStyle.GetFrameRowsHeight(10);
        if (region.Y < thresholdHeight)
            DrawMenuNavCompact(winPtr, style, (region.X - style.ItemSpacing.X) * .5f);
        else
            DrawMenuNavList(winPtr, style, region.X);
    }

    private void DrawMenuNavList(ImGuiWindowPtr winPtr, ImGuiStylePtr style, float width)
    {
        if (DrawMenuButton(winPtr, style, FAI.ToiletPortable, "Wardrobe", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(WardrobeUI)));
        CkGui.AttachTooltip("Restraint Sets, Restrictions, Gags, and Collars");

        if (DrawMenuButton(winPtr, style, FAI.Coins, "Cursed Loot", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(CursedLootUI)));
        CkGui.AttachTooltip("Gamble away your fortunes and freedom with Cursed Loot!");

        if (DrawMenuButton(winPtr, style, FAI.PersonHarassing, "Puppeteer", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(PuppeteerUI)));
        CkGui.AttachTooltip("Who's in control now? (Global & Per-Kinkster Control)");

        if (DrawMenuButton(winPtr, style, FAI.Bolt, "Triggers", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(TriggersUI)));
        CkGui.AttachTooltip("Monitor events and react to them");

        if (DrawMenuButton(winPtr, style, FAI.BoxOpen, "Toybox", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(ToyboxUI)));
        CkGui.AttachTooltip("Inspect data of owned actors!");

        if (DrawMenuButton(winPtr, style, FAI.WaveSquare, "Sex Toy Remote", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI)));
        CkGui.AttachTooltip("Control Simulated, or IRL Sex Toys! --COL--[WIP]--COL--");
        
        if (DrawMenuButton(winPtr, style, FAI.FileAlt, "Mod Presets", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(ModPresetsUI)));
        CkGui.AttachTooltip("Configure presets for your Penumbra mod settings!" +
            "--NL----COL--NOTICE: Will migrate to Penumbra's ModPresets soon.--COL--", ImGuiColors.DalamudYellow);

        if (DrawMenuButton(winPtr, style, FAI.Comments, "GagSpeak Chats", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(ChatWindowUI)));
        CkGui.AttachTooltip("Standalone UI for DMs and GlobalChat.");

        if (DrawMenuButton(winPtr, style, FAI.Trophy, "Achievements", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(AchievementsUI)));
        CkGui.AttachTooltip("View Achievement Progress & Rewards.");

        if (DrawMenuButton(winPtr, style, FAI.CloudUploadAlt, "Publications", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(PublicationsUI)));
        CkGui.AttachTooltip("Publish created Patterns & LociData for others to enjoy!");

        if (DrawMenuButton(winPtr, style, FAI.Cog, "Settings Menu", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(SettingsUi)));
        CkGui.AttachTooltip("Opens the Settings UI.");

        if (DrawMenuButton(winPtr, style, FAI.Book, "View Changelog", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(ChangelogUI)));
        CkGui.AttachTooltip("See the latest patch notes for Sundouleia.");

        SupportButton(winPtr, style, width);
        FeedbackButton(winPtr, style, width);
    }

    private void DrawMenuNavCompact(ImGuiWindowPtr winPtr, ImGuiStylePtr style, float width)
    {
        using (ImRaii.Group())
        {
            if (DrawMenuButton(winPtr, style, FAI.ToiletPortable, "Wardrobe", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(WardrobeUI)));
            CkGui.AttachTooltip("Restraint Sets, Restrictions, Gags, and Collars");

            if (DrawMenuButton(winPtr, style, FAI.Coins, "Cursed Loot", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(CursedLootUI)));
            CkGui.AttachTooltip("Gamble away your fortunes and freedom with Cursed Loot!");

            if (DrawMenuButton(winPtr, style, FAI.PersonHarassing, "Puppeteer", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(PuppeteerUI)));
            CkGui.AttachTooltip("Who's in control now? (Global & Per-Kinkster Control)");

            if (DrawMenuButton(winPtr, style, FAI.Bolt, "Triggers", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(TriggersUI)));
            CkGui.AttachTooltip("Monitor events and react to them");

            if (DrawMenuButton(winPtr, style, FAI.BoxOpen, "Toybox", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(ToyboxUI)));
            CkGui.AttachTooltip("Inspect data of owned actors!");

            if (DrawMenuButton(winPtr, style, FAI.WaveSquare, "Sex Toy Remote", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI)));
            CkGui.AttachTooltip("Control Simulated, or IRL Sex Toys! --COL--[WIP]--COL--");

            if (DrawMenuButton(winPtr, style, FAI.FileAlt, "Mod Presets", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(ModPresetsUI)));
            CkGui.AttachTooltip("Configure presets for your Penumbra mod settings!" +
                "--NL----COL--NOTICE: Will migrate to Penumbra's ModPresets soon.--COL--", ImGuiColors.DalamudYellow);
        }

        ImGui.SameLine();
        using (ImRaii.Group())
        {
            if (DrawMenuButton(winPtr, style, FAI.Comments, "GagSpeak Chats", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(ChatWindowUI)));
            CkGui.AttachTooltip("Standalone UI for DMs and GlobalChat.");

            if (DrawMenuButton(winPtr, style, FAI.Trophy, "Achievements", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(AchievementsUI)));
            CkGui.AttachTooltip("View Achievement Progress & Rewards.");

            if (DrawMenuButton(winPtr, style, FAI.CloudUploadAlt, "Publications", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(PublicationsUI)));
            CkGui.AttachTooltip("Publish created Patterns & LociData for others to enjoy!");

            if (DrawMenuButton(winPtr, style, FAI.Cog, "Settings Menu", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(SettingsUi)));
            CkGui.AttachTooltip("Opens the Settings UI.");

            if (DrawMenuButton(winPtr, style, FAI.Book, "View Changelog", width, false))
                _mediator.Publish(new UiToggleMessage(typeof(ChangelogUI)));
            CkGui.AttachTooltip("See the latest patch notes for Sundouleia.");

            SupportButton(winPtr, style, width);
            FeedbackButton(winPtr, style, width);
        }
    }

    private void SupportButton(ImGuiWindowPtr winPtr, ImGuiStylePtr style, float width)
    {
        var isShift = ImGui.GetIO().KeyShift;
        if (DrawMenuButton(winPtr, style, FAI.Coffee, "Support GagSpeak", width, false))
        {
            var url = isShift ? "https://www.patreon.com/CordeliaMist" : "https://www.ko-fi.com/cordeliamist";
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Bagagwa e)
            {
                Svc.Logger.Error($"Failed to open the support link. {e.Message}");
            }
        }

        // Dynamically swap the tooltip text based on the shift state
        var targetPlatform = isShift ? "patreon.com" : "ko-fi.com";
        var swapHint = isShift ? "Release SHIFT for Ko-Fi" : "Hold SHIFT for Patreon";

        CkGui.AttachTooltip("This plugin took a massive toll on my life." +
            "--NL--As happy as I am to make this free for all of you to enjoy, any support is much appreciated ♥" +
            $"--NL--Will open --COL--{targetPlatform}--COL-- in a new browser window." +
            $"--NL--({swapHint})", ImGuiColors.DalamudOrange);

        // Ensure the tutorial only fires when in the Patreon state, as it did in your original code
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.SelfPlug, MainUI.LastPos, MainUI.LastSize);
    }

    private void FeedbackButton(ImGuiWindowPtr winPtr, ImGuiStylePtr style, float width)
    {
        if (DrawMenuButton(winPtr, style, FAI.ThumbsUp, "Positive Feedback", width, false))
        {
            try { Process.Start(new ProcessStartInfo { FileName = "https://forms.gle/4AL43XUeWna2DtYK7", UseShellExecute = true }); }
            catch (Bagagwa e) { Svc.Logger.Error($"Failed to open the google form. {e.Message}"); }
        }
        CkGui.AttachTooltip("Opens a short 1 question positive feedback form ♥" +
            "--SEP--They're a nice way for me to reflect how my efforts are positively impacting others~");
    }

    private bool DrawMenuButton(ImGuiWindowPtr winPtr, ImGuiStylePtr style, FAI icon, string label, float width, bool disabled)
    {
        var id = ImGui.GetID(label);
        var pos = winPtr.DC.CursorPos;
        var itemSize = new Vector2(width, ImUtf8.FrameHeight);
        var bb = new ImRect(pos, pos + itemSize);
        var drawBox = new ImRect(bb.Min + style.FramePadding, bb.Max - style.FramePadding);

        ImGuiInternal.ItemSize(itemSize, style.FramePadding.Y);
        if (!ImGuiP.ItemAdd(bb, id, null))
            return false;

        bool hovered = false, active = false;
        var clicked = ImGuiP.ButtonBehavior(bb, id, ref hovered, ref active);
        // Render item
        ImGuiP.RenderNavHighlight(bb, id);
        ImGuiP.RenderFrame(bb.Min, bb.Max, 0);
        // Feint outline that respects the highlight states.
        var frameCol = GetFrameBg(hovered, active);
        winPtr.DrawList.AddRect(bb.Min, bb.Max, CkGui.ApplyAlpha(frameCol, .075f), style.FrameRounding, ImDrawFlags.RoundCornersAll, 1.5f);
        winPtr.DrawList.AddRectFilled(bb.Min, bb.Max, CkGui.ApplyAlpha(frameCol, .1f), style.FrameRounding, ImDrawFlags.RoundCornersAll);

        // The button renders its "selected" visual state while hovered or pressed.
        if (!disabled && hovered || active)
        {
            // Darken the gold slightly if actively clicking it
            var bgMain = active ? _pinkPressed : _pinkActive;
            var seam = pos.X + width * 0.80f;
            winPtr.DrawList.AddRectFilledMultiColor(pos, new Vector2(seam, bb.Max.Y), bgMain, _pinkFeint, _pinkFeint, bgMain);
            winPtr.DrawList.AddRectFilledMultiColor(new Vector2(seam, bb.Min.Y), bb.Max, _pinkFeint, _hidden, _hidden, _pinkFeint);
            // Glowing Vertical left bar
            var gap = (bb.Max.Y - bb.Min.Y) * .15f;
            var barMin = new Vector2(bb.Min.X, bb.Min.Y + gap);
            var barMax = new Vector2(bb.Min.X + 3f * ImGuiHelpers.GlobalScale, bb.Max.Y - gap);
            var step = gap / 3f;

            for (int g = 3; g >= 1; g--)
            {
                var pad = g * step;
                var gMin = new Vector2(barMin.X - pad, barMin.Y - pad);
                var gMax = new Vector2(barMax.X + pad, barMax.Y + pad);
                var gCol = GsCol.VibrantPink.Vec4().WithAlpha(0.10f / g).ToUint();
                winPtr.DrawList.AddRectFilled(gMin, gMax, gCol);
            }
            winPtr.DrawList.AddRectFilled(barMin, barMax, GsCol.LushPinkButton.Uint());
        }

        var drawPos = drawBox.Min;
        var txtCol = ImGui.GetColorU32(disabled ? ImGuiCol.TextDisabled : ImGuiCol.Text);
        var iconSize = CkGui.IconSize(icon);
        using (Svc.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            winPtr.DrawList.AddTextShadowed(icon.ToIconString(), drawPos, txtCol, 0xFF000000);

        drawPos.X += iconSize.X + style.ItemInnerSpacing.X;
        winPtr.DrawList.AddTextShadowed(label, drawPos, txtCol, 0xFF000000);

        if (label == "GagSpeak Chats")
        {
            var unreadMentions = _chatService.AllUnreadMentions();
            if (unreadMentions > 0)
            {
                var txt = unreadMentions > 99 ? "99+" : unreadMentions.ToString();

                // Calculate exact dimensions
                var textSize = ImGui.CalcTextSize(txt);
                var padding = new Vector2(6f * ImGuiHelpers.GlobalScale, 0);
                var bubbleSize = textSize + (padding * 2);

                // Position it immediately to the right of the button's content area, vertically centered in the row
                var bubbleMin = new Vector2(drawBox.Max.X - bubbleSize.X - style.FramePadding.X, bb.Min.Y + (itemSize.Y - bubbleSize.Y) / 2);
                var bubbleMax = bubbleMin + bubbleSize;

                // Draw the pill-shaped background (rounding = half of the height)
                winPtr.DrawList.AddRectFilled(bubbleMin, bubbleMax, ImGuiColors.TankBlue.ToUint(), bubbleSize.Y / 2);

                // Draw the text perfectly centered inside the pill
                winPtr.DrawList.AddTextShadowed(txt, bubbleMin + padding, uint.MaxValue, 0xFF000000);
            }
        }

        return clicked && !disabled;

        uint GetFrameBg(bool hovered, bool held)
            => ImGui.GetColorU32((hovered, held) switch
            {
                (true, true) => ImGuiCol.FrameBgActive,
                (true, false) => ImGuiCol.FrameBgHovered,
                _ => ImGuiCol.FrameBg,
            });
    }
}
