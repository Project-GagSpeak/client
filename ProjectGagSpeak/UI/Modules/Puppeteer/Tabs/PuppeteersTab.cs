using CkCommons;
using CkCommons.DrawSystem;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.DrawSystem;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class PuppeteersTab : IFancyTab
{
    private readonly ILogger<PuppeteersTab> _logger;
    private readonly MainHub _hub;
    private readonly PuppeteersDrawer _drawer;
    private readonly PuppeteerManager _manager;
    private readonly TutorialService _guides;

    private TagCollection _triggersBox = new();

    private Kinkster? _selected => _drawer.Selected;

    public PuppeteersTab(ILogger<PuppeteersTab> logger, MainHub hub,
        PuppeteersDrawer drawer, PuppeteerManager manager, TutorialService guides)
    {
        _logger = logger;
        _hub = hub;
        _drawer = drawer;
        _manager = manager;
        _guides = guides;
    }

    public string Label => "Puppeteers";
    public string Tooltip => "Manage how others can puppeteer you, distinct for each person.";
    public bool Disabled => false;

    // should be very similar to drawing out the list of items, except this will have a unique flavor to it.
    public void DrawContents(float width)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, ImUtf8.FramePadding - new Vector2(0,1));

        var leftW = width * 0.45f;
        var rounding = FancyTabBar.BarHeight * .4f;
        using (ImRaii.Group())
            DrawPuppeteers(leftW, rounding);
        _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.PuppeteersPairs, PuppeteerUI.LastPos, PuppeteerUI.LastSize,
            () => { /*select first kinkster in the list if we can, some tutorial stuff won't work otherwise?*/ });

        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            DrawSelectedPuppeteer(CkStyle.GetFrameRowsHeight(9).AddWinPadY(), rounding);
            _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.PuppeteersPairSettings, PuppeteerUI.LastPos, PuppeteerUI.LastSize);
            DrawMarionetteStats(rounding);
        }
    }

    private void DrawPuppeteers(float leftWidth, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedWH("list", new Vector2(leftWidth, ImGui.GetContentRegionAvail().Y), 0, GsCol.VibrantPink.Uint(), rounding);

        _drawer.DrawFilterRow(_.InnerRegion.X, 40);
        _drawer.DrawContents(flags: DynamicFlags.SelectableLeaves);
    }

    private void DrawSelectedPuppeteer(float innerHeight, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedW("Sel", ImGui.GetContentRegionAvail().X, innerHeight, 0, GsCol.VibrantPink.Uint(), rounding);

        if (_selected is not { } kinkster)
        {
            CkGui.CenterColorTextAligned("No Puppeteer Selected", ImGuiColors.DalamudRed);
            return;
        }

        using (ImRaii.Group())
        {
            CkGui.IconTextAligned(FAI.User);
            if (_manager.Puppeteers.TryGetValue(kinkster.UserData.UID, out var puppeteerData))
            {
                CkGui.TextFrameAlignedInline("Puppeteered by:");
                CkGui.ColorTextFrameAlignedInline(puppeteerData.NameWithWorld, CkCol.TriStateCheck.Vec4());
                CkGui.AttachToolTip($"{kinkster.GetDisplayName()} is associated with this PlayerName." +
                    $"--SEP--They can not puppeteer you within the boundaries you set." +
                    $"--SEP----COL--If they changed Name/World, they will need to send you it again.--COL--", ImGuiColors.TankBlue);
            }
            else
            {
                CkGui.ColorTextFrameAlignedInline("Kinkster's PlayerName not yet Stored!", ImGuiColors.DalamudRed);
                CkGui.HelpText("There is currently no PlayerName stored for this Kinkster." +
                    "--NL----COL--They will need to send you theirs for you to react to them.--COL--", ImGuiColors.TankBlue, true);
            }
        }
        _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.PuppeteersPairName, PuppeteerUI.LastPos, PuppeteerUI.LastSize);

        using (ImRaii.Group())
        {
            CkGui.IconTextAligned(FAI.Fingerprint);
            CkGui.TextFrameAlignedInline("Triggers");
            var ignoreCase = kinkster.OwnPerms.IgnoreTriggerCase;
            ImUtf8.SameLineInner();
            if (CkGui.Checkbox("##ignore-case", ref ignoreCase, UiService.DisableUI))
            {
                UiService.SetUITask(async () =>
                {
                    _logger.LogTrace($"Updating Ignore-Case to {ignoreCase}", LoggerType.Puppeteer);
                    // This updates the result between transit to look instant on the client end, reverting edit on failure.
                    await PermHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, nameof(PairPerms.IgnoreTriggerCase), ignoreCase);
                });
            }
            CkGui.ColorTextFrameAlignedInline("Ignore Case?", ImGuiColors.DalamudGrey2);

            // Trigger Phrases inside of a framed child
            using (var phraseBox = CkRaii.FramedChildPaddedW("triggers", _.InnerRegion.X, CkStyle.GetFrameRowsHeight(2), 0, GsCol.RemoteLines.Uint(), rounding, DFlags.RoundCornersAll))
            {
                var triggers = kinkster.OwnPerms.TriggerPhrase;
                if (_triggersBox.DrawTagsEditor("##box", triggers, out var updatedString, GsCol.VibrantPink.Vec4Ref()))
                {
                    _logger.LogTrace("The Tag Editor had an update!");
                    UiService.SetUITask(async () => await PermHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, nameof(PairPerms.TriggerPhrase), updatedString));
                }
            }
        }
        _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.PuppeteersPairTriggers, PuppeteerUI.LastPos, PuppeteerUI.LastSize);

        // Brackets
        using (ImRaii.Group())
            DrawBracketsRow(kinkster);
        _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.PuppeteersAdvanced, PuppeteerUI.LastPos, PuppeteerUI.LastSize,
            () => FancyTabBar.SelectTab("PuppeteerTabs", PuppeteerUI.PuppeteerTabs[2], PuppeteerUI.PuppeteerTabs));

        // Now the container that draws out the orders and the image.
        using var perms = CkRaii.Child("permissions", ImGui.GetContentRegionAvail());

        using (ImRaii.Group())
        {
            var filter = (uint)(kinkster.OwnPerms.PuppetPerms);
            using (ImRaii.Disabled(UiService.DisableUI))
            {
                foreach (var category in Enum.GetValues<PuppetPerms>().Skip(1))
                {
                    ImGui.CheckboxFlags($"Allow {category}", ref filter, (uint)category);

                    CkGui.AttachToolTip(category switch
                    {
                        PuppetPerms.All => $"Grant {kinkster.GetDisplayName()} access to all commands.--SEP--(Take Care with this)",
                        PuppetPerms.Alias => $"Allow {kinkster.GetDisplayName()} to make you execute alias triggers.",
                        PuppetPerms.Emotes => $"Allow {kinkster.GetDisplayName()} to make you perform emotes.",
                        PuppetPerms.Sit => $"Allow {kinkster.GetDisplayName()} to make you sit or cycle poses.",
                        _ => $"NO PERMS ALLOWED."
                    });
                }
            }
            // Check for updates
            if (kinkster.OwnPerms.PuppetPerms != (PuppetPerms)filter)
                UiService.SetUITask(async () => await PermHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, nameof(PairPerms.PuppetPerms), (PuppetPerms)filter));
        }
        _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.PuppeteersPairOrders, PuppeteerUI.LastPos, PuppeteerUI.LastSize);

        // Get the height, this will be the square ratio that the image is drawn in
        if (CosmeticService.CoreTextures.Cache[CoreTexture.PuppetPuppeteers] is { } wrap)
        {
            var imgSize = new Vector2(perms.InnerRegion.Y);
            ImGui.SameLine(perms.InnerRegion.X - imgSize.X);
            ImGui.Image(wrap.Handle, imgSize);
        }
    }

    private void DrawBracketsRow(Kinkster puppeteer)
    {
        CkGui.IconTextAligned(FAI.Code);
        CkGui.TextFrameAlignedInline("Custom Scope Brackets");

        var sChar = puppeteer.OwnPerms.StartChar.ToString();
        var eChar = puppeteer.OwnPerms.EndChar.ToString();
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetTextLineHeight());
        ImGui.InputText("##BracketBegin", ref sChar, 1);
        // Handle this way instead of on updates to prevent forcing the string to not be empty
        if (ImGui.IsItemDeactivated() && sChar.Length == 1)
        {
            if (!char.IsWhiteSpace(sChar, 0) && sChar[0] != puppeteer.OwnPerms.StartChar)
            {
                _logger.LogTrace($"Updating Start Bracket as it changed to: {sChar}");
                UiService.SetUITask(async () => await PermHelper.ChangeOwnUnique(_hub, puppeteer.UserData, puppeteer.OwnPerms, nameof(PairPerms.StartChar), sChar[0]));
            }
        }
        CkGui.AttachToolTip($"Optional Start character that scopes an order following a trigger phrase.");

        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetTextLineHeight());
        ImGui.InputText("##BracketEnd", ref eChar, 1);
        // Handle this way instead of on updates to prevent forcing the string to not be empty
        if (ImGui.IsItemDeactivated() && eChar.Length == 1)
        {
            if (!char.IsWhiteSpace(eChar, 0) && eChar[0] != puppeteer.OwnPerms.EndChar)
            {
                _logger.LogTrace($"Updating End Bracket as it changed to: {sChar}");
                UiService.SetUITask(async () => await PermHelper.ChangeOwnUnique(_hub, puppeteer.UserData, puppeteer.OwnPerms, nameof(PairPerms.EndChar), eChar[0]));
            }
        }
        CkGui.AttachToolTip($"Optional End character that scopes an order following a trigger phrase.");
    }

    private void DrawMarionetteStats(float rounding)
    {
        var region = ImGui.GetContentRegionAvail();
        using var _ = CkRaii.FramedChildPaddedWH("Stats", ImGui.GetContentRegionAvail(), 0, GsCol.VibrantPink.Uint(), rounding);

        if (_selected is not { } puppeteer)
            return;

        CkGui.FontTextCentered("Marionette Stats", Fonts.UidFont);
        CkGui.Separator(GsCol.VibrantPink.Uint());

        // Fallback in the case that this puppeteer is not yet tracked for us.
        if (!_manager.Puppeteers.TryGetValue(puppeteer.UserData.UID, out var data))
        {
            CkGui.ColorTextCentered("No Puppeteer Data Found", ImGuiColors.DalamudRed);
            return;
        }

        // Otherwise display the outcome
        ImGui.Text("Puppeteered:");
        CkGui.ColorTextInline($"{data.OrdersRecieved} Times", ImGuiColors.TankBlue);

        ImGui.Text("Sit Reactions:");
        CkGui.ColorTextInline($"{data.SitOrders}", ImGuiColors.TankBlue);

        ImGui.Text("Emote Reactions:");
        CkGui.ColorTextInline($"{data.EmoteOrders}", ImGuiColors.TankBlue);

        ImGui.Text("Alias Reactions:");
        CkGui.ColorTextInline($"{data.AliasOrders}", ImGuiColors.TankBlue);

        ImGui.Text("Other Reactions:");
        CkGui.ColorTextInline($"{data.OtherOrders}", ImGuiColors.TankBlue);
    }
}
