using QS3D.Platform.Domain;
using QS3D.Platform.Parity;

internal static class CubicostSharedParitySmoke
{
    internal static void Run()
    {
        Recognition();
        RecognitionCatalog();
        MepAggregation();
        ClashDetection();
        RateBuildUpAndBenchmark();
        SmartRateApplication();
        BqReferencesAndAdjustment();
        TenderAndProgress();
        TenderRevisionAndRounds();
        TimePhasedCost();
        IdentificationAndIssueReview();
    }

    private static void Recognition()
    {
        var profile = MepRecognitionProfiles.CreateDefault();
        var tray = profile.Recognize("mep-cabletray-main", null);
        Equal(MepRecognitionStatus.Matched, tray.Status);
        Equal(MepElementKind.CableTray, tray.MepKind!.Value);
        Equal(MepDiscipline.Mep, tray.Discipline!.Value);

        var custom = new MepRecognitionProfile(new[]
        {
            new MepRecognitionRule("pipe", 50, MepDiscipline.Mep, "Pipe", new[] { "SERVICE" }, mepKind: MepElementKind.Pipe),
            new MepRecognitionRule("duct", 50, MepDiscipline.Mep, "Duct", new[] { "SERVICE" }, mepKind: MepElementKind.Duct)
        });
        var ambiguous = custom.Recognize("service-main", null);
        Equal(MepRecognitionStatus.Ambiguous, ambiguous.Status);
        Require(!ambiguous.MepKind.HasValue, "ambiguous recognition must fail closed");
    }

    private static void RecognitionCatalog()
    {
        var catalog = new MepRecognitionProfileCatalog();
        catalog.Add(new NamedMepRecognitionProfile("default", "Default", MepRecognitionProfiles.CreateDefault(), true));
        Equal("default", catalog.Default!.ProfileId);
        Throws<InvalidOperationException>(() => catalog.Add(new NamedMepRecognitionProfile("other", "Other", MepRecognitionProfiles.CreateDefault(), true)));
        Throws<InvalidOperationException>(() => catalog.Add(new NamedMepRecognitionProfile("DEFAULT", "Duplicate", MepRecognitionProfiles.CreateDefault())));
    }

    private static void MepAggregation()
    {
        var rows = new MepQuantityService().Aggregate(new[]
        {
            new MepElement("D-1", MepElementKind.Duct, "SA", "500x300", "ZONE-A", 1, 10, 16),
            new MepElement("D-2", MepElementKind.Duct, "sa", "500X300", "zone-a", 1, 5, 8),
            new MepElement("FCU-1", MepElementKind.Equipment, "CHW", "FCU-05", "ZONE-A")
        });
        Equal(2, rows.Count);
        var duct = rows.Single(x => x.Kind == MepElementKind.Duct);
        Equal(2, duct.ElementCount);
        Equal(2, duct.QuantityCount);
        Near(15, duct.LengthM);
        Near(24, duct.AreaM2);
    }

    private static void ClashDetection()
    {
        var clashes = new ClashDetectionService().Detect(new[]
        {
            new CoordinationElement("S-1", MepDiscipline.Structure, "Beam", "STRUCT", "ZONE-A", new AxisAlignedBox(0, 0, 0, 1, 1, 1)),
            new CoordinationElement("M-1", MepDiscipline.Mep, "Duct", "SA", "ZONE-A", new AxisAlignedBox(.5, .2, .2, 1.5, .8, .8)),
            new CoordinationElement("M-2", MepDiscipline.Mep, "Pipe", "CHW", "ZONE-A", new AxisAlignedBox(1.1, 2, 0, 2, 3, 1))
        }, 1.05, true);
        Equal(2, clashes.Count);
        Equal(ClashKind.Hard, clashes[0].Kind);
        Equal(ClashKind.Clearance, clashes[1].Kind);
    }

