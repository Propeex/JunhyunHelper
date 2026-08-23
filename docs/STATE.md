# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-23

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램입니다.

핵심 기능:

- GameMode별 Profile / User Progress
- Quest availability / Hideout / Needed Items / Inventory
- Items / Ammo
- Map + MiniMap
- Game Content 안전 업데이트
- 사용자 동의형 Program Update
- Scanner + Mini Scanner

Runtime GPT/AI 의존성은 없습니다.

## 2. 현재 공개 릴리즈

현재 public stable은 **v1.3.1**입니다.

```text
version: v1.3.1 PUBLIC RELEASE / VERIFIED
release source: 028bfb600f4662962a0daac1dad04b570e018275
final PR CI: 32615869812 — SUCCESS
automated tests: 256 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.3.1-win-x64.zip
bytes: 80,310,221
SHA-256: 5c4b79cc5d373b4a28cbeb10be18b8369086b2ee9f0edc172530028dd71b1c3f
ProductVersion: 1.3.1+028bfb600f4662962a0daac1dad04b570e018275
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.3.1
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.0 → v1.3.1 mandatory Game Content update: none
v1.3.0 → v1.3.1 user.db migration: none
```

상세 검증 기록:

- `docs/RELEASE_1.3.1.md`
- `docs/.release-v1.3.1-status.json`

## 3. 제품 아키텍처 기준

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

- Core: canonical domain과 deterministic 계산
- Application: 사용자 유스케이스와 authoritative mutation
- Infrastructure: HTTP/source parsing, SQLite/file persistence, content/scanner/update I/O
- Desktop: WPF UI, presentation, Scanner capture/OCR/runtime, Map bridge
- Map/MiniMap donor는 제한적 compile-link 예외이며 donor updater/content ownership은 사용하지 않음

현재 pinned Map donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

## 4. Scanner 장기 제품 계약

Scanner는 게임 프로세스 내부 데이터를 읽지 않는 독립적인 화면 기반 보조 기능입니다.

```text
Tarkov / Display pixels
→ detail-window structural candidates
→ inspect-header structural refinement
   - dark title field
   - right red close/X
   - left magnifier shape
   - following first title glyphs
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ current official Korean catalog semantic matching
→ optional local Tarkov-font visual corroboration/recovery
→ confidence + top1/top2 margin gates
→ Item ID
→ local JunhyunHelper presentation data
→ Mini Scanner
```

장기 원칙:

- false positive보다 miss 선호
- geometry/structural/anchor score는 후보 evidence이지 Item identity가 아님
- matcher confidence/top1-top2 margin을 편의상 완화하지 않음
- current official Korean item catalog가 identity 권위
- historical alias를 production identity source로 무제한 누적하지 않음
- icon 하나만으로 Item identity 확정 금지
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지
- ambiguity/low confidence는 fail closed

## 5. Scanner capture

### TarkovWindow

```text
EscapeFromTarkov process/window
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ PrintWindow 우선
→ invalid/empty이면 exact client screen rectangle fallback
```

최소화 또는 유효하지 않은 client-area는 인식하지 않습니다. `PrintWindow` sparse validation은 불필요한 1440p/4K duplicate full-frame managed copy를 만들지 않습니다.

### DisplayTest

연결된 전체 디스플레이를 대상으로 TarkovWindow와 동일한 detector/OCR/catalog/presentation pipeline을 사용합니다. real/test continuous mode는 상호 배타적입니다.

### One-shot

- 1회 인게임: TarkovWindow capture를 한 번 정밀 분석
- 1회 테스트: 모든 연결 디스플레이를 한 번 정밀 분석
- continuous mode를 영구 변경하지 않음
- shared recognition state와 직렬화
- scan-time catalog network refresh를 시작하지 않음

## 6. Scanner Lab structural geometry

Scanner Lab v3.8 기반 RED-X/rectangle 구조가 production detail-window geometry 기준입니다.

- red-X connected component 후보
- rectangle/edge fallback 후보
- IoU deduplication
- 최대 8 candidates
- structural floor `0.34`
- structural score는 Item identity 점수가 아님
- continuous mode에서는 동일 quantized geometry가 안정화된 뒤 semantic recognition 수행
- verified detail/title signature가 유지되면 OCR 반복 억제

## 7. v1.3.1 inspect-header / title ROI

v1.3.1은 실제 Tarkov에서 관측된 `첫 글자 → 돋보기 오인` 실패를 수정한 PATCH입니다.

### Title-field evidence

상세창 상단의 어두운 neutral header strip을 먼저 구조 evidence로 사용합니다. panel-relative 좌표 하나만으로 title lane을 확정하지 않습니다.

### Right close/X

