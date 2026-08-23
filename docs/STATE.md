# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-24

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
- Scanner Ground Truth 교정 / 진단 dataset / full-pipeline regression

Runtime GPT/AI 의존성은 없습니다.

## 2. 현재 공개 릴리즈

현재 public stable / latest는 **v1.4.2**입니다.

```text
version: v1.4.2 PUBLIC RELEASE / VERIFIED
release source: a2d939b5f28e0d6de2468312bdd11467e3b35622
public tag source: a2d939b5f28e0d6de2468312bdd11467e3b35622
Scanner fix PR #160 CI: 32656154735 — SUCCESS
release-prep PR #161 CI: 32656572239 — SUCCESS
automated tests: 272 passed / 0 failed / 0 skipped
release run: 32656993853 — SUCCESS
independent public verifier: 32657225090 — SUCCESS
asset: Junhyun-Helper-v1.4.2-win-x64.zip
bytes: 80,385,620
SHA-256: e6aa57ac9492ebc3438335a5e0f66e4daf18c2b87b2b61abcb141de0f0d810a8
ProductVersion: 1.4.2+a2d939b5f28e0d6de2468312bdd11467e3b35622
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public SHA256SUMS: VERIFIED
public package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

현재 schema / compatibility:

```text
Desktop Version: 1.4.2
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
Scanner Ground Truth dataset: local diagnostics persistence
v1.4.1 → v1.4.2 mandatory Game Content update: none
v1.4.1 → v1.4.2 user.db migration: none
```

공식 검증 기록:

- `docs/RELEASE_1.4.2.md`
- `docs/.release-v1.4.2-status.json`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/CURRENT_SCANNER_WORK.md`

v1.4.2 공개 후 완료된 one-shot release/verifier workflow는 제거했으며 정상 CI는 `.github/workflows/ci.yml` 하나만 유지합니다.

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
- **Desktop**: WPF UI, presentation, Scanner capture/OCR/runtime/diagnostics, Map bridge
- **Map/MiniMap donor**: 제한적 compile-link 예외. donor updater/content ownership/hidden command는 사용하지 않음

현재 pinned Map donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

기존 `Propeex/Tarkov-Helper` 프로토타입은 요구사항의 권위가 아닙니다. 유지할 기능, 검증된 데이터/자산, 구현 아이디어, 시행착오 참고 용도로만 사용합니다.

## 4. Scanner 제품 계약

Scanner는 게임 프로세스 내부 데이터를 읽지 않는 독립적인 화면 기반 보조 기능입니다. 범용 OCR이 아니라 Tarkov UI 전용 폐쇄형 인식 시스템으로 취급합니다.

목표 pipeline:

```text
capture
→ detail-window detection
→ inspect-header / field localization
→ ROI extraction
→ OCR / visual recognition
→ current Tarkov catalog validation
→ final Item ID decision
→ mapped presentation
→ user correction / Ground Truth
→ dataset / export / regression
```

인식 단계와 실패 원인은 서로 분리합니다.

- Detail Window Detection
- Field Localization / Header Lock
- OCR Recognition
- Candidate Matching
- Parsing
- Data Mapping
- Unknown / Multiple

불변 원칙:

- false positive보다 miss를 선호
- geometry / structural score / anchor score / icon 하나만으로 Item identity 확정 금지
- current official Korean item catalog가 identity 권위
- historical alias를 production identity source로 무제한 누적하지 않음
- 실제 Ground Truth 없이 global confidence/margin/header threshold를 완화하지 않음
- ambiguity / low confidence / incomplete header lock은 fail closed
- current-catalog 밖 glyph를 특정 문자로 임의 치환하지 않음
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지

## 5. Capture / detail / header 기준

### TarkovWindow

```text
EscapeFromTarkov process/window
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ PrintWindow 우선
→ invalid/empty이면 exact client screen rectangle fallback
```

최소화 또는 유효하지 않은 client-area는 인식하지 않습니다.

### DisplayTest

연결된 전체 디스플레이에 TarkovWindow와 동일한 detector/OCR/catalog/presentation pipeline을 적용합니다. real/test continuous mode는 상호 배타적입니다.

### One-shot

- 1회 인게임: TarkovWindow를 한 번 정밀 분석
- 1회 테스트: 연결 display를 한 번 정밀 분석
- continuous mode를 영구 변경하지 않음
- shared recognition state와 직렬화
- scan-time catalog network refresh를 시작하지 않음

### Detail structural detection

- Scanner Lab v3.8 계열 구조
- red-X connected-component 후보
- rectangle/edge fallback 후보
- IoU deduplication
- continuous mode 최대 8 candidates
- one-shot 정밀 스캔 최대 12 candidates
- structural floor `0.34`
- structural score는 Item identity 점수가 아님
- continuous mode에서는 동일 quantized geometry가 안정화된 뒤 semantic recognition
- verified detail/title signature가 유지되면 불필요한 OCR 반복 억제

