// ImexpFiles.cs  —  generic .NET BinaryFormatter stream parser
//
// Parses an entire BinaryFormatter stream into a Dictionary<ObjectId, BfObj>
// that can then be navigated by field key.  The parser handles the forward-
// reference pattern used by the Qtegra/Imhotep serialization layer, where a
// parent object writes MemberReference records pointing to child ObjectIds
// that are defined later in the stream.

using System.Text;

static partial class ImexpReader
{
    // ── In-memory object graph ───────────────────────────────────────────────
    // Field values stored in BfObj.Fields are one of:
    //   bool / int / long / double / string / null  — primitive or null
    //   BfRef                                        — reference to another BfObj
    //   object?[]                                    — array of the above

    sealed class BfObj
    {
        public int Id;
        public string ClassName = "";
        public Dictionary<string, object?> Fields = new();
    }

    record BfRef(int Id);
    record BfSchema(
        int      ObjId,
        string   ClassName,
        int      FieldCount,
        string[] FieldNames,
        int[]    TypeEnums,
        TypeInfo[] TypeInfos,
        int      ValuePos);
    record TypeInfo(int? PrimitiveType = null, string? ClassName = null, int? LibraryId = null);

    // ── BinaryFormatter record type bytes ────────────────────────────────────
    const byte RT_ClassWithId            = 0x01;
    const byte RT_SystemClassWithMembers = 0x04;
    const byte RT_ClassWithMembers       = 0x05;
    const byte RT_BinaryObjectString     = 0x06;
    const byte RT_BinaryArray            = 0x07;
    const byte RT_MemberReference        = 0x09;
    const byte RT_ObjectNull             = 0x0A;
    const byte RT_MessageEnd             = 0x0B;
    const byte RT_BinaryLibrary          = 0x0C;
    const byte RT_NullMultiple256        = 0x0D;
    const byte RT_NullMultiple           = 0x0E;
    const byte RT_ArraySinglePrimitive   = 0x0F;
    const byte RT_ArraySingleObject      = 0x10;
    const byte RT_ArraySingleString      = 0x11;

    // ── Full-stream parser ───────────────────────────────────────────────────

    static Dictionary<int, BfObj> ParseStream(byte[] d)
    {
        var objs    = new Dictionary<int, BfObj>();
        var schemas = new Dictionary<int, BfSchema>();
        int p = 17; // skip SerializationHeaderRecord (1+4+4+4+4 bytes)

        while (p < d.Length && d[p] != RT_MessageEnd)
            p = ReadTopLevel(d, p, objs, schemas);

        return objs;
    }

