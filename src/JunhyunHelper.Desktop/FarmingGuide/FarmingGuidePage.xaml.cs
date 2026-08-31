using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Services;
using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage : UserControl
{
    internal const double CellSize = 34d;
    private const string PresetPlaceholder = "프리셋 선택";

    private static readonly (FarmingGuideEquipmentSlot Slot, string Label, bool Fixed)[] EquipmentSlots =
    [
        (FarmingGuideEquipmentSlot.Headset, "헤드셋", false),
        (FarmingGuideEquipmentSlot.Helmet, "헬멧", false),
        (FarmingGuideEquipmentSlot.FaceCover, "얼굴", false),
        (FarmingGuideEquipmentSlot.Armband, "완장", false),
        (FarmingGuideEquipmentSlot.BodyArmor, "방탄복", false),
        (FarmingGuideEquipmentSlot.Eyewear, "안경", false),
        (FarmingGuideEquipmentSlot.PrimaryWeapon1, "무기 1", false),
        (FarmingGuideEquipmentSlot.PrimaryWeapon2, "무기 2", false),
        (FarmingGuideEquipmentSlot.Holster, "권총", false),
        (FarmingGuideEquipmentSlot.Melee, "칼 · 고정", true),
        (FarmingGuideEquipmentSlot.Dogtag, "인식표 · 고정", true),
    ];

    private readonly ObservableCollection<SearchItemViewModel> _searchResults = [];
    private readonly Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState> _equipment = [];
    private readonly List<FarmingGuideStoredItemState> _storedItems = [];
    private readonly Dictionary<string, GameItem> _itemsById = new(StringComparer.Ordinal);

    private GameContentCatalog? _content;
    private ImageCacheService? _images;
    private FarmingGuidePresetStore? _presetStore;
    private string? _profileId;
    private FarmingGuideItemState? _rig;
    private FarmingGuideItemState? _backpack;
    private FarmingGuideItemState? _secureContainer;
    private FarmingGuideFixedEquipmentState _fixedEquipment = FarmingGuideFixedEquipmentState.Empty;
    private string? _selectedPresetName;
    private bool _updatingPresetCombo;
    private int _searchGeneration;

    internal DragSession? ActiveDrag { get; set; }
    internal Border? DragGhost { get; set; }
    internal DropProbe? CurrentDropProbe { get; set; }

    public FarmingGuidePage()
    {
        InitializeComponent();
        SearchResultsList.ItemsSource = _searchResults;
        Loaded += (_, _) => RefreshAll();
    }

    public void Configure(ImageCacheService images, FarmingGuidePresetStore presetStore)
    {
        _images = images ?? throw new ArgumentNullException(nameof(images));
        _presetStore = presetStore ?? throw new ArgumentNullException(nameof(presetStore));
    }

    public void SetData(GameContentCatalog content, string profileId)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        _content = content;
        _profileId = profileId;
        _itemsById.Clear();
        foreach (var item in content.Items)
            _itemsById[item.Id] = item;

        _equipment.Clear();
        _storedItems.Clear();
        _rig = null;
        _backpack = null;
        _secureContainer = null;
        _selectedPresetName = null;

        if (_presetStore is not null)
        {
            _fixedEquipment = _presetStore.LoadFixedEquipment();
            var state = _presetStore.LoadProfile(profileId);
            ApplySnapshot(state.WorkingSnapshot);
            _selectedPresetName = state.SelectedPresetName;
        }

        RefreshPresetChoices();
        ApplySearch();
        RefreshAll();
    }

    public void SetBusy(bool busy)
    {
        IsEnabled = !busy;
    }

    private void ApplySnapshot(FarmingGuideLoadoutSnapshot snapshot)
    {
        _equipment.Clear();
        foreach (var entry in snapshot.Equipment)
        {
            if (entry.Key is FarmingGuideEquipmentSlot.Melee or FarmingGuideEquipmentSlot.Dogtag)
                continue;
            if (_itemsById.ContainsKey(entry.Value.ItemId))
                _equipment[entry.Key] = entry.Value;
        }

        _rig = ValidateSavedCarrier(snapshot.Rig, FarmingGuideStorageKind.Rig);
        _backpack = ValidateSavedCarrier(snapshot.Backpack, FarmingGuideStorageKind.Backpack);
        _secureContainer = ValidateSavedCarrier(snapshot.SecureContainer, FarmingGuideStorageKind.SecureContainer);

        _storedItems.Clear();
        foreach (var stored in snapshot.StoredItems)
        {
            if (!_itemsById.ContainsKey(stored.Item.ItemId))
                continue;
            _storedItems.Add(stored);
        }
    }

    private FarmingGuideItemState? ValidateSavedCarrier(
        FarmingGuideItemState? state,
        FarmingGuideStorageKind storage)
    {
        if (state is null || !_itemsById.TryGetValue(state.ItemId, out var item))
            return null;
        return FarmingGuideCompatibility.IsStorageCarrierCompatible(storage, item) ? state : null;
    }

    internal FarmingGuideLoadoutSnapshot BuildSnapshot()
    {
        var equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(_equipment);
        return new FarmingGuideLoadoutSnapshot(
            equipment,
            _rig,
            _backpack,
            _secureContainer,
            _storedItems.ToArray());
    }

    internal void MarkChanged(bool fixedSetting = false)
    {
        if (!fixedSetting)
        {
            _selectedPresetName = null;
            RefreshPresetChoices();
        }

        PersistWorking();
        RefreshAll();
    }

    private void PersistWorking()
    {
        if (_presetStore is null || string.IsNullOrWhiteSpace(_profileId))
            return;
        _presetStore.SaveWorking(_profileId, BuildSnapshot(), _selectedPresetName);
    }

    private void RefreshAll()
    {
        if (!IsLoaded)
            return;
        RenderEquipment();
        RenderStorage();
        RefreshSummary();
    }

    private void RefreshPresetChoices()
    {
        if (_presetStore is null || string.IsNullOrWhiteSpace(_profileId))
            return;

        var profile = _presetStore.LoadProfile(_profileId);
        var choices = new[] { PresetPlaceholder }
            .Concat(profile.Presets.Select(preset => preset.Name))
            .ToArray();

        _updatingPresetCombo = true;
        PresetComboBox.ItemsSource = choices;
        PresetComboBox.SelectedItem = _selectedPresetName is not null && choices.Contains(_selectedPresetName)
            ? _selectedPresetName
            : PresetPlaceholder;
        _updatingPresetCombo = false;
    }

    private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingPresetCombo ||
            _presetStore is null ||
            string.IsNullOrWhiteSpace(_profileId) ||
            PresetComboBox.SelectedItem is not string name ||
            string.Equals(name, PresetPlaceholder, StringComparison.Ordinal))
        {
            return;
        }

        var state = _presetStore.SelectPreset(_profileId, name);
        _selectedPresetName = state.SelectedPresetName;
        ApplySnapshot(state.WorkingSnapshot);
        RefreshPresetChoices();
        RefreshAll();
    }

    private void SavePresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_presetStore is null || string.IsNullOrWhiteSpace(_profileId))
            return;

        var dialog = new FarmingGuidePresetNameWindow
        {
            Owner = Window.GetWindow(this),
        };
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.PresetName))
            return;

        var state = _presetStore.SavePreset(_profileId, dialog.PresetName, BuildSnapshot());
        _selectedPresetName = state.SelectedPresetName;
        RefreshPresetChoices();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

    private void ApplySearch()
    {
        if (_content is null || SearchTextBox is null || SearchResultsList is null)
            return;

        var query = SearchTextBox.Text.Trim();
        var generation = ++_searchGeneration;
        _searchResults.Clear();
        if (query.Length == 0)
            return;

        var matches = _content.Items
            .Where(item => Matches(item, query))
            .OrderBy(item => DisplayName(item), StringComparer.CurrentCultureIgnoreCase)
            .Take(80)
            .Select(item => new SearchItemViewModel(item))
            .ToArray();

        foreach (var match in matches)
            _searchResults.Add(match);

        _ = LoadResultImagesAsync(generation, matches);
    }

    private async Task LoadResultImagesAsync(int generation, IReadOnlyList<SearchItemViewModel> rows)
    {
        if (_images is null)
            return;

        foreach (var row in rows)
        {
            if (generation != _searchGeneration)
                return;
            var image = await _images.LoadAsync($"item-{row.Item.Id}", row.Item.IconUrl);
            if (generation != _searchGeneration)
                return;
            row.Image = image;
        }
    }

    private static bool Matches(GameItem item, string query) =>
        Contains(item.NameKo, query) ||
        Contains(item.NameEn, query) ||
        Contains(item.ShortNameKo, query) ||
        Contains(item.ShortNameEn, query);

    private static bool Contains(string? value, string query) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    internal static string DisplayName(GameItem item) =>
        item.NameKo ?? item.NameEn ?? item.ShortNameKo ?? item.ShortNameEn ?? item.Id;

    internal GameItem? ResolveItem(FarmingGuideItemState? state) =>
        state is not null && _itemsById.TryGetValue(state.ItemId, out var item) ? item : null;

    internal GameItem? ResolveItem(string itemId) =>
        _itemsById.TryGetValue(itemId, out var item) ? item : null;

    internal IReadOnlyDictionary<string, GameItem> ItemCatalog => _itemsById;

    internal FarmingGuideItemState? GetCarrier(FarmingGuideStorageKind kind) => kind switch
    {
        FarmingGuideStorageKind.Rig => _rig,
        FarmingGuideStorageKind.Backpack => _backpack,
        FarmingGuideStorageKind.SecureContainer => _secureContainer,
        _ => null,
    };

    internal void SetCarrier(FarmingGuideStorageKind kind, FarmingGuideItemState? state)
    {
        switch (kind)
        {
            case FarmingGuideStorageKind.Rig:
                _rig = state;
                break;
            case FarmingGuideStorageKind.Backpack:
                _backpack = state;
                break;
            case FarmingGuideStorageKind.SecureContainer:
                _secureContainer = state;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    internal List<FarmingGuideStoredItemState> StoredItems => _storedItems;
    internal Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState> Equipment => _equipment;

    internal FarmingGuideItemState? GetFixed(FarmingGuideEquipmentSlot slot) => slot switch
    {
        FarmingGuideEquipmentSlot.Melee => _fixedEquipment.Melee,
        FarmingGuideEquipmentSlot.Dogtag => _fixedEquipment.Dogtag,
        _ => null,
    };

    internal void SetFixed(FarmingGuideEquipmentSlot slot, FarmingGuideItemState? value)
    {
        _fixedEquipment = slot switch
        {
            FarmingGuideEquipmentSlot.Melee => _fixedEquipment with { Melee = value },
            FarmingGuideEquipmentSlot.Dogtag => _fixedEquipment with { Dogtag = value },
            _ => _fixedEquipment,
        };
        _presetStore?.SaveFixedEquipment(_fixedEquipment);
    }

    private void RefreshSummary()
    {
        if (_content is null)
            return;

        var itemStates = EnumerateAllItems().ToArray();
        var weight = itemStates
            .Select(ResolveItem)
            .Where(static item => item is not null)
            .Sum(item => item!.WeightKg ?? 0m);

        var totalCells = StorageDefinitions()
            .SelectMany(entry => entry.Grids)
            .Sum(grid => grid.Width * grid.Height);
        var usedCells = _storedItems.Sum(stored =>
        {
            var item = ResolveItem(stored.Item);
            if (item is null)
                return 0;
            var (width, height) = FarmingGuidePlacementEngine.Footprint(
                item.Width ?? 1,
                item.Height ?? 1,
                stored.Rotated);
            return width * height;
        });

        ValueSummaryText.Text = "—";
        WeightSummaryText.Text = $"{weight:0.00} kg";
        StorageSummaryText.Text = $"{usedCells} / {totalCells}";
    }

    private IEnumerable<FarmingGuideItemState> EnumerateAllItems()
    {
        foreach (var state in _equipment.Values)
            foreach (var nested in EnumerateItemTree(state))
                yield return nested;
        foreach (var state in new[] { _rig, _backpack, _secureContainer, _fixedEquipment.Melee, _fixedEquipment.Dogtag })
        {
            if (state is null)
                continue;
            foreach (var nested in EnumerateItemTree(state))
                yield return nested;
        }
        foreach (var stored in _storedItems)
            foreach (var nested in EnumerateItemTree(stored.Item))
                yield return nested;
    }

    private static IEnumerable<FarmingGuideItemState> EnumerateItemTree(FarmingGuideItemState state)
    {
        yield return state;
        foreach (var child in state.Attachments.Values.Concat(state.ArmorPlates.Values))
        {
            if (child is null)
                continue;
            foreach (var nested in EnumerateItemTree(child))
                yield return nested;
        }
    }

    internal IReadOnlyList<StorageDefinition> StorageDefinitions()
    {
        static FarmingGuideStorageGridDefinition[] FixedGrids(int count) =>
            Enumerable.Range(0, count)
                .Select(_ => new FarmingGuideStorageGridDefinition(1, 1, FarmingGuideItemFilter.Empty))
                .ToArray();

        return
        [
            new StorageDefinition(FarmingGuideStorageKind.Pockets, "포켓", FixedGrids(4), null),
            StorageCarrierDefinition(FarmingGuideStorageKind.Rig, "리그", _rig),
            new StorageDefinition(FarmingGuideStorageKind.SpecialSlots, "특수 슬롯", FixedGrids(3), null),
            StorageCarrierDefinition(FarmingGuideStorageKind.Backpack, "가방", _backpack),
            StorageCarrierDefinition(FarmingGuideStorageKind.SecureContainer, "컨테이너", _secureContainer),
        ];
    }

    private StorageDefinition StorageCarrierDefinition(
        FarmingGuideStorageKind kind,
        string label,
        FarmingGuideItemState? carrier)
    {
        var item = ResolveItem(carrier);
        return new StorageDefinition(
            kind,
            label,
            item?.FarmingGuideData?.StorageGrids ?? [],
            carrier);
    }

    internal sealed record StorageDefinition(
        FarmingGuideStorageKind Kind,
        string Label,
        IReadOnlyList<FarmingGuideStorageGridDefinition> Grids,
        FarmingGuideItemState? Carrier);

    internal sealed class SearchItemViewModel(GameItem item) : System.ComponentModel.INotifyPropertyChanged
    {
        private ImageSource? _image;
        public GameItem Item { get; } = item;
        public string Name => DisplayName(Item);
        public string EnglishName => Item.NameEn ?? string.Empty;
        public string SizeText => $"{Math.Max(1, Item.Width ?? 1)}×{Math.Max(1, Item.Height ?? 1)}";
        public ImageSource? Image
        {
            get => _image;
            set
            {
                if (ReferenceEquals(_image, value))
                    return;
                _image = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Image)));
            }
        }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
}
