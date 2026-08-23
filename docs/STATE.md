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
- Scanner Ground Truth 교정 / 진단 dataset / full-pipeline regression

Runtime GPT/AI 의존성은 없습니다.

## 2. 현재 공개 릴리즈

현재 public stable / latest는 **v1.4.0**입니다.

```text
version: v1.4.0 PUBLIC RELEASE / VERIFIED
release source: 1b7f565adec9dfa2546fb959c813310707aabd32
public tag source: 1b7f565adec9dfa2546fb959c813310707aabd32
feature PR #149 final CI: 32643727571 — SUCCESS
release-prep PR #150 final CI: 32644579509 — SUCCESS
automated tests: 268 passed / 0 failed / 0 skipped
release run: 32644951640 — SUCCESS
independent public verifier: 32645536757 — SUCCESS
asset: Junhyun-Helper-v1.4.0-win-x64.zip
bytes: 80,374,018
SHA-256: ef3676bbc7fb07fd45f4e9291e6fd4ef8a4a686a0f584cb1ddfdb6569376645f
ProductVersion: 1.4.0+1b7f565adec9dfa2546fb959c813310707aabd32
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public SHA256SUMS: VERIFIED
public package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.4.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
Scanner Ground Truth dataset: local diagnostics persistence
v1.3.5 → v1.4.0 mandatory Game Content update: none
v1.3.5 → v1.4.0 user.db migration: none
```

공식 검증 기록:

- `docs/RELEASE_1.4.0.md`
- `docs/.release-v1.4.0-status.json`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/CURRENT_SCANNER_WORK.md`

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

## 4. Scanner 장기 제품 계약

Scanner는 게임 프로세스 내부 데이터를 읽지 않는 독립적인 화면 기반 보조 기능입니다. 범용 OCR이 아니라 Tarkov UI 전용 폐쇄형 인식 시스템으로 취급합니다.

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
- live Ground Truth 없이 global confidence/margin/header threshold를 완화하지 않음
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
- 1회 테스트: 모든 연결 display를 한 번 정밀 분석
- continuous mode를 영구 변경하지 않음
- shared recognition state와 직렬화
- scan-time catalog network refresh를 시작하지 않음

### Detail structural detection

- Scanner Lab v3.8 계열 구조
- red-X connected-component 후보
- rectangle/edge fallback 후보
- IoU deduplication
- 최대 8 candidates
- structural floor `0.34`
- structural score는 Item identity 점수가 아님
- continuous mode에서는 동일 quantized geometry가 안정화된 뒤 semantic recognition
- verified detail/title signature가 유지되면 불필요한 OCR 반복 억제

Initial structural rectangle을 최종 authoritative bounds로 간주하지 않습니다. Full header lock 후 magnifier/X 실측값에서 detail-window top/left/right를 다시 정렬하고 bottom은 structural detector 값을 유지합니다.

### Inspect header / title ROI

Runtime semantic gate:

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
```

- title glyph는 title ROI의 수평 ownership을 갖지 않음
- magnifier 후보는 fixed frame-left lane에서만 생성
- ring bright band + hollow center + lower-right handle + outside background template 사용
- close/X는 red body/edge + expected geometry + diagonal X contrast template 결합
- diagnostic-only structural candidate는 관찰/저장할 수 있으나 OCR identity path에 들어가지 않음

상세 근거:

- `docs/SCANNER_V1.3.4_LIVE_HARDENING.md`
- `docs/DECISION_SCANNER_V1.3.4_LIVE_HARDENING_2026-08-23.md`
- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`
- `docs/.scanner-v1.3.3-header-evidence.json`

## 6. OCR / matcher / visual corroboration

Primary recognizer는 Windows `ko-KR` OCR입니다.

- title size에 따라 4x/6x/8x 확대
- first pass 실패 시 deep OCR/high-contrast/binary/inverse variants
- raw OCR과 matcher input 분리 보존
- current catalog 기반 character/symbol policy
- Korean-title contract에서 CJK Han ideograph hard reject
- arbitrary replacement로 confidence를 인위적으로 올리지 않음
- official catalog exact-first + conservative fuzzy + margin
- current-catalog unique one-unknown-glyph recovery
- bounded unique one-edit recovery
- ambiguous/low-confidence는 Item ID 미확정

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

## 7. v1.4.0 Ground Truth / correction / regression

공식 계약은 `docs/SCANNER_GROUND_TRUTH.md`입니다.

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

JSON/좌표/파일명 관리는 요구하지 않습니다.

### Dataset 관리 / export

- Case 수와 총 용량 표시
- Case 목록 확인
- 선택 Case 삭제
- 전체 dataset 삭제
- 일반 Scanner 로그는 별도 삭제
- `ScannerDiagnostics_YYYY-MM-DD.zip` export

### 자동 통계

- reviewed final accuracy
- Ground Truth 오류 유형
- observed pipeline stage
- detail ROI delta 평균/표준편차
- item-name ROI delta 평균/표준편차
- OCR observed → Ground Truth 문자 치환/삽입/누락 통계

### Full-pipeline regression

Reviewed Ground Truth의 `full.png`를 현재 production 경로로 다시 실행합니다.

```text
full.png
→ detail geometry
→ inspect-header lock
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

