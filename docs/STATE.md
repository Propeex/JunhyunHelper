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

현재 public stable / latest는 **v1.4.3**입니다.

```text
version: v1.4.3 PUBLIC RELEASE / VERIFIED
release source: f7e3870c81a7d7be025f1fe56d5b7f607546b250
public tag source: f7e3870c81a7d7be025f1fe56d5b7f607546b250
Scanner feature PR #165 CI: 32660568132 — SUCCESS
release-prep PR #166 CI: 32674399495 — SUCCESS
automated tests: 279 passed / 0 failed / 0 skipped
release run: 32674812862 — SUCCESS
independent public verifier: 32675069359 — SUCCESS
asset: Junhyun-Helper-v1.4.3-win-x64.zip
bytes: 80,389,336
SHA-256: fa5da9f2a6b9ea62f8a9a2ddfb1062bed81609fb96516a01089238b92067a8be
ProductVersion: 1.4.3+f7e3870c81a7d7be025f1fe56d5b7f607546b250
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public SHA256SUMS: VERIFIED
public package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

현재 schema / compatibility:

```text
Desktop Version: 1.4.3
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
Scanner Ground Truth dataset: local diagnostics persistence
v1.4.2 → v1.4.3 mandatory Game Content update: none
v1.4.2 → v1.4.3 user.db migration: none
```

공식 검증 기록:

- `docs/RELEASE_1.4.3.md`
- `docs/.release-v1.4.3-status.json`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/DECISION_SCANNER_SEMANTIC_CANDIDATE_AND_OCR_ALPHABET_2026-08-24.md`

완료된 v1.4.3 one-shot release/verifier workflow는 제거하고 정상 CI는 `.github/workflows/ci.yml` 하나만 유지합니다.

## 3. 아키텍처 기준

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

책임:

- **Core**: canonical domain, deterministic calculation, Scanner structural/identity/matcher 규칙
- **Application**: 사용자 use case, authoritative mutation, workspace orchestration
- **Infrastructure**: HTTP/source parsing, SQLite/file persistence, content/scanner/update I/O
- **Desktop**: WPF UI, presentation, Scanner capture/OCR/runtime/diagnostics, Map bridge
- **Map/MiniMap donor**: 제한적 compile-link 예외. donor updater/content ownership/hidden command는 사용하지 않음

현재 pinned Map donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

기존 `Propeex/Tarkov-Helper`는 요구사항의 권위가 아니다. 유지할 기존 기능, 검증된 데이터/자산, 구현 아이디어, 시행착오 참고 용도로만 사용한다.

## 4. Scanner 제품 계약

Scanner는 게임 프로세스 내부 데이터를 읽지 않는 독립적인 화면 기반 보조 기능입니다. 범용 OCR이 아니라 Tarkov UI 전용 closed-domain recognizer로 취급합니다.

Production pipeline:

```text
capture
→ detail rectangle proposals
→ inspect-header / field localization
→ ROI extraction
→ OCR / visual recognition
→ current Tarkov catalog validation
→ final Item ID decision
→ mapped presentation
→ user correction / Ground Truth
→ dataset / export / regression
```

단계를 분리합니다.

- Detail Window Detection / Proposal
- Field Localization / Header Lock
- OCR Recognition
- Candidate Matching
- Parsing
- Data Mapping
- Final Decision

불변 원칙:

- false positive보다 miss를 선호
- geometry / structural score / anchor score / icon 하나만으로 Item identity 확정 금지
- current official Korean item catalog가 identity authority
- 실제 Ground Truth 없이 global confidence/margin/header threshold 완화 금지
- ambiguity / low confidence / incomplete header lock은 fail closed
- current-catalog 밖 glyph를 특정 문자로 임의 치환 금지
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지

## 5. Capture / detail / header 기준

### Capture

TarkovWindow:

```text
EscapeFromTarkov process/window
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ PrintWindow 우선
→ invalid/empty이면 exact client screen rectangle fallback
```

DisplayTest는 연결된 전체 display에 같은 detector/OCR/catalog/presentation pipeline을 적용합니다. Real/test continuous mode는 상호 배타적입니다.

