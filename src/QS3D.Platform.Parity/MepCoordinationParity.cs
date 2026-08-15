using System.Collections.ObjectModel;

namespace QS3D.Platform.Parity;

[Flags]
public enum MepRecognitionSource
{
    None = 0,
    Layer = 1,
    BlockName = 2,
    LayerOrBlockName = Layer | BlockName
}

public enum MepDiscipline
{
    Mep = 0,
    Structure = 1,
    Architecture = 2
}

public enum MepElementKind
{
    Duct = 0,
    Pipe = 1,
    CableTray = 2,
    Conduit = 3,
    Cable = 4,
    Fitting = 5,
    Accessory = 6,
    Equipment = 7,
    Fixture = 8
}

public enum MepRecognitionStatus
{
    Unmatched = 0,
    Matched = 1,
    Ambiguous = 2
}

public sealed class MepRecognitionRule
{
    public MepRecognitionRule(
        string id,
        int priority,
        MepDiscipline discipline,
        string category,
        IEnumerable<string> tokens,
        MepRecognitionSource source = MepRecognitionSource.LayerOrBlockName,
        MepElementKind? mepKind = null)
    {
        Id = Text.Require(id, nameof(id));
        Priority = priority;
        if (!Enum.IsDefined(typeof(MepDiscipline), discipline)) throw new ArgumentOutOfRangeException(nameof(discipline));
        Discipline = discipline;
        Category = Text.Require(category, nameof(category));
        if (source == MepRecognitionSource.None || (source & ~MepRecognitionSource.LayerOrBlockName) != 0)
            throw new ArgumentOutOfRangeException(nameof(source));
        Source = source;
        if (discipline == MepDiscipline.Mep)
        {
            if (!mepKind.HasValue || !Enum.IsDefined(typeof(MepElementKind), mepKind.Value))
                throw new ArgumentException("MEP recognition rules require a valid MEP kind.", nameof(mepKind));
        }
        else if (mepKind.HasValue)
        {
            throw new ArgumentException("Only MEP recognition rules may define a MEP kind.", nameof(mepKind));
        }
        MepKind = mepKind;

        if (tokens is null) throw new ArgumentNullException(nameof(tokens));
        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
        {
            var value = Text.Require(token, nameof(tokens));
            if (seen.Add(value)) normalized.Add(value);
        }
        if (normalized.Count == 0) throw new ArgumentException("At least one recognition token is required.", nameof(tokens));
        Tokens = new ReadOnlyCollection<string>(normalized);
    }

    public string Id { get; }
    public int Priority { get; }
    public MepDiscipline Discipline { get; }
    public string Category { get; }
    public MepRecognitionSource Source { get; }
    public MepElementKind? MepKind { get; }
    public IReadOnlyList<string> Tokens { get; }

    internal bool Matches(string layer, string blockName)
    {
        if ((Source & MepRecognitionSource.Layer) != 0 && ContainsAny(layer)) return true;
        if ((Source & MepRecognitionSource.BlockName) != 0 && ContainsAny(blockName)) return true;
        return false;
    }

