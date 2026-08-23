# Scanner — 제품/기술 계약

기준일: 2026-08-23

상태: **`v1.3.4 PUBLIC VERIFIED / LIVE TARKOV CALIBRATION ONGOING`**

## 1. 목적

Scanner는 Tarkov 화면을 기존 JunhyunHelper Item ID와 진행/가격 데이터에 연결하는 화면 기반 입력 bridge입니다.

```text
화면 픽셀
→ structural detail candidates
→ red close/X + normalized X template
→ actual long neutral inspect-header frame
→ fixed frame-left search-icon lane
→ normalized magnifier ring/hollow/handle template
→ dark title field + text evidence
→ full HEADER_FRAME_LOCKED only
→ locked-header-based detail bounds refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog character/symbol sanitation
→ optional one-unknown-glyph current-catalog recovery
→ current official Korean catalog semantic match
→ bounded unique one-edit recovery when safe
→ optional Tarkov-font visual corroboration/recovery
→ conservative confidence + top1/top2 margin
→ Item ID or fail closed
→ JunhyunHelper local presentation data
→ Mini Scanner
```

오탐(false positive)은 미탐(false negative)보다 나쁩니다.

금지:

- game memory read
- DLL injection
- packet interception
- process-internal game data read
- icon/image 단독 identity 확정
- scan 순간 HTTP/API
- current official catalog 밖 임의 Item/text 생성
- current-catalog 밖 glyph를 특정 문자로 임의 치환
- live evidence 없이 acceptance threshold 완화

## 2. 현재 공개 기준선

