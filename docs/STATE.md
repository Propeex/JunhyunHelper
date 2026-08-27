# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-27  
상태: **v1.7.12 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다.

현재 요구사항 범위의 제품은 완성 상태이며 기본 운영 모드는 **유지보수**다. 새 기능은 사용자가 새로운 제품 요구사항으로 명시적으로 결정할 때만 시작한다.

주요 기능:

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
- Scanner Ground Truth 교정 / diagnostic dataset / regression

Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper`는 불완전한 프로토타입이며 새 제품 요구사항의 권위가 아니다. 검증된 Map/MiniMap donor source와 유용한 자산/아이디어만 제한적으로 사용한다.

## 2. 현재 public stable

```text
version: v1.7.12
exact product release source/tag target: d8d0f8eb1ffdd9b8c4ec890277a7b209b2458c2b
main CI run: 33042307773 — SUCCESS
release workflow run: 33042464642 — SUCCESS
release id: 377581895
asset: Junhyun-Helper.zip
asset id: 531791229
asset bytes: 80,477,641
asset SHA-256: 3f0d57f8a5dc92611bc8648a423c43d65917e63e0d73a771b559153803186fa1
checksum asset: SHA256SUMS.txt
checksum asset id: 531791226
checksum asset bytes: 86
checksum asset SHA-256: 97cf0d26c1d6c91c5876ee02f829225a23221e2bc893659d211055aa6af6a99d
397 passed / 0 failed / 0 skipped
published UTC: 2026-08-27T05:26:26Z
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.7.12`
- release target = exact product release source
- tag ref object = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- ZIP + checksum assets present
- public `Junhyun-Helper.zip` digest = exact main-CI package SHA-256

상세 공개 증거:

- `docs/RELEASE_1.7.12.md`
- `docs/.release-v1.7.12-status.json`
- `docs/RELEASE_NOTES_V1.7.12.md`

이후 documentation-only commit은 v1.7.12 product release source가 아니다. 제품 release source/tag target은 위 `d8d0f8eb...`로 고정한다. 이미 공개된 v1.7.12 tag/source/assets는 immutable historical product release로 취급한다.

## 3. 아키텍처

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

책임:

- **Core**: canonical domain, deterministic calculation, Quest 규칙, Scanner structural/normalization/matcher/presentation 정책
- **Application**: 사용자 use case, authoritative mutation, workspace orchestration
- **Infrastructure**: HTTP/source parsing, SQLite/file persistence, content/update I/O
- **Desktop**: WPF UI, presentation, Scanner capture/OCR/runtime/diagnostics, Map bridge
- **Map/MiniMap donor**: 제한적 compile-link 예외. donor updater/content ownership은 사용하지 않음

Desktop shared presentation infrastructure는 `MainWindow.OnInitialized`가 product-window composition owner다. 개별 Page의 internal presentation initialization은 해당 Page가 직접 소유하며 unrelated Page의 `Loaded` 순서에 의존하지 않는다.

## 4. Schema / 사용자 데이터

```text
Desktop target version: 1.7.12
Current public stable executable: 1.7.12
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
```

대표 저장 위치:

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/map-product-settings.json(.bak)
%LocalAppData%/JunhyunHelper/minimap-window-state.json
%LocalAppData%/JunhyunHelper/ammo-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
%LocalAppData%/JunhyunHelper/logs/
```

원칙:

- portable executable 옆에 mutable user data/log 생성 금지
- Program Update가 user.db, content/image cache, Map/MiniMap/Ammo/Scanner settings, Scanner logs/diagnostics/Ground Truth를 교체하지 않음
- user-reviewed Scanner Ground Truth는 자동 삭제하지 않음
- Scanner logs와 Ground Truth dataset lifetime 분리
- 정상 Scanner monitoring은 durable automatic correction Case를 생성하지 않음

## 5. Game Content / Scanner catalog

일반 Game Content와 Scanner catalog는 사용자에게 별도 관리 절차를 요구하지 않는다.

