# Scanner — 제품/기술 계약

기준일: 2026-08-26
상태: **v1.7.8 PUBLIC STABLE / FEATURE COMPLETE / MAINTENANCE ONLY**

이 문서는 현재 Scanner 제품 동작과 기술 안전 계약의 canonical 전문 문서다.
역사적 v1.3.x/v1.4.x/v1.5.x/v1.6.x/v1.7.x 근거는 각 버전별 결정·릴리즈 문서에 보존하고, 현재 구현 판단은 이 문서와 `STATE.md`, 실제 코드가 우선한다.

## 1. 목적과 경계

Scanner는 Escape from Tarkov 화면 픽셀을 기존 JunhyunHelper Item ID에 연결하는 독립 입력 subsystem이다.

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ close-X / magnifier / inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog sanitation / normalization
→ conservative catalog matching / bounded recovery
→ optional local Tarkov-font corroboration/recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional correction / Ground Truth
```

Scanner는 범용 OCR이 아니라 **현재 공식 한국어 Tarkov Item catalog를 identity authority로 사용하는 closed-domain recognizer**다.

오탐(false positive)은 미탐(false negative)보다 나쁘다.

금지:

- game memory read
- DLL injection
- packet interception
- process-internal game data read
- icon/image 단독 Item identity 확정
- scan 순간 HTTP/API
- current official catalog 밖 임의 Item 생성
- evidence 없이 OCR/semantic/matcher threshold 완화
- 제품 기본값에 automatic global `r`/`0`/한글 forced substitution table 추가
- cross-frame OCR result를 현재 Item identity proof로 재사용

## 2. Current v1.7.8 product state

Scanner는 기능 개발이 끝난 **maintenance-only** 상태다. 실제 사용자 evidence가 있는 회귀만 failure stage를 확인해 최소 수정한다.

현재 신뢰 경계와 사용자 계약:

- recognition log → exact current in-memory frame quick-correction
- reviewed Ground Truth와 runtime log lifecycle 분리
- 정상 monitoring은 durable automatic diagnostic Case를 생성하지 않음
- user-explicit correction save만 reviewed durable Ground Truth가 됨
- legacy `automatic_sample + unreviewed` Case만 fail-closed background cleanup
- Scanner catalog last-known-good / market coverage collapse protection
- Item ID 확정 이후 metadata/market/needed 동일-ID join
- Data Update transactional validation/read-back/activation hardening
- Scanner/Map hotkey는 primary key + optional Ctrl/Alt/Shift 공통 gesture contract
- anonymous public release redownload + public product smoke proof

v1.7.8은 레이드 인벤토리 수평선이 inspect header와 이어져 header-left ownership이 실제 상세창보다 왼쪽으로 확장되는 회귀를 사용자 reviewed 8 Case로 확인해 수정했다. 실패 6건은 OCR 오인식이 아니라 `HEADER_CLOSE_NOT_LOCKED` / `TITLE_ANCHOR_INCOMPLETE`로 OCR 이전에 차단됐다.

새 raid ownership recovery는 기존 정상 header 경로가 실패한 뒤 강한 `RED_X_CANDIDATE >= 0.90`인 경우에만 사용한다. 기존 최종 semantic floor `0.68`과 red close-X/magnifier/header/title evidence를 모두 유지한다.

일반 Scanner 화면, local item search, Mini Scanner settings schema v6, 교정 이미지 원본 좌표 보존 및 saved Case 재교정 흐름은 유지한다.

## 3. 핵심 안전 불변식

```text
structural floor = 0.34
trusted header floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