```text
version: v1.3.4
release source/tag: a78ddbc649747f1320236556f17e6b908304674a
final PR CI: 32636665202 — SUCCESS
267 passed / 0 failed / 0 skipped
release run: 32636927134 — SUCCESS
independent public verifier: 32637159066 — SUCCESS
asset: Junhyun-Helper-v1.3.4-win-x64.zip
bytes: 80,319,654
SHA-256: 8c442fec81a0b993a9a6b080e59b656668a7a73d8fadd8434595545b08c82e8e
ProductVersion: 1.3.4+a78ddbc649747f1320236556f17e6b908304674a
public/latest: VERIFIED
exact public tag source: VERIFIED
Draft/public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세: `docs/RELEASE_1.3.4.md`, `docs/.release-v1.3.4-status.json`.

## 3. Capture modes

### TarkovWindow

```text
EscapeFromTarkov process/window
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ PrintWindow 우선
→ invalid/empty이면 exact client screen rectangle fallback
```

최소화/유효하지 않은 client-area에서는 인식하지 않습니다.

### DisplayTest

모든 연결 디스플레이를 대상으로 실사용과 동일한 detector/OCR/catalog/presentation pipeline을 사용합니다. 실제 Scanner continuous mode와 test continuous mode는 상호 배타적입니다.

### One-shot

- 인게임 1회: 현재 TarkovWindow를 한 번 정밀 분석
- 테스트 1회: 모든 연결 디스플레이를 한 번 정밀 분석
- continuous mode를 영구 변경하지 않음
- scan-time catalog network refresh 없음
- shared detector/OCR/presentation state와 직렬화

## 4. Full Item identity catalog

Scanner identity catalog는 Needed Items subset이 아니라 Tarkov 전체 Item입니다.

준비/최신화 단계에서는 공식 데이터 source를 사용할 수 있지만 실제 scan 중에는 local/memory data만 사용합니다.

Identity health:

```text
accepted item count >= 4000
AND every accepted item has non-empty Item ID
AND every accepted item has non-empty official name
```

시장 데이터 coverage는 identity health와 분리합니다.

`ScannerCatalogService.LoadCacheAsync`와 `RefreshAsync`는 동일 operation gate를 사용하여 이전 GameMode operation이 최신 state를 뒤늦게 덮어쓰지 못합니다.

## 5. Structural detail-window detector

Scanner Lab v3.8의 RED-X/rectangle architecture를 candidate discovery 기준으로 유지합니다.

```text
capture
→ RED-X connected-component candidates
+
→ rectangle/edge fallback candidates
→ IoU deduplication
→ 최대 8 candidates
```

- structural floor `0.34`
- aspect/border/interior evidence
- structural score는 후보 순위이며 final Item identity가 아님
- continuous path에서 동일 quantized geometry가 안정화된 뒤 semantic recognition
- verified bounds/title signature가 유지되면 불필요한 OCR 반복 억제

v1.3.4부터 initial structural rectangle은 discovery seed입니다. Full header lock 이후 authoritative magnifier/X 위치에서 detail-window top/left/right를 다시 정렬합니다. Item/stat pane 높이는 달라질 수 있으므로 bottom은 기존 structural bottom을 유지합니다.

## 6. Inspect-header lock

### v1.3.3 — structural ownership

실제 2048×1280 Tarkov 상세창 12개에서 title start가 first glyph segmentation에 끌려가거나 magnifier association이 흔들리는 회귀를 근거로 `ScannerInspectHeaderLock`을 authoritative title-geometry layer로 사용합니다.

first Korean/title glyph connected component는 title ROI의 left edge를 결정하지 않습니다.

### v1.3.4 — fixed icon lane + shape template

v1.3.3 공개 후 title glyph가 magnifier처럼 보이며 후보로 승격되는 실전 failure를 차단했습니다.

Required structure:

1. **red close/X** — red body/edge + expected header geometry + diagonal X contrast
2. **long neutral top frame** — 실제 inspect header의 수평 frame
3. **fixed frame-left search-icon lane** — title lane과 분리된 magnifier 전용 후보 공간
4. **normalized magnifier template** — ring + hollow center + lower-right handle + outside background
5. **dark title field**
6. **title text presence**

magnifier location/size는 close height에 대해 scale-normalized 합니다.

```text
scale ≈ close.Height / 17
expected x ≈ frame.Left + 12 * scale
expected y ≈ frame.Top + 7 * scale
expected size ≈ 13 * scale
```

title glyph는 fixed search-icon lane 밖이면 morphology가 유사해도 magnifier candidate pool에 들어갈 수 없습니다.

Runtime gate:

```text
TitleImage exists
AND valid title bounds
AND valid magnifier bounds
AND valid close bounds
AND TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
```

partial/failed lock candidate는 OCR 후보 목록에서 제거합니다.

### Live evidence

12개 실제 screenshot 자체는 저장소에 커밋하지 않습니다. 비식별 header-relative 측정값만 유지합니다.

- `docs/.scanner-v1.3.3-header-evidence.json`
- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`

v1.3.4의 실제 failure와 최종 설계:

- `docs/SCANNER_V1.3.4_LIVE_HARDENING.md`
- `docs/DECISION_SCANNER_V1.3.4_LIVE_HARDENING_2026-08-23.md`

packaged-EXE smoke는 기존 12개 measured geometry와 title-lane decoy ring regression을 계속 실행합니다.

## 7. OCR / current-catalog sanitation / semantic matching

Primary text recognizer는 Windows `ko-KR` OCR입니다.

- title pixel height에 따라 4x/6x/8x 확대
- first-pass 실패 시 high-contrast/binary/inverse deep OCR
- full text / line / 유효 조합을 current catalog와 비교
- exact-first
- fuzzy confidence + top1/top2 margin
- ambiguous/low-confidence → Item ID 미확정
- historical alias를 production identity source로 무제한 누적하지 않음

`ScannerOcrCharacterPolicy`는 current official Korean item-name catalog에서 허용 문자/기호 집합을 파생합니다.

- raw OCR은 진단 정보로 그대로 유지
- matcher에는 sanitation 후 ordinary text를 전달
- current catalog에 존재하지 않는 punctuation/symbol은 ordinary matcher evidence에서 제거
- Korean item-title contract에서 Han ideograph는 invalid evidence
- 특정 OCR 문자를 임의 치환해 confidence를 올리지 않음