```text
remote Game Content
→ validate/build candidate
→ integrity validation
→ general content activation
→ Scanner catalog refresh
→ local last-known-good preservation on partial failure
```

Scanner scan 순간에는 local/memory catalog만 사용하며 identity 결정을 위해 network 요청을 시작하지 않는다.

공식 Korean Tarkov full-item catalog가 Scanner Item identity authority다. market/dimension coverage와 Item identity health는 분리한다.

## 6. Scanner — 현재 제품 계약

Scanner 상태: **FEATURE COMPLETE / MAINTENANCE ONLY**.

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ close-X / magnifier / inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user OCR substitution
→ conditional cross-environment title normalization
→ current-catalog sanitation / normalization
→ conservative catalog matching / bounded recovery
→ optional current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional correction / Ground Truth
```

Scanner는 범용 OCR이 아니라 closed-domain recognizer다. false positive는 miss보다 나쁘다.

핵심 불변식:

```text
structural floor = 0.34
trusted HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- geometry/environment normalization은 Item identity proof가 아님
- stale/cross-frame OCR/visual result를 current identity proof로 사용하지 않음
- Item ID 확정 전 price/needed/slot metadata를 identity evidence로 사용하지 않음
- current official catalog 밖 임의 Item 생성 금지
- reviewed evidence 없이 recognition threshold/candidate cap/matcher/visual acceptance 완화 금지

## 7. v1.7.12 — long-term maintenance hardening

v1.7.12는 사용자-visible 기능 추가 없이 Desktop lifecycle ownership과 장기 유지보수성을 개선한 patch다.

### Desktop page infrastructure ownership

`MainWindow.OnInitialized`가 다음 shared presentation infrastructure를 product-window lifetime에 한 번 연결한다.

```text
Quest / Hideout / Items / Ammo image cache
Ammo favorite store
cross-page content navigation wiring
Scanner global-command lifetime wiring
```

개별 MainWindow Page의 `Loaded` 순서가 unrelated Page의 준비 상태를 결정하지 않는다.

### Ammo presentation lifecycle

초기 dead-code audit에서 MainWindow의 page Loaded handlers를 제거한 뒤 actual published EXE smoke가 Ammo detail collapse/expand 초기화 회귀를 탐지했다.

원인은 제거한 `AmmoPage_Loaded` 본문의 기능이 아니라 Ammo class-level `Loaded` handler가 부모의 Loaded subscription 존재 여부에 간접 의존하던 hidden WPF lifecycle coupling이었다.

수정:

```text
AmmoPage.OnInitialized
→ DispatcherPriority.Loaded
→ Ammo search/detail presentation initialization
→ Ammo grid presentation initialization
```

부모 handler를 복구하는 대신 Ammo가 자기 presentation initialization을 직접 소유한다.

### Audit 판단

- 제거된 MainWindow page Loaded handlers: ownership 이동과 Ammo self-owned initialization 이후 actual dead
- `Legacy` Map/MiniMap bridge: active compatibility/integration, 유지
- Factory/Map/MiniMap smoke: active regression evidence, 유지
- Scanner diagnostic OCR reflection adapter: 의도적 technical debt, 유지
- original full-refresh mutation handlers + fast rebinding: lifecycle 관여 증거가 있어 삭제 보류
- Quest/Hideout/Items workspace의 반복 profile `LoadAsync`: `UserProfileStore` immutable in-process snapshot cache 때문에 현재 evidence만으로 DB 병목으로 판정하지 않음
- one-read/multi-build, 추가 cache, 병렬화: 실제 runtime trace가 병목을 증명할 때까지 보류

공식 결정:

- `docs/DECISION_LONG_TERM_MAINTENANCE_AUDIT_2026-08-27.md`
- `docs/DECISION_V1.7.12_MAINTENANCE.md`

## 8. v1.7.11 — maintenance polish

