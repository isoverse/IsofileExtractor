using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PureHDF;

namespace IsofileExtractor;

static class IarcReader
{
    public static void Read(ZipArchive zip, JsonObject root)
    {
        var infoEntry = zip.GetEntry("Info")
            ?? throw new InvalidDataException("no 'Info' entry in .iarc archive");

        ParseInfo(infoEntry, root, out int archiveVersion, out var plMeta);

        // processing lists
        var plArray = new JsonArray();
        foreach (var (guid, name, plId, nTasks) in plMeta)
        {
            var plEntry = zip.GetEntry($"ProcessingList_{plId}");
            if (plEntry is null) continue;
            plArray.Add(ParseProcessingList(plEntry, guid, name, plId, nTasks));
        }
        root["processing_lists"] = plArray;

        // methods
        var (methodsArray, methodParamMap) = ParseMethods(zip);
        root["methods"] = methodsArray;

        // systems
        root["systems"] = ParseSystems(zip);

        // tasks
        var taskEntries = GetTaskEntries(zip);
        var tasksArray = new JsonArray();
        foreach (var entry in taskEntries.OrderBy(e => e.LastWriteTime))
            tasksArray.Add(ParseTask(entry, zip, methodParamMap));
        root["tasks"] = tasksArray;
    }

    // ------- Info -------

    static void ParseInfo(ZipArchiveEntry entry, JsonObject root,
        out int archiveVersion,
        out List<(string Guid, string Name, int Id, int NTasks)> plMeta)
    {
        var doc = LoadXml(entry);
        archiveVersion = int.TryParse(doc.Root?.Element("Version")?.Value, out int v) ? v : 0;
        root["archive_version"] = archiveVersion;
        var created = doc.Root?.Element("CreatedDate")?.Value;
        if (!string.IsNullOrEmpty(created)) root["created_date"] = created;

        plMeta = doc.Root
            ?.Element("ProcessingLists")
            ?.Elements("SerialisedProcessingListMetaData")
            .Select(e => (
                Guid: e.Element("DefinitionUniqueIdentifier")?.Value ?? "",
                Name: e.Element("Name")?.Value ?? "",
                Id: int.TryParse(e.Element("ProcessingListId")?.Value, out int id) ? id : 0,
                NTasks: int.TryParse(e.Element("NumberOfTasks")?.Value, out int n) ? n : 0
            ))
            .ToList() ?? [];
    }

    // ------- ProcessingList -------

    static JsonObject ParseProcessingList(ZipArchiveEntry entry,
        string guid, string name, int plId, int nTasks)
    {
        var jo = new JsonObject
        {
            ["id"] = plId,
            ["name"] = name,
            ["guid"] = guid,
            ["n_tasks"] = nTasks,
        };

        var doc = LoadXml(entry);

        // Fixed GUIDs used by IonOS/LyticOS across all known archive versions:
        // species container, ratio list, and individual ratio definition bags
        const string SpeciesBagId = "10DC1602-5ED4-4D62-BAB0-2693E3FBC3AF";
        const string RatioListId  = "{BE588D62-C6A7-4718-A63D-7B0BDCBD9EEA}";
        const string RatioDefId   = "{42D28191-A6E9-4B7B-8C3D-0F0037624F7D}";

        var speciesArray = new JsonArray();
        foreach (var sb in doc.Descendants("SerialisablePropertyBag")
                             .Where(b => b.Element("Identifier")?.Value == SpeciesBagId))
        {
            var props = BagProps(sb);
            if (!props.TryGetValue("Species", out string? spName) || string.IsNullOrEmpty(spName))
                continue;

            var spObj = new JsonObject { ["name"] = spName };
            if (props.TryGetValue("DetectionBeamChannel", out string? detBm) && !string.IsNullOrEmpty(detBm))
                spObj["detection_beam"] = detBm;

            var ratiosArray = new JsonArray();
            foreach (var rb in ChildBags(sb, RatioListId).SelectMany(b => ChildBags(b, RatioDefId)))
            {
                var rp = BagProps(rb);
                rp.TryGetValue("Label", out string? label);
                if (string.IsNullOrEmpty(label)) continue;
                var ro = new JsonObject { ["label"] = label };
                if (rp.TryGetValue("NumeratorBeamChannel", out string? num) && !string.IsNullOrEmpty(num))
                    ro["numerator_beam"] = num;
                if (rp.TryGetValue("DenominatorBeamChannel", out string? den) && !string.IsNullOrEmpty(den))
                    ro["denominator_beam"] = den;
                if (rp.TryGetValue("DeltaLabel", out string? delta) && !string.IsNullOrEmpty(delta))
                    ro["delta_label"] = delta;
                ratiosArray.Add(ro);
            }
            if (ratiosArray.Count > 0) spObj["ratios"] = ratiosArray;
            speciesArray.Add(spObj);
        }
        if (speciesArray.Count > 0) jo["species"] = speciesArray;

        return jo;
    }

