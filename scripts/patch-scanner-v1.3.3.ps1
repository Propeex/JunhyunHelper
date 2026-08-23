$ErrorActionPreference = 'Stop'

function Replace-Exact([string]$Path, [string]$Old, [string]$New) {
    $text = [IO.File]::ReadAllText($Path)
    if (-not $text.Contains($Old)) { throw "Expected block not found: $Path" }
    $text = $text.Replace($Old, $New)
    [IO.File]::WriteAllText($Path, $text, [Text.UTF8Encoding]::new($false))
}

$anchor = 'src/JunhyunHelper.Desktop/Scanner/ScannerTitleAnchorRefiner.cs'
Replace-Exact $anchor @'
        var textStart = glyphs.Count > 0 ? glyphs[0].X : -1;
        var leftPadding = Math.Max(1, (int)Math.Round(panel.Width * 0.0015));
        var safeLeft = textStart >= 0
            ? Math.Max(field.Region.Width > 0 ? field.Region.X : broadLeft, textStart - leftPadding)
            : magnifier.Width > 0
                ? magnifier.X + magnifier.Width + Math.Max(2, (int)Math.Round(panel.Width * 0.003))
                : fallback.X;
'@ @'
        var textStart = glyphs.Count > 0 ? glyphs[0].X : -1;
        var leftPadding = Math.Max(1, (int)Math.Round(panel.Width * 0.0015));
        var anchorGap = Math.Max(2, (int)Math.Round(panel.Width * 0.003));
        var fallbackInset = Math.Max(2, (int)Math.Round(panel.Width * 0.006));

        // Horizontal title ownership belongs to the search-icon lane, not to the first
        // bright glyph component. Korean glyphs frequently split into several disconnected
        // pieces; treating the first surviving text-like component as the crop origin can
        // jump several syllables to the right (live v1.3.2 evidence: only "배" survived
        // from "스트라이크 담배"). The glyph envelope may refine vertical/right bounds,
        // but it must never move the title's left edge to the right of the icon/fallback lane.
        var fallbackSafeLeft = Math.Max(
            field.Region.Width > 0 ? field.Region.X : broadLeft,
            fallback.X + fallbackInset);
        fallbackSafeLeft = Math.Min(fallbackSafeLeft, Math.Max(broadLeft, broadRight - 1));

        var safeLeft = magnifier.Width > 0
            ? magnifier.X + magnifier.Width + anchorGap
            : fallbackSafeLeft;

        if (magnifier.Width > 0 && textStart >= 0)
        {
            var glyphLeft = Math.Max(
                magnifier.X + magnifier.Width + 1,
                textStart - leftPadding);
            safeLeft = Math.Min(safeLeft, glyphLeft);
        }
'@

Replace-Exact $anchor @'
            var scaleTarget = Math.Max(8.0, fieldHeight * 1.05);
            var scale = Math.Max(
                0,
                1.0 - Math.Abs(Math.Max(component.Width, component.Height) - scaleTarget) / scaleTarget);
            var square = 1.0 - Math.Min(1.0, Math.Abs(1.0 - aspect));
            var centerY = component.Y + component.Height / 2.0;
            var fieldCenterY = topBase + fieldHeight / 2.0;
            var vertical = Math.Max(
                0,
                1.0 - Math.Abs(centerY - fieldCenterY) / Math.Max(4.0, fieldHeight * 0.85));
            var expectedLeft = panel.X;
            var leftPosition = Math.Max(
                0,
                1.0 - Math.Abs(component.X - expectedLeft) /
                Math.Max(10.0, panel.Width * 0.06));
            var morphology = MagnifierMorphologyScore(bgra, stride, component);
'@ @'
            // The connected bright core of the live Tarkov magnifier is substantially
            // smaller than the full anti-aliased icon box. v1.3.2 incorrectly targeted
            // roughly one full title-field height, which penalized the real 13px bright
            // ring extracted from a ~29px header. Restrict candidates to the pre-title
            // lane first, then score the actual bright-core scale and ring topology.
            var preTitleLaneRight = fallbackTitle.X + Math.Max(2, (int)Math.Round(fieldHeight * 0.18));
            if (component.X > preTitleLaneRight)
                continue;

            var scaleTarget = Math.Max(7.0, fieldHeight * 0.50);
            var scale = Math.Max(
                0,
                1.0 - Math.Abs(Math.Max(component.Width, component.Height) - scaleTarget) / scaleTarget);
            var square = 1.0 - Math.Min(1.0, Math.Abs(1.0 - aspect));
            var centerY = component.Y + component.Height / 2.0;
            var fieldCenterY = topBase + fieldHeight / 2.0;
            var vertical = Math.Max(
                0,
                1.0 - Math.Abs(centerY - fieldCenterY) / Math.Max(4.0, fieldHeight * 0.85));
            var expectedLeft = fallbackTitle.X - Math.Max(6, (int)Math.Round(fieldHeight * 0.34));
            var leftPosition = Math.Max(
                0,
                1.0 - Math.Abs(component.X - expectedLeft) /
                Math.Max(8.0, fieldHeight * 0.90));
            var morphology = MagnifierMorphologyScore(bgra, stride, component);
