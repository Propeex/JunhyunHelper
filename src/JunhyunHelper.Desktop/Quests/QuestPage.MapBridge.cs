using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;

namespace JunhyunHelper.Desktop.Quests;

/// <summary>
/// Read-only boundary used by the Map subsystem. Quest is the only JunhyunHelper
/// product area that Map is allowed to read directly.
/// </summary>
public partial class QuestPage
{
    public GameContentCatalog? CurrentContentForMap => _content;

    public QuestWorkspace? CurrentWorkspaceForMap => _workspace;
}