Runtime OCR identity path의 최소 semantic gate:

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
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ PrintWindow 우선
→ invalid/empty이면 exact client screen rectangle fallback
```

최소화되었거나 유효한 client area가 없으면 인식하지 않는다.

### Display Test

연결된 display에 실사용과 동일한 detector/OCR/catalog/presentation pipeline을 적용한다.

- 실제 continuous Scanner와 Display Test continuous mode는 상호 배타적이다.
- 현재 일반 Scanner surface가 아니라 `고급` 영역에서 다룬다.

### One-shot

one-shot 기능은 현재도 유지된다.

일반 Scanner 화면에서는 별도 1회 스캔 버튼을 노출하지 않지만 전역 hotkey로 계속 사용할 수 있다.

기본값:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

사용자 설정 gesture는 일반 key 단독 또는 Ctrl/Alt/Shift의 선택적 조합을 허용한다. Windows key modifier는 지원하지 않는다.

계약:

- current TarkovWindow 또는 Display Test를 한 번 분석한다.
- continuous Scanner 상태를 영구 변경하지 않는다.
- local healthy catalog만 사용한다.
- scan-time network refresh를 시작하지 않는다.
- shared detector/OCR/presentation state와 직렬화한다.
- one-shot candidate cap은 12다.
- 동일 gesture 중복 지정은 허용하지 않는다.

## 5. Full Item identity catalog

Scanner identity catalog는 Needed Items subset이 아니라 현재 GameMode의 **공식 전체 Item catalog**다.

준비/업데이트 단계에서는 remote source를 사용할 수 있으나 실제 scan 순간에는 local/memory data만 사용한다.

Identity health와 market/dimension coverage는 분리한다.
가격 정보가 없다는 이유로 공식 Item identity를 무효화하지 않는다.

Catalog/cache lifecycle은 GameMode operation ordering을 지켜 과거 mode의 느린 load/refresh가 최신 mode state를 덮어쓰지 못하게 한다.

## 6. Game Data update

사용자는 일반 Tarkov 데이터와 Scanner catalog를 별도로 갱신할 필요가 없다.

```text
remote Game Content fetch/build
→ general content validation/activation
→ current GameMode Scanner item/market catalog refresh
→ combined status report
```

계약:

- Scanner refresh만 실패하면 healthy general Game Content를 rollback하지 않는다.
- 기존 healthy Scanner cache가 있으면 유지한다.
- partial failure를 상태로 보고한다.
- 현재 일반 Scanner surface에는 catalog force-refresh 버튼을 노출하지 않는다.

## 7. Scanner 일반 UI — current

상단 primary controls:

- `스캐너 ON/OFF`
- `설정`
- `고급`
- `현재 결과 교정`

`현재 결과 교정`은 `ScannerRecognitionDebugStore`에 보존된 최신 exact in-memory frame을 기존 `ScannerCorrectionWindow`로 연다. 다른 오래된 frame을 임의로 대체하지 않는다.

하단 2분할:

- 왼쪽: `아이템 검색`
- 오른쪽: `Scanner 로그`

`고급`에는 다음만 둔다.

- Display Test / 테스트 스캐너
- 교정 데이터 관리
- Scanner 성능 진단 자료 내보내기

일반 surface에는 catalog recovery, regression/export, log-delete 같은 개발/진단 action을 펼쳐 놓지 않는다.

## 8. Scanner 아이템 검색

검색은 현재 memory/local full-item catalog를 사용한다.

검색 순간 network work를 만들지 않는다.

검색 결과:

- local cached icon
- official item name

선택 후 presentation:

- icon
- official item name
- Tarkov Wiki
- flea positive 24h average
- best valid non-flea trader RUB sell price
- best trader name where trustworthy
- current required total

`current required total`은 inventory shortage가 아니다.

```text
NeededItems[itemId].RequiredTotal
```

을 사용한다.

## 9. Detail rectangle proposal

Scanner Lab v3.8 계열의 RED-X/rectangle discovery 구조를 유지한다.

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
- historical aspect ≈ 1.3은 약한 ordering hint일 뿐 hard reject가 아니다.
- tall/large inspect window를 aspect prior만으로 제거하지 않는다.
- high IoU 자체는 dedupe 조건이 아니다.
- top/bottom/left/right가 실질적으로 다른 후보는 겹쳐도 semantic validation까지 보존한다.
- 사실상 같은 edge-jitter duplicate만 정리한다.
- rough red-X proximity는 ranking hint이며 actual close-X proof가 아니다.

Initial structural rectangle은 authoritative final bounds가 아니다.

## 10. Inspect-header semantic lock

Required evidence:

1. red close-X body/edge/color + diagonal-X shape
2. neutral inspect-header/frame
3. frame-left magnifier/search lane
4. magnifier ring/hollow center/handle/background morphology
5. dark item-title field
6. title text evidence

Title glyph segmentation은 title ROI ownership을 결정하지 않는다.

현재 recovery 우선순위:

```text
primary inspect-header lock
→ live Ground Truth recovery
→ v1.7.8 raid header ownership recovery
→ contained-subpanel recovery
→ fail closed
```

Oversized/coarse proposal을 contained-subpanel fallback으로 복구하는 경우에도 동일 semantic gate를 다시 통과해야 한다.

### v1.7.8 raid header ownership recovery

사용자 reviewed 레이드 실패 6건에서 주변 UI의 neutral horizontal line이 inspect header와 이어져 기존 fallback의 header-left가 실제 상세창보다 47~132px 왼쪽으로 이동했다. 이 잘못된 left ownership 때문에 magnifier 예상 lane도 함께 밀려 실제 돋보기를 놓쳤다.

새 recovery 진입 조건:

```text
candidate reason = RED_X_CANDIDATE
candidate structural score >= 0.90
```

coarse detail rectangle은 header-left ownership proposal로만 사용하며 Item identity proof가 아니다.

독립적으로 다시 요구하는 evidence:

```text
close-X template >= 0.40
close relation evidence >= 0.60
candidate-owned neutral header >= 0.74
magnifier template >= 0.54
magnifier relation evidence >= 0.66
dark title field >= 0.58
title text evidence >= 0.22
final HEADER_FRAME_LOCKED >= 0.68
```

기존 threshold를 낮춘 것이 아니라 잘못된 horizontal ownership만 교정한 뒤 기존 semantic evidence를 다시 계산한다. 빨간 X가 없는 동일 geometry는 procedural negative smoke에서 반드시 fail closed한다.

공식 결정: `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`.

## 11. OCR pipeline

Primary text recognizer는 Windows `ko-KR` OCR이다.

```text
locked item-name ROI
→ scale/preprocess
→ normal WinRT OCR
→ 필요 시 deep/high-contrast/binary/inverse OCR
→ raw text preservation
→ user substitution
→ current-catalog sanitation/normalization
→ catalog matching
→ optional visual corroboration/recovery
```

계약:

- raw OCR은 forensic evidence로 보존한다.
- exact-first다.
- fuzzy matching은 conservative confidence + top1/top2 margin을 요구한다.
- ambiguous/low-confidence는 Item ID 미확정이다.
- production OCR field는 item-name 하나다.

## 12. Current-catalog character policy

- 정상 ASCII letter/digit는 noisy evidence로 유지한다.
- 공식 Item 이름에 실제 쓰는 quotes/hyphens/brackets 등은 보존한다.
- catalog-impossible Unicode glyph를 특정 `r`, `0`, `I`, `l` 등으로 자동 확정하지 않는다.
- impossible embedded glyph는 `?` unknown-position evidence로 보존할 수 있다.
- complete current catalog에서 후보가 유일하고 충분히 분리될 때만 bounded recovery를 허용한다.
- short/ambiguous pattern은 fail closed다.
- current catalog 밖 후보 생성 금지다.

## 13. User OCR substitutions — schema v6

사용자 OCR substitution engine 자체는 유지한다.

```text
raw OCR
→ enabled user substitutions (single ordered pass)
→ catalog sanitation / normalization
→ matcher
```

계약:

- 기본 substitution list는 empty
- exact user-owned rule
- raw OCR 원문 덮어쓰기 금지
- diagnostic에서 raw/substituted/normalized/matched를 구분
- recursive/cyclic/chained reprocessing 금지
- 사용자 rule은 product-wide automatic substitution table이 아님

현재 일반 Scanner 설정 창은 Mini Scanner/hotkey 사용 흐름을 우선하고, 기존 substitution 설정 데이터는 schema migration에서 보존한다.

## 14. Scanner display settings schema v6

v6의 핵심 변화는 Mini Scanner layout contract다.

고정 identity header:

- item icon
- official item name

사용자 표시/순서 설정 대상:

- trader sell price
- flea average price
- trader price per slot
- flea price per slot
- current needed

v5 이하 migration에서 가능한 한 다음을 보존한다.

- Scanner enabled state
- hotkeys
- existing visibility settings
- Mini Scanner position
- font size
- user OCR substitutions

v6부터 item icon/name은 숨길 수 없다.

## 15. Tarkov-font visual recovery

게임 font binary는 public package에 재배포하지 않는다.

read-only Tarkov install discovery → LocalAppData font cache → official item-name template/features 흐름을 유지한다.

- current official catalog 밖 후보 생성 금지
- visual top1과 margin 모두 요구
- font unavailable/error/ambiguous이면 healthy OCR evidence를 임의 폐기하지 않는다.
- font/template cache는 generation-aware + bounded다.

## 16. OCR serialization / same-cycle reuse

Title OCR과 Mini Scanner inventory-context OCR은 하나의 WinRT OCR serialization boundary를 공유한다.

재사용 조건:

```text
same active scan cycle
same normal/deep class
same width/height/BPP
exact pixel SHA-256 identical
```

- one-pixel difference → reuse 금지
- cycle change → cache clear
- inter-frame/cross-cycle cache 없음

## 17. Stage latency telemetry

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

성능 개선은 telemetry evidence로만 결정한다.
Threshold 완화는 성능 최적화 수단이 아니다.

## 18. Title continuity stabilization

Title continuity signature는 이미 semantic validation된 같은 detail에서 harmless dark-background/GPU variation 때문에 result가 불필요하게 흔들리는 것을 줄이는 보조 evidence다.

- Item identity proof가 아니다.
- visible glyph shape 변화는 signature를 바꾼다.
- visible title ink 없음 → fail closed
- 다른 title/geometry/identity evidence → stale result clear

## 19. Mapped presentation

Item ID가 확정된 뒤 local trusted data에서 계산한다.

```text
BestTraderSellPrice = max valid non-flea RUB-equivalent price
BestTraderName = selected trusted source
FleaAveragePrice = positive avg24hPrice
Slots = positive width × height
TraderPricePerSlot = valid trader price / slots
FleaPricePerSlot = valid flea avg / slots
RequiredTotal = ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

