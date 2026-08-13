using System.Globalization;
using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Domain;
using QS3D.Platform.Geometry;

namespace QS3D.Platform.InMemory;

public sealed class InMemoryCadDatabase : ICadDatabase, ICadHistory
{
    private readonly object _sync = new();
    private readonly Stack<DatabaseState> _undo = new();
    private readonly Stack<DatabaseState> _redo = new();
    private Dictionary<CadHandle, CadEntitySnapshot> _entities = new();
    private Dictionary<string, CadLayerSnapshot> _layers = CreateDefaultLayers();
    private Dictionary<string, CadBlockDefinitionSnapshot> _blocks = CreateDefaultBlocks();
    private string _currentLayerName = "0";
    private ulong _nextHandle = 1;
    private long _revision;

    public InMemoryCadDatabase()
    {
    }

    public InMemoryCadDatabase(IEnumerable<CadEntitySnapshot> entities)
        : this(entities, null, null, null)
    {
    }

    public InMemoryCadDatabase(IEnumerable<CadEntitySnapshot> entities, IEnumerable<CadLayerSnapshot>? layers, string? currentLayerName)
        : this(entities, layers, currentLayerName, null)
    {
    }

    public InMemoryCadDatabase(
        IEnumerable<CadEntitySnapshot> entities,
        IEnumerable<CadLayerSnapshot>? layers,
        string? currentLayerName,
        IEnumerable<CadBlockDefinitionSnapshot>? blocks)
    {
        ArgumentNullException.ThrowIfNull(entities);
        if (layers is not null)
        {
            _layers.Clear();
            foreach (var layer in layers)
            {
                ArgumentNullException.ThrowIfNull(layer);
                var normalized = NormalizeLayerName(layer.Name);
                if (!_layers.TryAdd(normalized, layer with { Name = normalized }))
                    throw new InvalidOperationException($"Duplicate CAD layer '{normalized}' in snapshot.");
            }
        }
        if (!_layers.ContainsKey("0")) _layers.Add("0", new CadLayerSnapshot("0"));

        if (blocks is not null)
        {
            foreach (var block in blocks)
            {
                ArgumentNullException.ThrowIfNull(block);
                var clone = NormalizeBlock(block, _layers);
                if (!_blocks.TryAdd(clone.Name, clone))
                    throw new InvalidOperationException($"Duplicate CAD block '{clone.Name}' in snapshot.");
            }
        }

        ulong maxHandle = 0;
        foreach (var entity in entities)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var layerName = NormalizeLayerName(entity.LayerName);
            if (!_layers.ContainsKey(layerName)) _layers.Add(layerName, new CadLayerSnapshot(layerName));
            var clone = entity with { Properties = CloneProperties(entity.Properties), LayerName = layerName };
            if (!_entities.TryAdd(entity.Handle, clone))
                throw new InvalidOperationException($"Duplicate CAD handle {entity.Handle} in snapshot.");
            var numericHandle = ulong.Parse(entity.Handle.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (numericHandle > maxHandle) maxHandle = numericHandle;
        }
        if (maxHandle == ulong.MaxValue && _entities.Count != 0)
            throw new InvalidOperationException("Snapshot exhausted the 64-bit CAD handle range.");
        _nextHandle = _entities.Count == 0 ? 1UL : maxHandle + 1UL;

        foreach (var entity in _entities.Values)
        {
            if (entity.Kind != CadEntityKind.BlockReference) continue;
            if (!entity.Properties.TryGetValue(CadBlockReferencePropertyNames.BlockName, out var blockName)) continue;
            if (!_blocks.ContainsKey(NormalizeBlockName(blockName)))
                throw new InvalidOperationException($"Block reference {entity.Handle} targets missing block '{blockName}'.");
        }

        _currentLayerName = currentLayerName is null ? "0" : NormalizeLayerName(currentLayerName);
        if (!_layers.TryGetValue(_currentLayerName, out var current))
            throw new InvalidOperationException($"Current layer '{_currentLayerName}' does not exist in snapshot.");
        if (current.IsFrozen || current.IsLocked)
            throw new InvalidOperationException("Current layer cannot be frozen or locked in the bootstrap database.");
    }

