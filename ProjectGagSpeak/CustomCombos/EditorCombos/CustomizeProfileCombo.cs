using GagSpeak.Gui;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagspeakAPI.Data.Struct;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace GagSpeak.CustomCombos.Editor;

public sealed class CustomizeProfileCombo : CkFilterComboCache<CustomizeProfile>
{
    private readonly GagspeakMediator _mediator;
    private Guid _currentItem;
    public CustomizeProfileCombo(ILogger log, GagspeakMediator mediator) : base(() => CustomizePlusCache.CPlusProfileList, log)
    {
        _mediator = mediator;
        _currentItem = Guid.Empty;
        SearchByParts = false;
    }

    protected override void DrawList(float width, float itemHeight)
    {
        base.DrawList(width, itemHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            Current = Items[NewSelection.Value];
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current.ProfileGuid == _currentItem)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.ProfileGuid == _currentItem);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, Guid currentProfile, float width, float innerWidth)
    {
        InnerWidth = innerWidth;
        _currentItem = currentProfile;
        // Maybe there is a faster way to know this, but atm I do not know.
        var previewName = Items.FirstOrDefault(i => i.ProfileGuid == _currentItem).ProfileName ?? "Select a Profile...";
        bool selected = Draw($"##{label}", previewName, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
        CkGui.AttachToolTip("[CTRL + L-Click] -> Fetch latest profile list.");
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && KeyMonitor.CtrlPressed())
            _mediator.Publish(new CustomizeProfileListRequest());

        return selected;
    }

    protected override string ToString(CustomizeProfile Profile)
        => Profile.ProfileName;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        using var id = ImRaii.PushId(globalIdx);
        var profile = Items[globalIdx];
        var ret = ImGui.Selectable(profile.ProfileName, selected);
        CkGui.AttachToolTip("Bound Guid: " + profile.ProfileGuid);
        return ret;
    }
}