Market/dimension 일부 누락은 affected field만 비우고 healthy Item identity를 폐기하지 않는다.

## 20. Mini Scanner — current

Mini Scanner는 matched Item presentation만 표시한다.

고정:

- icon
- official item name

사용자 순서/표시 설정:

- trader sell price
- flea average
- trader price/slot
- flea price/slot
- needed count

상인 가격은 가능한 경우 다음처럼 표시한다.

```text
Therapist 42,000₽
```

Mini Scanner window contract:

- Topmost
- ShowActivated=false
- ShowInTaskbar=false
- full-surface drag
- conservative Tarkov foreground/inventory context
- inventory OCR single-active + latest coalescing
- stale epoch result reject

## 21. Ground Truth / correction — current

Canonical durable evidence source는 `%LocalAppData%/JunhyunHelper/scanner/diagnostics/` 아래 **사용자가 명시적으로 저장한 reviewed Case dataset**이다.

정상 Scanner monitoring의 latest exact frame은 현재 교정을 위해 메모리에만 유지한다. 실패 자체만으로 durable Case를 만들지 않는다.

교정 화면은 원본 image가 커도 viewport 안에 맞게 자동 축소한다.

중요:

- visual scale과 Ground Truth coordinate를 분리
- 저장 ROI는 원본 pixel coordinate
- 축소 때문에 좌표 정밀도 손실 금지

