using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagspeakAPI.Data;
using ImGuiNET;

namespace GagSpeak.CkCommons.Gui.Modules.Puppeteer;

/// <summary> A sealed partial class that helps with pair-based selection and definition. </summary>
public sealed partial class PuppeteerHelper : DisposableMediatorSubscriberBase
{
    private readonly PuppeteerManager _manager;

    private PairCombo _pairCombo;

    public PuppeteerHelper(ILogger<PuppeteerHelper> logger, GagspeakMediator mediator,
        PairManager pairs, PuppeteerManager manager, FavoritesManager favorites, 
        MainConfig config) : base(logger, mediator)
    {
        _manager = manager;

        _pairCombo = new PairCombo(logger, pairs, favorites, () =>
        [
            ..pairs.DirectPairs
                .OrderByDescending(p => favorites._favoriteKinksters.Contains(p.UserData.UID))
                .ThenByDescending(u => u.IsVisible)
                .ThenByDescending(u => u.IsOnline)
                .ThenBy(pair => !pair.PlayerName.IsNullOrEmpty()
                    ? (config.Current.PreferNicknamesOverNames ? pair.GetNickAliasOrUid() : pair.PlayerName)
                    : pair.GetNickAliasOrUid(), StringComparer.OrdinalIgnoreCase)
        ]);

        Mediator.Subscribe<RefreshUiMessage>(this, _ =>
        {
            _pairCombo.RefreshPairList();
            OnPairUpdated?.Invoke();
        });
    }

    // This ensures we can notify anything that needs to update the selected pair.
    public event Action? OnPairUpdated;

    private string _selectedUid => SelectedPair?.UserData.UID ?? string.Empty;
    public Pair? SelectedPair => _pairCombo.Current; 
    public NamedAliasStorage? NamedStorage => _manager.PairAliasStorage.GetValueOrDefault(_selectedUid);
    public AliasStorage? Storage => NamedStorage?.Storage;
    public NamedAliasStorage? PairNamedStorage => SelectedPair?.LastPairAliasData;
    public AliasStorage? PairAliasStorage => PairNamedStorage?.Storage;

    public bool DrawPairSelector(string id, float width)
    {
        using var _ = ImRaii.Group();
        var oldPadding = ImGui.GetStyle().FramePadding;
        using var s = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f)
            .Push(ImGuiStyleVar.FramePadding, new Vector2(oldPadding.X, 1f));

        var shiftY = oldPadding.Y - ImGui.GetStyle().FramePadding.Y;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + shiftY);
        var pos = ImGui.GetCursorScreenPos();
        var size = new Vector2(width, ImGui.GetFrameHeight());
        ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, CkColor.FancyHeaderContrast.Uint(), ImGui.GetFrameHeight());
        bool change = _pairCombo.Draw(width, 1.25f);
        if(change)
        {
            Logger.LogInformation($"Selected Pair: {_pairCombo.Current?.GetNickAliasOrUid() ?? "None"} ({_pairCombo.Current?.UserData.ToString()})");
            OnPairUpdated?.Invoke();
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _pairCombo.ClearSelected();
            OnPairUpdated?.Invoke();
        }

        return change;
    }
}
