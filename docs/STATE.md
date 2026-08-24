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
- Scanner Ground Truth correction / diagnostics / full-pipeline regression

Runtime GPT/AI 의존성은 없습니다.

## 2. 공개 릴리즈와 현재 개발

현재 public stable / latest는 **v1.4.4**입니다.

```text
version: v1.4.4 PUBLIC RELEASE / VERIFIED
source/tag: 0c7f31e118122ffef6e5999f7a20a77d823a450d
asset: Junhyun-Helper-v1.4.4-win-x64.zip
bytes: 80,391,895
SHA-256: 64320e36ba94b6f206ef997e3d42a809c7beef2c859f4bc7f53f704f74866f40
ProductVersion: 1.4.4+0c7f31e118122ffef6e5999f7a20a77d823a450d
release run: 32680058795 — SUCCESS
public verifier: 32680422756 — SUCCESS
public/latest / exact tag / redownload / checksum / layout / EXE smoke: VERIFIED
```

공식 검증 기록:

- `docs/.release-v1.4.4-status.json`

현재 개발 목표는 **v1.5.0 Product Finishing Pass**입니다.

```text
branch: product/v1.5.0-usability-data-hardening
PR: #172 — Build v1.5.0 product finishing pass
status: IMPLEMENTATION COMPLETE / FINAL RELEASE GATE
Desktop Version: 1.5.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v5
Scanner catalog cache: v1/v2 readable, v2 written
```

공식 문서:

- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`
- `docs/STATUS_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`
- `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`
- `docs/RELEASE_NOTES_V1.5.0.md`

GitHub PR HEAD와 CI가 이 문서보다 최신이면 GitHub 상태를 우선합니다.

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

Pinned Map donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

기존 `Propeex/Tarkov-Helper`는 요구사항의 권위가 아닙니다. 기존 기능 확인, 검증된 데이터/자산, 구현 아이디어, 시행착오 참고 용도로만 사용합니다.

## 4. v1.5.0 승인 범위 상태

완료:

1. Scanner highest-trader/price-per-slot 등 mapped-data 누락 수정
2. Quest `확인 필요` 최신 live-data 전수 감사와 fail-closed compatibility 갱신
3. 일반 Game Data + Scanner catalog/market refresh 통합
4. 사용자 OCR 문자/문자열 치환
5. 후보 선택형 Ground Truth + manual rectangle / `없음` fallback
6. Scanner latency telemetry + 정확도 보존 최적화
7. continuous Scanner 결과 안정화
8. diagnostics/log retention
9. Scanner 일반/settings/advanced UI 분리 + 빠른 현재 결과 교정
10. 전체 제품 UI consistency audit

남은 것은 final CI / merge / public release / independent public verification / housekeeping입니다.

## 5. Scanner 제품 계약

Scanner는 게임 프로세스 내부 데이터를 읽지 않는 화면 기반 closed-domain recognizer입니다.

Production pipeline:

```text
capture
→ detail rectangle proposals
→ inspect-header semantic validation
→ title ROI
→ Windows ko-KR OCR
→ optional user substitution
→ current-catalog sanitation / normalization
→ matching / bounded recovery / optional visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ Mini Scanner
→ user correction / Ground Truth
```

절대 유지 정책:

- false positive보다 miss 선호
- rectangle geometry / structural score는 proposal evidence이며 identity proof가 아님
- `HEADER_FRAME_LOCKED >= 0.68`
- magnifier + red close-X 필요
- structural floor `0.34`
- continuous candidate max `8`
- one-shot candidate max `12`
- current official Korean Tarkov item catalog가 identity authority
- production OCR field는 `item_name` 하나
- trader/flea/slots/needed는 Item ID 이후 mapped data
- ambiguity / insufficient evidence는 fail closed
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지
- 자동 global `r/0/한글` 강제 substitution table 금지

## 6. Capture / semantic header

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

- 일반 UI `1회 스캔`과 기본 `Ctrl+Shift+F10`은 TarkovWindow 한 번 정밀 분석
- developer test one-shot은 기본 `Ctrl+Shift+F11`
- continuous mode를 영구 변경하지 않음
- shared recognition state와 직렬화
- scan-time catalog network refresh 시작하지 않음

Detail geometry 책임은 rectangle proposal 생성입니다.

- red-X connected-component proposal
- rectangle/edge fallback
- structural floor `0.34`
- continuous 8 / one-shot 12 candidates
- aspect는 weak ordering hint
- tall/large panel을 aspect만으로 제거하지 않음
- 사실상 동일 edge-jitter만 dedupe

Runtime OCR gate:

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
AND Magnifier evidence present
AND Close-X evidence present
```

