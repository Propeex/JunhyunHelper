# Scanner — 제품/기술 계약

기준일: 2026-08-24
상태: **v1.5.0 PUBLIC RELEASE / VERIFIED**

이 문서는 현재 Scanner 제품 동작과 기술 안전 계약의 canonical 전문 문서다. 역사적 v1.3.x/v1.4.x 보정 근거는 각 버전별 결정·릴리즈 문서에 보존하고, 현재 구현 판단은 이 문서와 `STATE.md`, 실제 코드가 우선한다.

## 1. 목적과 경계

Scanner는 Escape from Tarkov 화면 픽셀을 기존 JunhyunHelper Item ID에 연결하는 독립적인 입력 subsystem이다.

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
→ Mini Scanner
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

## 2. 현재 공개 기준선

```text
version: v1.5.0
exact release source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
296 passed / 0 failed / 0 skipped
release run: 32691423654 — SUCCESS
independent public verifier: 32691641614 — SUCCESS
asset: Junhyun-Helper-v1.5.0-win-x64.zip
bytes: 80,422,292
SHA-256: 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
ProductVersion: 1.5.0+6de738959740d12e6ccb81b65e50006e463eb699
public/latest: VERIFIED
exact public tag source: VERIFIED
public redownload/checksum/layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세:

- `docs/RELEASE_1.5.0.md`
- `docs/.release-v1.5.0-status.json`
- `docs/RELEASE_NOTES_V1.5.0.md`
- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`

## 3. 핵심 안전 불변식

다음 값은 v1.5.0 finishing work에서도 완화하지 않았다.

```text
structural floor = 0.34
trusted header floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
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
- full semantic gate 이전의 candidate는 diagnostics에는 남길 수 있으나 production OCR identity path에는 들어가지 않는다.
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

최소화되었거나 유효한 client-area가 없으면 인식하지 않는다.

### Display Test

연결된 전체 display에 실사용과 동일한 detector/OCR/catalog/presentation pipeline을 적용한다.

- 실제 Scanner continuous mode와 Display Test continuous mode는 상호 배타적이다.
- 개발·진단 surface이므로 일반 사용 UI에서는 `고급 / 진단` 아래에 둔다.

### One-shot

일반 Scanner 화면에 `1회 스캔` 버튼을 제공한다.

- 현재 TarkovWindow를 한 번 정밀 분석한다.
- continuous Scanner ON/OFF 상태를 영구 변경하지 않는다.
- local healthy catalog만 사용한다.
- scan-time network refresh를 시작하지 않는다.
- shared detector/OCR/presentation state와 직렬화한다.
- one-shot candidate cap은 12다.

전역 기본 단축키:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

MainWindow lifetime 동안 Scanner 탭을 보고 있지 않아도 동작한다. 동일 gesture 중복 지정은 허용하지 않는다.

## 5. Full Item identity catalog

Scanner identity catalog는 Needed Items subset이 아니라 현재 GameMode의 **공식 전체 Item catalog**다.

준비/업데이트 단계에서는 remote source를 사용할 수 있으나 실제 scan 순간에는 local/memory data만 사용한다.

Identity health 예시:

```text
accepted item count >= 4000
AND every accepted item has non-empty Item ID
AND every accepted item has non-empty official name
```

Market/dimension coverage는 identity health와 분리한다. 가격 데이터가 없다는 이유로 공식 Item identity 자체를 무효화하지 않는다.

Catalog/cache lifecycle은 GameMode operation ordering을 지켜 과거 mode의 느린 load/refresh가 최신 mode state를 덮어쓰지 못하게 한다.

## 6. Game Data update와 Scanner catalog 갱신

v1.5.0부터 사용자는 일반 Tarkov 데이터와 Scanner catalog를 별도로 갱신할 필요가 없다.

상단 Game Data update 흐름:

```text
remote Game Content fetch/build
→ general content validation/activation
→ current GameMode Scanner item/market catalog refresh
→ combined status report
```

계약:

- Scanner refresh만 실패하면 건강한 일반 Game Content를 rollback하지 않는다.
- 기존 healthy Scanner cache가 있으면 유지한다.
- partial failure를 상태로 보고한다.
- Scanner 탭의 `아이템 목록 최신화`는 일반 필수 절차가 아니라 고급/복구용이다.

## 7. Detail rectangle proposal

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

Initial structural rectangle은 authoritative final bounds가 아니다. Full semantic header lock 뒤 실제 magnifier/X evidence로 top/left/right를 refine하고, item/stat pane 높이 차이를 고려해 bottom은 구조 evidence를 보수적으로 사용한다.

## 8. Inspect-header semantic lock

Detail identity는 semantic anchors가 확립한다.

Required evidence:

1. **red close-X**
   - red dominance/body/edge
   - expected header-relative geometry
   - normalized diagonal-X contrast
2. **neutral inspect-header/frame**
3. **frame-left magnifier/search lane**
4. **normalized magnifier morphology**
   - ring
   - hollow center
   - handle
   - surrounding background
5. **dark item-title field**
6. **title text evidence**

Title glyph segmentation은 title ROI ownership을 결정하지 않는다. 실제 inspect-header frame과 semantic icon geometry가 title ROI를 소유한다.

Oversized/coarse proposal을 contained-subpanel fallback으로 복구하는 경우에도 동일 semantic gate를 다시 통과해야 한다.

Historical evidence:

- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`
- `docs/SCANNER_V1.3.4_LIVE_HARDENING.md`
- `docs/DECISION_SCANNER_HEADER_LOCK_2026-08-23.md`
- `docs/DECISION_SCANNER_V1.3.4_LIVE_HARDENING_2026-08-23.md`