    // ------- Methods -------

    // paramMap: methodId → (flowParameterId → displayName), used to resolve task values
    // Source: SerialisedFlowParameter.Id → DisplayName (present in both flat and nested formats)
    static (JsonArray methods, Dictionary<int, Dictionary<string, string>> paramMap) ParseMethods(ZipArchive zip)
    {
        var methodsArray = new JsonArray();
        var paramMap = new Dictionary<int, Dictionary<string, string>>();

        // V2/V3-flat: Method_<id> at root
        foreach (var entry in zip.Entries
            .Where(e => e.FullName.StartsWith("Method_") && !e.FullName.Contains('/'))
            .OrderBy(e => e.FullName))
        {
            if (!int.TryParse(entry.Name["Method_".Length..], out int id)) continue;
            var doc = LoadXml(entry);
            var snap = doc.Root?.Element("Snapshot") ?? doc.Root;
            methodsArray.Add(ParseMethodSnapshot(snap, id));
            var pm = BuildFlowParamMap(doc);
            if (pm.Count > 0) paramMap[id] = pm;
        }

        // V3-nested: Snapshot/<id>/snapshot.xml
        foreach (var entry in zip.Entries
            .Where(e => e.FullName.StartsWith("Snapshot/") && e.Name == "snapshot.xml")
            .OrderBy(e => e.FullName))
        {
            var parts = entry.FullName.Split('/');
            if (parts.Length < 3 || !int.TryParse(parts[1], out int id)) continue;
            var doc = LoadXml(entry);
            var methodObj = ParseMethodSnapshot(doc.Root, id);
            AddDisplayBeamMasses(zip, id, methodObj);
            methodsArray.Add(methodObj);
            var pm = BuildFlowParamMap(doc);
            if (pm.Count > 0) paramMap[id] = pm;
        }

        return (methodsArray, paramMap);
    }

    static Dictionary<string, string> BuildFlowParamMap(XDocument doc)
    {
        var pm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // SerialisedFlowParameter.Id → DisplayName (base layer; present in all archive versions)
        foreach (var fp in doc.Descendants("SerialisedFlowParameter"))
        {
            string? flowId = fp.Element("Id")?.Value?.Trim();
            string? displayName = fp.Element("DisplayName")?.Value?.Trim();
            if (!string.IsNullOrEmpty(flowId) && !string.IsNullOrEmpty(displayName))
                pm[flowId] = displayName;
        }

        // SerialisedMethodParameter.FlowParameterId → ColumnName overrides DisplayName when present
        // (ColumnName reflects the processing-list column label used in Isodat UI)
        foreach (var mp in doc.Descendants("SerialisedMethodParameter"))
        {
            string? flowId = mp.Element("FlowParameterId")?.Value?.Trim();
            string? colName = mp.Element("ColumnName")?.Value?.Trim();
            if (!string.IsNullOrEmpty(flowId) && !string.IsNullOrEmpty(colName) && colName != "(none)")
                pm[flowId] = colName;
        }

        return pm;
    }

    static JsonObject ParseMethodSnapshot(XElement? snap, int id)
    {
        var jo = new JsonObject { ["id"] = id };

        void Set(string key, string xmlTag)
        {
            var v = snap?.Element(xmlTag)?.Value;
            if (!string.IsNullOrEmpty(v)) jo[key] = v;
        }

        Set("name", "Name");
        Set("global_id", "GlobalIdentifier");
        Set("processing_list_guid", "ProcessingListTypeIdentifier");

        var flows = snap?.Descendants("SerialisedHierarchicalFlow")
            .Select(f => f.Element("Name")?.Value)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList() ?? [];
        if (flows.Count > 0)
        {
            var flowsArray = new JsonArray();
            foreach (var f in flows) flowsArray.Add(f);
            jo["flows"] = flowsArray;
        }

        var namedParams = snap?.Descendants("SerialisedMethodParameter")
            .Select(mp => (
                Name: mp.Element("ColumnName")?.Value,
                Value: mp.Element("StringValue")?.Value
            ))
            .Where(p => !string.IsNullOrEmpty(p.Name) && p.Name != "(none)" && !string.IsNullOrEmpty(p.Value))
            .ToList() ?? [];
        if (namedParams.Count > 0)
        {
            var paramsArray = new JsonArray();
            foreach (var (name, value) in namedParams)
                paramsArray.Add(new JsonObject { ["name"] = name, ["value"] = value?.Trim() });
            jo["params"] = paramsArray;
        }

        return jo;
    }