'@

Replace-Exact $anchor @'
            if (hasFollowers)
            {
                if (dominance < 1.08 && morphology < 0.68)
                    continue;
            }
            else if (morphology < 0.70 || leftPosition < 0.55)
            {
                continue;
            }
'@ @'
            if (hasFollowers)
            {
                if (dominance < 1.08 && morphology < 0.58)
                    continue;
            }
            else if (morphology < 0.60 || leftPosition < 0.50)
            {
                continue;
            }
'@

Replace-Exact $anchor '        return bestScore >= 0.56 ? best : default;' '        return bestScore >= 0.52 ? best : default;'

$catalog = 'src/JunhyunHelper.Infrastructure/Scanner/ScannerCatalogService.cs'
Replace-Exact $catalog @'
    public ScannerRecognition ResolveOcrText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ScannerRecognition.Failed("EMPTY_OCR");

        var assessment = _ocrPolicy.Assess(text);
        if (!assessment.HasPlausibleVariant)
            return ScannerRecognition.Failed("OCR_INVALID_CHARACTERS");

        lock (_dataGate)
            return _matcher.Resolve(assessment.FilteredText);
    }
'@ @'
    public ScannerRecognition ResolveOcrText(string text) =>
        ResolveOcrText(text, out _);

    public ScannerRecognition ResolveOcrText(
        string text,
        out ScannerOcrTextAssessment assessment)
    {
        assessment = _ocrPolicy.Assess(text);
        if (string.IsNullOrWhiteSpace(text))
            return ScannerRecognition.Failed("EMPTY_OCR");
        if (!assessment.HasPlausibleVariant)
            return ScannerRecognition.Failed("OCR_INVALID_CHARACTERS");

        lock (_dataGate)
            return _matcher.Resolve(assessment.FilteredText);
    }
'@

$runtime = 'src/JunhyunHelper.Desktop/Scanner/ScannerRuntimeService.cs'
Replace-Exact $runtime @'
            var text = await _ocr.ReadTextAsync(candidate.TitleImage, cancellationToken);
            var recognition = _catalog.ResolveOcrText(text);
            LogCandidateAttempt(mode, index, "ORIGINAL", candidate, text, recognition);
            var result = CreateSearchResult(candidate, recognition, text, "ORIGINAL", index, 0.82, 0.18);
'@ @'
            if (!HasTrustedTitleAnchors(candidate))
            {
                var rejected = CreateAnchorFailure(candidate, index, "ORIGINAL");
                bestFailure = PickBetterFailure(bestFailure, rejected);
                continue;
            }

            var text = await _ocr.ReadTextAsync(candidate.TitleImage, cancellationToken);
            var recognition = _catalog.ResolveOcrText(text, out var assessment);
            LogCandidateAttempt(mode, index, "ORIGINAL", candidate, text, assessment.FilteredText, recognition);
            var result = CreateSearchResult(candidate, recognition, text, assessment.FilteredText, "ORIGINAL", index, 0.82, 0.18);
'@

Replace-Exact $runtime @'
                var text = await deepOcr.ReadDeepTextAsync(candidate.TitleImage, cancellationToken);
                var recognition = _catalog.ResolveOcrText(text);
                LogCandidateAttempt(mode, index, "DEEP", candidate, text, recognition);
                var result = CreateSearchResult(candidate, recognition, text, "DEEP", index, 0.86, 0.14);
'@ @'
                if (!HasTrustedTitleAnchors(candidate))
                    continue;

                var text = await deepOcr.ReadDeepTextAsync(candidate.TitleImage, cancellationToken);
                var recognition = _catalog.ResolveOcrText(text, out var assessment);
                LogCandidateAttempt(mode, index, "DEEP", candidate, text, assessment.FilteredText, recognition);
                var result = CreateSearchResult(candidate, recognition, text, assessment.FilteredText, "DEEP", index, 0.86, 0.14);
'@

Replace-Exact $runtime @'
        return bestFailure ?? new CandidateSearchResult(
            false,
            null,
            ScannerRecognition.Failed("EMPTY_OCR"),
            string.Empty,
            "NONE",
            -1,
            0);
'@ @'
        return bestFailure ?? new CandidateSearchResult(
            false,
            null,
            ScannerRecognition.Failed("EMPTY_OCR"),
            string.Empty,
            string.Empty,
            "NONE",
            -1,
            0);
