# Isodat File Structure

Isodat files (`.dxf`, `.cf`, `.did`, `.caf`, `.scn`) are binary archives produced by Thermo Fisher's Isodat software. They store instrument data as a flat stream of serialized Microsoft Foundation Classes (MFC) C++ objects written by `CArchive`. Every valid isodat file begins with the two bytes `FF FF`, which is the CRuntimeClass header for the first object.

## MFC CArchive Serialization

MFC's `CArchive` serializes a C++ object graph by writing each object's class identity followed by the object's data fields. The identity is encoded as a **CRuntimeClass header** — a compact descriptor that either introduces a new class by name or references a class already seen earlier in the stream.

### CRuntimeClass Headers

| First 2 bytes (uint16 LE) | Meaning |
|---------------------------|---------|
| `FF FF` | New class: followed by a uint16 schema version, a uint16 class-name length, then the class name as ASCII bytes |
| `7F FF` | Long back-reference: followed by a uint32 that is the MFC map index of the previously registered class (it's unlikely that there are too many objects for shorts though) |
| `00 00` | NULL object pointer (no object follows) |
| Any value with high bit set (0x8000–0xBFFF, excluding 0xFFFF) | Short back-reference: the 15 lower bits are the MFC map index of the previously registered class |

After the class header (new or back-reference), the archive writes the object's serialized payload — the bytes produced by calling `CObject::Serialize(CArchive&)` on that instance.

### The MFC Map Counter

MFC maintains an internal counter (the "map") to assign stable indices to classes as they appear in the stream. The counter starts at **1**:

- Each **new-class header** (`FF FF`) consumes **two** map slots: one for the class descriptor itself and one for the first object instance of that class.
- Each **back-reference header** (short or long) consumes **one** map slot: one for the new object instance that reuses the already-registered class.
- A **NULL header** (`00 00`) consumes no slot.

Back-reference values encode the map index at the time the class was first registered. To resolve a back-reference, a reader must replay the entire counter sequence from the beginning — there is no random-access index.

## Sequential Reading Requirement

Isodat files **cannot be parsed in any order other than start-to-finish**. Two properties enforce this:

1. **Class resolution via map counter.** A back-reference like `0x8003` means "the class whose map index is 3." That index only has meaning if the reader has already processed all preceding objects and incremented the counter accordingly. Skipping any object or jumping to an arbitrary offset makes all subsequent back-references unresolvable.

2. **No skip table or offset index.** The file contains no table of contents, no per-object length prefix, and no byte-offset index. There is no way to compute where a given object ends without fully deserializing its payload, because object payloads may embed arbitrarily nested child objects.

As a consequence, the reader must process every object in document order, even objects whose data it does not care about.

## Object Payload Structure

Once the class header is consumed, the archive writes the object's payload in a fixed order determined by the C++ `Serialize` method:

1. **Parent class payload first.** Every `Serialize` override calls `ParentClass::Serialize(ar)` before writing its own fields. This means the fields of base classes appear in the stream before the fields of derived classes, and the entire inheritance chain is traversed recursively from the most-derived class up to the root.

2. **Schema version.** Immediately after the parent call (or at the very start if there is no parent call), the class writes a `DWORD` (4 bytes, uint32) schema version. This version gates which optional fields follow: a field introduced in version N is only present if `version >= N`.

3. **Own fields.** The class's fields are written in source-code declaration order. Field types follow standard C++ sizes: `DWORD`/`LONG` = 4 bytes, `double` = 8 bytes, `WORD` = 2 bytes, `BYTE` = 1 byte.

4. **Embedded child objects.** A class may write child objects inline by calling `ar.WriteObject(child)` or `ar << child`. Each child emits its own CRuntimeClass header followed by its own payload, producing recursive nesting.

## CBlockData Child-Object Lists

`CBlockData` subclasses (e.g. `CDualInletBlockData`, `CContiniousFlowBlockData`, `CScanStorage`) store variable-length lists of heterogeneous child objects. Each list is preceded by a `DWORD` count, followed by that many back-to-back object records (each with its own CRuntimeClass header and payload). These lists are the primary mechanism by which instrument-specific data sections (scan parts, hardware parts, gas configurations, etc.) are attached to a measurement.

## CString Encoding

MFC `CString` objects written by `CArchive` use a three-byte prefix:

```
FF FE FF
```

followed by a variable-length encoded character count (stored as a WORD if < 0x8000, or as `FF FF` + DWORD for longer strings), then the string data in **UTF-16 LE** (2 bytes per character). An empty string writes `FF FE FF 00 00`.

## File-Type Entry Points

Each file extension uses a fixed set of top-level objects as its entry point:

| Extension | Top-level objects (in order) |
|-----------|------------------------------|
| `.dxf`    | `CFileHeader`, `CContiniousFlowBlockData` |
| `.cf`     | `CFileHeader`, `CMethod`, `CPlotSettings`, `CBlockData` ×4 |
| `.did`    | `CFileHeader`, `CDualInletBlockData` |
| `.caf`    | `CFileHeader`, `CLong`, `CBlockDataContext` |
| `.scn`    | `CScanStorage` |

After the last expected object, the file must be fully read. Any remaining bytes indicate a parse error or an unimplemented trailing object.

## Related Documentation

- [Inheritance Hierarchy](isodat_inheritance_hierarchy.md) — full listing of all known C++ classes, their base classes, and which classes share a `Serialize` implementation.
- [Example: `.scn` structure tree](isodat_scn_structure_tree.md) — annotated object tree for a scan file, showing how `CScanStorage` nests hardware parts and gas configurations.
- [Example: `.dxf` structure tree](isodat_dxf_structure_tree.md) — annotated object tree for a continuous-flow file, showing the `CContiniousFlowBlockData` layout.
- [Example: `.did` structure tree](isodat_did_structure_tree.md) — annotated object tree for a dual-inlet file, showing the `CDualInletBlockData` layout.
