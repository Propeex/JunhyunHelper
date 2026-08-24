# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-24
상태: **v1.5.0 PUBLIC RELEASE / VERIFIED**

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
- Scanner Ground Truth 교정 / 진단 dataset / full-pipeline regression

Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper` 프로토타입은 제품 요구사항의 권위가 아니다. 유지할 기능, 검증된 데이터/자산, 구현 아이디어, 시행착오 참고 용도로만 사용한다.

## 2. 현재 공개 릴리즈

현재 public stable / latest는 **v1.5.0**이다.

```text
version: v1.5.0 PUBLIC RELEASE / VERIFIED
exact release source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
asset: Junhyun-Helper-v1.5.0-win-x64.zip
bytes: 80,422,292
SHA-256: 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
ProductVersion: 1.5.0+6de738959740d12e6ccb81b65e50006e463eb699
automated tests: 296 passed / 0 failed / 0 skipped
release run: 32691423654 — SUCCESS
independent public verifier: 32691641614 — SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public redownload: VERIFIED
SHA256SUMS: VERIFIED
package layout: VERIFIED
public EXE smoke: SUCCESS
```

공식 기록:

- `docs/.release-v1.5.0-status.json` — machine-readable durable verification
- `docs/RELEASE_1.5.0.md` — 공개 검증 기록
- `docs/RELEASE_NOTES_V1.5.0.md` — 사용자 변경점
- `docs/STATUS_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md` — 구현/릴리즈 완료 상태
- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md` — 승인된 범위/설계 결정

Schema / compatibility:

```text
Desktop Version: 1.5.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v5
Scanner catalog cache: v1/v2 readable, v2 written
Scanner Ground Truth dataset: local diagnostics persistence
```

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
- Scanner 일반 로그 삭제와 Ground Truth dataset 삭제는 독립 동작

## 5. Game Content / Scanner catalog update

v1.5.0부터 사용자는 일반 데이터 업데이트와 Scanner item/market catalog 갱신을 별도 절차로 이해할 필요가 없다.

상단 Game Data update 흐름이 다음을 함께 orchestration한다.

```text
remote Game Content
→ validate/build new content
→ general content activation
→ Scanner official item/market catalog refresh
→ status report
```

Scanner refresh만 실패하면 건강한 일반 Game Content를 rollback하지 않는다. 기존 healthy Scanner cache를 유지하고 부분 실패를 상태로 보고한다.

Scanner 화면의 `아이템 목록 최신화`는 일반 사용의 필수 절차가 아니라 고급/복구 기능으로 유지한다.

## 6. Quest availability / 최신 live-data 감사

`확인 필요`는 숨겨야 할 UI 노이즈가 아니라 안전하게 판정할 수 없는 조건을 나타낸다.

2026-08-24 live audit는 다음 GameMode를 대상으로 했다.

- `regular`
- `pve`
- `pvp-season`

Task-pool/profile-variable compatibility는 audited structure와 GameMode가 일치할 때만 synthetic value를 허용한다. Pool membership, threshold, trader, requirement shape가 달라지면 추측하지 않고 fail closed한다.

Reference:

- `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`

새 Tarkov data가 들어왔을 때 `확인 필요`가 증가하면 UI masking보다 source requirement shape와 evaluator coverage를 먼저 감사한다.

## 7. Scanner 제품 계약

Scanner는 게임 프로세스 내부 데이터를 읽지 않는 독립적인 화면 기반 Tarkov UI recognizer다. 범용 OCR이 아니라 current official catalog를 사용하는 closed-domain recognizer로 취급한다.