## 9. OCR pipeline

Primary text recognizer는 Windows `ko-KR` OCR이다.

일반 순서:

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
- full text / line / 유효 조합을 official catalog와 비교할 수 있다.
- exact-first다.
- fuzzy matching은 conservative confidence + top1/top2 margin을 요구한다.
- historical alias를 무제한 production identity source로 누적하지 않는다.
- ambiguous/low-confidence는 Item ID 미확정이다.

## 10. Current-catalog character/symbol policy

`ScannerOcrCharacterPolicy`는 현재 official item-name catalog에서 실제 쓰는 문자/기호를 기준으로 matcher evidence를 정리한다.

- 정상 ASCII letter/digit는 noisy evidence로 유지한다.
- 공식 Item 이름에 실제 쓰는 quotes/hyphens/brackets 등은 보존한다.
- Korean title contract에서 허용되지 않는 Han ideograph 등은 invalid evidence로 취급한다.
- catalog-impossible Unicode glyph를 특정 `r`, `0`, `I`, `l` 등으로 자동 확정하지 않는다.

### Unknown-glyph bounded recovery

`Esma「ch` 같은 catalog-impossible embedded glyph는 특정 문자 치환이 아니라 `?` unknown-position evidence로 보존할 수 있다.

Recovery는 complete current catalog에서 pattern candidate가 유일하고 충분히 분리될 때만 허용한다.

- short/ambiguous pattern은 fail closed
- duplicate official-name ambiguity는 fail closed
- global runner-up margin 부족은 fail closed
- current catalog 밖 후보 생성 금지

### Bounded edit recovery

Ordinary matcher가 실패한 경우에도 complete current catalog에서 유일하고 충분히 분리된 bounded edit candidate만 제한적으로 복구한다.

일반 confidence/margin을 낮추는 대체 경로가 아니다.

## 11. User OCR substitutions — settings schema v5

v1.5.0은 반복해서 검증된 OCR 오인식을 사용자가 직접 보정할 수 있게 한다.

```text
raw OCR
→ enabled user substitutions
→ catalog sanitation / normalization
→ matching
```

계약:

- Scanner display settings schema: **v5**
- 기본 substitution list는 비어 있다.
- 사용자 exact 문자열/문자 치환만 저장한다.
- 규칙 추가 / 삭제 / 개별 ON·OFF / 초기화를 지원한다.
- raw OCR 원문은 절대 덮어쓰지 않는다.
- diagnostic에서는 raw OCR / user-substituted OCR / normalized / matched result를 구분한다.
- 한 ordered pass만 수행한다.
- replacement 결과를 다시 이전/다음 규칙 체인에 재귀적으로 넣지 않는다.
- 사용자 규칙은 automatic product algorithm의 global substitution table이 아니다.

