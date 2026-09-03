using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Product-owned policy for recursive Tarkov item assemblies. Attachment state is a
/// tree: a weapon slot can contain a handguard which can itself contain a rail which
/// can itself contain another mod. All edit, candidate and persisted-state decisions
/// use current validated Game Content and fail closed when that structure no longer
/// proves compatibility.
/// </summary>
public static class FarmingGuideAssemblyPolicy
{
    public const int MaximumAssemblyDepth = 24;

    public static FarmingGuideItemState? Sanitize(
        FarmingGuideItemState? state,
        IReadOnlyDictionary<string, GameItem> itemCatalog)
    {
        ArgumentNullException.ThrowIfNull(itemCatalog);
        if (state is null || !itemCatalog.ContainsKey(state.ItemId))
            return null;

        var occupied = new List<GameItem>();
        return SanitizeNode(state, itemCatalog, occupied, depth: 0);
    }

    public static FarmingGuideItemState? GetNode(
        FarmingGuideItemState root,
        IReadOnlyList<string> attachmentPath)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(attachmentPath);

        var current = root;
        foreach (var slotId in attachmentPath)
        {
            if (!current.Attachments.TryGetValue(slotId, out var child) || child is null)
                return null;
            current = child;
        }
        return current;
    }

    public static FarmingGuideItemState SetAttachment(
        FarmingGuideItemState root,
        IReadOnlyList<string> ownerPath,
        string slotId,
        FarmingGuideItemState? value)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(ownerPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);
        return MutateNode(root, ownerPath, 0, owner =>
        {
            var attachments = owner.Attachments.ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal);
            attachments[slotId] = value;
            return owner with { Attachments = attachments };
        });
    }

    public static FarmingGuideItemState SetArmorPlate(
        FarmingGuideItemState root,
        IReadOnlyList<string> ownerPath,
        string slotId,
        FarmingGuideItemState? value)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(ownerPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);
        return MutateNode(root, ownerPath, 0, owner =>
        {
            var armor = owner.ArmorPlates.ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal);
            armor[slotId] = value;
            return owner with { ArmorPlates = armor };
        });
    }

    public static bool CanAttach(
        FarmingGuideItemState root,
        IReadOnlyList<string> ownerPath,
        FarmingGuideAttachmentSlotDefinition slot,
        GameItem candidate,
        IReadOnlyDictionary<string, GameItem> itemCatalog)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(ownerPath);
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(itemCatalog);

        var ownerState = GetNode(root, ownerPath);
        if (ownerState is null || !itemCatalog.TryGetValue(ownerState.ItemId, out var ownerItem))
            return false;
        if (!FarmingGuideCompatibility.FilterAllows(candidate, slot.Filters) ||
            FarmingGuideCompatibility.ItemsConflict(ownerItem, candidate))
        {
            return false;
        }

        return EnumerateStates(root)
            .Where(state => !ReferenceEquals(state, ownerState))
            .Select(state => itemCatalog.GetValueOrDefault(state.ItemId))
            .Where(static item => item is not null)
            .All(item => !FarmingGuideCompatibility.ItemsConflict(candidate, item!));
    }

    public static bool CanInstallArmorPlate(
        FarmingGuideItemState root,
        IReadOnlyList<string> ownerPath,
        FarmingGuideArmorSlotDefinition slot,
        GameItem candidate,
        IReadOnlyDictionary<string, GameItem> itemCatalog)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(ownerPath);
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(itemCatalog);

        if (slot.Locked || !slot.AllowedPlateIds.Contains(candidate.Id, StringComparer.Ordinal))
            return false;
        var ownerState = GetNode(root, ownerPath);
        if (ownerState is null || !itemCatalog.TryGetValue(ownerState.ItemId, out var ownerItem) ||
            FarmingGuideCompatibility.ItemsConflict(ownerItem, candidate))
        {
            return false;
        }

        return EnumerateStates(root)
            .Select(state => itemCatalog.GetValueOrDefault(state.ItemId))
            .Where(static item => item is not null)
            .All(item => !FarmingGuideCompatibility.ItemsConflict(candidate, item!));
    }

    public static IReadOnlyList<GameItem> CompatibleItems(
        FarmingGuideItemState root,
        IReadOnlyList<string> ownerPath,
        FarmingGuideAttachmentSlotDefinition slot,
        IReadOnlyDictionary<string, GameItem> itemCatalog)
    {
        ArgumentNullException.ThrowIfNull(itemCatalog);
        return itemCatalog.Values
            .Where(FarmingGuideSearchPolicy.IsDraggableInventoryItem)
            .Where(item => CanAttach(root, ownerPath, slot, item, itemCatalog))
            .OrderBy(static item => item.NameKo ?? item.NameEn ?? item.ShortNameKo ?? item.ShortNameEn ?? item.Id,
                StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<GameItem> CompatibleArmorPlates(
        FarmingGuideItemState root,
        IReadOnlyList<string> ownerPath,
        FarmingGuideArmorSlotDefinition slot,
        IReadOnlyDictionary<string, GameItem> itemCatalog)
    {
        ArgumentNullException.ThrowIfNull(itemCatalog);
        return itemCatalog.Values
            .Where(FarmingGuideSearchPolicy.IsDraggableInventoryItem)
            .Where(item => CanInstallArmorPlate(root, ownerPath, slot, item, itemCatalog))
            .OrderBy(static item => item.NameKo ?? item.NameEn ?? item.ShortNameKo ?? item.ShortNameEn ?? item.Id,
                StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static IEnumerable<FarmingGuideItemState> EnumerateStates(FarmingGuideItemState root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return EnumerateStatesCore(root, depth: 0);
    }

    public static string AssemblySignature(FarmingGuideItemState root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var parts = new List<string>();
        AppendSignature(root, parts, depth: 0);
        return string.Join('|', parts);
    }

    public static bool HasMissingRequiredSlots(
        FarmingGuideItemState root,
        IReadOnlyDictionary<string, GameItem> itemCatalog)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(itemCatalog);
        return HasMissingRequiredSlotsCore(root, itemCatalog, depth: 0);
    }

    private static FarmingGuideItemState SanitizeNode(
        FarmingGuideItemState state,
        IReadOnlyDictionary<string, GameItem> itemCatalog,
        List<GameItem> occupied,
        int depth)
    {
        var item = itemCatalog[state.ItemId];
        if (depth >= MaximumAssemblyDepth)
            return FarmingGuideItemState.Create(state.ItemId, state.RaidAcquired);

        occupied.Add(item);
        var attachments = new Dictionary<string, FarmingGuideItemState?>(StringComparer.Ordinal);
        var armor = new Dictionary<string, FarmingGuideItemState?>(StringComparer.Ordinal);
        var layout = item.FarmingGuideData;
        if (layout is not null)
        {
            foreach (var slot in layout.AttachmentSlots)
            {
                var childState = state.Attachments.GetValueOrDefault(slot.Id);
                if (childState is null || !itemCatalog.TryGetValue(childState.ItemId, out var childItem))
                    continue;
                if (!FarmingGuideCompatibility.FilterAllows(childItem, slot.Filters) ||
                    occupied.Any(current => FarmingGuideCompatibility.ItemsConflict(current, childItem)))
                {
                    continue;
                }

                attachments[slot.Id] = SanitizeNode(childState, itemCatalog, occupied, depth + 1);
            }

            foreach (var slot in layout.ArmorSlots.Where(static slot => !slot.Locked))
            {
                var childState = state.ArmorPlates.GetValueOrDefault(slot.Id);
                if (childState is null || !itemCatalog.TryGetValue(childState.ItemId, out var childItem))
                    continue;
                if (!slot.AllowedPlateIds.Contains(childItem.Id, StringComparer.Ordinal) ||
                    occupied.Any(current => FarmingGuideCompatibility.ItemsConflict(current, childItem)))
                {
                    continue;
                }

                armor[slot.Id] = SanitizeNode(childState, itemCatalog, occupied, depth + 1);
            }
        }

        return new FarmingGuideItemState(state.ItemId, attachments, armor)
        {
            RaidAcquired = state.RaidAcquired,
        };
    }

    private static FarmingGuideItemState MutateNode(
        FarmingGuideItemState current,
        IReadOnlyList<string> path,
        int index,
        Func<FarmingGuideItemState, FarmingGuideItemState> mutate)
    {
        if (index == path.Count)
            return mutate(current);

        var slotId = path[index];
        if (!current.Attachments.TryGetValue(slotId, out var child) || child is null)
            return current;
        var updatedChild = MutateNode(child, path, index + 1, mutate);
        if (ReferenceEquals(updatedChild, child))
            return current;

        var attachments = current.Attachments.ToDictionary(
            static entry => entry.Key,
            static entry => entry.Value,
            StringComparer.Ordinal);
        attachments[slotId] = updatedChild;
        return current with { Attachments = attachments };
    }

    private static IEnumerable<FarmingGuideItemState> EnumerateStatesCore(
        FarmingGuideItemState state,
        int depth)
    {
        yield return state;
        if (depth >= MaximumAssemblyDepth)
            yield break;
        foreach (var child in state.Attachments.Values.Concat(state.ArmorPlates.Values))
        {
            if (child is null)
                continue;
            foreach (var nested in EnumerateStatesCore(child, depth + 1))
                yield return nested;
        }
    }

    private static void AppendSignature(FarmingGuideItemState state, List<string> parts, int depth)
    {
        if (depth >= MaximumAssemblyDepth)
            return;
        parts.Add(state.ItemId);
        foreach (var entry in state.Attachments.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            if (entry.Value is null)
                continue;
            parts.Add($"A:{entry.Key}");
            AppendSignature(entry.Value, parts, depth + 1);
        }
        foreach (var entry in state.ArmorPlates.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            if (entry.Value is null)
                continue;
            parts.Add($"P:{entry.Key}");
            AppendSignature(entry.Value, parts, depth + 1);
        }
    }

    private static bool HasMissingRequiredSlotsCore(
        FarmingGuideItemState state,
        IReadOnlyDictionary<string, GameItem> itemCatalog,
        int depth)
    {
        if (depth >= MaximumAssemblyDepth || !itemCatalog.TryGetValue(state.ItemId, out var item))
            return false;
        var layout = item.FarmingGuideData;
        if (layout is null)
            return false;
        if (layout.AttachmentSlots.Any(slot => slot.Required && state.Attachments.GetValueOrDefault(slot.Id) is null))
            return true;
        return state.Attachments.Values.Concat(state.ArmorPlates.Values)
            .Where(static child => child is not null)
            .Any(child => HasMissingRequiredSlotsCore(child!, itemCatalog, depth + 1));
    }
}
