namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    public async Task<ScannerGroundTruthRegressionResult> RunGroundTruthRegressionAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _oneShotCoordinatorGate.WaitAsync(cancellationToken);
        try
        {
            var context = GetContext();
            if (context is null)
                throw new InvalidOperationException("회귀 테스트를 실행할 활성 프로필이 없습니다.");

            SetObservedContext(context);
            if (!await _catalog.EnsureLoadedAsync(context.GameMode, cancellationToken))
                throw new InvalidOperationException("현재 게임 모드의 Scanner 아이템 목록을 먼저 최신화해 주세요.");

            var resumeMode = ActiveCaptureMode;
            try
            {
                if (resumeMode is not null)
                {
                    Runtime.PublishExternalState(
                        ScannerRuntimeState.Stabilizing,
                        "Ground Truth 회귀 테스트를 위해 실시간 스캔을 잠시 멈추는 중입니다.");
                    await Runtime.PauseForOneShotAsync(cancellationToken);
                }

                var service = new ScannerGroundTruthRegressionService(
                    _ocr,
                    _catalog,
                    itemId => Presentation.CreateSnapshot(itemId));
                var result = await service.RunAsync(
                    ScannerDiagnosticDataset.RootPath,
                    cancellationToken);

                ScannerDiagnosticLog.Write(
                    "ground-truth-regression",
                    resumeMode,
                    ("reviewed", result.ReviewedCases),
                    ("executed", result.ExecutedCases),
                    ("stillCorrect", result.StillCorrect),
                    ("solved", result.Solved),
                    ("stillFailing", result.StillFailing),
                    ("regressions", result.Regressions),
                    ("errors", result.Errors),
                    ("accuracy", result.CurrentAccuracy));
                return result;
            }
            finally
            {
                if (resumeMode is not null &&
                    !_disposed &&
                    ActiveCaptureMode == resumeMode)
                {
                    await Runtime.StartAsync(resumeMode.Value, CancellationToken.None);
                }
            }
        }
        finally
        {
            _oneShotCoordinatorGate.Release();
        }
    }
}