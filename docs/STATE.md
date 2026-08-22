# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-22

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

현재 public stable은 **v1.2.1**입니다.

```text
version: v1.2.1 PUBLIC RELEASE / VERIFIED
release source: 8c0de649f18d7caa4f5669a06511c15e784dfd29
final PR CI: 32540688111 — SUCCESS
exact-source release run: 32542259521 — SUCCESS
automated tests: 255 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.1-win-x64.zip
bytes: 80,306,749
SHA-256: 48a8b54fcdc3346a092ef3da2744f2d4ca7e27d99da5b52e3ebee7b55fa0affa
ProductVersion: 1.2.1+8c0de649f18d7caa4f5669a06511c15e784dfd29
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.2.1
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v3
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.2.0 → v1.2.1 mandatory Game Content update: none
v1.2.0 → v1.2.1 user.db migration: none
```

상세 검증 기록은 `docs/RELEASE_1.2.1.md`에 있습니다.

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

## 4. Scanner 제품 계약

Scanner는 게임 프로세스 내부 데이터를 읽지 않는 독립적인 화면 기반 보조 기능입니다.

```text
Tarkov / Display pixels
→ detail-window structural candidates
→ red close + magnifier + title-field anchor refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ current official Korean catalog semantic matching
   OR conservative current-catalog Tarkov-font visual recovery
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

## 5. Scanner recognition 구조

### Structural geometry

- Scanner Lab v3.8의 RED-X/rectangle structural candidate 구조 유지
- 최대 8 candidate
- structural floor `0.34`
- 동일 quantized geometry가 연속 관측될 때만 continuous semantic recognition 안정화
- verified detail/title signature가 유지되면 OCR 반복을 억제하고 presentation만 갱신

### Title anchor

- 우측 red close/X
- 좌측 magnifier/search icon
- 어두운 title-field strip

magnifier evidence가 충분하면 실제 OCR ROI는 magnifier 오른쪽부터 시작합니다. anchor가 불확실하면 검증된 Scanner Lab geometry ROI로 fallback합니다.

v1.2.1에서 anchor diagnostic score는 단순 존재 여부를 1.0으로 승격하지 않고 실제 detector score를 보존합니다. 이는 진단 정확도 개선이며 Item ID acceptance threshold 변경이 아닙니다.

### OCR / character policy

- Windows `ko-KR` OCR이 primary path
- current official Korean catalog에서 허용 문자 집합 계산
- catalog에 없는 unexpected character는 corrupted OCR evidence
- Han ideograph는 Korean item-title contract에서 invalid evidence
- 임의 문자 치환으로 confidence를 인위적으로 높이지 않음
- existing OCR semantic success는 font visual recovery가 덮어쓰지 않음

### Tarkov-font visual recovery

OCR이 비거나 손상된 경우 current official item-name universe 안에서만 visual recovery를 수행할 수 있습니다.

- Bender regular/bold + Noto Sans CJK KR local font support
- conservative top1 score + top1/top2 margin 필요
- ambiguous candidate는 거부
- arbitrary text/Item 생성 금지
- visual result도 current catalog identity를 통과해야 함

## 6. v1.2.1 deterministic Scanner hardening

### Font source/cache generation

- Tarkov `resources.assets`를 전체 managed byte array로 읽지 않고 bounded streaming scan
- source path/length/last-write를 `scanner/fonts/font-cache.json` manifest에 기록
- 실제 Bender/Noto cache 바이너리 SHA-256 조합을 generation key로 사용
- Tarkov source generation 또는 font generation 변경 시 loaded fonts/rendered templates 재사용 금지
- 부분/중단 추출에서 구세대와 신세대 font variant 혼합 방지
- corrupt/unusable font cache는 font recovery만 fail-soft로 비활성화하고 primary OCR path 유지
- 게임 font 바이너리는 배포 파일에 포함하지 않음

### Visual cache bounds

- OCR-guided title template cache bounded
- full-catalog glyph-mask cache bounded
- full-catalog aspect cache bounded
- 모든 visual cache key에 exact font generation 포함

장시간 Scanner 실행에서 rendered template cache가 무제한 증가하지 않으며 Tarkov 업데이트 후 stale glyph template을 재사용하지 않습니다.

### Mini Scanner inventory probe

- inventory/stash OCR probe 동시 실행 최대 1개
- 반복 `Show` 요청은 latest snapshot으로 coalesce
- item 변경 시 stale probe cancel
- epoch가 바뀐 old result는 화면 적용 금지
- inventory context가 불확실하면 Mini Scanner hidden

### One-shot/profile lifecycle

- one-shot은 기존 continuous loop 실제 종료를 await한 뒤 shared detector/OCR/presentation state 사용
- one-shot 종료 후 최신 사용자 state가 같은 mode를 여전히 요청할 때만 이전 mode 복구
- profile/GameMode monitor는 one-shot gate 뒤 최신 context를 다시 읽음
- stale monitor tick이 이전 profile/mode를 부활시키지 않음

### Shutdown/resource lifetime

Font-aware OCR은 active-operation lease를 사용합니다. Dispose 요청 이후 신규 operation은 거부하고, 이미 실행 중인 recognition이 종료된 뒤 Skia/font resources를 해제합니다.

### Capture allocation

`PrintWindow` visual-content validation은 locked bitmap의 sparse pixel을 직접 읽습니다. 이 사전 검사 때문에 1440p/4K 전체 frame을 별도의 managed array로 한 번 더 복사하지 않습니다. 실제 detector용 BGRA copy는 유지합니다.

## 7. 1회 고정밀 스캔 / 단축키

- continuous Scanner OFF에서도 `1회 고정밀 스캔` 가능
- 기본 global hotkey: `Ctrl+Shift+F10`
- Scanner UI에서 변경/비활성화 가능
- Scanner display settings schema v3
- local healthy Scanner catalog만 사용
- one-shot이 scan-time network refresh를 시작하지 않음
- duplicate invocation은 coordinator gate로 직렬화/거부

## 8. 인식 이미지 / diagnostics

`인식 이미지`는 process memory에 최신 diagnostic frame 1개만 유지합니다.

표시/기록 가능한 정보:

- capture source/origin
- selected detail bounds
- title ROI
- magnifier / close anchor bounds
- structural/title-anchor evidence
- OCR/visual recognition pass
- OCR text
- candidate official name
- confidence / second score / reason

스크린샷/raw pixel은 디스크에 저장하지 않습니다.

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

로그 삭제는 recent activity, current/rotated scanner log, 최신 in-memory recognition image를 함께 clear합니다. 진단 I/O 실패는 Scanner fatal이 아닙니다.

## 9. Mini Scanner

- match 성공 item 정보만 표시
- runtime/OCR/error/status text는 overlay에 표시하지 않음
- WPF Topmost + native HWND_TOPMOST
- ShowActivated=false / no-activate
- 전체 카드가 drag hit surface
- drag cursor는 Arrow
- MiniMap과 독립 Window/service/settings lifecycle
- 실제 Scanner mode에서는 Tarkov foreground + inventory/stash context를 보수적으로 확인
- test/preview path는 deterministic 개발 검증을 위해 context gate bypass 가능

Title OCR과 inventory-context OCR은 `SerializedScannerOcrEngine`을 통한 하나의 WinRT OCR serialization boundary를 공유합니다.

## 10. Scanner catalog / market 계약

Identity catalog health:

```text
accepted item count >= 4000
AND every accepted item has non-empty Item ID
AND every accepted item has non-empty official name
```

시장 데이터 coverage는 identity health와 분리합니다.

- raw `traderPrices` 지원
- derived `sellFor` 지원
- best trader price = 유효한 non-flea RUB 환산 최고가
- flea average = positive `avg24hPrice`
- slots = positive width × height
- price/slot = valid price와 slots가 모두 있을 때만 계산
- market/dimension 누락/오류는 해당 표시 필드만 fail closed

4,000개 valid identity에 trader price가 0개여도 identity catalog는 사용 가능하며, 3,999개 identity catalog는 거부합니다.

현재 필요한 수량:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

Inventory 차감 부족량을 Scanner 의미로 사용하지 않습니다.

## 11. Icon / local cache

- Game Content update에서 canonical item 전체 icon prefetch
- 실제 scan 순간에는 icon HTTP 없음
- local image-cache만 사용
- 개별 icon 실패는 전체 update/identity를 fatal로 만들지 않음
- decode/freeze 성공 아이콘은 process-local cache에서 재사용 가능

## 12. Persistence / 사용자 데이터

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

Program package와 사용자 데이터는 분리되어 있습니다. v1.2.0 → v1.2.1은 user.db migration이나 mandatory Game Content update가 없습니다.

## 13. Map / MiniMap

Map/MiniMap은 pinned donor source를 제한적으로 compile-link한 독립 subsystem입니다.

- general marker/artwork/config → pinned Map bundle
- current Quest state/geometry → JunhyunHelper bridge
- donor updater/content DB/global hidden command/legacy logger는 product ownership에서 제외
- 구체적 defect/performance 근거 없이 broad refactor하지 않음

Main Map cross-floor smoke는 donor의 bounded settle timer와 product-owned opacity `0.75`가 steady-state로 650 ms 유지되는 것을 검증합니다. post-v1.2.0 smoke hardening은 test harness 안정화이며 public v1.2.0 binary를 변경하지 않았습니다.

## 14. Program Update / 배포

정식 release는 Draft-first 검증을 사용합니다.

```text
exact release source
→ build/tests/publish/smoke
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

