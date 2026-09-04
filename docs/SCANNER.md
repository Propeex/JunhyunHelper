# Scanner — 제품/기술 계약

상태: **EVERGREEN CURRENT SCANNER CONTRACT / FEATURE COMPLETE / MAINTENANCE ONLY**

이 문서는 현재 Scanner 제품 동작과 기술 안전 계약의 canonical 전문 문서다. 정확한 현재 릴리즈 버전·SHA·CI·asset은 `docs/PROJECT_STATE.json`, `docs/CURRENT_STATE.md`, `docs/STATE.md`를 사용한다. 역사적 근거는 버전별 결정/릴리즈 문서에 보존한다.

## 1. 목적과 경계

Scanner는 Escape from Tarkov 화면 픽셀을 JunhyunHelper Item ID에 연결하는 closed-domain input subsystem이다.

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ close-X / magnifier / inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user OCR substitution
→ conditional cross-environment title normalization
→ current-catalog sanitation / normalization
→ conservative catalog matching / bounded recovery
→ optional current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional correction / reviewed Ground Truth
```

Scanner는 범용 OCR이 아니라 **현재 공식 한국어 Tarkov full-item catalog를 identity authority로 사용하는 closed-domain recognizer**다.

오탐(false positive)은 미탐(false negative)보다 나쁘다.

금지/불변 원칙:

- current official catalog 밖 임의 Item 생성 금지
- geometry/environment normalization 단독 Item identity 확정 금지
- scan 순간 HTTP/API를 Item identity proof에 사용 금지
- stale/cross-frame OCR 또는 visual result를 현재 Item identity proof로 재사용 금지
- Item ID 확정 전 price/needed/slot/source/previous-frame metadata를 identity evidence로 사용 금지
- icon/image-only identity 금지
- game memory read / DLL injection / packet interception 금지
- 새로운 reviewed evidence 없이 semantic/OCR/matcher/visual acceptance 완화 금지

## 2. Current product state

Scanner는 기능 개발이 끝난 **maintenance-only** 상태다. 실제 사용자 evidence가 있는 회귀만 failure stage를 확인해 affected layer에 최소 수정한다.

현재 주요 계약:

- recognition log → latest exact current in-memory frame quick correction
- runtime log와 reviewed Ground Truth lifecycle 분리
- 정상 monitoring은 durable automatic diagnostic Case를 생성하지 않음
- user-explicit correction save만 reviewed durable Ground Truth
- legacy `automatic_sample + unreviewed`만 fail-closed cleanup
- full official Korean item catalog가 identity authority
- Item ID 확정 이후 metadata/market/needed는 동일-ID join
- Scanner `필요 개수` = canonical `NeededItems[itemId].RemainingTotal`
- Scanner item search Quest/Hideout source = canonical `NeededItems[itemId].Sources`
- Scanner/Map configurable hotkey = primary key + optional Ctrl/Alt/Shift, extra-modifier compatible / most-specific-wins
- Scanner Settings가 Mini display/order와 global Scanner hotkey 편집을 함께 소유
- Scanner Advanced는 MainWindow shared in-app overlay에 host
- verified main-CI artifact만 stable release 가능

## 3. 핵심 안전 불변식

```text
structural floor = 0.34
trusted header floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

Runtime OCR identity path 최소 semantic gate:

```text
TitleImage exists
AND valid title bounds
AND valid magnifier bounds
AND valid close-X bounds
AND TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
```

원칙:

- geometry는 proposal evidence일 뿐 Item identity proof가 아니다.
- structural score는 Item confidence가 아니다.
- close-X나 magnifier 하나만으로 detail identity를 확정하지 않는다.
- full semantic gate 이전 candidate는 diagnostics에는 남길 수 있으나 production OCR identity path에는 들어가지 않는다.
- ambiguity, incomplete lock, low confidence는 no identity로 끝낸다.
- threshold/candidate cap은 새로운 reviewed Ground Truth evidence 없이 변경하지 않는다.

## 4. Capture modes

### TarkovWindow

```text
EscapeFromTarkov process/window
→ client-area geometry
→ Borderless client capture
→ proven capture path
→ invalid/empty이면 exact-client screen fallback
```

최소화되었거나 유효한 client area가 없으면 인식하지 않는다.

### Display Test

연결된 display에 실사용과 동일한 detector/OCR/catalog/presentation pipeline을 적용한다.