    // Read one top-level record, populate objs/schemas, return next position.
    static int ReadTopLevel(byte[] d, int p, Dictionary<int, BfObj> objs, Dictionary<int, BfSchema> schemas)
    {
        byte rt = d[p];
        switch (rt)
        {
            case RT_BinaryLibrary:
            {
                p += 5; // RecordType(1) + LibraryId(4)
                var (len, np) = Read7Bit(d, p);
                return np + len;
            }

            case RT_ClassWithMembers:
            case RT_SystemClassWithMembers:
            {
                var sc  = ParseSchema(d, p, schemas);
                var obj = new BfObj { Id = sc.ObjId, ClassName = sc.ClassName };
                objs[obj.Id] = obj;
                int vp = sc.ValuePos;
                for (int i = 0; i < sc.FieldCount; i++)
                {
                    var (v, np) = ReadValue(d, vp, sc.TypeEnums[i], sc.TypeInfos[i], objs, schemas);
                    obj.Fields[sc.FieldNames[i]] = v;
                    vp = np;
                }
                return vp;
            }

            case RT_ClassWithId:
            {
                int oid    = ReadInt32(d, p + 1);
                int metaId = ReadInt32(d, p + 5);
                int vp     = p + 9;
                if (!schemas.TryGetValue(metaId, out var sc))
                    throw new Exception($"ClassWithId: no schema for MetadataId {metaId} at {p}");
                var obj = new BfObj { Id = oid, ClassName = sc.ClassName };
                objs[oid] = obj;
                for (int i = 0; i < sc.FieldCount; i++)
                {
                    var (v, np) = ReadValue(d, vp, sc.TypeEnums[i], sc.TypeInfos[i], objs, schemas);
                    obj.Fields[sc.FieldNames[i]] = v;
                    vp = np;
                }
                return vp;
            }

            case RT_BinaryObjectString:
            {
                int oid = ReadInt32(d, p + 1);
                var (sl, snp) = Read7Bit(d, p + 5);
                var sobj = new BfObj { Id = oid, ClassName = "__string" };
                sobj.Fields["__value"] = Encoding.UTF8.GetString(d, snp, sl);
                objs[oid] = sobj;
                return snp + sl;
            }

            case RT_BinaryArray:
            {
                var (_, nextP) = ReadBinaryArray(d, p, objs, schemas);
                return nextP;
            }

            case RT_ObjectNull:    return p + 1;
            case RT_NullMultiple256: return p + 2;
            case RT_NullMultiple:  return p + 5;

            case RT_ArraySinglePrimitive:
            {
                int oid     = ReadInt32(d, p + 1);
                int length  = ReadInt32(d, p + 5);
                int primType = d[p + 9];
                var elemTi  = new TypeInfo(PrimitiveType: primType);
                var items   = new object?[length];
                int vp      = p + 10;
                for (int i = 0; i < length; i++)
                {
                    var (v, np) = ReadValue(d, vp, 0, elemTi, objs, schemas);
                    items[i] = v; vp = np;
                }
                var arr = new BfObj { Id = oid, ClassName = "__array" };
                arr.Fields["__items"] = items;
                objs[oid] = arr;
                return vp;
            }

            case RT_ArraySingleObject:
            {
                int oid    = ReadInt32(d, p + 1);
                int length = ReadInt32(d, p + 5);
                int vp     = p + 9;
                var items  = new List<object?>();
                int e = 0;
                while (e < length)
                {
                    if (vp < d.Length && d[vp] == RT_NullMultiple256)
                    { int n = d[vp + 1]; vp += 2; for (int j = 0; j < n; j++) items.Add(null); e += n; continue; }
                    if (vp < d.Length && d[vp] == RT_NullMultiple)
                    { int n = ReadInt32(d, vp + 1); vp += 5; for (int j = 0; j < n; j++) items.Add(null); e += n; continue; }
                    var (v, np2) = ReadValue(d, vp, 2, new TypeInfo(), objs, schemas);
                    items.Add(v); vp = np2; e++;
                }
                var arr = new BfObj { Id = oid, ClassName = "__array" };
                arr.Fields["__items"] = items.ToArray();
                objs[oid] = arr;
                return vp;
            }

            case RT_ArraySingleString:
            {
                int oid    = ReadInt32(d, p + 1);
                int length = ReadInt32(d, p + 5);
                int vp     = p + 9;
                var items  = new List<object?>();
                int e = 0;
                while (e < length)
                {
                    if (vp < d.Length && d[vp] == RT_NullMultiple256)
                    { int n = d[vp + 1]; vp += 2; for (int j = 0; j < n; j++) items.Add(null); e += n; continue; }
                    if (vp < d.Length && d[vp] == RT_NullMultiple)
                    { int n = ReadInt32(d, vp + 1); vp += 5; for (int j = 0; j < n; j++) items.Add(null); e += n; continue; }
                    var (v, np2) = ReadValue(d, vp, 1, new TypeInfo(), objs, schemas);
                    items.Add(v); vp = np2; e++;
                }
                var arr = new BfObj { Id = oid, ClassName = "__array" };
                arr.Fields["__items"] = items.ToArray();
                objs[oid] = arr;
                return vp;
            }

            default:
                throw new Exception($"Unexpected top-level record 0x{rt:X2} at {p}");
        }
    }

