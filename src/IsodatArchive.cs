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
public sealed class IsodatArchive : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly Dictionary<int, (string Name, int ArchiveVersion)> _classRegistry = new();
    private readonly List<string> _warnings = new();
    private int _mapCount = 1;  // MFC m_nMapCount, starts at 1

    public int LastArchiveVersion { get; private set; }
    public string? LastClassName { get; private set; }

    public IsodatArchive(Stream stream)
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
            _mapCount += 2;  // class slot + object slot
        }
        else if (b0 == 0x7F && b1 == 0xFF)
        {
            int packed   = _reader.ReadInt32();
            int classIdx = packed & 0x7FFF_FFFF;
            (className, archiveVersion) = _classRegistry[classIdx];
            _mapCount++;     // object slot only
        }
        else
        {
            int packed   = b0 | (b1 << 8);
            int classIdx = packed & 0x7FFF;
            (className, archiveVersion) = _classRegistry[classIdx];
            _mapCount++;     // object slot only
        }

        LastArchiveVersion = archiveVersion;
        LastClassName = className;

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
        int v = _reader.ReadInt32();
        if (v > maxSupported)
            _warnings.Add(
                $"{className} schema version v{v} is newer than supported v{maxSupported}; " +
                "fields added after that version will not be read");
        return v;
    }

    public long Position => _reader.BaseStream.Position;

    // -------------------------------------------------------------------------
    // Primitive reads
    // -------------------------------------------------------------------------

    public int    ReadInt32()   => _reader.ReadInt32();
    public int    ReadUInt16()  => _reader.ReadUInt16();
    public int    ReadUInt8()   => _reader.ReadByte();
    public long   ReadUInt32()  => _reader.ReadUInt32();   // uint → long, no overflow
    public double ReadDouble()  => _reader.ReadDouble();
    public double ReadFloat()   => _reader.ReadSingle();
    public bool   ReadBool32()  => _reader.ReadInt32() != 0;
    public bool   ReadBool8()   => _reader.ReadByte() != 0;

    public byte[] ReadBytes(int n)  => _reader.ReadBytes(n);
    public void   SkipBytes(int n)  => _reader.BaseStream.Seek(n, SeekOrigin.Current);

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

    public void Dispose() => _reader.Dispose();
}
