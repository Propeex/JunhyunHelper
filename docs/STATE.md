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

현재 public stable은 **v1.3.3**입니다.

```text
version: v1.3.3 PUBLIC RELEASE / VERIFIED
release source: 41bf5b8374ba774866aab4b60a25376d9b5548c2
public tag source: 41bf5b8374ba774866aab4b60a25376d9b5548c2
final PR CI: 32625223009 — SUCCESS
automated tests: 263 passed / 0 failed / 0 skipped
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

```text
Desktop Version: 1.3.3
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.2 → v1.3.3 mandatory Game Content update: none
v1.3.2 → v1.3.3 user.db migration: none
```

공식 검증 기록:

- `docs/RELEASE_1.3.3.md`
- `docs/.release-v1.3.3-status.json`

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
   - right red close/X
   - long neutral top frame
   - bounded frame-left search-icon lane
   - magnifier morphology/core
   - dark title field + text evidence
→ HEADER_FRAME_LOCKED
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog-derived character/symbol sanitation
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

## 7. v1.3.3 inspect-header / title ROI 기준

v1.3.2 공개 후 실제 Tarkov 2048×1280 상세창 12개에서 title-start / magnifier-anchor 회귀가 재확인되었습니다.

v1.3.3의 핵심 결정은 **title glyph가 title ROI의 수평 ownership을 가지지 않는 것**입니다.

### Authoritative header lock

다음 구조를 결합합니다.

1. 우측 red close/X
2. 길게 이어지는 neutral top frame
3. frame-left의 bounded search-icon lane
4. 13px-class magnifier bright core + ring/hollow/handle morphology
5. dark title field
6. title text presence

모두 결합되어 `HEADER_FRAME_LOCKED`가 된 candidate만 OCR identity path로 진행할 수 있습니다.

Runtime은 추가로 다음을 요구합니다.

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
```

partial/failed lock은 refiner 단계에서 0.47 이하로 제한되며 runtime에서도 다시 거부됩니다.

### First-glyph rule

first Korean/title glyph connected component는 더 이상 OCR title ROI의 left edge를 결정하거나 오른쪽으로 이동시킬 수 없습니다. 실제 magnifier 오른쪽의 구조적 gap을 기준으로 title start를 정합니다.

### 12-case evidence

원본 사용자 screenshot 자체는 저장소에 넣지 않습니다. 비식별 header-relative 측정값만 다음 파일에 보존합니다.

- `docs/.scanner-v1.3.3-header-evidence.json`

12개 실측 geometry는 packaged-EXE smoke의 synthetic regression에서 모두 재생됩니다.

상세:

- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`
- `docs/DECISION_SCANNER_HEADER_LOCK_2026-08-23.md`

## 8. OCR / current-catalog sanitation / semantic matcher

Primary text recognizer는 Windows `ko-KR` OCR입니다.

- title size에 따라 4x/6x/8x 확대
- first pass 실패 시 deep OCR/high-contrast/binary/inverse variants
- current catalog 기반 character/symbol policy
- raw OCR은 진단용으로 보존하고 실제 matcher input은 sanitation 후 별도로 기록
- current catalog에 없는 punctuation/symbol은 matcher evidence에서 제거
- Korean-title contract에서 CJK Han ideograph는 hard reject
- arbitrary character replacement로 confidence를 인위적으로 올리지 않음
- official catalog exact-first + conservative fuzzy + margin
- ambiguous/low-confidence는 Item ID 미확정

v1.3.2의 bounded one-edit recovery는 그대로 유지합니다.

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

사용자가 명시적으로 `이미지 저장`을 선택하면 **실제 분석 원본 frame**을 PNG로 export할 수 있습니다.

- 자동 screenshot 저장 없음
- export PNG에 diagnostic rectangle/text overlay 합성 없음
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

Program package와 사용자 데이터는 분리됩니다. v1.3.2 → v1.3.3은 user.db migration이나 mandatory Game Content update가 없습니다.

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
→ independent public finalizer
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

## 17. v1.3.3 공개 검증 결과

완료:

- PR #145 final CI `32625223009`: SUCCESS
- automated tests: **263 passed / 0 failed / 0 skipped**
- exact-source release run `32625403609`: SUCCESS
- exact release source/tag: `41bf5b8374ba774866aab4b60a25376d9b5548c2`
- public/latest: VERIFIED
- public ZIP re-download/hash/size/SHA256SUMS/layout: VERIFIED
- ProductVersion/FIRST_RUN: VERIFIED
- public-downloaded Product UI + Scanner + Mini Scanner + Main Map + Factory + MiniMap smoke: SUCCESS

공개 asset:

```text
Junhyun-Helper-v1.3.3-win-x64.zip
80,314,373 bytes
SHA-256 0771d3c7dee5a8f19904d52eeedc7b9abbd6027a7b000255ebd33c296bc2186f
```

## 18. 현재 알려진 열린 영역

제품 기능 결함으로 확정된 release blocker는 없습니다.

Scanner는 **실제 Tarkov live calibration을 계속하는 단계**입니다. 모든 해상도/DPI/UI 위치/아이템명 조합을 실게임에서 검증한 것은 아닙니다.

새 evidence가 발생하면 다음 순서로 분리합니다.

```text
capture
→ detail structural detection
→ inspect-header frame lock / title ROI
→ OCR
→ catalog sanitation/matcher
→ Tarkov-font visual corroboration
→ Item ID
→ trader/flea/RequiredTotal presentation
→ Mini Scanner / stale-state handling
```

실제 실패 evidence 없이 confidence/margin을 완화하지 않습니다.

## 19. 다음 작업

1. 실제 Tarkov Borderless 환경에서 다양한 아이템/위치/해상도/DPI로 사용
2. 성공 / 미인식 / 오인식 / detector / header lock / ROI / OCR / matcher / visual 실패 분류
3. 정확히 인식된 Item ID에서 highest trader / flea `avg24hPrice` / `RequiredTotal` end-to-end 검증
4. 빠른 연속 scan의 stale isolation 검증
5. 장시간 CPU / memory / UI responsiveness 검증
6. 실제 실패를 regression으로 먼저 고정한 뒤 해당 단계만 수정

상세 작업 목록: `docs/NEXT.md`.
