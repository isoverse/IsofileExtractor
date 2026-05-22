using System.Text;

namespace IsodatReader;

/// <summary>
/// Sequential binary reader for the MFC CArchive serialization protocol used
/// by Thermo isodat .dxf / .scn files.
///
/// CRuntimeClass header formats:
///   New class:        ff ff | uint16 archiveVersion | uint16 nameLen | ASCII name
///   Short back-ref:   uint16 where bit-15=1, bits 14-0 = MFC map counter index
///   Long back-ref:    7f ff | int32 where bit-31=flag, bits 30-0 = MFC map counter index
///
/// MFC CString format:
///   ff fe ff | length (1/2/4 bytes) | UTF-16LE payload
///
/// MFC map counter (_mapCount):
///   MFC CArchive uses ONE shared counter for both class slots and object slots.
///   Each WriteObject(newObj) with a new class consumes two slots: one for the
///   class and one for the object. A WriteObject for an already-seen class
///   consumes only one slot (the object). Back-reference tags carry the counter
///   value at which the class (or object) was originally registered, so we must
///   maintain the same counter to resolve them correctly.
/// </summary>
public sealed class IsodatFile : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly Dictionary<int, (string Name, int ArchiveVersion)> _classRegistry = new();
    private readonly List<string> _warnings = new();
    private readonly List<ObjectLogEntry> _objectLog = new();
    private readonly Stack<int> _containerStack = new();

    private readonly Dictionary<string, (int Version, long Pos)> _schemaVersions = new();  // className → first-seen schema version + position
    private readonly Dictionary<string, int> _archiveVersions = new();    // className → CRuntimeClass archive version
    private int _mapCount = 1;  // MFC m_nMapCount, starts at 1

    public IReadOnlyDictionary<int, (string Name, int ArchiveVersion)> ClassRegistry => _classRegistry;
    public IReadOnlyList<ObjectLogEntry> ObjectLog => _objectLog;

    internal int? CurrentContainerObjIdx => _containerStack.Count > 0 ? _containerStack.Peek() : null;
    internal void PushContainer(int objIdx) => _containerStack.Push(objIdx);
    internal void PopContainer() { if (_containerStack.Count > 0) _containerStack.Pop(); }

    public IsodatFile(Stream stream)
    {
        _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
    }

    // -------------------------------------------------------------------------
    // CRuntimeClass protocol
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads the next CRuntimeClass header and advances the MFC map counter.
    /// New-class: consumes two counter slots (class + object pre-allocation).
    /// Back-ref:  consumes one counter slot (object only; class already stored).
    /// Returns the class name.
    /// </summary>
    /// <returns>
    /// The class name, or <c>null</c> if the stream contained the MFC NULL
    /// WriteObject tag (<c>00 00</c>).  The map counter is NOT advanced for null.
    /// </returns>
    public string? ReadCRuntimeClass(string? expected = null)
    {
        long startPos = _reader.BaseStream.Position;
        byte b0 = _reader.ReadByte();
        byte b1 = _reader.ReadByte();

        // MFC NULL WriteObject: 00 00 — no counter slot consumed
        if (b0 == 0x00 && b1 == 0x00)
        {
            if (expected is not null)
                throw new InvalidDataException(
                    $"Expected CRuntimeClass '{expected}' but encountered NULL WriteObject");
            return null;
        }

        string className;
        int archiveVersion;

        if (b0 == 0xFF && b1 == 0xFF)
        {
            archiveVersion = _reader.ReadUInt16();
            ushort nameLen = _reader.ReadUInt16();
            className = Encoding.ASCII.GetString(_reader.ReadBytes(nameLen));
            _classRegistry[_mapCount] = (className, archiveVersion);
            _archiveVersions[className] = archiveVersion;
            int classIdx = _mapCount;
            int objIdx = _mapCount + 1;
            _mapCount += 2;  // class slot + object slot
            _objectLog.Add(new ObjectLogEntry(classIdx, objIdx, startPos, CurrentContainerObjIdx, className, archiveVersion));
        }
        else if (b0 == 0x7F && b1 == 0xFF)
        {
            // cached object that has a long from ID (>32,767), doubt this will ever happen
            int packed = _reader.ReadInt32();
            if ((packed & unchecked((int)0x8000_0000)) == 0)
                throw new InvalidDataException(
                    $"expected class reference with high-bit flag set, " +
                    $"but raw value 0x{packed:x8} has bit 31 clear — likely stream misalignment");
            int classIdx = packed & 0x7FFF_FFFF;
            if (!_classRegistry.TryGetValue(classIdx, out var entry))
                throw new InvalidDataException(
                    $"CRuntimeClass back-reference refers to map index {classIdx} " +
                    $"which does not exist (current map count: {_mapCount})");
            (className, archiveVersion) = entry;
            int objIdx = _mapCount;
            _mapCount++;     // object slot only
            _objectLog.Add(new ObjectLogEntry(classIdx, objIdx, startPos, CurrentContainerObjIdx, className, archiveVersion));
        }
        else
        {
            // cached object with short form ID <= 32767
            int packed = b0 | (b1 << 8);
            if ((packed & 0x8000) == 0)
                throw new InvalidDataException(
                    $"expected class reference with high-bit flag set, " +
                    $"but raw value 0x{packed:x4} has bit 15 clear — likely stream misalignment");
            int classIdx = packed & 0x7FFF;
            if (!_classRegistry.TryGetValue(classIdx, out var entry))
                throw new InvalidDataException(
                    $"CRuntimeClass back-reference refers to map index {classIdx} " +
                    $"which does not exist (current map count: {_mapCount})");
            (className, archiveVersion) = entry;
            int objIdx = _mapCount;
            _mapCount++;     // object slot only
            _objectLog.Add(new ObjectLogEntry(classIdx, objIdx, startPos, CurrentContainerObjIdx, className, archiveVersion));
        }

        if (expected is not null && className != expected)
            throw new InvalidDataException(
                $"Expected CRuntimeClass '{expected}' but encountered '{className}'");

        return className;
    }

    // -------------------------------------------------------------------------
    // Schema version
    // -------------------------------------------------------------------------

    public int ReadSchemaVersion(string className, int maxSupported)
    {
        long pos = _reader.BaseStream.Position;
        int v = _reader.ReadInt32();
        if (v <= 0)
            throw new InvalidDataException(
                $"{className} schema version {v} is invalid (stream misaligned?)");
        if (v > maxSupported + 5)
            throw new InvalidDataException(
                $"{className} schema version v{v} is implausibly higher than the supported v{maxSupported} — stream is likely misaligned");
        if (v > maxSupported)
            _warnings.Add(
                $"{className} schema version v{v} is newer than supported v{maxSupported}; " +
                "fields added after that version will not be read and may lead to misalignment");
        if (_schemaVersions.TryGetValue(className, out var prev))
        {
            if (v != prev.Version)
                throw new InvalidDataException(
                    $"{className} schema version mismatch: first instance had v{prev.Version} (0x{prev.Pos:x})," +
                    $"this instance has v{v} — stream is likely misaligned");
        }
        else
        {
            _schemaVersions[className] = (v, pos);
        }
        return v;
    }

    public long Position => _reader.BaseStream.Position;

    // -------------------------------------------------------------------------
    // Primitive reads
    // -------------------------------------------------------------------------

    public int ReadInt32() => _reader.ReadInt32();
    public int ReadUInt16() => _reader.ReadUInt16();
    public int ReadUInt8() => _reader.ReadByte();
    public long ReadUInt32() => _reader.ReadUInt32();
    public double ReadDouble() => _reader.ReadDouble();
    public double ReadFloat() => _reader.ReadSingle();
    public bool ReadBool32() => _reader.ReadInt32() != 0;
    public bool ReadBool8() => _reader.ReadByte() != 0;

    public byte[] ReadBytes(int n) => _reader.ReadBytes(n);
    public void SkipBytes(int n) => _reader.BaseStream.Seek(n, SeekOrigin.Current);

    // COLORREF (0x00BBGGRR as int32 LE) → #rrggbb
    public string ReadColor()
    {
        int v = _reader.ReadInt32();
        return $"#{v & 0xFF:x2}{(v >> 8) & 0xFF:x2}{(v >> 16) & 0xFF:x2}";
    }

    // Unix timestamp (int32 seconds) → ISO 8601
    public string ReadTimestamp()
    {
        int sec = _reader.ReadInt32();
        return DateTimeOffset.FromUnixTimeSeconds(sec).ToString("o");
    }

    // -------------------------------------------------------------------------
    // MFC CString
    // -------------------------------------------------------------------------

    public string ReadMfcString()
    {
        byte b0 = _reader.ReadByte();
        byte b1 = _reader.ReadByte();
        byte b2 = _reader.ReadByte();
        if (b0 != 0xFF || b1 != 0xFE || b2 != 0xFF)
            throw new InvalidDataException(
                $"Expected MFC CString header ff fe ff, found {b0:x2} {b1:x2} {b2:x2}");

        int len;
        byte lb = _reader.ReadByte();
        if (lb == 0xFF)
        {
            byte h0 = _reader.ReadByte();
            byte h1 = _reader.ReadByte();
            if (h0 == 0xFF && h1 == 0xFF)
                len = _reader.ReadInt32();
            else
                len = h0 | (h1 << 8);
        }
        else
        {
            len = lb;
        }

        if (len == 0) return "";
        return Encoding.Unicode.GetString(_reader.ReadBytes(len * 2));
    }

    // -------------------------------------------------------------------------
    // Diagnostics
    // -------------------------------------------------------------------------

    public IReadOnlyList<string> Warnings => _warnings;
    public void AddWarning(string msg) => _warnings.Add(msg);

    public string? PeekClassAt(long position)
    {
        long saved = _reader.BaseStream.Position;
        _reader.BaseStream.Seek(position, SeekOrigin.Begin);
        byte b0 = _reader.ReadByte(); byte b1 = _reader.ReadByte();
        string? name = null;
        if (b0 == 0x7f && b1 == 0xff)
        {
            int idx = (int)(_reader.ReadUInt32() & 0x7fffffff);
            if (_classRegistry.TryGetValue(idx, out var info)) name = info.Name;
        }
        else if (b0 == 0xff && b1 == 0xff)
        {
            _reader.ReadUInt16(); // archiveVersion
            ushort nameLen = _reader.ReadUInt16();
            name = Encoding.ASCII.GetString(_reader.ReadBytes(nameLen));
        }
        else if (!(b0 == 0 && b1 == 0))
        {
            int idx = (b0 | (b1 << 8)) & 0x7fff;
            if (_classRegistry.TryGetValue(idx, out var info)) name = info.Name;
        }
        _reader.BaseStream.Seek(saved, SeekOrigin.Begin);
        return name;
    }

    public void DumpBytes(int n)
    {
        long pos = _reader.BaseStream.Position;
        byte[] bytes = _reader.ReadBytes(n);
        _reader.BaseStream.Seek(pos, SeekOrigin.Begin);
        Console.Error.WriteLine($"  bytes: {string.Join(" ", bytes.Select(b => b.ToString("x2")))}");
    }

    internal void SetObjectLogValue(int index, string? value) => _objectLog[index].Value = value;

    internal void SetObjectLogGroup(int index, int idx, int total)
    {
        _objectLog[index].BlockObjectIdx = idx + 1;  // display is 1-based
        _objectLog[index].GroupTotal = total;
    }

    /// <summary>
    /// Copies every entry from <paramref name="secondary"/>'s ObjectLog into this log,
    /// remapping ObjIdx values to <c>100_000_000 + secondary.ObjIdx</c> so they never
    /// collide with the main counter.  ContainerObjIdx is remapped via the same offset
    /// except for root entries (ContainerObjIdx not present in secondary), which are
    /// attached to <paramref name="outerContainerObjIdx"/> (the second CBinary's ObjIdx).
    /// Start positions are adjusted by <paramref name="baseOffset"/> (the stream position
    /// immediately before the secondary buffer was read from the main stream).
    /// The original secondary ClassIdx / ObjIdx are preserved in SecondaryClassIdx /
    /// SecondaryObjIdx for CSV output.
    /// </summary>
    public void AppendSecondaryObjectLog(IsodatFile secondary, int outerContainerObjIdx, long baseOffset)
    {
        const int Base = 100_000_000;
        // Build the set of ObjIdx values that exist in the secondary log so we can
        // distinguish "no container" (root) from "container present but not found".
        var secondaryObjIdxSet = new HashSet<int>(secondary._objectLog.Select(e => e.ObjIdx));

        foreach (var e in secondary._objectLog)
        {
            int mappedObjIdx = Base + e.ObjIdx;
            int? mappedContainer = e.ContainerObjIdx.HasValue
                ? (secondaryObjIdxSet.Contains(e.ContainerObjIdx.Value)
                    ? Base + e.ContainerObjIdx.Value
                    : outerContainerObjIdx)
                : outerContainerObjIdx;

            var entry = new ObjectLogEntry(
                ClassIdx: e.ClassIdx,
                ObjIdx: mappedObjIdx,
                Start: baseOffset + e.Start,
                ContainerObjIdx: mappedContainer,
                ClassName: e.ClassName,
                ArchiveVersion: e.ArchiveVersion)
            {
                Value = e.Value,
                BlockObjectIdx = e.BlockObjectIdx,
                GroupTotal = e.GroupTotal,
                SecondaryClassIdx = e.ClassIdx,
                SecondaryObjIdx = e.ObjIdx,
            };
            _objectLog.Add(entry);
        }
    }

    public void Dispose() => _reader.Dispose();
}

public record ObjectLogEntry(
    int ClassIdx,
    int ObjIdx,
    long Start,
    int? ContainerObjIdx,
    string ClassName,
    int ArchiveVersion)
{
    public string? Value { get; internal set; }
    public int? BlockObjectIdx { get; internal set; }
    public int? GroupTotal { get; internal set; }
    public bool IsBlockObject => BlockObjectIdx.HasValue;
    public int? SecondaryClassIdx { get; internal set; } // original ClassIdx in the secondary archive
    public int? SecondaryObjIdx { get; internal set; }   // original ObjIdx in the secondary archive
}