- 실제 continuous Scanner와 Display Test continuous mode는 상호 배타적이다.
- 일반 Scanner surface가 아니라 `고급`에서 다룬다.
- v1.7.14 `고급`은 standalone product Window가 아니라 MainWindow shared overlay에 host된다.

### One-shot

기본값:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

계약:

- current TarkovWindow 또는 Display Test를 한 번 분석
- continuous Scanner 상태를 영구 변경하지 않음
- local healthy catalog만 사용
- scan-time network refresh 시작 금지
- shared detector/OCR/presentation state와 직렬화
- one-shot candidate cap 12
- 동일 Scanner gesture 중복 지정 금지

## 5. Full Item identity catalog

Scanner identity catalog는 Needed Items subset이 아니라 현재 GameMode의 **공식 전체 Item catalog**다.

준비/업데이트 단계에서는 remote source를 사용할 수 있으나 scan 순간에는 local/memory data만 사용한다.

Identity health와 market/dimension coverage는 분리한다. 가격 정보가 없다는 이유로 공식 Item identity를 무효화하지 않는다.

Catalog/cache lifecycle은 GameMode ordering을 지켜 과거 mode의 느린 load/refresh가 최신 mode state를 덮어쓰지 못하게 한다.

## 6. Game Data Update

사용자는 일반 Tarkov 데이터와 Scanner catalog를 별도로 갱신할 필요가 없다.

```text
remote Game Content fetch/build
→ general content validation/activation
→ current GameMode Scanner catalog refresh
→ combined status
```

- Scanner refresh만 실패하면 healthy general Game Content를 rollback하지 않는다.
- 기존 healthy same-mode Scanner cache가 있으면 유지한다.
- partial failure를 상태로 보고한다.

## 7. Scanner UI — current contract

Normal Scanner surface:

- `스캐너 ON/OFF`
- `설정`
- `고급`
- `현재 결과 교정` — 우측 command lane
- item search
- recognition log

### Scanner Settings

`ScannerSettingsWindow.xaml(.cs)`가 다음을 함께 소유한다.

Mini Scanner optional information:

- visibility
- order

Global Scanner hotkeys:

- 1회 인게임 스캔
- 1회 테스트 스캔
- Scanner ON/OFF

Display/order 변경과 hotkey 변경은 기존 Scanner settings persistence authority에 즉시 반영한다. 별도 dedicated hotkey Window는 없다.

**`ScannerHotkeySettingsWindow.xaml/.cs`는 v1.7.14에서 제거됐다.** 이 화면을 병렬 설정 authority로 복구하지 않는다.

Hotkey capture 규칙:

- modifier-only 입력 → capture preview
- Delete/Backspace → 미지정
- Esc → capture cancel
- Windows modifier → 미지원
- 다른 Scanner action과 동일 gesture → 거부
- 실제 persistence는 existing `ScannerCoordinator.SetOneShotTarkovHotkey`, `SetOneShotTestHotkey`, `SetScannerToggleHotkey` path 사용

Mini Scanner icon/official name은 fixed identity header이므로 `항상 표시` 설명 row를 두지 않는다.

### Scanner Advanced

`ScannerAdvancedWindow.xaml(.cs)`는:

- Display Test / 테스트 Scanner
- 교정 데이터 관리
- support/performance diagnostics

를 제공한다.

v1.7.14 product path:

```text
Advanced launcher
→ MainWindow.ToggleInAppWindowAsync("scanner-advanced", advanced)
→ shared overlay card
```

- standalone `Show()` product path 사용 금지
- 내용 자체의 별도 `닫기` button 없음
- same launcher 재클릭 / backdrop / common overlay X로 dismiss
- existing advanced action semantics는 child가 유지

### Search

Scanner item search는 local/memory catalog를 사용하고 입력창 오른쪽 내부 `×` clear affordance를 사용한다. Clear affordance는 filtering/identity logic을 소유하지 않는다.

### Current correction

`현재 결과 교정`은 `ScannerRecognitionDebugStore`에 보존된 **최신 exact in-memory frame**만 correction editor로 연다. 오래된 다른 frame이나 자동 sample을 현재 truth로 대체하지 않는다.

## 8. Item-title OCR

기본 OCR은 serialized Windows ko-KR boundary를 사용한다.

Normal OCR이 성공하면 그 결과를 그대로 사용한다. 정상 success path에 luminance histogram/copy/추가 OCR 비용을 넣지 않는다.

