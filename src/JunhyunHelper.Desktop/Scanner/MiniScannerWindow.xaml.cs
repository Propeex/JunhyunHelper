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

    private bool _positionInitialized;

    public MiniScannerWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyExtendedStyles();
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        Cursor = Cursors.SizeAll;
    }

    public event Action<double, double>? PositionCommitted;

    public void Render(ScannerItemSnapshot snapshot, ScannerDisplaySettings settings, bool editMode)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(settings);
        _ = editMode;

        ScannerStatusText.Visibility = Visibility.Collapsed;
        ScannerStatusText.Text = string.Empty;
        ItemContentGrid.Visibility = Visibility.Visible;
        ApplyExtendedStyles();
        ApplySnapshot(snapshot, settings);
        ShowAndPosition(settings);
    }

    public void RenderStatus(string message, ScannerDisplaySettings settings, bool editMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(settings);
        _ = editMode;

        ItemContentGrid.Visibility = Visibility.Collapsed;
        ScannerStatusText.Visibility = Visibility.Visible;
        ScannerStatusText.Text = message.Trim();
        ScannerStatusText.FontSize = Math.Clamp(settings.FontSize * 0.78, 12, 22);
        ApplyExtendedStyles();
        ShowAndPosition(settings);
    }

    public void ApplySettings(ScannerDisplaySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ScannerStatusText.FontSize = Math.Clamp(settings.FontSize * 0.78, 12, 22);
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
    }

    public (double X, double Y) GetPosition() => (Left, Top);

    private void ApplySnapshot(ScannerItemSnapshot snapshot, ScannerDisplaySettings settings)
    {
        ItemIcon.Visibility = settings.ShowItemIcon && snapshot.Icon is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
        ItemIcon.Source = ItemIcon.Visibility == Visibility.Visible ? snapshot.Icon : null;

        ConfigureLine(ItemNameText, settings.ShowItemName, snapshot.OfficialName, settings.FontSize);
        ConfigureLine(
            TraderPriceText,
            settings.ShowTraderSellPrice && snapshot.TraderSellPrice.HasValue,
            snapshot.TraderSellPrice is { } trader ? $"상인  {FormatRoubles(trader)}" : string.Empty,
            settings.FontSize);
        ConfigureLine(
            FleaPriceText,
            settings.ShowFleaAveragePrice && snapshot.FleaAveragePrice.HasValue,
            snapshot.FleaAveragePrice is { } flea ? $"플리  {FormatRoubles(flea)}" : string.Empty,
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
    }

    private void ShowAndPosition(ScannerDisplaySettings settings)
    {
        if (!IsVisible)
            Show();

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

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
            return;

        try
        {
            DragMove();
            PositionCommitted?.Invoke(Left, Top);
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

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr handle, int index, int newLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr handle, int index, IntPtr newLong);
}