릴리즈 완료 후 일회성 release/verify workflow와 status marker는 제거합니다. 상시 workflow는 `.github/workflows/ci.yml`만 유지하는 것이 기본입니다.

## 15. v1.2.1 검증 결과

Final PR CI `32540688111`과 exact-source release run `32542259521`에서 다음을 통과했습니다.

- exact release source `8c0de649f18d7caa4f5669a06511c15e784dfd29`
- Windows Release build
- **255 automated tests / 0 failure / 0 skipped**
- win-x64 self-contained single-file publish
- ProductVersion / FIRST_RUN / package root audit
- Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap actual EXE smoke
- one-shot mode restoration / title-anchor-magnifier product smoke
- graceful shutdown / clean portable root
- Draft package re-download verification
- Draft-downloaded EXE smoke
- public/latest transition
- exact public tag source verification
- public package re-download verification
- public-downloaded EXE smoke

Public asset:

```text
Junhyun-Helper-v1.2.1-win-x64.zip
80,306,749 bytes
SHA-256 48a8b54fcdc3346a092ef3da2744f2d4ca7e27d99da5b52e3ebee7b55fa0affa
ProductVersion 1.2.1+8c0de649f18d7caa4f5669a06511c15e784dfd29
```

중복 release-controller run `32542441274`는 canonical public v1.2.1이 이미 생성된 뒤 Draft re-download 단계에서 canonical public ZIP과 자신의 별도 ZIP hash가 달라 중단됐습니다. public release/tag/source/assets는 변경하지 않았습니다. 상세는 `docs/RELEASE_1.2.1.md`에 기록합니다.

