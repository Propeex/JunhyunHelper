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

현재 public stable은 **v1.3.2**입니다.

```text
version: v1.3.2 PUBLIC RELEASE / VERIFIED
release source: 922797a99ea221fdc4984dd6ed05df552149d6e4
public tag source: 922797a99ea221fdc4984dd6ed05df552149d6e4
final PR CI: 32619142034 — SUCCESS
automated tests: 263 passed / 0 failed / 0 skipped
release run: 32621021058
asset: Junhyun-Helper-v1.3.2-win-x64.zip
bytes: 80,311,752
SHA-256: 6e3a7af2de50dfd14f1c49ccb39753177a0bce5b22993bb8bb94ffde93086767
ProductVersion: 1.3.2+922797a99ea221fdc4984dd6ed05df552149d6e4
public/latest: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.3.2
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.1 → v1.3.2 mandatory Game Content update: none
v1.3.1 → v1.3.2 user.db migration: none
```

공식 검증 기록:

- `docs/RELEASE_1.3.2.md`
- `docs/.release-v1.3.2-status.json`

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
→ inspect-header refinement
   - dark title field
   - right red close/X
   - left magnifier morphology
   - first title-glyph evidence
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
- geometry / structural score / anchor score는 Item identity 자체가 아님
- icon 하나만으로 Item ID 확정 금지
- current official Korean item catalog가 identity 권위
- historical alias를 production identity source로 무제한 누적하지 않음
- live evidence 없이 global confidence/margin을 편의상 완화하지 않음
- ambiguity / low confidence는 fail closed
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

Scanner Lab v3.8 계열 구조를 production geometry 기준으로 사용합니다.

- red-X connected-component 후보
- rectangle/edge fallback 후보
- IoU deduplication
- 최대 8 candidates
- structural floor `0.34`
- structural score는 Item identity 점수가 아님
- continuous mode에서는 동일 quantized geometry가 안정화된 뒤 semantic recognition
- verified detail/title signature가 유지되면 불필요한 OCR 반복 억제

## 7. v1.3.1 inspect-header / title ROI 기준

v1.3.1은 실제 Tarkov에서 관측된 `첫 글자 → 돋보기 오인` 실패를 수정했습니다.

### Title field

상세창 상단의 dark neutral strip을 구조 evidence로 사용합니다. panel-relative 좌표 하나만으로 title lane을 확정하지 않습니다.

### Right close/X

우측 상단 red-dominant component를 찾고 edge proximity/shape를 평가하여 title ROI의 우측 안전 경계를 제공합니다.

### Magnifier

magnifier는 단순히 `좌측 상단의 밝고 네모난 component`로 인정하지 않습니다.

주요 evidence:

- header 내 상대 위치
- expected icon size 대비 크기
- aspect
- hollow/dark center
- bright ring perimeter
- lower-right handle
- 오른쪽 title glyph corroboration

structural panel-left가 실제 magnifier보다 안쪽으로 drift할 수 있으므로 search 영역을 제한적으로 왼쪽으로 확장합니다. 실제 magnifier가 복구되면 OCR ROI는 icon 오른쪽에서 시작하되 실제 첫 title glyph를 보존해야 합니다.

상세: `docs/SCANNER_V1.3.1_RECOGNITION.md`.

## 8. v1.3.2 live-evidence recognition delta

v1.3.2는 v1.3.1 공개 후 실제 Tarkov/DisplayTest에서 확인된 추가 OCR 실패를 근거로 한 PATCH입니다.

관측된 대표 사례:

- `Thermite 테르밋` → `` ` The「mite 테르밋`` 계열 OCR
- `Gunpowder "Eagle" 화약` → `` ` Gunpowde「 ...`` 계열 OCR

### Magnifier association

- nearby/following title glyph component는 **corroboration이지 prerequisite가 아님**
- glyph segmentation이 sparse해도 ring / hollow center / lower-right handle / expected left-header position이 충분하면 magnifier 후보 유지
- 첫 Korean glyph를 icon으로 오인하지 않도록 morphology와 left-position gate는 유지
- packaged-EXE smoke의 synthetic regression은 실제 live magnifier 약 21×19~20px scale에 맞춤

### OCR symbol policy

- punctuation/symbol whitelist는 current official Korean item catalog에서 catalog generation마다 파생
- catalog에 존재하는 quote/hyphen/parenthesis/slash/period 등은 보존
- `「`처럼 current catalog에 없는 punctuation/symbol은 matcher 전에 제거
- letter/digit는 OCR 자체 confusion일 수 있으므로 fuzzy/visual correction evidence로 유지
- CJK Han ideograph는 Korean item-title contract에서 hard reject
- symbol 제거 후 identity evidence가 지나치게 짧아지면 fail closed

### Bounded one-edit recovery

기존 normal fuzzy percentage threshold는 유지합니다.

- normalized length <= 6: 매우 엄격한 short-name gate 유지
- 7~12: 기존 medium-name percentage floor 유지
- 13+: 기존 long-name floor 유지
- normal top1/top2 margin 유지

별도 예외 경로는 다음 조건을 모두 만족할 때만 허용합니다.

```text
normalized official length >= 7
AND edit distance == 1
AND candidate is unique over the complete current catalog
AND best candidate is the ordinary match top1
AND best - global runner-up >= 10 percentage points
```

