# Scanner — 제품/기술 계약

기준일: 2026-08-27
상태: **v1.7.11 PUBLIC STABLE / FEATURE COMPLETE / MAINTENANCE ONLY**

이 문서는 현재 Scanner 제품 동작과 기술 안전 계약의 canonical 전문 문서다. 역사적 근거는 버전별 결정/릴리즈 문서에 보존하고, 현재 구현 판단은 이 문서와 `STATE.md`, 실제 코드가 우선한다.

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
→ optional correction / Ground Truth
```

Scanner는 범용 OCR이 아니라 **현재 공식 한국어 Tarkov full-item catalog를 identity authority로 사용하는 closed-domain recognizer**다.

오탐(false positive)은 미탐(false negative)보다 나쁘다.

금지/불변 원칙:

- current official catalog 밖 임의 Item 생성 금지
- geometry/environment normalization 단독 Item identity 확정 금지
- scan 순간 HTTP/API를 Item identity proof에 사용 금지
- stale/cross-frame OCR 또는 visual result를 현재 Item identity proof로 재사용 금지
- Item ID 확정 전 price/needed/slot metadata를 identity evidence로 사용 금지
- 새로운 reviewed evidence 없이 semantic/OCR/matcher/visual acceptance 완화 금지

## 2. Current v1.7.11 product state

Scanner는 기능 개발이 끝난 **maintenance-only** 상태다. 실제 사용자 evidence가 있는 회귀만 failure stage를 확인해 affected layer에 최소 수정한다.

현재 주요 계약:

- recognition log → latest exact current in-memory frame quick-correction
- runtime log와 reviewed Ground Truth lifecycle 분리
- 정상 monitoring은 durable automatic diagnostic Case를 생성하지 않음
- user-explicit correction save만 reviewed durable Ground Truth
- legacy `automatic_sample + unreviewed`만 fail-closed cleanup
- full official item catalog가 identity authority
- Item ID 확정 이후 metadata/market/needed는 동일-ID join
- Scanner `필요 개수`는 canonical `NeededItems[itemId].RemainingTotal`
- Scanner/Map configurable hotkey는 primary key + optional Ctrl/Alt/Shift이며 extra modifier compatibility / most-specific-wins 정책 적용
- verified main-CI artifact만 stable release 가능

v1.7.11은 Scanner identity recognition 기준을 변경하지 않았다. `필요 개수` presentation과 configurable hotkey modifier UX만 Scanner 관련 변경이다.

v1.7.10의 공개 배포 환경별 luminance normalization은 그대로 유지한다.

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
→ existing proven capture path
→ invalid/empty이면 existing exact-client fallback
```

최소화되었거나 유효한 client area가 없으면 인식하지 않는다.

### Display Test

연결된 display에 실사용과 동일한 detector/OCR/catalog/presentation pipeline을 적용한다.

- 실제 continuous Scanner와 Display Test continuous mode는 상호 배타적이다.
- 일반 Scanner surface가 아니라 `고급` 영역에서 다룬다.

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
- 동일 gesture 중복 지정 금지

## 5. Full Item identity catalog

Scanner identity catalog는 Needed Items subset이 아니라 현재 GameMode의 **공식 전체 Item catalog**다.

준비/업데이트 단계에서는 remote source를 사용할 수 있으나 scan 순간에는 local/memory data만 사용한다.

Identity health와 market/dimension coverage는 분리한다. 가격 정보가 없다는 이유로 공식 Item identity를 무효화하지 않는다.

Catalog/cache lifecycle은 GameMode ordering을 지켜 과거 mode의 느린 load/refresh가 최신 mode state를 덮어쓰지 못하게 한다.

## 6. Game Data update

사용자는 일반 Tarkov 데이터와 Scanner catalog를 별도로 갱신할 필요가 없다.

```text
remote Game Content fetch/build
→ general content validation/activation
→ current GameMode Scanner catalog refresh
→ combined status
```