v1.7.11은 Scanner identity recognition이 아니라 표시·입력·MiniMap 사용성을 수정한 patch다.

### Scanner 필요 개수

Item ID가 확정된 뒤 Scanner / Mini Scanner의 `필요 개수`는 다음 canonical 값을 사용한다.

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
```

- `RequiredTotal`은 전체 요구량이며 사용자 표시값이 아님
- 현재 Inventory와 FIR 조건을 반영한 Needed Items 계산 결과를 그대로 사용
- Scanner가 Quest/Hideout/Inventory 계산을 별도 재구현하지 않음
- Item ID 확정 전에는 identity evidence로 사용하지 않음

### configurable hotkey modifier matching

Map / Scanner configurable hotkey의 현재 계약:

- primary key 일치 필수
- 등록된 Ctrl/Alt/Shift는 모두 눌려 있어야 함
- 등록하지 않은 Ctrl/Alt/Shift 추가 입력은 허용
- 같은 primary key에서 여러 binding이 compatible하면 required modifier 수가 더 많은 binding 우선
- 동률은 기존 기능 우선순위/안정적인 등록 순서 사용
- Windows modifier 미지원
- Map bare NumPad0~5 direct floor 유지

### MiniMap first-open / size persistence

- MiniMap 첫 표시 전에 현재 Main Map UI 선택을 shared `MapTrackerService`로 동기화
- 이전 tracker key가 첫 frame의 잘못된 지도 source가 되지 않도록 함
- MiniMap width/height는 `%LocalAppData%/JunhyunHelper/minimap-window-state.json`에 저장
- 재시작 뒤 복원하고 donor min/max 범위로 clamp

### standard ToolTip

- 설명용 standard WPF ToolTip은 제품 전역에서 표시하지 않음
- 지도 marker detail 등 기능 자체인 custom Popup/information surface는 유지

공식 결정: `docs/DECISION_V1.7.11_MAINTENANCE.md`.

## 9. Scanner v1.7.10 — cross-environment normalization

사용자가 확정한 제품 방향은 특정 PC별 보정이 아니라 공개 배포 범용성이다.

v1.7.10 runtime:

```text
normal OCR
→ text 있음: 기존 결과 즉시 사용
→ text 없음: title luminance profile 분석
    → reference/flat: 기존 경로 유지
    → lifted/washed/low-contrast: adaptive grayscale normalized auxiliary OCR
→ existing bounded deep OCR
    → environment abnormal일 때만 normalized auxiliary evidence 추가
