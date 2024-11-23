using Dalamud.Plugin;
using Penumbra.Api.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.IpcSubscribers;

/// <inheritdoc cref="IPenumbraApiEditing.ConvertTextureFile"/>
public sealed class ConvertTextureFile(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, int, bool, Task>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ConvertTextureFile)}";

    /// <inheritdoc cref="IPenumbraApiEditing.ConvertTextureFile"/>
    public Task Invoke(string inputFile, string outputFile, TextureType textureType, bool mipMaps = true)
        => Invoke(inputFile, outputFile, (int)textureType, mipMaps);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, string, int, bool, Task> Provider(IDalamudPluginInterface pi, IPenumbraApiEditing api)
        => new(pi, Label, (a, b, c, d) => api.ConvertTextureFile(a, b, (TextureType)c, d));
}

/// <inheritdoc cref="IPenumbraApiEditing.ConvertTextureData"/>
public sealed class ConvertTextureData(IDalamudPluginInterface pi)
    : FuncSubscriber<byte[], int, string, int, bool, Task>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ConvertTextureData)}";

    /// <inheritdoc cref="IPenumbraApiEditing.ConvertTextureData"/>
    public Task Invoke(byte[] rgbaData, int width, string outputFile, TextureType textureType, bool mipMaps = true)
        => Invoke(rgbaData, width, outputFile, (int)textureType, mipMaps);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<byte[], int, string, int, bool, Task> Provider(IDalamudPluginInterface pi, IPenumbraApiEditing api)
        => new(pi, Label, (a, b, c, d, e) => api.ConvertTextureData(a, b, c, (TextureType)d, e));
}