이 규칙은 `Thermite` 계열 한 글자 누락/치환을 복구하기 위한 제한적 경로입니다. multi-edit 저 80%대 OCR을 percentage만으로 확정하는 규칙이 아닙니다.

상세:

- `docs/SCANNER_V1.3.2_LIVE_EVIDENCE.md`
- `docs/DECISION_SCANNER_LIVE_EVIDENCE_2026-08-23.md`
- `docs/SCANNER_SYMBOL_POLICY.md`

## 9. OCR / semantic matcher

Primary text recognizer는 Windows `ko-KR` OCR입니다.

- title size에 따라 4x/6x/8x 확대
- first pass 실패 시 deep OCR/high-contrast/binary/inverse variants
- current catalog 기반 character/symbol policy
- arbitrary character replacement로 confidence를 인위적으로 올리지 않음
- official catalog exact-first + conservative fuzzy + margin
- ambiguous/low-confidence는 Item ID 미확정

## 10. Tarkov-font visual corroboration / recovery

게임 폰트 바이너리를 public package에 포함하지 않습니다.

```text
Tarkov resources.assets (read-only)
→ bounded SFNT discovery
→ %LocalAppData%/JunhyunHelper/scanner/fonts
→ source manifest + font generation key
→ Bender regular/bold + Korean fallback
→ current official item-name rendered templates/features
```

사용 방식:

- plausible OCR이 있으면 semantic shortlist + title-font verifier
- OCR이 비거나 심하게 손상되면 strict full-catalog visual matcher
- semantic OCR success에서도 명확한 시각 모순이 있을 때만 보수적으로 교정
- visual unavailable/error/ambiguous이면 healthy OCR을 임의로 폐기하지 않음
- current catalog 밖 Item 생성 금지
- top1 score + margin 부족 시 reject

Font/template cache는 generation-aware + bounded입니다.

## 11. Scanner 사용자 분석 / 단축키

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

## 12. 인식 이미지 / diagnostics

최신 diagnostic frame 1개를 메모리에 유지합니다.

확인 가능한 정보:

- capture source/origin
- selected detail bounds
- title ROI
- magnifier / close anchor bounds
- structural/header evidence
- OCR/visual pass
- OCR text
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

## 13. Scanner catalog / 표시 데이터

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

## 14. Mini Scanner

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

## 15. Persistence / 사용자 데이터

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

Program package와 사용자 데이터는 분리됩니다. v1.3.1 → v1.3.2는 user.db migration이나 mandatory Game Content update가 없습니다.

## 16. Map / MiniMap

Map/MiniMap은 pinned donor source를 제한적으로 compile-link한 독립 subsystem입니다.

- general marker/artwork/config → pinned Map bundle
- current Quest state/geometry → JunhyunHelper bridge
- donor updater/content DB/global hidden command/legacy logger는 product ownership에서 제외
- 구체적 defect/performance evidence 없이 broad refactor하지 않음

## 17. Program Update / 배포 계약

정식 release는 다음 순서를 지켜야 합니다.

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

## 18. v1.3.2 공개 검증 결과

완료:

- PR #144 final CI `32619142034`: SUCCESS
- automated tests: **263 passed / 0 failed / 0 skipped**
- exact-source release run `32621021058`: SUCCESS
- exact release source/tag: `922797a99ea221fdc4984dd6ed05df552149d6e4`
- public/latest: VERIFIED
- public ZIP re-download/hash/size/checksum/layout: VERIFIED
- ProductVersion/FIRST_RUN: VERIFIED
- public-downloaded Product UI + Scanner + Mini Scanner + Main Map + Factory + MiniMap smoke: SUCCESS

공개 asset:

```text
Junhyun-Helper-v1.3.2-win-x64.zip
80,311,752 bytes
SHA-256 6e3a7af2de50dfd14f1c49ccb39753177a0bce5b22993bb8bb94ffde93086767
```

## 19. 현재 알려진 열린 영역

제품 기능 결함으로 확정된 release blocker는 없습니다.

Scanner는 **실제 Tarkov live calibration을 계속하는 단계**입니다. 아직 모든 해상도/DPI/UI 위치/아이템명 조합을 실게임에서 검증한 것은 아닙니다.

새 evidence가 발생하면 다음 순서로 분리합니다.

```text
capture
→ detail structural detection
→ header anchor/title ROI
→ OCR
→ catalog sanitation/matcher
→ Tarkov-font visual corroboration
→ Item ID
→ trader/flea/RequiredTotal presentation
→ Mini Scanner / stale-state handling
```

실제 실패 evidence 없이 confidence/margin을 완화하지 않습니다.

## 20. 다음 작업

1. 실제 Tarkov Borderless 환경에서 다양한 아이템/위치/해상도/DPI로 사용
2. 성공 / 미인식 / 오인식 / detector / ROI / OCR / matcher / visual 실패 분류
3. 정확히 인식된 Item ID에서 highest trader / flea `avg24hPrice` / `RequiredTotal` end-to-end 검증
4. 빠른 연속 scan의 stale isolation 검증
5. 장시간 CPU / memory / UI responsiveness 검증
6. 실제 실패를 regression으로 먼저 고정한 뒤 해당 단계만 수정

상세 작업 목록: `docs/NEXT.md`.