One-shot:

- 1회 인게임: TarkovWindow 한 번 정밀 분석
- 1회 테스트: 연결 display 한 번 정밀 분석
- continuous mode를 영구 변경하지 않음
- shared recognition state와 직렬화
- scan-time catalog network refresh 시작하지 않음

### v1.4.3 detail rectangle proposal policy

Scanner Lab 3.8 계열 geometry의 책임은 **rectangle proposal 생성**입니다. 상세창 확정 권한은 없습니다.

- red-X connected-component proposal
- rectangle/edge fallback proposal
- structural floor `0.34`
- one-shot 최대 12 candidates
- continuous 최대 8 candidates
- historical `aspect ≈ 1.3`은 약한 ordering hint
- tall/large detail window는 aspect prior만으로 제거하지 않음
- high IoU 자체는 dedupe 조건이 아님
- top/bottom/left/right가 실질적으로 다르면 겹쳐도 semantic stage까지 보존
- 사실상 동일한 edge-jitter proposal만 near-duplicate 제거
- rough red-X proximity는 ranking hint이며 실제 close-X proof가 아님
- structural score는 Item identity score가 아님

Initial structural rectangle은 최종 authoritative bounds가 아닙니다. Full semantic header lock 뒤 magnifier/X 실측값으로 top/left/right를 다시 정렬하며 bottom은 구조 evidence를 사용합니다.

### Inspect-header / title semantic gate

