# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-23

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램입니다.

핵심 기능: Profile/User Progress, Quest, Hideout, Needed Items/Inventory, Items, Ammo, Map/MiniMap, Game Content Update, Program Update, Scanner/Mini Scanner. Runtime GPT/AI 의존성은 없습니다.

## 2. 현재 공개 릴리즈

```text
v1.3.0 PUBLIC RELEASE / VERIFIED
release source: f03441672d39165678fa53f57af46f103070d50e
final PR #142 CI: 32611343850 — SUCCESS
public verification status commit: 4cefa27012eafa62d40ef99f4efd630f3c53127a
tests: 256 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.3.0-win-x64.zip
bytes: 80,306,655
SHA-256: 5880c71098d737b7ffd3447eb77a55195d09d76ea12be7ff79df4eb055ac8344
ProductVersion: 1.3.0+f03441672d39165678fa53f57af46f103070d50e
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache: v1/v2 readable, v2 written
v1.2.2 → v1.3.0 mandatory Game Content update: none
v1.2.2 → v1.3.0 user.db migration: none
```

상세: `docs/RELEASE_1.3.0.md`.

## 3. 아키텍처

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
- pinned Map donor revision: `d933792b6042a51cea38dc44b686a096fe30de67`

## 4. Scanner recognition 계약

```text
Tarkov / Display pixels
→ Scanner Lab v3.8 structural candidates
→ red close + magnifier + title-field refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ official-name semantic resolution
   OR conservative current-catalog Tarkov-font visual recovery
→ confidence + top1/top2 margin gates
→ Item ID or fail closed
→ local presentation data
→ Mini Scanner
```

장기 원칙:

- false positive보다 miss 선호
- structural/anchor score만으로 Item identity 확정 금지
- current official Korean item catalog가 identity 권위
- confidence/margin 임의 완화 금지
- icon 단독 identity 금지
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지

## 5. v1.3.0 Scanner workflow

### Recognition image export

- latest raw recognition source frame 1개를 process memory에 유지
- 사용자가 `인식 이미지` → `이미지 저장`을 선택할 때만 PNG export
- export PNG에는 diagnostic overlay를 합성하지 않음
- automatic raw screenshot disk persistence는 하지 않음
- `로그 삭제`는 user-exported PNG를 삭제하지 않음

### One-shot

- one-shot in-game = `ScannerCaptureMode.TarkovWindow`
- one-shot test = `ScannerCaptureMode.DisplayTest`; 모든 연결 디스플레이를 한 번만 동일 pipeline으로 검사
- 두 one-shot은 Scanner 탭 버튼 없이 global hotkey-only
- continuous Scanner/Test 상태를 영구 변경하지 않음
- local healthy catalog만 사용, scan-time network refresh 없음
- 기존 one-shot/continuous/profile lifecycle serialization 유지

### Global hotkeys

```text
Ctrl+Shift+F10  1회 인게임 스캔
Ctrl+Shift+F11  1회 테스트 스캔
Ctrl+Shift+F12  Scanner ON/OFF
```

- Scanner 탭의 하나의 설정 창에서 각 command 변경/disable
- duplicate gesture 저장 금지
- registration ownership은 ScannerPage가 아니라 MainWindow lifetime
- Scanner settings schema v4
- v3 `OneShotHotkey` 사용자 값은 v4 in-game one-shot으로 우선 보존
- 기존 사용자 값과 신규 default 충돌 시 신규 command만 비충돌 fallback 사용

## 6. Scanner data contract

- best trader = 유효한 non-flea RUB 환산 판매가 최댓값
- flea average = positive `avg24hPrice`
- price/slot = 유효 price와 positive width×height가 모두 있을 때만 계산
- current needed = `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal`
- Inventory 차감 부족량을 Scanner 필요 수량 의미로 사용하지 않음
- 시장/크기 누락은 해당 field만 fail closed하고 Item identity 전체를 버리지 않음

## 7. 유지되는 deterministic hardening

- generation-aware Tarkov title-font cache / bounded visual caches
- Mini Scanner inventory OCR single-active/coalesced/stale-result reject
- one-shot/profile/GameMode lifecycle serialization
- shutdown-safe font-aware OCR lifetime
- PrintWindow sparse validation duplicate full-frame allocation 제거
- Scanner catalog disk load/network refresh mode-transition serialization

## 8. Persistence

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/
%LocalAppData%/JunhyunHelper/scanner/fonts/
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

Program package와 사용자 데이터는 분리합니다.

## 9. Release gate

v1.3.0은 다음을 통과했습니다.

- Windows Release build
- 256/256 automated tests
- win-x64 self-contained single-file publish/root audit
- ProductVersion/FIRST_RUN identity audit
- rendered Scanner v1.3 UI contract
- schema v4/default/v3 migration self-check
- Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap actual EXE smoke
- graceful shutdown / clean portable root
- Draft asset re-download verification + EXE smoke
- public/latest + exact public tag verification
- public asset checksum/root/ProductVersion/FIRST_RUN re-download verification
- independent public-downloaded EXE smoke

## 10. 후속 원칙

최신 Tarkov live E2E calibration은 실제 사용자 환경에서 계속합니다. 실제 사용 중 문제가 생기면 `scanner.log`, `인식 이미지`, 필요 시 explicit PNG export로 근거를 고정한 뒤 capture → geometry/anchors → OCR/visual matcher → catalog → presentation → inventory gate → overlay → resource usage 순으로 분리합니다. Live evidence 없이 threshold를 추측 변경하지 않습니다.