    private bool ContainsAny(string source)
    {
        if (source.Length == 0) return false;
        for (var i = 0; i < Tokens.Count; i++)
            if (source.IndexOf(Tokens[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }
}

public sealed class MepRecognitionResult
{
    internal MepRecognitionResult(
        MepRecognitionStatus status,
        MepDiscipline? discipline,
        string? category,
        MepElementKind? mepKind,
        IReadOnlyList<string> matchedRuleIds)
    {
        Status = status;
        Discipline = discipline;
        Category = category;
        MepKind = mepKind;
        MatchedRuleIds = matchedRuleIds;
    }

    public MepRecognitionStatus Status { get; }
    public MepDiscipline? Discipline { get; }
    public string? Category { get; }
    public MepElementKind? MepKind { get; }
    public IReadOnlyList<string> MatchedRuleIds { get; }
}

public sealed class MepRecognitionProfile
{
    private readonly IReadOnlyList<MepRecognitionRule> _rules;

    public MepRecognitionProfile(IEnumerable<MepRecognitionRule> rules)
    {
        if (rules is null) throw new ArgumentNullException(nameof(rules));
        var snapshot = new List<MepRecognitionRule>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules)
        {
            if (rule is null) throw new ArgumentException("Recognition profile contains a null rule.", nameof(rules));
            if (!ids.Add(rule.Id)) throw new ArgumentException("Duplicate recognition rule id: " + rule.Id + ".", nameof(rules));
            snapshot.Add(rule);
        }
        if (snapshot.Count == 0) throw new ArgumentException("Recognition profile must contain at least one rule.", nameof(rules));
        snapshot.Sort(static (left, right) =>
        {
            var byPriority = right.Priority.CompareTo(left.Priority);
            return byPriority != 0 ? byPriority : StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id);
        });
        _rules = new ReadOnlyCollection<MepRecognitionRule>(snapshot);
    }

    public IReadOnlyList<MepRecognitionRule> Rules => _rules;

    public MepRecognitionResult Recognize(string? layer, string? blockName)
    {
        var layerText = (layer ?? string.Empty).Trim();
        var blockText = (blockName ?? string.Empty).Trim();
        var top = new List<MepRecognitionRule>();
        var priority = int.MinValue;
        for (var i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            if (!rule.Matches(layerText, blockText)) continue;
            if (rule.Priority < priority) break;
            if (rule.Priority > priority)
            {
                priority = rule.Priority;
                top.Clear();
            }
            top.Add(rule);
        }

        if (top.Count == 0)
            return new MepRecognitionResult(MepRecognitionStatus.Unmatched, null, null, null, Array.Empty<string>());

        var first = top[0];
        var ambiguous = false;
        for (var i = 1; i < top.Count; i++)
        {
            var current = top[i];
            if (current.Discipline != first.Discipline ||
                !StringComparer.OrdinalIgnoreCase.Equals(current.Category, first.Category) ||
                current.MepKind != first.MepKind)
            {
                ambiguous = true;
                break;
            }
        }

        var ids = new string[top.Count];
        for (var i = 0; i < top.Count; i++) ids[i] = top[i].Id;
        if (ambiguous)
            return new MepRecognitionResult(MepRecognitionStatus.Ambiguous, null, null, null, ids);
        return new MepRecognitionResult(MepRecognitionStatus.Matched, first.Discipline, first.Category, first.MepKind, ids);
    }
}

public static class MepRecognitionProfiles
{
    public static MepRecognitionProfile CreateDefault() => new(new[]
    {
        Mep("mep.cable-tray", 900, "CableTray", MepElementKind.CableTray, "CABLETRAY", "CABLE_TRAY", "CABLE-TRAY", "TRAY"),
        Mep("mep.conduit", 890, "Conduit", MepElementKind.Conduit, "CONDUIT"),
        Mep("mep.duct", 880, "Duct", MepElementKind.Duct, "DUCT"),
        Mep("mep.pipe", 870, "Pipe", MepElementKind.Pipe, "PIPE", "PIPING"),
        Mep("mep.cable", 860, "Cable", MepElementKind.Cable, "CABLE", "WIRE"),
        Mep("mep.fitting", 850, "Fitting", MepElementKind.Fitting, "FITTING", "ELBOW", "REDUCER", "COUPLING", "TEE_", "TEE-"),
        Mep("mep.accessory", 840, "Accessory", MepElementKind.Accessory, "VALVE", "DAMPER", "ACCESSORY"),
        Mep("mep.equipment", 830, "Equipment", MepElementKind.Equipment, "EQUIP", "AHU", "FCU", "PUMP", "FAN", "CHILLER", "BOILER"),
        Mep("mep.fixture", 820, "Fixture", MepElementKind.Fixture, "FIXTURE", "LUMINAIRE", "LIGHTING", "LIGHT_", "LIGHT-", "SOCKET", "OUTLET", "SWITCH", "SANITARY", "SPRINKLER"),
        Building("structure.beam", 700, MepDiscipline.Structure, "Beam", "BEAM"),
        Building("structure.column", 690, MepDiscipline.Structure, "Column", "COLUMN"),
        Building("structure.foundation", 680, MepDiscipline.Structure, "Foundation", "FOOTING", "FOUNDATION", "PILE"),
        Building("structure.generic", 670, MepDiscipline.Structure, "Structure", "STRUCT", "RC_", "RC-"),
        Building("architecture.wall", 600, MepDiscipline.Architecture, "Wall", "WALL"),
        Building("architecture.slab", 590, MepDiscipline.Architecture, "Slab", "SLAB", "FLOOR"),
        Building("architecture.ceiling", 580, MepDiscipline.Architecture, "Ceiling", "CEILING"),
        Building("architecture.roof", 570, MepDiscipline.Architecture, "Roof", "ROOF"),
        Building("architecture.generic", 560, MepDiscipline.Architecture, "Architecture", "ARCH")
    });