Production pipeline:

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ red close-X / magnifier / neutral header semantic validation
→ HEADER_FRAME_LOCKED
→ locked detail bounds / item-name ROI
→ Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog character/symbol sanitation
→ conservative catalog matching / bounded recovery
→ optional local Tarkov-font visual corroboration/recovery
→ Item ID or fail closed
→ local mapped presentation
→ Mini Scanner
→ optional user correction / Ground Truth
```

핵심 불변 계약:

- false positive보다 miss를 선호
- rectangle geometry는 proposal이며 identity proof가 아님
- structural score는 Item identity score가 아님
- full semantic header gate 전에는 production OCR identity path 진입 금지
- `HEADER_FRAME_LOCKED >= 0.68`
- valid magnifier evidence 필수
- valid red close-X evidence 필수
- structural floor `0.34`
- continuous max 8 candidates
- one-shot max 12 candidates
- current official Korean Tarkov item catalog가 Item identity authority
- ambiguity / low confidence는 fail closed
- scan-time network 금지
- game memory read 금지
- DLL injection 금지
- packet interception 금지
- production OCR field는 `item_name` 하나
- price/flea/slots/needed는 Item ID 이후 mapped data
- 실제 Ground Truth 없이 global threshold/candidate cap 완화 금지
- 제품 기본값에 자동 global r/0/한글 forced substitution table 금지

## 8. Capture / proposal / semantic header

### Tarkov capture

```text
EscapeFromTarkov process/window
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ PrintWindow 우선
→ invalid/empty이면 exact client screen rectangle fallback
```

Display Test는 연결된 전체 display에 같은 recognition pipeline을 적용한다. 실제 Scanner mode와 Display Test continuous mode는 상호 배타적이다.

One-shot:

- `1회 스캔`은 Tarkov window를 한 번 정밀 분석
- 전역 기본 단축키: Ctrl+Shift+F10
- 테스트 one-shot 기본 단축키: Ctrl+Shift+F11
- Scanner ON/OFF 기본 단축키: Ctrl+Shift+F12
- continuous mode를 영구 변경하지 않음
- scan-time catalog network refresh를 시작하지 않음

### Rectangle proposal

Geometry의 책임은 가능한 detail rectangle을 만드는 것이다.

- red-X connected component proposal
- rectangle/edge fallback proposal
- historical aspect prior는 약한 ranking hint
- tall/large detail window를 aspect prior만으로 제거하지 않음
- high IoU 자체는 dedupe 조건이 아님
- 실제 edge가 다른 후보는 semantic stage까지 보존
- 사실상 같은 edge-jitter duplicate만 정리

### Semantic detail identity

OCR 진입 조건:

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
AND Magnifier evidence present
AND Close-X evidence present
```

Evidence는 red X shape/color, frame-left magnifier shape, neutral header/frame, dark title field, title text evidence를 결합한다.

Oversized/coarse rectangle recovery도 같은 semantic gate를 다시 통과해야 한다.

## 9. OCR / matcher / user substitution

Primary recognizer는 Windows `ko-KR` OCR이다.

- title-size 기반 확대
- normal OCR 후 필요 시 deep/high-contrast/binary/inverse variants
- raw OCR과 실제 matcher input 분리 보존
- exact-first + conservative fuzzy + top1/top2 margin
- current catalog 밖 impossible glyph는 특정 문자로 임의 확정하지 않음
- bounded unique recovery만 허용
- 필요 시 local Tarkov-font visual corroboration

### User OCR substitution — schema v5

사용자가 반복해서 검증한 OCR 오류는 exact 문자열 규칙으로 등록할 수 있다.

```text
raw OCR
→ enabled user substitutions (single pass)
→ catalog sanitation / normalization
→ matcher
```

계약:

- 기본 규칙 목록은 비어 있음
- 규칙 추가/삭제/ON·OFF/초기화 지원
- raw OCR forensic evidence는 덮어쓰지 않음
- substitution 결과를 raw/normalized/matched 결과와 구분
- 한 번의 ordered pass만 수행
- recursive/cyclic reprocessing 없음
- 사용자가 만든 규칙은 automatic product-wide substitution table이 아님

## 10. Scanner mapped presentation

Production OCR field는 item-name 하나다. 다음 값은 Item ID 확정 후 local trusted data에서 계산한다.