대표 목적은 사용자가 실제 반복 확인한 `「` → `r` 같은 오인식을 자신의 환경에서 선택적으로 보정하는 것이다.

## 12. Tarkov-font visual corroboration / recovery

게임 font binary는 JunhyunHelper public package에 재배포하지 않는다.

```text
Tarkov resources.assets (read-only)
→ bounded SFNT discovery/extraction
→ %LocalAppData%/JunhyunHelper/scanner/fonts
→ source manifest + font SHA generation
→ Bender regular/bold + Korean fallback
→ rendered official item-name templates/features
```

- OCR semantic shortlist가 있으면 visual corroboration에 사용할 수 있다.
- OCR이 비거나 심하게 손상됐을 때 strict visual recovery를 시도할 수 있다.
- current official catalog 밖 후보 생성 금지
- visual top1과 margin 모두 요구
- font unavailable/error/ambiguous이면 건강한 OCR evidence를 임의 폐기하지 않는다.
- font/template cache는 generation-aware + bounded다.

## 13. OCR serialization과 same-cycle reuse

Title OCR과 Mini Scanner inventory-context OCR은 하나의 WinRT OCR serialization boundary를 공유한다.

v1.5.0의 정확도 보존 최적화:

- **같은 active scan cycle 안에서만** 재사용 가능
- exact key는 bitmap width/height/BPP + pixel SHA-256을 포함
- pixel이 완전히 동일한 경우에만 WinRT OCR output을 재사용
- normal/deep cache를 분리
- cycle이 바뀌면 cache를 폐기
- frame 간 OCR cache 없음
- 과거 frame 결과로 현재 evidence를 대체하지 않음

## 14. Stage latency telemetry

v1.5.0은 성능을 추측으로 조정하지 않고 stage별 비용을 측정한다.

측정 stage:

- `capture`
- `rectangle-proposal`
- `semantic-header`
- `ocr-normal`
- `ocr-deep`
- `visual-recovery`
- `catalog-matching`
- `presentation`
- `endToEndMs`

Telemetry는 scan operation/cycle 단위로 연결된다. Detector-only continuous cycle은 log churn을 줄이기 위해 sampling할 수 있고, semantic/one-shot cycle은 상세 계측한다.

성능 개선 우선순위는 threshold 완화가 아니라 duplicate work, OCR 호출 수, bitmap copy/convert, visual/catalog recovery 비용을 evidence 기반으로 줄이는 것이다.

## 15. Continuous result stabilization

기존 raw BGRA title hash는 harmless dark background/GPU pixel variation에도 바뀔 수 있었다.

v1.5.0은 already-verified detail의 continuity를 위해 title-ink shape 기반 stable signature를 사용한다.

- dark background mode를 추정
- 의미 있는 밝은 title ink shape만 사용
- trailing unused title ROI width를 identity에서 제외
- 같은 glyph shape + 작은 background noise는 같은 continuity signature가 될 수 있음
- visible glyph shape가 바뀌면 signature가 달라짐
- visible title ink가 없으면 fail closed

중요:

- 이 signature는 **Item identity를 확립하지 않는다.**
- semantic gate를 통과한 trusted result를 잠깐 안정화하는 continuity evidence일 뿐이다.
- geometry/title identity가 실제로 달라지면 이전 trusted result를 즉시 해제한다.
- generic detector miss는 기존 bounded miss policy를 유지한다.

## 16. Scanner mapped presentation

Production OCR field는 **`item_name` 하나**다.

아래는 Item ID 확정 뒤 local trusted data에서 조회/계산한다.

### 최고 상점가

유효한 non-flea 판매처의 RUB 환산 가격 중 최댓값.

가능하면 그 가격을 제공하는 최고가 상인명도 표시한다.

### 플리마켓 평균가

positive `avg24hPrice`.

### Slots / price per slot

```text
slots = positive width × height
```

가격과 slots가 모두 유효할 때만 trader/flea price-per-slot을 계산한다.

### 필요한 개수

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

Inventory를 차감한 shortage가 아니다.

Market/dimension 일부 누락이나 parsing 실패는 해당 표시 필드만 fail closed하고 이미 확정된 Item identity를 버리지 않는다.