## 7. OCR / matcher / user substitution

Primary recognizer는 Windows `ko-KR` OCR입니다.

- normal OCR
- 필요 시 deep/high-contrast/binary/inverse variants
- raw OCR / user-substituted text / normalized matcher text를 구분
- exact-first + conservative fuzzy + top1/top2 separation
- bounded current-catalog recovery
- optional local Tarkov-font corroboration/recovery
- ambiguous/low-confidence는 Item ID 미확정

사용자 치환 처리:

```text
raw OCR
→ user substitution
→ catalog sanitation / normalization
→ matching
```

- persistent rules
- add / delete / per-rule ON/OFF / reset
- single-pass/non-recursive
- default empty
- raw OCR forensic evidence는 절대 덮어쓰지 않음

자동 알고리즘은 current-catalog 밖 embedded glyph를 특정 `r`, `0`, `I`, `l`, 한글로 전역 강제 치환하지 않습니다.

## 8. Ground Truth / correction / regression

저장 root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

기본 교정은 detector evidence 선택입니다.

- detail rectangle candidate
- red close-X candidate
- magnifier candidate
- item-name ROI candidate
- correct item/text

Candidate에 정답이 없으면 manual rectangle fallback을 사용하고, 실제 semantic object가 없어야 하면 `없음`을 기록합니다. Candidate ID / rank / score / geometry가 Ground Truth와 함께 저장됩니다.

대표 evidence:

- full / detail / ROI / processed / annotated images
- case metadata
- raw/substituted/normalized OCR
- matcher candidates
- structural/header evidence
- mapped presentation
- user Ground Truth

Full-pipeline regression은 저장된 `full.png`를 현재 detail proposal → semantic header → ROI → OCR/recovery → catalog matching → final Item ID 경로로 다시 실행합니다.

## 9. Scanner 표시 데이터

Production OCR field는 `item_name` 하나입니다.

Item ID 확정 후 mapped data:

- highest non-flea trader sell price
- best trader name
- positive flea `avg24hPrice`
- positive `width × height` slots
- trader/flea price per slot
- `NeededItems[itemId].RequiredTotal`

Inventory 차감 부족량은 Scanner의 `필요 개수` 의미가 아닙니다. Market/dimension 일부가 누락되어도 해당 필드만 비우고 Item identity를 폐기하지 않습니다.

## 10. Scanner update / UI

상단 `데이터 업데이트`가 일반 Game Content와 Scanner catalog/market refresh를 함께 담당합니다.

- 일반 데이터가 성공한 뒤 Scanner refresh만 실패하면 일반 데이터 rollback 금지
- 기존 healthy Scanner cache 유지
- partial failure를 사용자 상태로 보고
- Scanner-only forced refresh는 `고급 / 진단` recovery action

Scanner 일반 surface:

- Scanner ON/OFF
- 1회 스캔
- 현재 결과 교정
- runtime status
- 최근 인식 기록

`설정`:

- global hotkeys
- OCR substitution
- Mini Scanner 표시 항목

`고급 / 진단`:

- display test
- recognition image
- regression
- GT export/manage
- forced catalog refresh
- log clear

Mini Scanner 우클릭 → `현재 결과 교정`을 지원합니다.

## 11. Scanner latency / continuous stabilization

Latency stages:

- capture
- rectangle proposal
- semantic header
- normal OCR
- deep OCR
- visual recovery
- catalog matching/recovery
- presentation
- end-to-end

정확도 보존 최적화:

- active scan-cycle 내부에서 dimensions/format/pixels SHA-256이 완전히 같은 OCR bitmap만 reuse
- normal/deep cache 분리
- frame/cycle 간 OCR cache 없음
- threshold/candidate cap 변경 없음

Continuous trusted result:

