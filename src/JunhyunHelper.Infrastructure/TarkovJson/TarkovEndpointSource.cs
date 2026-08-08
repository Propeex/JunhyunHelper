namespace JunhyunHelper.Infrastructure.TarkovJson;

public sealed record TarkovEndpointSource(
    TarkovJsonDocument BaseDocument,
    TarkovLocalization Localization);
