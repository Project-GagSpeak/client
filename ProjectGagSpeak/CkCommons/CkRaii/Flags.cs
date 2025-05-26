namespace GagSpeak.CkCommons.Raii;

public enum HeaderFlags : byte
{
    /// <summary> Aligns the header text to the center. </summary>
    /// <remarks> Only one AlignFlag will be accepted in any operation. </remarks>
    AlignCenter = 0x00,

    /// <summary> Aligns the header text to the left. </summary>
    /// <remarks> Only one AlignFlag will be accepted in any operation. </remarks>
    AlignLeft = 0x01,

    /// <summary> Aligns the header text to the right. </summary>
    /// <remarks> Only one AlignFlag will be accepted in any operation. </remarks>
    AlignRight = 0x02,

    /// <summary> The passed in size includes the header height, and should have it subtracted before making the body. </summary>
    /// <remarks> useful for cases where your height is ImGui.GetContentRegionAvail().Y </remarks>
    SizeIncludesHeader = 0x04,

    /// <summary> Means any container should append WindowPadding.Y * 2 to the size parameter. </summary>
    /// <remarks> Useful for when you want to pass in an internal height you know of. </remarks>
    AddPaddingToHeight = 0x08,


    CR_HeaderLeft = SizeIncludesHeader | AlignLeft,
    CR_HeaderCentered = SizeIncludesHeader | AlignCenter,
    CR_HeaderRight = SizeIncludesHeader | AlignRight,
}