→ existing conservative catalog matching
→ Item ID or fail closed
```

정규화 정책:

- P60로 dark title-field background 추정
- P99.75로 sparse bright glyph foreground 추정
- effectively flat/no-contrast input은 normalization 금지
- 정상 normal OCR 성공 시 histogram/copy/추가 OCR 비용을 만들지 않음
- normalization은 identity proof가 아니며 기존 catalog matcher를 그대로 통과해야 함

Deterministic procedural regression:

- reference SDR-like luminance
- lifted/washed HDR→SDR-like luminance
- compressed contrast
- low-contrast gamma/rendering variation
- 1080p/1440p/4K proportional title raster
- flat/no-contrast negative input

공식 결정: `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`.

## 10. Scanner v1.7.9 — Mini Scanner presentation authority

v1.7.9는 recognition success 뒤 Mini Scanner가 별도 inventory-header OCR 실패로 표시를 veto하던 presentation 회귀를 제거했다.

현재 권위:

```text
Scanner semantic success
→ Item ID confirmed
→ presentation snapshot
→ Mini Scanner
```

- preview/display-test는 표시 가능
- 이미 표시 중이면 confirmed Item result로 즉시 갱신
- 숨겨진 real Scanner의 최초 표시만 Tarkov foreground를 요구
- auxiliary inventory-header OCR은 confirmed Item presentation을 veto하지 못함

Sticky presentation은 성공 시 miss budget을 reset하고 실제 miss 3회째에 숨긴다.

## 11. Scanner v1.7.8 — raid inspect-header ownership

사용자 reviewed 8 Case에서 실패 6건은 OCR 오인식이 아니라 OCR 이전 `HEADER_CLOSE_NOT_LOCKED` / `TITLE_ANCHOR_INCOMPLETE`였다.

레이드 인벤토리 수평선이 inspect header와 이어져 header-left ownership이 실제 상세창보다 47~132px 왼쪽으로 확장됐다.

Recovery order:

```text
primary header lock
→ live Ground Truth recovery
→ v1.7.8 raid ownership recovery
→ contained-subpanel recovery
→ fail closed
```

raid recovery는 `RED_X_CANDIDATE >= 0.90`에서만 진입하고 기존 close-X, magnifier, neutral header, dark title field, text evidence와 최종 `HEADER_FRAME_LOCKED >= 0.68`을 모두 요구한다.

## 12. Scanner v1.7.7 — data/log/hotkey contract

당시 확립한 저장/교정 계약은 현재도 유지한다.

- normal monitoring은 durable automatic Case를 만들지 않음
- latest exact frame은 current correction용으로 메모리에만 유지
- 사용자 명시적 correction save만 reviewed durable Ground Truth
- legacy `automatic_sample + unreviewed`만 5분 recent-write safety 및 pre-delete state 재확인 후 cleanup
- reviewed/manual/corrupt/unknown/state-changed Case는 preserve fail closed
- 동일 activity failure는 30초 collapse
- primary key + optional Ctrl/Alt/Shift 구성, bare key 허용, Windows modifier 미지원
- Map bare NumPad0~5 direct floor 유지

modifier matching의 현재 동작은 v1.7.11 계약이 우선한다.

## 13. Scanner v1.7.6 — performance baseline

문제 PC actual Tarkov `ReadingTitle → ShowingItem` 성공 12건:

```text
minimum: 38.07 ms
median: 63.92 ms
maximum: 1.05 s
mean: 211.47 ms
```

Display Test:

```text
하프 마스크: 10,840.877 ms → 70.603 ms
USB 보안 플래시 드라이브: 12,686.278 ms → 1,354.775 ms
```

root cause는 Windows OCR 자체가 아니라 같은 cycle의 exact current-pixel visual evidence 반복 계산이었다. 재사용은 동일 cycle/exact pixels에만 한정하며 cross-frame identity cache가 아니다.

## 14. Scanner UI / hotkeys

일반 Scanner 상단:

```text
스캐너 ON/OFF
설정
고급
현재 결과 교정
```

`현재 결과 교정`은 최신 exact in-memory Scanner frame만 교정 창으로 연다.

`고급`:

- Display Test / 테스트 스캐너
- 교정 데이터 관리
- Scanner 성능 진단 자료 내보내기

기본 one-shot hotkeys:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

Configurable hotkey는 v1.7.11 modifier compatibility/specificity 계약을 따른다.

## 15. Ground Truth

Ground Truth는 **사용자가 직접 검토/교정하고 명시적으로 저장한 Case**만 의미한다.

- normal monitoring의 자동 결과 ≠ Ground Truth
- private user pixel evidence를 CI에 넣기 위해 공개 저장소에 commit하지 않음
- reviewed dataset이 runnable한 recognition 변경은 `REGRESSION=0`을 요구
- procedural/synthetic matrix는 reviewed Ground Truth를 대체하지 않으며 환경 robustness regression용이다

## 16. CI / release contract

Release candidate gate:

```text
Release build
→ automated tests
→ Windows x64 self-contained single-file publish
→ startup / rendered Product UI / Scanner / Map / Factory / MiniMap smoke
→ graceful shutdown / clean portable root
→ release package + SHA256 verification
→ artifact upload
```

Stable release는 **main push CI가 성공한 exact main commit**의 artifact만 Release workflow가 게시한다.

v1.7.12 proof:

```text
PR #197 final head: 23e1784c25954f4900a57cfa4c1c9821d5d6d668
final PR CI: 33042136686 — SUCCESS
main release source: d8d0f8eb1ffdd9b8c4ec890277a7b209b2458c2b
main CI: 33042307773 — SUCCESS
Release workflow: 33042464642 — SUCCESS
397 tests passed
```

Public latest/tag-ref readback에서 target/source, stable flags, required assets를 확인했고 공개 `Junhyun-Helper.zip` digest가 main-CI package SHA-256과 일치한다. 이 세션에서는 binary asset 자체를 별도 anonymous client로 재다운로드하지 않았으므로 수행하지 않은 byte-level anonymous redownload를 완료했다고 기록하지 않는다.

## 17. 유지보수 원칙

새 문제는 다음 순서로 처리한다.

```text
evidence
→ failure stage
→ root cause
→ affected layer only
→ reviewed regression where runnable
→ deterministic procedural regression where applicable
→ full Windows CI/publish/product smoke/package
→ PATCH release
→ public release readback
→ canonical docs sync
```

새 실제 evidence 없이 Scanner threshold/candidate cap/OCR/matcher/visual acceptance를 선제 조정하지 않는다.

## 18. 공식 문서

- `docs/CURRENT_STATE.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/SCANNER.md`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/MAINTENANCE_CONTRACTS.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/ARCHITECTURE.md`
- `docs/DECISION_PRODUCT_COMPLETE_2026-08-26.md`
- `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`
- `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`
- `docs/DECISION_V1.7.9_MINI_SCANNER_SHOW_2026-08-26.md`
- `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`
- `docs/DECISION_V1.7.11_MAINTENANCE.md`
- `docs/DECISION_LONG_TERM_MAINTENANCE_AUDIT_2026-08-27.md`
- `docs/DECISION_V1.7.12_MAINTENANCE.md`
- `docs/RELEASE_NOTES_V1.7.12.md`
- `docs/RELEASE_1.7.12.md`
- `docs/.release-v1.7.12-status.json`