## 17. Ground Truth / correction

기본 저장 root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

자동 diagnostic Case는 정답이 아니다. 사용자가 검토/교정한 Case만 Ground Truth다.

### Candidate-first correction

v1.5.0 기본 교정 흐름:

1. detail rectangle candidate 선택
2. red close-X candidate 선택
3. magnifier candidate 선택
4. item-name ROI candidate 선택
5. correct item/text 지정
6. 저장

각 선택에는 candidate ID/rank/score/geometry를 함께 보존한다.

Fallback:

- 정답 candidate가 없으면 manual rectangle 지정
- detector가 semantic object 자체를 만들지 못했다면 `없음` 기록 가능

이 구조로 다음 failure를 분리한다.

- proposal recall miss
- proposal ranking problem
- close-X semantic miss
- magnifier semantic miss
- title ROI miss
- OCR/matcher miss

## 18. Diagnostic Case / regression

대표 evidence:

- `full.png`
- detail/title/processed ROI
- annotated image
- `case.json`
- raw OCR
- user-substituted OCR
- normalized matcher input
- final matched Item ID/name
- confidence / second score / margin
- matcher top candidates
- structural/header evidence
- selected detector candidates
- mapped presentation
- user Ground Truth

사용자가 명시적으로 저장/export하는 경우 full capture pixels가 진단 데이터에 포함될 수 있다.

Full-pipeline regression:

```text
reviewed full.png
→ current proposals
→ current semantic header lock
→ current title ROI
→ current OCR/deep OCR/user substitution/visual recovery
→ current official catalog matching
→ final Item ID
```

결과:

- `STILL_CORRECT`
- `SOLVED`
- `STILL_FAILING`
- `REGRESSION`
- `ERROR`

기존 정상 reviewed Case가 새 코드에서 실패하면 평균 성능이 좋아져도 regression이다.

## 19. Diagnostics/log retention

사용자-reviewed Ground Truth는 자동 삭제하지 않는다.

자동 삭제 대상은 다음 둘을 동시에 만족한 Case뿐이다.

```text
retention == automatic_sample
AND review_status == unreviewed
```

기본 bound:

```text
max age: 30 days
max automatic cases: 300
max automatic bytes: 512 MiB
recent-case safety window: 2 hours
```

- corrupt/unknown metadata는 fail closed하여 보존
- 삭제 직전 metadata를 다시 읽어 correction/delete race를 줄임
- retention 삭제가 발생하면 diagnostic log에 기록

로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
%LocalAppData%/JunhyunHelper/logs/startup.log
%LocalAppData%/JunhyunHelper/logs/startup.log.1
```

Scanner/startup logs는 bounded rotation을 사용한다.

`로그 삭제`는 recent activity + scanner log(.1) + latest in-memory recognition image를 정리한다. Ground Truth dataset과 사용자가 export한 파일은 별개다.

## 20. Scanner UI

### 일반 surface

- `스캐너` ON/OFF
- `1회 스캔`
- `현재 결과 교정`
- runtime status
- 최근 인식 기록

### `설정`

- global hotkeys
- 사용자 OCR substitutions
- Mini Scanner 표시 설정

### `고급 / 진단`

- Display Test
- 인식 이미지
- regression
- Ground Truth 내보내기
- Ground Truth 관리
- Scanner catalog `아이템 목록 최신화`/복구
- 로그 삭제
- diagnostic storage 정보

기능 자체를 삭제하지 않고 일반 플레이 surface와 developer/recovery surface를 분리한다.

## 21. Mini Scanner

- match 성공 Item 정보만 overlay에 표시
- runtime/OCR/error/status text는 overlay에 노출하지 않음
- WPF Topmost + native topmost
- no-activate
- 전체 카드 drag surface
- 실제 Scanner mode에서는 Tarkov foreground + inventory/stash context를 보수적으로 확인
- inventory-context OCR probe는 single-active
- repeated request는 latest snapshot으로 coalesce
- item/context epoch가 바뀐 stale result는 reject
- scan 순간 icon HTTP 없음

### 빠른 현재 결과 교정

Mini Scanner context menu의 `현재 결과 교정`은 latest `ScannerRecognitionDebugStore` snapshot과 current Scanner coordinator를 사용해 correction window를 바로 연다.

오인식 직후 몇 초 안에 reviewed Ground Truth로 남길 수 있는 동선을 목표로 한다.

## 22. Icon / local cache

- Game Content update/cache 준비 단계에서 canonical item icon을 준비
- scan 순간 HTTP 없음
- local image cache 사용
- 개별 icon 실패는 Item identity/catalog 전체를 fatal로 만들지 않음
- decode/freeze된 healthy icon은 process-local cache 재사용 가능

## 23. Persistence

대표 위치:

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/
%LocalAppData%/JunhyunHelper/scanner/fonts/
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
%LocalAppData%/JunhyunHelper/logs/
```