Normal OCR miss 또는 기존 bounded deep pass에서만 environment normalization 후보를 평가한다.

## 9. Cross-environment normalization

제품 목표는 특정 PC별 tuning이 아니라 입력 canonicalization이다.

금지 접근:

```text
if HDR -> threshold A
if 1440p -> threshold B
if GPU X -> threshold C
```

현재 runtime:

```text
normal OCR
→ text 있음: 기존 결과 즉시 사용
→ text 없음: title ROI luminance profile 분석
    → reference/flat: 기존 경로 유지
    → lifted/washed/low-contrast: normalized auxiliary OCR
→ existing bounded deep OCR
    → abnormal environment일 때만 normalized auxiliary evidence 추가
→ existing conservative catalog matching
→ Item ID or fail closed
```

Luminance profile:

- P60: dark title-field background estimate
- P99.75: sparse bright glyph foreground estimate
- contrast span이 minimum usable contrast 아래면 flat/no-contrast
- flat/no-contrast는 adaptive normalization 금지
- reference SDR-like profile은 historical preprocessing 유지
- lifted/washed/compressed-contrast profile만 adaptive grayscale mapping 허용

Normalization은 Item identity proof가 아니다. 출력 OCR evidence는 기존 catalog matcher/ambiguity 기준을 그대로 통과해야 한다.

Deterministic procedural matrix:

- reference SDR-like
- HDR→SDR-like lifted/washed
- lifted + compressed contrast
- low-contrast gamma/rendering variation
- 1080p / 1440p / 4K proportional title raster
- flat/no-contrast negative

Procedural matrix는 reviewed Ground Truth를 대체하지 않는다.

## 10. Detail rectangle proposal

RED-X/rectangle discovery 구조를 유지한다.

```text
capture
→ RED-X connected-component proposals
+
→ rectangle/edge fallback proposals
→ near-duplicate cleanup
→ ranked candidate set
```

계약:

- structural floor `0.34`
- continuous cap `8`
- one-shot cap `12`
- aspect ratio는 ordering hint일 뿐 hard reject가 아님
- overlapping candidate라도 실질적 geometry가 다르면 semantic validation까지 보존
- rough red-X proximity는 ranking hint이며 actual close-X proof가 아님
- initial structural rectangle은 authoritative final bounds가 아님

## 11. Inspect-header semantic lock

Required evidence는 close-X, magnifier, neutral header/frame, title field relation을 함께 사용한다.

최종 production OCR gate는 `HEADER_FRAME_LOCKED >= 0.68`이다.

### Raid ownership recovery

Raid inventory horizontal line이 inspect header와 이어져 header-left ownership이 실제 상세창보다 왼쪽으로 확장되는 회귀에 대한 recovery 순서:

```text
primary header lock
→ live Ground Truth recovery
→ raid ownership recovery
→ contained-subpanel recovery
→ fail closed
```

Raid recovery entry:

```text
candidate reason = RED_X_CANDIDATE
structural score >= 0.90
```

Coarse proposal은 header-left ownership proposal일 뿐 Item identity proof가 아니다. Close-X, magnifier, neutral header, dark title field, title text evidence와 final header floor를 독립적으로 다시 요구한다.

## 12. Catalog matching / bounded recovery

OCR text는 current official catalog를 대상으로 sanitation/normalization 후 보수적으로 매칭한다.

- exact official name 우선
- conservative confidence + top1/top2 margin
- ambiguous result fail closed
- bounded unknown/edit recovery only
- user substitution은 명시적 사용자 correction 범위에서만 적용
- automatic global forced substitution table을 제품 기본값으로 만들지 않음
- optional visual corroboration은 current exact pixels에 한정
- matcher/visual recovery acceptance를 new reviewed evidence 없이 완화하지 않음

## 13. Same-cycle performance / pacing contract

Continuous observation target:

```text
200 ms
```

Pacing policy는 cycle overrun 뒤 missed tick을 back-to-back replay하지 않는다.

과거 일부 PC의 5~13초 latency root cause는 Windows OCR 자체가 아니라 동일 current-frame visual evidence의 반복 계산이었다.

문제 PC actual Tarkov `ReadingTitle → ShowingItem` 성공 baseline:

```text
minimum 38.07 ms
median  63.92 ms
maximum 1.05 s
mean    211.47 ms
```

Same active Scanner cycle에서 **exact current-pixel identity가 동일한 evidence만** 재사용한다. Cycle이 바뀌면 폐기한다. Cross-frame Item identity cache가 아니다.

