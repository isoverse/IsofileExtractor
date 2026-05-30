using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;

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
        root["methods"] = ParseMethods(zip);

        // systems
        root["systems"] = ParseSystems(zip);

        // tasks
        var taskEntries = GetTaskEntries(zip);
        var tasksArray = new JsonArray();
        foreach (var entry in taskEntries.OrderBy(e => e.LastWriteTime))
            tasksArray.Add(ParseTask(entry));
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

    static JsonArray ParseMethods(ZipArchive zip)
    {
        var methodsArray = new JsonArray();

        // V2/V3-flat: Method_<id> at root
        foreach (var entry in zip.Entries
            .Where(e => e.FullName.StartsWith("Method_") && !e.FullName.Contains('/'))
            .OrderBy(e => e.FullName))
        {
            if (!int.TryParse(entry.Name["Method_".Length..], out int id)) continue;
            var doc = LoadXml(entry);
            // root is SerialisedMethodSnapshotProxy; snapshot content is under <Snapshot>
            var snap = doc.Root?.Element("Snapshot") ?? doc.Root;
            methodsArray.Add(ParseMethodSnapshot(snap, id));
        }

        // V3-nested: Snapshot/<id>/snapshot.xml
        foreach (var entry in zip.Entries
            .Where(e => e.FullName.StartsWith("Snapshot/") && e.Name == "snapshot.xml")
            .OrderBy(e => e.FullName))
        {
            var parts = entry.FullName.Split('/');
            if (parts.Length < 3 || !int.TryParse(parts[1], out int id)) continue;
            var doc = LoadXml(entry);
            methodsArray.Add(ParseMethodSnapshot(doc.Root, id));
        }

        return methodsArray;
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

    // ------- Systems -------

    // Fixed GUIDs inside the IRMS device (VisION / isoprime visION) PropertyBag
    const string IrmsSpeciesBagId = "4CBF5188-0ECA-46D3-9A8E-F913A4164934"; // per-species
    const string IrmsTuningBagId  = "D7A6969B-45A7-4BE7-B819-12E12D9C54F3"; // per-tuning
    const string IrmsBeamCupId    = "7440D4F0-2E31-40FF-BF19-5BC24A3227F9"; // per-beam cup

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
            beamsArray.Add(new JsonObject { ["beam"] = beam, ["low_gain"] = useLowGain == "True" });
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

            var tunings = spBag.Descendants("SerialisablePropertyBag")
                .Where(b => b.Element("Identifier")?.Value == IrmsTuningBagId)
                .Select(b => BagProps(b).GetValueOrDefault("TuningName")?.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
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

    static JsonObject ParseTask(ZipArchiveEntry entry)
    {
        var doc = LoadXml(entry);
        var r = doc.Root!;
        var jo = new JsonObject();

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

        var datasets = r.Element("DataSets")?.Elements("SerialisableDataSet") ?? [];
        var dsArray = new JsonArray();
        foreach (var ds in datasets)
        {
            var dsObj = new JsonObject();
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
                dsObj[key] = asInt && int.TryParse(v, out int n) ? (JsonNode)n : v;
            }
            dsArray.Add(dsObj);
        }
        if (dsArray.Count > 0) jo["datasets"] = dsArray;

        return jo;
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
