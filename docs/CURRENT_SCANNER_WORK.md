# Current Scanner Work

기준일: 2026-08-24
상태: **v1.5.0 PUBLIC RELEASE / VERIFIED — LIVE GROUND TRUTH MAINTENANCE**

현재 작업은 새 Scanner 기능을 추측으로 추가하는 것이 아니라 **v1.5.0을 공식 기준선으로 실제 Tarkov 사용 Ground Truth를 축적하고, 발견되는 실패를 stage별로 교정하는 것**이다.

## 공개 기준선

```text
public stable/latest: v1.5.0
exact release source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
final PR #172 CI: 32688080850 — SUCCESS
296 tests / 0 failed / 0 skipped
release run: 32691423654 — SUCCESS
independent public verifier: 32691641614 — SUCCESS
asset: Junhyun-Helper-v1.5.0-win-x64.zip
bytes: 80,422,292
SHA-256: 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
ProductVersion: 1.5.0+6de738959740d12e6ccb81b65e50006e463eb699
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download / SHA256SUMS / package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

공식 기록:

- `docs/RELEASE_1.5.0.md`
- `docs/.release-v1.5.0-status.json`
- `docs/SCANNER.md`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`

완료된 one-shot release/verifier workflow는 제거됐으며 steady-state workflow는 `.github/workflows/ci.yml` 하나다.

## 현재 production recognition 기준선

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ red close-X + magnifier + neutral inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog sanitation / normalization
→ conservative official-catalog matching / bounded recovery
→ optional Tarkov-font visual corroboration/recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

불변 계약:

- false positive보다 miss 선호
- geometry는 proposal이며 identity proof가 아님
- structural floor `0.34`
- trusted header floor `0.68`
- magnifier + red close-X 필수
- continuous max 8 candidates
- one-shot max 12 candidates
- current official Korean Tarkov catalog가 identity authority
- production OCR field는 item-name 하나
- price/flea/slots/needed는 Item ID 이후 mapped data
- scan-time network 없음
- game memory read / DLL injection / packet interception 없음
- product default automatic global OCR forced substitution 없음

## v1.5.0에서 이미 완료된 개선

### Mapped presentation

Item ID 이후 local trusted data에서:

- 최고 non-flea trader RUB 가격
- 최고가 상인명
- flea positive `avg24hPrice`
- positive `width × height` slots
- trader/flea price per slot
- `NeededItems[itemId].RequiredTotal`

을 연결한다.

Market/dimension 일부 누락은 해당 field만 비우며 healthy Item identity를 폐기하지 않는다.

### Unified data update

상단 Game Data update가 일반 Game Content 성공 후 현재 GameMode Scanner full-item/market catalog refresh까지 orchestration한다.

Scanner 전용 `아이템 목록 최신화`는 일반 필수 절차가 아니라 고급/복구 기능이다.

### User OCR substitution

Scanner settings schema v5.

```text
raw OCR
→ enabled user substitutions (single pass)
→ catalog sanitation / normalization
→ matcher
```

- default empty
- exact user-owned rules
- raw OCR forensic evidence 별도 보존
- recursive/chained reprocessing 없음

### Candidate-first Ground Truth correction

기본 교정 흐름:

1. detail rectangle candidate
2. close-X candidate
3. magnifier candidate
4. item-name ROI candidate
5. correct item/text
6. save

Candidate ID/rank/score/geometry를 함께 저장한다.

- 정답 candidate가 없으면 manual rectangle fallback
- detector가 semantic object를 만들지 못했으면 `없음`
- Scanner 일반 화면과 Mini Scanner에서 `현재 결과 교정`으로 빠르게 접근

### Latency telemetry / 보수적 최적화

측정 stage:

- capture
- rectangle-proposal
- semantic-header
- ocr-normal
- ocr-deep
- visual-recovery
- catalog-matching
- presentation
- end-to-end

같은 active scan cycle에서 exact pixel bitmap이 동일할 때만 WinRT OCR 결과를 재사용한다. Cross-frame OCR cache는 없다.

### Continuous result stabilization

이미 semantic validation을 통과한 같은 detail을 보고 있는 동안 harmless dark-background/GPU pixel variation으로 trusted result가 불필요하게 깜빡이지 않게 title-ink shape continuity signature를 사용한다.

이 signature는 Item identity proof가 아니다. 다른 title/geometry evidence에서는 stale result를 즉시 폐기한다.

### Retention

Reviewed Ground Truth는 자동 삭제하지 않는다.

Automatic unreviewed diagnostic sample만:

```text
max age = 30 days
max cases = 300
max bytes = 512 MiB
recent protection = 2 hours
```