    public CadCapabilities Capabilities => CadCapabilities.TwoDimensional | CadCapabilities.Blocks | CadCapabilities.Layouts | CadCapabilities.Layers;
    public long Revision { get { lock (_sync) return _revision; } }
    public ICadHistory History => this;
    public bool CanUndo { get { lock (_sync) return _undo.Count != 0; } }
    public bool CanRedo { get { lock (_sync) return _redo.Count != 0; } }

    public ICadTransaction BeginTransaction(CadTransactionMode mode = CadTransactionMode.ReadWrite)
    {
        lock (_sync)
            return new Transaction(this, mode, _revision, _nextHandle, CloneEntities(_entities), CloneLayers(_layers), CloneBlocks(_blocks), _currentLayerName);
    }

    public void Undo()
    {
        lock (_sync)
        {
            if (_undo.Count == 0) throw new InvalidOperationException("Nothing to undo.");
            _redo.Push(Capture());
            Restore(_undo.Pop());
            _revision++;
        }
    }

    public void Redo()
    {
        lock (_sync)
        {
            if (_redo.Count == 0) throw new InvalidOperationException("Nothing to redo.");
            _undo.Push(Capture());
            Restore(_redo.Pop());
            _revision++;
        }
    }

    private void Publish(
        long expectedRevision,
        ulong nextHandle,
        Dictionary<CadHandle, CadEntitySnapshot> entities,
        Dictionary<string, CadLayerSnapshot> layers,
        Dictionary<string, CadBlockDefinitionSnapshot> blocks,
        string currentLayerName)
    {
        lock (_sync)
        {
            if (_revision != expectedRevision)
                throw new InvalidOperationException("Drawing changed after this transaction began.");
            _undo.Push(Capture());
            _redo.Clear();
            _entities = CloneEntities(entities);
            _layers = CloneLayers(layers);
            _blocks = CloneBlocks(blocks);
            _currentLayerName = currentLayerName;
            _nextHandle = nextHandle;
            _revision++;
        }
    }

    private DatabaseState Capture() => new(CloneEntities(_entities), CloneLayers(_layers), CloneBlocks(_blocks), _currentLayerName, _nextHandle);

    private void Restore(DatabaseState state)
    {
        _entities = CloneEntities(state.Entities);
        _layers = CloneLayers(state.Layers);
        _blocks = CloneBlocks(state.Blocks);
        _currentLayerName = state.CurrentLayerName;
        _nextHandle = state.NextHandle;
    }

    private static Dictionary<string, CadLayerSnapshot> CreateDefaultLayers()
        => new(StringComparer.OrdinalIgnoreCase) { ["0"] = new CadLayerSnapshot("0") };

    private static Dictionary<string, CadBlockDefinitionSnapshot> CreateDefaultBlocks()
        => new(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<CadHandle, CadEntitySnapshot> CloneEntities(Dictionary<CadHandle, CadEntitySnapshot> source)
    {
        var result = new Dictionary<CadHandle, CadEntitySnapshot>();
        foreach (var pair in source)
            result.Add(pair.Key, pair.Value with { Properties = CloneProperties(pair.Value.Properties) });
        return result;
    }

    private static Dictionary<string, CadLayerSnapshot> CloneLayers(Dictionary<string, CadLayerSnapshot> source)
    {
        var result = new Dictionary<string, CadLayerSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source) result.Add(pair.Key, pair.Value);
        return result;
    }