Initial structural rectangle을 최종 authoritative bounds로 간주하지 않습니다. Full header lock 후 magnifier/X 실측값에서 detail-window top/left/right를 다시 정렬하고 bottom은 구조 evidence를 사용합니다.

### Inspect header / title ROI

Runtime semantic gate:

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
```

- title glyph는 title ROI의 수평 ownership을 갖지 않음
- magnifier 후보는 frame-left search-icon lane에서 생성
- ring bright band + hollow center + handle + outside background evidence 사용
- close/X는 red body/edge + expected geometry + diagonal X contrast evidence 결합
- diagnostic-only structural candidate는 관찰/저장할 수 있으나 OCR identity path에 들어가지 않음

v1.4.1 fallback:

- primary `ScannerInspectHeaderLock`을 우선 사용
- primary가 fail-closed 한 경우 reviewed live Ground Truth 기반 header refiner 사용
- 어두운 red close X, neutral gray top border, 실제 magnifier orientation, dark title field/text evidence를 함께 검증
- recovered header도 최종 `HEADER_FRAME_LOCKED >= 0.68`을 통과해야 함

v1.4.2 contained-subpanel fallback:

- v1.4.1 실제 데이터에서 stash/inventory의 큰 구조 frame 내부 수백 px 아래에 실제 inspect header가 있는 실패를 확인
- primary + v1.4.1 live header refiner가 모두 실패했을 때만 oversized candidate 내부 proposal scan 실행
- proposal도 close X, magnifier, dark title field, title text evidence, frame ownership을 다시 검증
- 최종 `HEADER_FRAME_LOCKED >= 0.68` gate는 그대로 유지
- 단순히 큰 frame을 상세창으로 인정하거나 header threshold를 낮추는 수정이 아님

관련 문서:

- `docs/CURRENT_SCANNER_WORK.md`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/SCANNER_V1.3.4_LIVE_HARDENING.md`
- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`

## 6. OCR / matcher / visual corroboration

Primary recognizer는 Windows `ko-KR` OCR입니다.

- title size에 따라 확대 variant 사용
- first pass 실패 시 deep OCR / high-contrast / binary / inverse variants
- raw OCR과 matcher input 분리 보존
- current catalog 기반 character/symbol policy
- Korean-title contract에 맞지 않는 문자 evidence는 보수적으로 처리
- arbitrary replacement로 confidence를 인위적으로 올리지 않음
- official catalog exact-first + conservative fuzzy + margin
- current-catalog unique one-unknown-glyph recovery
- bounded unique one-edit recovery
- ambiguous/low-confidence는 Item ID 미확정

v1.4.2 bounded recovery:

실제 reviewed Ground Truth에서 `Grizzly`, `Emelya`, `Iskra`, `Axel` 계열 glyph 혼동이 확인됐습니다. 일부는 정답 item이 matcher top-1인데 2~3 glyph 오류로 기존 gate에서 거부됐습니다.

따라서 ordinary matcher가 실패한 경우에만:

- catalog 전체에서 유일하고 충분히 분리된 2-edit candidate
- 충분히 긴 suffix가 일치하고 catalog 전체에서 유일한 2~3-edit candidate

를 제한적으로 복구합니다.

변경하지 않은 것:

- global confidence threshold
- top1/top2 margin의 일반 완화
- `r`, `0`, 복잡한 한글 등의 전역 glyph 치환표
- ambiguous / low-evidence multi-edit fail-closed 정책

Tarkov-font visual path:

```text
Tarkov resources.assets (read-only)
→ bounded SFNT discovery
→ %LocalAppData%/JunhyunHelper/scanner/fonts
→ source manifest + font generation key
→ Bender regular/bold + Korean fallback
→ current official item-name rendered templates/features
```

- plausible OCR이면 semantic shortlist + title-font verifier
- OCR이 비거나 심하게 손상되면 strict full-catalog visual matcher
- healthy OCR은 명확한 시각 모순이 있을 때만 보수적으로 교정
- visual unavailable/error/ambiguous이면 OCR을 임의 폐기하지 않음
- current catalog 밖 Item 생성 금지

## 7. Ground Truth / correction / regression

공식 계약은 `docs/SCANNER_GROUND_TRUTH.md`입니다.

v1.4.0에서 교정/dataset/regression 기반을 제품화했습니다. 이후 실제 Tarkov Ground Truth를 알고리즘 변경의 근거로 사용합니다.

### Case와 evidence

모든 최신 diagnostic capture에 Case ID를 부여하고 같은 ID로 `scanner.log`, dataset directory, `case.json`을 연결합니다.

저장 root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

대표 보존물:

- `full.png`
- `detail_window.png`
- `annotated.png`
- detected/corrected item-name ROI
- OCR preprocessing evidence
- `case.json`
- raw OCR / matcher text
- program Item ID / official name
- confidence / second score / margin
- matcher top candidates
- pipeline stage
- user Ground Truth label
- mapped presentation

자동/미검증 Case의 `pipeline.stage`와 사용자가 검증한 `ground_truth_error_type`은 분리합니다. 자동 Case는 Ground Truth로 취급하지 않습니다.

### 사용자 교정

Scanner UI에서 사용자는 다음만 수행합니다.

- `맞음`
- 상세보기 영역 수정
- 아이템명 영역 수정
- 정답 아이템명 입력
- 영역 + 텍스트 동시 교정

JSON/좌표/파일명 관리는 요구하지 않습니다. 사용자 rectangle과 정답 텍스트가 Ground Truth입니다.

### Dataset 관리 / export

- Case 수와 총 용량 표시
- Case 목록 확인
- 선택 Case 삭제
- 전체 dataset 삭제
- 일반 Scanner 로그 별도 삭제
- `ScannerDiagnostics_YYYY-MM-DD.zip` export

Export에는 dataset/summary/environment/cases/images/logs를 포함하며 사용자 원본 화면 픽셀이 포함될 수 있음을 명시합니다.

### 자동 통계

- reviewed final accuracy
- Ground Truth 오류 유형
- observed pipeline stage
- detail ROI delta 평균/표준편차
- item-name ROI delta 평균/표준편차
- OCR observed → Ground Truth 문자 치환/삽입/누락 통계
- matcher top-candidate evidence

### Full-pipeline regression

Reviewed Ground Truth의 `full.png`를 현재 production 경로로 다시 실행합니다.

```text
full.png
→ detail geometry
→ inspect-header / contained-subpanel lock
→ title ROI
→ current OCR/deep OCR/font recovery
→ current catalog matching
→ final Item ID
```

결과:

- `STILL_CORRECT`
- `SOLVED`
- `STILL_FAILING`
- `REGRESSION`
- `ERROR`

과거 정상 Case가 현재 실패하면 평균 정확도가 상승했더라도 `REGRESSION`으로 취급합니다.

현재 알려진 기술 부채:

- regression path와 live path 일부 locked-window projection 로직 중복
- exact OCR-consumed processed bitmap을 모든 path에서 동일 객체로 직접 보존하는 구조는 추가 개선 여지 있음
- 실제 Tarkov rendered sample dictionary는 향후 Ground Truth 축적 후 검토
- ROI-only reviewed case의 geometry-only replay는 final identity replay보다 제한적

## 8. Scanner 표시 데이터 의미

현재 게임 화면 OCR 필드는 **`item_name` 하나**입니다.

다음은 화면 숫자 OCR이 아니라 Item ID 확정 이후 로컬 데이터에서 계산/조회하는 `mapped_data`입니다.

- highest trader sell price = 유효한 non-flea RUB 환산 판매가 최댓값
- flea average = positive `avg24hPrice`
- slots = positive `width × height`
- price/slot = valid price와 slots가 모두 존재할 때만
- required count = `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal`

Inventory 차감 부족량은 Scanner의 `필요 개수` 의미가 아닙니다. market/dimension 누락은 해당 표시 필드만 fail closed하고 Item identity health와 분리합니다.

가격·플리·슬롯·필요 개수를 OCR 필드로 새로 만들지 않습니다.

## 9. Scanner 사용자 UI / 단축키

Scanner display settings schema는 **v4**입니다.

기본 global hotkey:

- 1회 인게임: `Ctrl+Shift+F10`
- 1회 테스트: `Ctrl+Shift+F11`
- Scanner ON/OFF: `Ctrl+Shift+F12`

계약:

- MainWindow lifetime 동안 Scanner 탭 밖에서도 동작
- Scanner 탭에서 각각 변경/비활성화 가능
- 동일 gesture 중복 금지
- schema v3 one-shot 사용자 key를 schema v4로 보수적으로 승계

v1.4.2에서 단축키 설정 창의 `스캐너 ON/OFF` 세 번째 행이 아래쪽에서 잘리던 layout clipping을 수정했습니다. 단축키 기능 자체는 바뀌지 않았습니다.

Scanner 탭 주요 진단 버튼:

- 인식 이미지
- 교정
- 회귀 테스트
- 교정 데이터 내보내기
- 교정 데이터 관리
- 로그 삭제

일반 Scanner 로그 삭제와 Ground Truth dataset 삭제는 별도 동작입니다.

## 10. Mini Scanner

- match 성공 Item 정보만 표시
- runtime/OCR/error/status text는 overlay에 표시하지 않음
- WPF Topmost + native HWND_TOPMOST
- ShowActivated=false / no-activate
- 전체 카드 drag hit surface
- 실제 Scanner mode에서는 Tarkov foreground + inventory/stash context를 보수적으로 확인
- inventory/stash OCR probe 최대 1개
- 반복 요청은 latest coalesce
- item/context epoch가 바뀐 stale result는 화면에 적용하지 않음

Title OCR과 inventory-context OCR은 하나의 WinRT OCR serialization boundary를 공유합니다.

## 11. Persistence / 사용자 데이터

대표 저장 위치:

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/map-product-settings.json(.bak)
%LocalAppData%/JunhyunHelper/ammo-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
%LocalAppData%/JunhyunHelper/logs/
```

