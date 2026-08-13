using System.Globalization;
using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Domain;

namespace QS3D.Platform.InMemory;

public sealed class InMemoryCadDatabase : ICadDatabase, ICadHistory
{
    private readonly object _sync = new();
    private readonly Stack<DatabaseState> _undo = new();
    private readonly Stack<DatabaseState> _redo = new();
    private Dictionary<CadHandle, CadEntitySnapshot> _entities = new();
    private ulong _nextHandle = 1;
    private long _revision;

    public InMemoryCadDatabase()
    {
    }

    public InMemoryCadDatabase(IEnumerable<CadEntitySnapshot> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ulong maxHandle = 0;
        foreach (var entity in entities)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var clone = entity with { Properties = CloneProperties(entity.Properties) };
            if (!_entities.TryAdd(entity.Handle, clone))
                throw new InvalidOperationException($"Duplicate CAD handle {entity.Handle} in snapshot.");
            var numericHandle = ulong.Parse(entity.Handle.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (numericHandle > maxHandle) maxHandle = numericHandle;
        }
        if (maxHandle == ulong.MaxValue && _entities.Count != 0)
            throw new InvalidOperationException("Snapshot exhausted the 64-bit CAD handle range.");
        _nextHandle = _entities.Count == 0 ? 1UL : maxHandle + 1UL;
    }

    public CadCapabilities Capabilities => CadCapabilities.TwoDimensional | CadCapabilities.Blocks | CadCapabilities.Layouts;
    public long Revision { get { lock (_sync) return _revision; } }
    public ICadHistory History => this;
    public bool CanUndo { get { lock (_sync) return _undo.Count != 0; } }
    public bool CanRedo { get { lock (_sync) return _redo.Count != 0; } }

    public ICadTransaction BeginTransaction(CadTransactionMode mode = CadTransactionMode.ReadWrite)
    {
        lock (_sync)
            return new Transaction(this, mode, _revision, _nextHandle, CloneEntities(_entities));
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

    private void Publish(long expectedRevision, ulong nextHandle, Dictionary<CadHandle, CadEntitySnapshot> entities)
    {
        lock (_sync)
        {
            if (_revision != expectedRevision)
                throw new InvalidOperationException("Drawing changed after this transaction began.");
            _undo.Push(Capture());
            _redo.Clear();
            _entities = CloneEntities(entities);
            _nextHandle = nextHandle;
            _revision++;
        }
    }

    private DatabaseState Capture() => new(CloneEntities(_entities), _nextHandle);

    private void Restore(DatabaseState state)
    {
        _entities = CloneEntities(state.Entities);
        _nextHandle = state.NextHandle;
    }

    private static Dictionary<CadHandle, CadEntitySnapshot> CloneEntities(Dictionary<CadHandle, CadEntitySnapshot> source)
    {
        var result = new Dictionary<CadHandle, CadEntitySnapshot>();
        foreach (var pair in source)
            result.Add(pair.Key, pair.Value with { Properties = CloneProperties(pair.Value.Properties) });
        return result;
    }

    private static Dictionary<string, string> CloneProperties(IReadOnlyDictionary<string, string> source)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in source)
            result.Add(pair.Key, pair.Value);
        return result;
    }

    private sealed record DatabaseState(Dictionary<CadHandle, CadEntitySnapshot> Entities, ulong NextHandle);

    private sealed class Transaction : ICadTransaction
    {
        private readonly InMemoryCadDatabase _owner;
        private readonly long _baseRevision;
        private Dictionary<CadHandle, CadEntitySnapshot>? _working;
        private ulong _nextHandle;
        private bool _committed;
        private bool _changed;

        public Transaction(InMemoryCadDatabase owner, CadTransactionMode mode, long baseRevision, ulong nextHandle, Dictionary<CadHandle, CadEntitySnapshot> working)
        {
            _owner = owner;
            Mode = mode;
            _baseRevision = baseRevision;
            _nextHandle = nextHandle;
            _working = working;
        }

        public CadTransactionMode Mode { get; }

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

        public CadHandle Append(CadEntityDraft draft)
        {
            RequireWrite();
            ArgumentNullException.ThrowIfNull(draft);
            if (_nextHandle == 0) throw new InvalidOperationException("CAD handle range exhausted.");
            var handle = new CadHandle(_nextHandle.ToString("X", CultureInfo.InvariantCulture));
            _nextHandle = _nextHandle == ulong.MaxValue ? 0 : _nextHandle + 1;
            var properties = draft.Properties is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : CloneProperties(draft.Properties);
            _working!.Add(handle, new CadEntitySnapshot(handle, draft.Kind, draft.Extents, properties));
            _changed = true;
            return handle;
        }

        public void Update(CadEntitySnapshot entity)
        {
            RequireWrite();
            ArgumentNullException.ThrowIfNull(entity);
            if (!_working!.ContainsKey(entity.Handle))
                throw new KeyNotFoundException($"Entity {entity.Handle} does not exist.");
            _working[entity.Handle] = entity with { Properties = CloneProperties(entity.Properties) };
            _changed = true;
        }

        public void Erase(CadHandle handle)
        {
            RequireWrite();
            if (!_working!.Remove(handle))
                throw new KeyNotFoundException($"Entity {handle} does not exist.");
            _changed = true;
        }

        public void Commit()
        {
            RequireWrite();
            if (_committed) throw new InvalidOperationException("Transaction is already committed.");
            if (_changed)
                _owner.Publish(_baseRevision, _nextHandle, _working!);
            _committed = true;
        }

        public void Dispose() => _working = null;

        private void RequireWrite()
        {
            EnsureOpen();
            if (Mode != CadTransactionMode.ReadWrite)
                throw new InvalidOperationException("Transaction is read-only.");
        }

        private void EnsureOpen()
        {
            if (_working is null)
                throw new ObjectDisposedException(nameof(Transaction));
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
        if (_documents.Any(x => x.Id == document.Id))
            throw new InvalidOperationException($"Drawing {document.Id.Value:D} is already open.");
        _documents.Add(document);
        ActiveDocument = document;
    }

    public void Activate(DrawingId id)
    {
        ActiveDocument = _documents.FirstOrDefault(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Drawing {id.Value:D} is not open.");
    }

    public bool Close(DrawingId id)
    {
        var index = _documents.FindIndex(x => x.Id == id);
        if (index < 0) return false;
        var closing = _documents[index];
        _documents.RemoveAt(index);
        if (ReferenceEquals(ActiveDocument, closing))
            ActiveDocument = _documents.LastOrDefault();
        return true;
    }
}