## 8. Scanner 표시 데이터 의미

현재 게임 화면 OCR 필드는 **`item_name` 하나**입니다.

다음은 화면 숫자 OCR이 아니라 Item ID 확정 이후 로컬 데이터에서 계산/조회하는 `mapped_data`입니다.

- highest trader sell price = 유효한 non-flea RUB 환산 판매가 최댓값
- flea average = positive `avg24hPrice`
- slots = positive `width × height`
- price/slot = valid price와 slots가 모두 존재할 때만
- required count = `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal`

Inventory 차감 부족량은 Scanner의 `필요 개수` 의미가 아닙니다. market/dimension 누락은 해당 표시 필드만 fail closed하고 Item identity health와 분리합니다.

## 9. Scanner 사용자 분석 / 단축키

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

## 10. Mini Scanner

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

## 11. Persistence / 사용자 데이터

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/map-product-settings.json(.bak)
%LocalAppData%/JunhyunHelper/ammo-favorites.json(.bak)
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/
%LocalAppData%/JunhyunHelper/scanner/fonts/
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

Program package와 사용자 데이터는 분리됩니다. 프로그램 업데이트는 Ground Truth dataset과 기존 사용자 데이터를 교체하지 않습니다.

## 12. Map / MiniMap

Map/MiniMap은 pinned donor source를 제한적으로 compile-link한 독립 subsystem입니다.

- general marker/artwork/config → pinned Map bundle
- current Quest state/geometry → JunhyunHelper bridge
- donor updater/content DB/global hidden command/legacy logger는 product ownership에서 제외
- 구체적 defect/performance evidence 없이 broad refactor하지 않음

## 13. Program Update / 배포 계약

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

## 14. v1.4.0 공개 검증 결과

완료:

- PR #149 Ground Truth/correction/regression final CI `32643727571`: SUCCESS
- PR #150 v1.4.0 release-prep final CI `32644579509`: SUCCESS
- automated tests: **268 passed / 0 failed / 0 skipped**
- exact-source release run `32644951640`: SUCCESS
- independent public verifier `32645536757`: SUCCESS
- exact release source/tag: `1b7f565adec9dfa2546fb959c813310707aabd32`
- public/latest: VERIFIED
- public ZIP re-download/hash/SHA256SUMS/layout: VERIFIED
- ProductVersion/FIRST_RUN: VERIFIED
- public-downloaded Product UI + Map/Factory/MiniMap smoke: SUCCESS

공개 asset:

```text
Junhyun-Helper-v1.4.0-win-x64.zip
80,374,018 bytes
SHA-256 ef3676bbc7fb07fd45f4e9291e6fd4ef8a4a686a0f584cb1ddfdb6569376645f
ProductVersion 1.4.0+1b7f565adec9dfa2546fb959c813310707aabd32
```

기계 판독 증거: `docs/.release-v1.4.0-status.json`.

## 15. 현재 알려진 열린 영역

제품 기능 결함으로 확정된 release blocker는 없습니다.

Scanner는 이제 **실제 Tarkov Ground Truth를 축적하면서 정확도를 개선하는 단계**입니다. 모든 해상도/DPI/UI 위치/아이템명 조합을 실게임에서 검증한 것은 아닙니다.

현재 의도적으로 남긴 기술 부채/개선 후보:

- OCR 전처리 evidence는 현재 production 규칙을 저장 계층에서 재현하며, OCR engine이 실제 소비한 bitmap을 직접 발행하는 구조는 아직 아님
- 충분한 실사용 Ground Truth가 생기기 전에는 detector/header/OCR/matcher threshold를 임의 튜닝하지 않음
- real Tarkov rendered sample dictionary는 수집된 Ground Truth를 기반으로 점진적으로 확장

새 evidence가 발생하면 다음 순서로 실패 단계를 분리합니다.

```text
capture
→ detail structural detection
→ close template / header frame / fixed magnifier lane-template
→ locked detail bounds / title ROI
→ OCR / preprocessing
→ catalog sanitation / semantic matcher
→ Tarkov-font visual corroboration
→ Item ID
→ mapped presentation
→ Mini Scanner / stale-state handling
```

## 16. 다음 작업

1. v1.4.0을 실제 Tarkov Borderless 환경에서 다양한 아이템/위치/해상도/DPI로 사용
2. 정상 결과는 필요한 표본을 `맞음`으로 검증
3. 미인식/오인식은 문제 직후 `교정`에서 실제 상세창/아이템명 ROI와 정답 텍스트를 기록
4. 충분한 reviewed Ground Truth 축적 후 `summary`와 OCR confusion/ROI delta 분석
5. `회귀 테스트`로 현재 기준선 고정
6. detail/header/ROI/OCR/matcher 중 실제 실패 단계만 수정
7. 전체 dataset replay 후 기존 정상 `REGRESSION=0` 확인
8. Item ID가 맞는 Case에서 trader/flea/slots/RequiredTotal mapped data도 함께 검증
9. 장시간 CPU/memory/UI responsiveness와 빠른 연속 scan stale isolation을 실사용에서 확인

상세 작업 목록: `docs/NEXT.md`.
