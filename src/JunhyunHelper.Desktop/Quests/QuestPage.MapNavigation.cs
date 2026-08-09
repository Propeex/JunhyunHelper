using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Desktop.Quests;

public partial class QuestPage
{
    /// <summary>
    /// Opens a Quest from an external product surface such as the Map sidebar.
    /// Filters are normalized so the requested current Quest cannot be hidden.
    /// </summary>
    public bool FocusQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return false;

        var target = _rows.FirstOrDefault(row =>
            string.Equals(row.Entry.Quest.Id, questId, StringComparison.Ordinal));
        if (target is null)
            return false;

        _updatingFilters = true;
        try
        {
            SearchBox.Text = string.Empty;

            StatusFilter.SelectedItem = StatusFilter.Items
                .Cast<FilterOption>()
                .FirstOrDefault(option =>
                    string.Equals(
                        option.Value,
                        QuestAvailabilityState.Current.ToString(),
                        StringComparison.Ordinal))
                ?? StatusFilter.Items.Cast<FilterOption>().FirstOrDefault();

            TraderFilter.SelectedItem = TraderFilter.Items
                .Cast<FilterOption>()
                .FirstOrDefault(option => option.Value is null);
            MapFilter.SelectedItem = MapFilter.Items
                .Cast<FilterOption>()
                .FirstOrDefault(option => option.Value is null);
        }
        finally
        {
            _updatingFilters = false;
        }

        ApplyFilters();
        var visibleTarget = (QuestList.ItemsSource as IEnumerable<QuestRow>)?
            .FirstOrDefault(row => string.Equals(
                row.Entry.Quest.Id,
                questId,
                StringComparison.Ordinal));
        if (visibleTarget is null)
            return false;

        QuestList.SelectedItem = visibleTarget;
        QuestList.ScrollIntoView(visibleTarget);
        QuestList.Focus();
        return true;
    }
}
