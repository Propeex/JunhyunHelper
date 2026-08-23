# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-23

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램입니다.

핵심 기능:

- GameMode별 Profile / User Progress
- Quest availability / prerequisite / special trader / profile-variable
- Hideout
- Needed Items / FIR·일반 Inventory / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Map + MiniMap
- Game Content 안전 업데이트 / image cache
- 사용자 동의형 Program Update
- Scanner + Mini Scanner

Runtime GPT/AI 의존성은 없습니다.

## 2. 현재 공개 릴리즈

현재 public stable은 **v1.3.4**입니다.

```text
version: v1.3.4 PUBLIC RELEASE / VERIFIED
release source: a78ddbc649747f1320236556f17e6b908304674a
public tag source: a78ddbc649747f1320236556f17e6b908304674a
final PR CI: 32636665202 — SUCCESS
automated tests: 267 passed / 0 failed / 0 skipped
release run: 32636927134 — SUCCESS
independent public verifier: 32637159066 — SUCCESS
asset: Junhyun-Helper-v1.3.4-win-x64.zip
bytes: 80,319,654
SHA-256: 8c442fec81a0b993a9a6b080e59b656668a7a73d8fadd8434595545b08c82e8e
ProductVersion: 1.3.4+a78ddbc649747f1320236556f17e6b908304674a
public/latest: VERIFIED
exact public tag source: VERIFIED
Draft re-download: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.3.4
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.3 → v1.3.4 mandatory Game Content update: none
v1.3.3 → v1.3.4 user.db migration: none
```

공식 검증 기록:

- `docs/RELEASE_1.3.4.md`
- `docs/.release-v1.3.4-status.json`

## 3. 아키텍처 기준

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

책임:

- **Core**: canonical domain, deterministic calculation, Scanner identity/matcher 규칙
- **Application**: 사용자 유스케이스, authoritative mutation, workspace orchestration
- **Infrastructure**: HTTP/source parsing, SQLite/file persistence, content/scanner/update I/O
- **Desktop**: WPF UI, presentation, Scanner capture/OCR/runtime, Map bridge
- **Map/MiniMap donor**: 제한적 compile-link 예외. donor updater/content ownership/hidden command는 사용하지 않음

현재 pinned Map donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

## 4. Scanner 장기 제품 계약

Scanner는 게임 프로세스 내부 데이터를 읽지 않는 독립적인 화면 기반 보조 기능입니다.

```text
Tarkov / Display pixels
→ detail-window structural candidates
→ actual inspect-header lock
   - right red close/X + normalized X-shape template
   - long neutral top frame
   - fixed frame-left search-icon lane
   - normalized magnifier ring/hollow/handle template
   - dark title field + text evidence
→ full HEADER_FRAME_LOCKED only
→ locked-header-based detail bounds refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog-derived character/symbol sanitation
→ optional one-unknown-glyph current-catalog recovery
→ current official Korean catalog semantic matching
→ bounded one-edit recovery when uniquely safe
→ optional local Tarkov-font visual corroboration/recovery
→ conservative confidence + top1/top2 margin
→ Item ID or fail closed
→ local presentation data
→ Mini Scanner
```

불변 원칙:

- false positive보다 miss 선호
- geometry / structural score / anchor score / icon 하나만으로 Item identity 확정 금지
- current official Korean item catalog가 identity 권위
- historical alias를 production identity source로 무제한 누적하지 않음
- live evidence 없이 global confidence/margin을 완화하지 않음
- ambiguity / low confidence / incomplete header lock은 fail closed
- current-catalog 밖 glyph를 특정 문자로 임의 치환하지 않음
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지

## 5. Capture modes

### TarkovWindow

```text
EscapeFromTarkov process/window
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ PrintWindow 우선
→ invalid/empty이면 exact client screen rectangle fallback
```

최소화 또는 유효하지 않은 client-area는 인식하지 않습니다. 불필요한 대형 duplicate full-frame managed copy를 만들지 않습니다.

### DisplayTest

연결된 전체 디스플레이를 대상으로 TarkovWindow와 동일한 detector/OCR/catalog/presentation pipeline을 적용합니다. real/test continuous mode는 상호 배타적입니다.

### One-shot

- 1회 인게임: TarkovWindow를 한 번 정밀 분석
- 1회 테스트: 모든 연결 display를 한 번 정밀 분석
- continuous mode를 영구 변경하지 않음
- shared recognition state와 직렬화
- scan-time catalog network refresh를 시작하지 않음

