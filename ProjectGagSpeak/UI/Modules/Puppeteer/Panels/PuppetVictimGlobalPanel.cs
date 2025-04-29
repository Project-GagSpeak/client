using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Tutorial;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using ImGuiNET;
using OtterGui.Text;
using OtterGui.Widgets;

namespace GagSpeak.CkCommons.Gui.Modules.Puppeteer;
public sealed partial class PuppetVictimGlobalPanel
{
    private readonly ILogger<PuppetVictimGlobalPanel> _logger;
    private readonly MainHub _hub;
    private readonly GlobalData _globals;
    private readonly PuppeteerManager _manager;
    private readonly FavoritesManager _favorites;
    private readonly TutorialService _guides;

    private LazyCSVCache _triggerPhraseCSV { get; init; }
    private TagButtons _phraseButtonList = new();

    public PuppetVictimGlobalPanel(
        ILogger<PuppetVictimGlobalPanel> logger,
        MainHub hub,
        GlobalData globals,
        PuppeteerManager manager,
        FavoritesManager favorites,
        TutorialService guides)
    {
        _logger = logger;
        _hub = hub;
        _globals = globals;
        _manager = manager;
        _favorites = favorites;
        _guides = guides;

        _triggerPhraseCSV = new LazyCSVCache(() => _globals.GlobalPerms?.TriggerPhrase ?? string.Empty);
        _triggerPhraseCSV.StringChanged += UpdateTriggerPhrase;
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, PuppeteerTabs tabMenu)
    {
        // Icon TabBar
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("PuppeteerTopLeft", drawRegions.TopLeft.Size))
            tabMenu.Draw(drawRegions.TopLeft.Size, ImGuiHelpers.ScaledVector2(4));

        // Permission Handling & Examples.
        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        DrawPermsAndExamples(drawRegions.BotLeft);

        // Search Bar
        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("PuppeteerTopRight", drawRegions.TopRight.Size))
            DrawAliasSearch(drawRegions.TopRight.SizeX);

        // Alias List
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("PuppeteerBotRight", drawRegions.BotRight.Size, false, WFlags.NoScrollbar))
            DrawAliasItems(drawRegions.BotRight.Size);
    }

    private string _searchStr = string.Empty;
    private void DrawAliasSearch(float width)
    {
        var change = FancySearchBar.Draw("##GlobalAliasSearch", width, "Search for an Alias", ref _searchStr, 200);
        if(change)
            _logger.LogInformation($"Searching for Alias: {_searchStr}");
    }

    private void DrawAliasItems(Vector2 region)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12);
        using var _ = ImRaii.Child("GlobalAliasList", region, false, WFlags.AlwaysUseWindowPadding);
    }

    private void DrawPermsAndExamples(CkHeader.DrawRegion region)
    {
        DrawPermissionsBox(region);

        var verticalShift = new Vector2(0, ImGui.GetItemRectSize().Y + ImGui.GetStyle().WindowPadding.Y);
        ImGui.SetCursorScreenPos(region.Pos + verticalShift);
        DrawExamplesBox(region.Size - verticalShift);

        // Draw the grey strip thingy here.
    }

    private void DrawPermissionsBox(CkHeader.DrawRegion drawRegion)
    {
        using var _ = ImRaii.Group();

        var wdl = ImGui.GetWindowDrawList();
        wdl.ChannelsSplit(2);

        wdl.ChannelsSetCurrent(1);
        DrawPermissionsBoxForeground(drawRegion, wdl);

        wdl.ChannelsSetCurrent(0);


        wdl.ChannelsMerge();
    }

    private void DrawPermissionsBoxForeground(CkHeader.DrawRegion drawRegion, ImDrawListPtr drawlist)
    {
        var paddedRegion = drawRegion.Size - ImGui.GetStyle().WindowPadding * 2;
        var triggerPhraseSize = new Vector2(drawRegion.SizeX, ImGui.GetFrameHeightWithSpacing() * 4);

        ImGui.SetCursorScreenPos(drawRegion.Pos + ImGui.GetStyle().WindowPadding);
        using var _ = ImRaii.Group();

        var cursorPos = ImGui.GetCursorPos();
        var pos = ImGui.GetCursorScreenPos();
        using (ImRaii.Group())
        {
            var headerSize = new Vector2(drawRegion.SizeX, ImGui.GetFrameHeight());
            var headerMaxPos = drawRegion.Pos + headerSize;
            drawlist.AddRectFilled(drawRegion.Pos, headerMaxPos, CkColor.VibrantPink.Uint(), ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersTopLeft);
            drawlist.AddLine(drawRegion.Pos with { Y = headerMaxPos.Y + 2 }, headerMaxPos + new Vector2(0, 2), CkColor.FancyHeaderContrast.Uint(), 4);

            ImGui.SetCursorPosX(cursorPos.X + ImGui.GetFrameHeight());
            ImUtf8.TextFrameAligned("Global Puppeteer Settings");
        }

        // extract the tabs by splitting the string by comma's
        using (CkRaii.FramedChildPadded("Trigger Phrases", triggerPhraseSize, CkColor.FancyHeader.Uint()))
        {
            _triggerPhraseCSV.Sync();

            if (_phraseButtonList.Draw("", "", _triggerPhraseCSV, out var editedTag) is { } idx && idx != -1)
            {
                if (idx < _triggerPhraseCSV.Count)
                {
                    if (editedTag.Length == 0) 
                        _triggerPhraseCSV.Remove(idx);
                    else
                        _triggerPhraseCSV.Rename(idx, editedTag);
                }
                else
                    _triggerPhraseCSV.Add(editedTag);
            }
        }


    }

    private void UpdateTriggerPhrase(string newPhrase)
    {
        _logger.LogInformation($"Updated own pair permission: TriggerPhrase to {newPhrase} from {_triggerPhraseCSV.CurrentCache}");
        _hub.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(nameof(UserGlobalPermissions.TriggerPhrase), newPhrase), UpdateDir.Own)).ConfigureAwait(false);
    }


    private void DrawExamplesBox(Vector2 region)
    {

    }

}