으로 제한한다. Corrupt/unknown metadata는 fail closed하여 보존한다.

## 현재 Scanner UI

일반 surface:

- Scanner ON/OFF
- `1회 스캔`
- `현재 결과 교정`
- runtime status
- recent recognition history

`설정`:

- global hotkeys
- OCR substitutions
- Mini Scanner display options

`고급 / 진단`:

- Display Test
- 인식 이미지
- regression
- Ground Truth export/manage
- `아이템 목록 최신화` recovery action
- 로그 삭제
- diagnostic storage information

## 현재 실제 작업

### 1. Reviewed Ground Truth 축적

권장 실사용 흐름:

```text
v1.5.0 실제 Tarkov 사용
→ 정상 대표 결과면 `현재 결과 교정` → 맞음
→ miss/wrong identity면 즉시 `현재 결과 교정`
→ detector candidate / 영역 / 정답 item/text truth 저장
→ reviewed Ground Truth 축적
→ 필요 시 diagnostics ZIP export
```

### 2. Failure stage 분류

새 실패는 다음 중 어디에서 시작했는지 먼저 특정한다.

```text
capture
→ structural proposal recall/ranking
→ close-X semantic evidence
→ magnifier semantic evidence
→ inspect-header lock
→ item-name ROI
→ raw OCR
→ user substitution
→ catalog sanitation/matcher
→ visual recovery
→ Item ID
→ mapped presentation
→ overlay / stale-state timing
```

### 3. Replay regression

수정 전후 reviewed `full.png` 전체를 현재 production pipeline으로 replay한다.

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

기존 정상 Case가 실패하면 평균 정확도가 올라가도 `REGRESSION`이다.

### 4. 정확도보다 false-positive 방지 우선

Wrong identity는 miss보다 높은 우선순위로 분석한다.

추가 Ground Truth 없이 다음을 낮추지 않는다.

- structural floor
- header floor
- matcher confidence
- top1/top2 margin
- candidate caps

## 우선 관찰 표본

- 다양한 resolution / DPI / Tarkov UI scale
- tall/large detail window
- stash/inventory frame과 크게 겹치는 detail proposal
- detector가 정답 candidate를 만들지 못하는 recall miss
- 정답 candidate가 낮은 rank로 밀리는 ranking 문제
- close-X/magnifier semantic miss
- short/sparse item title
- `r`, `0`, slash-zero-like glyph, complex Hangul
- punctuation이 포함된 official item name
- near-name ambiguity false positive
- user substitution이 실제 반복 오인식을 안전하게 보정하는지
- Item ID 성공 뒤 trader/flea/slots/RequiredTotal completeness
- 빠른 Item 전환 stale-result isolation
- 장시간 CPU/memory/UI responsiveness
- telemetry에서 OCR/deep/visual recovery가 실제 병목인지

## 성능 개선 원칙

v1.4.x 때처럼 “성능 최적화 보류” 상태는 아니다. v1.5.0에서 telemetry와 첫 exact-same-cycle OCR reuse가 이미 들어갔다.

향후 추가 최적화는 telemetry evidence를 먼저 본다.

우선 후보:

- 동일 candidate/frame duplicate work 감소
- unnecessary deep OCR 감소
- bitmap copy/convert 감소
- visual recovery 조기 종료
- catalog recovery candidate work 감소

금지:

- 성능을 이유로 header/matcher threshold 완화
- candidate cap을 근거 없이 감소
- cross-frame OCR result를 현재 evidence로 재사용
- stale previous Item result를 현재 identity proof로 사용

## 작은 기술 부채

`src/JunhyunHelper.Desktop/Scanner/ScannerLatencyTypeAliases.cs`는 telemetry 통합 중 남은 `ScannerDetectedCandidate` type alias다.

v1.5.0 exact public source를 release 이후 흔들기 위해 제거하지 않는다. 향후 PATCH에서 실제 type declaration으로 정리할 경우 full build/tests/publish/Product UI/Map/Scanner smoke를 다시 통과해야 한다.

## 다음 PATCH 판단 기준

다음 PATCH는 다음 중 하나 이상의 실제 evidence가 있을 때 추진한다.

- reviewed Ground Truth에서 반복되는 miss/wrong-identity cluster
- 기존 정상 Case의 reproducible regression
- telemetry에서 확인된 명확한 latency bottleneck
- mapped-data source shape 변화
- catalog/Quest live-data compatibility drift
- 실제 사용자 UI/장시간 안정성 문제

새 기능을 먼저 추가하는 것이 목표가 아니다. **v1.5.0 public baseline을 안정적으로 유지하면서 evidence가 있는 결함만 수정한다.**