우측 상단에서 red-dominant component를 찾고 edge proximity/shape를 함께 평가합니다. 이 anchor는 title ROI의 우측 안전 경계를 제공합니다.

### Magnifier

magnifier는 더 이상 `좌측 상단의 밝고 네모난 component`만으로 인정하지 않습니다.

평가 evidence:

- header 내 상대 위치
- expected icon size 대비 크기
- aspect
- hollow/dark center
- bright ring perimeter
- lower-right handle
- 오른쪽에 뒤따르는 title glyph evidence

### Panel-left drift

structural panel left가 실제 magnifier보다 안쪽으로 잡힐 수 있으므로 magnifier search 영역을 제한적으로 왼쪽으로 확장합니다. 실제 magnifier가 복구되면 OCR ROI는 그 오른쪽에서 시작하되 실제 첫 title glyph는 포함해야 합니다.

### Regression

packaged-EXE smoke에서 다음을 합성합니다.

- 실제 magnifier ring+handle
- 더 작은 Korean-like first glyph
- 일부러 안쪽으로 drift한 panel-left
- dark title field
- red close/X

실제 magnifier 선택 + first glyph 보존이 모두 충족되어야 smoke가 성공합니다.

상세: `docs/SCANNER_V1.3.1_RECOGNITION.md`.

## 8. OCR / character policy / semantic matching

Primary text recognizer는 Windows `ko-KR` OCR입니다.

- title size에 따라 4x/6x/8x 확대
- first pass 실패 시 deep OCR/high-contrast/binary/inverse variants
- current official Korean catalog에서 허용 문자 집합 파생
- unexpected character는 corrupted evidence
- 한자는 Korean title contract에서 invalid evidence
- 임의 문자 치환으로 confidence를 올리지 않음
- official catalog exact-first + conservative fuzzy + margin
- ambiguous/low confidence는 Item ID 미확정

## 9. Tarkov-font visual corroboration / recovery

게임 폰트 바이너리를 public package에 포함하지 않습니다.

```text
Tarkov resources.assets (read-only)
→ bounded SFNT discovery
→ %LocalAppData%/JunhyunHelper/scanner/fonts
→ source manifest + actual font-binary generation key
→ Bender regular/bold + Noto CJK KR support
→ current official item-name rendered templates/features
```

### OCR failure/corruption

- plausible OCR text가 있으면 semantic shortlist + title-font verifier
- OCR이 비거나 심하게 손상되면 strict full-catalog visual matcher
- current catalog 밖 Item 생성 금지
- top1 score + top1/top2 margin 부족 시 reject

### OCR semantic success — v1.3.1

semantic OCR success도 필요 시 local Tarkov-font/current-catalog renderer로 corroborate할 수 있습니다.

- visual result가 같은 Item ID → OCR 유지
- visual evidence unavailable/error/ambiguous → healthy OCR 유지
- strict visual evidence가 다른 current official Item ID를 명확히 가리킬 때만 identity 교정

따라서 visual layer는 모든 OCR success를 거부하는 mandatory gate가 아니라 명확한 시각 모순을 교정하는 conservative hardening입니다.

Font/template cache는 generation-aware + bounded입니다. Tarkov source/font generation 변경 후 stale template을 재사용하지 않습니다.

## 10. v1.3.0 사용자 분석 / 단축키 워크플로

Scanner display settings schema는 **v4**입니다.

전역 기본키:

- 1회 인게임 스캔: `Ctrl+Shift+F10`
- 1회 테스트 스캔: `Ctrl+Shift+F11`
- Scanner ON/OFF: `Ctrl+Shift+F12`

계약:

- MainWindow lifetime 동안 Scanner 탭 밖에서도 동작
- Scanner 탭 `단축키 설정`에서 각각 변경/비활성화
- 동일 gesture 중복 금지
- v1.2.x/schema v3 one-shot 사용자 키를 인게임 one-shot으로 우선 보존
- 기존 사용자 키와 신규 기본키가 충돌하면 신규 명령 쪽만 non-conflicting fallback
- one-shot 인게임/테스트 버튼은 제품 UI에 없음

## 11. 인식 이미지 / diagnostics

`인식 이미지`는 최신 diagnostic frame 1개를 메모리에 유지합니다.

표시 가능한 정보:

- capture source/origin
- selected detail bounds
- title ROI
- magnifier / close anchor bounds
- structural/header evidence
- OCR/visual pass
- OCR text
- candidate official name
- confidence / second score / reason

v1.3.0부터 사용자가 명시적으로 `이미지 저장`을 선택하면 **분석에 실제 사용된 원본 frame**을 PNG로 export할 수 있습니다.

- 자동 screenshot 저장 없음
- export PNG에는 diagnostic rectangle/text를 합성하지 않음
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