## 19. 유지보수 안전 계약 및 알려진 기술 부채

### Scanner support-bundle privacy

`Scanner > 고급 > Scanner 성능 진단 자료 내보내기`의 support ZIP은 환경/성능 trace와 bounded diagnostic log만 포함한다.

다음은 포함하지 않는다.

- Scanner Ground Truth image / source pixel dataset
- `user.db` 또는 profile database
- Tarkov/game account information
- 사용자 진행도나 계정 식별에 해당하는 데이터

Exporter 변경 시 이 exclusion을 release regression으로 계속 검증한다.

### Diagnostic OCR adapter

현재 `SerializedScannerOcrEngine`은 production `ScannerLab38OcrEngine`을 `DiagnosticScannerLab38OcrEngine`으로 감싸 fine-grained WinRT OCR timing/health telemetry를 수집한다. 이 diagnostic adapter는 기존 production engine instance를 재사용하기 위해 내부 `_engine` 접근에 reflection을 사용한다.

이 구조는 v1.7.6 문제-PC에서 검증된 실행 behavior를 보존하기 위해 당시 즉시 정리하지 않은 **알려진 유지보수 기술 부채**다.

향후 구조 정리 시에는:

- exact telemetry/health policy를 raw OCR owner로 이동
- diagnostic adapter/reflection 의존 제거
- 사용자-reviewed Ground Truth 회귀 `REGRESSION=0` 유지
- v1.7.6 문제-PC performance evidence와 현재 latency contract 유지
- full Windows CI/publish/Scanner product smoke 통과

를 모두 만족해야 한다. 코드 미관만을 이유로 이 경계를 선제 변경하지 않는다.

## 20. 2026-08-27 유지보수 기반 정리

제품 runtime/version/release를 변경하지 않고 앞으로의 유지보수 안전성을 높이는 작업을 PR #196에서 완료했다.

