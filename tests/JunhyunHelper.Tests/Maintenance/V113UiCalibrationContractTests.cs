using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V113UiCalibrationContractTests
{
    [Fact]
    public void Quest_items_and_hideout_attach_the_shared_inline_clear_directly_from_page_construction()
    {
        var root = FindRepositoryRoot();
        var quest = Read(root, "src", "JunhyunHelper.Desktop", "Quests", "QuestPage.xaml.cs");
        var items = Read(root, "src", "JunhyunHelper.Desktop", "Items", "ItemsPage.xaml.cs");
        var hideout = Read(root, "src", "JunhyunHelper.Desktop", "Hideout", "HideoutPage.xaml.cs");
        var smoke = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ProductContractSmoke.cs");

        foreach (var source in new[] { quest, items, hideout })
            Assert.Contains("ProductSearchClearButtonBehavior.Attach(SearchBox);", source, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(root, "src", "JunhyunHelper.Desktop", "Items", "ItemsPage.SearchClearLifecycle.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src", "JunhyunHelper.Desktop", "Hideout", "HideoutPage.SearchClearLifecycle.cs")));
        Assert.Contains("ApplyTemplate();", smoke, StringComparison.Ordinal);
        Assert.Contains("page-owned inline clear glyph", smoke, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductSearchClearButtonBehavior.Attach(searchBox)", smoke, StringComparison.Ordinal);
    }

    [Fact]
    public void Expanded_map_marker_panel_uses_available_height_and_scrolls_only_real_overflow()
    {
        var root = FindRepositoryRoot();
        var body = Read(root, "src", "JunhyunHelper.Desktop", "Map", "MapPage.JunhyunMarkerPanelBodyLayout.cs");

        Assert.Contains("var maximumPanelHeight = Math.Max(120, mapHeight - 16);", body, StringComparison.Ordinal);
        Assert.Contains("var panelHeight = maximumPanelHeight;", body, StringComparison.Ordinal);
        Assert.Contains("MapMarkersOverlay.Height = panelHeight;", body, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility = ScrollBarVisibility.Auto", body, StringComparison.Ordinal);
        Assert.Contains("ComputedVerticalScrollBarVisibility", body, StringComparison.Ordinal);
        Assert.Contains("marker-panel-uses-available-height=ok", body, StringComparison.Ordinal);
        Assert.DoesNotContain("MapMarkersContent.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity))", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Correction_image_mouse_wheel_zoom_preserves_source_pixel_coordinates()
    {
        var root = FindRepositoryRoot();
        var xaml = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerCorrectionWindow.xaml");
        var zoom = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerCorrectionWindow.Zoom.cs");
        var smoke = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.CorrectionZoomSmoke.cs");

        Assert.Contains("x:Name=\"ImageScrollViewer\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseWheel=\"ImageScrollViewer_PreviewMouseWheel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ImageCanvasHost.LayoutTransform = _correctionImageScale", zoom, StringComparison.Ordinal);
        Assert.Contains("CorrectionZoomMaximumMultiplier = 8.0", zoom, StringComparison.Ordinal);
        Assert.Contains("CorrectionImageCoordinatesRemainSourcePixelsForSmoke", zoom, StringComparison.Ordinal);
        Assert.Contains("mouse-wheel-zoom=ok", smoke, StringComparison.Ordinal);
        Assert.Contains("source-pixel-coordinates=ok", smoke, StringComparison.Ordinal);
    }

    [Fact]
    public void Correction_capture_retains_recent_analyzed_semantics_only_for_the_same_title_signature()
    {
        var root = FindRepositoryRoot();
        var store = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerRecognitionDebugStore.cs");
        var hotkey = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerCoordinator.CorrectionCapture.cs");
        var manual = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.CurrentCorrection.cs");

        Assert.Contains("CorrectionSemanticCarryWindow = TimeSpan.FromSeconds(3)", store, StringComparison.Ordinal);
        Assert.Contains("_lastAnalyzedFrame = _frame;", store, StringComparison.Ordinal);
        Assert.Contains("public static ScannerRecognitionDebugFrame? GetCorrectionSnapshot()", store, StringComparison.Ordinal);
        Assert.Contains("!string.Equals(current.TitleSignature, analyzed.TitleSignature, StringComparison.Ordinal)", store, StringComparison.Ordinal);
        Assert.Contains("age >= TimeSpan.Zero && age <= CorrectionSemanticCarryWindow", store, StringComparison.Ordinal);
        Assert.Contains("RecognitionReason = analyzed.RecognitionReason", store, StringComparison.Ordinal);
        Assert.Contains("OcrText = analyzed.OcrText", store, StringComparison.Ordinal);

        Assert.Contains("ScannerRecognitionDebugStore.GetCorrectionSnapshot()", hotkey, StringComparison.Ordinal);
        Assert.Contains("ScannerRecognitionDebugStore.GetCorrectionSnapshot()", manual, StringComparison.Ordinal);
    }

    private static string Read(string root, params string[] path) =>
        File.ReadAllText(Path.Combine([root, .. path]));

    private static string FindRepositoryRoot([CallerFilePath] string sourcePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)
            ?? throw new InvalidOperationException("Test source path is unavailable."));

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src")) &&
                Directory.Exists(Path.Combine(directory.FullName, "tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the JunhyunHelper repository root.");
    }
}