    private static Dictionary<string, CadBlockDefinitionSnapshot> CloneBlocks(Dictionary<string, CadBlockDefinitionSnapshot> source)
    {
        var result = new Dictionary<string, CadBlockDefinitionSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source) result.Add(pair.Key, CloneBlock(pair.Value));
        return result;
    }

    private static CadBlockDefinitionSnapshot CloneBlock(CadBlockDefinitionSnapshot block)
        => new(block.Name, block.BasePoint, block.Entities.Select(CloneDraft).ToArray());

    private static CadEntityDraft CloneDraft(CadEntityDraft draft)
        => new(draft.Kind, draft.Extents, draft.Properties is null ? null : CloneProperties(draft.Properties), draft.LayerName);

    private static Dictionary<string, string> CloneProperties(IReadOnlyDictionary<string, string> source)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in source) result.Add(pair.Key, pair.Value);
        return result;
    }

    private static string NormalizeLayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Layer name must not be blank.", nameof(name));
        return name.Trim();
    }

    private static string NormalizeBlockName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Block name must not be blank.", nameof(name));
        return name.Trim();
    }

    private static CadBlockDefinitionSnapshot NormalizeBlock(CadBlockDefinitionSnapshot block, Dictionary<string, CadLayerSnapshot> layers)
    {
        var name = NormalizeBlockName(block.Name);
        if (block.Entities is null || block.Entities.Count == 0)
            throw new InvalidOperationException($"Block '{name}' must contain at least one entity.");
        var members = new CadEntityDraft[block.Entities.Count];
        for (var index = 0; index < block.Entities.Count; index++)
        {
            var member = block.Entities[index] ?? throw new InvalidOperationException($"Block '{name}' contains a null entity draft.");
            var layerName = member.LayerName is null ? "0" : NormalizeLayerName(member.LayerName);
            if (!layers.ContainsKey(layerName)) layers.Add(layerName, new CadLayerSnapshot(layerName));
            members[index] = new CadEntityDraft(member.Kind, member.Extents, member.Properties is null ? null : CloneProperties(member.Properties), layerName);
        }
        return new CadBlockDefinitionSnapshot(name, block.BasePoint, members);
    }

    private sealed record DatabaseState(
        Dictionary<CadHandle, CadEntitySnapshot> Entities,
        Dictionary<string, CadLayerSnapshot> Layers,
        Dictionary<string, CadBlockDefinitionSnapshot> Blocks,
        string CurrentLayerName,
        ulong NextHandle);

    private sealed class Transaction : ICadTransaction
    {
        private readonly InMemoryCadDatabase _owner;
        private readonly long _baseRevision;
        private Dictionary<CadHandle, CadEntitySnapshot>? _working;
        private Dictionary<string, CadLayerSnapshot>? _layers;
        private Dictionary<string, CadBlockDefinitionSnapshot>? _blocks;
        private string _currentLayerName;
        private ulong _nextHandle;
        private bool _committed;
        private bool _changed;

        public Transaction(
            InMemoryCadDatabase owner,
            CadTransactionMode mode,
            long baseRevision,
            ulong nextHandle,
            Dictionary<CadHandle, CadEntitySnapshot> working,
            Dictionary<string, CadLayerSnapshot> layers,
            Dictionary<string, CadBlockDefinitionSnapshot> blocks,
            string currentLayerName)
        {
            _owner = owner;
            Mode = mode;
            _baseRevision = baseRevision;
            _nextHandle = nextHandle;
            _working = working;
            _layers = layers;
            _blocks = blocks;
            _currentLayerName = currentLayerName;
        }

        public CadTransactionMode Mode { get; }
        public string CurrentLayerName { get { EnsureOpen(); return _currentLayerName; } }

        public CadEntitySnapshot? Get(CadHandle handle)
        {
            EnsureOpen();
            return _working!.TryGetValue(handle, out var entity) ? entity : null;
        }

        public IReadOnlyList<CadEntitySnapshot> Query()
        {
            EnsureOpen();
            return _working!.Values.OrderBy(static x => x.Handle).ToArray();
        }

        public IReadOnlyList<CadLayerSnapshot> GetLayers()
        {
            EnsureOpen();
            return _layers!.Values.OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(static x => x.Name, StringComparer.Ordinal).ToArray();
        }

        public CadLayerSnapshot? GetLayer(string name)
        {
            EnsureOpen();
            var normalized = NormalizeLayerName(name);
            return _layers!.TryGetValue(normalized, out var layer) ? layer : null;
        }

        public IReadOnlyList<CadBlockDefinitionSnapshot> GetBlocks()
        {
            EnsureOpen();
            return _blocks!.Values
                .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static x => x.Name, StringComparer.Ordinal)
                .Select(CloneBlock)
                .ToArray();
        }

        public CadBlockDefinitionSnapshot? GetBlock(string name)
        {
            EnsureOpen();
            var normalized = NormalizeBlockName(name);
            return _blocks!.TryGetValue(normalized, out var block) ? CloneBlock(block) : null;
        }

        public CadHandle Append(CadEntityDraft draft)
        {
            RequireWrite();
            ArgumentNullException.ThrowIfNull(draft);
            if (_nextHandle == 0) throw new InvalidOperationException("CAD handle range exhausted.");
            var layerName = draft.LayerName is null ? _currentLayerName : NormalizeLayerName(draft.LayerName);
            var layer = RequireLayer(layerName);
            if (layer.IsLocked) throw new InvalidOperationException($"Layer '{layerName}' is locked.");
            var handle = new CadHandle(_nextHandle.ToString("X", CultureInfo.InvariantCulture));
            _nextHandle = _nextHandle == ulong.MaxValue ? 0 : _nextHandle + 1;
            var properties = draft.Properties is null ? new Dictionary<string, string>(StringComparer.Ordinal) : CloneProperties(draft.Properties);
            _working!.Add(handle, new CadEntitySnapshot(handle, draft.Kind, draft.Extents, properties, layer.Name));
            _changed = true;
            return handle;
        }

        public void Update(CadEntitySnapshot entity)
        {
            RequireWrite();
            ArgumentNullException.ThrowIfNull(entity);
            if (!_working!.TryGetValue(entity.Handle, out var existing))
                throw new KeyNotFoundException($"Entity {entity.Handle} does not exist.");
            if (RequireLayer(existing.LayerName).IsLocked) throw new InvalidOperationException($"Layer '{existing.LayerName}' is locked.");
            var targetLayer = RequireLayer(entity.LayerName);
            if (targetLayer.IsLocked) throw new InvalidOperationException($"Layer '{targetLayer.Name}' is locked.");
            _working[entity.Handle] = entity with { Properties = CloneProperties(entity.Properties), LayerName = targetLayer.Name };
            _changed = true;
        }

        public void Erase(CadHandle handle)
        {
            RequireWrite();
            if (!_working!.TryGetValue(handle, out var existing))
                throw new KeyNotFoundException($"Entity {handle} does not exist.");
            if (RequireLayer(existing.LayerName).IsLocked) throw new InvalidOperationException($"Layer '{existing.LayerName}' is locked.");
            _working.Remove(handle);
            _changed = true;
        }

        public void CreateLayer(string name)
        {
            RequireWrite();
            var normalized = NormalizeLayerName(name);
            if (_layers!.ContainsKey(normalized)) throw new InvalidOperationException($"Layer '{normalized}' already exists.");
            _layers.Add(normalized, new CadLayerSnapshot(normalized));
            _changed = true;
        }

        public void UpdateLayer(CadLayerSnapshot layer)
        {
            RequireWrite();
            ArgumentNullException.ThrowIfNull(layer);
            var normalized = NormalizeLayerName(layer.Name);
            if (!_layers!.ContainsKey(normalized)) throw new KeyNotFoundException($"Layer '{normalized}' does not exist.");
            if (StringComparer.OrdinalIgnoreCase.Equals(normalized, _currentLayerName) && (layer.IsFrozen || layer.IsLocked))
                throw new InvalidOperationException("Current layer cannot be frozen or locked.");
            _layers[normalized] = layer with { Name = normalized };
            _changed = true;
        }

        public void EraseLayer(string name)
        {
            RequireWrite();
            var normalized = NormalizeLayerName(name);
            if (StringComparer.OrdinalIgnoreCase.Equals(normalized, "0")) throw new InvalidOperationException("Layer 0 cannot be erased.");
            if (StringComparer.OrdinalIgnoreCase.Equals(normalized, _currentLayerName)) throw new InvalidOperationException("Current layer cannot be erased.");
            if (!_layers!.ContainsKey(normalized)) throw new KeyNotFoundException($"Layer '{normalized}' does not exist.");
            if (_working!.Values.Any(entity => StringComparer.OrdinalIgnoreCase.Equals(entity.LayerName, normalized)))
                throw new InvalidOperationException($"Layer '{normalized}' cannot be erased while it owns entities.");
            if (_blocks!.Values.SelectMany(static block => block.Entities).Any(entity => StringComparer.OrdinalIgnoreCase.Equals(entity.LayerName ?? "0", normalized)))
                throw new InvalidOperationException($"Layer '{normalized}' cannot be erased while block definitions reference it.");
            _layers.Remove(normalized);
            _changed = true;
        }

        public void SetCurrentLayer(string name)
        {
            RequireWrite();
            var layer = RequireLayer(name);
            if (layer.IsFrozen || layer.IsLocked) throw new InvalidOperationException("Frozen or locked layer cannot become current.");
            if (!StringComparer.OrdinalIgnoreCase.Equals(_currentLayerName, layer.Name))
            {
                _currentLayerName = layer.Name;
                _changed = true;
            }
        }

        public void CreateBlock(string name, Point3 basePoint, IReadOnlyList<CadEntityDraft> entities)
        {
            RequireWrite();
            ArgumentNullException.ThrowIfNull(entities);
            var normalized = NormalizeBlockName(name);
            if (_blocks!.ContainsKey(normalized)) throw new InvalidOperationException($"Block '{normalized}' already exists.");
            if (entities.Count == 0) throw new InvalidOperationException("A block definition must contain at least one entity.");

            var members = new CadEntityDraft[entities.Count];
            for (var index = 0; index < entities.Count; index++)
            {
                var member = entities[index] ?? throw new InvalidOperationException("Block definition contains a null entity draft.");
                var layerName = member.LayerName is null ? "0" : NormalizeLayerName(member.LayerName);
                var layer = RequireLayer(layerName);
                members[index] = new CadEntityDraft(member.Kind, member.Extents, member.Properties is null ? null : CloneProperties(member.Properties), layer.Name);
            }

            _blocks.Add(normalized, new CadBlockDefinitionSnapshot(normalized, basePoint, members));
            _changed = true;
        }

        public void EraseBlock(string name)
        {
            RequireWrite();
            var normalized = NormalizeBlockName(name);
            if (!_blocks!.ContainsKey(normalized)) throw new KeyNotFoundException($"Block '{normalized}' does not exist.");
            if (_working!.Values.Any(entity => ReferencesBlock(entity, normalized)))
                throw new InvalidOperationException($"Block '{normalized}' cannot be erased while references exist.");
            _blocks.Remove(normalized);
            _changed = true;
        }

        public CadHandle InsertBlock(string name, Point3 insertionPoint, double uniformScale = 1d, double rotationRadians = 0d)
        {
            RequireWrite();
            var normalized = NormalizeBlockName(name);
            if (!_blocks!.TryGetValue(normalized, out var block)) throw new KeyNotFoundException($"Block '{normalized}' does not exist.");
            Numeric.RequireFinite(uniformScale, nameof(uniformScale));
            if (uniformScale <= 0d) throw new ArgumentOutOfRangeException(nameof(uniformScale), uniformScale, "Block scale must be greater than zero.");
            Numeric.RequireFinite(rotationRadians, nameof(rotationRadians));

            var extents = TransformExtents(block, insertionPoint, uniformScale, rotationRadians);
            var properties = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CadBlockReferencePropertyNames.BlockName] = block.Name,
                [CadBlockReferencePropertyNames.InsertionX] = insertionPoint.X.ToString("R", CultureInfo.InvariantCulture),
                [CadBlockReferencePropertyNames.InsertionY] = insertionPoint.Y.ToString("R", CultureInfo.InvariantCulture),
                [CadBlockReferencePropertyNames.InsertionZ] = insertionPoint.Z.ToString("R", CultureInfo.InvariantCulture),
                [CadBlockReferencePropertyNames.UniformScale] = uniformScale.ToString("R", CultureInfo.InvariantCulture),
                [CadBlockReferencePropertyNames.RotationRadians] = rotationRadians.ToString("R", CultureInfo.InvariantCulture)
            };
            return Append(new CadEntityDraft(CadEntityKind.BlockReference, extents, properties, _currentLayerName));
        }

        public void Commit()
        {
            RequireWrite();
            if (_committed) throw new InvalidOperationException("Transaction is already committed.");
            if (_changed) _owner.Publish(_baseRevision, _nextHandle, _working!, _layers!, _blocks!, _currentLayerName);
            _committed = true;
        }

        public void Dispose()
        {
            _working = null;
            _layers = null;
            _blocks = null;
        }

        private CadLayerSnapshot RequireLayer(string name)
        {
            var normalized = NormalizeLayerName(name);
            return _layers!.TryGetValue(normalized, out var layer) ? layer : throw new KeyNotFoundException($"Layer '{normalized}' does not exist.");
        }

        private static bool ReferencesBlock(CadEntitySnapshot entity, string blockName)
            => entity.Kind == CadEntityKind.BlockReference
                && entity.Properties.TryGetValue(CadBlockReferencePropertyNames.BlockName, out var referenced)
                && StringComparer.OrdinalIgnoreCase.Equals(referenced, blockName);

        private static BoundingBox3 TransformExtents(CadBlockDefinitionSnapshot block, Point3 insertionPoint, double scale, double rotationRadians)
        {
            Point3? minimum = null;
            Point3? maximum = null;
            foreach (var member in block.Entities)
            {
                var box = member.Extents;
                Accumulate(TransformPoint(new Point3(box.Min.X, box.Min.Y, box.Min.Z), block.BasePoint, insertionPoint, scale, rotationRadians), ref minimum, ref maximum);
                Accumulate(TransformPoint(new Point3(box.Min.X, box.Min.Y, box.Max.Z), block.BasePoint, insertionPoint, scale, rotationRadians), ref minimum, ref maximum);
                Accumulate(TransformPoint(new Point3(box.Min.X, box.Max.Y, box.Min.Z), block.BasePoint, insertionPoint, scale, rotationRadians), ref minimum, ref maximum);
                Accumulate(TransformPoint(new Point3(box.Min.X, box.Max.Y, box.Max.Z), block.BasePoint, insertionPoint, scale, rotationRadians), ref minimum, ref maximum);
                Accumulate(TransformPoint(new Point3(box.Max.X, box.Min.Y, box.Min.Z), block.BasePoint, insertionPoint, scale, rotationRadians), ref minimum, ref maximum);
                Accumulate(TransformPoint(new Point3(box.Max.X, box.Min.Y, box.Max.Z), block.BasePoint, insertionPoint, scale, rotationRadians), ref minimum, ref maximum);
                Accumulate(TransformPoint(new Point3(box.Max.X, box.Max.Y, box.Min.Z), block.BasePoint, insertionPoint, scale, rotationRadians), ref minimum, ref maximum);
                Accumulate(TransformPoint(new Point3(box.Max.X, box.Max.Y, box.Max.Z), block.BasePoint, insertionPoint, scale, rotationRadians), ref minimum, ref maximum);
            }
            return new BoundingBox3(minimum ?? throw new InvalidOperationException("Block has no extents."), maximum!.Value);
        }

        private static Point3 TransformPoint(Point3 point, Point3 basePoint, Point3 insertionPoint, double scale, double rotationRadians)
        {
            var x = (point.X - basePoint.X) * scale;
            var y = (point.Y - basePoint.Y) * scale;
            var z = (point.Z - basePoint.Z) * scale;
            var cosine = Math.Cos(rotationRadians);
            var sine = Math.Sin(rotationRadians);
            return new Point3(
                insertionPoint.X + (x * cosine) - (y * sine),
                insertionPoint.Y + (x * sine) + (y * cosine),
                insertionPoint.Z + z);
        }

        private static void Accumulate(Point3 point, ref Point3? minimum, ref Point3? maximum)
        {
            if (minimum is null)
            {
                minimum = point;
                maximum = point;
                return;
            }
            var min = minimum.Value;
            var max = maximum!.Value;
            minimum = new Point3(Math.Min(min.X, point.X), Math.Min(min.Y, point.Y), Math.Min(min.Z, point.Z));
            maximum = new Point3(Math.Max(max.X, point.X), Math.Max(max.Y, point.Y), Math.Max(max.Z, point.Z));
        }

        private void RequireWrite()
        {
            EnsureOpen();
            if (Mode != CadTransactionMode.ReadWrite) throw new InvalidOperationException("Transaction is read-only.");
        }

        private void EnsureOpen()
        {
            if (_working is null || _layers is null || _blocks is null) throw new ObjectDisposedException(nameof(Transaction));
        }
    }
}