    private static MepRecognitionRule Mep(string id, int priority, string category, MepElementKind kind, params string[] tokens) =>
        new(id, priority, MepDiscipline.Mep, category, tokens, MepRecognitionSource.LayerOrBlockName, kind);

    private static MepRecognitionRule Building(string id, int priority, MepDiscipline discipline, string category, params string[] tokens) =>
        new(id, priority, discipline, category, tokens);
}

public sealed class MepElement
{
    public MepElement(
        string id,
        MepElementKind kind,
        string system,
        string specification,
        string region,
        int count = 1,
        double lengthM = 0,
        double areaM2 = 0,
        double volumeM3 = 0)
    {
        Id = Text.Require(id, nameof(id));
        if (!Enum.IsDefined(typeof(MepElementKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind;
        System = Text.Require(system, nameof(system));
        Specification = Text.Require(specification, nameof(specification));
        Region = Text.Require(region, nameof(region));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        Count = count;
        LengthM = Numeric.NonNegativeFinite(lengthM, nameof(lengthM));
        AreaM2 = Numeric.NonNegativeFinite(areaM2, nameof(areaM2));
        VolumeM3 = Numeric.NonNegativeFinite(volumeM3, nameof(volumeM3));
    }

    public string Id { get; }
    public MepElementKind Kind { get; }
    public string System { get; }
    public string Specification { get; }
    public string Region { get; }
    public int Count { get; }
    public double LengthM { get; }
    public double AreaM2 { get; }
    public double VolumeM3 { get; }
}

public sealed class MepQuantityGroup
{
    internal MepQuantityGroup(string region, string system, string specification, MepElementKind kind, int elementCount, int quantityCount, double lengthM, double areaM2, double volumeM3)
    {
        Region = region;
        System = system;
        Specification = specification;
        Kind = kind;
        ElementCount = elementCount;
        QuantityCount = quantityCount;
        LengthM = lengthM;
        AreaM2 = areaM2;
        VolumeM3 = volumeM3;
    }

    public string Region { get; }
    public string System { get; }
    public string Specification { get; }
    public MepElementKind Kind { get; }
    public int ElementCount { get; }
    public int QuantityCount { get; }
    public double LengthM { get; }
    public double AreaM2 { get; }
    public double VolumeM3 { get; }
}

public sealed class MepQuantityService
{
    public IReadOnlyList<MepQuantityGroup> Aggregate(IEnumerable<MepElement> elements)
    {
        if (elements is null) throw new ArgumentNullException(nameof(elements));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groups = new Dictionary<string, MutableMepGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in elements)
        {
            if (element is null) throw new ArgumentException("MEP input contains null.", nameof(elements));
            if (!seen.Add(element.Id)) throw new ArgumentException("Duplicate MEP element id: " + element.Id + ".", nameof(elements));
            var key = element.Region + "\u001f" + element.System + "\u001f" + element.Specification + "\u001f" + (int)element.Kind;
            if (!groups.TryGetValue(key, out var group))
            {
                group = new MutableMepGroup(element);
                groups.Add(key, group);
            }
            group.Add(element);
        }

        var result = new List<MepQuantityGroup>(groups.Count);
        foreach (var group in groups.Values) result.Add(group.ToImmutable());
        result.Sort(static (left, right) =>
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.Region, right.Region);
            if (compare != 0) return compare;
            compare = StringComparer.OrdinalIgnoreCase.Compare(left.System, right.System);
            if (compare != 0) return compare;
            compare = StringComparer.OrdinalIgnoreCase.Compare(left.Specification, right.Specification);
            return compare != 0 ? compare : left.Kind.CompareTo(right.Kind);
        });
        return new ReadOnlyCollection<MepQuantityGroup>(result);
    }