완료 범위:

- `docs/MAINTENANCE_CONTRACTS.md`를 유지보수 안전 계약으로 추가
- 버전 종속 `v1.7.1` Live Data Probe를 장기 운영형 `.github/workflows/live-data-probe.yml`로 교체
- Live Probe는 hermetic PR/main CI와 분리하고 daily schedule + manual dispatch로 운용
- production Game Content build와 동일하게 json.tarkov.dev, edition source, Tarkov Wiki Ballistics source를 관찰
- Wiki Ballistics failure/schema drift는 production과 동일하게 fail-soft warning으로 기록하고 기본 canonical content의 Fatal validation과 구분
- Live Probe는 성공/경고/실패 모두 Actions log에 주요 entity 수량과 source warning을 명시적으로 남김
- `ContentUpdateCompletenessGuard`의 50% retained boundary와 no-baseline 의미를 회귀 테스트로 고정
- collection schema가 array/object 밖으로 drift하면 fail closed하는 회귀 추가
- `DATA_VALIDATION.md`, `ARCHITECTURE.md`, `DEVELOPER_REFERENCE.md`를 현재 runtime 계약과 일치시킴

검증:

```text
final PR head: f996d945f6c59548e8287ba930573c193fc522fc
final PR CI: 33038571161 — SUCCESS
396 passed / 0 failed / 0 skipped
Windows publish: SUCCESS
Product UI / Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / package verification: SUCCESS

one-time live probe run: 33038439864 — SUCCESS
Regular: items=5312 quests=517 objectives=1457 questItems=305 hideout=26 ammo=200 validationIssues=0 fatal=0
PvE:     items=5312 quests=514 objectives=1434 questItems=293 hideout=26 ammo=200 validationIssues=0 fatal=0
Wiki Ballistics: registered 186/200 ammo, safely matched effectiveness for 186

PR #196: MERGED
main merge: bbb8b9b64116c71889cfc4fe3f208ef0e1d67bc6
main CI: 33038777867 — SUCCESS
396 passed / 0 failed / 0 skipped
```

Live Probe의 `sourceWarnings=1`은 Regular/PvE 각각 Wiki Ballistics가 186/200종을 확인하고 186종을 안전하게 매칭했다는 enrichment 상태 메시지이며 Fatal validation은 0이다.

main CI에서도 Windows publish, Product UI / Map / Factory / MiniMap smoke, graceful shutdown, package verification이 모두 성공했다. 앞으로 외부 source drift는 장기 `live-data-probe.yml`로 일반 제품 CI와 분리해 감시한다.

## 21. 2026-08-27 장기 완성도 audit / v1.7.12 완료

PR #197에서 첫 장기 performance/dead-code/architecture audit와 Desktop lifecycle 경계 개선을 완료하고 v1.7.12로 공개했다.

최종 검증/배포:

```text
PR #197 final head: 23e1784c25954f4900a57cfa4c1c9821d5d6d668
final PR CI: 33042136686 — SUCCESS
397 passed / 0 failed / 0 skipped

PR #197: MERGED
exact product release source: d8d0f8eb1ffdd9b8c4ec890277a7b209b2458c2b
main CI: 33042307773 — SUCCESS
397 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Scanner / Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS

Release workflow: 33042464642 — SUCCESS
public release: v1.7.12
release id: 377581895
Junhyun-Helper.zip: 80,477,641 bytes
SHA-256: 3f0d57f8a5dc92611bc8648a423c43d65917e63e0d73a771b559153803186fa1
```

Public `/releases/latest`와 tag ref 모두 v1.7.12 exact source `d8d0f8eb...`을 가리키고 공개 ZIP digest가 main-CI package와 일치한다.

이후 release record/STATE/README 같은 documentation-only commit은 v1.7.12 제품 릴리즈 소스가 아니다. Release workflow는 이미 공개된 stable version을 immutable하게 취급하고 required assets만 확인해야 한다.