Runtime OCR 진입 조건:

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
AND Magnifier evidence present
AND Close-X evidence present
```

Evidence:

- close: red body/edge + expected geometry + diagonal X contrast
- magnifier: frame-left search lane + ring/hollow center/handle/background
- neutral header/frame
- dark title field
- title text evidence

Diagnostic-only structural candidate는 관찰/저장할 수 있으나 semantic gate 전에는 OCR identity path에 들어가지 않습니다.

Fallback 순서:

```text
ScannerInspectHeaderLock
→ 실패 시 v1.4.1 live Ground Truth refiner
→ 둘 다 실패한 oversized candidate에서 v1.4.2 contained-subpanel proposals
→ 같은 close/magnifier/title/header evidence 재검증
→ HEADER_FRAME_LOCKED >= 0.68
```

v1.4.3은 이 trusted gate를 낮추지 않았습니다.

## 6. OCR / matcher / visual corroboration

Primary recognizer는 Windows `ko-KR` OCR입니다.

- title size 기반 확대
- first pass 실패 시 deep OCR / high-contrast / binary / inverse variants
- raw OCR과 normalized/matcher input을 분리 보존
- official catalog exact-first + conservative fuzzy + margin
- Tarkov-font visual corroboration/recovery
- ambiguous/low-confidence는 Item ID 미확정

### v1.4.2 reviewed-GT bounded recovery

Ordinary matcher가 실패한 경우에만:

- current catalog 전체에서 유일하고 충분히 분리된 2-edit candidate
- 충분히 긴 suffix가 일치하고 catalog 전체에서 유일한 2~3-edit candidate

를 제한적으로 복구합니다.

### v1.4.3 current-catalog alphabet / unknown glyph

- current official item names에서 실제 사용되는 letter/digit/symbol inventory 생성
- ordinary ASCII letters/digits는 fuzzy noisy evidence로 유지
- 공식 이름에 실제 사용되는 quotes/hyphens/brackets 등은 보존
- `Ø` 같은 catalog-impossible Unicode letter/symbol은 정상 identity 문자로 신뢰하지 않음
- impossible embedded glyph는 특정 `r`, `0`, `I`, `l` 등으로 전역 치환하지 않고 `?` evidence로 보존
- 1~2 unknown glyph pattern은 complete current catalog에서 유일하고 충분한 global separation이 있을 때만 복구
- ambiguous wildcard는 fail closed

따라서 `r`, `0`, complex Hangul OCR 자체가 일반적으로 해결되었다고 간주하지 않습니다. 불가능한 glyph filtering과 closed-domain bounded recovery를 강화한 것입니다.

변경하지 않은 것:

- generic confidence threshold
- top1/top2 margin 일반 완화
- global glyph substitution table
- current catalog 밖 Item 생성

### Tarkov-font visual path

```text
Tarkov resources.assets (read-only)
→ bounded SFNT discovery
→ %LocalAppData%/JunhyunHelper/scanner/fonts
→ source manifest + generation key
→ Bender regular/bold + Korean fallback
→ current official item-name rendered templates/features
```

Visual path가 unavailable/error/ambiguous이면 healthy OCR evidence를 임의 폐기하지 않습니다.

## 7. Ground Truth / correction / regression

공식 계약: `docs/SCANNER_GROUND_TRUTH.md`

저장 root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

대표 evidence:

- `full.png`
- `detail_window.png`
- `detected_roi.png`
- `corrected_roi.png`
- `processed_roi.png`
- `annotated.png`
- `case.json`
- raw OCR / normalized / matcher text
- Item ID / official name
- confidence / second score / margin
- matcher top candidates
- structural/header evidence
- pipeline stage
- user Ground Truth
- mapped presentation

사용자 교정 UX:

- `맞음`
- 상세보기 영역 수정
- 아이템명 영역 수정
- 정답 아이템명 입력
- 영역 + 텍스트 동시 교정

사용자 rectangle과 정답 text가 Ground Truth입니다. 자동 diagnostic Case는 Ground Truth로 취급하지 않습니다.

Dataset:

- Case 수 / 용량
- Case 목록 / 선택 삭제 / 전체 삭제
- Scanner 일반 로그 별도 삭제
- `ScannerDiagnostics_YYYY-MM-DD.zip` export
- summary / environment / cases / images / logs 포함

Full-pipeline regression:

```text
full.png
→ current detail proposals
→ inspect-header / contained-subpanel semantic lock
→ title ROI
→ current OCR/deep OCR/font recovery
→ current catalog alphabet/matching
→ final Item ID
```

결과는 `STILL_CORRECT`, `SOLVED`, `STILL_FAILING`, `REGRESSION`, `ERROR`로 분류합니다. 과거 정상 Case가 현재 실패하면 평균 정확도가 상승해도 regression입니다.

## 8. Scanner 표시 데이터 의미

Production Scanner OCR field는 **`item_name` 하나**입니다.

아래는 Item ID 확정 뒤 로컬 데이터에서 계산/조회하는 `mapped_data`입니다.

- highest trader sell price: flea 제외 유효 판매처 RUB 환산 가격 최댓값
- flea average: positive `avg24hPrice`
- slots: positive `width × height`
- price/slot: price와 slots가 둘 다 유효할 때만
- required count: `NeededItems[itemId].RequiredTotal`

Inventory 차감 부족량은 Scanner의 `필요 개수` 의미가 아닙니다. market/dimension 누락은 해당 표시 필드만 fail closed하고 Item identity health와 분리합니다.

## 9. Scanner UI / 단축키

Scanner display settings schema: **v4**

기본 global hotkey:

- 1회 인게임: `Ctrl+Shift+F10`
- 1회 테스트: `Ctrl+Shift+F11`
- Scanner ON/OFF: `Ctrl+Shift+F12`

MainWindow lifetime 동안 Scanner 탭 밖에서도 동작합니다. Gesture 중복은 허용하지 않습니다.

Scanner 탭 진단/관리:

- 인식 이미지
- 교정
- 회귀 테스트
- 교정 데이터 내보내기
- 교정 데이터 관리
- 로그 삭제

일반 Scanner 로그 삭제와 Ground Truth dataset 삭제는 독립 동작입니다.

## 10. Mini Scanner

- match 성공 Item 정보만 표시
- runtime/OCR/error/status text는 overlay에 표시하지 않음
- WPF Topmost + native HWND_TOPMOST
- ShowActivated=false / no-activate
- 전체 카드 drag hit surface
- 실제 Scanner mode에서는 Tarkov foreground + inventory/stash context를 보수적으로 확인
- inventory/stash OCR probe 최대 1개
- 반복 요청 latest coalesce
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

- portable executable 옆에 mutable user data/log 생성 금지
- Program Update가 user.db, content/image cache, Map/Ammo/Scanner settings, Scanner logs/diagnostics를 교체하지 않음
- v1.4.3 user.db migration 없음
- v1.4.3 mandatory Game Content update 없음

## 12. Program Update / Release 계약

- GitHub public stable release를 사용자 동의형 Program Update 기준으로 사용
- release tag / Desktop Version / FIRST_RUN / ZIP / ProductVersion identity 일치
- Windows x64 .NET 10 self-contained single-file
- installer 없음 / 관리자 권한 불필요
- package root는 `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets/`
- exact-source build/test/publish/smoke
- draft asset re-download 검증 후 public/latest 게시
- public asset re-download로 SHA256SUMS, layout, ProductVersion, EXE smoke 검증
- independent verifier가 durable `docs/.release-vX.Y.Z-status.json` 기록
- 완료된 one-shot controller/verifier workflow 제거

v1.4.3 exact source/tag:

```text
f7e3870c81a7d7be025f1fe56d5b7f607546b250
```

태그와 공개 ProductVersion은 release controller/finalizer commit이 아니라 위 exact source를 가리킵니다.

## 13. v1.4.3 검증 요약

```text
feature PR #165 CI: 32660568132 — SUCCESS
release-prep PR #166 CI: 32674399495 — SUCCESS
279 tests / 0 failed / 0 skipped
exact release source/tag: f7e3870c81a7d7be025f1fe56d5b7f607546b250
release run: 32674812862 — SUCCESS
public verifier: 32675069359 — SUCCESS
asset: Junhyun-Helper-v1.4.3-win-x64.zip
bytes: 80,389,336
sha256: fa5da9f2a6b9ea62f8a9a2ddfb1062bed81609fb96516a01089238b92067a8be
ProductVersion: 1.4.3+f7e3870c81a7d7be025f1fe56d5b7f607546b250
public/latest: VERIFIED
public redownload: VERIFIED
SHA256SUMS: VERIFIED
package layout: VERIFIED
public EXE smoke: SUCCESS
```

Release blocker는 없습니다.

## 14. 현재 열린 작업 / 다음 단계

현재 Scanner 핵심 과제는 **v1.4.3 실제 Tarkov Ground Truth를 추가 축적하는 것**입니다.

```text
v1.4.3 real usage
→ 정상 결과 대표 표본 `맞음`
→ 미인식/오인식 직후 `교정`
→ reviewed Ground Truth 축적
→ diagnostics ZIP export
→ summary / OCR confusion / ROI delta / matcher candidate 분석
→ 실제 실패 stage 특정
→ 그 stage만 수정
→ 전체 reviewed dataset replay
→ 기존 정상 REGRESSION=0 확인
```

특히 확인할 것:

- tall/large detail window
- stash/inventory frame과 high-IoU로 겹치는 실제 detail rectangle
- `r`, `0`, slash-zero-like Unicode glyph 및 복잡한 한글
- 정상 punctuation item names
- near-name ambiguity에서 false positive 여부
- Item ID 성공 시 trader/flea/slots/RequiredTotal mapped_data
- 빠른 연속 사용 stale-result isolation
- 장시간 CPU/memory/UI responsiveness

**Scanner 속도 최적화는 현재 의도적으로 보류**합니다. 정확도와 안정성이 더 고정된 뒤 capture/OCR 반복, candidate budget, semantic validation, visual recovery 비용을 실제 측정해 별도 최적화합니다.

## 15. 알려진 잔여 과제

- 일부 historical case에서 semantic header/title이 복구되어도 structural bottom 보존 때문에 detail bottom이 실제보다 낮게 남을 수 있음
- diagnostic `TITLE_ANCHOR_INCOMPLETE` stage classification 오류 가능성
- 추가 해상도/DPI/UI 배치 live validation 필요
- `r`, `0`, complex Hangul OCR engine 자체는 일반적으로 해결되지 않음
- exact OCR-consumed processed bitmap 보존 구조에 개선 여지 있음
- rendered sample dictionary는 reviewed Ground Truth가 충분히 쌓인 뒤 확장
- 추가 Ground Truth 없이 generic matcher/header threshold를 완화하지 않음
