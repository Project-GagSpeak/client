using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

// Attributions & References:
// --------------------------
// - Main ChatLog pointer logic adapted from older Dalamud:
//   https://github.com/ottercorp/Dalamud/blob/f89e9ebca547a7a14fbd6acb64ee7d8cf666f1b3/Dalamud/Game/Gui/GameGui.cs#L283C19-L283C33
// - Chat input visibility pattern from Caraxi SimpleTweaks:
//   https://github.com/Caraxi/SimpleTweaksPlugin/blob/0cf2c68a2e6411d667af0851ca36f0ff59d21626/Tweaks/Chat/HideChatAuto.cs#L26

namespace GagSpeak.GameInternals.Addons;

/// <summary>
///     Essential Info Nessisary for GagSpeak that comes from the Addon_chatLogPanels*.
/// </summary>
public static unsafe class AddonChatLog
{
    private const uint TEXT_INPUT_CURSOR_ID = 2;
    private const uint TEXT_INPUT_NODE_ID = 5;

    /// <summary> 
    ///     The Area in the Chat Box with the Chat-Input field and button row.
    /// </summary>
    private static AddonChatLogPanel* _mainChatLog => (AddonChatLogPanel*)(AtkUnitBase*)AtkHelper.GetAddonByName("ChatLog");

    /// <summary>
    ///     The 4 Chat Panels in your Chat Box.
    /// </summary>
    private static AddonChatLogPanel*[] _chatLogPanels =>
    [
        (AddonChatLogPanel*)(AtkUnitBase*)AtkHelper.GetAddonByName("ChatLogPanel_0"),
        (AddonChatLogPanel*)(AtkUnitBase*)AtkHelper.GetAddonByName("ChatLogPanel_1"),
        (AddonChatLogPanel*)(AtkUnitBase*)AtkHelper.GetAddonByName("ChatLogPanel_2"),
        (AddonChatLogPanel*)(AtkUnitBase*)AtkHelper.GetAddonByName("ChatLogPanel_3")
    ];

    // Exposed Properties.
    public static unsafe bool IsChatInputVisible => HasValidRoot(_mainChatLog) && _mainChatLog->AtkUnitBase.RootNode->IsVisible();
    public static unsafe bool IsChatInputFocused
    {
        get
        {
            var node = GetChatInputCursorNode();
            return node != null && node->IsVisible();
        }
    }

    // Exposed Methods.
    public static unsafe void EnsureNoChatInputFocus()
    {
        var node = GetChatInputCursorNode();

        if (node != null && node->IsVisible())
            RaptureAtkModule.Instance()->ClearFocus();
    }

    public static unsafe void SetChatInputVisibility(bool state)
    {
        if (HasValidRoot(_mainChatLog))
            _mainChatLog->AtkUnitBase.RootNode->ToggleVisibility(state);
    }

    public static unsafe void SetChatPanelVisibility(bool state)
    {
        foreach (var panel in _chatLogPanels)
        {
            if (!HasValidRoot(panel))
                continue;
            // Toggle the visibility of the panel's root node.
            panel->AtkUnitBase.RootNode->ToggleVisibility(state);
        }
    }

    // Private Methods.
    /// <summary> Validates an <see cref="AddonChatLogPanel"/>'s space in memory. </summary>
    /// <returns> If AddonChatLogPanel* != null and if its AtkUnitBase.RootNode != null </returns>
    private static bool HasValidRoot(AddonChatLogPanel* panel)
        => panel != null && panel->AtkUnitBase.RootNode != null;

    /// <summary>
    ///     Obtains the Chat Input's Cursor Node to interact with it's State.
    /// </summary>
    /// <returns> the <see cref="AtkResNode"/> of the InputCursor for the Chat Input. </returns>
    private static AtkResNode* GetChatInputCursorNode()
    {
        // Validate ChatInput Node
        if(!HasValidRoot(_mainChatLog)) 
            return null;

        // Grab the TextInput Node & Validate path to UldManager.
        var txtInputNode = (AtkComponentNode*)_mainChatLog->AtkUnitBase.GetNodeById(TEXT_INPUT_NODE_ID);
        if (txtInputNode is null || txtInputNode->Component is null)
            return null;

        // Scan the UldManager for a node with the TEXT_INPUT_CURSOR_ID
        var uldManager = txtInputNode->Component->UldManager;
        if (uldManager.NodeList is null)
            return null;

        // Inline search for the node by ID
        for (var i = 0; i < uldManager.NodeListCount; i++)
        {
            var node = uldManager.NodeList[i];
            // If true, this is the TextInputCursorNode
            if (node != null && node->NodeId == TEXT_INPUT_CURSOR_ID)
                return node;
        }

        return null;
    }
}