'@

Replace-Exact $runtime @'
    private static CandidateSearchResult CreateSearchResult(
        ScannerInspectCandidate candidate,
        ScannerRecognition recognition,
        string text,
        string pass,
        int candidateIndex,
        double semanticWeight,
        double structuralWeight) =>
        new(
            recognition.Success,
            candidate,
            recognition,
            text,
            pass,
            candidateIndex,
            recognition.Confidence * semanticWeight + candidate.StructuralScore * structuralWeight);
'@ @'
    private static CandidateSearchResult CreateSearchResult(
        ScannerInspectCandidate candidate,
        ScannerRecognition recognition,
        string rawText,
        string matcherText,
        string pass,
        int candidateIndex,
        double semanticWeight,
        double structuralWeight) =>
        new(
            recognition.Success,
            candidate,
            recognition,
            rawText,
            matcherText,
            pass,
            candidateIndex,
            recognition.Confidence * semanticWeight +
            (candidate.StructuralScore * 0.55 + candidate.TitleAnchorScore * 0.45) * structuralWeight);

    private static CandidateSearchResult CreateAnchorFailure(
        ScannerInspectCandidate candidate,
        int candidateIndex,
        string pass) =>
        new(
            false,
            candidate,
            ScannerRecognition.Failed("TITLE_ANCHOR_INCOMPLETE"),
            string.Empty,
            string.Empty,
            pass,
            candidateIndex,
            candidate.StructuralScore * 0.05);

    private static bool HasTrustedTitleAnchors(ScannerInspectCandidate candidate) =>
        candidate.TitleImage is not null &&
        candidate.TitleBounds.Width > 0 &&
        candidate.TitleBounds.Height > 0 &&
        candidate.MagnifierBounds is { Width: > 0, Height: > 0 } &&
        candidate.CloseBounds is { Width: > 0, Height: > 0 } &&
        candidate.TitleAnchorScore >= 0.48 &&
        !string.Equals(candidate.TitleAnchorReason, "GEOMETRY_FALLBACK", StringComparison.Ordinal);
'@

Replace-Exact $runtime '        if (!string.IsNullOrWhiteSpace(candidate.OcrText) && string.IsNullOrWhiteSpace(current.OcrText))' '        if (!string.IsNullOrWhiteSpace(candidate.MatcherText) && string.IsNullOrWhiteSpace(current.MatcherText))'

Replace-Exact $runtime @'
    private static void LogCandidateAttempt(
        ScannerCaptureMode mode,
        int index,
        string pass,
        ScannerInspectCandidate candidate,
        string text,
        ScannerRecognition recognition)
'@ @'
    private static void LogCandidateAttempt(
        ScannerCaptureMode mode,
        int index,
        string pass,
        ScannerInspectCandidate candidate,
        string rawText,
        string matcherText,
        ScannerRecognition recognition)
'@
Replace-Exact $runtime '            ("ocr", text),' @'
            ("rawOcr", rawText),
            ("matcherText", matcherText),
'@

Replace-Exact $runtime @'
        ScannerRecognitionDebugStore.UpdateAnalysis(
            search.Candidate,
            search.Pass,
            search.OcrText,
            search.Recognition);
'@ @'
        ScannerRecognitionDebugStore.UpdateAnalysis(
            search.Candidate,
            search.Pass,
            search.OcrText,
            search.MatcherText,
            search.Recognition);
'@
Replace-Exact $runtime '            ("text", search.OcrText));' @'
            ("rawText", search.OcrText),
            ("matcherText", search.MatcherText));
'@

Replace-Exact $runtime @'
    private sealed record CandidateSearchResult(
        bool Success,
        ScannerInspectCandidate? Candidate,
        ScannerRecognition Recognition,
        string OcrText,
        string Pass,
        int CandidateIndex,
        double CombinedScore);
'@ @'
    private sealed record CandidateSearchResult(
        bool Success,
        ScannerInspectCandidate? Candidate,
        ScannerRecognition Recognition,
        string OcrText,
        string MatcherText,
        string Pass,
        int CandidateIndex,
        double CombinedScore);
'@

$oneShot = 'src/JunhyunHelper.Desktop/Scanner/ScannerRuntimeService.OneShot.cs'
Replace-Exact $oneShot @'
            var text = await _ocr.ReadTextAsync(candidate.TitleImage, cancellationToken);
            var recognition = _catalog.ResolveOcrText(text);
            LogCandidateAttempt(mode, index, "ONESHOT_ORIGINAL", candidate, text, recognition);
            var result = CreateSearchResult(candidate, recognition, text, "ONESHOT_ORIGINAL", index, 0.82, 0.18);
