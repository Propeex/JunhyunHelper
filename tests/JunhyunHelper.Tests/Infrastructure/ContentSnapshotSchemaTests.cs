using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class ContentSnapshotSchemaTests
{
    [Fact]
    public void V12MarksOlderReadableSnapshotsForRefresh()
    {
        Assert.Equal(12, ContentSnapshotStore.CurrentSchemaVersion);
        Assert.True(ContentSnapshotStore.RequiresCurrentSchemaRefresh(
            Snapshot(schemaVersion: 10, EmptyCatalog())));
        Assert.True(ContentSnapshotStore.RequiresCurrentSchemaRefresh(
            Snapshot(schemaVersion: 11, EmptyCatalog())));
        Assert.False(ContentSnapshotStore.RequiresCurrentSchemaRefresh(
            Snapshot(schemaVersion: 12, EmptyCatalog())));
    }

    private static StoredContentSnapshot Snapshot(int schemaVersion, GameContentCatalog content) =>
        new(schemaVersion, GameMode.Regular, DateTimeOffset.UtcNow, content, []);

    private static GameContentCatalog EmptyCatalog() =>
        new([], [], [], [], [], [], []);
}