    private sealed class MutableMepGroup
    {
        internal MutableMepGroup(MepElement seed)
        {
            Region = seed.Region;
            System = seed.System;
            Specification = seed.Specification;
            Kind = seed.Kind;
        }

        internal string Region { get; }
        internal string System { get; }
        internal string Specification { get; }
        internal MepElementKind Kind { get; }
        internal int ElementCount { get; private set; }
        internal int QuantityCount { get; private set; }
        internal double LengthM { get; private set; }
        internal double AreaM2 { get; private set; }
        internal double VolumeM3 { get; private set; }

        internal void Add(MepElement element)
        {
            ElementCount++;
            checked { QuantityCount += element.Count; }
            LengthM = Numeric.SafeAdd(LengthM, element.LengthM, "MEP length");
            AreaM2 = Numeric.SafeAdd(AreaM2, element.AreaM2, "MEP area");
            VolumeM3 = Numeric.SafeAdd(VolumeM3, element.VolumeM3, "MEP volume");
        }

        internal MepQuantityGroup ToImmutable() => new(Region, System, Specification, Kind, ElementCount, QuantityCount, LengthM, AreaM2, VolumeM3);
    }
}

public readonly struct AxisAlignedBox
{
    public AxisAlignedBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
    {
        MinX = Numeric.Finite(minX, nameof(minX));
        MinY = Numeric.Finite(minY, nameof(minY));
        MinZ = Numeric.Finite(minZ, nameof(minZ));
        MaxX = Numeric.Finite(maxX, nameof(maxX));
        MaxY = Numeric.Finite(maxY, nameof(maxY));
        MaxZ = Numeric.Finite(maxZ, nameof(maxZ));
        if (maxX < minX || maxY < minY || maxZ < minZ) throw new ArgumentException("AABB maximum must not be below minimum.");
    }

    public double MinX { get; }
    public double MinY { get; }
    public double MinZ { get; }
    public double MaxX { get; }
    public double MaxY { get; }
    public double MaxZ { get; }
}

public sealed class CoordinationElement
{
    public CoordinationElement(string elementId, MepDiscipline discipline, string category, string system, string region, AxisAlignedBox bounds)
    {
        ElementId = Text.Require(elementId, nameof(elementId));
        if (!Enum.IsDefined(typeof(MepDiscipline), discipline)) throw new ArgumentOutOfRangeException(nameof(discipline));
        Discipline = discipline;
        Category = Text.Require(category, nameof(category));
        System = Text.Require(system, nameof(system));
        Region = Text.Require(region, nameof(region));
        Bounds = bounds;
    }

    public string ElementId { get; }
    public MepDiscipline Discipline { get; }
    public string Category { get; }
    public string System { get; }
    public string Region { get; }
    public AxisAlignedBox Bounds { get; }
}

public enum ClashKind
{
    Hard = 0,
    Clearance = 1
}

public sealed class ClashResult
{
    internal ClashResult(string leftElementId, string rightElementId, ClashKind kind, double separationM, double overlapXM, double overlapYM, double overlapZM)
    {
        LeftElementId = leftElementId;
        RightElementId = rightElementId;
        Kind = kind;
        SeparationM = separationM;
        OverlapXM = overlapXM;
        OverlapYM = overlapYM;
        OverlapZM = overlapZM;
    }

