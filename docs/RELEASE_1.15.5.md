# 준현 헬퍼 v1.15.5 공개 릴리즈 기록

상태: **PUBLIC STABLE / VERIFIED**  
기준일: **2026-09-01 KST**

## 릴리즈 identity

```text
version/tag: v1.15.5
exact product source/tag target:
62466a957a7e32a623a0ffcfad96bfb16504f823
validated PR head:
2d9f01da32e3e80860c5a87b2d2e73bc87c31b17
merge PR: #271
release id: 380587916
published UTC: 2026-09-01T14:42:06Z
```

Draft PR #270은 구현 branch를 review-ready로 전환하는 connector 경로 문제 때문에 merge하지 않고 종료되었다. 최종 구현은 non-draft PR #271에서 동일 작업 연속선으로 검증·병합되었으며 공개 product source는 위 exact merge commit이다.

## 검증

PR final head:

```text
CI: 33516899412 — SUCCESS
Shutdown Race CI: 33516899393 — SUCCESS
Documentation Consistency: 33516899505 — SUCCESS
```

Exact main/product source:

```text
CI: 33520705401 — SUCCESS
Shutdown Race CI: 33520705533 — SUCCESS
Documentation Consistency: 33520705395 — SUCCESS
Release workflow: 33521076146 — SUCCESS
Tests: 593 passed / 0 failed / 0 skipped
```

Exact-main CI는 Windows Release/XAML build, deterministic tests, self-contained win-x64 publish, published EXE Product UI/Map/Farming Guide runtime smoke, graceful shutdown, release package/checksum 검증을 모두 통과했다.

v1.15.5 Farming Guide regression evidence에는 다음이 포함된다.

- compact raid instruction vocabulary와 실제 cross-area move/discard 표시
- same-storage-area repacking 지시 억제
- 4x4 Key-tool-like nested Workbench가 fitting일 때 양 축 scrollbar 비활성화 및 zero scrollable extent
- 장비 교체 후 displaced equipment/carrier 보존
- displaced carrier nesting 후 내부 grid 재사용
- bounded destructive retention policy
- raid baseline 대비 현재 snapshot 기반 Needed count
- 기존 source-backed filters, locks, reserved cells, dedicated-container preference, equipment superiority와 complete-equipment boundary 보존

## Exact-main Actions artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9805674187
bytes: 242,052,034
SHA-256:
6281d8f2ef0f5ab0d0b6414b6cded95852f9006d23806527c8467badb8bfc088
```

## 공개 assets

```text
Junhyun-Helper.zip
asset id: 539684740
bytes: 80,705,841
SHA-256:
32df6c471cf79349932a83a5d7598fecb8971548e4b38bb7bdab917602898d69

SHA256SUMS.txt
asset id: 539684739
bytes: 86
asset SHA-256:
683a2374431389efdc7d3176816917ef8ef466c2b493aa9bc78dfd6416be4f98
```

Exact-main packaging log의 `Junhyun-Helper.zip` SHA-256과 GitHub public asset digest가 동일하다.

## 공개 readback

- `/releases/latest` = v1.15.5
- release `target_commitish` = `62466a957a7e32a623a0ffcfad96bfb16504f823`
- lightweight `refs/tags/v1.15.5` = `62466a957a7e32a623a0ffcfad96bfb16504f823`
- `draft=false`
- `prerelease=false`
- 공개 asset 이름/ID/크기/digest = 위 기록과 일치

따라서 `62466a957a7e32a623a0ffcfad96bfb16504f823`만 v1.15.5 immutable product source다. 이후 문서-only commit은 제품 source가 아니다.

## 관련 공식 문서

- `docs/.release-v1.15.5-status.json`
- `docs/RELEASE_NOTES_V1.15.5.md`
- `docs/DECISION_V1.15.5_FARMING_GUIDE_PRESENTATION_VIEWPORT.md`
- `docs/DECISION_V1.15.5_FARMING_GUIDE_STATE_TRANSITION_PLANNER.md`
- `docs/PROJECT_STATE.json`