원칙:

- portable executable 옆에 mutable user data/log를 만들지 않음
- Program Update가 user.db, content cache, image cache, Map/Ammo/Scanner 사용자 설정, Scanner logs/diagnostics를 교체하지 않음
- atomic JSON / backup recovery를 사용하는 설정은 현재 계약 유지
- v1.4.2에는 user.db migration 없음
- v1.4.2에는 mandatory Game Content update 없음

## 12. Program Update / Release 계약

- GitHub의 public stable release를 사용자 동의형 Program Update 기준으로 사용
- 정식 release tag / Desktop Version / FIRST_RUN / ZIP 이름 / ProductVersion 일치
- Windows x64 .NET 10 self-contained single-file
- 관리자 권한 불필요
- installer 없음
- release package root는 `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets/` 구조
- public release 전에 exact-source build/test/publish/smoke
- draft asset 재다운로드 검증 후 public/latest 게시
- public asset을 다시 다운로드하여 SHA256SUMS, package layout, ProductVersion, EXE smoke 검증
- 독립 verifier가 durable status를 `docs/.release-vX.Y.Z-status.json`에 기록
- release가 검증된 뒤 one-shot controller/verifier workflow 제거

v1.4.2 exact release source는 반드시 다음 SHA입니다.

```text
a2d939b5f28e0d6de2468312bdd11467e3b35622
```