Candidate-first fields:

1. detail rectangle
2. close-X
3. magnifier
4. item-name ROI
5. correct item/text

현재 기본 선택 UX는 이미지 위 candidate box 직접 클릭이다.

Fallback:

- correct candidate 없음 → manual rectangle
- actual semantic object 없음 → explicit `없음`

Candidate evidence 저장:

- ID
- type
- rank
- score
- geometry / normalized geometry
- selected/none/manual 상태

## 22. 저장 Case 재교정

`교정 데이터 관리`에서 기존 Case를 다시 열 수 있다.

복원 가능한 source:

- `case.json`
- `full.png`
- `candidate_selection.json`

기존 Ground Truth item/text와 candidate 선택을 복원해 같은 editor에서 수정한다.

- same Case ID 유지
- reviewed Ground Truth 갱신
- 복원 실패 시 기존 data 보존
- legacy automatic Case != Ground Truth 원칙 유지

## 23. Diagnostics / retention

### Durable Ground Truth

새 버전의 정상 monitoring은 durable automatic Case를 생성하지 않는다.

```text
runtime capture / recognition
→ latest exact frame in memory
→ bounded runtime text log
→ user explicitly chooses correction
→ user explicitly saves
→ reviewed durable Ground Truth
```

상세창 미탐지, header lock 실패, OCR/matcher 실패, ambiguity, 반복 stationary failure만으로 dataset이 증가하지 않는다.

