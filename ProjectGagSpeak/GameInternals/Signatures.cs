namespace GagSpeak.GameInternals;

// References for Knowledge
// ========================
// - ReceiveActionEffect
// https://github.com/NightmareXIV/ECommons/blob/master/ECommons/Hooks/ActionEffect.cs
// - OnEmote
// https://github.com/MgAl2O4/PatMeDalamud/blob/42385a92d1a9c3f043f35128ee68dc623cfc6a20/plugin/EmoteReaderHooks.cs#L21C87-L21C130
// - Callback Sig
// https://github.com/NightmareXIV/ECommons/blob/6ea40a9eea2e805f2f566fe0493749c7c0639ea3/ECommons/Automation/Callback.cs#L64
// - FireCallback Sig
// https://github.com/Caraxi/SimpleTweaksPlugin/blob/02abb1c3e4a140cbccded03af1e0637c3c5665ff/Debugging/AddonDebug.cs#L127
// - ForceDisableMovementPtr (THIS VALUE CAN BE SHARED BY OTHER PLUGINS (LIKE CAMMY) BE SURE IT IS HANDLED ACCORDINGLY)
// https://github.com/PunishXIV/Orbwalker/blob/4d171bc7c79a492bf9159d705dafa7bc97f0c174/Orbwalker/Memory.cs#L74
// - LMB+RMB MousePreventor2
// https://github.com/Drahsid/HybridCamera/blob/2e18760d64be14d2dc16405168d5a7a8f236ff3c/HybridCamera/MovementHook.cs#L216
// - UnfollowTarget
// Pray to whatever gods exist someone can still help with this.


// For further control, look more indepth to Lifestream:
// https://github.com/NightmareXIV/Lifestream/blob/main/Lifestream/Tasks/CrossDC/TaskTpAndGoToWard.cs#L132

public static class Signatures
{
    // gip.HookFromAddress<ProcessActionEffect>(ss.ScanText(this)
    public const string ReceiveActionEffect = "40 ?? 56 57 41 ?? 41 ?? 41 ?? 48 ?? ?? ?? ?? ?? ?? ?? 48";
    
    // ScanType: Signature
    public const string OnEmote = "E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24";

    // DetourName = nameof(FireCallbackDetour), Fallibility = Fallibility.Auto), Define via SignatureAttribute.
    public const string FireCallback = "E8 ?? ?? ?? ?? 0F B6 E8 8B 44 24 20";

    // Marshal.GetDelegateForFunctionPointer<ForcedStayCallbackDelegate>(ss.ScanText(this))
    public const string Callback = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 0F B7 81";

    // ScanType.StaticAddress, Fallibility = Fallibility.Infallible. Define via SignatureAttribute.
    public const string ForceDisableMovement = "F3 0F 10 05 ?? ?? ?? ?? 0F 2E C7";

    // DetourName = nameof(MovementUpdate), Fallibility = Fallibility.Auto, Define via SignatureAttribute.
    public const string MouseAutoMove2 = "48 8b c4 48 89 70 ?? 48 89 78 ?? 55 41 56 41 57";

    // DetourName = nameof(TestUpdate), Fallibility = Fallibility.Auto, Define via SignatureAttribute.
    public const string UnfollowTarget = "48 89 5c 24 ?? 48 89 74 24 ?? 57 48 83 ec ?? 48 8b d9 48 8b fa 0f b6 89 ?? ?? 00 00 be 00 00 00 e0";

    // Sends a constructed chat message to the server.
    internal const string SendChat = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 45 84 C9";

    // Sanitizes string for chat.
    internal const string SanitizeString = "E8 ?? ?? ?? ?? EB 0A 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 48 8D AE";
}