    // Read a single field value; return (value, nextPosition).
    static (object? val, int next) ReadValue(byte[] d, int p, int typeEnum, TypeInfo ti,
        Dictionary<int, BfObj> objs, Dictionary<int, BfSchema> schemas)
    {
        if (typeEnum == 0) // Primitive
        {
            return ti.PrimitiveType switch
            {
                1  => (d[p] != 0,                                    p + 1),  // Boolean
                2  => ((int)d[p],                                    p + 1),  // Byte
                3  => ((int)d[p],                                    p + 2),  // Char (UTF-16 LE)
                5  => (ReadDecimalString(d, p),                      p + 16), // Decimal
                6  => (BitConverter.ToDouble(d, p),                  p + 8),  // Double
                7  => ((int)BitConverter.ToInt16(d, p),              p + 2),  // Int16
                8  => (ReadInt32(d, p),                              p + 4),  // Int32
                9  => (BitConverter.ToInt64(d, p),                   p + 8),  // Int64
                10 => ((int)(sbyte)d[p],                             p + 1),  // SByte
                11 => ((double)BitConverter.ToSingle(d, p),          p + 4),  // Single
                12 => (BitConverter.ToInt64(d, p),                   p + 8),  // TimeSpan (ticks)
                13 => (BitConverter.ToInt64(d, p),                   p + 8),  // DateTime (ticks)
                14 => ((int)BitConverter.ToUInt16(d, p),             p + 2),  // UInt16
                15 => ((long)BitConverter.ToUInt32(d, p),            p + 4),  // UInt32
                16 => (BitConverter.ToInt64(d, p),                   p + 8),  // UInt64
                _ => throw new Exception($"Unsupported PrimitiveType {ti.PrimitiveType} at {p}")
            };
        }

        if (typeEnum == 1) // String
        {
            if (d[p] == RT_BinaryObjectString)
            {
                int oid = ReadInt32(d, p + 1);
                var (sl, snp) = Read7Bit(d, p + 5);
                string sv = Encoding.UTF8.GetString(d, snp, sl);
                var sobj = new BfObj { Id = oid, ClassName = "__string" };
                sobj.Fields["__value"] = sv;
                objs[oid] = sobj;
                return (sv, snp + sl);
            }
            if (d[p] == RT_MemberReference) return (new BfRef(ReadInt32(d, p + 1)), p + 5);
            if (d[p] == RT_ObjectNull)      return (null, p + 1);
            throw new Exception($"ReadValue(String): unexpected 0x{d[p]:X2} at {p}");
        }

        // typeEnum 2 (Object), 3 (SystemClass), 4 (Class)
        if (typeEnum is 2 or 3 or 4)
        {
            if (d[p] == RT_MemberReference) return (new BfRef(ReadInt32(d, p + 1)), p + 5);
            if (d[p] == RT_ObjectNull)      return (null, p + 1);
            // A BinaryLibrary record may precede the class when its library is first seen
            int pp = p;
            while (pp < d.Length && d[pp] == RT_BinaryLibrary)
                pp = ReadTopLevel(d, pp, objs, schemas);
            int oid = d[pp] switch
            {
                RT_ClassWithMembers or RT_SystemClassWithMembers => ReadInt32(d, pp + 1),
                RT_ClassWithId        => ReadInt32(d, pp + 1),
                RT_BinaryArray        => ReadInt32(d, pp + 1),
                RT_BinaryObjectString => ReadInt32(d, pp + 1),
                _ => throw new Exception($"ReadValue(Class): unexpected 0x{d[pp]:X2} at {pp}")
            };
            return (new BfRef(oid), ReadTopLevel(d, pp, objs, schemas));
        }

        // typeEnum 5 (ObjectArray), 6 (StringArray), 7 (PrimitiveArray)
        if (typeEnum is 5 or 6 or 7)
        {
            if (d[p] == RT_MemberReference) return (new BfRef(ReadInt32(d, p + 1)), p + 5);
            if (d[p] == RT_ObjectNull)      return (null, p + 1);
            int pp = p;
            while (pp < d.Length && d[pp] == RT_BinaryLibrary)
                pp = ReadTopLevel(d, pp, objs, schemas);
            // BinaryArray (0x07) and all single-array shorthands store ObjectId at bytes 1-4
            return (new BfRef(ReadInt32(d, pp + 1)), ReadTopLevel(d, pp, objs, schemas));
        }

        throw new Exception($"ReadValue: unsupported BinaryTypeEnum {typeEnum} at {p}");
    }

    // Parse a BinaryArray record (0x07); return (objectId, nextPosition).
    static (int oid, int next) ReadBinaryArray(byte[] d, int p,
        Dictionary<int, BfObj> objs, Dictionary<int, BfSchema> schemas)
    {
        int oid  = ReadInt32(d, p + 1);
        int pp   = p + 6;                   // RecordType(1)+ObjId(4)+BinaryArrayTypeEnum(1)
        int rank = ReadInt32(d, pp); pp += 4;
        int total = 1;
        for (int r = 0; r < rank; r++) { total *= ReadInt32(d, pp); pp += 4; }

        int elemType = d[pp++];
        var elemTi   = new TypeInfo();
        if      (elemType == 0) { elemTi = new TypeInfo(PrimitiveType: d[pp++]); }
        else if (elemType == 3) { var (l, np) = Read7Bit(d, pp); elemTi = new TypeInfo(ClassName: Encoding.UTF8.GetString(d, np, l)); pp = np + l; }
        else if (elemType == 4) { var (l, np) = Read7Bit(d, pp); string cn = Encoding.UTF8.GetString(d, np, l); pp = np + l; int lib = ReadInt32(d, pp); pp += 4; elemTi = new TypeInfo(ClassName: cn, LibraryId: lib); }
        else if (elemType == 7) { elemTi = new TypeInfo(PrimitiveType: d[pp++]); }

        var items = new List<object?>();
        int e = 0;
        while (e < total)
        {
            if (pp < d.Length && d[pp] == RT_NullMultiple256)
            { int n = d[pp + 1]; pp += 2; for (int j = 0; j < n; j++) items.Add(null); e += n; continue; }
            if (pp < d.Length && d[pp] == RT_NullMultiple)
            { int n = ReadInt32(d, pp + 1); pp += 5; for (int j = 0; j < n; j++) items.Add(null); e += n; continue; }
            var (v, np2) = ReadValue(d, pp, elemType, elemTi, objs, schemas);
            items.Add(v); pp = np2; e++;
        }

        var arr = new BfObj { Id = oid, ClassName = "__array" };
        arr.Fields["__items"] = items.ToArray();
        objs[oid] = arr;
        return (oid, pp);
    }

