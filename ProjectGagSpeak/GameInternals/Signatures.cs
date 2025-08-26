namespace GagSpeak.GameInternals;

// References for Knowledge
// ========================
// - ReceiveActionEffect
// https://github.com/NightmareXIV/ECommons/blob/master/ECommons/Hooks/ActionEffect.cs
// - OnEmote
// https://github.com/MgAl2O4/PatMeDalamud/blob/main/plugin/EmoteReaderHooks.cs
// - Callback Sig
// https://github.com/NightmareXIV/ECommons/blob/master/ECommons/Automation/Callback.cs
// - FireCallback Sig (no longer exists?)
// https://github.com/Caraxi/SimpleTweaksPlugin/blob/02abb1c3e4a140cbccded03af1e0637c3c5665ff/Debugging/AddonDebug.cs#L127
// - ForceDisableMovementPtr (THIS VALUE CAN BE SHARED BY OTHER PLUGINS (LIKE CAMMY) BE SURE IT IS HANDLED ACCORDINGLY)
// https://github.com/PunishXIV/Orbwalker/blob/4d171bc7c79a492bf9159d705dafa7bc97f0c174/Orbwalker/Memory.cs#L74
// - LMB+RMB MousePreventor2
// https://github.com/Drahsid/HybridCamera/blob/2e18760d64be14d2dc16405168d5a7a8f236ff3c/HybridCamera/MovementHook.cs#L216
// - UnfollowTarget
// Pray to whatever gods exist someone can still help with this.
// - Movement & Camera Control (Imprisonment)
// https://github.com/NightmareXIV/Lifestream/blob/main/Lifestream/Movement/OverrideMovement.cs
// https://github.com/NightmareXIV/Lifestream/blob/main/Lifestream/Movement/OverrideCamera.cs
// - ApplyGlamourPlate
// Given graciously by Hassel.
// - ProcessChatInput
// 

// ================================================
// - VFXEDITOR Signatures:
// 
// FFXIVClientStruct Sound Manager for handling sound effects and music
// https://github.com/aers/FFXIVClientStructs/blob/f42f0b960f0c956e62344daf161a2196123f0426/FFXIVClientStructs/FFXIV/Client/Sound/SoundManager.cs
//
// Penumbra's approach to intercepting and modifying incoming loaded sounds (requires replacement)
// https://github.com/xivdev/Penumbra/blob/0d1ed6a926ccb593bffa95d78a96b48bd222ecf7/Penumbra/Interop/Hooks/Animation/LoadCharacterSound.cs#L11
//
// Ocalot's Way to play sounds stored within VFX containers when applied on targets: (one we will use)
// https://github.com/0ceal0t/Dalamud-VFXEditor/blob/10c8420d064343f5f6bd902485cbaf28f7524e0d/VFXEditor/Interop

// For further control, look more indepth to Lifestream:
// https://github.com/NightmareXIV/Lifestream/blob/main/Lifestream/Tasks/CrossDC/TaskTpAndGoToWard.cs#L132

public static class Signatures
{
    // gip.HookFromAddress<ProcessActionEffect>(ss.ScanText(this)
    public const string ReceiveActionEffect = "40 55 56 57 41 54 41 55 41 56 41 57 48 8D AC 24";
    
    // ScanType: Signature
    public const string OnEmote = "E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24";

    // DetourName = nameof(FireCallbackDetour), Fallibility = Fallibility.Auto), Define via SignatureAttribute.
    public const string FireCallback = "E8 ?? ?? ?? ?? 0F B6 E8 8B 44 24 20";

    // Marshal.GetDelegateForFunctionPointer<ForcedStayCallbackDelegate>(ss.ScanText(this))
    public const string Callback = "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? BF";

    // ScanType.StaticAddress, Fallibility = Fallibility.Infallible. Define via SignatureAttribute.
    public const string ForceDisableMovement = "F3 0F 10 05 ?? ?? ?? ?? 0F 2E C7";

    // DetourName = nameof(MovementUpdate), Fallibility = Fallibility.Auto, Define via SignatureAttribute.
    public const string MouseAutoMove2 = "48 8b c4 4c 89 48 ?? 53 55 57 41 54 48 81 ec ?? 00 00 00";

    // DetourName = nameof(TestUpdate), Fallibility = Fallibility.Auto, Define via SignatureAttribute.
    public const string UnfollowTarget = "48 89 5c 24 ?? 48 89 74 24 ?? 57 48 83 ec ?? 48 8b d9 48 8b fa 0f b6 89 ?? ?? 00 00 be 00 00 00 e0";

    // Signatures for Imprisonment
    public const string RMICamera = "48 8B C4 53 48 81 EC ?? ?? ?? ?? 44 0F 29 50 ??";

    public const string RMIWalk = "E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D";

    public const string RMIWalkIsInputEnabled1 = "E8 ?? ?? ?? ?? 84 C0 75 10 38 43 3C";

    public const string RMIWalkIsInputEnabled2 = "E8 ?? ?? ?? ?? 84 C0 75 03 88 47 3F";

    // DetourName = nameof(ApplyGlamourPlateDetour), Fallibility = Fallibility.Auto, Define via SignatureAttribute.
    public const string ApplyGlamourPlate = "E8 ?? ?? ?? ?? 41 C6 44 24 ?? ?? E9 ?? ?? ?? ?? 0F B6 83";


    // Sends a constructed chat message to the server. (No longer nessisary)
    // public const string SendChat = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 45 84 C9";

    // DetourName = nameof(ProcessChatInputDetour), Fallibility = Fallibility.Auto, Define via SignatureAttribute.
    public const string ProcessChatInput = "E8 ?? ?? ?? ?? FE 87 ?? ?? ?? ?? C7 87";

    // Spatial Audio Sigs from VFXEDITOR
    internal const string CreateStaticVfx = "E8 ?? ?? ?? ?? F3 0F 10 35 ?? ?? ?? ?? 48 89 43 08";
    internal const string RunStaticVfx = "E8 ?? ?? ?? ?? ?? ?? ?? 8B 4A ?? 85 C9";
    internal const string RemoveStaticVfx = "40 53 48 83 EC 20 48 8B D9 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 28 33 D2 E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9";

    internal const string CreateActorVfx = "40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8";
    internal const string RemoveActorVfx = "0F 11 48 10 48 8D 05"; // the weird one
}