## 6. Detail-window structural detection

Scanner Lab v3.8 계열 구조를 production candidate geometry 기준으로 유지합니다.

- red-X connected-component 후보
- rectangle/edge fallback 후보
- IoU deduplication
- 최대 8 candidates
- structural floor `0.34`
- structural score는 Item identity 점수가 아님
- continuous mode에서는 동일 quantized geometry가 안정화된 뒤 semantic recognition
- verified detail/title signature가 유지되면 불필요한 OCR 반복 억제

v1.3.4에서는 initial structural rectangle을 최종 authoritative bounds로 간주하지 않습니다. Full header lock 후 magnifier/X 실측값에서 detail-window top/left/right를 다시 정렬하고, 아이템별 stat pane 높이 차이를 보존하기 위해 bottom만 structural detector 값을 유지합니다.

## 7. Inspect-header / title ROI 기준

### v1.3.3 기반 결정

실제 Tarkov 2048×1280 상세창 12개에서 title-start / magnifier-anchor 회귀를 재측정해 **title glyph가 title ROI의 수평 ownership을 가지지 않도록** 고정했습니다.

다음 구조를 결합합니다.

1. 우측 red close/X
2. 길게 이어지는 neutral top frame
3. frame-left의 search-icon lane
4. magnifier core
5. dark title field
6. title text presence

Runtime은 다음을 요구합니다.

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
```

first Korean/title glyph connected component는 OCR title ROI의 left edge를 결정하거나 오른쪽으로 이동시킬 수 없습니다.

### v1.3.4 강화

v1.3.4는 title glyph가 magnifier 후보로 승격되는 실제 failure를 차단하기 위해 search-icon candidate space 자체를 제한했습니다.

```text
scale ≈ close.Height / 17
magnifier expected x ≈ frame.Left + 12 * scale
magnifier expected y ≈ frame.Top + 7 * scale
magnifier expected size ≈ 13 * scale
```

- magnifier 후보는 fixed frame-left lane 안에서만 생성
- ring bright band + hollow center + lower-right handle + outside background template 사용
- title lane glyph는 shape가 유사해도 candidate pool에 들어갈 수 없음
- close/X는 red body/edge + expected geometry + diagonal X contrast template 결합
- full `HEADER_FRAME_LOCKED` + score 0.68 + valid magnifier/X를 모두 통과한 structural candidate만 OCR 후보 목록에 남음

12개 실측 geometry의 비식별 측정값은 `docs/.scanner-v1.3.3-header-evidence.json`에 보존되며 packaged-EXE synthetic regression으로 계속 재생됩니다.

상세:

- `docs/SCANNER_V1.3.4_LIVE_HARDENING.md`
- `docs/DECISION_SCANNER_V1.3.4_LIVE_HARDENING_2026-08-23.md`
- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`

## 8. OCR / current-catalog sanitation / semantic matcher

Primary text recognizer는 Windows `ko-KR` OCR입니다.

- title size에 따라 4x/6x/8x 확대
- first pass 실패 시 deep OCR/high-contrast/binary/inverse variants
- current catalog 기반 character/symbol policy
- raw OCR은 진단용으로 보존하고 실제 matcher input은 sanitation 후 별도로 기록
- current catalog에 없는 punctuation/symbol은 ordinary matcher evidence에서 제거
- Korean-title contract에서 CJK Han ideograph는 hard reject
- arbitrary character replacement로 confidence를 인위적으로 올리지 않음
- official catalog exact-first + conservative fuzzy + margin
- ambiguous/low-confidence는 Item ID 미확정

### one-unknown-glyph recovery — v1.3.4

실제 `Esma「ch` 계열 failure처럼 current-catalog 밖 symbol이 **영숫자 사이에 정확히 한 번** 나타나면 ordinary sanitized text와 별도로 `?` pattern을 보존할 수 있습니다.

```text
raw:      Esma「ch 에스마르호 지혈대
ordinary: Esmach 에스마르호 지혈대
pattern:  Esma?ch 에스마르호 지혈대
```

`?`는 `r`이나 다른 특정 글자를 뜻하지 않습니다. 해당 위치 한 glyph의 정체가 미상이라는 evidence만 표현합니다.

복구 조건:

```text
normalized pattern length >= 7
AND exactly one unknown glyph
AND same-length exact-slot candidate is unique over complete current catalog
AND duplicate official name이 아님
AND best - global wildcard runner-up >= 10 percentage points
```

Short title, ambiguous candidate, close runner-up은 fail closed합니다.

### bounded one-edit recovery — v1.3.2부터 유지

```text
normalized official length >= 7
AND edit distance == 1
AND candidate is unique over the complete current catalog
AND best candidate is the ordinary match top1
AND best - global runner-up >= 10 percentage points
```

multi-edit 저신뢰 OCR을 percentage만으로 확정하지 않습니다.

## 9. Tarkov-font visual corroboration / recovery

게임 폰트 바이너리를 public package에 포함하지 않습니다.

```text
Tarkov resources.assets (read-only)
→ bounded SFNT discovery
→ %LocalAppData%/JunhyunHelper/scanner/fonts
→ source manifest + font generation key
→ Bender regular/bold + Korean fallback
→ current official item-name rendered templates/features
```

- plausible OCR이 있으면 semantic shortlist + title-font verifier
- OCR이 비거나 심하게 손상되면 strict full-catalog visual matcher
- semantic OCR success에서도 명확한 시각 모순이 있을 때만 보수적으로 교정
- visual unavailable/error/ambiguous이면 healthy OCR을 임의로 폐기하지 않음
- current catalog 밖 Item 생성 금지
- top1 score + margin 부족 시 reject
- Font/template cache는 generation-aware + bounded

## 10. Scanner 사용자 분석 / 단축키

Scanner display settings schema는 **v4**입니다.

기본 global hotkey:

- 1회 인게임: `Ctrl+Shift+F10`
- 1회 테스트: `Ctrl+Shift+F11`
- Scanner ON/OFF: `Ctrl+Shift+F12`

계약:

- MainWindow lifetime 동안 Scanner 탭 밖에서도 동작
- Scanner 탭에서 각각 변경/비활성화 가능
- 동일 gesture 중복 금지
- schema v3의 기존 one-shot 사용자 key를 schema v4로 보수적으로 승계
- one-shot 인게임/테스트 버튼은 제품 UI에 없음

## 11. 인식 이미지 / diagnostics

최신 diagnostic frame 1개를 메모리에 유지합니다.

확인 가능한 정보:

- capture source/origin
- selected detail bounds
- title ROI
- magnifier / close anchor bounds
- structural/header evidence
- OCR/visual pass
- raw OCR
- sanitized matcher input
- candidate official name
- confidence / second score / reason

자동 screenshot persistence는 없습니다.

사용자가 명시적으로 `이미지 저장`을 선택하면 **실제 분석 frame + detector rectangle**을 PNG로 export합니다.

- 초록: selected detail window
- 파랑: OCR title ROI
- 노랑: magnifier
- 빨강: close/X
- rectangle은 실제 capture와 동일한 pixel coordinate로 합성
- `로그 삭제`는 사용자 export PNG를 삭제하지 않음

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

## 12. Scanner catalog / 표시 데이터

Identity catalog health:

```text
accepted item count >= 4000
AND every accepted item has non-empty Item ID
AND every accepted item has non-empty official name
```

catalog disk load/network refresh는 mode-transition gate로 직렬화되어 이전 GameMode operation이 최신 state를 덮어쓰지 못합니다.

표시 의미:

- highest trader sell price = 유효한 non-flea RUB 환산 판매가 최댓값
- flea average = positive `avg24hPrice`
- slots = positive `width × height`
- price/slot = valid price와 slots가 모두 존재할 때만
- required count = `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal`

Inventory 차감 부족량은 Scanner의 `필요 개수` 의미가 아닙니다. market/dimension 누락은 해당 표시 필드만 fail closed하고 Item identity health와 분리합니다.

## 13. Mini Scanner

- match 성공 Item 정보만 표시
- runtime/OCR/error/status text는 overlay에 표시하지 않음
- WPF Topmost + native HWND_TOPMOST
- ShowActivated=false / no-activate
- 전체 카드 drag hit surface / Arrow cursor
- 실제 Scanner mode에서는 Tarkov foreground + inventory/stash context를 보수적으로 확인
- inventory/stash OCR probe 최대 1개
- 반복 요청은 latest coalesce
- item/context epoch가 바뀐 stale result는 화면에 적용하지 않음

