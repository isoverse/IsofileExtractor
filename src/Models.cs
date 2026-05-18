using JsonName = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace IsodatReader;

// ---------------------------------------------------------------------------
// Each record mirrors one read_<ClassName> function in the R package.
// The Read() method replicates the exact field order and version gates.
//
// Key rule: parent-class fields are read directly (no CRuntimeClass header)
// because parent Serialize() is called inline, not via WriteObject.
// Only leaf-class objects that were written with WriteObject() have a header.
// ---------------------------------------------------------------------------

/// <summary>CData — base for most isodat objects.</summary>
public record CData
{
    public int     Version { get; init; }
    public int     AppId  { get; init; }   // enum APP_ID at +0x04
    public string  Label  { get; init; } = "";
    public string  Value  { get; init; } = "";
    public int?    Flags  { get; init; }   // bit flags at +0x7c (v >= 3 only)

    public static CData Read(IsodatArchive ar)
    {
        int    version = ar.ReadSchemaVersion("CData", maxSupported: 3);
        int    appId   = ar.ReadUInt16();
        string label   = ar.ReadMfcString();
        string value   = ar.ReadMfcString();
        int?   flags   = version >= 3 ? ar.ReadInt32() : null;
        return new CData { Version = version, AppId = appId, Label = label, Value = value, Flags = flags };
    }
}

/// <summary>CData::CBlockData — named container with N child objects.</summary>
public record CBlockData
{
    [JsonName("p_c_data")]
    public CData PCData   { get; init; } = new();
    public int   Version  { get; init; }
    public int   NObjects { get; init; }  // children stored at +0x98

    public static CBlockData Read(IsodatArchive ar)
    {
        var cdata   = CData.Read(ar);
        int version = ar.ReadSchemaVersion("CBlockData", maxSupported: 2);
        int nObjs   = ar.ReadInt32();
        return new CBlockData { PCData = cdata, Version = version, NObjects = nObjs };
    }
}

/// <summary>CData::CTimeObject — UTC timestamp.</summary>
public record CTimeObject
{
    [JsonName("p_c_data")]
    public CData          PCData   { get; init; } = new();
    public int            Version  { get; init; }
    public DateTimeOffset Datetime { get; init; }

    public static CTimeObject Read(IsodatArchive ar)
    {
        var cdata   = CData.Read(ar);
        int version = ar.ReadSchemaVersion("CTimeObject", maxSupported: 1);
        int unixSec = ar.ReadInt32();
        return new CTimeObject
        {
            PCData   = cdata,
            Version  = version,
            Datetime = DateTimeOffset.FromUnixTimeSeconds(unixSec)
        };
    }
}

/// <summary>CSimple — lightweight base (label only, no value field).</summary>
public record CSimple
{
    public int    Version { get; init; }
    public string Label   { get; init; } = "";

    public static CSimple Read(IsodatArchive ar)
    {
        int    version = ar.ReadSchemaVersion("CSimple", maxSupported: 2);
        string label   = ar.ReadMfcString();
        return new CSimple { Version = version, Label = label };
    }
}

/// <summary>CSimple::CStr — string value.</summary>
public record CStr
{
    [JsonName("p_c_simple")]
    public CSimple PCSimple { get; init; } = new();
    public int     Version  { get; init; }
    public string  Value    { get; init; } = "";

    public static CStr Read(IsodatArchive ar)
    {
        var    csimple = CSimple.Read(ar);
        int    version = ar.ReadSchemaVersion("CStr", maxSupported: 2);
        string value   = ar.ReadMfcString();
        return new CStr { PCSimple = csimple, Version = version, Value = value };
    }
}

/// <summary>CBlockData::CDataIndex — index block, no child data in practice.</summary>
public record CDataIndex
{
    [JsonName("p_c_block_data")]
    public CBlockData PCBlockData { get; init; } = new();

    public static CDataIndex Read(IsodatArchive ar)
    {
        var cblock = CBlockData.Read(ar);
        if (cblock.NObjects != 0)
            throw new InvalidDataException(
                $"CDataIndex: expected 0 child objects, got {cblock.NObjects}");
        ar.ReadInt32();  // trailing sentinel (always 1, discarded on load)
        return new CDataIndex { PCBlockData = cblock };
    }
}

/// <summary>
/// CFileHeader — always the first object in a .dxf file.
///
/// Differs from other CBlockData subclasses: the parent CBlockData::Serialize
/// is called mid-function (after own fields magic/version/runtime_class/xac),
/// and only starting at schema version 3.
/// </summary>
public record CFileHeader
{
    public int        Magic              { get; init; }
    public int        Version            { get; init; }
    public string     RuntimeClass       { get; init; } = "";   // CString field at +0xa8; always "CBlockData"
    public string     Xac                { get; init; } = "";   // CString field at +0xac; usually empty
    public int?       Xb0               { get; init; }          // int at +0xb0; default 0 (v >= 2)
    [JsonName("p_c_block_data")]
    public CBlockData?  PCBlockData      { get; init; }          // parent CBlockData::Serialize (v >= 3)
    public CTimeObject? CTimeObject     { get; init; }          // via WriteObject (v >= 3)
    public CStr?        CStr            { get; init; }          // via WriteObject (v >= 3)
    public CDataIndex?  CDataIndex      { get; init; }          // via WriteObject (v >= 4)
    public string?    IsodatVersion     { get; init; }          // product version string (v >= 5)
    public string?    IsodatMinorVersion { get; init; }         // service pack version string (v >= 6)

    public static CFileHeader Read(IsodatArchive ar)
    {
        // Own fields before parent
        int    magic        = ar.ReadInt32();
        int    version      = ar.ReadSchemaVersion("CFileHeader", maxSupported: 6);
        string runtimeClass = ar.ReadMfcString();
        string xac          = ar.ReadMfcString();

        int? xb0 = version >= 2 ? ar.ReadInt32() : null;

        CBlockData?  cBlockData  = null;
        CTimeObject? cTimeObject = null;
        CStr?        cStr        = null;

        if (version >= 3)
        {
            // Parent CBlockData is serialized inline (no CRuntimeClass header)
            cBlockData = CBlockData.Read(ar);
            if (cBlockData.NObjects != 2)
                throw new InvalidDataException(
                    $"CFileHeader: expected 2 child CData objects in block, got {cBlockData.NObjects}");

            // CTimeObject and CStr were written with WriteObject → have CRuntimeClass headers
            ar.ReadCRuntimeClass("CTimeObject");
            cTimeObject = CTimeObject.Read(ar);

            ar.ReadCRuntimeClass("CStr");
            cStr = CStr.Read(ar);
        }

        CDataIndex? cDataIndex = null;
        if (version >= 4)
        {
            ar.ReadCRuntimeClass("CDataIndex");
            cDataIndex = CDataIndex.Read(ar);
        }

        string? isodatVersion      = null;
        string? isodatMinorVersion = null;
        if (version >= 5)
        {
            isodatVersion = ar.ReadMfcString();
            if (version >= 6)
                isodatMinorVersion = ar.ReadMfcString();
        }

        return new CFileHeader
        {
            Magic               = magic,
            Version             = version,
            RuntimeClass        = runtimeClass,
            Xac                 = xac,
            Xb0                 = xb0,
            PCBlockData         = cBlockData,
            CTimeObject         = cTimeObject,
            CStr                = cStr,
            CDataIndex          = cDataIndex,
            IsodatVersion       = isodatVersion,
            IsodatMinorVersion  = isodatMinorVersion
        };
    }
}
