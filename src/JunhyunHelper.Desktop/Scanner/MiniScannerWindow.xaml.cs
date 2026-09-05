using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

public partial class MiniScannerWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly TimeSpan TransientStatusDuration = TimeSpan.FromSeconds(2);

    private DispatcherTimer? _transientStatusTimer;
    private bool _hideWhenTransientStatusEnds;
    private bool _positionInitialized;

    public MiniScannerWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            ApplyExtendedStyles();
            EnforceTopmost();
        };
        Closed += (_, _) => _transientStatusTimer?.Stop();
    }

    public event Action<double, double>? PositionCommitted;

    public void Render(ScannerItemSnapshot snapshot, ScannerDisplaySettings settings)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(settings);

        _hideWhenTransientStatusEnds = false;
        ItemContentPanel.Visibility = Visibility.Visible;
        ApplySnapshot(snapshot, settings);
        ShowAndPosition(settings);
    }

    public void ShowTransientStatus(string text, ScannerDisplaySettings settings, bool hasItemContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(settings);

        _hideWhenTransientStatusEnds = !hasItemContent;
        ItemContentPanel.Visibility = hasItemContent ? Visibility.Visible : Visibility.Collapsed;
        TransientStatusText.Text = text;
        TransientStatusText.FontSize = settings.FontSize;
        TransientStatusBadge.Visibility = Visibility.Visible;

        ShowAndPosition(settings);

        _transientStatusTimer ??= CreateTransientStatusTimer();
        _transientStatusTimer.Stop();
        _transientStatusTimer.Start();
    }

    public void ApplySettings(ScannerDisplaySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ApplyInformationOrder(settings);
        if (settings.PositionX.HasValue && settings.PositionY.HasValue)
        {
            Left = settings.PositionX.Value;
            Top = settings.PositionY.Value;
            _positionInitialized = true;
        }
    }


    public (double X, double Y) GetPosition() => (Left, Top);

    private DispatcherTimer CreateTransientStatusTimer()
    {
        var timer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TransientStatusDuration,
        };
        timer.Tick += (_, _) => CompleteTransientStatus();
        return timer;
    }

    private void CompleteTransientStatus()
    {
        _transientStatusTimer?.Stop();
        TransientStatusBadge.Visibility = Visibility.Collapsed;
        TransientStatusText.Text = string.Empty;

        if (!_hideWhenTransientStatusEnds)
            return;

        _hideWhenTransientStatusEnds = false;
        ItemContentPanel.Visibility = Visibility.Visible;
        Hide();
    }

    private void ApplySnapshot(ScannerItemSnapshot snapshot, ScannerDisplaySettings settings)
    {
        ItemIcon.Visibility = Visibility.Visible;
        ItemIcon.Source = snapshot.Icon;
        ConfigureLine(ItemNameText, true, snapshot.OfficialName, settings.FontSize);

        ConfigureLine(
            TraderPriceText,
            settings.ShowTraderSellPrice && snapshot.TraderSellPrice.HasValue,
            snapshot.TraderSellPrice is { } trader
                ? $"{TraderPriceLabel(snapshot)}  {FormatRoubles(trader)}"
                : string.Empty,
            settings.FontSize);
        ConfigureLine(
            FleaPriceText,
            settings.ShowFleaAveragePrice && snapshot.FleaAveragePrice.HasValue,
            snapshot.FleaAveragePrice is { } flea ? $"플리 평균  {FormatRoubles(flea)}" : string.Empty,
            settings.FontSize);
        ConfigureLine(
            TraderSlotPriceText,
            settings.ShowTraderPricePerSlot && snapshot.TraderPricePerSlot.HasValue,
            snapshot.TraderPricePerSlot is { } traderSlot ? $"상인/칸  {FormatRoubles(traderSlot)}" : string.Empty,
            settings.FontSize);
        ConfigureLine(
            FleaSlotPriceText,
            settings.ShowFleaPricePerSlot && snapshot.FleaPricePerSlot.HasValue,
            snapshot.FleaPricePerSlot is { } fleaSlot ? $"플리/칸  {FormatRoubles(fleaSlot)}" : string.Empty,
            settings.FontSize);
        ConfigureLine(
            CurrentNeededText,
            settings.ShowCurrentNeeded,
            FormatCurrentNeeded(snapshot),
            settings.FontSize);
        ConfigureLine(
            AmmoPickupText,
            settings.ShowAmmoPickup && snapshot.AmmoShouldPickUp.HasValue,
            FormatAmmoPickup(snapshot),
            settings.FontSize);

        ApplyInformationOrder(settings);
    }

    private void ApplyInformationOrder(ScannerDisplaySettings settings)
    {
        var controls = new Dictionary<string, TextBlock>(StringComparer.Ordinal)
        {
            [ScannerDisplaySettings.TraderSellPriceField] = TraderPriceText,
            [ScannerDisplaySettings.FleaAveragePriceField] = FleaPriceText,
            [ScannerDisplaySettings.TraderPricePerSlotField] = TraderSlotPriceText,
            [ScannerDisplaySettings.FleaPricePerSlotField] = FleaSlotPriceText,
            [ScannerDisplaySettings.CurrentNeededField] = CurrentNeededText,
            [ScannerDisplaySettings.AmmoPickupField] = AmmoPickupText,
        };

        InfoStackPanel.Children.Clear();
        foreach (var key in settings.MiniScannerInfoOrder)
        {
            if (controls.Remove(key, out var control))
                InfoStackPanel.Children.Add(control);
        }

        foreach (var key in ScannerDisplaySettings.DefaultInfoOrder)
        {
            if (controls.Remove(key, out var control))
                InfoStackPanel.Children.Add(control);
        }
    }

    private static string TraderPriceLabel(ScannerItemSnapshot snapshot) =>
        string.IsNullOrWhiteSpace(snapshot.BestTraderName)
            ? "상인"
            : snapshot.BestTraderName.Trim();

    private static string FormatCurrentNeeded(ScannerItemSnapshot snapshot)
    {
        var fir = Math.Max(0, snapshot.CurrentNeededFir);
        var nonFir = Math.Max(0, snapshot.CurrentNeeded - fir);
        return $"필요  {fir.ToString("N0", CultureInfo.InvariantCulture)}(인레이드) + {nonFir.ToString("N0", CultureInfo.InvariantCulture)}개";
    }

    private static string FormatAmmoPickup(ScannerItemSnapshot snapshot)
    {
        if (snapshot.AmmoShouldPickUp is not { } shouldPickUp)
            return string.Empty;

        var decision = shouldPickUp ? "주워야 함" : "안 주워도 됨";
        return string.IsNullOrWhiteSpace(snapshot.EvaluatedAmmoName)
            ? $"탄약  {decision}"
            : $"탄약  {decision} · {snapshot.EvaluatedAmmoName} 기준";
    }

    private void ShowAndPosition(ScannerDisplaySettings settings)
    {
        if (!IsVisible)
            Show();

        ApplyExtendedStyles();
        EnforceTopmost();
        UpdateLayout();
        if (!_positionInitialized)
        {
            ApplyPosition(settings);
            _positionInitialized = true;
        }
    }

    private void ApplyPosition(ScannerDisplaySettings settings)
    {
        if (settings.PositionX.HasValue && settings.PositionY.HasValue)
        {
            Left = settings.PositionX.Value;
            Top = settings.PositionY.Value;
            return;
        }

        var workArea = SystemParameters.WorkArea;
        Left = Math.Max(workArea.Left, workArea.Right - ActualWidth - 36);
        Top = workArea.Top + 110;
    }

    private void DragSurface_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        try
        {
            DragMove();
            PositionCommitted?.Invoke(Left, Top);
            EnforceTopmost();
            e.Handled = true;
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void ApplyExtendedStyles()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
            return;

        var styles = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        styles |= WsExToolWindow;
        styles |= WsExNoActivate;
        styles &= ~WsExTransparent;
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(styles));
    }

    private void EnforceTopmost()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
            return;

        Topmost = true;
        _ = SetWindowPos(
            handle,
            HwndTopmost,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
    }

    private static void ConfigureLine(
        TextBlock textBlock,
        bool visible,
        string text,
        double fontSize)
    {
        textBlock.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        textBlock.Text = visible ? text : string.Empty;
        textBlock.FontSize = fontSize;
    }

    private static string FormatRoubles(int value) =>
        value.ToString("N0", CultureInfo.InvariantCulture) + "₽";

    private static IntPtr GetWindowLongPtr(IntPtr handle, int index) =>
        IntPtr.Size == 8
            ? GetWindowLongPtr64(handle, index)
            : new IntPtr(GetWindowLong32(handle, index));

    private static IntPtr SetWindowLongPtr(IntPtr handle, int index, IntPtr newLong) =>
        IntPtr.Size == 8
            ? SetWindowLongPtr64(handle, index, newLong)
            : new IntPtr(SetWindowLong32(handle, index, newLong.ToInt32()));

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr handle, int index, int newLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr handle, int index, IntPtr newLong);
}
