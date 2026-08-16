using QS3D.Platform.Parity;

internal static class TbqBqLibraryParitySmoke
{
    internal static void Run()
    {
        CreatesNamedHierarchyAndImportsProjectBills();
        SnapshotsRemainIndependent();
        ValidationFailsClosed();
    }

    private static void CreatesNamedHierarchyAndImportsProjectBills()
    {
        var library = TbqBqLibraryWorkspace.Create("Master BQ")
            .AddContainer("CAT-STR", BqLibraryNodeKind.Category, "Structure")
            .AddContainer("HEAD-CONC", BqLibraryNodeKind.Heading, "Concrete", "CAT-STR")
            .AddBill("MANUAL:B-000", Bill("B-000", "Manual bill"), "HEAD-CONC");

        var imported = library.ImportFromProject(
            new[]
            {
                Bill("B-002", "Imported second"),
                Bill("B-001", "Imported first")
            },
            "head-conc");

        Equal("Master BQ", imported.Name);
        Equal(5, imported.Nodes.Count);
        var children = imported.ChildrenOf("HEAD-CONC");
        Equal(3, children.Count);
        Sequence(new[] { "MANUAL:B-000", "PROJECT:B-001", "PROJECT:B-002" }, children.Select(static node => node.NodeId));
        Sequence(new[] { "B-000", "B-001", "B-002" }, children.Select(static node => node.BillItem!.ItemCode));
        Equal(BqLibraryNodeKind.Heading, imported.Nodes.Single(static node => node.NodeId == "HEAD-CONC").Kind);
    }

    private static void SnapshotsRemainIndependent()
    {
        var empty = TbqBqLibraryWorkspace.Create("Reusable Library");
        var category = empty.AddContainer("CAT", BqLibraryNodeKind.Category, "Category");
        var imported = category.ImportFromProject(new[] { Bill("B-1", "Past project bill") }, "CAT");

        Equal(0, empty.Nodes.Count);
        Equal(1, category.Nodes.Count);
        Equal(2, imported.Nodes.Count);
        Equal("m", imported.Nodes.Single(static node => node.BillItem is not null).BillItem!.Unit);
    }

    private static void ValidationFailsClosed()
    {
        var library = TbqBqLibraryWorkspace.Create("Library")
            .AddContainer("CAT", BqLibraryNodeKind.Category, "Category")
            .AddBill("BILL-1", Bill("B-1", "Bill"), "CAT");

        Throws<ArgumentException>(() => library.AddContainer("X", BqLibraryNodeKind.Bill, "Wrong"));
        Throws<InvalidOperationException>(() => library.AddContainer("cat", BqLibraryNodeKind.Subcategory, "Duplicate"));
        Throws<InvalidOperationException>(() => library.AddContainer("SUB", BqLibraryNodeKind.Subcategory, "Missing parent", "MISSING"));
        Throws<InvalidOperationException>(() => library.AddContainer("CHILD", BqLibraryNodeKind.Heading, "Under bill", "BILL-1"));
        Throws<InvalidOperationException>(() => library.AddBill("BILL-2", Bill("b-1", "Duplicate bill"), "CAT"));
        Throws<ArgumentException>(() => library.AddContainer("BLANK-PARENT", BqLibraryNodeKind.Category, "Blank parent", " "));
        Throws<ArgumentException>(() => library.AddBill("BLANK-BILL", Bill("B-9", "Blank parent"), " "));
        Throws<ArgumentException>(() => library.ChildrenOf(" "));
        Throws<ArgumentException>(() => library.ImportFromProject(
            new[] { Bill("B-2", "A"), Bill("b-2", "B") },
            "CAT"));
        Throws<ArgumentException>(() => library.ImportFromProject(new BqLibraryItem[] { null! }, "CAT"));
        Throws<InvalidOperationException>(() => library.ImportFromProject(new[] { Bill("B-3", "Bill") }, "BILL-1"));
    }

    private static BqLibraryItem Bill(string code, string description) =>
        new(code, description, "m", "Imported/Concrete");

    private static void Sequence<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        var expectedArray = expected.ToArray();
        var actualArray = actual.ToArray();
        if (!expectedArray.SequenceEqual(actualArray))
            throw new InvalidOperationException("Expected [" + string.Join(",", expectedArray) + "] but got [" + string.Join(",", actualArray) + "].");
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
    }
}