- highest trader sell price: flea 제외 유효 판매처 RUB 환산 가격 최댓값
- best trader name: highest price source가 신뢰 가능할 때
- flea average: positive `avg24hPrice`
- slots: positive `width × height`
- trader price/slot: price와 slots 모두 유효할 때
- flea price/slot: flea price와 slots 모두 유효할 때
- required count: `NeededItems[itemId].RequiredTotal`

Inventory를 차감한 부족량은 Scanner의 `필요 개수` 의미가 아니다.

Market/dimension 일부 누락은 해당 presentation field만 비우며, 건강한 Item ID를 identity failure로 승격하지 않는다.

## 11. Scanner Ground Truth / correction / regression

저장 root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

대표 evidence:

- full.png
- detail/title/processed ROI
- annotated image
- case.json
- raw OCR / user-substituted OCR / normalized matcher text
- Item ID / official name
- confidence / second score / margin
- matcher top candidates
- structural/header evidence
- mapped presentation
- user Ground Truth
- detector candidate identity/rank/score/geometry

Correction 기본 UX는 candidate-first다.

1. detail rectangle 후보
2. close-X 후보
3. magnifier 후보
4. item-name ROI 후보
5. 정답 item/text
6. 저장

후보에 정답이 없으면 manual rectangle을 직접 지정할 수 있다. Detector가 semantic object를 만들지 못한 사실 자체를 학습 데이터로 남기기 위해 `없음` 선택도 저장할 수 있다.

자동 diagnostic Case는 정답이 아니다. 사용자가 검토/교정한 Case만 Ground Truth로 취급한다.

Regression replay 결과는 `STILL_CORRECT`, `SOLVED`, `STILL_FAILING`, `REGRESSION`, `ERROR`로 분류한다. 기존 정상 reviewed Case가 새 코드에서 실패하면 평균 성능과 무관하게 regression이다.

## 12. Latency telemetry / optimization

v1.5.0은 threshold 완화 대신 stage latency를 계측한다.

측정 stage:

- capture
- rectangle-proposal
- semantic-header
- ocr-normal
- ocr-deep
- visual-recovery
- catalog-matching
- presentation
- end-to-end

현재 보수적 최적화:

- 같은 active scan cycle 안에서만
- width/height/BPP + exact pixel SHA-256이 같은 OCR bitmap만
- normal/deep cache를 분리해
- 이미 계산한 WinRT OCR 결과를 재사용

Frame 간 OCR cache는 없다. 다른 cycle/frame evidence를 과거 결과로 대체하지 않는다.

## 13. Continuous result stabilization

같은 상세창을 보고 있을 때 dark background/GPU pixel noise만으로 raw BGRA hash가 바뀌는 문제를 줄이기 위해 title-ink shape 기반 stable signature를 사용한다.

- dark background variation 무시
- 의미 없는 trailing title ROI width 무시
- visible title glyph shape 변화는 signature 변화
- signature 자체는 Item identity proof가 아님
- 이미 semantic gate를 통과한 trusted detail의 continuity 판단에만 사용
- 명확한 다른 title/geometry/identity evidence에서는 stale result 폐기
- detector miss는 기존 bounded miss policy를 유지

## 14. Diagnostics / log retention

사용자-reviewed Ground Truth는 자동 삭제하지 않는다.

자동 삭제 가능 대상은 다음 둘을 동시에 만족해야 한다.

```text
retention == automatic_sample
AND review_status == unreviewed
```

기본 bound:

- max age: 30 days
- max automatic cases: 300
- max automatic bytes: 512 MiB
- recent-case safety window: 2 hours

Corrupt/unknown metadata는 fail closed하여 보존한다. 삭제 직전 metadata를 다시 읽어 correction/delete race를 줄인다.

Scanner log와 startup log는 bounded rotation을 사용한다.

## 15. Scanner UI

일반 사용 surface:

- Scanner ON/OFF
- 1회 스캔
- 현재 결과 교정
- runtime status
- recent recognition history

`설정`:

