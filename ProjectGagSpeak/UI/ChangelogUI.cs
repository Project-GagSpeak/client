using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.Components;
using ImGuiNET;
using OtterGui;
using System.Numerics;
namespace GagSpeak.UI;

internal class ChangelogUI : WindowMediatorSubscriberBase
{
    private readonly GagspeakConfigService _configService;
    private readonly UiSharedService _uiSharedService;
    public ChangelogUI(ILogger<ChangelogUI> logger, GagspeakMediator mediator,
        GagspeakConfigService configService, UiSharedService uiSharedService) 
        : base(logger, mediator, "GagSpeak ChangeLog")
    {
        _configService = configService;
        _uiSharedService = uiSharedService;
        SizeConstraints = new()
        {
            MinimumSize = new(550, 300),
            MaximumSize = new(550, 450)
        };

        Flags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize;

        // Init changelog Data
        changelog = new Changelog();
        // set selected version to latest version
        selectedVersion = changelog.Versions.FirstOrDefault();
    }

    private VersionEntry? selectedVersion = null;
    private Changelog changelog;

    protected override void PreDrawInternal() { }
    protected override void PostDrawInternal() { }
    protected override void DrawInternal()
    {
        var region = ImGui.GetContentRegionAvail();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var topLeftSideHeight = region.Y;
        // create the draw-table for the selectable and viewport displays
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(5f * _uiSharedService.GetFontScalerFloat(), 0));
        using (var table = ImRaii.Table($"ChangelogUITable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV))
        {
            if (!table) return;
            // setup columns.
            ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, 125f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextColumn();

            var regionSize = ImGui.GetContentRegionAvail();
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));

            using (var leftChild = ImRaii.Child($"###GagSetupLeft", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar))
            {
                if(ImGui.Button("Close Changelog", new Vector2(ImGui.GetContentRegionAvail().X, 25f)))
                {
                    this.Toggle();
                }
                ImGui.Separator();

                DrawEntries();
            }
            // pop pushed style variables and draw next column.
            ImGui.PopStyleVar();
            ImGui.TableNextColumn();

            DrawSelectedVersionEntries();
        }
    }

    private void DrawEntries()
    {
        using var child = ImRaii.Child("ChangelogEntries", ImGui.GetContentRegionAvail());
        if (!child) return;


        // Group versions by Major and Minor
        var groupedVersions = changelog.Versions
            .GroupBy(v => (v.Major, v.Minor, v.Build))
            .OrderBy(g => g.Key.Major)
            .ThenBy(g => g.Key.Minor)
            .ThenBy(g => g.Key.Build).Reverse();

        foreach (var group in groupedVersions)
        {
            var groupLabel = $"Version {group.Key.Major}.{group.Key.Minor}.{group.Key.Build}.X";
            var groupFlags = ImGuiTreeNodeFlags.None;

            bool isOpened = false;
            using (var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudViolet))
            {
                isOpened = ImGui.TreeNodeEx(groupLabel, groupFlags);
            }

            if (!isOpened) continue;

            var sortedVersions = group
                .OrderBy(v => v.Build)
                .ThenBy(v => v.Fix);

            foreach (var entry in sortedVersions.Reverse())
            {
                using (var id = ImRaii.PushId(entry.VersionString))
                {
                    var flags = selectedVersion==entry ? ImGuiTreeNodeFlags.Selected | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet : ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet;
                    if (ImGui.TreeNodeEx(entry.VersionString, flags))
                    {
                        if (ImGui.IsItemClicked())
                        {
                            selectedVersion = entry;
                        }
                        ImGui.TreePop(); 
                    }
                }
            }
            ImGui.TreePop();

        }
    }

    private void DrawVersionInfo(VersionEntry version, ref int idx)
    {
        using var id = ImRaii.PushId(idx++);
        using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
        var flags = ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.Leaf;

        if (ImGui.TreeNodeEx(version.VersionString, flags))
        {
            if (ImGui.IsItemClicked())
            {
                selectedVersion = version;
            }
        }
    }

    private void DrawSelectedVersionEntries()
    {
        if (selectedVersion == null) return;

        foreach (var category in selectedVersion.CategorizedEntries)
        {
            // if the catagory has no entries, continue
            if (category.Value.Count == 0) continue;
            ImGui.Text(GetCategoryHeader(category.Key));
            foreach (var entry in category.Value)
            {
                entry.DrawContents();
            }

        }
    }

    private string GetCategoryHeader(EntryType type)
    {
        return type switch
        {
            EntryType.MainUpdatePoint => "Main Update Points",
            EntryType.FeatureAddition => "Feature Additions",
            EntryType.QualityOfLife => "Quality of Life",
            EntryType.BugFix => "Bug Fixes",
            _ => "UNKNOWN TYPE",
        };
    }
}