### v1.3.4 one-unknown-glyph evidence

실제 `Esma「ch`처럼 current-catalog 밖 symbol이 영숫자 사이에 정확히 한 번 나타나는 경우 ordinary text와 별도로 `?` pattern을 보존할 수 있습니다.

```text
raw:      Esma「ch 에스마르호 지혈대
ordinary: Esmach 에스마르호 지혈대
pattern:  Esma?ch 에스마르호 지혈대
```

`?`는 특정 문자에 대한 치환이 아니라 한 glyph 위치가 미상이라는 evidence입니다.

복구 조건:

```text
normalized pattern length >= 7
AND exactly one ?
AND same-length exact-slot candidate is unique over complete current catalog
AND duplicate official name이 아님
AND best - global wildcard runner-up >= 10 percentage points
```

- short title에는 적용하지 않음
- ambiguous candidate는 fail closed
- global margin 부족은 fail closed
- current official catalog 밖 후보 생성 금지

### bounded one-edit recovery — v1.3.2부터 유지

```text
normalized official length >= 7
AND edit distance == 1
AND candidate is unique over the complete current catalog
AND candidate is ordinary matcher top1
AND best - global runner-up >= 10 percentage points
```

이는 single-edit 오류만 제한적으로 복구합니다. multi-edit low-confidence OCR을 percentage만으로 확정하지 않습니다.

## 8. Tarkov-font visual corroboration / recovery

게임 font binary는 JunhyunHelper public package에 넣지 않습니다.

```text
Tarkov resources.assets (read-only)
→ bounded SFNT discovery/extraction
→ %LocalAppData%/JunhyunHelper/scanner/fonts
→ source manifest + actual font SHA generation
→ Bender regular/bold + Noto CJK KR
→ rendered current official item-name templates/features
```

- plausible OCR variant가 있으면 semantic shortlist + visual verifier
- OCR이 비거나 심하게 손상되면 strict full-catalog visual matcher
- visual top1 + top1/top2 margin 모두 필요
- current official catalog 밖 후보 생성 금지
- semantic OCR success와 visual이 다를 때 strict visual evidence가 명확한 경우에만 current catalog 안에서 correction 허용
- font unavailable/renderer error/ambiguous이면 healthy OCR을 임의로 폐기하지 않음
- Font/template cache는 generation-aware + bounded

## 9. Scanner UI / global hotkeys

Scanner display settings schema: **v4**.

기본 global hotkeys:

- 1회 인게임: `Ctrl+Shift+F10`
- 1회 테스트: `Ctrl+Shift+F11`
- Scanner ON/OFF: `Ctrl+Shift+F12`

- MainWindow lifetime 동안 Scanner 탭 밖에서도 동작
- Scanner 탭 `단축키 설정`에서 변경/비활성화
- 동일 gesture 중복 지정 금지
- schema v3 old one-shot gesture는 인게임 one-shot으로 우선 보존
- 신규 command default와 충돌하면 신규 command 쪽만 fallback
- one-shot 인게임/테스트 버튼은 제품 UI에 없음

## 10. 인식 이미지 / 로그

`인식 이미지`는 최신 diagnostic frame 1개를 메모리에 유지합니다.

진단 정보:

- capture source/origin
- selected detail bounds
- title ROI
- magnifier / close bounds
- structural/header evidence
- OCR/visual pass
- **raw OCR**
- **sanitized matcher input**
- official candidate
- confidence / second score / reason

자동 screenshot 저장은 없습니다.

`이미지 저장`을 명시적으로 사용하면 실제 capture 위에 detector rectangle을 합성한 사용자 지정 PNG를 export합니다.