    private static void RateBuildUpAndBenchmark()
    {
        var buildUp = new CostRateBuildUp("R1", "CONC", "m3", "VND", new[]
        {
            new CostResourceComponent("LAB", "Labour", "hr", 2m, 50m),
            new CostResourceComponent("MAT", "Material", "kg", 3m, 20m)
        }, 10m, 5m);
        Equal(160m, buildUp.DirectUnitCost);
        Equal(184.8m, buildUp.UnitRate);

        var catalog = new HistoricalCostCatalog(new[]
        {
            new HistoricalCostRecord("H1", "CONC", "APT|HN", 10m, 100m, "VND", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            new HistoricalCostRecord("H2", "CONC", "APT|HN", 20m, 240m, "VND", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        });
        var benchmark = new CostBenchmarkService().Analyze(catalog, "CONC", "APT|HN", "VND", 12.1m);
        Equal(2, benchmark.SampleCount);
        Equal(11m, benchmark.AverageUnitCost);
        Equal(10m, benchmark.DeviationFromAveragePercent!.Value);

        var largeCatalog = new HistoricalCostCatalog(new[]
        {
            new HistoricalCostRecord("MAX1", "MAX", "APT|HN", 1m, decimal.MaxValue, "VND", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            new HistoricalCostRecord("MAX2", "MAX", "APT|HN", 1m, decimal.MaxValue, "VND", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        });
        var largeBenchmark = new CostBenchmarkService().Analyze(largeCatalog, "MAX", "APT|HN", "VND");
        Equal(decimal.MaxValue, largeBenchmark.AverageUnitCost);
        Equal(decimal.MaxValue, largeBenchmark.MedianUnitCost);
    }

    private static void SmartRateApplication()
    {
        var service = new SmartRateApplicationService();
        var request = new RateApplicationRequest("A", "m3", "APT|HN");
        var matched = service.Match(request, new[]
        {
            new RateApplicationCandidate("A", "m3", "APT|HN", 100m, "history", 10),
            new RateApplicationCandidate("A", "m3", "APT|HN", 110m, "preferred", 20)
        });
        Equal(RateApplicationStatus.Matched, matched.Status);
        Equal(110m, matched.UnitRate!.Value);
        Equal("preferred", matched.SourceId!);

        var ambiguous = service.Match(request, new[]
        {
            new RateApplicationCandidate("A", "m3", "APT|HN", 100m, "one", 20),
            new RateApplicationCandidate("A", "m3", "APT|HN", 110m, "two", 20)
        });
        Equal(RateApplicationStatus.Ambiguous, ambiguous.Status);
        Require(!ambiguous.UnitRate.HasValue, "ambiguous smart rate must fail closed");
    }

    private static void BqReferencesAndAdjustment()
    {
        var catalog = new BqLibraryCatalog(new[] { new BqLibraryItem("A", "Concrete", "m3") });
        catalog.ImportFromProject(new[] { new BqLibraryItem("A", "Concrete 2", "m3") }, true);
        Equal("Concrete 2", catalog.Find("a")!.Description);
        Throws<ArgumentException>(() => catalog.ImportFromProject(new[]
        {
            new BqLibraryItem("B", "One", "m"),
            new BqLibraryItem("b", "Two", "m")
        }, true));

        var index = new CostReferenceIndex(new[]
        {
            new CostReferenceMark("M1", "A", "R1", "Library"),
            new CostReferenceMark("M2", "B", "R1", "Project")
        });
        Equal(2, index.FindByRate("r1").Count);
        Equal(110m, CostAdjustmentService.ByRatio(100m, 10m).AdjustedTotal);

        var target = CostAdjustmentService.ToTarget(100m, 125m);
        Equal(125m, target.AdjustedTotal);
        Equal(25m, target.RatioPercent);
        var zero = CostAdjustmentService.ToTarget(0m, 0m);
        Equal(0m, zero.AdjustedTotal);
        Equal(0m, zero.RatioPercent);
        Throws<InvalidOperationException>(() => CostAdjustmentService.ToTarget(0m, 1m));

        var trades = TradeCostAnalysisService.Analyze(new[]
        {
            new TradeCostLine("Structure", 60m),
            new TradeCostLine(" ", 40m)
        }, 20m);
        Equal(2, trades.Count);
        Equal(2m, trades.Single(x => x.TradeCode == "Unclassified").CostPerM2!.Value);
    }

    private static void TenderAndProgress()
    {
        var tender = new TenderEvaluationService().Evaluate(
            Requirements(),
            new[]
            {
                new TenderBid("B1", "One", "VND", new[] { new TenderQuoteLine("A", 10m), new TenderQuoteLine("B", 20m) }),
                new TenderBid("B2", "Two", "VND", new[] { new TenderQuoteLine("A", 12m), new TenderQuoteLine("B", 10m) }),
                new TenderBid("B3", "Three", "VND", new[] { new TenderQuoteLine("A", 9m) })
            });
        Equal(2, tender.Single(x => x.BidId == "B1").Rank);
        Equal(1, tender.Single(x => x.BidId == "B2").Rank);
        Equal(0, tender.Single(x => x.BidId == "B3").Rank);

        var claim = new ProgressClaimService().Evaluate(
            new[] { new ProgressContractItem("A", "m3", 10m, 100m) },
            new[] { new ProgressClaimLine("A", 8m, 4m) },
            10m);
        Equal(2m, claim.Lines[0].CertifiedThisPeriodQuantity);
        Equal(2m, claim.Lines[0].RejectedQuantity);
        Equal(180m, claim.NetCertifiedThisPeriod);
    }

    private static void TenderRevisionAndRounds()
    {
        var changes = TenderRevisionService.Compare(
            new[]
            {
                new TenderRevisionLine("A", "Concrete", "m3", 2m),
                new TenderRevisionLine("B", "Rebar", "kg", 10m)
            },
            new[]
            {
                new TenderRevisionLine("A", "Concrete changed", "m3", 2m),
                new TenderRevisionLine("C", "Formwork", "m2", 20m)
            });
        Equal(3, changes.Count);
        Equal(TenderRevisionChangeKind.Changed, changes.Single(x => x.ItemCode == "A").Kind);
        Equal(TenderRevisionChangeKind.Removed, changes.Single(x => x.ItemCode == "B").Kind);
        Equal(TenderRevisionChangeKind.Added, changes.Single(x => x.ItemCode == "C").Kind);

        var first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var second = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var rounds = new MultiRoundTenderEvaluationService().Evaluate(Requirements(), new[]
        {
            new TenderRound("R2", second, new[] { new TenderBid("B1", "One", "VND", new[] { new TenderQuoteLine("A", 9m), new TenderQuoteLine("B", 18m) }) }),
            new TenderRound("R1", first, new[] { new TenderBid("B1", "One", "VND", new[] { new TenderQuoteLine("A", 10m), new TenderQuoteLine("B", 20m) }) })
        });
        Equal("R1", rounds[0].RoundId);
        Equal("R2", rounds[1].RoundId);
        Equal(1, rounds[1].Results[0].Rank);
    }

    private static void TimePhasedCost()
    {
        var jan = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var feb = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var buckets = TimePhasedCostService.Summarize(new[]
        {
            new TimePhasedCostItem("A", jan, 100m, 80m, 70m),
            new TimePhasedCostItem("B", jan, 50m, 40m, 35m),
            new TimePhasedCostItem("A", feb, 200m, 160m, 140m)
        });
        Equal(2, buckets.Count);
        Equal(150m, buckets[0].BaselineValue);
        Equal(350m, buckets[1].CumulativeBaselineValue);
        Equal(245m, buckets[1].CumulativeCertifiedValue);
    }

    private static void IdentificationAndIssueReview()
    {
        var profile = new CadIdentificationProfile(
            true,
            BeamSizeReadMode.WidthByHeight,
            BeamEndExtensionMode.WithinTolerance,
            .02,
            new[] { new ColorClassificationRule("red-wall", 1, "Wall", 10) },
            supportsPdfTextRecognition: true,
            supportsPdfTextRestore: true);
        Equal("Wall", profile.ClassifyColor(1)!);
        Require(profile.ShouldExtendBeamEnd(.01), "gap within tolerance should extend");
        Require(!profile.ShouldExtendBeamEnd(.03), "gap outside tolerance should not extend");

        var drawing = DrawingId.New();
        var created = new DateTime(2026, 8, 15, 6, 0, 0, DateTimeKind.Utc);
        var issue = new CoordinationIssue(
            "C1",
            CoordinationIssueKind.ExactHardClash,
            CoordinationIssueSeverity.High,
            "Duct vs Beam",
            "MEP-1",
            "STR-1",
            new CadReference(drawing, new CadHandle("A")),
            new CadReference(drawing, new CadHandle("B")),
            "MEP/STRUCTURE",
            "Duct/Beam",
            "SA",
            "ZONE-A",
            0,
            created);
        issue.TransitionTo(CoordinationIssueStatus.InReview, created.AddMinutes(1));
        issue.AddComment(new CoordinationIssueComment("CM1", "tester", "confirmed", created.AddMinutes(2)));
        issue.TransitionTo(CoordinationIssueStatus.Resolved, created.AddMinutes(3));
        Equal(CoordinationIssueStatus.Resolved, issue.Status);
        Equal(1, issue.Comments.Count);
        Throws<InvalidOperationException>(() => issue.AddComment(new CoordinationIssueComment("CM1", "tester", "duplicate", created.AddMinutes(4))));
    }

    private static TenderRequirement[] Requirements() => new[]
    {
        new TenderRequirement("A", "Concrete", "m3", 2m),
        new TenderRequirement("B", "Rebar", "kg", 1m)
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }

    private static void Near(double expected, double actual, double tolerance = 1e-12)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}