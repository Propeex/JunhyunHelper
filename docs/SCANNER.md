# Scanner — 제품/기술 계약

기준일: 2026-08-23

상태: **`v1.3.3 PUBLIC VERIFIED / LIVE TARKOV CALIBRATION ONGOING`**

## 1. 목적

Scanner는 Tarkov 화면을 기존 JunhyunHelper Item ID와 진행/가격 데이터에 연결하는 화면 기반 입력 bridge입니다.

```text
화면 픽셀
→ structural detail candidates
→ actual inspect-header frame lock
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog character/symbol sanitation
→ current official Korean catalog semantic match
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
- live evidence 없이 acceptance threshold 완화

## 2. 현재 공개 기준선

```text
version: v1.3.3
release source/tag: 41bf5b8374ba774866aab4b60a25376d9b5548c2
final PR CI: 32625223009 — SUCCESS
263 passed / 0 failed / 0 skipped
release run: 32625403609 — SUCCESS
asset: Junhyun-Helper-v1.3.3-win-x64.zip
bytes: 80,314,373
SHA-256: 0771d3c7dee5a8f19904d52eeedc7b9abbd6027a7b000255ebd33c296bc2186f
ProductVersion: 1.3.3+41bf5b8374ba774866aab4b60a25376d9b5548c2
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세: `docs/RELEASE_1.3.3.md`, `docs/.release-v1.3.3-status.json`.

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

Scanner Lab v3.8의 RED-X/rectangle architecture를 candidate geometry 기준으로 유지합니다.

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

## 6. v1.3.3 inspect-header frame lock

v1.3.2 이후 실제 2048×1280 Tarkov 상세창 12개에서 title start가 첫 글자 segmentation에 끌려가거나 magnifier association이 흔들리는 회귀가 재확인되었습니다.

v1.3.3은 `ScannerInspectHeaderLock`을 authoritative title-geometry layer로 사용합니다.

### Required structure

1. **red close/X** — detail header 우측 control
2. **long neutral top frame** — 실제 inspect header의 수평 frame
3. **bounded frame-left icon lane** — search icon을 찾을 수 있는 제한된 영역
4. **magnifier bright core + morphology** — 약 13px-class core, ring/hollow/handle evidence
5. **dark title field**
6. **title text presence**

위 구조가 결합되어 `HEADER_FRAME_LOCKED`가 된 candidate만 OCR identity path로 진행할 수 있습니다.

Runtime gate:

```text
TitleImage exists
AND valid title bounds
AND valid magnifier bounds
AND valid close bounds
AND TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
```

partial/failed lock은 refiner에서 score를 `<= 0.47`로 제한하고 runtime에서도 다시 거부합니다.

### First-glyph ownership

first Korean/title glyph connected component는 더 이상 title ROI의 left edge를 결정하지 않습니다. 따라서 glyph fragmentation이나 anti-aliasing 때문에 title start가 오른쪽으로 밀려 첫 글자가 잘리는 경로를 허용하지 않습니다.

### Live evidence

12개 실제 screenshot 자체는 저장소에 커밋하지 않습니다. 비식별 header-relative 측정값만 유지합니다.

- `docs/.scanner-v1.3.3-header-evidence.json`
- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`
- `docs/DECISION_SCANNER_HEADER_LOCK_2026-08-23.md`

packaged-EXE smoke는 12개 measured geometry를 synthetic frame으로 모두 재생합니다.

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
- matcher에는 sanitation 후 text를 전달
- current catalog에 존재하지 않는 punctuation/symbol은 matcher evidence에서 제거
- Korean item-title contract에서 Han ideograph는 invalid evidence
- 특정 OCR 문자를 임의 치환해 confidence를 올리지 않음

v1.3.2의 bounded one-edit recovery는 유지합니다.

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

`이미지 저장`을 사용하면 실제 분석 원본 frame을 사용자 지정 PNG로 export합니다.

- 자동 screenshot 저장 없음
- export PNG에 diagnostic rectangle/text overlay를 합성하지 않음
- 다음 scan 전에 문제 frame을 저장하는 것이 권장 실사용 절차

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

v1.3.3 공개 검증은 완료됐지만 실제 Tarkov recognition calibration은 계속 진행합니다.

문제 evidence:

```text
실제 아이템 이름
+ success / miss / wrong identity
+ 문제 직후 저장한 인식 원본 PNG
+ 필요 시 scanner.log
```

분석 단계:

```text
capture
→ structural candidate
→ inspect-header frame lock / title ROI
→ OCR
→ current-catalog sanitation / semantic matcher
→ Tarkov-font visual corroboration/recovery
→ Item ID
→ presentation data
→ overlay
```

live evidence 없이 confidence/margin을 임의로 완화하지 않습니다.

## 16. 공개 검증

v1.3.3에서 검증된 항목:

- exact release source/tag
- Windows Release build
- 263/263 tests
- 12-case live-header geometry regression
- win-x64 self-contained single-file publish
- package root/ProductVersion/FIRST_RUN audit
- Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke
- Draft asset re-download verification
- public/latest
- public SHA256SUMS / exact tag source
- independent public re-download verification
- public-downloaded actual EXE smoke
- graceful shutdown / clean portable root

상세 증거: `docs/RELEASE_1.3.3.md`, `docs/.release-v1.3.3-status.json`.
