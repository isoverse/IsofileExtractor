using System.Text.Json.Nodes;

namespace IsodatReader;

/// <summary>
/// Thrown when binary parsing fails. Carries the full object-tree path
/// (class name + stream position of each object header) from the outermost
/// dispatched class down to where the error occurred, plus the exact stream
/// position at the point of failure.
/// </summary>
public sealed class IsodatParseException : Exception
{
    private readonly List<(string ClassName, long HeaderPos)> _path;

    // Innermost: raw exception from a primitive read
    internal IsodatParseException(string className, long headerPos, long errorPos, Exception cause)
        : base(cause.Message, cause)
    {
        _path    = [(className, headerPos)];
        ErrorPos = errorPos;
    }

    // Copy constructor used by PrependPath
    private IsodatParseException(IsodatParseException source)
        : base(source.InnerException!.Message, source.InnerException)
    {
        _path         = new List<(string, long)>(source._path);
        ErrorPos      = source.ErrorPos;
        PartialResult = source.PartialResult;
    }

    /// <summary>Stream position (bytes) at the point the error was thrown.</summary>
    public long ErrorPos { get; }

    /// <summary>Partially-built JSON result from the reader that failed, if available.</summary>
    public JsonNode? PartialResult { get; internal set; }

    /// <summary>
    /// Returns a new exception with <paramref name="className"/> prepended to
    /// the path, used by each outer Dispatch call as the exception propagates up.
    /// </summary>
    internal IsodatParseException PrependPath(string className, long headerPos)
    {
        var copy = new IsodatParseException(this);
        copy._path.Insert(0, (className, headerPos));
        return copy;
    }

    public override string Message
    {
        get
        {
            string path = string.Join(" -> ",
                _path.Select(p => $"{p.ClassName} (@0x{p.HeaderPos:x})"));
            return $"{path}: {InnerException!.Message} (@0x{ErrorPos:x})";
        }
    }
}
