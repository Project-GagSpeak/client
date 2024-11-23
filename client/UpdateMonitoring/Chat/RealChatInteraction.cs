using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;


namespace GagSpeak.UpdateMonitoring.Chat;

/// <summary>
/// Danger Class that uses signature hooks to construct, intercept from, and send off messages to the ChatBox.
/// 
/// Hooks and signatures pulled from 
/// https://github.com/NightmareXIV/ECommons/blob/9e90d0032f0efd4c9e65d9c5a8e8bd0e99557d68/ECommons/Automation/Chat.cs#L83
/// Which was also pulled from ChatTwo, all rights reserved.
/// 
/// Slightly modified to adapt to our dependency injection system.
/// </summary>
public class ChatSender
{

    private static class Signatures
    {
        internal const string SendChat = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 45 84 C9"; // Sends a constructed chat message to the server.
        internal const string SanitizeString = "E8 ?? ?? ?? ?? EB 0A 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 48 8D AE"; // Sanitizes string for chat.
    }

    // define our delegates.
    private delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);
    private ProcessChatBoxDelegate ProcessChatBox { get; }
    private readonly unsafe delegate* unmanaged<Utf8String*, int, IntPtr, void> _sanitizeString = null!;

    internal ChatSender(ISigScanner scanner)
    {
        // Attempt to get the ProcessChatBox delegate from the signature
        if (scanner.TryScanText(Signatures.SendChat, out var processChatBoxPtr))
        {
            ProcessChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(processChatBoxPtr);
        }

        // attempt to get the string sanitizer delegate from the signature
        unsafe
        {
            if (scanner.TryScanText(Signatures.SanitizeString, out var sanitizeStringPtr))
            {
                _sanitizeString = (delegate* unmanaged<Utf8String*, int, IntPtr, void>)sanitizeStringPtr;
            }
        }
    }

    /// <b> This method does not throw any exceptions, and should be handled with fucking caution </b>
    /// <para>
    /// it is primarily used to initialize the actual sending of chat to the server, hence the
    /// unsafe method, and can not be merged with the sendMessage function.
    /// </para>
    /// </summary>
    [Obsolete("Use safe message sending")]
    public unsafe void SendMessageUnsafe(byte[] message)
    {
        // Ensure our Signature is Valid.
        if (ProcessChatBox == null)
        {
            throw new InvalidOperationException("Could not find signature for chat sending");
        }

        // Fetch the UIModule to interact with the ChatBox.
        var uiModule = (IntPtr)Framework.Instance()->GetUIModule();

        // Create a new ChatPayload Message to construct our string.
        using var payload = new ChatPayload(message);

        // Marshal the payload to a pointer in memory
        var mem1 = Marshal.AllocHGlobal(400);

        // StructureToPtr - Marshals data from a managed object to an unmanaged block of memory.
        Marshal.StructureToPtr(payload, mem1, false);

        // Process the structured message to the UIModule ChatBox
        ProcessChatBox(uiModule, mem1, nint.Zero, 0);

        // Free back up our memory to avoid Memory leaks!
        Marshal.FreeHGlobal(mem1);
    }

    /// <summary>
    /// <para> Send a given message to the chat box. <b>USE THIS METHOD OVER THE UNSAFE ONE.</b></para>
    /// </summary>
    public void SendMessage(string message)
    {
        // Get the number of bytes our message contains.
        var bytes = Encoding.UTF8.GetBytes(message);

        // Throw Exception if Message is empty
        if (bytes.Length == 0)
        {
            throw new ArgumentException("message is empty", nameof(message));
        }

        // Throw exception if message is too long
        if (bytes.Length > 500)
        {
            throw new ArgumentException($"message is longer than 500 bytes, and is {bytes.Length}", nameof(message));
        }

        // if the message length is not equal to the sanitised message length, throw an exception.
        if (message.Length != SanitizeText(message).Length)
        {
            throw new ArgumentException("message contained invalid characters", nameof(message));
        }

#pragma warning disable CS0618 // Type or member is obsolete
        SendMessageUnsafe(bytes);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Sanitize the text into a format SeStrings will accept for sending.
    /// </summary>
    public unsafe string SanitizeText(string text)
    {
        if (_sanitizeString == null)
        {
            throw new InvalidOperationException("Could not find signature for chat sanitization");
        }

        var uText = Utf8String.FromString(text);
        _sanitizeString(uText, 0x27F, IntPtr.Zero);
        var sanitized = uText->ToString();
        uText->Dtor();
        IMemorySpace.Free(uText);

        return sanitized;
    }


    [StructLayout(LayoutKind.Explicit)] // Lets us control the physical layout of the data fields of a class or structure in memory.
    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")] // We need to keep the pointer alive
    // Struct format for the ChatMessagePayload.
    private readonly struct ChatPayload : IDisposable
    {
        [FieldOffset(0)]
        private readonly nint textPtr;

        [FieldOffset(16)]
        private readonly ulong textLen;

        [FieldOffset(8)]
        private readonly ulong unk1;

        [FieldOffset(24)]
        private readonly ulong unk2;

        // The constructor for the chatpayload struct, we need to allocate memory for the string bytes, and then copy the string bytes to the text pointer
        internal ChatPayload(byte[] stringBytes)
        {
            // AllocHGlobal - Allocates memory from the unmanaged memory of the process by using the specified number of bytes.
            textPtr = Marshal.AllocHGlobal(stringBytes.Length + 30);
            // Copy - Copies data from a managed array to an unmanaged memory pointer, or from an unmanaged memory pointer to a managed array.
            Marshal.Copy(stringBytes, 0, textPtr, stringBytes.Length);
            // WriteByte - Writes a single byte value to unmanaged memory.
            Marshal.WriteByte(textPtr + stringBytes.Length, 0);

            // Set the text length to the length of the string bytes + 1
            textLen = (ulong)(stringBytes.Length + 1);
            // Set the unknowns to 64 and 0, as they should be for chat message sending.
            unk1 = 64;
            unk2 = 0;
        }

        // when we dispose of our chat payload, we must be sure to free the memory we allowed in this.textPtr
        public void Dispose()
        {
            Marshal.FreeHGlobal(textPtr);
        }
    }
}