    static void AddDisplayBeamMasses(ZipArchive zip, int methodId, JsonObject methodObj)
    {
        // Prefix for display settings files: Snapshot/<id>/Extensions/IRMSAcquisitionDisplaySettings/
        string prefix = $"Snapshot/{methodId}/Extensions/IRMSAcquisitionDisplaySettings/";

        var settingsEntries = zip.Entries
            .Where(e => e.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && e.FullName.Length > prefix.Length)
            .OrderBy(e => e.Name)
            .ToList();

        if (settingsEntries.Count == 0) return;

        var beamMassesArray = new JsonArray();
        foreach (var entry in settingsEntries)
        {
            string speciesName = Path.GetFileNameWithoutExtension(entry.Name);
            using var stream = entry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string json = reader.ReadToEnd();

            JsonNode? root;
            try { root = JsonNode.Parse(json); }
            catch { continue; }

            var beams = new JsonArray();
            CollectBeamMasses(root, beams);
            if (beams.Count == 0) continue;

            beamMassesArray.Add(new JsonObject
            {
                ["species"] = speciesName,
                ["beams"] = beams,
            });
        }

        if (beamMassesArray.Count > 0)
            methodObj["beam_masses"] = beamMassesArray;
    }

    static void CollectBeamMasses(JsonNode? node, JsonArray result)
    {
        if (node is not JsonObject obj) return;

        // Check if this node has P (properties) array with BeamChannel + MassNumber
        if (obj["P"] is JsonArray props)
        {
            string? beam = null, massStr = null;
            foreach (var p in props)
            {
                if (p is not JsonObject po) continue;
                string? id = po["I"]?.GetValue<string>();
                string? val = po["V"]?.GetValue<string>();
                if (id == "BeamChannel") beam = val;
                else if (id == "MassNumber") massStr = val;
            }
            if (beam is not null && massStr is not null && int.TryParse(massStr, out int mass))
            {
                result.Add(new JsonObject { ["beam"] = beam, ["mass"] = mass });
                return;
            }
        }

        // Recurse into B (children) array
        if (obj["B"] is JsonArray children)
            foreach (var child in children)
                CollectBeamMasses(child, result);
    }

    // ------- Systems -------

    // Fixed GUIDs inside the IRMS device (VisION / isoprime visION) PropertyBag
    const string IrmsSpeciesBagId = "4CBF5188-0ECA-46D3-9A8E-F913A4164934"; // per-species
    const string IrmsBeamCupId    = "7440D4F0-2E31-40FF-BF19-5BC24A3227F9"; // per-beam cup
    // Note: tuning bag GUID varies by archive version; detected by presence of TuningName property

    static JsonArray ParseSystems(ZipArchive zip)
    {
        var systemsArray = new JsonArray();

        foreach (var entry in zip.Entries
            .Where(e => e.FullName.StartsWith("System_") && !e.FullName.Contains('/'))
            .OrderBy(e => e.FullName))
        {
            var outer = LoadXml(entry).Root!;
            if (!int.TryParse(outer.Element("Id")?.Value, out int id)) continue;

            var jo = new JsonObject { ["id"] = id };
            var name = outer.Element("Name")?.Value;
            if (!string.IsNullOrEmpty(name)) jo["name"] = name;
            var gid = outer.Element("GlobalIdentifier")?.Value;
            if (!string.IsNullOrEmpty(gid)) jo["global_id"] = gid;

            var innerXml = outer.Element("SerialisedContent")?.Value;
            if (!string.IsNullOrEmpty(innerXml))
            {
                var inner = XDocument.Parse(innerXml).Root;
                ParseIrmsDevice(inner, jo);
            }

            systemsArray.Add(jo);
        }

        return systemsArray;
    }

