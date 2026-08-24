# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-24
상태: **v1.6.0 PUBLIC RELEASE / VERIFIED — LIVE GROUND TRUTH MAINTENANCE**

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다.

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
- Scanner Ground Truth 교정 / diagnostic dataset / full-pipeline regression

Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper` 프로토타입은 제품 요구사항의 권위가 아니다. 유지할 기능, 검증된 데이터/자산, 구현 아이디어, 시행착오 참고 용도로만 사용한다.

## 2. 공개 stable과 현재 source

현재 public stable/latest는 **v1.6.0**이다.

```text
exact release source/tag: e18c108380572913552030aa677bba06ebf49355
stable asset: Junhyun-Helper.zip
stable bytes: 80,425,013
stable SHA-256: f9384ff49d522afb5976efe291ff932d66063dcfeee64b0aed7a5daa691a12c5
v1.5 bridge asset: Junhyun-Helper-v1.6.0-win-x64.zip
bridge bytes: 80,424,089
bridge SHA-256: 3f05b20ccbd7463fb590889042b1b706290a88e0568cd00c3b2fa23cf966dfc8
release verification run: 32710012954
299 passed / 0 failed / 0 skipped
public/latest: VERIFIED
anonymous public redownload + EXE smoke: SUCCESS
```

현재 `main`은 public v1.6.0 exact source 이후의 release-record/housekeeping commit을 포함할 수 있다. 공개 v1.6.0 제품 source의 권위는 tag `v1.6.0`과 위 exact source SHA다.

Schema / compatibility:

```text
Desktop target version: 1.6.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1/v2 readable, v2 written
Scanner Ground Truth dataset: local diagnostics persistence
```

v1.6.0 공식 문서:

- `docs/DECISION_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`
- `docs/STATUS_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`
- `docs/RELEASE_NOTES_V1.6.0.md`
- `docs/RELEASE_1.6.0.md`
- `docs/SCANNER.md`
- `docs/CURRENT_SCANNER_WORK.md`

## 3. 아키텍처

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

책임:

- **Core**: canonical domain, deterministic calculation, Quest 조건 규칙, Scanner structural/identity/matcher 규칙
- **Application**: 사용자 use case, authoritative mutation, workspace orchestration
- **Infrastructure**: HTTP/source parsing, SQLite/file persistence, Game Content/Scanner/update I/O
- **Desktop**: WPF UI, presentation, Scanner capture/OCR/runtime/diagnostics, Map bridge
- **Map/MiniMap donor**: 제한적 compile-link 예외. donor updater/content ownership/hidden command는 사용하지 않음

현재 pinned Map donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

## 4. 사용자 데이터 / persistence

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
- user-reviewed Scanner Ground Truth는 자동 삭제하지 않음
- Scanner logs와 Ground Truth dataset lifetime을 분리

## 5. Game Content / Scanner catalog update

사용자는 일반 데이터 업데이트와 Scanner item/market catalog 갱신을 별도 절차로 이해할 필요가 없다.

```text
remote Game Content
→ validate/build new content
→ general content activation
→ Scanner official item/market catalog refresh
→ status report
```

Scanner refresh만 실패하면 healthy general Game Content를 rollback하지 않는다. 기존 healthy Scanner cache를 유지하고 partial failure를 보고한다.

v1.6.0 일반 Scanner 화면에는 catalog force-refresh action을 노출하지 않는다.

## 6. Quest availability / latest live-data audit

`확인 필요`는 숨겨야 할 UI 노이즈가 아니라 안전하게 판정할 수 없는 조건을 나타낸다.

2026-08-24 live audit 대상:

- `regular`
- `pve`
- `pvp-season`

Task-pool/profile-variable compatibility는 audited structure와 GameMode가 일치할 때만 synthetic value를 허용한다. Pool membership, threshold, trader, requirement shape가 달라지면 추측하지 않고 fail closed한다.

Reference: `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`

## 7. Scanner production contract

Scanner는 게임 프로세스 내부 데이터를 읽지 않는 화면 기반 Tarkov UI recognizer다.

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ red close-X / magnifier / neutral header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog sanitation / normalization
→ conservative catalog matching / bounded recovery
→ optional local Tarkov-font visual corroboration/recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional user correction / Ground Truth
```

핵심 불변 계약:

