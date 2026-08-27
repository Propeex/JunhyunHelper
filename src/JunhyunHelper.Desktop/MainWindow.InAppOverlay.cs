using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private Grid? _inAppOverlayRoot;
    private Border? _inAppOverlayCard;
    private ContentControl? _inAppOverlayContent;
    private TextBlock? _inAppOverlayTitle;
    private string? _inAppOverlayKey;
    private Window? _inAppHostedWindow;
    private IInAppOverlayDialog? _inAppOverlayAdapter;
    private TaskCompletionSource<bool?>? _inAppOverlayCompletion;

    internal Task<bool?> ToggleInAppWindowAsync(string key, Window dialog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(dialog);

        if (IsInAppOverlayOpen(key))
        {
            RequestActiveInAppOverlayDismiss();
            return Task.FromResult<bool?>(false);
        }

        if (_inAppOverlayRoot is { Visibility: Visibility.Visible })
            CloseInAppOverlay(false);

        EnsureInAppOverlayHost();
        if (dialog.Content is not UIElement content)
            throw new InvalidOperationException($"{dialog.GetType().Name} does not expose hostable UI content.");

        var adapter = dialog as IInAppOverlayDialog;
        if (adapter is not null)
            adapter.AttachInAppOverlay(CloseInAppOverlay);

        // Window-scoped resources stop participating after the content is re-parented.
        // Copy them onto the hosted root before detaching so templates/styles keep the
        // same lookup semantics inside the product-owned overlay.
        if (content is FrameworkElement contentRoot && dialog.Resources.Count > 0)
        {
            foreach (DictionaryEntry entry in dialog.Resources)
            {
                if (!contentRoot.Resources.Contains(entry.Key))
                    contentRoot.Resources[entry.Key] = entry.Value;
            }
        }

        dialog.Content = null;
        return ShowInAppOverlay(
            key,
            dialog.Title,
            content,
            dialog.Width,
            dialog.Height,
            dialog,
            adapter);
    }

    /// <summary>
    /// Hosts an existing product-owned UI element in the same MainWindow overlay shell
    /// used by window-backed editors. The caller remains responsible for temporarily
    /// detaching/re-attaching the element from its normal visual parent.
    /// </summary>
    internal Task<bool?> ShowInAppElementAsync(
        string key,
        string title,
        UIElement content,
        double preferredWidth,
        double preferredHeight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(content);

        if (IsInAppOverlayOpen(key))
        {
            RequestActiveInAppOverlayDismiss();
            return Task.FromResult<bool?>(false);
        }

        if (_inAppOverlayRoot is { Visibility: Visibility.Visible })
            CloseInAppOverlay(false);

        EnsureInAppOverlayHost();
        return ShowInAppOverlay(
            key,
            title,
            content,
            preferredWidth,
            preferredHeight,
            hostedWindow: null,
            adapter: null);
    }

    internal bool IsInAppOverlayOpen(string key) =>
        _inAppOverlayRoot is { Visibility: Visibility.Visible } &&
        string.Equals(_inAppOverlayKey, key, StringComparison.Ordinal);

    internal bool DismissInAppOverlay(string key)
    {
        if (!IsInAppOverlayOpen(key))
            return false;

        RequestActiveInAppOverlayDismiss();
        return true;
    }

    private Task<bool?> ShowInAppOverlay(
        string key,
        string title,
        UIElement content,
        double preferredWidth,
        double preferredHeight,
        Window? hostedWindow,
        IInAppOverlayDialog? adapter)
    {
        _inAppOverlayCompletion = new TaskCompletionSource<bool?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _inAppHostedWindow = hostedWindow;
        _inAppOverlayAdapter = adapter;
        _inAppOverlayKey = key;
        _inAppOverlayTitle!.Text = title;
        _inAppOverlayContent!.Content = content;

        var availableWidth = ActualWidth > 0 ? Math.Max(420, ActualWidth - 80) : 900;
        var availableHeight = ActualHeight > 0 ? Math.Max(360, ActualHeight - 80) : 760;
        _inAppOverlayCard!.Width = double.IsFinite(preferredWidth) && preferredWidth > 0
            ? Math.Min(preferredWidth, availableWidth)
            : Math.Min(680, availableWidth);
        _inAppOverlayCard.Height = double.IsFinite(preferredHeight) && preferredHeight > 0
            ? Math.Min(preferredHeight, availableHeight)
            : Math.Min(650, availableHeight);
        _inAppOverlayCard.MaxWidth = availableWidth;
        _inAppOverlayCard.MaxHeight = availableHeight;

        _inAppOverlayRoot!.Visibility = Visibility.Visible;
        _inAppOverlayRoot.Focus();
        return _inAppOverlayCompletion.Task;
    }

    private void EnsureInAppOverlayHost()
    {
        if (_inAppOverlayRoot is not null)
            return;
        if (Content is not Grid productRoot)
            throw new InvalidOperationException("MainWindow root must be a Grid to host product overlays.");

        var overlay = new Grid
        {
            Visibility = Visibility.Collapsed,
            Focusable = true,
        };
        Panel.SetZIndex(overlay, 10000);
        Grid.SetRowSpan(overlay, Math.Max(1, productRoot.RowDefinitions.Count));
        Grid.SetColumnSpan(overlay, Math.Max(1, productRoot.ColumnDefinitions.Count));

        var backdrop = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
        };
        backdrop.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            RequestActiveInAppOverlayDismiss();
        };
        overlay.Children.Add(backdrop);

        var card = new Border
        {
            Background = TryFindResource("BackgroundDarkBrush") as Brush ?? Brushes.Black,
            BorderBrush = TryFindResource("BorderBrush") as Brush ?? Brushes.DimGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0),
        };
        Panel.SetZIndex(card, 1);

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid
        {
            Background = TryFindResource("BackgroundMediumBrush") as Brush ?? Brushes.DimGray,
            Margin = new Thickness(0),
        };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new TextBlock
        {
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 12, 12, 12),
        };
        header.Children.Add(title);
        var close = new Button
        {
            Content = "✕",
            Width = 38,
            Height = 32,
            Margin = new Thickness(0, 7, 8, 7),
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
        };
        close.Click += (_, _) => RequestActiveInAppOverlayDismiss();
        Grid.SetColumn(close, 1);
        header.Children.Add(close);
        layout.Children.Add(header);

        var contentHost = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };
        Grid.SetRow(contentHost, 1);
        layout.Children.Add(contentHost);
        card.Child = layout;
        overlay.Children.Add(card);

        productRoot.Children.Add(overlay);
        _inAppOverlayRoot = overlay;
        _inAppOverlayCard = card;
        _inAppOverlayContent = contentHost;
        _inAppOverlayTitle = title;
    }

    private void RequestActiveInAppOverlayDismiss()
    {
        if (_inAppOverlayAdapter is not null)
        {
            if (!_inAppOverlayAdapter.TryDismissInAppOverlay())
                return;
            return;
        }

        CloseInAppOverlay(false);
    }

    private void CloseInAppOverlay(bool? result)
    {
        if (_inAppOverlayRoot is null || _inAppOverlayRoot.Visibility != Visibility.Visible)
            return;

        var completion = _inAppOverlayCompletion;
        _inAppOverlayContent!.Content = null;
        _inAppOverlayRoot.Visibility = Visibility.Collapsed;
        _inAppOverlayKey = null;
        _inAppHostedWindow = null;
        _inAppOverlayAdapter = null;
        _inAppOverlayCompletion = null;
        completion?.TrySetResult(result);
    }
}