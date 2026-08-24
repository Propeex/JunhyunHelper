# RELEASE 1.4.4 — Scanner short-title OCR and live glyph evidence hardening

상태: `PUBLIC STABLE / VERIFIED`

기준일: 2026-08-24

## 범위

v1.4.4는 새 사용자 기능을 추가하지 않는 PATCH 릴리즈다. public v1.4.3을 실제 Tarkov에서 테스트한 뒤 제출된 `ScannerDiagnostics_2026-08-24.zip`의 user-reviewed Ground Truth를 분석하여 확인한 짧은 아이템명 OCR 실패와 문자 혼동을 보완한다.

이번 변경의 핵심은 **상세보기 창/아이템명 semantic ROI를 다시 바꾸는 것이 아니라, 신뢰된 title ROI에서 OCR이 실패했을 때만 OCR 입력을 더 적절하게 만드는 것**이다.

```text
trusted semantic title ROI
→ normal Windows ko-KR OCR
→ 성공: 기존 matcher/visual path
→ 실패 + sparse trailing-background evidence
→ OCR-only tight title view
→ OCR/deep OCR 재시도
→ current official catalog validation
→ 기존 fail-closed matcher
```

Scanner 속도 최적화는 정확도 안정화 이후 별도 작업으로 유지한다.

## 실제 Ground Truth

v1.4.3 실사용 archive:

```text
전체 Case: 7
user-reviewed: 4
reviewed final: 4
program correct: 3
reviewed accuracy: 75%
reviewed error: OCR_RECOGNITION 1
capture: 2560x1600
Windows system DPI: 144
```

### Awl

Reviewed Case `case_20260824004232199_000003`:

```text
Ground Truth: Awl
pipeline: OCR_OR_PREPROCESSING_FAILED
reason: EMPTY_OCR
structural confidence: 0.972684...
structural reason: RED_X_CANDIDATE
header confidence: 0.818829...
header reason: HEADER_FRAME_LOCKED
title ROI: 985x28
```

상세창과 header는 정상적으로 잠겼고 title ROI도 올바르다. 실제 `Awl` glyph는 ROI 좌측 약 30px 범위에 존재하지만 나머지 약 955px가 어두운 빈 배경이며 original/deep Windows OCR 모두 빈 문자열을 반환했다.

따라서 이 실패는 detail detector/header/matcher 문제가 아니라 **매우 넓고 sparse한 title ROI를 OCR 엔진에 그대로 넘긴 입력 geometry 문제**로 분류한다.

### r 계열 문자

동일 archive에서 실제 WinRT OCR이 lowercase `r`을 일본어 corner bracket 형태 `「`로 읽는 패턴을 반복 확인했다.

```text
Esmarch → Esma「ch
figurine → figu「ine
```

v1.4.3 current-catalog alphabet policy는 `「`처럼 현재 공식 item-name catalog에서 identity 문자로 사용되지 않는 embedded glyph를 신뢰하지 않고 unknown-position evidence로 보존할 수 있다. 실제 reviewed Esmarch Case는 최종적으로 올바른 Item ID로 복구됐다.

따라서 v1.4.4에서도 `「 → r` 같은 전역 치환표는 추가하지 않는다.

### o / O

실사용에서 lowercase `o`가 uppercase `O`로 OCR되는 현상이 관찰됐다. 현재 matcher canonical normalization은 Latin text를 invariant lowercase로 변환하므로 `o`와 `O`는 Item ID 판정에서 이미 동일하다.

따라서 O/o 전역 치환도 추가하지 않고, case-only 차이가 canonical exact identity임을 자동 테스트로 고정한다.

## 제품 변경

### Sparse short-title OCR fallback

새 `ScannerSparseTitleCropPlanner`는 semantic ROI를 변경하지 않고 OCR-only tight view가 안전한지를 계산한다.

- BGRA title image에서 dark background luminance를 측정한다.
- 충분히 밝은 실제 glyph column evidence를 찾는다.
- far-right의 isolated low-energy noise는 마지막 glyph로 보지 않는다.
- 왼쪽 경계와 전체 높이는 항상 보존한다.
- rightmost supported ink 뒤에 title 높이에 비례한 padding을 남긴다.
- crop 결과가 충분히 큰 trailing-background 제거가 아닐 경우 적용하지 않는다.
- 실제 title ink가 field 전반에 넓게 분포하면 crop하지 않는다.

Runtime:

1. 기존 full title ROI로 normal OCR을 먼저 수행한다.
2. 결과가 비었고 sparse planner가 안전한 tight view를 증명할 때만 tight view normal OCR을 한 번 재시도한다.
3. deep OCR 단계에서도 기존 결과가 catalog resolve에 실패하고 sparse 조건이 성립하면 tight view deep OCR evidence를 추가한다.
4. 이후 기존 current-catalog matcher와 Tarkov-font visual recovery 계약을 그대로 사용한다.

`ocr-tight-title-crop` diagnostic event에 original/crop width, rightmost ink, retained ratio, foreground evidence, background/threshold를 기록한다.

### 문자 정책

- raw OCR은 engine forensic evidence이므로 원문을 보존한다.
- catalog-impossible glyph를 특정 `r`, `0`, `I`, `l` 등으로 임의 치환하지 않는다.
- unknown-glyph recovery는 current official catalog에서 정답이 유일하고 충분히 분리된 경우에만 성공한다.
- `o/O`를 포함한 Latin case-only 차이는 canonical matcher에서 동일 identity로 처리한다.
- 짧은 이름을 맞추기 위해 generic fuzzy threshold를 낮추지 않는다.