    // ── Schema parser ────────────────────────────────────────────────────────

    static BfSchema ParseSchema(byte[] d, int pos, Dictionary<int, BfSchema> schemas)
    {
        bool isSystem = d[pos] == RT_SystemClassWithMembers;
        int objId = ReadInt32(d, pos + 1);
        int p = pos + 5;

        var (nameLen, p2) = Read7Bit(d, p);
        string className = Encoding.UTF8.GetString(d, p2, nameLen);
        p = p2 + nameLen;

        int fc = ReadInt32(d, p); p += 4;
        var names     = new string[fc];
        var typeEnums = new int[fc];
        var typeInfos = new TypeInfo[fc];

        for (int i = 0; i < fc; i++)
        { var (l, np) = Read7Bit(d, p); names[i] = Encoding.UTF8.GetString(d, np, l); p = np + l; }

        for (int i = 0; i < fc; i++) typeEnums[i] = d[p + i];
        p += fc;

        for (int i = 0; i < fc; i++)
        {
            int te = typeEnums[i];
            if      (te == 0) { typeInfos[i] = new TypeInfo(PrimitiveType: d[p++]); }
            else if (te == 3) { var (l, np) = Read7Bit(d, p); typeInfos[i] = new TypeInfo(ClassName: Encoding.UTF8.GetString(d, np, l)); p = np + l; }
            else if (te == 4) { var (l, np) = Read7Bit(d, p); string cn = Encoding.UTF8.GetString(d, np, l); p = np + l; int lib = ReadInt32(d, p); p += 4; typeInfos[i] = new TypeInfo(ClassName: cn, LibraryId: lib); }
            else if (te == 7) { typeInfos[i] = new TypeInfo(PrimitiveType: d[p++]); }
            else              { typeInfos[i] = new TypeInfo(); }
        }

        if (!isSystem) p += 4; // LibraryId for 0x05 records

        var sc = new BfSchema(objId, className, fc, names, typeEnums, typeInfos, p);
        schemas[objId] = sc;
        return sc;
    }

    // ── Low-level read helpers ───────────────────────────────────────────────

    static (int value, int next) Read7Bit(byte[] d, int pos)
    {
        int result = 0, shift = 0;
        while (true)
        { int b = d[pos++]; result |= (b & 0x7F) << shift; shift += 7; if ((b & 0x80) == 0) break; }
        return (result, pos);
    }

    static int ReadInt32(byte[] d, int p) =>
        d[p] | (d[p+1] << 8) | (d[p+2] << 16) | (d[p+3] << 24);

    static string ReadDecimalString(byte[] d, int p)
    {
        // .NET Decimal: lo(4) + mid(4) + hi(4) + flags(4)
        int lo    = ReadInt32(d, p);
        int mid   = ReadInt32(d, p + 4);
        int hi    = ReadInt32(d, p + 8);
        int flags = ReadInt32(d, p + 12);
        bool neg  = (flags & unchecked((int)0x80000000)) != 0;
        int scale = (flags >> 16) & 0x1F;
        return new decimal(lo, mid, hi, neg, (byte)scale)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    // ── Navigation helpers ───────────────────────────────────────────────────

    static BfObj? Resolve(object? val, Dictionary<int, BfObj> objs)
    {
        if (val is BfRef r && objs.TryGetValue(r.Id, out var o)) return o;
        return null;
    }

    static string? GetString(object? val, Dictionary<int, BfObj> objs)
    {
        if (val is string s) return s;
        if (val is BfRef)    return Resolve(val, objs)?.Fields.GetValueOrDefault("__value") as string;
        return null;
    }

    static object?[]? GetItems(object? val, Dictionary<int, BfObj> objs)
    {
        var arr = Resolve(val, objs);
        if (arr?.ClassName == "__array" && arr.Fields.TryGetValue("__items", out var items))
            return items as object?[];
        return null;
    }

    static IEnumerable<BfObj> GetObjItems(object? val, Dictionary<int, BfObj> objs)
    {
        var items = GetItems(val, objs);
        if (items is null) yield break;
        foreach (var item in items)
        {
            var o = Resolve(item, objs);
            if (o is not null) yield return o;
        }
    }

    // QuantityType objects (ElectricVoltage, ElectricResistance, etc.) serialize
    // as _Version(int32) + one double field.  Return that double, or null.
    static double? GetQuantityDouble(object? val, Dictionary<int, BfObj> objs)
    {
        var qobj = Resolve(val, objs);
        if (qobj is null) return null;
        foreach (var kv in qobj.Fields)
            if (kv.Value is double d) return d;
        return null;
    }
}
