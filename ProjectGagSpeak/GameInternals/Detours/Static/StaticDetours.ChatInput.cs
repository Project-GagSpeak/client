using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.String;
using GagSpeak.GameInternals.Agents;
using GagSpeak.PlayerClient;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using System.Text.RegularExpressions;

namespace GagSpeak.GameInternals.Detours;
public partial class StaticDetours
{
    /// <summary>
    ///     Intercepts messages after pressing enter and before they are sent off to the server.
    ///     Be very fucking careful with how you handle this as one slip-up makes you detectable.
    /// </summary>
    private unsafe delegate byte ProcessChatInputDelegate(IntPtr uiModule, byte** message, IntPtr a3);
    [Signature(Signatures.ProcessChatInput, DetourName = nameof(ProcessChatInputDetour), Fallibility = Fallibility.Auto)]
    private Hook<ProcessChatInputDelegate> ProcessChatInputHook { get; set; } = null!;

    /// <summary>
    ///     Intercepts messages after pressing enter and before they are sent off to the server.
    ///     Be very fucking careful with how you handle this as one slip-up makes you detectable.
    /// </summary>
    /// <remarks> Yes, you are wondering that is why I am not functionalizing any of this to not take risks. </remarks>
    private unsafe byte ProcessChatInputDetour(IntPtr uiModule, byte** message, IntPtr a3)
    {
        try
        {
            if (ClientData.Globals is not { } g || _gags.ServerGagData is not { } gagData)
                return ProcessChatInputHook.Original(uiModule, message, a3);

            // Grab the original string.
            var originalSeString = MemoryHelper.ReadSeStringNullTerminated((nint)(*message));
            var messageDecoded = originalSeString.ToString();

            // Debug the output (remove later)
            foreach (var payload in originalSeString.Payloads)
                Logger.LogTrace($"Message Payload [{payload.Type}]: {payload.ToString()}");

            Logger.LogTrace($"Message Payload Present", LoggerType.ChatDetours);

            if (string.IsNullOrWhiteSpace(messageDecoded))
                return ProcessChatInputHook.Original(uiModule, message, a3);

            // If we are not meant to garble the message, then return original.
            if (!g.ChatGarblerActive || !gagData.AnyGagActive())
                return ProcessChatInputHook.Original(uiModule, message, a3);

            /* -------------------------- MUFFLERCORE / GAGSPEAK CHAT GARBLER TRANSLATION LOGIC -------------------------- */
            // Firstly, make sure that we are setup to allow garbling in the current channel.
            var prefix = string.Empty;
            InputChannel channel = 0;
            var muffleMessage = g.AllowedGarblerChannels.IsActiveChannel((int)ChatLogAgent.CurrentChannel());

            // It's possible to be in a channel (ex. Say) but send (/party Hello World), we must check this.
            if (messageDecoded.StartsWith("/"))
            {
                // If its a command outside a chatChannel command, return original.
                if (!ChatLogAgent.IsPrefixForGsChannel(messageDecoded, out prefix, out channel))
                    return ProcessChatInputHook.Original(uiModule, message, a3);

                // Handle Tells, these are special, use advanced Regex to protect name mix-up
                if (channel is InputChannel.Tell)
                {
                    Logger.LogTrace($"[Chat Processor]: Matched Command is a tell command");
                    // Using /gag command on yourself sends /tell which should be caught by this
                    // Depends on the message to start like :"/tell {targetPlayer} *{playerPayload.PlayerName}"
                    // Since only outgoing tells are affected, {targetPlayer} and {playerPayload.PlayerName} will be the same
                    // TODO: (/t <1> /t <0> and /t <me> are self-tells, maybe adjust this to include those?)
                    var selfTellRegex = @"(?<=^|\s)/t(?:ell)?\s{1}(?<name>\S+\s{1}\S+)@\S+\s{1}\*\k<name>(?=\s|$)";

                    // If the condition is not matched here, it means we are performing a self-tell (someone is messaging us), so return original.
                    if (!Regex.Match(messageDecoded, selfTellRegex).Value.IsNullOrEmpty())
                    {
                        Logger.LogTrace("[Chat Processor]: Ignoring Message as it is a self tell garbled message.");
                        return ProcessChatInputHook.Original(uiModule, message, a3);
                    }

                    // Match any other outgoing tell to preserve target name including all valid placeholders that
                    // target another player: t, tell, r, reply, f, focus, tt, t2t and party members 2-8.
                    var tellRegex = @"(?<=^|\s)/t(?:ell)?\s{1}(?:\S+\s{1}\S+@\S+|\<(?:[trf2-8]|tell|reply|focus|tt|t2t)\>)\s?(?=\S|\s|$)";

                    prefix = Regex.Match(messageDecoded, tellRegex).Value;
                }
                Logger.LogTrace($"Matched Command [{prefix}] for [{channel}]", LoggerType.ChatDetours);

                // Finally if we reached this point, update `muffleAllowedForChannel` to reflect the intended channel.
                muffleMessage = g.AllowedGarblerChannels.IsActiveChannel((int)channel);
            }

            // If it's not allowed, do not garble.
            if (muffleMessage)
            {
                Logger.LogTrace($"Detouring Message: {messageDecoded}", LoggerType.ChatDetours);

                // only obtain the text payloads from this message, as nothing else should madder.
                var textPayloads = originalSeString.Payloads.OfType<TextPayload>().ToList();
                // merge together the text of all the split text payloads.
                var originalText = string.Join("", textPayloads.Select(tp => tp.Text));
                // Get the string to garble starting after the prefix text.
                var stringToProcess = originalText.Substring(prefix.Length);
                // set the output to the prefix + the garbled message.
                var output = string.IsNullOrEmpty(prefix)
                    ? _muffler.GarbleMessage(stringToProcess)
                    : prefix + " " + _muffler.GarbleMessage(stringToProcess);

                if (string.IsNullOrWhiteSpace(output))
                    return 0; // Do not sent message.

                Logger.LogTrace("Output: " + output, LoggerType.ChatDetours);
                var newSeString = new SeStringBuilder().Add(new TextPayload(output)).Build();

                // Verify its a legal width
                if (newSeString.TextValue.Length <= 500)
                {
                    var utf8String = Utf8String.FromString(".");
                    utf8String->SetString(newSeString.Encode());
                    return ProcessChatInputHook.Original(uiModule, (byte**)((nint)utf8String).ToPointer(), a3);
                }
                else
                {
                    Logger.LogError("Chat Garbler Variant of Message was longer than max message length!");
                    return ProcessChatInputHook.Original(uiModule, message, a3);
                }
            }
        }
        catch (Bagagwa e)
        {
            Logger.LogError($"Error sending message to chat box (secondary): {e}");
        }

        // return the original message untranslated
        return ProcessChatInputHook.Original(uiModule, message, a3);
    }
}