Title OCR과 inventory-context OCR은 하나의 WinRT OCR serialization boundary를 공유합니다.

## 14. Persistence / 사용자 데이터

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/map-product-settings.json(.bak)
%LocalAppData%/JunhyunHelper/ammo-favorites.json(.bak)
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/
%LocalAppData%/JunhyunHelper/scanner/fonts/
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

Program package와 사용자 데이터는 분리됩니다. v1.3.3 → v1.3.4는 user.db migration이나 mandatory Game Content update가 없습니다.

## 15. Map / MiniMap

Map/MiniMap은 pinned donor source를 제한적으로 compile-link한 독립 subsystem입니다.

- general marker/artwork/config → pinned Map bundle
- current Quest state/geometry → JunhyunHelper bridge
- donor updater/content DB/global hidden command/legacy logger는 product ownership에서 제외
- 구체적 defect/performance evidence 없이 broad refactor하지 않음

## 16. Program Update / 배포 계약

정식 release는 다음 순서를 지킵니다.

```text
exact release source
→ build/tests/publish/package audit
→ actual packaged EXE Product UI/Scanner/Map smoke
→ ZIP + SHA256SUMS
→ Draft release
→ Draft asset re-download verification
→ Draft-downloaded EXE smoke
→ public/latest
→ exact tag source verification
→ public asset re-download verification
→ public-downloaded EXE smoke
→ independent public verifier
→ durable release record
→ one-shot workflow cleanup
```

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

Program Update는 program-owned files만 교체하며 `%LocalAppData%/JunhyunHelper` 사용자 데이터를 건드리지 않습니다.

## 17. v1.3.4 공개 검증 결과

완료:

- PR #146 final CI `32636665202`: SUCCESS
- automated tests: **267 passed / 0 failed / 0 skipped**
- exact-source release run `32636927134`: SUCCESS
- independent public verifier `32637159066`: SUCCESS
- exact release source/tag: `a78ddbc649747f1320236556f17e6b908304674a`
- public/latest: VERIFIED
- Draft/public ZIP re-download/hash/size/SHA256SUMS/layout: VERIFIED
- ProductVersion/FIRST_RUN: VERIFIED
- Draft/public-downloaded Product UI + Scanner + Mini Scanner + Main Map + Factory + MiniMap smoke: SUCCESS
- one-shot release/public-verifier workflows: removed after successful durable evidence write

공개 asset:

```text
Junhyun-Helper-v1.3.4-win-x64.zip
80,319,654 bytes
SHA-256 8c442fec81a0b993a9a6b080e59b656668a7a73d8fadd8434595545b08c82e8e
ProductVersion 1.3.4+a78ddbc649747f1320236556f17e6b908304674a
```

## 18. 현재 알려진 열린 영역

제품 기능 결함으로 확정된 release blocker는 없습니다.

Scanner는 **실제 Tarkov live calibration을 계속하는 단계**입니다. 모든 해상도/DPI/UI 위치/아이템명 조합을 실게임에서 검증한 것은 아닙니다.

새 evidence가 발생하면 다음 순서로 분리합니다.

```text
capture
→ detail structural detection
→ close template / header frame / fixed magnifier lane-template
→ locked detail bounds / title ROI
→ raw OCR
→ catalog sanitation / unknown-glyph / semantic matcher
→ Tarkov-font visual corroboration
→ Item ID
→ trader/flea/RequiredTotal presentation
→ Mini Scanner / stale-state handling
```

실제 실패 evidence 없이 confidence/margin을 완화하지 않습니다.

## 19. 다음 작업

1. 실제 Tarkov Borderless 환경에서 다양한 아이템/위치/해상도/DPI로 사용
2. 성공 / 미인식 / 오인식 / detector / close/frame/magnifier / bounds / ROI / OCR / matcher / visual 실패 분류
3. 문제 발생 직후 v1.3.4 진단 PNG와 scanner.log를 함께 보존
4. 정확히 인식된 Item ID에서 highest trader / flea `avg24hPrice` / `RequiredTotal` end-to-end 검증
5. 빠른 연속 scan의 stale isolation 검증
6. 장시간 CPU / memory / UI responsiveness 검증
7. 실제 실패를 regression으로 먼저 고정한 뒤 해당 단계만 수정

상세 작업 목록: `docs/NEXT.md`.
