using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace OtterGui.Widgets;

public static partial class Widget
{
    private static ReadOnlySpan<byte> HexBytes
        => "0123456789ABCDEF"u8;

    public static unsafe void DrawHexViewer(ReadOnlySpan<byte> data, uint offsetColor = 0, uint color1 = 0, uint color2 = 0)
    {
        if (data.Length == 0)
            return;

        var       font    = UiBuilder.MonoFont;
        using var _       = ImRaii.PushFont(font);
        var       emWidth = font.GetCharAdvance('m');
        var       spacing = ImGui.GetStyle().ItemInnerSpacing.X;

        // Get the required number of digits for the byte offset.
        var addressDigitCount = 8 - (BitOperations.LeadingZeroCount((uint)data.Length - 1) >> 2);
        // Spacing is correct for 32 bytes shown per line, too much for 16 and not enough for more, but should not generally matter much.
        var charsPerRow = (int)MathF.Floor((ImGui.GetContentRegionAvail().X - 9 * spacing) / emWidth);
        var bytesPerRow = (charsPerRow - addressDigitCount - 2) / 4;

        // Check that we actually need multiple lines and lock to power of 2.
        bytesPerRow = Math.Max(8, Math.Min(1 << BitOperations.Log2((uint)bytesPerRow), data.Length));
        if (bytesPerRow == data.Length)
            addressDigitCount = 0;

        // Set default colors if necessary.
        if (color1 == 0)
            color1 = ImGui.GetColorU32(ImGuiCol.Text);
        if (color2 == 0)
            color2 = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        if (offsetColor == 0)
            offsetColor = ImGui.GetColorU32(ImGuiCol.Text);

        // Prepare the full buffer. by setting up constant values.
        var capacity = addressDigitCount + 2 + 4 * bytesPerRow; // address ':' {' ' hex hex} ' ' {printable}
        var buffer   = stackalloc byte[capacity + 1];
        buffer[capacity]          = 0;
        buffer[addressDigitCount] = (byte)':';
        var offset = addressDigitCount + 1;
        var end    = offset + 3 * bytesPerRow;
        for (var i = offset; i <= end; i += 3)
            buffer[i] = (byte)' ';

        var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        var numRows = (data.Length + bytesPerRow - 1) / bytesPerRow;
        clipper.Begin(numRows, ImGui.GetTextLineHeightWithSpacing());
        while (clipper.Step())
        {
            for (var actualRow = clipper.DisplayStart; actualRow < clipper.DisplayEnd; actualRow++)
            {
                if (actualRow >= numRows)
                    return;

                if (actualRow < 0)
                    continue;

                // Prepare buffer data. Set the address first.
                var rowStart = actualRow * bytesPerRow;
                var bufferI  = 0;
                for (var i = addressDigitCount; i-- > 0;)
                    buffer[bufferI++] = HexBytes[(rowStart >> (i << 2)) & 0xF];

                // Set the actual byte values for hex and printable.
                var numBytes        = Math.Min(data.Length - rowStart, bytesPerRow);
                var printableOffset = 3 * bytesPerRow + addressDigitCount + 2;
                for (var i = 0; i < numBytes; ++i)
                {
                    var @byte = data[rowStart + i];
                    buffer[bufferI += 2]        = HexBytes[@byte >> 4];
                    buffer[++bufferI]           = HexBytes[@byte & 0xF];
                    buffer[printableOffset + i] = @byte is >= 32 and < 127 ? @byte : (byte)'.';
                }

                // Clear lacking byte values for the last line.
                for (var i = numBytes; i < bytesPerRow; ++i)
                {
                    buffer[bufferI += 2]        = (byte)' ';
                    buffer[++bufferI]           = (byte)' ';
                    buffer[printableOffset + i] = (byte)' ';
                }

                // Start drawing the text. First the address, if using more than a single row.
                var packStart = buffer + addressDigitCount + 2;
                if (bytesPerRow < data.Length)
                {
                    using var color = ImRaii.PushColor(ImGuiCol.Text, offsetColor);
                    ImGuiNative.igTextUnformatted(buffer, packStart);
                    ImGui.SameLine(0, spacing);
                }

                // Then the 8 byte blocks in alternating colors with slight spacing in between.
                for (var i = 0; i < bytesPerRow; i += 8)
                {
                    var       packEnd = packStart + 24;
                    using var color   = ImRaii.PushColor(ImGuiCol.Text, (i & 8) == 0 ? color1 : color2);
                    ImGuiNative.igTextUnformatted(packStart, packEnd);
                    ImGui.SameLine(0, spacing);
                    packStart = packEnd;
                }

                // Finally the printable block.
                ImGuiNative.igTextUnformatted(packStart, buffer + capacity);
            }
        }

        clipper.End();
        clipper.Destroy();
    }
}
