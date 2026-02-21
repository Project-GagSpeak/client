using CkCommons;
using CkCommons.DrawSystem;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.DrawSystem;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class MarionettesTab : IFancyTab
{
    private readonly ILogger<MarionettesTab> _logger;
    private readonly MainHub _hub;
    private readonly ReactionsDrawer _aliasDrawer;
    private readonly MarionetteDrawer _drawer;
    private readonly MarionetteDrawSystem _dds;
    private readonly PuppeteerManager _manager;
    private readonly TutorialService _guides;

    private TagCollection _triggersBox = new();

    private AliasTrigger? _selected => _drawer.Selected;

    public MarionettesTab(ILogger<MarionettesTab> logger, MainHub hub, ReactionsDrawer aliasDrawer,
        MarionetteDrawer drawer, MarionetteDrawSystem dds, PuppeteerManager manager, TutorialService guides)
    {
        _logger = logger;
        _hub = hub;
        _aliasDrawer = aliasDrawer;
        _drawer = drawer;
        _dds = dds;
        _manager = manager;
        _guides = guides;
    }

    public string   Label       => "Marionettes";
    public string   Tooltip     => "See what control kinksters surrendered to you, and their shared aliases.";
    public bool     Disabled    => false;

    // should be very similar to drawing out the list of items, except this will have a unique flavor to it.
    public void DrawContents(float width)
    {
        using var _ = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, ImUtf8.FramePadding - new Vector2(0, 1));

        var leftW = width * 0.45f;
        var rounding = FancyTabBar.BarHeight * .4f;

        using (ImRaii.Group())
        {
            DrawMarionetteCombo(leftW, rounding);
            _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.MarionettesPairs, PuppeteerUI.LastPos, PuppeteerUI.LastSize);
            DrawMarionetteAliases(leftW, rounding);
            _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.MarionettesPairAliases, PuppeteerUI.LastPos, PuppeteerUI.LastSize);
        }
        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            DrawMarionettesPerms(CkStyle.GetFrameRowsHeight(6).AddWinPadY(), rounding);
            _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.MarionettesPairPermissions, PuppeteerUI.LastPos, PuppeteerUI.LastSize);
            DrawAliasPreview(rounding);
            _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.MarionettesPairAliasConfig, PuppeteerUI.LastPos, PuppeteerUI.LastSize);
        }
    }

    private void DrawMarionetteCombo(float width, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedW("Combo", width, ImUtf8.FrameHeight, 0, GsCol.VibrantPink.Uint(), rounding);
        if (_dds.DrawMarionetteCombo(_.InnerRegion.X, 1f))
        {
            _logger.LogInformation("We selected a Marionette!");
        }
        CkGui.AttachToolTip("Stores the list of Kinksters that set trigger phrases for you or have enabled puppeteer permissions." +
            "--SEP--To interact with these Kinksters, they must have your name, which you can send after selecting a Kinkster.");
    }

    private void DrawMarionetteAliases(float leftWidth, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedWH("marionette_aliases", new Vector2(leftWidth, ImGui.GetContentRegionAvail().Y), 0, GsCol.VibrantPink.Uint(), rounding);

        _drawer.DrawFilterRow(_.InnerRegion.X, 40);
        _drawer.DrawContents(flags: DynamicFlags.SelectableLeaves);
    }

    private void DrawMarionettesPerms(float innerHeight, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedW("marionette", ImGui.GetContentRegionAvail().X, innerHeight, 0, GsCol.VibrantPink.Uint(), rounding);

        if (_dds.SelectedMarionette is not { } marionette)
        {
            CkGui.CenterColorTextAligned("No Marionette Selected", ImGuiColors.DalamudRed);
            return;
        }
        using (ImRaii.Group())
        {
            CkGui.IconTextAligned(FAI.User);
            if (marionette.IsListeningToClient)
            {
                CkGui.ColorTextFrameAlignedInline(PlayerData.NameWithWorld, CkCol.TriStateCheck.Vec4Ref());
                CkGui.AttachToolTip($"{marionette.GetDisplayName()} is associating your PlayerName with your Kinkster.", ImGuiColors.TankBlue);
            }
            else
            {
                CkGui.ColorTextFrameAlignedInline($"{marionette.GetNickAliasOrUid()}'s not listening to you!", ImGuiColors.DalamudRed);
                CkGui.HelpText($"{marionette.GetDisplayName()} can't perform orders from you until you send them your name." +
                    $"--SEP--Do so by using the --COL--Sync Button--COL-- to the right.", ImGuiColors.TankBlue, true);
            }
            var endX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
            ImGui.SameLine(endX - CkGui.IconTextButtonSize(FAI.CloudUploadAlt, "Send Name"));
            if (CkGui.IconTextButton(FAI.CloudUploadAlt, "Send Name", disabled: UiService.DisableUI, isInPopup: true))
            {
                _logger.LogInformation("Syncing Player Name with Marionette.");
                UiService.SetUITask(async () =>
                {
                    await GagspeakEx.WaitForPlayerLoading().ConfigureAwait(false);
                    var nameWorld = PlayerData.NameWithWorld;
                    var res = await _hub.UserSendNameToKinkster(new(marionette.UserData, nameWorld)).ConfigureAwait(false);
                    if (res.ErrorCode is not GagSpeakApiEc.Success)
                        _logger.LogWarning($"Failed to send Player Name to Marionette: {res.ErrorCode}");
                    else
                    {
                        _logger.LogInformation($"Successfully sent Player Name to Marionette.");
                        marionette.UpdateIsListening(true);
                    }
                });
            }
            CkGui.AttachToolTip($"Sends {marionette.GetDisplayName()} your Name@World, allowing them to respond to your orders.");
        }
        _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.MarionettesPairName, PuppeteerUI.LastPos, PuppeteerUI.LastSize);

        using (ImRaii.Group())
        {
            CkGui.IconTextAligned(FAI.Fingerprint);
            CkGui.TextFrameAlignedInline("Triggers");
            if (!marionette.PairPerms.IgnoreTriggerCase)
                CkGui.ColorTextFrameAlignedInline("(Case Sensitive)", ImGuiColors.DalamudGrey2);

            // Trigger Phrases inside of a framed child
            using (var phraseBox = CkRaii.FramedChildPaddedW("triggers", _.InnerRegion.X, CkStyle.GetFrameRowsHeight(2), 0, GsCol.RemoteLines.Uint(), rounding, DFlags.RoundCornersAll))
            {
                var triggers = marionette.PairPerms.TriggerPhrase;
                _triggersBox.DrawTagsPreview("##box", triggers);
            }

            // Brackets
            CkGui.IconTextAligned(FAI.Code);
            CkGui.TextFrameAlignedInline("Custom Scope Brackets");

            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(ImGui.GetTextLineHeight());
            var sChar = marionette.PairPerms.StartChar.ToString();
            ImGui.InputText("##BracketBegin", ref sChar, 1, ITFlags.ReadOnly);
            CkGui.AttachToolTip($"Optional Start character that scopes an order following a trigger phrase.");

            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(ImGui.GetTextLineHeight());
            var eChar = marionette.OwnPerms.EndChar.ToString();
            ImGui.InputText("##BracketEnd", ref eChar, 1, ITFlags.ReadOnly);
            CkGui.AttachToolTip($"Optional End character that scopes an order following a trigger phrase.");
        }
        _guides.OpenTutorial(TutorialType.Puppeteer, StepsPuppeteer.MarionettesPairTriggerWords, PuppeteerUI.LastPos, PuppeteerUI.LastSize);

        // Next row, draw out the permissions in color text.
        CkGui.IconTextAligned(FAI.Flag);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        CkGui.ColorTextBool("Sit/CPose", marionette.PairPerms.PuppetPerms.HasAny(PuppetPerms.Sit));
        ImGui.SameLine();
        CkGui.ColorTextBool("Emotes", marionette.PairPerms.PuppetPerms.HasAny(PuppetPerms.Emotes));
        ImGui.SameLine();
        CkGui.ColorTextBool("Aliases", marionette.PairPerms.PuppetPerms.HasAny(PuppetPerms.Alias));
        ImGui.SameLine();
        CkGui.ColorTextBool("Any/All", marionette.PairPerms.PuppetPerms.HasAny(PuppetPerms.All));

        // Get the height, this will be the square ratio that the image is drawn in
        //if (CosmeticService.CoreTextures.Cache[CoreTexture.PuppetMarionette] is { } wrap)
        //{
        //    var imgSize = new Vector2(perms.InnerRegion.Y);
        //    ImGui.SameLine(perms.InnerRegion.X - imgSize.X);
        //    ImGui.Image(wrap.Handle, imgSize);
        //}
    }

    private void DrawAliasPreview(float rounding)
    {
        // This will cover the remaining content region
        using var _ = CkRaii.FramedChildPaddedWH("Preview", ImGui.GetContentRegionAvail(), 0, GsCol.VibrantPink.Uint(), rounding);
        if (_selected is not { } alias)
        {
            CkGui.ColorTextCentered("Select an Alias to preview it here!", ImGuiColors.DalamudRed);
            return;
        }

        CkGui.IconTextAligned(FAI.AssistiveListeningSystems);
        CkGui.TextFrameAlignedInline("Detects \"");
        ImGui.SameLine(0, 0);
        CkGui.ColorTextFrameAligned(alias.InputCommand, ImGuiColors.TankBlue);
        ImGui.SameLine(0, 0);
        CkGui.TextFrameAligned("\"");

        if (alias.IgnoreCase)
        {
            ImGui.SameLine();
            CkGui.RightFrameAlignedColor("Ignores Case", ImGuiColors.DalamudGrey2);
        }

        CkGui.SeparatorSpaced(GsCol.VibrantPink.Uint());
        if (alias.Actions.Count is 0)
        {
            CkGui.ColorTextCentered("No Reactions assigned!", ImGuiColors.DalamudRed);
            return;
        }

        // Determine height and draw
        var rows = alias.Actions.Count;
        using var inner = CkRaii.Child("reacitons", new Vector2(_.InnerRegion.X, CkStyle.GetFrameRowsHeight(rows)));

        foreach (var reaction in alias.Actions.ToList())
        {
            switch (reaction)
            {
                case TextAction ta: _aliasDrawer.DrawTextRow(ta); break;
                case GagAction ga: _aliasDrawer.DrawGagRow(ga); break;
                case RestrictionAction rsa: _aliasDrawer.DrawRestrictionRow(rsa); break;
                case RestraintAction rta: _aliasDrawer.DrawRestraintRow(rta); break;
                case MoodleAction ma: _aliasDrawer.DrawMoodleRow(ma); break;
                case PiShockAction ps: _aliasDrawer.DrawShockRow(ps); break;
                case SexToyAction sta: _aliasDrawer.DrawToyRow(sta); break;
            }
        }
    }
}
