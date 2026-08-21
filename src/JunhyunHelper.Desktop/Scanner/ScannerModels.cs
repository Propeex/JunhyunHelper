using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JunhyunHelper.Application.Items;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Desktop.Scanner;

public sealed record ScannerDataContext(
    GameMode GameMode,
    GameContentCatalog Content,
    ItemsWorkspace ItemsWorkspace);

public sealed record ScannerItemSnapshot(
    string ItemId,
    string OfficialName,
    ImageSource? Icon,
    int? TraderSellPrice,
    int? FleaAveragePrice,
    int? TraderPricePerSlot,
    int? FleaPricePerSlot,
    int Slots,
    int CurrentNeeded);

public sealed record ScannerInspectCandidate(
    Rect Bounds,
    string GeometrySignature,
    string TitleSignature,
    BitmapSource? TitleImage);

public enum ScannerRuntimeState
{
    Disabled,
    NoProfile,
    CatalogUnavailable,
    WaitingForVision,
    WaitingForInspectWindow,
    Stabilizing,
    ReadingTitle,
    ShowingItem,
    Uncertain,
    Error,
}

public sealed record ScannerRuntimeStatus(
    ScannerRuntimeState State,
    string Message,
    string? ItemId = null,
    string? OfficialName = null,
    DateTimeOffset? UpdatedAt = null)
{
    public DateTimeOffset Timestamp { get; } = UpdatedAt ?? DateTimeOffset.Now;
}