- Scanner hotkeys
- OCR substitutions
- Mini Scanner 표시 설정

`고급 / 진단`:

- Display Test
- 인식 이미지
- regression
- Ground Truth export/manage
- Scanner catalog 강제 최신화/복구
- 로그 삭제
- diagnostic storage 정보

Mini Scanner는 context menu의 `현재 결과 교정`으로 최신 recognition debug snapshot을 곧바로 correction window에 전달한다.

## 16. 전체 UI 기준

v1.5.0 UI audit 대상:

- Main
- Quest
- Hideout
- Items
- Ammo
- Map / MiniMap
- Scanner
- Profile/settings/diagnostic dialogs

기능 축소나 불필요한 redesign보다 clipping, scroll, hierarchy, status wording, 일반/고급 surface 구분을 우선했다.

MainWindow의 과거 MinWidth 900은 실제 header와 Items 2-pane 최소 구조보다 작아 clipping 가능성이 있었으므로 1180으로 교정했다. 기본 폭은 1320을 유지한다.

Map/MiniMap은 이미 강한 product smoke를 가진 검증 subsystem이므로 v1.5.0에서 불필요하게 재설계하지 않았다.

## 17. Release / Program Update 계약

배포 형태:

```text
Windows x64
.NET 10 self-contained single-file
installer 없음
관리자 권한 불필요
```

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

Program Update:

```text
latest public stable 확인
→ strictly newer면 사용자 동의
→ exact Windows ZIP + SHA256SUMS
→ checksum/package 검증
→ program-owned files transaction 교체
→ 새 버전 재시작
```

Release gate:

- exact source build/test/publish/smoke
- exact tag/source identity
- draft asset redownload verification
- public stable/latest publication
- fresh independent anonymous public redownload
- SHA256SUMS/hash/size/layout/ProductVersion/FIRST_RUN 검증
- public-downloaded EXE Product UI/Map/Scanner smoke + graceful shutdown
- machine-readable durable release status
- one-shot release/verifier workflows cleanup

v1.5.0은 위 모든 항목을 통과했다.

## 18. v1.5.0 검증 요약

```text
final PR #172 release-candidate CI: 32688080850 — SUCCESS
296 tests / 0 failed / 0 skipped
exact product source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
release run: 32691423654 — SUCCESS
public verifier: 32691641614 — SUCCESS
asset: Junhyun-Helper-v1.5.0-win-x64.zip
bytes: 80,422,292
sha256: 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
ProductVersion: 1.5.0+6de738959740d12e6ccb81b65e50006e463eb699
public/latest: VERIFIED
exact tag source: VERIFIED
public redownload: VERIFIED
public package layout: VERIFIED
public EXE smoke: SUCCESS
```

Release blocker는 없다.

## 19. 현재 개발 방향

v1.5.0을 공식 제품 기준선으로 유지한다.

Scanner의 다음 개선은 실제 사용에서 수집된 reviewed Ground Truth를 기반으로 한다.

```text
real Tarkov usage
→ 정상 대표 표본 `맞음`
→ 미인식/오인식 직후 `현재 결과 교정`
→ reviewed Ground Truth 축적
→ diagnostics export / regression replay
→ 실패 stage 특정
→ 해당 stage만 수정
→ 전체 reviewed dataset replay
→ REGRESSION=0 확인
```

특히 계속 관찰할 영역:

- 다양한 해상도/DPI/UI scale의 semantic header geometry
- 짧은/희소 item name OCR
- r / 0 / slash-zero-like glyph / complex Hangul
- punctuation item names
- near-name ambiguity의 false positive 여부
- Item ID 성공 후 mapped market data completeness
- 빠른 item 전환에서 stale-result isolation
- 장시간 CPU/memory/UI responsiveness
- telemetry에서 확인되는 실제 OCR/visual recovery 병목

추가 evidence 없이 generic matcher confidence, top1/top2 margin, header floor, structural floor, candidate caps를 완화하지 않는다.