- **Lime/초록 3px**: selected detail
- **DeepSkyBlue/파랑 2px**: OCR title ROI
- **Gold/노랑 2px**: magnifier
- **OrangeRed/빨강 2px**: close/X
- capture와 동일한 1:1 pixel coordinate
- 문제 피드백 시 저장 PNG 하나로 detector/ROI drift를 확인 가능

로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

`로그 삭제`는 recent activity + current/rotated scanner log + latest in-memory recognition image를 정리합니다. 사용자가 별도 저장한 PNG는 삭제하지 않습니다.

## 11. Mini Scanner

- match 성공 item 정보만 표시
- runtime/OCR/error/status text는 overlay에 표시하지 않음
- WPF Topmost + native HWND_TOPMOST
- no-activate
- 전체 카드 drag surface / Arrow cursor
- 실제 mode에서 Tarkov foreground + inventory/stash context를 보수적으로 확인
- inventory OCR probe single-active
- repeated request latest coalescing
- stale epoch result reject

Title OCR과 inventory-context OCR은 하나의 WinRT OCR serialization boundary를 공유합니다.

## 12. Scanner 표시 데이터

### 최고 상점가

유효한 non-flea 판매처의 RUB 환산 가격 중 최댓값.

### 플리마켓 평균가

positive `avg24hPrice`.

### 슬롯 가격

positive `width × height`가 존재할 때만 계산.

### 필요한 개수

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

Inventory를 차감한 shortage가 아닙니다.

market/dimension 누락/오류는 해당 표시 필드만 fail closed하며 이미 확정된 Item identity 자체를 버리지 않습니다.

## 13. Icon / local cache

- Game Content update에서 canonical item icon prefetch
- scan 순간 icon HTTP 없음
- local image-cache 사용
- 개별 icon 실패는 전체 identity/catalog을 fatal로 만들지 않음
- decode/freeze 성공 icon은 process-local cache 재사용 가능

## 14. Persistence

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/
%LocalAppData%/JunhyunHelper/scanner/fonts/
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

Scanner 설정/cache/log는 program package와 분리됩니다.

## 15. 실제 Tarkov calibration

v1.3.4 공개 검증은 완료됐지만 실제 Tarkov recognition calibration은 계속 진행합니다.

문제 evidence:

```text
실제 아이템 이름
+ success / miss / wrong identity
+ 문제 직후 저장한 v1.3.4 진단 PNG
+ 필요 시 scanner.log
```

분석 단계:

```text
capture
→ structural candidate
→ close shape / header frame
→ fixed magnifier lane + template
→ locked detail bounds / title ROI
→ raw OCR
→ current-catalog sanitation / unknown-glyph / semantic matcher
→ Tarkov-font visual corroboration/recovery
→ Item ID
→ presentation data
→ overlay
```

live evidence 없이 confidence/margin을 임의로 완화하지 않습니다.

## 16. 공개 검증

v1.3.4에서 검증된 항목:

- exact release source/tag `a78ddbc649747f1320236556f17e6b908304674a`
- Windows Release build
- **267/267 tests**
- 12-case live-header geometry regression
- title-lane decoy magnifier regression
- unknown-glyph unique/ambiguous/short-title fail-closed regression
- diagnostic PNG four-color overlay renderer smoke
- win-x64 self-contained single-file publish
- package root/ProductVersion/FIRST_RUN audit
- Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke
- Draft asset re-download + actual EXE smoke
- public/latest
- public SHA256SUMS / exact tag source
- independent public re-download verification
- public-downloaded actual EXE smoke
- graceful shutdown / clean portable root
- one-shot release/public-verifier workflow cleanup

공개 asset:

```text
Junhyun-Helper-v1.3.4-win-x64.zip
80,319,654 bytes
SHA-256 8c442fec81a0b993a9a6b080e59b656668a7a73d8fadd8434595545b08c82e8e
ProductVersion 1.3.4+a78ddbc649747f1320236556f17e6b908304674a
```

상세 증거: `docs/RELEASE_1.3.4.md`, `docs/.release-v1.3.4-status.json`.
