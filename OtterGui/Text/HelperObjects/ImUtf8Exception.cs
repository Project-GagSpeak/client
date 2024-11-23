namespace OtterGui.Text.HelperObjects;

/// <summary>
/// Exception thrown when the formatter encounters an error while trying to format,
/// or if the supplied format results in text longer than the static buffers allow.
/// </summary>
public class ImUtf8FormatException() : Exception("Could not format UTF8 String.");

/// <summary>
/// Exception thrown when the internal buffers do not suffice to store data.
/// </summary>
public class ImUtf8SizeException() : Exception("Input data is longer than buffer size.");
