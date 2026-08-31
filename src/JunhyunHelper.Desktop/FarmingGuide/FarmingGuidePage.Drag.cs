using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    internal enum DragOriginKind
    {
        Search,
        Equipment,
        Carrier,
        StoredItem,
        WorkbenchSlot,
    }

    internal sealed class DragSession
    {
        public required GameItem Item { get; init; }
        public required FarmingGuideItemState State { get; init; }
        public required DragOriginKind Origin { get; init; }
        public FarmingGuideEquipmentSlot? EquipmentSlot { get; init; }
        public bool FixedEquipment { get; init; }
        public FarmingGuideStorageKind? CarrierKind { get; init; }
        public string? StoredInstanceId { get; init; }
        public WorkbenchSlotKind? WorkbenchSlotKind { get; init; }
        public string? WorkbenchSlotId { get; init; }
        public Point MouseDown { get; init; }
        public bool Started { get; set; }
        public bool Rotated { get; set; }
    }

    internal sealed record DropProbe(
        object? Target,
        bool Valid,
        int X = 0,
        int Y = 0);

    private Border? _dropHighlight;
    private Border? _transientDropTarget;
    private Image? _dragGhostImage;

    private void SearchResult_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: SearchItemViewModel row })
            return;
        BeginPotentialDrag(row.Item, FarmingGuideItemState.Create(row.Item.Id), DragOriginKind.Search, e);
    }

    private void Equipment_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1 || sender is not Border { Tag: EquipmentDropTarget target })
            return;
        var state = target.Fixed ? GetFixed(target.Slot) : Equipment.GetValueOrDefault(target.Slot);
        var item = ResolveItem(state);
        if (state is null || item is null)
            return;
        BeginPotentialDrag(
            item,
            state,
            DragOriginKind.Equipment,
            e,
            equipmentSlot: target.Slot,
            fixedEquipment: target.Fixed);
    }

    private void Carrier_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1 || sender is not Border { Tag: CarrierDropTarget target })
            return;
        var state = GetCarrier(target.Kind);
        var item = ResolveItem(state);
        if (state is null || item is null)
            return;
        BeginPotentialDrag(item, state, DragOriginKind.Carrier, e, carrierKind: target.Kind);
    }

    private void PlacedItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1 || sender is not Border { Tag: PlacedItemSource source })
            return;
        var item = ResolveItem(source.Placement.Item);
        if (item is null)
            return;
        BeginPotentialDrag(
            item,
            source.Placement.Item,
            DragOriginKind.StoredItem,
            e,
            storedInstanceId: source.Placement.InstanceId,
            initialRotation: source.Placement.Rotated);
    }

    private void BeginPotentialDrag(
        GameItem item,
        FarmingGuideItemState state,
        DragOriginKind origin,
        MouseButtonEventArgs e,
        FarmingGuideEquipmentSlot? equipmentSlot = null,
        bool fixedEquipment = false,
        FarmingGuideStorageKind? carrierKind = null,
        string? storedInstanceId = null,
        bool initialRotation = false,
        WorkbenchSlotKind? workbenchSlotKind = null,
        string? workbenchSlotId = null)
    {
        ActiveDrag = new DragSession
        {
            Item = item,
            State = state,
            Origin = origin,
            EquipmentSlot = equipmentSlot,
            FixedEquipment = fixedEquipment,
            CarrierKind = carrierKind,
            StoredInstanceId = storedInstanceId,
            WorkbenchSlotKind = workbenchSlotKind,
            WorkbenchSlotId = workbenchSlotId,
            MouseDown = e.GetPosition(RootGrid),
            Rotated = initialRotation,
        };
        Focus();
        Keyboard.Focus(this);
    }

    private void Root_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (ActiveDrag is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var point = e.GetPosition(RootGrid);
        if (!ActiveDrag.Started)
        {
            if (Math.Abs(point.X - ActiveDrag.MouseDown.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(point.Y - ActiveDrag.MouseDown.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }
            StartDragVisual();
        }

        UpdateDragVisual(point);
        e.Handled = true;
    }

    private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (ActiveDrag is not { Started: true } || e.Key != Key.R)
            return;

        ActiveDrag.Rotated = !ActiveDrag.Rotated;
        UpdateGhostSize();
        UpdateDragVisual(Mouse.GetPosition(RootGrid));
        e.Handled = true;
    }

    private void Root_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ActiveDrag is null)
            return;

        if (!ActiveDrag.Started)
        {
            ActiveDrag = null;
            return;
        }

        var session = ActiveDrag;
        var releasePoint = e.GetPosition(RootGrid);
        var probe = ProbeDrop(releasePoint, session);
        if (probe?.Valid == true)
        {
            var fixedOnlyChange = session.FixedEquipment ||
                                  probe.Target is EquipmentDropTarget { Fixed: true };
            RemoveOrigin(session, destructiveCarrierRemoval: false, destructiveStoredRemoval: false);
            ApplyDrop(session, probe);
            MarkChanged(fixedOnlyChange);
        }
        else if (session.Origin != DragOriginKind.Search && IsClearlyOutsideDropArea(releasePoint))
        {
            RemoveOrigin(session, destructiveCarrierRemoval: true, destructiveStoredRemoval: true);
            MarkChanged(session.FixedEquipment);
        }

        EndDragVisual();
        e.Handled = true;
    }

    private void StartDragVisual()
    {
        if (ActiveDrag is null)
            return;
        ActiveDrag.Started = true;
        Mouse.Capture(this, CaptureMode.SubTree);

        _dragGhostImage = CreateItemImage(ActiveDrag.Item, ActiveDrag.Rotated, new Thickness(2));
        DragGhost = new Border
        {
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(2),
            BorderBrush = (Brush)FindResource("AccentBrush"),
            Background = (Brush)FindResource("BackgroundMediumBrush"),
            Opacity = 0.92,
            ClipToBounds = true,
            IsHitTestVisible = false,
            Child = _dragGhostImage,
        };
        DragOverlay.Children.Add(DragGhost);
        UpdateGhostSize();

        _dropHighlight = new Border
        {
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(3),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
        };
        DragOverlay.Children.Insert(0, _dropHighlight);
    }

    private void UpdateGhostSize()
    {
        if (ActiveDrag is null || DragGhost is null)
            return;
        var (width, height) = FarmingGuidePlacementEngine.Footprint(
            ActiveDrag.Item.Width ?? 1,
            ActiveDrag.Item.Height ?? 1,
            ActiveDrag.Rotated);
        DragGhost.Width = width * CellSize;
        DragGhost.Height = height * CellSize;
        if (_dragGhostImage is not null)
            ApplyItemImageRotation(_dragGhostImage, ActiveDrag.Rotated);
    }

    private void UpdateDragVisual(Point point)
    {
        if (ActiveDrag is null || DragGhost is null)
            return;

        Canvas.SetLeft(DragGhost, point.X + 13);
        Canvas.SetTop(DragGhost, point.Y + 13);
        CurrentDropProbe = ProbeDrop(point, ActiveDrag);
        ShowDropHighlight(CurrentDropProbe);
    }

    private DropProbe? ProbeDrop(Point rootPoint, DragSession session)
    {
        var tagged = FindDropTargetAt(rootPoint);
        if (tagged is null)
        {
            var hit = RootGrid.InputHitTest(rootPoint) as DependencyObject;
            tagged = FindTaggedAncestor(hit);
        }

        if (session.FixedEquipment)
        {
            if (tagged is EquipmentDropTarget fixedTarget)
            {
                return new DropProbe(
                    fixedTarget,
                    fixedTarget.Fixed && fixedTarget.Slot == session.EquipmentSlot);
            }
            return null;
        }

        if (tagged is GridDropTarget grid)
            return ProbeGrid(grid, session, PointInGrid(rootPoint, grid.Canvas));

        var movingStoredAggregate = session.Origin == DragOriginKind.StoredItem &&
                                    session.StoredInstanceId is { } movingId &&
                                    StoredItems.Any(item =>
                                        string.Equals(item.ParentInstanceId, movingId, StringComparison.Ordinal));
        if (movingStoredAggregate)
            return tagged is null ? null : new DropProbe(tagged, false);

        if (tagged is WorkbenchSlotDropTarget workbenchSlot)
            return new DropProbe(workbenchSlot, CanDropIntoWorkbenchSlot(workbenchSlot, session.Item));
        if (tagged is EquipmentDropTarget equipment)
            return new DropProbe(equipment, CanEquip(equipment, session.Item));
        if (tagged is CarrierDropTarget carrier)
            return new DropProbe(carrier, CanSetCarrier(carrier.Kind, session.Item, session));

        var near = FindNearbyGrid(rootPoint, 11d);
        return near is null ? null : ProbeGrid(near, session, PointInGrid(rootPoint, near.Canvas));
    }

    private DropProbe ProbeGrid(GridDropTarget target, DragSession session, Point point)
    {
        var x = Math.Clamp((int)Math.Floor(point.X / CellSize), 0, Math.Max(0, target.Width - 1));
        var y = Math.Clamp((int)Math.Floor(point.Y / CellSize), 0, Math.Max(0, target.Height - 1));
        var existing = StoredItems
            .Where(item =>
                item.GridIndex == target.GridIndex &&
                IsOnStorageSurface(item, target.Kind, target.ParentInstanceId))
            .Select(item =>
            {
                var current = ResolveItem(item.Item);
                var footprint = FarmingGuidePlacementEngine.Footprint(
                    current?.Width ?? 1,
                    current?.Height ?? 1,
                    item.Rotated);
                return new FarmingGuideGridPlacement(item.InstanceId, item.X, item.Y, footprint.Width, footprint.Height);
            })
            .ToArray();

        var movingPopulatedCarrier = session.Origin == DragOriginKind.Carrier &&
                                     session.CarrierKind is { } originKind &&
                                     StoredItems.Any(item =>
                                         item.ParentInstanceId is null && item.Storage == originKind);
        var createsCycle = session.Origin == DragOriginKind.StoredItem &&
                           session.StoredInstanceId is { } movingId &&
                           WouldCreateNestedCycle(movingId, target.ParentInstanceId);
        var valid = !movingPopulatedCarrier &&
                    !createsCycle &&
                    FarmingGuideCompatibility.FilterAllows(session.Item, target.Filter) &&
                    FarmingGuidePlacementEngine.CanPlace(
                        target.Width,
                        target.Height,
                        x,
                        y,
                        session.Item.Width ?? 1,
                        session.Item.Height ?? 1,
                        session.Rotated,
                        existing,
                        session.StoredInstanceId);
        return new DropProbe(target, valid, x, y);
    }

    private bool WouldCreateNestedCycle(string movingInstanceId, string? targetParentInstanceId)
    {
        var current = targetParentInstanceId;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (current is not null && visited.Add(current))
        {
            if (string.Equals(current, movingInstanceId, StringComparison.Ordinal))
                return true;
            current = StoredItems.FirstOrDefault(item =>
                string.Equals(item.InstanceId, current, StringComparison.Ordinal))?.ParentInstanceId;
        }
        return false;
    }

    private bool CanEquip(EquipmentDropTarget target, GameItem item)
    {
        if (!FarmingGuideCompatibility.IsEquipmentSlotCompatible(target.Slot, item))
            return false;
        if (target.Slot == FarmingGuideEquipmentSlot.BodyArmor &&
            ResolveItem(_rig)?.FarmingGuideData?.IsArmoredRig == true)
        {
            return false;
        }

        if (target.Slot == FarmingGuideEquipmentSlot.Headset &&
            Equipment.TryGetValue(FarmingGuideEquipmentSlot.Helmet, out var helmetState) &&
            ResolveItem(helmetState)?.FarmingGuideData?.BlocksHeadphones == true)
        {
            return false;
        }
        if (target.Slot == FarmingGuideEquipmentSlot.Helmet &&
            item.FarmingGuideData?.BlocksHeadphones == true &&
            Equipment.ContainsKey(FarmingGuideEquipmentSlot.Headset))
        {
            return false;
        }

        return !EnumerateCurrentlyEquippedItems(target.Slot)
            .Any(other => FarmingGuideCompatibility.ItemsConflict(item, other));
    }

    private IEnumerable<GameItem> EnumerateCurrentlyEquippedItems(FarmingGuideEquipmentSlot replacing)
    {
        foreach (var entry in Equipment)
        {
            if (entry.Key == replacing)
                continue;
            var item = ResolveItem(entry.Value);
            if (item is not null)
                yield return item;
        }
        foreach (var carrier in new[] { _rig, _backpack, _secureContainer })
        {
            var item = ResolveItem(carrier);
            if (item is not null)
                yield return item;
        }
    }

    private bool CanSetCarrier(FarmingGuideStorageKind kind, GameItem item, DragSession session)
    {
        if (!FarmingGuideCompatibility.IsStorageCarrierCompatible(kind, item))
            return false;

        var movingSameCarrier = session.Origin == DragOriginKind.Carrier &&
                                session.CarrierKind == kind;
        var targetContainsItems = StoredItems.Any(stored =>
            stored.ParentInstanceId is null && stored.Storage == kind);
        if (!FarmingGuideLoadoutPolicy.CanReplaceCarrier(movingSameCarrier, targetContainsItems))
            return false;

        if (kind == FarmingGuideStorageKind.Rig &&
            item.FarmingGuideData?.IsArmoredRig == true &&
            Equipment.ContainsKey(FarmingGuideEquipmentSlot.BodyArmor))
        {
            return false;
        }
        return !EnumerateCurrentlyEquippedItems((FarmingGuideEquipmentSlot)(-1))
            .Any(other => FarmingGuideCompatibility.ItemsConflict(item, other));
    }

    private void ApplyDrop(DragSession session, DropProbe probe)
    {
        switch (probe.Target)
        {
            case EquipmentDropTarget equipment:
                if (equipment.Fixed)
                    SetFixed(equipment.Slot, session.State);
                else
                    Equipment[equipment.Slot] = session.State;
                break;
            case CarrierDropTarget carrier:
            {
                var movingSameCarrier = session.Origin == DragOriginKind.Carrier &&
                                        session.CarrierKind == carrier.Kind;
                if (!movingSameCarrier && GetCarrier(carrier.Kind)?.ItemId != session.State.ItemId)
                    RemoveCarrierContents(carrier.Kind);
                SetCarrier(carrier.Kind, session.State);
                break;
            }
            case GridDropTarget grid:
                StoredItems.Add(new FarmingGuideStoredItemState(
                    session.StoredInstanceId ?? Guid.NewGuid().ToString("N"),
                    session.State,
                    grid.Kind,
                    grid.GridIndex,
                    probe.X,
                    probe.Y,
                    session.Rotated,
                    grid.ParentInstanceId));
                break;
            case WorkbenchSlotDropTarget slot:
                SetWorkbenchSlotState(slot.Kind, slot.SlotId, session.State);
                break;
        }
    }

    private void RemoveOrigin(
        DragSession session,
        bool destructiveCarrierRemoval,
        bool destructiveStoredRemoval)
    {
        switch (session.Origin)
        {
            case DragOriginKind.Search:
                return;
            case DragOriginKind.Equipment when session.EquipmentSlot is { } slot:
                if (session.FixedEquipment)
                    SetFixed(slot, null);
                else
                    Equipment.Remove(slot);
                return;
            case DragOriginKind.Carrier when session.CarrierKind is { } kind:
                SetCarrier(kind, null);
                if (destructiveCarrierRemoval)
                    RemoveCarrierContents(kind);
                return;
            case DragOriginKind.StoredItem when session.StoredInstanceId is { } instanceId:
                if (destructiveStoredRemoval)
                    RemoveStoredTree(instanceId);
                else
                    StoredItems.RemoveAll(item => item.InstanceId == instanceId);
                return;
            case DragOriginKind.WorkbenchSlot when
                session.WorkbenchSlotKind is { } workbenchKind &&
                session.WorkbenchSlotId is { } workbenchSlotId:
                SetWorkbenchSlotState(workbenchKind, workbenchSlotId, null);
                return;
        }
    }

    private void RemoveCarrierContents(FarmingGuideStorageKind kind)
    {
        var rootIds = StoredItems
            .Where(item => item.ParentInstanceId is null && item.Storage == kind)
            .Select(item => item.InstanceId)
            .ToArray();
        foreach (var instanceId in rootIds)
            RemoveStoredTree(instanceId);
    }

    private void RemoveStoredTree(string instanceId)
    {
        var pending = new Stack<string>();
        pending.Push(instanceId);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var child in StoredItems
                         .Where(item => string.Equals(item.ParentInstanceId, current, StringComparison.Ordinal))
                         .Select(item => item.InstanceId)
                         .ToArray())
            {
                pending.Push(child);
            }
            StoredItems.RemoveAll(item => string.Equals(item.InstanceId, current, StringComparison.Ordinal));
        }
    }

    private bool IsClearlyOutsideDropArea(Point point) =>
        FindDropTargetAt(point) is null && FindNearbyGrid(point, 11d) is null;

    private void ShowDropHighlight(DropProbe? probe)
    {
        if (_dropHighlight is null || ActiveDrag is null)
            return;

        ResetTransientDropTarget();
        if (probe?.Target is not GridDropTarget grid)
        {
            _dropHighlight.Visibility = Visibility.Collapsed;
            switch (probe?.Target)
            {
                case EquipmentDropTarget equipment:
                    SetTransientDropTarget(equipment.Border, probe.Valid);
                    break;
                case CarrierDropTarget carrier:
                    SetTransientDropTarget(carrier.Border, probe.Valid);
                    break;
                case WorkbenchSlotDropTarget workbenchSlot:
                    SetTransientDropTarget(workbenchSlot.Border, probe.Valid);
                    break;
            }
            return;
        }

        var origin = grid.Canvas.TranslatePoint(new Point(0, 0), RootGrid);
        var footprint = FarmingGuidePlacementEngine.Footprint(
            ActiveDrag.Item.Width ?? 1,
            ActiveDrag.Item.Height ?? 1,
            ActiveDrag.Rotated);
        _dropHighlight.Width = footprint.Width * CellSize;
        _dropHighlight.Height = footprint.Height * CellSize;
        _dropHighlight.BorderBrush = (Brush)FindResource(probe.Valid ? "SuccessBrush" : "DangerBrush");
        _dropHighlight.Background = new SolidColorBrush(
            probe.Valid ? Color.FromArgb(70, 76, 175, 80) : Color.FromArgb(70, 216, 91, 91));
        Canvas.SetLeft(_dropHighlight, origin.X + probe.X * CellSize);
        Canvas.SetTop(_dropHighlight, origin.Y + probe.Y * CellSize);
        _dropHighlight.Visibility = Visibility.Visible;
    }

    private void SetTransientDropTarget(Border border, bool valid)
    {
        _transientDropTarget = border;
        border.BorderBrush = (Brush)FindResource(valid ? "SuccessBrush" : "DangerBrush");
    }

    private void ResetTransientDropTarget()
    {
        if (_transientDropTarget is null)
            return;
        _transientDropTarget.BorderBrush = (Brush)FindResource("BorderBrush");
        _transientDropTarget = null;
    }

    private void EndDragVisual()
    {
        Mouse.Capture(null);
        ResetTransientDropTarget();
        ActiveDrag = null;
        CurrentDropProbe = null;
        if (DragGhost is not null)
            DragOverlay.Children.Remove(DragGhost);
        if (_dropHighlight is not null)
            DragOverlay.Children.Remove(_dropHighlight);
        DragGhost = null;
        _dragGhostImage = null;
        _dropHighlight = null;
        RenderEquipment();
        RenderStorage();
        RenderWorkbench();
    }

    private object? FindDropTargetAt(Point rootPoint)
    {
        object? candidate = null;
        foreach (var element in FindVisualChildren<FrameworkElement>(RootGrid))
        {
            if (element.Tag is not (EquipmentDropTarget or CarrierDropTarget or GridDropTarget or WorkbenchSlotDropTarget) ||
                !IsPointWithinVisibleBounds(element, rootPoint))
            {
                continue;
            }

            if (element.Tag is GridDropTarget)
                return element.Tag;
            candidate ??= element.Tag;
        }
        return candidate;
    }

    private bool IsPointWithinVisibleBounds(
        FrameworkElement element,
        Point rootPoint,
        bool requireElementBounds = true)
    {
        DependencyObject? current = element;
        while (current is not null && !ReferenceEquals(current, RootGrid))
        {
            if (current is FrameworkElement framework)
            {
                if (!framework.IsVisible || framework.ActualWidth <= 0 || framework.ActualHeight <= 0)
                    return false;

                var isElement = ReferenceEquals(framework, element);
                var clipsPoint = isElement
                    ? requireElementBounds
                    : framework.ClipToBounds || framework is ScrollViewer or ScrollContentPresenter;
                if (clipsPoint)
                {
                    Point origin;
                    try
                    {
                        origin = framework.TranslatePoint(new Point(0, 0), RootGrid);
                    }
                    catch (InvalidOperationException)
                    {
                        return false;
                    }

                    var visibleBounds = new Rect(
                        origin,
                        new Size(framework.ActualWidth, framework.ActualHeight));
                    if (!visibleBounds.Contains(rootPoint))
                        return false;
                }
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return current is not null;
    }

    private object? FindTaggedAncestor(DependencyObject? current)
    {
        while (current is not null && !ReferenceEquals(current, RootGrid))
        {
            if (current is FrameworkElement
                {
                    Tag: EquipmentDropTarget or CarrierDropTarget or GridDropTarget or WorkbenchSlotDropTarget
                } element)
            {
                return element.Tag;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private GridDropTarget? FindNearbyGrid(Point rootPoint, double tolerance)
    {
        foreach (var canvas in FindVisualChildren<Canvas>(RootGrid))
        {
            if (canvas.Tag is not GridDropTarget target ||
                !IsPointWithinVisibleBounds(canvas, rootPoint, requireElementBounds: false))
            {
                continue;
            }

            var origin = canvas.TranslatePoint(new Point(0, 0), RootGrid);
            var bounds = new Rect(origin, new Size(canvas.ActualWidth, canvas.ActualHeight));
            bounds.Inflate(tolerance, tolerance);
            if (bounds.Contains(rootPoint))
                return target;
        }
        return null;
    }

    private Point PointInGrid(Point rootPoint, Canvas canvas)
    {
        var origin = canvas.TranslatePoint(new Point(0, 0), RootGrid);
        return new Point(rootPoint.X - origin.X, rootPoint.Y - origin.Y);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
                yield return typed;
            foreach (var nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }
}