'@ @'
            if (!HasTrustedTitleAnchors(candidate))
            {
                var rejected = CreateAnchorFailure(candidate, index, "ONESHOT_ORIGINAL");
                bestFailure = PickBetterFailure(bestFailure, rejected);
                continue;
            }

            var text = await _ocr.ReadTextAsync(candidate.TitleImage, cancellationToken);
            var recognition = _catalog.ResolveOcrText(text, out var assessment);
            LogCandidateAttempt(mode, index, "ONESHOT_ORIGINAL", candidate, text, assessment.FilteredText, recognition);
            var result = CreateSearchResult(candidate, recognition, text, assessment.FilteredText, "ONESHOT_ORIGINAL", index, 0.82, 0.18);
'@
Replace-Exact $oneShot @'
                var text = await deepOcr.ReadDeepTextAsync(candidate.TitleImage, cancellationToken);
                var recognition = _catalog.ResolveOcrText(text);
                LogCandidateAttempt(mode, index, "ONESHOT_DEEP", candidate, text, recognition);
                var result = CreateSearchResult(candidate, recognition, text, "ONESHOT_DEEP", index, 0.88, 0.12);
'@ @'
                if (!HasTrustedTitleAnchors(candidate))
                    continue;

                var text = await deepOcr.ReadDeepTextAsync(candidate.TitleImage, cancellationToken);
                var recognition = _catalog.ResolveOcrText(text, out var assessment);
                LogCandidateAttempt(mode, index, "ONESHOT_DEEP", candidate, text, assessment.FilteredText, recognition);
                var result = CreateSearchResult(candidate, recognition, text, assessment.FilteredText, "ONESHOT_DEEP", index, 0.88, 0.12);
'@
Replace-Exact $oneShot @'
        return bestSuccess ?? bestFailure ?? new CandidateSearchResult(
            false,
            null,
            ScannerRecognition.Failed("EMPTY_OCR"),
            string.Empty,
            "ONESHOT_NONE",
            -1,
            0);
'@ @'
        return bestSuccess ?? bestFailure ?? new CandidateSearchResult(
            false,
            null,
            ScannerRecognition.Failed("EMPTY_OCR"),
            string.Empty,
            string.Empty,
            "ONESHOT_NONE",
            -1,
            0);
'@

$debugStore = 'src/JunhyunHelper.Desktop/Scanner/ScannerRecognitionDebugStore.cs'
Replace-Exact $debugStore @'
        string pass,
        string ocrText,
        ScannerRecognition recognition)
'@ @'
        string pass,
        string ocrText,
        string matcherText,
        ScannerRecognition recognition)
'@
Replace-Exact $debugStore '                OcrText = ocrText,' @'
                OcrText = ocrText,
                MatcherText = matcherText,
'@
Replace-Exact $debugStore @'
    string OcrText = "",
    string? CandidateName = null,
'@ @'
    string OcrText = "",
    string MatcherText = "",
    string? CandidateName = null,
'@

$debugWindow = 'src/JunhyunHelper.Desktop/Scanner/ScannerRecognitionDebugWindow.xaml.cs'
Replace-Exact $debugWindow @'
        var ocr = string.IsNullOrWhiteSpace(_frame.OcrText)
            ? "(없음)"
            : _frame.OcrText.Replace("\r", " ").Replace("\n", " / ");
        DetailText.Text =
            $"캡처: {_frame.Source} · 구조 {_frame.StructuralScore:P1} ({_frame.StructuralReason}) · " +
            $"제목 anchor {_frame.TitleAnchorScore:P1} ({_frame.TitleAnchorReason})\n" +
            $"pass: {_frame.Pass} · OCR: {ocr}\n" +
'@ @'
        var rawOcr = string.IsNullOrWhiteSpace(_frame.OcrText)
            ? "(없음)"
            : _frame.OcrText.Replace("\r", " ").Replace("\n", " / ");
        var matcherText = string.IsNullOrWhiteSpace(_frame.MatcherText)
            ? "(없음)"
            : _frame.MatcherText.Replace("\r", " ").Replace("\n", " / ");
        var magnifierState = _frame.MagnifierBounds is { Width: > 0, Height: > 0 } ? "확인" : "실패";
        var closeState = _frame.CloseBounds is { Width: > 0, Height: > 0 } ? "확인" : "실패";
        DetailText.Text =
            $"캡처: {_frame.Source} · 구조 {_frame.StructuralScore:P1} ({_frame.StructuralReason}) · " +
            $"제목 anchor {_frame.TitleAnchorScore:P1} ({_frame.TitleAnchorReason}) · 돋보기 {magnifierState} · X {closeState}\n" +
            $"pass: {_frame.Pass} · 매칭 입력: {matcherText}\n" +
            $"OCR 원본(진단용): {rawOcr}\n" +
'@

Write-Host 'Scanner v1.3.3 live-title patch applied.'
