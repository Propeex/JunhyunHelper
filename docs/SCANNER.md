# Scanner — 제품/기술 계약

기준일: 2026-08-23

상태: **`v1.3.1 PUBLIC VERIFIED / LIVE TARKOV CALIBRATION ONGOING`**

## 1. 목적

Scanner는 Tarkov 화면을 기존 JunhyunHelper Item ID와 진행/가격 데이터에 연결하는 화면 기반 입력 bridge입니다.

```text
화면 픽셀
→ structural detail candidates
→ inspect-header structural refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR + catalog character policy
→ current official Korean catalog semantic match
→ optional Tarkov-font visual corroboration/recovery
→ confidence + top1/top2 margin
→ Item ID
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

## 2. 현재 공개 기준선

```text
version: v1.3.1
release source: 028bfb600f4662962a0daac1dad04b570e018275
final PR CI: 32615869812 — SUCCESS
256 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.3.1-win-x64.zip
bytes: 80,310,221
SHA-256: 5c4b79cc5d373b4a28cbeb10be18b8369086b2ee9f0edc172530028dd71b1c3f
ProductVersion: 1.3.1+028bfb600f4662962a0daac1dad04b570e018275
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세: `docs/RELEASE_1.3.1.md`, `docs/SCANNER_V1.3.1_RECOGNITION.md`.

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

Catalog state writer인 `LoadCacheAsync`와 `RefreshAsync`는 동일 operation gate를 사용하여 이전 GameMode refresh가 최신 profile/cache state를 뒤늦게 덮어쓰지 못합니다.

## 5. Structural detail-window detector

Scanner Lab v3.8의 RED-X/rectangle architecture를 production geometry 기준으로 유지합니다.

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
- verified bounds/title signature가 유지되면 OCR 반복 억제

## 6. v1.3.1 inspect-header structural refinement

실제 Tarkov에서 `아이템 이름 첫 글자 → magnifier 오인` 사례가 확인되어 title ROI 생성 방식을 강화했습니다.

### Header evidence

다음을 독립 evidence로 결합합니다.

- dark neutral title-field strip
- right red close/X
- left magnifier/search icon
- magnifier 오른쪽의 실제 first title glyphs

아이콘 하나 또는 panel-relative 좌표 하나만으로 title ROI를 결정하지 않습니다.

### Red close/X

- 우측 상단 red-dominant connected component
- right-edge proximity
- shape compactness

이 evidence는 title ROI의 최대 우측 안전 경계에도 사용합니다.

### Magnifier

단순 `밝고 네모난 component`가 아니라 다음을 평가합니다.

- header 내 상대 위치
- expected icon size 대비 크기
- width/height aspect
- hollow/dark center
- bright ring perimeter
- lower-right handle
- 오른쪽에 뒤따르는 title glyph evidence

### Panel-left drift

structural panel-left가 실제 magnifier보다 일부 안쪽으로 잡혀도 제한된 범위에서 왼쪽을 다시 검색합니다. 화면 전체의 임의 아이콘을 검색하지 않고 title-field/red-X/glyph relation 안에서만 anchor를 보정합니다.

### First-glyph preservation

magnifier를 찾은 뒤 실제 첫 title glyph 시작점을 별도로 확인하여 OCR ROI가 첫 글자를 잘라내지 않도록 합니다.

packaged-EXE smoke는 inward-drifted panel-left + real-ish magnifier + Korean-like first glyph + dark field + red X 조합을 합성해 이 계약을 고정합니다.

## 7. OCR / character policy / semantic matching

Primary text recognizer는 Windows `ko-KR` OCR입니다.

- title pixel height에 따라 4x/6x/8x 확대
- first-pass 실패 시 high-contrast/binary/inverse deep OCR
- full text / line / 유효 조합을 current catalog와 비교
- exact-first
- fuzzy confidence + top1/top2 margin
- ambiguous/low-confidence → Item ID 미확정
- historical alias를 production identity source로 무제한 누적하지 않음

`ScannerOcrCharacterPolicy`는 current official Korean item-name catalog에서 허용 문자 집합을 파생합니다.

- 공식 이름에 없는 unexpected character → corrupted evidence
- Korean-title contract에서 Han ideograph → invalid evidence
- 특정 OCR 문자를 임의 치환해 confidence를 올리지 않음

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

Font/template cache는 generation-aware + bounded입니다.

### OCR failure/corruption

- plausible OCR variant가 있으면 semantic shortlist + visual verifier
- OCR이 비거나 심하게 손상되면 strict full-catalog visual matcher
- current official catalog 안에서만 후보 생성
- visual top1 + top1/top2 margin 모두 필요
- ambiguous candidate reject

### OCR semantic success — v1.3.1

semantic OCR success도 필요 시 시각 corroboration을 수행할 수 있습니다.

- visual result == OCR Item ID → OCR 유지
- font unavailable / renderer error / ambiguous → healthy OCR 유지
- strict visual evidence가 다른 current official Item ID를 명확히 지목 → 그 Item ID로만 교정 허용

즉 visual은 모든 OCR success를 무조건 재판정하는 mandatory gate가 아닙니다. 명확한 시각 모순을 교정하는 conservative hardening입니다.

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
- OCR text
- official candidate
- confidence / second score / reason

v1.3.0부터 `이미지 저장`을 사용하면 실제 분석 원본 frame을 사용자 지정 PNG로 export합니다.

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

v1.3.1은 공개 검증되었지만 실제 Tarkov recognition calibration은 계속 진행합니다.

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
→ inspect header / title ROI
→ OCR / visual corroboration
→ catalog identity
→ presentation data
→ overlay
```

live evidence 없이 confidence/margin을 임의로 완화하지 않습니다.

## 16. 공개 검증

v1.3.1에서 검증된 항목:

- exact release source/tag
- Windows Release build
- 256/256 tests
- win-x64 self-contained single-file publish
- package root/ProductVersion/FIRST_RUN audit
- inspect-header first-glyph regression
- Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke
- Draft/Public re-download verification
- public/latest
- public SHA256SUMS
- public-downloaded actual EXE smoke
- graceful shutdown / clean portable root

상세 증거: `docs/RELEASE_1.3.1.md`.