- false positive보다 miss 선호
- geometry/structural score는 proposal evidence이며 Item identity proof가 아님
- full semantic gate 전 production OCR identity path 진입 금지
- `HEADER_FRAME_LOCKED >= 0.68`
- valid magnifier + red close-X evidence 필수
- structural floor `0.34`
- continuous max 8 candidates
- one-shot max 12 candidates
- current official Korean Tarkov full-item catalog가 identity authority
- ambiguity / low confidence는 fail closed
- production OCR field는 item-name 하나
- price/flea/slots/needed는 Item ID 이후 mapped data
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지
- product-default automatic global forced substitution 금지
- cross-frame OCR cache 금지
- reviewed Ground Truth 없이 threshold/cap 완화 금지

## 8. Capture / one-shot / semantic header

Tarkov capture:

```text
EscapeFromTarkov process/window
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ PrintWindow 우선
→ invalid/empty이면 exact client screen rectangle fallback
```

Display Test는 같은 recognition pipeline을 적용하며 real continuous Scanner와 상호 배타적이다.

One-shot 기능은 v1.6.0에서도 유지한다.

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

일반 화면의 `1회 스캔` 버튼은 제거했지만 기능 자체를 삭제한 것이 아니다.

Rectangle proposal:

- RED-X component + rectangle/edge fallback
- aspect prior는 약한 ranking hint
- tall/large detail을 aspect alone으로 reject하지 않음
- high IoU alone으로 dedupe하지 않음
- edge가 실질적으로 다른 후보는 semantic stage까지 보존
- near-identical edge jitter만 정리