    static void ParseIrmsDevice(XElement? inner, JsonObject jo)
    {
        if (inner is null) return;

        // Find the device whose PropertyBag contains species bags (stable GUID across versions)
        var irmsDevice = inner.Element("Devices")
            ?.Elements("SerialisedDeviceSnapshot")
            .FirstOrDefault(d => d.Descendants("SerialisablePropertyBag")
                .Any(b => b.Element("Identifier")?.Value == IrmsSpeciesBagId));
        if (irmsDevice is null) return;

        var deviceBag = irmsDevice.Element("PropertyBag");

        // Conductance calibration sets: bags with Beam1..Beam10 numeric properties
        var inuseCal  = default(Dictionary<string, double>?);
        var lowCal    = default(Dictionary<string, double>?);
        var highCal   = default(Dictionary<string, double>?);
        var beamNumPat = new Regex(@"^Beam\d+$");
        foreach (var bag in deviceBag?.Descendants("SerialisablePropertyBag") ?? [])
        {
            var props = BagProps(bag);
            var vals = new Dictionary<string, double>();
            foreach (var (k, v) in props)
            {
                if (!beamNumPat.IsMatch(k)) continue;
                if (double.TryParse(v, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double d) && d > 0)
                    vals[k] = d;
            }
            if (vals.Count == 0) continue;
            double fracLow = (double)vals.Values.Count(v => v > 5e-10) / vals.Count;
            if (fracLow > 0.75)
            {
                if (lowCal is null || vals.Count > lowCal.Count) lowCal = vals;
            }
            else if (fracLow < 0.25)
            {
                if (highCal is null || vals.Count > highCal.Count) highCal = vals;
            }
            else
            {
                if (inuseCal is null || vals.Count > inuseCal.Count) inuseCal = vals;
            }
        }

        // Beams: deduplicated by channel — identical across all species/tunings (fixed hardware)
        var beamsSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var beamsArray = new JsonArray();
        foreach (var beamBag in deviceBag?.Descendants("SerialisablePropertyBag")
                 .Where(b => b.Element("Identifier")?.Value == IrmsBeamCupId) ?? [])
        {
            var bp = BagProps(beamBag);
            if (!bp.TryGetValue("BeamChannel", out string? beam) || string.IsNullOrEmpty(beam))
                continue;
            if (!beamsSeen.Add(beam)) continue;
            bp.TryGetValue("UseLowGain", out string? useLowGain);
            bool lowGain = useLowGain == "True";

            var beamObj = new JsonObject
            {
                ["beam"] = beam,
                ["low_gain"] = lowGain,
                ["nominal_R_ohm"] = lowGain ? 1e9 : 1e11,
            };

            if (inuseCal is not null && inuseCal.TryGetValue(beam, out double inuseG))
            {
                beamObj["inuse_conductance_S"] = inuseG;
                beamObj["inuse_R_ohm"] = 1.0 / inuseG;
            }
            if (lowCal is not null && lowCal.TryGetValue(beam, out double lowG))
            {
                beamObj["low_gain_conductance_S"] = lowG;
                beamObj["low_gain_R_ohm"] = 1.0 / lowG;
            }
            if (highCal is not null && highCal.TryGetValue(beam, out double highG))
            {
                beamObj["high_gain_conductance_S"] = highG;
                beamObj["high_gain_R_ohm"] = 1.0 / highG;
            }

            beamsArray.Add(beamObj);
        }
        if (beamsArray.Count > 0) jo["beams"] = beamsArray;

        // Species
        var speciesArray = new JsonArray();
        foreach (var spBag in deviceBag?.Descendants("SerialisablePropertyBag")
                 .Where(b => b.Element("Identifier")?.Value == IrmsSpeciesBagId) ?? [])
        {
            var sp = BagProps(spBag);
            if (!sp.TryGetValue("SpeciesName", out string? spName) || string.IsNullOrEmpty(spName))
                continue;

            var spObj = new JsonObject { ["name"] = spName };
            if (sp.TryGetValue("IsCurrentSpecies", out string? isCur))
                spObj["is_current"] = isCur == "True";
            if (sp.TryGetValue("TargetBeam", out string? targetBeam) && !string.IsNullOrEmpty(targetBeam))
                spObj["target_beam"] = targetBeam;

            // Tuning bag GUID varies across archive versions; detect by TuningName property
            var tunings = spBag.Descendants("SerialisablePropertyBag")
                .Select(b => BagProps(b).GetValueOrDefault("TuningName"))
                .Where(t => !string.IsNullOrEmpty(t))
                .Select(t => Regex.Replace(t!.Trim(), @"\s+", " "))
                .Where(t => t.Length > 0)
                .Distinct()
                .ToList();
            if (tunings.Count > 0)
            {
                var tuningsArray = new JsonArray();
                foreach (var t in tunings) tuningsArray.Add(t);
                spObj["tunings"] = tuningsArray;
            }

            speciesArray.Add(spObj);
        }
        if (speciesArray.Count > 0) jo["species"] = speciesArray;
    }