catalog disk load/network refresh는 동일 mode-transition gate로 직렬화되어 이전 GameMode operation이 최신 state를 덮어쓰지 못합니다.

표시 의미:

- highest trader sell price = 유효한 non-flea RUB 환산 판매가 최댓값
- flea average = positive `avg24hPrice`
- slots = positive `width × height`
- price/slot = valid price와 slots가 모두 존재할 때만
- current needed = `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal`

Inventory 차감 부족량은 Scanner의 `필요 개수` 의미가 아닙니다. market/dimension 누락은 해당 표시 필드만 fail closed하고 Item identity health와 분리합니다.

## 13. Mini Scanner

- match 성공 item 정보만 표시
- runtime/OCR/error/status text는 overlay에 표시하지 않음
- WPF Topmost + native HWND_TOPMOST
- ShowActivated=false / no-activate
- 전체 카드 drag hit surface / Arrow cursor
- 실제 Scanner mode에서는 Tarkov foreground + inventory/stash context를 보수적으로 확인
- inventory/stash OCR probe 최대 1개
- 반복 요청 latest coalesce
- item/context epoch가 바뀐 stale result 화면 적용 금지

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

Program package와 사용자 데이터는 분리됩니다. v1.3.0 → v1.3.1은 user.db migration이나 mandatory Game Content update가 없습니다.

## 15. Map / MiniMap

Map/MiniMap은 pinned donor source를 제한적으로 compile-link한 독립 subsystem입니다.

- general marker/artwork/config → pinned Map bundle
- current Quest state/geometry → JunhyunHelper bridge
- donor updater/content DB/global hidden command/legacy logger는 product ownership에서 제외
- 구체적 defect/performance 근거 없이 broad refactor하지 않음

## 16. Program Update / 배포

정식 release 검증 계약:

```text
exact release source
→ build/tests/publish/package audit
→ actual EXE Product UI/Scanner/Map smoke
→ ZIP + SHA256SUMS
→ Draft release
→ Draft asset re-download verification
→ Draft-downloaded EXE smoke
→ public/latest
→ exact tag verification
→ public asset re-download verification
→ public-downloaded EXE smoke
```

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

업데이트는 program-owned files만 교체하며 `%LocalAppData%/JunhyunHelper` 사용자 데이터를 건드리지 않습니다.

## 17. v1.3.1 공개 검증

PR #143 final CI `32615869812`와 독립 public finalizer에서 다음을 통과했습니다.

- Release build
- 256/256 automated tests
- win-x64 self-contained single-file publish
- package root/ProductVersion/FIRST_RUN audit
- inspect-header synthetic regression
- Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap actual EXE smoke
- exact tag source `028bfb600f4662962a0daac1dad04b570e018275`
- public/latest
- public ZIP + SHA256SUMS re-download
- public-downloaded ProductVersion/root layout
- public-downloaded actual EXE smoke
- graceful shutdown / portable-root cleanliness

공개 asset:

```text
Junhyun-Helper-v1.3.1-win-x64.zip
80,310,221 bytes
SHA-256 5c4b79cc5d373b4a28cbeb10be18b8369086b2ee9f0edc172530028dd71b1c3f
```

## 18. 현재 개발 우선순위

현재 Scanner는 기능 추가보다 **실제 Tarkov 화면에서의 recognition calibration**이 우선입니다.

사용자가 제공하는 실패 evidence의 기본 단위:

```text
실제 아이템 이름
+ success / miss / wrong identity 결과
+ 문제 발생 직후 저장한 인식 원본 PNG
+ 필요 시 scanner.log
```

개선 순서:

```text
live evidence
→ capture 문제인지 확인
→ structural candidate
→ inspect-header/title ROI
→ OCR/font visual
→ catalog identity
→ presentation/data
→ regression
```

실제 evidence 없이 recognition threshold를 추측해서 완화하지 않습니다.

## 19. 공식 문서 우선순위

- 현재 상태: `docs/STATE.md`, `docs/CURRENT_STATE.md`
- 제품 요구사항: `docs/PRODUCT.md`
- 전체 아키텍처: `docs/ARCHITECTURE.md`
- Scanner 기준선: `docs/SCANNER.md`
- v1.3.1 recognition 계약: `docs/SCANNER_V1.3.1_RECOGNITION.md`
- Scanner 검증: `docs/SCANNER_TEST_PLAN.md`
- 공개 릴리즈 증거: `docs/RELEASE_1.3.1.md`
- 결정: `docs/DECISIONS.md` 및 날짜/주제별 decision 문서
- 구현 참조: `docs/DEVELOPER_REFERENCE.md`

새 대화에서는 저장소 전체를 다시 분석하지 말고 이 문서와 관련 전문 문서를 읽은 뒤 필요한 코드만 확인합니다.