Wall-clock benchmark를 normal CI의 고정 합격선으로 사용하지 않는다.

## 14. Mini Scanner / item-search presentation

Confirmed Item identity가 presentation authority다.

Mini Scanner display contract:

```text
confirmed Scanner Item
→ preview/display-test: show
→ overlay already visible: immediate update
→ hidden real Scanner:
   Tarkov foreground yes → show
   Tarkov foreground no  → hidden
```

Sticky presentation:

```text
success → show/update + miss budget reset
miss #1 → retain last good
miss #2 → retain last good
miss #3 → hide
```

Progress-only state는 miss로 세지 않는다.

### Needed quantity

Item ID 확정 뒤:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
```

이 값은 current Inventory/FIR 조건이 반영된 canonical remaining requirement다. Scanner가 raw inventory를 다시 빼거나 `RequiredTotal`을 사용자 표시값으로 사용하지 않는다.

### Quest / Hideout source

Searched confirmed needed item:

```text
confirmed Item ID
→ ItemsWorkspace.Plan.NeededItems[itemId].Sources
→ related Quest/Hideout source rows
→ existing content navigation
```

- source list는 기존 Needed Items 계산 결과를 same-ID로 join한다.
- Scanner가 Quest/Hideout requirement/source를 별도 재구성하지 않는다.
- source/needed information은 Item ID 확정 전 identity evidence가 아니다.

### Other mapped fields

- official name
- local cached icon
- trusted best non-flea trader price/name
- positive flea `avg24hPrice`
- positive width/height slot count
- price/slot where derivable

Market/dimension failure는 affected presentation field만 비우고 Item identity를 소급 무효화하지 않는다.

## 15. Ground Truth / durable data

Ground Truth는 사용자가 직접 검토/교정하고 명시적으로 저장한 Case만 의미한다.

Normal monitoring:

```text
runtime capture / recognition
→ latest exact frame in memory
→ bounded runtime log
→ user explicitly opens correction
→ user explicitly saves
→ reviewed durable Ground Truth
```

Correction coordinates는 original source pixels가 authority다. UI auto-fit/display scale 좌표를 durable truth로 저장하지 않는다.

Candidate-first correction:

1. detail rectangle
2. close-X
3. magnifier
4. item-name ROI
5. correct item/text

Candidate가 정답을 포함하지 않으면 manual rectangle, 실제 semantic object가 없으면 explicit `없음`을 사용한다.

Saved Case re-edit는 same Case ID와 existing reviewed data를 보존한다. Restore failure 시 existing Ground Truth를 overwrite/delete하지 않는다.

Legacy automatic Case cleanup은 다음 모두를 증명할 때만 허용한다.

```text
retention = automatic_sample
review_status = unreviewed
recent-write safety satisfied
metadata/state re-read unchanged
```

reviewed/manual/corrupt/unknown/state-changed Case는 preserve fail closed한다.

Private user pixel evidence를 CI 편의를 위해 public repository에 넣지 않는다.

## 16. Activity/log/retention contract

- 동일 Scanner activity failure는 bounded collapse 정책으로 반복 spam을 줄임
- bounded text log와 reviewed Ground Truth lifetime 분리
- runtime diagnostics는 failure stage 설명에 사용
- durable user-reviewed evidence는 자동 삭제하지 않음
- `SerializedScannerOcrEngine` reflection diagnostic adapter는 intentional technical debt

## 17. Hotkey contract

Scanner와 configurable Map actions는 `primary key + optional Ctrl/Alt/Shift`를 공유한다.

현재 matching 계약:

- primary key 일치 필수
- binding에 등록된 Ctrl/Alt/Shift는 모두 눌려 있어야 함
- 등록하지 않은 Ctrl/Alt/Shift 추가 입력 허용
- 같은 primary key에 여러 compatible binding이 있으면 required modifier 수가 더 많은 더 구체적인 binding 우선
- 동률은 기존 기능 우선순위/안정적인 등록 순서
- bare key 허용
- Windows modifier 미지원
- Map bare NumPad0~5 direct floor 유지
- modifier+NumPad configurable Map action 허용

v1.7.14는 hotkey matching 의미를 바꾸지 않고 editor ownership을 Scanner Settings로 통합했다.

## 18. v1.7.14 shared overlay contract

Scanner Settings와 Scanner Advanced는 MainWindow shared overlay owner를 사용한다.

```text
launcher
→ MainWindow.ToggleInAppWindowAsync
→ shared overlay
→ same launcher / backdrop / common X → dismiss
```

- overlay는 표시/닫기 lifetime만 소유
- child validation/persistence semantics를 MainWindow가 재구현하지 않음
- Scanner Advanced standalone product Window 금지
- old dedicated Scanner hotkey Window 금지

Actual Product UI smoke는 Scanner Advanced를 실제 shared overlay에 host한 상태에서 rendering/clipping/dismiss contract를 검증한다.

## 19. CI / release contract

Release candidate gate:

```text
Release build
→ deterministic tests
→ Windows x64 self-contained single-file publish
→ ProductVersion / FIRST_RUN verification
→ actual Product UI / Scanner / Map / Factory / MiniMap smoke
→ graceful shutdown / clean portable root
→ release package + SHA256 verification
→ exact main source CI
→ exact artifact Release workflow
→ public tag/release/asset readback
```

Current release proof is intentionally not duplicated in this specialist contract. Use `docs/PROJECT_STATE.json`, `docs/CURRENT_STATE.md`, `docs/STATE.md`, and the release record for the current version. Published stable assets remain immutable.

## 20. Maintenance workflow

```text
evidence
→ failure stage
→ root cause
→ affected layer only
→ reviewed replay where runnable
→ procedural regression where applicable
→ full Windows CI/publish/product smoke/package
→ PATCH release
→ public release readback
→ canonical docs sync
```

새 실제 evidence 없이 threshold/candidate cap/OCR/matcher/visual acceptance를 선제 변경하지 않는다.

Failure stage 예시:

- capture
- structural proposal
- inspect-header ownership/semantic lock
- title ROI
- OCR
- substitution/sanitation
- catalog match/ambiguity
- visual corroboration
- presentation join
- Mini Scanner context
- settings/overlay UI
- Ground Truth persistence

## 21. Support-bundle privacy contract

Scanner support/performance diagnostic export는 환경/성능 trace와 bounded diagnostic log만 지원 분석용으로 다룬다.

다음 사용자 data를 자동 포함하지 않는다.

- reviewed Ground Truth source pixels/dataset
- `user.db`
- profile database
- game-account information
- account-identifying user-progress data

필요한 Ground Truth는 사용자가 명시적으로 전달한 reviewed evidence로 별도 처리한다.

## 22. Current specialist file map

Core:

- `Core/Scanner/ScannerRecognition.cs`
- `Core/Scanner/ScannerItemMatcher.cs`
- `Core/Scanner/ScannerObservationPacingPolicy.cs`
- `Core/Scanner/ScannerOcrCharacterPolicy.cs`
- `Core/Scanner/ScannerOcrSubstitution.cs`
- `Core/Scanner/ScannerPresentationJoin.cs`
- `Core/Scanner/ScannerTitleIdentitySignature.cs`

Infrastructure:

- `Infrastructure/Scanner/ScannerCatalogService.cs`

Desktop:

- `Scanner/ScannerCoordinator*.cs`
- `Scanner/ScannerRuntimeService*.cs`
- `Scanner/ScannerLab38WindowsVision.cs`
- `Scanner/FontAwareScannerOcrEngine.cs`
- `Scanner/SerializedScannerOcrEngine.cs`
- `Scanner/TarkovTitleFontProvider.cs`
- `Scanner/ScannerFullCatalogVisualMatcher.cs`
- `Scanner/ScannerItemPresentationService.cs`
- `Scanner/ScannerRecognitionDebugStore.cs`
- `Scanner/ScannerLatencyTelemetry.cs`
- `Scanner/ScannerPage.xaml(.cs)`
- `Scanner/ScannerPage.ProductUsability.cs`
- `Scanner/ScannerSettingsWindow.xaml(.cs)`
- `Scanner/ScannerAdvancedWindow.xaml(.cs)`
- `Scanner/MiniScannerWindow.xaml(.cs)`
- `MainWindow.ScannerItemNavigation.cs`
- `MainWindow.InAppOverlay.cs`
- `MainWindow.ProductUiLayoutSmoke.cs`

Current regression references:

- `tests/JunhyunHelper.Tests/Maintenance/V1714UiConsistencyContractTests.cs`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/SCANNER_GROUND_TRUTH.md`

현재 진행 중 Scanner 작업의 유무와 중단 지점은 `docs/ACTIVE_WORK.md`를 기준으로 판단한다.