    // ------- Task discovery -------

    static IEnumerable<ZipArchiveEntry> GetTaskEntries(ZipArchive zip)
    {
        // V3 nested: AcquisitionTask/Task_<UUID>/AcquisitionTask.xml
        var nested = zip.Entries
            .Where(e => e.FullName.StartsWith("AcquisitionTask/Task_") && e.Name == "AcquisitionTask.xml")
            .ToList();
        if (nested.Count > 0) return nested;

        // V2 / V3-flat: Task_<UUID> with no directory component
        return zip.Entries.Where(e => e.FullName.StartsWith("Task_") && !e.FullName.Contains('/'));
    }

    // ------- Task parsing -------

    static JsonObject ParseTask(ZipArchiveEntry entry, ZipArchive zip,
        Dictionary<int, Dictionary<string, string>> methodParamMap)
    {
        var doc = LoadXml(entry);
        var r = doc.Root!;
        var jo = new JsonObject();

        // V3-nested: "AcquisitionTask/Task_<uuid>/AcquisitionTask.xml" → taskHdfDir = "AcquisitionTask/Task_<uuid>"
        // V2/V3-flat: no directory component → root-level <id>.hdf5
        string? taskHdfDir = entry.FullName.Contains('/')
            ? string.Join("/", entry.FullName.Split('/').Take(2))
            : null;

        void Set(string key, string xmlTag)
        {
            var v = r.Element(xmlTag)?.Value;
            if (!string.IsNullOrEmpty(v)) jo[key] = v;
        }

        void SetInt(string key, string xmlTag)
        {
            var v = r.Element(xmlTag)?.Value;
            if (string.IsNullOrEmpty(v)) return;
            jo[key] = int.TryParse(v, out int n) ? (JsonNode)n : v;
        }

        Set("name", "Name");
        SetInt("id", "Id");
        Set("global_id", "GlobalIdentifier");
        Set("acquisition_start", "AcquisitionStartDate");
        Set("acquisition_end", "AcquisitionEndDate");
        Set("completion_state", "CompletionState");
        SetInt("method_id", "MethodId");
        SetInt("system_snapshot_id", "SystemSnapshotId");
        Set("processing_list_guid", "ProcessingListTypeIdentifier");
        // V3 additions
        Set("sample_type", "SampleType");
        Set("task_list_name", "TaskListName");
        Set("system_description", "SystemDescription");

        // Task parameter values — resolve names via method param map
        int methodId = (jo["method_id"] as JsonValue)?.TryGetValue<int>(out int mid) == true ? mid : -1;
        methodParamMap.TryGetValue(methodId, out var pm);
        var taskVals = r.Element("Values")?.Elements("SerialisableTaskValue") ?? [];
        var valuesObj = new JsonObject();
        foreach (var tv in taskVals)
        {
            string? paramId = tv.Element("ParameterIdentifier")?.Value?.Trim();
            string? val = tv.Element("Value")?.Value?.Trim();
            if (string.IsNullOrEmpty(paramId) || string.IsNullOrEmpty(val)) continue;
            string name = pm is not null && pm.TryGetValue(paramId, out string? colName) ? colName : paramId;
            valuesObj[name] = val;
        }
        if (valuesObj.Count > 0) jo["values"] = valuesObj;

        var datasets = r.Element("DataSets")?.Elements("SerialisableDataSet") ?? [];
        var dsArray = new JsonArray();
        foreach (var ds in datasets)
        {
            var dsObj = new JsonObject();
            int dsId = -1;
            foreach (var (key, tag, asInt) in new (string, string, bool)[]
            {
                ("id",     "Id",               true),
                ("type",   "TypeIdentifier",   false),
                ("status", "AcquireDataStatus", false),
                ("start",  "AcquireStartDate", false),
                ("end",    "AcquireEndDate",   false),
            })
            {
                var v = ds.Element(tag)?.Value;
                if (string.IsNullOrEmpty(v)) continue;
                if (asInt && int.TryParse(v, out int n))
                {
                    dsObj[key] = n;
                    if (key == "id") dsId = n;
                }
                else dsObj[key] = v;
            }
            if (dsId >= 0)
            {
                string hdfPath = taskHdfDir is not null
                    ? $"{taskHdfDir}/{dsId}/AcquisitionDataSet.hdf5"
                    : $"{dsId}.hdf5";
                var hdfEntry = zip.GetEntry(hdfPath);
                if (hdfEntry is not null)
                {
                    var hdfData = ReadHdf5Data(hdfEntry);
                    if (hdfData is not null) dsObj["data"] = hdfData;
                }
            }
            dsArray.Add(dsObj);
        }
        if (dsArray.Count > 0) jo["datasets"] = dsArray;

        return jo;
    }

