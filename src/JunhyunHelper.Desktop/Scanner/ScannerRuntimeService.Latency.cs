using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerRuntimeService
{
    private (ScannerRecognition Recognition, ScannerOcrTextAssessment Assessment) ResolveCatalogTextMeasured(string text)
    {
        using var timing = ScannerLatencyTelemetry.Measure(ScannerLatencyTelemetry.CatalogMatching);
        var recognition = _catalog.ResolveOcrText(text, out var assessment);
        return (recognition, assessment);
    }
}