public sealed class InMemorySelection : ICadSelection
{
    private readonly HashSet<CadHandle> _current = new();
    public IReadOnlyCollection<CadHandle> Current => _current.ToArray();
    public void Set(IEnumerable<CadHandle> handles)
    {
        ArgumentNullException.ThrowIfNull(handles);
        _current.Clear();
        foreach (var handle in handles) _current.Add(handle);
    }
    public void Clear() => _current.Clear();
}

public sealed class InMemoryEditor : ICadEditor
{
    private readonly List<string> _messages = new();
    public InMemoryEditor(ICadSelection selection) => Selection = selection;
    public ICadSelection Selection { get; }
    public IReadOnlyList<string> Messages => _messages;
    public void WriteMessage(string message) => _messages.Add(message ?? string.Empty);
}

public sealed class InMemoryCadDocument : ICadDocument
{
    public InMemoryCadDocument(string name) : this(DrawingId.New(), name, new InMemoryCadDatabase())
    {
    }

    public InMemoryCadDocument(DrawingId id, string name, InMemoryCadDatabase database)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("Drawing ID must not be empty.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name.Trim();
        Database = database ?? throw new ArgumentNullException(nameof(database));
        Editor = new InMemoryEditor(new InMemorySelection());
    }

    public DrawingId Id { get; }
    public string Name { get; }
    public ICadDatabase Database { get; }
    public ICadEditor Editor { get; }
}

public sealed class InMemoryDocumentManager : IDocumentManager
{
    private readonly List<ICadDocument> _documents = new();
    public IReadOnlyList<ICadDocument> Documents => _documents.ToArray();
    public ICadDocument? ActiveDocument { get; private set; }

    public ICadDocument CreateNew(string name)
    {
        var document = new InMemoryCadDocument(name);
        Open(document);
        return document;
    }

    public void Open(InMemoryCadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_documents.Any(x => x.Id == document.Id)) throw new InvalidOperationException($"Drawing {document.Id.Value:D} is already open.");
        _documents.Add(document);
        ActiveDocument = document;
    }

    public void Activate(DrawingId id)
    {
        ActiveDocument = _documents.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException($"Drawing {id.Value:D} is not open.");
    }

    public bool Close(DrawingId id)
    {
        var index = _documents.FindIndex(x => x.Id == id);
        if (index < 0) return false;
        var closing = _documents[index];
        _documents.RemoveAt(index);
        if (ReferenceEquals(ActiveDocument, closing)) ActiveDocument = _documents.LastOrDefault();
        return true;
    }
}