    // ------- HDF5 -------

    static JsonObject? ReadHdf5Data(ZipArchiveEntry entry)
    {
        using var ms = new MemoryStream((int)entry.Length);
        using (var s = entry.Open()) s.CopyTo(ms);
        ms.Position = 0;

        using var file = H5File.Open(ms, leaveOpen: false);
        var ds = file.Dataset("DataSet");

        long n = (long)ds.Space.Dimensions[0];
        if (n == 0) return null;

        int rowSize = (int)ds.Type.Size;
        var members = ds.Type.Compound!.Members.ToList();
        var memberNames = members.Select(m => m.Name).ToHashSet();

        var raw = ds.Read<byte[]>();
        var data = new JsonObject();

        if (ds.AttributeExists("Species"))
        {
            string species = ds.Attribute("Species").Read<string>() ?? "";
            if (!string.IsNullOrEmpty(species)) data["species"] = species;
        }
        if (ds.AttributeExists("Tuning"))
        {
            string tuning = ds.Attribute("Tuning").Read<string>() ?? "";
            if (!string.IsNullOrEmpty(tuning)) data["tuning"] = tuning;
        }

        if (memberNames.Contains("Element"))
        {
            // Vario EA Results: Element (fixed-length UTF-16 string), PerCent (float64), Area (float64)
            var elemMem = members.First(m => m.Name == "Element");
            var pctMem  = members.First(m => m.Name == "PerCent");
            var areaMem = members.First(m => m.Name == "Area");
            var ea = new JsonArray();
            for (int i = 0; i < n; i++)
            {
                int off = i * rowSize;
                string elem = Encoding.Unicode
                    .GetString(raw, off + elemMem.Offset, elemMem.Type.Size)
                    .TrimEnd('\0', ' ');
                double pct  = BitConverter.ToDouble(raw, off + pctMem.Offset);
                double area = BitConverter.ToDouble(raw, off + areaMem.Offset);
                ea.Add(new JsonObject { ["element"] = elem, ["percent"] = pct, ["area"] = area });
            }
            data["ea_results"] = ea;
        }
        else
        {
            // Scan-based: Beam* or TCD — all columns are float64; emit each as an array
            foreach (var member in members)
            {
                int off = member.Offset;
                var arr = new double[n];
                for (int i = 0; i < n; i++)
                    arr[i] = BitConverter.ToDouble(raw, i * rowSize + off);
                data[member.Name] = JsonValue.Create(arr)!;
            }
        }

        return data;
    }

    // ------- Helpers -------

    // Files declare encoding="utf-16" but are physically stored as UTF-8/ASCII bytes.
    static XDocument LoadXml(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return XDocument.Load(reader);
    }

    static Dictionary<string, string> BagProps(XElement bag) =>
        bag.Element("SerialisedPropertyBagProperties")
           ?.Elements("PersistedPropertyBagProperty")
           .Where(p => p.Element("Identifier")?.Value is not null)
           .GroupBy(p => p.Element("Identifier")!.Value)
           .ToDictionary(g => g.Key, g => g.First().Element("Value")?.Value ?? "")
        ?? [];

    static IEnumerable<XElement> ChildBags(XElement bag, string id) =>
        bag.Element("SerialisedChildPropertyBags")
           ?.Elements("SerialisablePropertyBag")
           .Where(b => b.Element("Identifier")?.Value == id)
        ?? [];
}