    public string LeftElementId { get; }
    public string RightElementId { get; }
    public ClashKind Kind { get; }
    public double SeparationM { get; }
    public double OverlapXM { get; }
    public double OverlapYM { get; }
    public double OverlapZM { get; }
}

public sealed class ClashDetectionService
{
    public IReadOnlyList<ClashResult> Detect(IEnumerable<CoordinationElement> elements, double clearanceM, bool includeSameDiscipline = false)
    {
        if (elements is null) throw new ArgumentNullException(nameof(elements));
        clearanceM = Numeric.NonNegativeFinite(clearanceM, nameof(clearanceM));
        var list = new List<CoordinationElement>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in elements)
        {
            if (element is null) throw new ArgumentException("Coordination input contains null.", nameof(elements));
            if (!ids.Add(element.ElementId)) throw new ArgumentException("Duplicate coordination element id: " + element.ElementId + ".", nameof(elements));
            list.Add(element);
        }
        list.Sort(static (a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.ElementId, b.ElementId));

        var results = new List<ClashResult>();
        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                var left = list[i];
                var right = list[j];
                if (!includeSameDiscipline && left.Discipline == right.Discipline) continue;
                var dx = Gap(left.Bounds.MinX, left.Bounds.MaxX, right.Bounds.MinX, right.Bounds.MaxX);
                var dy = Gap(left.Bounds.MinY, left.Bounds.MaxY, right.Bounds.MinY, right.Bounds.MaxY);
                var dz = Gap(left.Bounds.MinZ, left.Bounds.MaxZ, right.Bounds.MinZ, right.Bounds.MaxZ);
                var separation = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (separation > clearanceM) continue;
                var hard = dx == 0d && dy == 0d && dz == 0d;
                if (!hard && clearanceM == 0d) continue;
                results.Add(new ClashResult(
                    left.ElementId,
                    right.ElementId,
                    hard ? ClashKind.Hard : ClashKind.Clearance,
                    separation,
                    Overlap(left.Bounds.MinX, left.Bounds.MaxX, right.Bounds.MinX, right.Bounds.MaxX),
                    Overlap(left.Bounds.MinY, left.Bounds.MaxY, right.Bounds.MinY, right.Bounds.MaxY),
                    Overlap(left.Bounds.MinZ, left.Bounds.MaxZ, right.Bounds.MinZ, right.Bounds.MaxZ)));
            }
        }
        return new ReadOnlyCollection<ClashResult>(results);
    }

    private static double Gap(double minA, double maxA, double minB, double maxB)
    {
        if (maxA < minB) return minB - maxA;
        if (maxB < minA) return minA - maxB;
        return 0d;
    }

    private static double Overlap(double minA, double maxA, double minB, double maxB)
    {
        var value = Math.Min(maxA, maxB) - Math.Max(minA, minB);
        return value > 0d ? value : 0d;
    }
}

internal static class Text
{
    internal static string Require(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Text value is required.", parameterName);
        var trimmed = value.Trim();
        for (var i = 0; i < trimmed.Length; i++)
            if (char.IsControl(trimmed[i])) throw new ArgumentException("Text value must not contain control characters.", parameterName);
        return trimmed;
    }
}

internal static class Numeric
{
    internal static double Finite(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
        return value == 0d ? 0d : value;
    }

    internal static double NonNegativeFinite(double value, string parameterName)
    {
        value = Finite(value, parameterName);
        if (value < 0d) throw new ArgumentOutOfRangeException(parameterName, "Value must be non-negative.");
        return value;
    }

    internal static double SafeAdd(double left, double right, string label)
    {
        var value = left + right;
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " overflowed.");
        return value == 0d ? 0d : value;
    }
}
