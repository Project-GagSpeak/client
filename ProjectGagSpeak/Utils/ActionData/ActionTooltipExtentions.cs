using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagspeakAPI.Attributes;

namespace GagSpeak.Utils;
public static class ActionTooltipEx
{
    /// <summary>
    ///     Obtains the correct title based on the trait the action tooltip is for.
    /// </summary>
    /// <param name="trait"> The trait to get the title for.</param>
    /// <returns> The title of the trait.</returns>
    public static string GetTitle(Traits trait) => trait switch
    {
        Traits.Gagged => "Gagged!",
        Traits.Blindfolded => "Blinded!",
        Traits.Weighty => "Weighed Down!",
        Traits.Immobile => "Immobilized!",
        Traits.BoundLegs => "Legs in a Bind!",
        Traits.BoundArms => "Arms in a Bind!",
        _ => string.Empty,
    };

    /// <summary>
    ///     Obtains the correct description text based on the trait the action tooltip is for.
    /// </summary>
    /// <param name="trait"> The trait to get the description for.</param>
    /// <param name="sourceLabel"> The label of the source that applied the trait.</param>
    /// <returns> The description of the trait.</returns>
    public static SeString GetDescription(Traits trait, string sourceLabel)
    {
        var seString = new SeStringBuilder();
        seString.Append(trait switch
        {
            Traits.Gagged => "With a muffled voice, the spell falters — its power silenced before it can take form.",
            Traits.Blindfolded => "Unable to see your target’s movements, swift attacks from afar seem to always miss their mark.",
            Traits.Weighty => "Weighted by your binds, swift movement is now but a memory — replaced only by hobbled steps.",
            Traits.Immobile => "No matter how you strain, your body refuses to budge — locked in place and helpless to act.",
            Traits.BoundLegs => "Your legs make an effort to act, but the binds offer no give — stopping the action before it begins.",
            Traits.BoundArms => "Your arms make an effort to act, but the binds offer no give — stopping the action before it begins.",
            _ => "ERR.UNK_TRAIT"
        });
        seString.Add(new NewLinePayload());
        seString.Add(new UIForegroundPayload(504));
        seString.Add(new UIGlowPayload(505));
        seString.Append("Source:");
        seString.Add(new UIForegroundPayload(0));
        seString.Add(new UIGlowPayload(0));
        seString.Append(sourceLabel);
        return seString.Build();
    }

    /// <summary>
    ///     Replaces the text of a text node in an addon.
    ///     This is used to update the action tooltip with the correct trait information.
    /// </summary>
    /// <param name="addon">pointer to the addon</param>
    /// <param name="textNodeId">index of the text node within the addon</param>
    /// <param name="text">new text to use</param>
    /// <remarks> If the text node does not exist, it will do nothing. </remarks>
    public static unsafe void ReplaceTextNodeText(AtkUnitBase* addon, uint textNodeId, string text)
    {
        var textNode = addon->GetTextNodeById(textNodeId);
        if (textNode is null)
            return;

        textNode->SetText(text);
    }

    /// <summary>
    ///     Replaces the text of a text node in an addon.
    ///     This is used to update the action tooltip with the correct trait information.
    /// </summary>
    /// <param name="addon">pointer to the addon</param>
    /// <param name="textNodeId">index of the text node within the addon</param>
    /// <param name="text">new text to use</param>
    /// <remarks> If the text node does not exist, it will do nothing. </remarks>
    public static unsafe void ReplaceTextNodeText(AtkUnitBase* addon, uint textNodeId, SeString text)
    {
        var textNode = addon->GetTextNodeById(textNodeId);
        if (textNode is null)
            return;

        textNode->SetText(text.EncodeWithNullTerminator());
    }
}