- Scanner refresh만 실패하면 healthy general Game Content를 rollback하지 않는다.
- 기존 healthy Scanner cache가 있으면 유지한다.
- partial failure를 상태로 보고한다.

## 7. Scanner UI — current

상단 primary controls:

- `스캐너 ON/OFF`
- `설정`
- `고급`
- `현재 결과 교정`

`현재 결과 교정`은 `ScannerRecognitionDebugStore`에 보존된 최신 exact in-memory frame만 기존 correction window로 연다. 다른 오래된 frame을 대체하지 않는다.

`고급`:

- Display Test / 테스트 스캐너
- 교정 데이터 관리
- Scanner 성능 진단 자료 내보내기

## 8. Item-title OCR

기본 OCR은 serialized Windows ko-KR boundary를 사용한다.

Normal OCR이 성공하면 그 결과를 그대로 사용한다. v1.7.10부터 정상 성공 경로에 luminance histogram/copy/추가 OCR 비용을 넣지 않는다.

Normal OCR miss 또는 기존 bounded deep pass에서만 environment normalization 후보를 평가한다.

## 9. v1.7.10 environment normalization

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
- contrast span이 최소 usable contrast 아래면 flat/no-contrast로 간주
- flat/no-contrast는 adaptive normalization 금지
- reference SDR-like profile은 historical preprocessing 유지
- lifted/washed/compressed-contrast profile만 adaptive grayscale mapping 허용

Normalization은 Item identity proof가 아니다. 출력 OCR evidence는 기존 catalog matcher/ambiguity 기준을 그대로 통과해야 한다.

### Procedural environment regression

Deterministic regression matrix:

- reference SDR-like luminance
- HDR→SDR-like lifted/washed luminance
- lifted + compressed contrast
- low-contrast gamma/rendering variation
- 1080p proportional title raster
- 1440p proportional title raster
- 4K proportional title raster
- flat/no-contrast negative case

Procedural matrix는 reviewed Ground Truth를 대체하지 않는다. 실제 reviewed Case가 있는 recognition change는 기존 정답을 유지한 replay에서 `REGRESSION=0`을 요구한다.

공식 결정: `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`.

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
- aspect ratio는 ordering hint일 뿐 hard reject가 아님
- overlapping candidate라도 실질적 geometry가 다르면 semantic validation까지 보존
- rough red-X proximity는 ranking hint이며 actual close-X proof가 아님
- initial structural rectangle은 authoritative final bounds가 아님

## 11. Inspect-header semantic lock

Required evidence는 close-X, magnifier, neutral header/frame, title field relation을 함께 사용한다.

최종 production OCR gate는 `HEADER_FRAME_LOCKED >= 0.68`이다.

### v1.7.8 raid ownership recovery

레이드 inventory horizontal line이 inspect header와 이어져 기존 fallback이 header-left를 실제 상세창보다 47~132px 왼쪽으로 소유하던 회귀를 reviewed 8 Case로 확인했다.

Recovery order:

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

- ambiguous result는 fail closed
- user substitution은 명시적 사용자 교정 범위에서만 적용
- automatic global forced substitution table을 제품 기본값으로 만들지 않음
- optional visual corroboration은 current exact pixels에 한정
- matcher/visual recovery acceptance를 new reviewed evidence 없이 완화하지 않음

## 13. v1.7.6 performance contract

과거 일부 PC의 5~13초 latency root cause는 Windows OCR 자체가 아니라 동일 current-frame visual evidence의 반복 계산이었다.

문제 PC actual Tarkov `ReadingTitle → ShowingItem` 성공 12건:

```text
minimum 38.07 ms
median  63.92 ms
maximum 1.05 s
mean    211.47 ms
```

Display Test:

```text
하프 마스크: 10,840.877 ms → 70.603 ms
USB 보안 플래시 드라이브: 12,686.278 ms → 1,354.775 ms
```

