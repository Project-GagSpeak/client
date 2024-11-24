using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using GagSpeak.Services;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using OtterGui.Text;

namespace GagSpeak.UI;

internal class InteractionEventsUI : WindowMediatorSubscriberBase
{
    private readonly EventAggregator _eventAggregator;
    private readonly UiSharedService _uiShared;
    private bool ThemePushed = false;

    private List<InteractionEvent> CurrentEvents => _eventAggregator.EventList.Value.OrderByDescending(f => f.EventTime).ToList();
    private List<InteractionEvent> FilteredEvents => CurrentEvents.Where(f => (string.IsNullOrEmpty(FilterText) || ApplyDynamicFilter(f))).ToList();
    private string FilterText = string.Empty;
    private InteractionFilter FilterCatagory = InteractionFilter.All;

    public InteractionEventsUI(ILogger<InteractionEventsUI> logger, GagspeakMediator mediator,
        EventAggregator eventAggregator, UiSharedService uiShared) : base(logger, mediator, "Interaction Events Viewer")
    {
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse;
        AllowClickthrough = false;
        AllowPinning = false;
        
        _eventAggregator = eventAggregator;
        _uiShared = uiShared;
        SizeConstraints = new()
        {
            MinimumSize = new(500, 300),
            MaximumSize = new(600, 2000)
        };
    }

    private bool ApplyDynamicFilter(InteractionEvent f)
    {
        // Map each InteractionFilter type to the corresponding property check
        var filterMap = new Dictionary<InteractionFilter, Func<InteractionEvent, string>>
    {
        // For the Applier filter, combine ApplierNickAliasOrUID and ApplierUID
        { InteractionFilter.Applier, e => $"{e.ApplierNickAliasOrUID} {e.ApplierUID}" },
        { InteractionFilter.Interaction, e => e.InteractionType.ToString() },
        { InteractionFilter.Content, e => e.InteractionContent }
    };

        // If "All" is selected, return true if any of the fields contain the filter text
        if (FilterCatagory == InteractionFilter.All)
        {
            return filterMap.Values.Any(getField => getField(f).Contains(FilterText, StringComparison.OrdinalIgnoreCase));
        }

        // Otherwise, use the selected filter type to apply the filter
        return filterMap.TryGetValue(FilterCatagory, out var getField)
            && getField(f).Contains(FilterText, StringComparison.OrdinalIgnoreCase);
    }


    private void ClearFilters()
    {
        FilterText = string.Empty;
        _uiShared._selectedComboItems["Type##InteractionViewerFilterType"] = InteractionFilter.All;
        FilterCatagory = InteractionFilter.All;
    }

    public override void OnOpen()
    {
        ClearFilters();
        EventAggregator.UnreadInteractionsCount = 0;
    }

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));

            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }
    protected override void DrawInternal()
    {
        using (ImRaii.Group())
        {
            // Draw out the clear filters button
            if (_uiShared.IconTextButton(FontAwesomeIcon.Ban, "Clear"))
                ClearFilters();

            // On the same line, draw out the search bar.
            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(160f);
            ImGui.InputTextWithHint("##InteractionEventSearch", "Search Filter Text...", ref FilterText, 64, ImGuiInputTextFlags.EnterReturnsTrue);

            // On the same line, draw out the filter category dropdown
            ImUtf8.SameLineInner();
            _uiShared.DrawCombo("Type##InteractionViewerFilterType", 110f, Enum.GetValues<InteractionFilter>(), (type) => type.ToName(),
                (i) => FilterCatagory = i, FilterCatagory, flags: ImGuiComboFlags.NoArrowButton);

            // On the same line, at the very end, draw the button to open the event folder.
            var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.FolderOpen, "EventLogs");
            var distance = ImGui.GetContentRegionAvail().X - buttonSize;
            ImGui.SameLine(distance);
            if (_uiShared.IconTextButton(FontAwesomeIcon.FolderOpen, "EventLogs"))
            {
                ProcessStartInfo ps = new()
                {
                    FileName = _eventAggregator.EventLogFolder,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };
                Process.Start(ps);
            }
        }

        DrawInteractionsList();
    }

    private void DrawInteractionsList()
    {
        var cursorPos = ImGui.GetCursorPosY();
        var max = ImGui.GetWindowContentRegionMax();
        var min = ImGui.GetWindowContentRegionMin();
        var width = max.X - min.X;
        var height = max.Y - cursorPos;
        using var table = ImRaii.Table("interactionsTable", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg, new Vector2(width, height));
        if (!table)
            return;

        ImGui.TableSetupColumn("Time");
        ImGui.TableSetupColumn("Applier");
        ImGui.TableSetupColumn("Interaction");
        ImGui.TableSetupColumn("Details");
        ImGui.TableHeadersRow();

        foreach (var ev in FilteredEvents)
        {
            ImGui.TableNextColumn();
            // Draw out the time it was applied
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(ev.EventTime.ToString("T", CultureInfo.CurrentCulture));
            ImGui.TableNextColumn();
            // Draw out the applier
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(!string.IsNullOrEmpty(ev.ApplierNickAliasOrUID) ? ev.ApplierNickAliasOrUID : (!string.IsNullOrEmpty(ev.ApplierUID) ? ev.ApplierUID : "--")); 
            ImGui.TableNextColumn();
            // Draw out the interaction type
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(ev.InteractionType.ToName());
            ImGui.TableNextColumn();
            // Draw out the details
            ImGui.AlignTextToFramePadding();
            var posX = ImGui.GetCursorPosX();
            var maxTextLength = ImGui.GetWindowContentRegionMax().X - posX;
            var textSize = ImGui.CalcTextSize(ev.InteractionContent).X;
            var msg = ev.InteractionContent;
            while (textSize > maxTextLength)
            {
                msg = msg[..^5] + "...";
                textSize = ImGui.CalcTextSize(msg).X;
            }
            ImGui.TextUnformatted(msg);
            if (!string.Equals(msg, ev.InteractionContent, StringComparison.Ordinal))
                UiSharedService.AttachToolTip(ev.InteractionContent);
        }
    }
}