Scanner settings/cache/fonts/log/diagnostics는 program package와 분리한다. Program Update는 이 사용자 데이터를 덮어쓰지 않는다.

## 24. 실제 Tarkov calibration 운영

v1.5.0은 public verified baseline이지만 Scanner 개선은 실제 사용 evidence를 계속 축적한다.

권장 실사용 loop:

```text
real Tarkov usage
→ 정상 대표 결과 `맞음`
→ miss/wrong identity 직후 `현재 결과 교정`
→ reviewed Ground Truth 축적
→ diagnostics export / regression replay
→ failure stage 특정
→ 해당 stage만 수정
→ 전체 reviewed dataset replay
→ REGRESSION=0 확인
```

특히 계속 관찰할 영역:

- 다양한 해상도/DPI/UI scale
- tall/large detail windows
- stash/inventory frames와 겹치는 proposals
- short/sparse titles
- `r`, `0`, slash-zero-like glyph, complex Hangul
- punctuation item names
- near-name ambiguity false positive
- Item ID 성공 뒤 mapped market data completeness
- 빠른 item 전환 stale-result isolation
- 장시간 CPU/memory/UI responsiveness
- latency telemetry에서 실제 병목이 되는 OCR/visual recovery stage

문제가 생기면 다음 순서로 원인을 분리한다.

```text
capture
→ proposal
→ close-X / magnifier / semantic header
→ locked bounds / title ROI
→ raw OCR
→ user substitution
→ sanitation / matcher
→ visual recovery
→ Item ID
→ mapped presentation
→ overlay
```

## 25. 공개 검증

v1.5.0에서 검증 완료:

- final PR #172 release-candidate CI `32688080850` — SUCCESS
- 296 tests / 0 failed / 0 skipped
- Windows x64 self-contained single-file publish
- exact ProductVersion / FIRST_RUN identity
- package root/forbidden dependency audit
- Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke
- graceful shutdown / clean portable root
- exact source tag `6de738959740d12e6ccb81b65e50006e463eb699`
- release run `32691423654` — SUCCESS
- draft asset redownload/hash/identity/EXE smoke
- public stable/latest publication
- independent fresh-runner public verifier `32691641614` — SUCCESS
- anonymous public ZIP + SHA256SUMS redownload
- public hash/size/layout/ProductVersion/FIRST_RUN verification
- public-downloaded EXE Product UI/Map/Scanner smoke
- durable `docs/.release-v1.5.0-status.json`
- one-shot release/verifier workflows removed; steady-state `ci.yml` only

공개 asset:

```text
Junhyun-Helper-v1.5.0-win-x64.zip
80,422,292 bytes
SHA-256 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
ProductVersion 1.5.0+6de738959740d12e6ccb81b65e50006e463eb699
```

## 26. 역사 문서

현재 계약을 이해한 뒤 필요한 경우에만 과거 evidence를 참고한다.

- `docs/SCANNER_LAB_3_8_REFERENCE.md`
- `docs/SCANNER_V1.3.2_LIVE_EVIDENCE.md`
- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`
- `docs/SCANNER_V1.3.4_LIVE_HARDENING.md`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/DECISION_SCANNER_HEADER_LOCK_2026-08-23.md`
- `docs/DECISION_SCANNER_V1.3.4_LIVE_HARDENING_2026-08-23.md`
- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`

과거 문서의 version-specific UI/schema 설명이 이 현재 계약과 충돌하면 **이 문서와 현재 코드가 우선**한다.