## 변경하지 않은 계약

- structural floor: `0.34`
- trusted header floor: `0.68`
- `HEADER_FRAME_LOCKED` + magnifier + close-X evidence
- one-shot max 12 candidates / continuous max 8
- Windows ko-KR OCR primary/deep
- Tarkov-font visual recovery
- current official Korean full item catalog authority
- false positive보다 miss를 선호하는 fail-closed 원칙
- OCR production field는 `item_name` 하나
- 최고 상점가 / flea avg24hPrice / slots / RequiredTotal은 Item ID 이후 `mapped_data`
- scan-time network 없음
- game memory read / DLL injection / packet interception 없음
- Scanner 속도 최적화는 이번 범위에서 제외

## 구현 및 회귀 검증

Feature PR #170 final head:

`5fda0e43844fe30ac9e5e6fed8b902804a025b68`

Feature PR #170 final CI:

`32678557783 — SUCCESS`

Feature merge:

`e4e20aa306225cdc9224bb929ef93099f1a3e3ab`

Release-prep PR #171 final CI:

`32678982028 — SUCCESS`

Exact public release source / tag:

`0c7f31e118122ffef6e5999f7a20a77d823a450d`

검증:

```text
Windows Desktop build: SUCCESS
automated tests: 283 passed / 0 failed / 0 skipped
Windows x64 self-contained single-file publish: SUCCESS
packaged Product UI + Map/Factory/MiniMap + Scanner smoke: SUCCESS
graceful shutdown + clean portable root: SUCCESS
```

추가 자동 회귀:

- Awl-like 985x28 sparse synthetic title이 trailing-background crop plan 생성
- 마지막 glyph 뒤 안전 padding 보존
- far-right isolated single-pixel noise가 crop을 무력화하지 않음
- title ink가 넓게 분포하면 no-crop
- Latin case-only OCR 차이가 canonical exact identity

사용자 screenshot bytes 자체는 source test fixture로 저장하지 않는다.

설계 결정:

- `docs/DECISION_SCANNER_SHORT_TITLE_AND_LIVE_GLYPH_EVIDENCE_2026-08-24.md`

## 공개 릴리즈 결과

v1.4.4는 2026-08-24에 public stable/latest로 공개하고 독립 검증을 완료했다.

```text
version: v1.4.4
exact source/tag: 0c7f31e118122ffef6e5999f7a20a77d823a450d
asset: Junhyun-Helper-v1.4.4-win-x64.zip
bytes: 80391895
SHA256: 64320e36ba94b6f206ef997e3d42a809c7beef2c859f4bc7f53f704f74866f40
ProductVersion: 1.4.4+0c7f31e118122ffef6e5999f7a20a77d823a450d
tests: 283 / 283
release run: 32680058795 — SUCCESS
independent public verifier: 32680422756 — SUCCESS
published at UTC: 2026-08-24T01:35:12Z
```

독립 public verifier는 인증된 빌드 산출물을 그대로 신뢰하지 않고 GitHub public latest 경로에서 ZIP과 `SHA256SUMS.txt`를 다시 내려받아 다음을 재검증했다.

- public latest가 `v1.4.4`이며 draft/prerelease가 아님
- public `v1.4.4` tag가 exact source SHA를 가리킴
- public ZIP SHA256이 `SHA256SUMS.txt`와 일치
- package root/layout 정상
- required Map database 존재
- PDB/nested archive 없음
- EXE ProductVersion exact match
- FIRST_RUN version exact match
- public-downloaded EXE Product UI + Map + Scanner smoke 성공
- 정상 Main Window close 및 graceful shutdown 성공

Durable machine-readable release status:

- `docs/.release-v1.4.4-status.json`

## 릴리즈 게이트 결과

1. release-prep PR CI success — 완료
2. exact source SHA 고정 — 완료
3. tag `v1.4.4` exact source 생성 — 완료
4. exact-source build + exactly 283 tests — 완료
5. win-x64 self-contained single-file publish — 완료
6. package root / ProductVersion / FIRST_RUN audit — 완료
7. packaged EXE Product UI + Map + Scanner smoke / graceful shutdown — 완료
8. ZIP + `SHA256SUMS.txt` — 완료
9. draft asset re-download / hash / layout / EXE smoke — 완료
10. public stable/latest publish — 완료
11. public asset re-download / hash / SHA256SUMS / layout / ProductVersion — 완료
12. public-downloaded EXE smoke — 완료
13. independent public verifier — 완료 (`32680422756 — SUCCESS`)
14. durable `docs/.release-v1.4.4-status.json` — 완료
15. one-shot release/verifier workflow 제거 — housekeeping에서 완료

## 알려진 잔여 과제

- `r`, `0`, complex Hangul을 OCR engine 수준에서 일반적으로 해결한 것은 아니다. 실제 Ground Truth와 current-catalog evidence를 이용해 fail-closed recovery를 강화한다.
- very short title의 실제 OCR 개선 효과는 v1.4.4 공개 후 동일/유사 아이템으로 live validation이 추가로 필요하다.
- 일부 historical case의 structural bottom offset 문제는 추가 Ground Truth가 필요하다.
- diagnostics `TITLE_ANCHOR_INCOMPLETE` stage classification 개선은 별도 작업이다.
- 추가 해상도/DPI/UI layout validation이 필요하다.
- generic OCR/header/matcher threshold는 추가 Ground Truth 없이 완화하지 않는다.
- Scanner speed optimization은 정확도 안정화 이후로 계속 보류한다.
