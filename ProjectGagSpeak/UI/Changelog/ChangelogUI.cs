using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Changelog;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using OtterGui.Text;
using OtterGuiInternal;
using SharpYaml;
using System.Text.Json;

namespace GagSpeak.Gui;

public class ChangelogUI : WindowMediatorSubscriberBase
{
    private enum ChangelogPage { Changelog, Contributors }

    // Log Data
    private ChangelogFile? _changelogFile;
    private readonly List<string> _supporters = [];
    private readonly List<string> _contributors = [];

    private bool _scrollUp = false;
    private static float _closeWidth = 100f * ImGuiHelpers.GlobalScale;
    private ChangelogPage _selectedPage = ChangelogPage.Changelog;
    private readonly Vector2 _defaultSize = new(710, 745);

    public ChangelogUI(ILogger<ChangelogUI> logger, GagspeakMediator mediator)
        : base(logger, mediator, "Changelog UI")
    {
        this.SetBoundaries(_defaultSize, new(710, 1500));
        this.PinningClickthroughFalse();
        AllowBackgroundBlur = false;
        ShowCloseButton = false;
    }

    public override void OnOpen()
    {
        // Load the YAML on open, if not already loaded, not on startup.
        if (_changelogFile is null)
            LoadData().Wait();

        _scrollUp = true;
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(2f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 12f);
        ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, ImGui.GetColorU32(ImGuiCol.TitleBg));
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, ImGui.GetColorU32(ImGuiCol.TitleBg));
        ImGui.PushStyleColor(ImGuiCol.Header, 0x90545454);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0xDE6B6B6B);
        CkGui.CenterNextWindow(_defaultSize.X, _defaultSize.Y, ImGuiCond.Appearing);
        base.PreDraw();
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor(4);
        base.PostDraw();
    }

    protected override void DrawInternal()
    {
        var headerBottom = DrawHeader();
        ImGui.SetCursorScreenPos(headerBottom);
        ImGui.Spacing();

        DrawContent();

        ImGui.Separator();
        CkGui.SetCursorXtoCenter(_closeWidth);
        if (CkGui.IconTextButtonCentered(FAI.SquareXmark, "Close", _closeWidth))
            IsOpen = false;
    }

    private Vector2 DrawHeader()
    {
        var winPtr = ImGuiInternal.GetCurrentWindow();
        var style = ImGui.GetStyle();
        var innerMinPos = winPtr.InnerRect.Min + new Vector2(0, style.WindowPadding.Y);
        var innerMaxPos = winPtr.InnerRect.Max;
        winPtr.DrawList.PushClipRect(innerMinPos, innerMaxPos, false);

        var width = innerMaxPos.X - innerMinPos.X;
        var image = CosmeticService.CoreTextures.Cache[CoreTexture.ChangelogBanner];
        var scale = width / image.Size.X;
        var size = image.Size * scale;
        var spacing = ImUtf8.ItemSpacing;

        // Top Image
        winPtr.DrawList.AddDalamudImage(image, innerMinPos, size);

        // Below draw out the buttons.
        winPtr.DrawList.AddDalamudImage(image, innerMinPos, size);

        // Below draw out the buttons.
        var drawPos = innerMinPos + new Vector2(0, size.Y);
        var boxH = ImUtf8.FrameHeight + spacing.Y * 2;
        winPtr.DrawList.AddRectFilled(drawPos, drawPos + new Vector2(width, boxH), new Vector4(0.12f, 0.12f, 0.15f, 0.6f).ToUint());

        ImGui.SetCursorScreenPos(drawPos + new Vector2(0, spacing.Y));
        var buttonW = (width - spacing.X) / 2;

        if (CkGui.IconTextButtonCentered(FAI.Book, "Changelog", buttonW, true))
            _selectedPage = ChangelogPage.Changelog;

        ImGui.SameLine();

        if (CkGui.IconTextButtonCentered(FAI.PeopleGroup, "Contributors", buttonW, true, true))
            _selectedPage = ChangelogPage.Contributors;

        drawPos += new Vector2(0, boxH);
        winPtr.DrawList.AddRectFilledMultiColor(drawPos, innerMaxPos, 0x489567D2, 0x489567D2, 0, 0);
        winPtr.DrawList.AddLine(drawPos, drawPos + new Vector2(width, 0), 0xFF000000);

        winPtr.DrawList.PopClipRect();

        return drawPos;
    }

    private void DrawContent()
    {
        var contentArea = ImGui.GetContentRegionAvail() - new Vector2(0, ImUtf8.FrameHeight + ImUtf8.ItemSpacing.Y * 3 + ImGui.GetStyle().WindowPadding.Y);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f).Push(ImGuiStyleVar.WindowPadding, new Vector2(6f)).Push(ImGuiStyleVar.ScrollbarRounding, 2f);
        using var _ = ImRaii.Child("contents", contentArea);
        if (!_) return;

        if (_scrollUp)
        {
            _scrollUp = false;
            ImGui.SetScrollHereY(0);
        }

        if (_selectedPage is ChangelogPage.Changelog)
            DrawChangelog();
        else if (_selectedPage is ChangelogPage.Contributors)
            DrawContributors();
    }
    private void DrawChangelog()
    {
        var winPtr = ImGuiInternal.GetCurrentWindow();
        if (winPtr.SkipItems)
            return;

        if (_changelogFile is null)
        {
            CkGui.FontTextCentered("Loading Changelog...", Fonts.SubtitleFont);
            if (CosmeticService.Loading.GetWrapOrDefault() is { } wrap)
            {
                var size = new Vector2(ImGui.GetContentRegionAvail().X * .5f);
                CkGui.SetCursorXtoCenter(size.X);
                ImGui.Image(wrap.Handle, size);
            }
            return;
        }

        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f);

        if (!string.IsNullOrWhiteSpace(_changelogFile.Tagline))
        {
            CkGui.InlineSpacingInner();
            CkGui.FontTextAligned(_changelogFile.Tagline, Fonts.HeaderFont);

            // Subline
            if (!string.IsNullOrWhiteSpace(_changelogFile.Subline))
            {
                CkGui.InlineSpacingInner();
                CkGui.ColorText(_changelogFile.Subline, 0x88FFFFFF);
            }
            // Padding
            ImGui.Spacing();
        }

        // Draw out all versions
        if (_changelogFile.Changelog is not { } log || log.Count is 0)
            return;

        // Draw out all versions.
        for (var i = 0; i < log.Count; i++)
        {
            var isFirst = i is 0;
            DrawChangelogVersion(winPtr, log[i], isFirst ? 0xFF7FE57F : 0xFFD8BFBF, isFirst);
        }
    }

    private void DrawContributors()
    {
        ImGui.Text("Nothing here yet!");
    }

    private void DrawChangelogVersion(ImGuiWindowPtr winPtr, ChangelogVersion logEntry, uint titleColor, bool isFirst)
    {
        if (!DrawCollapsingVersionHeader(logEntry, titleColor, isFirst))
            return;
        // Optional Header Message
        if (!string.IsNullOrEmpty(logEntry.HeaderMessage))
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8 * ImGuiHelpers.GlobalScale);
            ImGui.Text(logEntry.HeaderMessage);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8 * ImGuiHelpers.GlobalScale);
        }
        // Draw out all segments for that version
        foreach (var segment in logEntry.Segments)
            DrawVersionSegment(winPtr, segment);
    }
    private bool DrawCollapsingVersionHeader(ChangelogVersion logEntry, uint titleColor, bool isFirst)
    {
        var flags = isFirst ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;

        ImGui.PushStyleColor(ImGuiCol.Text, titleColor);
        var isOpen = ImGui.CollapsingHeader($" {logEntry.Version} — {logEntry.Date} ", flags);
        ImGui.PopStyleColor();

        ImGui.SameLine();
        CkGui.ColorText(logEntry.Title, 0xFFD8BFBF);
        return isOpen;
    }

    private void DrawVersionSegment(ImGuiWindowPtr winPtr, VersionSegment segment)
    {
        var bgMax = winPtr.DC.CursorPos + new Vector2(ImGui.GetContentRegionAvail().X, ImUtf8.FrameHeight);
        var accentColor = AccentToColor(segment.Accent);
        // Fix coloring later.
        winPtr.DrawList.AddRectFilled(winPtr.DC.CursorPos, bgMax, 0x99261E1E, 4f);
        //winPtr.DrawList.AddRectFilled(winPtr.DC.CursorPos, winPtr.DC.CursorPos + new Vector2(3, bgMax.Y - winPtr.DC.CursorPos.Y), accentColor, 2f);

        var icon = segment.Icon is FAI.None ? FAI.SquareFull : segment.Icon;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2 * ImGuiHelpers.GlobalScale);
        CkGui.FramedIconText(icon, accentColor);
        ImUtf8.SameLineInner();
        CkGui.FontTextAligned(segment.Title, Fonts.GameFont, accentColor);

        // Simple direct bullets.
        using var _ = ImRaii.PushIndent(10f * ImGuiHelpers.GlobalScale);
        
        if (segment.Bullets.Count > 0)
            foreach (var item in segment.Bullets)
                BulletText(item);

        // Then the sub-sections afterwards for segmented changelog details.
        if (segment.Subsections.Count is not 0)
        {
            foreach (var sub in segment.Subsections)
            {
                if (sub.Bullets.Count is 0)
                    continue;
                CkGui.FontText(sub.Title, Fonts.GameFont);
                foreach (var item in sub.Bullets)
                    BulletText(item);
                ImGui.Spacing();
            }
        }

        ImGui.Spacing();

        void BulletText(ChangelogBullet bullet)
        {
            CkGui.BulletText(bullet.Text, 0xFF7FE57F, bullet.IsImportant);
            if (!string.IsNullOrWhiteSpace(bullet.Contributor))
                CkGui.ColorTextInline($" - {bullet.Contributor}", GsCol.ShopKeeperColor.Vec4(), false);
        }
    }

    private static uint AccentToColor(AccentColor accent) => accent switch
    {
        AccentColor.None => ImGui.GetColorU32(ImGuiCol.Text),
        AccentColor.Gold => 0xFF6CD5FF,
        AccentColor.Yellow => ImGuiColors.DalamudYellow.ToUint(),
        AccentColor.Green => ImGuiColors.HealerGreen.ToUint(),
        AccentColor.Blue => 0xFFFFCC00,
        AccentColor.Purple => 0xFFFFA7ED,
        AccentColor.Teal => 0xFFF9FF6C,
        AccentColor.Red => 0xFF5D5DFF,
        AccentColor.Pink => 0xFFB576FF,
        AccentColor.Grey => CkCol.DdsFolderBorder.Uint(),
        _ => ImGui.GetColorU32(ImGuiCol.Text)
    };

    public Task LoadData()
    {
        return Task.Run(async () =>
        {
            var path = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Assets", "changelog.yaml");
            // Attempt to read in the resource stream.
            var yaml = await File.ReadAllTextAsync(path);

            var options = new YamlSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                // Ignore extra YAML fields, not missing ones
                DefaultIgnoreCondition = YamlIgnoreCondition.WhenReading,
            };

            _changelogFile = YamlSerializer.Deserialize<ChangelogFile>(yaml, options);
            if (_changelogFile is null)
                return;

            Validate(_changelogFile);
        });

        static void Validate(ChangelogFile file)
        {
            if (!Enum.IsDefined(file.Icon))
                throw new InvalidDataException($"Invalid root icon: {file.Icon}");

            foreach (var version in file.Changelog)
            {
                if (string.IsNullOrWhiteSpace(version.Version))
                    throw new InvalidDataException("Missing version");

                // Ensure Segments is never null
                version.Segments ??= [];

                foreach (var seg in version.Segments)
                {
                    // Catch YAML null-overwrites
                    seg.Bullets ??= [];
                    seg.Subsections ??= [];

                    if (!Enum.IsDefined(seg.Icon))
                        seg.Icon = FAI.None;
                    if (!Enum.IsDefined(seg.Accent))
                        seg.Accent = AccentColor.None;

                    foreach (var sub in seg.Subsections)
                        sub.Bullets ??= [];
                }
            }
        }
    }
}
