# Scanner v1.1.5 Test Plan

기준일: 2026-08-21

상태: **`PUBLIC RELEASE GATE PASSED / v1.1.5 PUBLIC VERIFIED / INDEPENDENT PUBLIC VERIFICATION PASSED / LIVE TARKOV E2E POST-RELEASE`**

이 문서는 v1.1.5의 자동/Windows/public release gate와 공개 후 실제 Tarkov 환경 검증을 분리합니다.

## 1. Release blocking gate — 완료

1. Windows Release Desktop build
2. 전체 automated tests 0 failure / 0 skipped
3. Scanner Lab v3.8 structural/title ROI regression
4. current official Korean catalog/matcher regression
5. raw `traderPrices` / derived `sellFor` market regression
6. market-health rejection regression
7. Tarkov title-font SFNT parser / Hangul fallback smoke
8. win-x64 self-contained single-file publish
9. ProductVersion = `1.1.5+<exact release source>`
10. FIRST_RUN first line = v1.1.5
11. package root/dependency/PDB/nested-archive audit
12. actual published EXE startup
13. rendered Product UI + Scanner/Mini Scanner assertions
14. Main Map / Factory / MiniMap runtime smoke
15. graceful Main Window close/process exit
16. Draft ZIP/checksum/package/ProductVersion/FIRST_RUN verification
17. Draft-downloaded EXE smoke
18. public/latest 전환
19. exact public tag → release source SHA verification
20. public ZIP/checksum/package/ProductVersion/FIRST_RUN 재검증
21. public-downloaded EXE smoke + graceful shutdown
22. separate runner에서 public metadata/tag/package를 독립 재검증
23. independently downloaded public EXE smoke + graceful shutdown

실제 최신 Tarkov 실행 E2E와 current `resources.assets` extraction은 environment-dependent empirical validation이므로 public release blocker가 아닙니다. 다만 실패 시 반드시 fail-closed 또는 기존 OCR-only fallback이어야 합니다.

## 2. Scanner Lab v3.8 recognition regression

반드시 유지:

- RED-X connected-component path
- RED-X anchored outer-window reconstruction
- rectangle/edge fallback
- IoU candidate deduplication
- runtime candidate limit 8
- structural floor 0.34
- geometry alone으로 final inspect 확정 금지
- adaptive 4x/6x/8x Windows ko-KR OCR
- 상위 3개 Deep OCR fallback
- current official Korean full-item catalog semantic validation
- exact-first matcher
- confidence/top1-top2 margin 유지
- low-confidence/ambiguous → no Item ID

고정 구조 회귀:

- cropped `Ophthalmoscope 검안경`: outer inspect/title ROI
- full `Water 0.6L 물병` screenshot: central inspect/title ROI
- strong inner rectangle coexistence
- no RED-X rectangle fallback
- uniform frame fail-closed

## 3. Candidate/runtime 안정화

검증 계약:

- candidate가 없으면 stable hit = 0
- 서로 다른 geometry signature만 이어지면 2-hit stable로 승격하지 않음
- 연속 candidate 집합에 같은 quantized `GeometrySignature`가 있을 때만 stable hit 누적
- mode/change/miss/reset에서 previous signature history clear
- verified bounds + title signature가 유지되면 OCR 반복 억제
- title/geometry 변화 시 기존 Item clear 후 재검증
- same verified detail은 OCR 없이 1초 presentation refresh만 수행
- `RequiredTotal` 등 presentation data 변화가 같은 상세창에서도 반영됨

## 4. OCR concurrency — v1.1.5

`ScannerLab38OcrEngine` 위에 하나의 `SerializedScannerOcrEngine`을 둡니다.

검증 계약:

- Item title OCR과 inventory-context OCR이 concurrent WinRT OCR call을 만들지 않음
- shared serialized boundary가 availability/error 의미를 변경하지 않음
- Item-title runtime만 `FontAwareScannerOcrEngine`을 사용
- inventory-context detector는 serialized OCR을 직접 사용하고 Item-font recovery를 호출하지 않음
- OCR serialization 때문에 Scanner mode switching/shutdown에서 deadlock이 발생하지 않음

## 5. Tarkov title-font recovery — v1.1.5

### 5.1 기본 동작

확인된 UI contract:

- inspect top Item name = `ItemInfoWindowLabels._caption`
- primary font = Bender family
- Korean fallback = `Noto Sans CJK KR`

검증 계약:

1. normal OCR은 기존 inner engine 결과를 그대로 반환한다.
2. Deep OCR은 기존 Deep OCR을 먼저 수행한다.
3. 기존 `_catalog.ResolveOcrText(text)`가 success이면 font verifier를 실행하지 않고 원 결과를 유지한다.
4. 기존 Deep OCR semantic failure일 때만 font-aware recovery를 시도한다.
5. shortlist는 current official Korean full-item catalog에서만 만든다.
6. Bender Regular/Bold와 Noto KR fallback으로 official name을 렌더링한다.
7. observed title ROI와 rendered glyph mask를 scale/tolerance를 두고 비교한다.
8. semantic + visual + combined + top1/top2 margin을 모두 통과해야 accept한다.
9. short official name은 더 엄격한 threshold를 사용한다.
10. ambiguous/weak result는 recovery하지 않는다.
11. recovered name도 기존 catalog resolver를 통해 Item ID로 연결한다.
12. font shape만으로 standalone Item identity를 만들지 않는다.

### 5.2 Font provider / cache

`TarkovTitleFontProvider` 검증:

- running `EscapeFromTarkov` executable path에서 `EscapeFromTarkov_Data/resources.assets` 탐색
- game asset read-only
- embedded SFNT `OTTO` / TrueType signature 탐색
- SFNT table directory/offset/length bounds validation
- SkiaSharp actual typeface metadata로 Bender/Noto family 확인
- Bender Regular/Bold, Noto Sans CJK KR Regular만 app-local cache
- public package에 Bender binary 없음
- source asset mtime이 cache보다 최신이면 stale cache를 정상으로 사용하지 않음
- asset missing/unreadable/oversize/invalid metadata → font recovery unavailable
- font recovery unavailable이어도 기존 OCR-only pipeline은 정상 동작
- game directory write 없음

Published-EXE smoke에 포함된 deterministic contract:

- synthetic valid SFNT parser acceptance
- invalid zero-table SFNT rejection
- Hangul fallback segmentation: `가` = Korean fallback, `A` = Bender path

실제 Tarkov `resources.assets` binary 자체는 CI에 없으므로 live extraction은 §13에서 별도로 검증합니다.

## 6. Catalog / market data — v1.1.5

Full catalog:

- 4,000개 이상 current Korean Item load
- regular / pve / pvp-season
- Korean translation + English per-key fallback
- corrupt/missing cache reject
- requested mode missing 시 wrong-mode identity 사용 금지
- AtomicJson backup recovery

Market health:

- valid Item count >= 4,000
- positive best-trader coverage >= 500
- name만 정상이고 market coverage가 비정상적으로 비어 있는 candidate는 reject
- unhealthy candidate가 known-good in-memory/cache data를 덮지 못함

Market projection:

- raw JSON `traderPrices` positive `priceRUB`가 있으면 최댓값 사용
- raw trader data가 없으면 derived `sellFor` fallback
- `sellFor`에서는 flea source를 best trader 계산에서 제외
- flea row가 trader보다 높아도 trader price에 사용하지 않음
- 플리 평균가는 `avg24hPrice > 0`만 사용
- zero/missing `avg24hPrice` → null
- invalid/non-positive dimension → slots 0, price/slot null
- valid price + slots → integer price/slot

자동 회귀:

- raw 4,000-Item `traderPrices` fixture가 trader price와 trader price/slot을 채움
- 4,000-Item market-empty catalog가 reject됨
- 기존 market/dimension projection regression 유지

## 7. 현재 필요한 수량

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

검증:

- Inventory 차감 부족량을 Scanner 의미로 사용하지 않음
- NeededItems에 없으면 0
- presentation snapshot을 주기적으로 다시 구성
- Quest/Hideout 진행으로 `RequiredTotal`이 바뀌면 같은 상세창을 열어 둔 상태에서 최신 값 반영
- presentation refresh 자체는 OCR을 재실행하지 않음

## 8. Icon / performance — v1.1.5

- scan-time icon HTTP 없음
- local image-cache만 사용
- invalid/missing local icon은 해당 icon 표시만 omit
- 성공적으로 decode/freeze한 동일 stableId+URL icon은 process memory cache 재사용
- presentation refresh가 같은 PNG file decode를 반복하지 않음
- explicit Game Content update 시 Quest/Hideout/Ammo subset이 아니라 **전체 canonical Item catalog**의 icon URL을 prefetch queue에 포함
- existing valid cached PNG는 재다운로드하지 않음
- 개별 image failure는 전체 content update fatal 아님

## 9. Scanner UI

유지:

- `스캐너 OFF`
- `테스트 OFF`
- `아이템 목록 최신화`
- 7개 display checkbox
- recent recognition activity
- activity header 우측 `로그 삭제`

없어야 함:

- 위치 편집/초기화
- Foundation verification/preview controls
- 상시 설명문

## 10. Mini Scanner — v1.1.5

### 표시 contract

- matched Item information만 표시
- waiting/runtime/OCR/error/diagnostic text element 없음
- Scanner OFF → hidden
- unresolved/uncertain Item → hidden
- MiniMap과 독립 lifecycle

### Window/input contract

- WPF `Topmost=True`
- render/show/drag 이후 native `HWND_TOPMOST` 재assert
- `ShowActivated=false`
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`
- root card 전체가 hit-testable drag surface
- text/icon child 위에서 mouse-down해도 drag 가능
- nonzero-alpha near-transparent background로 WPF layered hit test 보장
- `ForceCursor=True`, Arrow cursor
- drag 종료 위치 저장
- negative multi-monitor coordinate 허용

### Display settings migration

schema v2 one-time migration에서 intended defaults:

- Item icon ON
- trader price ON
- trader price/slot ON

migration 이후 사용자의 checkbox 변경은 persist해야 합니다.

### Published WPF smoke

실제 published EXE에서:

- `ScannerStatusText`가 존재하지 않음
- root `DragSurface`가 hit-testable
- `ForceCursor=True`
- cursor = Arrow
- background alpha > 0
- topmost/noactivate/taskbar contract
- sample Item의 trader 42,000 표시
- trader/slot 21,000 표시
- render 이후 window visible/topmost 유지

## 11. Inventory/stash auto visibility — v1.1.5

실사용 gate:

```text
matched Item wants overlay
→ foreground window == EscapeFromTarkov
→ valid visible/non-minimized client area
→ top client strip capture
→ regular ko-KR OCR
→ 필요 시 Deep OCR
→ semantic anchors >= 2
→ overlay allow
```

현재 anchors:

- `장비`
- `건강상태` / `건강 상태`
- `스킬`
- `지도`
- `종합정보` / `종합 정보`

검증 계약:

- 다른 app foreground → hidden
- minimized/invalid client → hidden
- OCR uncertain / anchor < 2 → hidden
- decision short cache 약 850ms
- raw pixels/screenshots persist 금지
- only matched Item presentation 시점에 context probe
- Display-test/explicit preview는 deterministic smoke를 위해 bypass
- Borderless/windowed target; exclusive fullscreen support는 claim하지 않음

## 12. Diagnostics / log clear

Developer log:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

유지할 metadata events:

- candidate structural/OCR/match/semantic-selected/runtime
- `inventory-context`
- `title-font-cache-load-failed`
- `title-font-extract-ready`
- `title-font-extract-missing`
- `title-font-extract-failed`
- `title-font-verify-accepted`
- `title-font-verify-rejected`
- `title-font-recovery-error`

저장 금지:

- screenshot
- raw pixel buffer

`로그 삭제` end-to-end smoke:

1. diagnostic/activity baseline clear
2. diagnostic/activity 생성
3. current/rotated scanner log file 생성
4. rendered `로그 삭제` Button Click
5. activity = 0
6. `scanner.log` 없음
7. `scanner.log.1` 없음

삭제 I/O 실패는 recognition fatal로 확대하지 않습니다.

## 13. Public release verification — 완료

Exact release source/tag:

```text
3541bab6536ff91a00f394c4f7b03d5cbf112746
```

Final candidate CI:

```text
run: 32493986403 — SUCCESS
249 passed / 0 failed / 0 skipped
published EXE product smoke: SUCCESS
```

Exact-source first release run:

```text
run: 32494487841
build/test/publish/package/exact EXE smoke/ZIP/Draft creation: SUCCESS
final Draft tag-ref check: workflow-ordering FAILURE
```

이 failure는 Draft 상태에서는 public Git tag ref가 아직 생성되지 않는 GitHub lifecycle을 즉시 조회한 자동화 순서 문제였습니다. 제품/패키지 gate와 Draft 생성은 이미 통과했으며 public transition 전 failure였습니다.

Draft resume/public verification:

```text
run: 32495042444 — SUCCESS
Draft identity/asset re-download: VERIFIED
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
public tag exact source: VERIFIED
public asset re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