## 16. 실제 Tarkov 후속 검증

최신 Tarkov live E2E calibration은 public release blocker가 아니며 실제 사용자 환경에서 계속 검증합니다.

문제 발생 시 다음 순서로 근거를 분리합니다.

1. capture source/window state
2. detail structural candidate geometry
3. close/magnifier/title anchor evidence
4. actual title ROI
5. OCR character policy
6. semantic/visual matcher
7. current catalog Item ID
8. presentation/market/RequiredTotal bridge
9. inventory/stash visibility gate
10. Mini Scanner window behavior
11. long-session CPU/memory/handles/OCR rate

`scanner.log`와 `인식 이미지`로 재현 조건을 고정한 뒤 evidence-based PATCH를 적용합니다. live 근거 없이 confidence/margin을 낮추지 않습니다.

## 17. 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / fail-closed availability |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / steady-state product smoke 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.2.1 public package verified |
| Scanner + Mini Scanner | **v1.2.1 public verified / live Tarkov calibration 및 evidence-based follow-up 진행 대상** |

## 18. 다음 작업 원칙

현재 공개 제품은 v1.2.1입니다. 다음 Scanner 수정은 실제 사용에서 관측된 miss/false positive/performance evidence를 우선합니다.

- threshold를 추측해서 낮추지 않음
- existing public behavior를 코드가 존재한다는 이유만으로 설계 요구사항으로 승격하지 않음
- product contract와 current implementation 충돌 시 원인과 영향을 문서화한 뒤 수정
- 기능 추가보다 요청된 범위의 안정성/정확성/일관성을 우선
- 중요한 결정과 release 상태는 확정 즉시 저장소 문서에 기록