태그와 공개 ProductVersion은 controller/finalizer commit이 아니라 이 SHA를 가리킵니다.

## 13. v1.4.2 검증 요약

실제 사용자 Ground Truth:

```text
61 total cases
16 user-reviewed cases
```

제품 수정 검증:

```text
PR #160 CI: 32656154735 — SUCCESS
272 tests / 0 failed / 0 skipped
Windows build/publish: SUCCESS
Product UI + Map/Factory/MiniMap + Scanner smoke: SUCCESS
graceful shutdown: SUCCESS
```

Release 검증:

```text
release-prep CI: 32656572239 — SUCCESS
exact release source/tag: a2d939b5f28e0d6de2468312bdd11467e3b35622
release run: 32656993853 — SUCCESS
public verifier: 32657225090 — SUCCESS
asset: Junhyun-Helper-v1.4.2-win-x64.zip
bytes: 80,385,620
sha256: e6aa57ac9492ebc3438335a5e0f66e4daf18c2b87b2b61abcb141de0f0d810a8
ProductVersion: 1.4.2+a2d939b5f28e0d6de2468312bdd11467e3b35622
public/latest: VERIFIED
public redownload: VERIFIED
SHA256SUMS: VERIFIED
package layout: VERIFIED
public EXE smoke: SUCCESS
```

Release blocker는 없습니다.

## 14. 현재 열린 작업 / 다음 단계

현재 Scanner의 핵심 과제는 **v1.4.2를 실제 Tarkov에서 다시 사용해 Ground Truth를 추가 축적하는 것**입니다.

```text
v1.4.2 real usage
→ 정상 결과 대표 표본 `맞음`
→ 미인식/오인식 직후 `교정`
→ reviewed Ground Truth 축적
→ summary / OCR confusion / ROI delta / matcher candidate 분석
→ 회귀 테스트
→ 실제 실패 stage 특정
→ 그 stage만 수정
→ 전체 dataset replay
→ 기존 정상 REGRESSION=0 확인
```

특히 확인할 것:

- v1.4.2 contained-subpanel fallback이 oversized stash/inventory frame 실패를 실제로 해결하는지
- `r`, `0`, 복잡한 한글 등 OCR 혼동에서 bounded matcher recovery가 정답률을 높이면서 false positive를 만들지 않는지
- Item ID가 맞을 때 최고 상점가 / 플리 평균가 / slots / RequiredTotal mapped_data가 안정적으로 표시되는지
- 빠른 연속 사용에서 stale result isolation이 유지되는지
- 장시간 사용 시 CPU/memory/UI responsiveness

**Scanner 속도 최적화는 현재 보류**합니다. 정확도와 안정성이 충분히 고정된 뒤 capture/OCR 반복, candidate budget, visual path 비용을 측정해 별도 최적화합니다.

실제 Ground Truth 없이 threshold를 감으로 수정하지 않습니다.
