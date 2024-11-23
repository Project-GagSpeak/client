using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Glamourer.Api.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Glamourer.Api.IpcSubscribers.Legacy;

public sealed class Revert(IDalamudPluginInterface pi)
    : ActionSubscriber<string>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(Revert)}";

    public new void Invoke(string characterName)
        => base.Invoke(characterName);
}

public sealed class RevertCharacter(IDalamudPluginInterface pi)
    : ActionSubscriber<ICharacter?>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(RevertCharacter)}";

    public new void Invoke(ICharacter? character)
        => base.Invoke(character);
}

public sealed class RevertLock(IDalamudPluginInterface pi)
    : ActionSubscriber<string, uint>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(RevertLock)}";

    public new void Invoke(string characterName, uint key)
        => base.Invoke(characterName, key);
}

public sealed class RevertCharacterLock(IDalamudPluginInterface pi)
    : ActionSubscriber<ICharacter?, uint>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(RevertCharacterLock)}";

    public new void Invoke(ICharacter? character, uint key)
        => base.Invoke(character, key);
}

public sealed class RevertToAutomation(IDalamudPluginInterface pi)
    : FuncSubscriber<string, uint, bool>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(RevertToAutomation)}";

    public new bool Invoke(string characterName, uint key)
        => base.Invoke(characterName, key);
}

public sealed class RevertToAutomationCharacter(IDalamudPluginInterface pi)
    : FuncSubscriber<ICharacter?, uint, bool>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(RevertToAutomationCharacter)}";

    public new bool Invoke(ICharacter? character, uint key)
        => base.Invoke(character, key);
}

public sealed class Unlock(IDalamudPluginInterface pi)
    : FuncSubscriber<ICharacter?, uint, bool>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(Unlock)}";

    public new bool Invoke(ICharacter? character, uint key)
        => base.Invoke(character, key);
}

public sealed class UnlockName(IDalamudPluginInterface pi)
    : FuncSubscriber<string, uint, bool>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(UnlockName)}";

    public new bool Invoke(string characterName, uint key)
        => base.Invoke(characterName, key);
}

public static class StateChanged
{
    public const string Label = $"Penumbra.{nameof(StateChanged)}";

    public static EventSubscriber<int, nint, Lazy<string>> Subscriber(IDalamudPluginInterface pi,
        params Action<int, nint, Lazy<string>>[] actions)
        => new(pi, Label, actions);
}

public sealed class GetAllCustomization(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string?>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(GetAllCustomization)}";

    public new string? Invoke(string characterName)
        => base.Invoke(characterName);
}

public sealed class GetAllCustomizationFromCharacter(IDalamudPluginInterface pi)
    : FuncSubscriber<ICharacter?, string?>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(GetAllCustomizationFromCharacter)}";

    public new string? Invoke(ICharacter? character)
        => base.Invoke(character);
}

public sealed class GetAllCustomizationLocked(IDalamudPluginInterface pi)
    : FuncSubscriber<string, uint, string?>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(GetAllCustomizationLocked)}";

    public new string? Invoke(string characterName, uint key)
        => base.Invoke(characterName, key);
}

public sealed class GetAllCustomizationFromLockedCharacter(IDalamudPluginInterface pi)
    : FuncSubscriber<ICharacter?, uint, string?>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(GetAllCustomizationFromLockedCharacter)}";

    public new string? Invoke(ICharacter? character, uint key)
        => base.Invoke(character, key);
}

public sealed class ApplyAll(IDalamudPluginInterface pi)
    : ActionSubscriber<string, string>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyAll)}";

    public new void Invoke(string characterName, string stateBase64)
        => base.Invoke(characterName, stateBase64);
}

public sealed class ApplyAllOnce(IDalamudPluginInterface pi)
    : ActionSubscriber<string, string>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyAllOnce)}";

    public new void Invoke(string characterName, string stateBase64)
        => base.Invoke(characterName, stateBase64);
}

public sealed class ApplyAllToCharacter(IDalamudPluginInterface pi)
    : ActionSubscriber<ICharacter?, string>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyAllToCharacter)}";

    public new void Invoke(ICharacter? character, string stateBase64)
        => base.Invoke(character, stateBase64);
}

public sealed class ApplyAllOnceToCharacter(IDalamudPluginInterface pi)
    : ActionSubscriber<ICharacter?, string>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyAllOnceToCharacter)}";

    public new void Invoke(ICharacter? character, string stateBase64)
        => base.Invoke(character, stateBase64);
}

public sealed class ApplyOnlyEquipment(IDalamudPluginInterface pi)
    : ActionSubscriber<string, string>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyOnlyEquipment)}";

    public new void Invoke(string characterName, string stateBase64)
        => base.Invoke(characterName, stateBase64);
}

public sealed class ApplyOnlyEquipmentToCharacter(IDalamudPluginInterface pi)
    : ActionSubscriber<ICharacter?, string>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyOnlyEquipmentToCharacter)}";

    public new void Invoke(ICharacter? character, string stateBase64)
        => base.Invoke(character, stateBase64);
}

public sealed class ApplyOnlyCustomization(IDalamudPluginInterface pi)
    : ActionSubscriber<string, string>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyOnlyCustomization)}";

    public new void Invoke(string characterName, string stateBase64)
        => base.Invoke(characterName, stateBase64);
}

public sealed class ApplyOnlyCustomizationToCharacter(IDalamudPluginInterface pi)
    : ActionSubscriber<ICharacter?, string>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyOnlyCustomizationToCharacter)}";

    public new void Invoke(ICharacter? character, string stateBase64)
        => base.Invoke(character, stateBase64);
}

public sealed class ApplyAllLock(IDalamudPluginInterface pi)
    : ActionSubscriber<string, string, uint>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyAllLock)}";

    public new void Invoke(string characterName, string stateBase64, uint key)
        => base.Invoke(characterName, stateBase64, key);
}

public sealed class ApplyAllToCharacterLock(IDalamudPluginInterface pi)
    : ActionSubscriber<ICharacter?, string, uint>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyAllToCharacterLock)}";

    public new void Invoke(ICharacter? character, string stateBase64, uint key)
        => base.Invoke(character, stateBase64, key);
}

public sealed class ApplyOnlyEquipmentLock(IDalamudPluginInterface pi)
    : ActionSubscriber<string, string, uint>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyOnlyEquipmentLock)}";

    public new void Invoke(string characterName, string stateBase64, uint key)
        => base.Invoke(characterName, stateBase64, key);
}

public sealed class ApplyOnlyEquipmentToCharacterLock(IDalamudPluginInterface pi)
    : ActionSubscriber<ICharacter?, string, uint>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyOnlyEquipmentToCharacterLock)}";

    public new void Invoke(ICharacter? character, string stateBase64, uint key)
        => base.Invoke(character, stateBase64, key);
}

public sealed class ApplyOnlyCustomizationLock(IDalamudPluginInterface pi)
    : ActionSubscriber<string, string, uint>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyOnlyCustomizationLock)}";

    public new void Invoke(string characterName, string stateBase64, uint key)
        => base.Invoke(characterName, stateBase64, key);
}

public sealed class ApplyOnlyCustomizationToCharacterLock(IDalamudPluginInterface pi)
    : ActionSubscriber<ICharacter?, string, uint>(pi, Label)
{
    public const string Label = $"Glamourer.{nameof(ApplyOnlyCustomizationToCharacterLock)}";

    public new void Invoke(ICharacter? character, string stateBase64, uint key)
        => base.Invoke(character, stateBase64, key);
}