같은 Scanner latency cycle에서 exact current-pixel identity가 동일한 visual evidence만 재사용한다. Cycle이 바뀌면 즉시 폐기한다. cross-frame Item identity cache가 아니다.

## 14. Mini Scanner presentation

Confirmed Item identity가 presentation authority다.

v1.7.9에서 Item ID 확정 후 Mini Scanner만 별도로 수행하던 auxiliary inventory-header OCR의 display veto 권한을 제거했다.

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

Item ID 확정 뒤 `필요 개수`는 `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`을 표시한다. 이는 current Inventory와 FIR 조건이 반영된 canonical remaining requirement이며 `RequiredTotal`을 Scanner가 별도 표시하거나 재계산하지 않는다.

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

Legacy automatic Case cleanup은 다음 모두를 증명할 때만 허용한다.

```text
retention = automatic_sample
review_status = unreviewed
recent-write safety >= 5 minutes
metadata/state re-read unchanged
```

reviewed/manual/corrupt/unknown/state-changed Case는 preserve fail closed한다.

Private user pixel evidence를 CI 편의를 위해 public repository에 넣지 않는다.

## 16. Activity/log contract

- 동일 Scanner activity failure는 30초 동안 collapse
- bounded text log와 reviewed Ground Truth lifetime은 분리
- runtime diagnostics는 failure stage 설명에 사용
- durable user-reviewed evidence는 자동 삭제하지 않음

## 17. Hotkey contract

Scanner와 configurable Map actions는 `primary key + optional Ctrl/Alt/Shift`를 공유한다.

현재 matching 계약:

- primary key 일치 필수
- binding에 등록된 Ctrl/Alt/Shift는 모두 눌려 있어야 함
- 등록하지 않은 Ctrl/Alt/Shift 추가 입력은 허용
- 같은 primary key에 여러 compatible binding이 있으면 required modifier 수가 더 많은 더 구체적인 binding 우선
- 동률은 기존 기능 우선순위/안정적인 등록 순서
- bare key 허용
- Windows modifier 미지원
- Map bare NumPad0~5 direct floor 유지
- modifier+NumPad configurable Map action 허용

공식 결정: `docs/DECISION_V1.7.11_MAINTENANCE.md`.

## 18. CI / release contract

Release candidate gate:

```text
Release build
→ automated tests
→ Windows x64 self-contained single-file publish
→ startup / rendered Product UI / Scanner / Map / Factory / MiniMap smoke
→ graceful shutdown / clean portable root
→ release package + SHA256 verification
→ artifact upload
```

Stable release는 main CI가 성공한 exact main commit의 artifact만 Release workflow가 게시한다.

v1.7.11 proof:

```text
PR #194 final head: 4351670d378fedf7000ada4d613bf1527e203a16
PR CI: 33032104032 — SUCCESS
main release source: 0f97c6e5340ae91581a9242ec236bbd7885b34d5
main CI: 33033282963 — SUCCESS
Release workflow: 33033434877 — SUCCESS
release id: 377531277
asset id: 531635485
asset SHA-256: f1ad15debc29b7a167a13448c8df65785f57139a91d8b5d246205a14f9a5800d
392 passed / 0 failed / 0 skipped
```

## 19. Maintenance workflow

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

## 20. Support-bundle privacy contract

`Scanner > 고급 > Scanner 성능 진단 자료 내보내기`는 환경/성능 trace와 bounded diagnostic log만 지원 분석용 ZIP으로 내보낸다.

다음 사용자-private 또는 계정 관련 데이터는 support bundle에 포함하지 않는다.

- Scanner Ground Truth image / source pixel dataset
- `user.db` 또는 profile database
- Tarkov/game account information
- 사용자 진행도나 계정 식별에 해당하는 데이터

이 exclusion은 진단 기능의 제품 안전 계약이다. 향후 exporter를 변경할 때도 유지해야 하며, release regression에서 금지 파일/데이터가 support ZIP에 들어가지 않음을 검증한다.