OCR 진입 최소 gate:

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
AND Magnifier evidence present
AND Close-X evidence present
```

Oversized/coarse recovery도 같은 gate를 다시 통과해야 한다.

## 9. OCR / matcher / substitution

Primary recognizer는 Windows `ko-KR` OCR이다.

```text
locked item-name ROI
→ normal/deep OCR variants
→ raw OCR preservation
→ optional user substitution
→ current-catalog sanitation / normalization
→ exact-first conservative matching
→ bounded recovery
→ optional visual recovery
```

- raw OCR forensic evidence는 별도 보존
- user substitution은 single ordered pass
- 기본 substitution list는 empty
- recursive/cyclic/chained reprocessing 없음
- current catalog impossible glyph를 특정 r/0/I/l로 전역 강제 치환하지 않음
- ambiguous candidate는 fail closed

Scanner display settings는 v6이지만 기존 user OCR substitution data는 migration에서 보존한다.

## 10. Scanner mapped presentation / item search

Item ID 확정 후 local trusted data:

- highest valid non-flea trader RUB price
- best trader name where trustworthy
- flea positive `avg24hPrice`
- slots = positive `width × height`
- trader price/slot
- flea price/slot
- required total = `NeededItems[itemId].RequiredTotal`

Inventory shortage는 Scanner `필요 개수` 의미가 아니다.

v1.6.0 Scanner item search는 같은 current full-item catalog와 local presentation data를 사용한다.

검색 순간 network request를 만들지 않는다.

검색 결과: cached icon + official name.
선택 후: icon/name/Wiki/flea/best trader/current needed.

## 11. Scanner 일반 UI — v1.6.0

상단 primary actions:

- `스캐너 ON/OFF`
- `설정`
- `고급`

하단:

- 왼쪽 `아이템 검색`
- 오른쪽 `Scanner 로그`

`설정`은 hotkey와 Mini Scanner display/order를 우선한다.

`고급`은 Display Test, current-result correction, correction dataset management 같은 실사용 진단 흐름을 우선한다.

개발/복구 action을 일반 Scanner surface에 펼쳐 놓지 않는다.

## 12. Mini Scanner / settings schema v6

항상 표시하는 identity header:

- item icon
- official item name

사용자가 표시 여부와 순서를 저장하는 field:

1. trader sell price
2. flea average price
3. trader price/slot
4. flea price/slot
5. current needed

가능하면 trader row는 실제 최고가 상인명 + price 형식을 사용한다.

예: `Therapist 42,000₽`

v5 이하 migration에서 가능한 한 다음을 보존한다.

- enabled state
- 3 hotkeys
- 기존 visibility
- position
- font size
- OCR substitutions

Mini Scanner window safety:

- Topmost
- ShowActivated=false
- ShowInTaskbar=false
- full-surface drag
- matched Item presentation only
- inventory OCR single-active/latest coalescing
- stale epoch reject

## 13. Ground Truth / correction / saved Case re-edit

Root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

대표 evidence:

- full.png
- detail/title/processed ROI
- annotated image
- case.json
- candidate_selection.json
- raw/substituted/normalized OCR
- Item ID / official name
- confidence / second / margin
- matcher top candidates
- structural/header evidence
- mapped presentation
- user Ground Truth

v1.6.0 correction UX:

- 큰 원본 image는 viewport 안에 auto-fit
- display scale과 saved coordinate를 분리
- Ground Truth ROI는 항상 original pixel coordinate
- detail / close-X / magnifier / item-name ROI 후보를 image 위 box 직접 클릭
- correct candidate 없음 → manual rectangle
- actual semantic object 없음 → explicit `없음`

저장된 Case는 correction data manager에서 다시 열 수 있다.

복원 source:

- `case.json`
- `full.png`
- `candidate_selection.json`

same Case ID를 유지해 reviewed Ground Truth를 재교정한다. 복원 실패 시 기존 Case를 보존한다.

자동 diagnostic Case는 정답이 아니다. user-reviewed Case만 Ground Truth다.

Replay result:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

기존 정상 reviewed Case가 실패하면 평균 성능과 무관하게 REGRESSION이다.

## 14. Latency / stabilization / retention

Stage telemetry:

- capture
- rectangle-proposal
- semantic-header
- ocr-normal
- ocr-deep
- visual-recovery
- catalog-matching
- presentation
- end-to-end

OCR reuse는 같은 active scan cycle + exact-identical pixel bitmap일 때만 허용한다.

Title continuity signature는 이미 trusted semantic detail의 continuity evidence일 뿐 Item identity proof가 아니다.

Reviewed Ground Truth는 자동 삭제하지 않는다.

Automatic unreviewed diagnostics only:

```text
max age = 30 days
max cases = 300
max bytes = 512 MiB
recent protection = 2 hours
```

Corrupt/unknown metadata는 preserve fail closed한다. Logs는 bounded rotation한다.

## 15. Release / Program Update contract

배포 형태:

```text
Windows x64
.NET 10 self-contained single-file
installer 없음
관리자 권한 불필요
```

v1.6.0부터 stable user package:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

ZIP/folder name에는 version을 넣지 않는다.
Version identity는 Desktop project / ProductVersion / tag / GitHub Release metadata에 둔다.

Program Update는 public stable의 exact asset/checksum/package identity를 검증한 뒤 program-owned files만 transaction 교체한다. 사용자 LocalAppData는 유지한다.

Release gate:

- exact source build/test/publish/smoke
- exact ProductVersion/FIRST_RUN identity
- stable ZIP 생성 + top-level `준현 헬퍼/` 구조 검증
- exact tag/source identity
- public stable/latest publication
- independent anonymous public redownload
- checksum/hash/size/layout/ProductVersion/FIRST_RUN 검증
- public-downloaded EXE Product UI/Map/Scanner smoke + graceful shutdown
- durable machine-readable release status
- temporary release/verifier workflow cleanup

## 16. v1.6.0 검증 현황

중간 feature/UI gate CI `32700507526`:

```text
Desktop build: SUCCESS
automated tests: 296 / 296 SUCCESS
Windows x64 publish: SUCCESS
Product UI / Scanner / Mini Scanner smoke: SUCCESS
Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown: SUCCESS
artifact upload: SUCCESS
```

이 성공 후 version 1.6.0, FIRST_RUN, stable ZIP CI gate, final docs를 추가했으므로 **최신 HEAD final CI가 release prerequisite**다.

## 17. 현재 개발 방향

v1.6.0 공개 검증 전:

1. latest HEAD final CI
2. PR #174 merge
3. main push CI
4. exact source/tag
5. public v1.6.0 release
6. public ZIP/EXE independent verification
7. final release status 기록

v1.6.0 공개 검증 후:

```text
real Tarkov usage
→ representative correct result review
→ miss/wrong identity correction
→ reviewed Ground Truth accumulation
→ failure stage classification
→ affected stage only modification
→ full reviewed replay
→ REGRESSION=0
→ PATCH 판단
```

추가 evidence 없이 matcher confidence, top1/top2 margin, header floor, structural floor, candidate caps를 완화하지 않는다.

작은 기술 부채 `ScannerLatencyTypeAliases.cs`는 release risk를 감수해 제거할 사안이 아니며 향후 PATCH cleanup 후보로 유지한다.
