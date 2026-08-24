from pathlib import Path

REPLACEMENTS = {
    "docs/SCANNER_TEST_PLAN.md": [
        ("durable `docs/.release-v1.6.0-status.json`", "durable `docs/.release-v1.7.0-status.json`"),
        ("v1.6 normal Scanner page **does not require a visible one-shot button**", "current normal Scanner page **does not require a visible one-shot button**"),
        ("## 14. Ground Truth correction regression — v1.6", "## 14. Ground Truth correction regression — current"),
        ("Logs remain bounded rotation. v1.6 normal Scanner page no longer requires a log-delete button.", "Logs remain bounded rotation. Current normal Scanner page does not require a log-delete button."),
        ("## 18. Scanner Product UI regression — v1.6", "## 18. Scanner Product UI regression — current"),
        ("## 21. Package / version regression — v1.6", "## 21. Package / version regression — v1.7.0"),
        ("- project version = 1.6.0\n- ProductVersion starts `1.6.0`\n- FIRST_RUN first line exactly `준현 헬퍼 v1.6.0 — Windows x64`", "- project version = 1.7.0\n- ProductVersion starts `1.7.0`\n- FIRST_RUN first line exactly `준현 헬퍼 v1.7.0 — Windows x64`"),
        ("- public latest release = v1.6.0", "- public latest release = v1.7.0"),
        ("Temporary release/verifier workflows, if created, are deleted afterward so steady-state workflow returns to `ci.yml` only.", "Temporary one-shot release/verifier workflows, if created, are deleted afterward. Steady-state workflows are the permanent `ci.yml` + immutable-release `release.yml` pair."),
        ("After v1.6 public verification:", "After v1.7.0 public verification:"),
    ],
    "docs/SCANNER.md": [
        ("계속 유지되는 v1.6.x 사용자 흐름은", "계속 유지되는 기존 사용자 흐름은"),
        ("- v1.6.0에서는 일반 Scanner surface가 아니라 `고급` 영역에서 다룬다.", "- 현재 일반 Scanner surface가 아니라 `고급` 영역에서 다룬다."),
        ("one-shot 기능은 v1.6.0에서도 유지된다.", "one-shot 기능은 현재도 유지된다."),
        ("- v1.6.0 일반 Scanner surface에는 catalog force-refresh 버튼을 노출하지 않는다.", "- 현재 일반 Scanner surface에는 catalog force-refresh 버튼을 노출하지 않는다."),
        ("## 7. Scanner 일반 UI — v1.6.0", "## 7. Scanner 일반 UI — current"),
        ("v1.6.0 일반 Scanner 설정 창은 Mini Scanner/hotkey 사용 흐름을 우선하고, 기존 substitution 설정 데이터는 schema migration에서 보존한다.", "현재 일반 Scanner 설정 창은 Mini Scanner/hotkey 사용 흐름을 우선하고, 기존 substitution 설정 데이터는 schema migration에서 보존한다."),
        ("## 20. Mini Scanner — v1.6.0", "## 20. Mini Scanner — current"),
        ("## 21. Ground Truth / correction — v1.6.0", "## 21. Ground Truth / correction — current"),
        ("v1.6.0 기본 선택 UX는 이미지 위 candidate box 직접 클릭이다.", "현재 기본 선택 UX는 이미지 위 candidate box 직접 클릭이다."),
        ("## 25. Release package contract — v1.6.0", "## 25. Release package contract — current (v1.6.0부터)"),
        ("## 26. v1.6.0 이후 작업", "## 26. Current work — LIVE GROUND TRUTH MAINTENANCE"),
        ("v1.6.0 공개 검증 후 Scanner는 live Ground Truth maintenance 단계다.", "v1.7.0 공개 검증 후 Scanner는 LIVE GROUND TRUTH MAINTENANCE 단계다."),
    ],
    "docs/SCANNER_GROUND_TRUTH.md": [
        ("## 6. v1.6 correction UX — image-first candidate selection", "## 6. Current correction UX — image-first candidate selection"),
        ("v1.6.0 기본 교정 흐름:", "현재 기본 교정 흐름:"),
        ("v1.6.0부터 `교정 데이터 관리`에서 기존 Case를 다시 열 수 있다.", "현재 `교정 데이터 관리`에서 기존 Case를 다시 열 수 있다."),
        ("v1.6 일반 Scanner page는 user-facing log-delete/developer export buttons를 노출하지 않는다.", "현재 일반 Scanner page는 user-facing log-delete/developer export buttons를 노출하지 않는다."),
        ("## 19. v1.6.0 completed scope", "## 19. Current completed scope"),
        ("## 20. Post-v1.6 development loop", "## 20. Current development loop"),
    ],
    "docs/DEVELOPER_REFERENCE.md": [
        ("- `docs/DECISION_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`\n- `docs/STATUS_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`", "- `docs/DECISION_V1.7.0_PRODUCT_COMPLETION_2026-08-24.md`\n- `docs/STATUS_V1.7.0_PRODUCT_COMPLETION_2026-08-25.md`"),
        ("- **v1.6.0**: normal Scanner surface/search, Mini Scanner ordered fields, settings schema v6, image-first correction, saved Case re-edit, stable release package naming", "- **v1.6.0**: normal Scanner surface/search, Mini Scanner ordered fields, settings schema v6, image-first correction, saved Case re-edit, stable release package naming\n- **v1.7.0**: recognition-log quick correction, Data Update transaction/completeness hardening, Scanner market baseline protection, same-ID presentation join, public release proof"),
        ("`Scanner/ScannerCoordinator.Search.cs` — v1.6 local full-item search/details", "`Scanner/ScannerCoordinator.Search.cs` — current local full-item search/details"),
        ("`Scanner/ScannerPage.xaml(.cs)` — v1.6 normal Scanner/search/log surface", "`Scanner/ScannerPage.xaml(.cs)` — current normal Scanner/search/log surface"),
        ("`Scanner/ScannerCorrectionWindow.xaml(.cs)` — v1.6 auto-fit image + direct candidate box selection + manual/none fallback", "`Scanner/ScannerCorrectionWindow.xaml(.cs)` — current auto-fit image + direct candidate box selection + manual/none fallback"),
        ("One-shot 기능은 유지하지만 v1.6 normal page에는 버튼을 두지 않는다.", "One-shot 기능은 유지하지만 current normal page에는 버튼을 두지 않는다."),
        ("v1.6 normal settings UI는 hotkey/Mini Scanner order를 우선하지만 기존 substitution data는 migration에서 보존한다.", "Current normal settings UI는 hotkey/Mini Scanner order를 우선하지만 기존 substitution data는 migration에서 보존한다."),
        ("## 9.18 v1.6 Ground Truth correction / re-edit", "## 9.18 Current Ground Truth correction / re-edit"),
        ("## 9.20 Scanner UI — v1.6", "## 9.20 Scanner UI — current"),
        ("v1.6.0 user-facing package contract:", "Current user-facing package contract (v1.6.0부터):"),
        ("Scanner/ScannerLatencyTypeAliases.cs`는 `ScannerDetectedCandidate` type alias다. v1.6 release risk를 감수해 제거할 제품 이점이 없다.", "Scanner/ScannerLatencyTypeAliases.cs`는 `ScannerDetectedCandidate` type alias다. 불필요한 release risk를 감수해 제거할 제품 이점이 없다."),
        ("Current automated suite:\n\n```text\n296 tests\n```", "Current automated suite:\n\n```text\n348 tests\n```"),
        ("v1.6 release-candidate gate:", "v1.7.0 verified release gate:"),
        ("- durable `docs/.release-v1.6.0-status.json`", "- durable `docs/.release-v1.7.0-status.json`"),
        ("Intermediate green gate CI `32700507526` passed build/296 tests/publish/Product UI/Scanner/Map/graceful shutdown before final version/package/doc changes. Latest HEAD must pass again.", "v1.7.0 exact-source/public proof completed 348 tests, Windows publish, rendered Product UI/Scanner/Map smoke, graceful shutdown, package integrity, anonymous public redownload, and public-downloaded product smoke. Current housekeeping state remains separately CI-verified."),
    ],
}

for file_name, replacements in REPLACEMENTS.items():
    path = Path(file_name)
    text = path.read_text(encoding="utf-8").replace("\r\n", "\n")
    for old, new in replacements:
        count = text.count(old)
        if count != 1:
            raise RuntimeError(f"{file_name}: expected exactly one match for {old!r}, found {count}")
        text = text.replace(old, new, 1)
    path.write_text(text, encoding="utf-8", newline="\n")

print("Normalized v1.7.0 current documentation contracts.")
