using System.Text.Json;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class TarkovQuestAvailabilityDelayImportTests
{
    [Fact]
    public void ImportsAvailabilityDelayWindow()
    {
        var importer = new TarkovQuestImporter();
        var source = ParseTasks(
            """
            {
              "quest-a": {
                "id": "quest-a",
                "name": "Quest A",
                "availableDelaySecondsMin": 3600,
                "availableDelaySecondsMax": 32400
              }
            }
            """);

        var quest = Assert.Single(importer.Import(source, new TarkovLocalization()));

        Assert.Equal(3600, quest.AvailableDelaySecondsMin);
        Assert.Equal(32400, quest.AvailableDelaySecondsMax);
        Assert.True(quest.HasAvailabilityDelay);
    }

    [Fact]
    public void MissingDelayDefaultsToZero()
    {
        var importer = new TarkovQuestImporter();
        var source = ParseTasks(
            """
            {
              "quest-a": {
                "id": "quest-a",
                "name": "Quest A"
              }
            }
            """);

        var quest = Assert.Single(importer.Import(source, new TarkovLocalization()));

        Assert.Equal(0, quest.AvailableDelaySecondsMin);
        Assert.Equal(0, quest.AvailableDelaySecondsMax);
        Assert.False(quest.HasAvailabilityDelay);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(10, -1)]
    [InlineData(20, 10)]
    public void RejectsInvalidDelayWindow(int minimum, int maximum)
    {
        var importer = new TarkovQuestImporter();
        var source = ParseTasks(
            $$"""
            {
              "quest-a": {
                "id": "quest-a",
                "name": "Quest A",
                "availableDelaySecondsMin": {{minimum}},
                "availableDelaySecondsMax": {{maximum}}
              }
            }
            """);

        Assert.Throws<InvalidDataException>(() =>
            importer.Import(source, new TarkovLocalization()));
    }

    private static TarkovJsonDocument ParseTasks(string tasksJson)
    {
        using var document = JsonDocument.Parse(
            $$"""
            {
              "data": {
                "tasks": {{tasksJson}}
              }
            }
            """);
        return TarkovJsonDocument.Parse(document.RootElement);
    }
}