### Legacy automatic Case cleanup

이전 버전에서 이미 생성된 Case는 다음을 모두 증명할 때만 background cleanup 대상이다.

```text
retention == automatic_sample
AND review_status == unreviewed
AND recent write safety window >= 5 minutes
AND pre-delete metadata/state re-read confirms unchanged ownership/status
```

- qualifying legacy automatic Case는 오래된 30일/300개/512MiB cap 정책이 아니라 해당 proof를 기준으로 정리한다.
- reviewed/manual Case는 자동 삭제하지 않는다.
- corrupt/unknown/unreadable metadata는 preserve fail closed한다.
- 삭제 직전 상태가 변경되면 보존한다.
- cleanup은 recognition hot path 밖 background maintenance에서 수행한다.

### Logs

- `%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)`
- `%LocalAppData%/JunhyunHelper/logs/startup.log(.1)`

Scanner text diagnostic log는 bounded rotation/retention을 유지한다. Ground Truth image lifetime과 분리한다.

사용자 activity feed의 동일 실패는 30초 동안 collapse하지만 support text diagnostics 자체를 Ground Truth로 취급하지 않는다.

공식 결정: `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`.

## 24. Replay regression

Reviewed Case replay result:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

기존에 맞았던 reviewed Case가 새 코드에서 틀리면 평균 정확도가 올라가도 `REGRESSION`이다.

v1.7.8 raid fix는 사용자 reviewed 8 Case의 픽셀 evidence와 procedural positive/negative smoke로 검증했다. 사용자 원본 Ground Truth image는 공개 저장소/CI artifact에 포함하지 않는다.

## 25. Release package contract — current

정식 user-facing package:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

ZIP/folder 이름에 version number를 붙이지 않는다.

Version identity는 다음에서 관리한다.

- Desktop project version
- EXE ProductVersion
- Git tag
- GitHub Release metadata

CI는 실제 stable ZIP을 생성해 내부 top-level folder와 required files를 검증한다.

현재 public stable proof는 `docs/RELEASE_1.7.8.md`와 `docs/.release-v1.7.8-status.json`을 기준으로 한다.

## 26. Current work — MAINTENANCE ONLY

v1.7.8 공개 검증 후 Scanner는 FEATURE COMPLETE / MAINTENANCE ONLY 단계다.

```text
real Tarkov usage
→ representative correct result review
→ miss/wrong identity correction
→ reviewed Ground Truth accumulation
→ failure-stage classification
→ modify affected stage only
→ full reviewed replay
→ REGRESSION=0
→ PATCH 판단
```

새 기능을 계속 추가하거나 성능 수치만 더 낮추는 것이 기본 방향이 아니다.
실제 Ground Truth/support evidence가 있는 실패만 좁게 수정한다.
