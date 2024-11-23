using Penumbra.Api.Enums;

namespace Penumbra.Api.Api;

/// <summary> API methods pertaining to the editing of mods or game files. </summary>
public interface IPenumbraApiEditing
{
    /// <summary>
    /// Convert the given texture file into a different type or format and potentially add mip maps.
    /// </summary>
    /// <param name="inputFile"> The path to the input file, which may be of .dds, .tex or .png format. </param>
    /// <param name="outputFile"> The desired output path. Can be the same as input. </param>
    /// <param name="textureType"> The file type and format to convert the data to. </param>
    /// <param name="mipMaps"> Whether to add mip maps or not. Ignored for .png. </param>
    /// <returns> A task for when the conversion is finished or has failed. </returns>
    public Task ConvertTextureFile(string inputFile, string outputFile, TextureType textureType, bool mipMaps);

    /// <summary>
    /// Convert the given RGBA32 texture data into a different type or format and potentially add mip maps.
    /// </summary>
    /// <param name="rgbaData"> The input byte data for a picture given in RGBA32 format. </param>
    /// <param name="width"> The width of the input picture. The height is computed from the size of <paramref name="rgbaData"/> and this. </param>
    /// <param name="outputFile"> The desired output path. Can be the same as input. </param>
    /// <param name="textureType"> The file type and format to convert the data to. </param>
    /// <param name="mipMaps"> Whether to add mip maps or not. Ignored for .png. </param>
    /// <returns> A task for when the conversion is finished or has failed. </returns>
    public Task ConvertTextureData(byte[] rgbaData, int width, string outputFile, TextureType textureType, bool mipMaps);
}