Independent public verification:

```text
run: 32495225958 — SUCCESS
public metadata/latest/tag: VERIFIED
public asset independent re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
normal shutdown / clean portable root: SUCCESS
```

최종 공개 패키지:

```text
asset: Junhyun-Helper-v1.1.5-win-x64.zip
bytes: 80,269,429
SHA-256: dc31177ae1bd4d152453a010dffe6cbb1e6c1d2a4a7e2eb82fb7444fa99c0748
ProductVersion: 1.1.5+3541bab6536ff91a00f394c4f7b03d5cbf112746
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v1.1.5
```

최종 run/source/hash/bytes는 `docs/RELEASE_1.1.5.md`, `docs/STATE.md`, `docs/CURRENT_STATE.md`에 고정합니다.

## 14. 공개 후 실제 Tarkov 검증

CI runner에는 실제 current Tarkov 설치가 없으므로 다음은 사용자 환경에서 우선 검증합니다.

1. 실제 Borderless detail candidate 안정성
2. foreground inventory/stash Korean anchor gate
3. current `resources.assets`에서 Bender Regular/Bold + Noto Sans CJK KR 추출
4. normal/Deep OCR 성공률
5. font-aware recovery의 accept/reject 품질
6. mixed Korean/Latin Item names의 glyph fallback
7. false positive / miss 비율
8. 다양한 Item의 최고 상점가 / 플리 평균가 / price per slot
9. 현재 필요한 수량 live refresh
10. Mini Scanner topmost/noactivate/full-card drag/Arrow cursor
11. Mini Scanner / MiniMap / Alt+Tab 공존
12. 장시간 CPU/memory/handles/OCR rate

Font/context 문제는 `scanner.log`의 `inventory-context`, `title-font-*`, candidate/OCR/match metadata로 진단합니다.

실사용 보정 원칙:

- context anchor가 불확실하면 overlay hidden 유지
- font extraction이 실패하면 OCR-only fallback 유지
- visual evidence가 약하면 no Item ID 유지
- matcher confidence/top1-top2 margin을 낮춰 false positive를 늘리지 않음
- 구조/anchor/font parser 문제는 후속 PATCH에서 국소적으로 수정
