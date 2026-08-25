# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-25
상태: **v1.7.2 PUBLIC RELEASE / VERIFIED — LIVE GROUND TRUTH MAINTENANCE**

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

## 2. 공개 stable과 exact source

현재 public stable/latest는 **v1.7.2**다.

```text
exact release source/tag: 8775feba23a2c9ecc6326626527cdfd54f4f0414
stable asset: Junhyun-Helper.zip
stable bytes: 80,444,391
stable SHA-256: 81d8e6a82db0f4b33ebbdd2bf7f455c1d92ffc2f8b6015f6ba6190e616be1fc0
main CI run: 32842508995
release workflow run: 32842783940
362 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Scanner / Mini Scanner / Map / Factory / MiniMap smoke: SUCCESS
public/latest: v1.7.2 / VERIFIED
```

GitHub public release asset metadata의 `Junhyun-Helper.zip` digest와 size를 다시 읽어 Release runner가 업로드 직전에 계산한 hash/size와 동일함을 확인했다.

현재 `main`은 public v1.7.2 exact source 이후의 release-record/housekeeping commit을 포함할 수 있다. 공개 v1.7.2 제품 source의 권위는 tag `v1.7.2`와 위 exact source SHA다.

Schema / compatibility:

```text
Desktop target version: 1.7.2
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
Scanner Ground Truth dataset: local diagnostics persistence
```

v1.7.2 공식 문서:

- `docs/DECISION_V1.7.2_MINI_SCANNER_STABILITY_2026-08-25.md`
- `docs/RELEASE_NOTES_V1.7.2.md`
- `docs/RELEASE_1.7.2.md`
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

- **Core**: canonical domain, deterministic calculation, Quest 조건 규칙, Scanner structural/identity/matcher 및 presentation retention 정책
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
→ validate/build candidate
→ integrity validation
→ general content activation
→ Scanner official item/market catalog refresh
→ status report
```

안전 계약:

- fail closed / Last Known Good 유지
- partial payload 보호
- canonical dangling reference 검증 유지
- objective-specific selector semantics만 필요한 경우 좁게 예외 처리
- Scanner refresh만 실패하면 healthy general Game Content를 rollback하지 않음

현재 일반 Scanner 화면에는 catalog force-refresh action을 노출하지 않는다.

## 6. Quest availability / live-data audit

`확인 필요`는 숨겨야 할 UI 노이즈가 아니라 안전하게 판정할 수 없는 조건을 나타낸다.

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
- continuous scan interval `350 ms`
- semantic retry interval `1200 ms`
- current official Korean Tarkov full-item catalog가 identity authority
- ambiguity / low confidence는 fail closed
- production OCR field는 item-name 하나
- price/flea/slots/needed는 Item ID 이후 mapped data
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지
- product-default automatic global forced substitution 금지
- cross-frame OCR cache를 Item identity proof로 사용 금지
- reviewed Ground Truth 없이 threshold/cap 완화 금지

v1.7.2에서 위 threshold/candidate cap/cadence는 변경하지 않았다.

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

One-shot 기능:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

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

현재 Scanner item search는 같은 current full-item catalog와 local presentation data를 사용하며 검색 순간 network request를 만들지 않는다.

## 11. Scanner 일반 UI

상단 primary actions:

- `스캐너 ON/OFF`
- `설정`
- `고급`

하단:

- 왼쪽 `아이템 검색`
- 오른쪽 `Scanner 로그`

`설정`은 hotkey와 Mini Scanner display/order를 우선한다.
`고급`은 Display Test, current-result correction, correction dataset management 같은 실사용 진단 흐름을 우선한다.

## 12. Mini Scanner / presentation retention

항상 표시하는 identity header:

- item icon
- official item name

사용자가 표시 여부와 순서를 저장하는 field:

1. trader sell price
2. flea average price
3. trader price/slot
4. flea price/slot
5. current needed

Mini Scanner window safety:

- Topmost
- ShowActivated=false
- ShowInTaskbar=false
- full-surface drag
- matched Item presentation only
- stale epoch reject

v1.7.2 sticky presentation 계약:

```text
No Item
  └─ A 확정 → Show A

Show A
  ├─ A 재확정 → A 유지 / presentation miss budget reset
  ├─ B 확정 → 즉시 B로 교체 / budget reset
  ├─ 실제 식별 miss #1 → A 유지
  ├─ 실제 식별 miss #2 → A 유지
  └─ 실제 식별 miss #3 → Hide
```

- candidate 안정화 / title 변화 확인 / OCR 진행은 miss로 집계하지 않음
- runtime 내부 verified identity 재탐색 기준 `MissesToHide = 2`는 그대로 유지
- Mini Scanner presentation만 별도 3-miss retention 사용
- inventory/stash context OCR은 hidden overlay의 **initial entry gate**
- 이미 visible인 확정 Item을 단발 auxiliary context-OCR false/exception으로 숨기지 않음
- Scanner OFF / suspend / profile·catalog·vision unavailable / fatal error / dispose는 즉시 hard hide

Reference: `docs/DECISION_V1.7.2_MINI_SCANNER_STABILITY_2026-08-25.md`

## 13. Ground Truth / correction

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

자동 diagnostic Case는 정답이 아니다. user-reviewed Case만 Ground Truth다.

Replay result:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

기존 정상 reviewed Case가 실패하면 평균 성능과 무관하게 REGRESSION이다.

## 14. Latency / diagnostics retention

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

성능 최적화는 telemetry와 live evidence를 바탕으로 별도 수행한다. v1.7.2 표시 안정성 수정에서 recognition threshold/candidate cap/cadence를 성능 목적으로 변경하지 않았다.

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

stable user package:

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
- release asset hash/size metadata readback
- durable release status 기록

## 16. v1.7.2 검증 현황

PR #180 final CI:

```text
build: SUCCESS
362 / 362 tests: SUCCESS
Windows x64 publish: SUCCESS
Product UI / Scanner / Mini Scanner smoke: SUCCESS
Map / Factory / MiniMap smoke: SUCCESS
package verification: SUCCESS
```

main exact source CI:

```text
run: 32842508995
source: 8775feba23a2c9ecc6326626527cdfd54f4f0414
362 / 362 tests: SUCCESS
Windows x64 publish: SUCCESS
Product UI / Scanner / Mini Scanner / Map smoke: SUCCESS
package verification: SUCCESS
```

release:

```text
workflow run: 32842783940
tag/latest: v1.7.2
asset: Junhyun-Helper.zip
bytes: 80,444,391
SHA-256: 81d8e6a82db0f4b33ebbdd2bf7f455c1d92ffc2f8b6015f6ba6190e616be1fc0
public metadata readback: MATCH
```

## 17. 현재 개발 방향

v1.7.2는 공개 stable 완료 상태다.

다음 Scanner 개선은 추측 기반 threshold 완화가 아니라 다음 순서를 따른다.

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

Mini Scanner 표시 안정성 문제는 v1.7.2에서 recognition과 presentation life-cycle을 분리해 해결했다. 이후 사용성 이슈는 Item identity 정확도 문제와 presentation 문제를 구분해서 진단한다.
