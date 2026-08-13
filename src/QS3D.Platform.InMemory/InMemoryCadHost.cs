using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Domain;

namespace QS3D.Platform.InMemory;

public sealed class InMemoryCadDatabase : ICadDatabase
{
    private readonly object _sync = new();
    private Dictionary<CadHandle, CadEntitySnapshot> _entities = new();
    private long _nextHandle = 1;
    private long _revision;

    public CadCapabilities Capabilities => CadCapabilities.TwoDimensional | CadCapabilities.Blocks | CadCapabilities.Layouts;
    public long Revision { get { lock (_sync) return _revision; } }

    public ICadTransaction BeginTransaction(CadTransactionMode mode = CadTransactionMode.ReadWrite)
    {
        lock (_sync)
            return new Transaction(this, mode, _revision, _nextHandle, new Dictionary<CadHandle, CadEntitySnapshot>(_entities));
    }

    private void Publish(long expectedRevision, long nextHandle, Dictionary<CadHandle, CadEntitySnapshot> entities)
    {
        lock (_sync)
        {
            if (_revision != expectedRevision)
                throw new InvalidOperationException("Drawing changed after this transaction began.");
            _entities = entities;
            _nextHandle = nextHandle;
            _revision++;
        }
    }

    private sealed class Transaction : ICadTransaction
    {
        private readonly InMemoryCadDatabase _owner;
        private readonly long _baseRevision;
        private Dictionary<CadHandle, CadEntitySnapshot>? _working;
        private long _nextHandle;
        private bool _committed;

        public Transaction(InMemoryCadDatabase owner, CadTransactionMode mode, long baseRevision, long nextHandle, Dictionary<CadHandle, CadEntitySnapshot> working)
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
            var handle = new CadHandle((_nextHandle++).ToString("X"));
            var properties = draft.Properties is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(draft.Properties, StringComparer.Ordinal);
            _working!.Add(handle, new CadEntitySnapshot(handle, draft.Kind, draft.Extents, properties));
            return handle;
        }

        public void Update(CadEntitySnapshot entity)
        {
            RequireWrite();
            ArgumentNullException.ThrowIfNull(entity);
            if (!_working!.ContainsKey(entity.Handle))
                throw new KeyNotFoundException($"Entity {entity.Handle} does not exist.");
            _working[entity.Handle] = entity with { Properties = new Dictionary<string, string>(entity.Properties, StringComparer.Ordinal) };
        }

        public void Erase(CadHandle handle)
        {
            RequireWrite();
            if (!_working!.Remove(handle))
                throw new KeyNotFoundException($"Entity {handle} does not exist.");
        }

        public void Commit()
        {
            RequireWrite();
            if (_committed) throw new InvalidOperationException("Transaction is already committed.");
            _owner.Publish(_baseRevision, _nextHandle, new Dictionary<CadHandle, CadEntitySnapshot>(_working!));
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
    public InMemoryCadDocument(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = DrawingId.New();
        Name = name.Trim();
        Database = new InMemoryCadDatabase();
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
        _documents.Add(document);
        ActiveDocument = document;
        return document;
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
