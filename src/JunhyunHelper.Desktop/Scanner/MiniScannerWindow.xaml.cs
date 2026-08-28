using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

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

    private bool _positionInitialized;

    public MiniScannerWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            ApplyExtendedStyles();
            EnforceTopmost();
        };
    }

    public event Action<double, double>? PositionCommitted;

    public void Render(ScannerItemSnapshot snapshot, ScannerDisplaySettings settings, bool editMode)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(settings);
        _ = editMode;

        ApplySnapshot(snapshot, settings);
        ShowAndPosition(settings);
    }

    public void ApplySettings(ScannerDisplaySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.PositionX.HasValue && settings.PositionY.HasValue)
        {
            Left = settings.PositionX.Value;
            Top = settings.PositionY.Value;
            _positionInitialized = true;
        }
    }

    // Kept for the hidden Foundation preview API. User-facing position edit mode no
    // longer exists; the Mini Scanner is always draggable while visible.
    public void SetEditMode(bool editMode)
    {
        _ = editMode;
        ApplyExtendedStyles();
        EnforceTopmost();
    }

    public (double X, double Y) GetPosition() => (Left, Top);

    private void ApplySnapshot(ScannerItemSnapshot snapshot, ScannerDisplaySettings settings)
    {
        // Icon and item name are the fixed Mini Scanner identity header. They are never
        // hidden by user settings; missing icon data leaves the reserved icon area empty.
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
            FleaMinimumPriceText,
            settings.ShowFleaMinimumPrice && snapshot.FleaMinimumPrice.HasValue,
            snapshot.FleaMinimumPrice is { } fleaMinimum
                ? $"플리 최저  {FormatRoubles(fleaMinimum)}"
                : string.Empty,
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
            $"필요  {snapshot.CurrentNeeded.ToString("N0", CultureInfo.InvariantCulture)}",
            settings.FontSize);

        ApplyInformationOrder(settings);
    }

    private void ApplyInformationOrder(ScannerDisplaySettings settings)
    {
        var controls = new Dictionary<string, TextBlock>(StringComparer.Ordinal)
        {
            [ScannerDisplaySettings.TraderSellPriceField] = TraderPriceText,
            [ScannerDisplaySettings.FleaAveragePriceField] = FleaPriceText,
            [ScannerDisplaySettings.FleaMinimumPriceField] = FleaMinimumPriceText,
            [ScannerDisplaySettings.TraderPricePerSlotField] = TraderSlotPriceText,
            [ScannerDisplaySettings.FleaPricePerSlotField] = FleaSlotPriceText,
            [ScannerDisplaySettings.CurrentNeededField] = CurrentNeededText,
        };

        InfoStackPanel.Children.Clear();
        foreach (var key in settings.MiniScannerInfoOrder)
        {
            if (controls.Remove(key, out var control))
                InfoStackPanel.Children.Add(control);
        }

        // Defensive compatibility for malformed/older settings passed directly to the
        // window without normalization. Every known row still remains reachable.
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
