using System.Net;
using System.Text;
using System.Text.Json;
using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Content;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class WikiBallisticsEffectivenessClientTests
{
    [Fact]
    public async Task LoadsAndEnrichesVerifiedRepresentativeWikiRows()
    {
        var fixtures = new[]
        {
            new Fixture("ae-jhp", ".50 AE JHP", new AmmoArmorEffectiveness(6, 1, 0, 0, 0, 0)),
            new Fixture("ae-copper", ".50 AE Copper Solid", new AmmoArmorEffectiveness(6, 6, 6, 5, 3, 2)),
            new Fixture("blackout-whisper", ".300 Blackout Whisper", new AmmoArmorEffectiveness(6, 4, 2, 1, 0, 0)),
            new Fixture("apm", ".366 TKM AP-M", new AmmoArmorEffectiveness(6, 6, 6, 6, 5, 4)),
            new Fixture("flechette", "12/70 Flechette", new AmmoArmorEffectiveness(6, 6, 6, 5, 5, 5)),
        };

        var html = "<table>" + string.Concat(fixtures.Select((fixture, index) =>
            BuildWikiRow(
                fixture.Name,
                fixture.Rating,
                appendSuperscript: index == 0))) + "</table>";
        var json = JsonSerializer.Serialize(new { parse = new { text = html } });

        using var client = new HttpClient(new StaticResponseHandler(HttpStatusCode.OK, json));
        var sourceClient = new WikiBallisticsEffectivenessClient(client);
        var source = await sourceClient.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(source.Available);
        Assert.Equal(fixtures.Length, source.Rows.Count);

        var content = BuildContent(fixtures);
        var enriched = WikiBallisticsEffectivenessClient.Enrich(content, source);

        Assert.Equal(fixtures.Length, enriched.MatchedAmmoCount);
        Assert.Empty(enriched.Warnings);

        foreach (var fixture in fixtures)
        {
            var ammo = Assert.Single(
                enriched.Content.Ammunition,
                value => value.ItemId == fixture.ItemId);
            Assert.Equal(fixture.Rating, ammo.ArmorEffectiveness);
        }
    }

    [Fact]
    public async Task SourceFailureLeavesCoreAmmoUnchangedAndDoesNotGuess()
    {
        using var client = new HttpClient(new StaticResponseHandler(
            HttpStatusCode.ServiceUnavailable,
            "service unavailable"));
        var sourceClient = new WikiBallisticsEffectivenessClient(client);
        var source = await sourceClient.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(source.Available);
        Assert.NotEmpty(source.Warnings);

        var content = BuildContent(
        [
            new Fixture("one", "Test Round", new AmmoArmorEffectiveness(6, 6, 6, 6, 6, 6)),
        ]);
        var enriched = WikiBallisticsEffectivenessClient.Enrich(content, source);

        Assert.Same(content, enriched.Content);
        Assert.Null(Assert.Single(enriched.Content.Ammunition).ArmorEffectiveness);
    }

    [Fact]
    public async Task ConflictingRowsAreRejectedInsteadOfChoosingOne()
    {
        var first = new AmmoArmorEffectiveness(6, 6, 6, 4, 2, 1);
        var second = new AmmoArmorEffectiveness(6, 6, 6, 5, 3, 2);
        var html = "<table>" +
                   BuildWikiRow("Conflict Round", first) +
                   BuildWikiRow("Conflict Round", second) +
                   "</table>";
        var json = JsonSerializer.Serialize(new { parse = new { text = html } });

        using var client = new HttpClient(new StaticResponseHandler(HttpStatusCode.OK, json));
        var source = await new WikiBallisticsEffectivenessClient(client)
            .LoadAsync(TestContext.Current.CancellationToken);
        var content = BuildContent(
        [
            new Fixture("conflict", "Conflict Round", first),
        ]);

        var enriched = WikiBallisticsEffectivenessClient.Enrich(content, source);

        Assert.Equal(0, enriched.MatchedAmmoCount);
        Assert.Null(Assert.Single(enriched.Content.Ammunition).ArmorEffectiveness);
        Assert.Contains(enriched.Warnings, warning => warning.Contains("서로 다른", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SnapshotRoundTripPreservesOptionalRatings()
    {
        var rating = new AmmoArmorEffectiveness(6, 6, 6, 5, 3, 2);
        var content = BuildContent(
        [
            new Fixture("roundtrip", ".50 AE Copper Solid", rating),
        ]);
        content = content with
        {
            Ammo = content.Ammunition
                .Select(ammo => ammo with { ArmorEffectiveness = rating })
                .ToArray(),
        };

        var directory = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-WikiAmmo-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "content.db");
        try
        {
            var store = new ContentSnapshotStore();
            await store.WriteNewAsync(
                path,
                GameMode.Regular,
                content,
                cancellationToken: TestContext.Current.CancellationToken);
            var read = await store.ReadAsync(
                path,
                TestContext.Current.CancellationToken);

            Assert.Equal(rating, Assert.Single(read.Content.Ammunition).ArmorEffectiveness);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static GameContentCatalog BuildContent(IReadOnlyList<Fixture> fixtures)
    {
        var items = fixtures
            .Select(fixture => new GameItem(
                fixture.ItemId,
                NameKo: null,
                NameEn: fixture.Name,
                ShortNameKo: null,
                ShortNameEn: null,
                IconUrl: null,
                WikiUrl: null,
                CategoryIds: Array.Empty<string>()))
            .ToArray();

        var ammo = fixtures
            .Select(fixture => new AmmoDefinition(
                fixture.ItemId,
                "CaliberTest",
                "bullet",
                ProjectileCount: 1,
                Damage: 50,
                ArmorDamage: 40,
                PenetrationPower: 30,
                FragmentationChance: 0,
                RicochetChance: 0,
                AccuracyModifier: 0,
                RecoilModifier: 0,
                InitialSpeed: 500,
                HeavyBleedModifier: 0,
                LightBleedModifier: 0,
                Tracer: false,
                TracerColor: null,
                Acquisitions: Array.Empty<AmmoAcquisition>()))
            .ToArray();

        return new GameContentCatalog(
            items,
            Traders: [],
            Maps: [],
            Quests: [],
            QuestObjectives: [],
            QuestItemRequirements: [],
            HideoutStations: [],
            Ammo: ammo);
    }

    private static string BuildWikiRow(
        string name,
        AmmoArmorEffectiveness rating,
        bool appendSuperscript = false)
    {
        var displayName = WebUtility.HtmlEncode(name) +
                          (appendSuperscript ? "<sup>S</sup>" : string.Empty);
        var ratings = string.Concat(rating.Values.Select(value => $"<td>{value}</td>"));

        return $"<tr>" +
               $"<td>test caliber</td>" +
               $"<td><a href=\"/wiki/test\">{displayName}</a></td>" +
               $"<td>50</td><td>30</td><td>40%</td><td>500 m/s</td>" +
               ratings +
               $"</tr>";
    }

    private sealed record Fixture(
        string ItemId,
        string Name,
        AmmoArmorEffectiveness Rating);

    private sealed class StaticResponseHandler(
        HttpStatusCode statusCode,
        string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
                RequestMessage = request,
            };
            return Task.FromResult(response);
        }
    }
}