- 가장 가까운 detail candidate가 verified geometry 범위 안이고 title identity signature가 유지되면 기존 Item ID/snapshot을 유지
- title identity는 bright glyph shape를 사용해 harmless dark-background/trailing-ROI variation을 흡수
- glyph가 달라지면 signature도 달라짐
- signature 계산 실패 시 detector exact signature fallback
- 다른 identity evidence가 확인되면 previous trusted item 폐기 후 재검증
- detector 단발 miss는 bounded consecutive-miss 경로에서 처리

## 12. Retention / logs

자동 삭제 금지:

- user-reviewed Ground Truth
- review/ownership metadata가 unknown/corrupt인 Case

자동 unreviewed diagnostic Case:

- max age 30 days
- max 300 cases
- max 512 MiB
- recent 2-hour safety window

일반 Scanner/startup logs는 bounded rotation을 사용합니다.

대표 위치:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
%LocalAppData%/JunhyunHelper/logs/startup.log
```

## 13. Mini Scanner

- matched Item만 표시
- status/error text는 overlay에 표시하지 않음
- WPF Topmost + native HWND_TOPMOST
- ShowActivated=false / no-activate
- 전체 카드 drag
- Tarkov foreground + inventory/stash context를 보수적으로 확인
- inventory context OCR probe max 1 / latest request coalesce
- item/context epoch가 바뀐 stale result 적용 금지
- current result correction context menu 제공

Title OCR과 inventory-context OCR은 하나의 WinRT serialization boundary를 공유합니다.

## 14. Persistence / 사용자 데이터

대표 위치:

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

- portable executable 옆 mutable user data/log 생성 금지
- Program Update가 user.db, content/image cache, Map/Ammo/Scanner settings, logs/diagnostics를 교체하지 않음
- v1.5.0 user.db migration 없음

## 15. Quest v1.5.0 live audit

공식 문서: `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`

Audited drift:

- Prapor LL3 pool: 6 → 7
- Mechanic LL2: 10 → 11
- Ragman LL3 seed: 5 → 6
- Skier LL4 seed: 4 → 5
- Skier LL2: Regular/PvE 4, PvP Season 3

`QuestTaskPoolVariableCompatibility`는 trader / LL / thresholds / quest membership / GameMode가 감사 구조와 일치할 때만 synthetic interpretation을 허용합니다. Exact profile variable은 항상 우선하고 조금이라도 구조가 다르면 fail closed합니다.

마지막 temporary live audit run `32687388519`은 SUCCESS였으며 audit 완료 후 temporary workflow는 release candidate에서 제거했습니다.

## 16. Program Update / Release 계약

- GitHub public stable release가 Program Update authority
- release tag / Desktop Version / FIRST_RUN / ZIP / ProductVersion identity 일치
- Windows x64 .NET 10 self-contained single-file
- installer 없음 / 관리자 권한 불필요
- package root: `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets/`
- exact-source build/test/publish/full Product UI/Map/Scanner smoke
- draft asset redownload + hash/layout/ProductVersion/smoke 검증
- stable/latest publish
- fresh runner에서 인증 없이 public release metadata/tag/assets 재조회
- public ZIP/SHA256SUMS redownload + hash/layout/ProductVersion/full EXE smoke
- durable `docs/.release-vX.Y.Z-status.json` 기록
- 완료 후 temporary release/public verifier workflow 제거

## 17. 검증 상태 / 다음 작업

v1.5.0 full pre-finalization gate:

```text
HEAD: 7cb9aea9e62b900ed2972196789e5127a405d21e
CI: 32687388529 — SUCCESS
296 tests / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Map / Scanner smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
```

그 뒤 version 1.5.0, FIRST_RUN, release notes, status docs, README/canonical docs와 temporary audit cleanup을 반영했습니다. 따라서 **최종 PR HEAD의 fresh CI green이 merge 전 필수**입니다.

다음 단계:

1. final PR HEAD CI green
2. PR #172 merge
3. exact main merge SHA normal CI green
4. exact merge SHA에 고정한 temporary v1.5.0 release workflow
5. exact-source release + draft verification
6. public stable/latest publish
7. independent anonymous public redownload/hash/layout/ProductVersion/full EXE smoke
8. `docs/.release-v1.5.0-status.json` 기록
9. temporary release workflow cleanup
10. `STATE.md`, `CURRENT_STATE.md`, README를 `v1.5.0 PUBLIC RELEASE / VERIFIED`로 최종 갱신
