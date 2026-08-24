using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Desktop.Map;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    /// <summary>
    /// Rendered WPF layout assertions for product-critical UI contracts. These checks
    /// intentionally inspect arranged controls rather than treating build success or
    /// source strings as UI validation.
    /// </summary>
    private async Task VerifyProductUiLayoutAsync()
    {
        VerifyFlexibleCandidateRenderedLayout();
        VerifyAmmoRenderedControls();
        VerifyScannerRenderedControls();
        await VerifyQuestSidebarRenderedLayoutAsync();
    }

    private void VerifyFlexibleCandidateRenderedLayout()
    {
        if (ItemsPage.Resources["FlexibleCandidateTemplate"] is not DataTemplate template)
            throw new InvalidOperationException("Flexible candidate template was not found for rendered UI smoke.");

        if (template.LoadContent() is not Button candidateButton || candidateButton.Content is not Grid rowGrid)
            throw new InvalidOperationException("Flexible candidate template did not render the expected Button/Grid row.");

        candidateButton.DataContext = new FlexibleCandidateLayoutProbe
        {
            ItemId = "ui-smoke-item",
            Name = "UI alignment probe item",
            CategoryLabel = "장비",
            OwnedFir = 12,
            OwnedNonFir = 34,
        };

        var host = new Grid
        {
            Width = 900,
            Height = 74,
        };
        host.Children.Add(candidateButton);
        host.Measure(new Size(900, 74));
        host.Arrange(new Rect(0, 0, 900, 74));
        candidateButton.ApplyTemplate();
        host.UpdateLayout();

        if (rowGrid.ActualWidth < 820)
        {
            throw new InvalidOperationException(
                $"Flexible candidate content is still centered/content-sized: gridWidth={rowGrid.ActualWidth:F1}, " +
                $"buttonWidth={candidateButton.ActualWidth:F1}.");
        }

        if (rowGrid.ColumnDefinitions.Count != 4 ||
            Math.Abs(rowGrid.ColumnDefinitions[0].ActualWidth - 52) > 0.5 ||
            Math.Abs(rowGrid.ColumnDefinitions[2].ActualWidth - 108) > 0.5 ||
            Math.Abs(rowGrid.ColumnDefinitions[3].ActualWidth - 96) > 0.5)
        {
            throw new InvalidOperationException(
                "Flexible candidate fixed icon/FIR/general lanes did not render at their canonical widths.");
        }

        var iconBorder = rowGrid.Children
            .OfType<Border>()
            .FirstOrDefault(element => Grid.GetColumn(element) == 0)
            ?? throw new InvalidOperationException("Flexible candidate icon lane was not rendered.");
        var nameStack = rowGrid.Children
            .OfType<StackPanel>()
            .FirstOrDefault(element => Grid.GetColumn(element) == 1)
            ?? throw new InvalidOperationException("Flexible candidate name lane was not rendered.");
        var firStack = rowGrid.Children
            .OfType<StackPanel>()
            .FirstOrDefault(element => Grid.GetColumn(element) == 2)
            ?? throw new InvalidOperationException("Flexible candidate FIR lane was not rendered.");
        var generalStack = rowGrid.Children
            .OfType<StackPanel>()
            .FirstOrDefault(element => Grid.GetColumn(element) == 3)
            ?? throw new InvalidOperationException("Flexible candidate general lane was not rendered.");

        var iconX = iconBorder.TranslatePoint(new Point(0, 0), rowGrid).X;
        var nameX = nameStack.TranslatePoint(new Point(0, 0), rowGrid).X;
        var firX = firStack.TranslatePoint(new Point(0, 0), rowGrid).X;
        var generalX = generalStack.TranslatePoint(new Point(0, 0), rowGrid).X;
        var firRight = firX + firStack.ActualWidth;
        var generalRight = generalX + generalStack.ActualWidth;

        if (Math.Abs(iconX) > 0.5 ||
            Math.Abs(nameX - 60) > 0.5 ||
            Math.Abs(firRight - (rowGrid.ActualWidth - 96)) > 0.75 ||
            Math.Abs(generalRight - rowGrid.ActualWidth) > 0.75)
        {
            throw new InvalidOperationException(
                $"Flexible candidate rendered axes drifted: icon={iconX:F1}, name={nameX:F1}, " +
                $"firRight={firRight:F1}, generalRight={generalRight:F1}, row={rowGrid.ActualWidth:F1}.");
        }
    }

    private void VerifyAmmoRenderedControls()
    {
        AmmoPage.UpdateLayout();

        var favorite = AmmoPage.FavoriteCaliberButton.Content as string;
        if (favorite is not ("☆" or "★"))
        {
            throw new InvalidOperationException(
                $"Ammo favorite button still contains text instead of a single star: '{favorite ?? "<null>"}'.");
        }

        if (AmmoPage.FavoriteCaliberButton.Width > 50)
            throw new InvalidOperationException("Ammo favorite star button is wider than the compact control contract.");

        var toggle = AmmoPage.ProductDetailToggleButton;
        var initial = toggle.Content as string;
        if (initial != "▼")
        {
            throw new InvalidOperationException(
                $"Expanded ammo detail handle must render ▼ only, but rendered '{initial ?? "<null>"}'.");
        }

        if (toggle.Width > 50)
            throw new InvalidOperationException("Ammo detail handle is still a text-width button instead of a compact arrow handle.");

        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        AmmoPage.UpdateLayout();
        if (toggle.Content as string != "▲" || AmmoPage.ProductDetailHost.Visibility != Visibility.Collapsed)
            throw new InvalidOperationException("Collapsed ammo detail state did not render ▲ with the detail host hidden.");

        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        AmmoPage.UpdateLayout();
        if (toggle.Content as string != "▼" || AmmoPage.ProductDetailHost.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Expanded ammo detail state did not restore ▼ with the detail host visible.");
    }

    private void VerifyScannerRenderedControls()
    {
        ScannerPlaceholder.UpdateLayout();

        if (ScannerPlaceholder.ScannerToggleButton.Content as string != "스캐너 OFF" ||
            ScannerPlaceholder.ScannerToggleButton.MinWidth < 100 ||
            ScannerPlaceholder.SettingsButton.Content as string != "설정" ||
            ScannerPlaceholder.SettingsButton.MinWidth < 80 ||
            ScannerPlaceholder.AdvancedButton.Content as string != "고급" ||
            ScannerPlaceholder.AdvancedButton.MinWidth < 80)
        {
            throw new InvalidOperationException(
                "Scanner top bar did not render the approved Scanner/Settings/Advanced controls.");
        }

        if (ScannerPlaceholder.ItemSearchBox.MinHeight < 30 ||
            ScannerPlaceholder.ActivityItems.ItemsSource is null ||
            ScannerPlaceholder.EmptyActivityText.Text != "아직 인식 기록이 없습니다.")
        {
            throw new InvalidOperationException(
                "Scanner search/log split surface did not render its product contract.");
        }

        var forbiddenLabels = new HashSet<string>(StringComparer.Ordinal)
        {
            "1회 스캔",
            "현재 결과 교정",
            "테스트 OFF",
            "아이템 목록 최신화",
            "로그 삭제",
            "회귀 테스트",
            "인식 이미지",
            "교정 데이터 내보내기",
        };
        var exposedForbidden = FindVisualDescendants<Button>(ScannerPlaceholder)
            .Select(button => button.Content as string)
            .Where(label => !string.IsNullOrWhiteSpace(label) && forbiddenLabels.Contains(label))
            .ToArray();
        if (exposedForbidden.Length > 0)
        {
            throw new InvalidOperationException(
                "Scanner normal page exposes an advanced/developer control: " +
                string.Join(", ", exposedForbidden));
        }

        var activityProbe = new ScannerActivityEntry(
            DateTimeOffset.Now,
            ScannerCaptureMode.DisplayTest,
            "들격소총",
            "돌격소총",
            0.944,
            0.721,
            true,
            "FUZZY");
        if (!activityProbe.Summary.Contains("들격소총", StringComparison.Ordinal) ||
            !activityProbe.Summary.Contains("돌격소총", StringComparison.Ordinal) ||
            !activityProbe.Summary.Contains("94.4%", StringComparison.Ordinal) ||
            activityProbe.ResultLabel != "식별 성공" ||
            !activityProbe.DetailText.Contains("유사도 기준 통과", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Scanner recognition activity does not produce the required user-readable decision summary.");
        }

        // Log clearing remains an internal bounded-retention operation. v1.6 intentionally
        // removes the user-facing clear button from the normal Scanner page.
        if (!ScannerDiagnosticLog.Clear())
            throw new InvalidOperationException("Scanner smoke could not establish an empty diagnostic baseline.");
        ScannerDiagnosticLog.Write("ocr-result", ScannerCaptureMode.DisplayTest, ("text", "로그 보존 테스트"));
        ScannerDiagnosticLog.Write(
            "match-result",
            ScannerCaptureMode.DisplayTest,
            ("success", true),
            ("reason", "EXACT"),
            ("officialName", "로그 보존 테스트"),
            ("confidence", 1d),
            ("secondScore", 0d));
        if (ScannerDiagnosticLog.GetRecentActivities().Count == 0 || !File.Exists(ScannerDiagnosticLog.Path))
            throw new InvalidOperationException("Scanner smoke could not create a diagnostic/activity record.");
        if (!ScannerDiagnosticLog.Clear())
            throw new InvalidOperationException("Scanner internal log retention cleanup failed.");

        VerifyScannerDiagnosticExportOverlay();
    }

    private static void VerifyScannerDiagnosticExportOverlay()
    {
        const int width = 180;
        const int height = 120;
        const int stride = width * 4;
        var raw = new byte[stride * height];
        for (var offset = 3; offset < raw.Length; offset += 4)
            raw[offset] = 255;
        var image = System.Windows.Media.Imaging.BitmapSource.Create(
            width, height, 96, 96, PixelFormats.Bgra32, null, raw, stride);
        image.Freeze();

        var frame = new ScannerRecognitionDebugFrame(
            image, 0, 0, "ui-smoke",
            new Rect(10, 10, 150, 90),
            new Rect(35, 28, 95, 22),
            new Rect(18, 28, 13, 13),
            new Rect(138, 24, 20, 16),
            0.9, "STRUCTURE_MATCH", 0.9, "HEADER_FRAME_LOCKED");
        var rendered = ScannerRecognitionDebugWindow.RenderDiagnosticBitmap(frame);
        var pixels = new byte[stride * height];
        rendered.CopyPixels(pixels, stride, 0);

        (byte B, byte G, byte R) Pixel(int x, int y)
        {
            var offset = y * stride + x * 4;
            return (pixels[offset], pixels[offset + 1], pixels[offset + 2]);
        }

        var green = Pixel(40, 10);
        var blue = Pixel(70, 28);
        var gold = Pixel(24, 28);
        var red = Pixel(148, 24);
        if (!(green.G > green.R + 80 && green.G > green.B + 80) ||
            !(blue.B > blue.R + 80) ||
            !(gold.R > gold.B + 100 && gold.G > gold.B + 100) ||
            !(red.R > red.G + 80 && red.R > red.B + 80))
        {
            throw new InvalidOperationException(
                $"Scanner diagnostic PNG overlay colors were not rendered: " +
                $"green={green}, blue={blue}, gold={gold}, red={red}.");
        }
    }

    private async Task VerifyQuestSidebarRenderedLayoutAsync()
    {
        var sidebar = _legacyMapQuestSidebarV2
            ?? throw new InvalidOperationException("Quest sidebar was not available for rendered UI smoke.");

        var marker = new JunhyunQuestMarkerProjectionV2(
            "ui-smoke-marked",
            "UI Marked Quest",
            "ui-smoke-objective",
            "Objective",
            "A",
            100,
            100,
            null);
        sidebar.SetState(
            "UI smoke",
            [
                new LegacyMapQuestEntryV2("ui-smoke-marked", "UI Marked Quest", [marker], true, "A"),
                new LegacyMapQuestEntryV2("ui-smoke-unmarked", "UI Unmarked Quest", [], true, null),
                new LegacyMapQuestEntryV2("ui-smoke-disabled", "UI Disabled Marker Quest", [], false, "C"),
            ]);

        await Dispatcher.InvokeAsync(sidebar.UpdateLayout);

        var toggle = FindVisualDescendants<Button>(sidebar)
            .FirstOrDefault(button => button.Content as string is "▶" or "◀")
            ?? throw new InvalidOperationException("Quest sidebar handle was not rendered.");
        if (toggle.Content as string == "▶")
        {
            toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.InvokeAsync(sidebar.UpdateLayout);
        }

        var handleX = toggle.TranslatePoint(new Point(0, 0), sidebar).X;
        var rightGap = sidebar.ActualWidth - (handleX + toggle.ActualWidth);
        if (sidebar.ActualWidth < LegacyMapQuestSidebarV2.ExpandedWidth - 1 || rightGap > 6)
        {
            throw new InvalidOperationException(
                $"Expanded Quest sidebar handle is not attached to the outside/right edge: " +
                $"sidebar={sidebar.ActualWidth:F1}, handleX={handleX:F1}, handleWidth={toggle.ActualWidth:F1}, rightGap={rightGap:F1}.");
        }

        var titles = FindVisualDescendants<TextBlock>(sidebar)
            .Where(text => text.Text is "UI Marked Quest" or "UI Unmarked Quest" or "UI Disabled Marker Quest")
            .ToArray();
        if (titles.Length != 3)
            throw new InvalidOperationException($"Quest sidebar smoke expected 3 rendered Quest titles but found {titles.Length}.");

        var xPositions = titles
            .Select(text => text.TranslatePoint(new Point(0, 0), sidebar).X)
            .ToArray();
        if (xPositions.Max() - xPositions.Min() > 0.75)
        {
            throw new InvalidOperationException(
                "Quest title starts are still content-centered instead of sharing one X axis: " +
                string.Join(", ", xPositions.Select(value => value.ToString("F1"))));
        }
    }

    private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T typed)
                yield return typed;

            foreach (var descendant in FindVisualDescendants<T>(child))
                yield return descendant;
        }
    }

    private sealed class FlexibleCandidateLayoutProbe
    {
        public required string ItemId { get; init; }
        public required string Name { get; init; }
        public required string CategoryLabel { get; init; }
        public required int OwnedFir { get; init; }
        public required int OwnedNonFir { get; init; }
    }
}
