# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 대화 기억이 아니라 저장소의 공식 문서, 코드, 테스트, GitHub 상태가 프로젝트의 기준입니다.

기준일: **2026-08-30 KST**  
상태: **v1.11.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태이며 기본 운영 모드는 유지보수다.

주요 기능:

- GameMode별 Profile / User Progress
- Quest / Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger / cleanup
- Items / cross-navigation
- Ammo / favorites / profile-aware pickup 판단
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- 사용자 동의형 Program Update
- Scanner + Mini Scanner
- Scanner Saved Case / Ground Truth / diagnostics / regression dataset
- Scanner 아이템 정보 DB
- Scanner Favorites / Recents

Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper` 저장소는 준현 헬퍼의 제품 사양 권위가 아니다. Map/MiniMap에 한해 검증된 pinned donor source를 제한적으로 compile-link한다.

## 2. 현재 public stable

```text
version: v1.11.1
exact product release source/tag target:
6314eaf866539747eadd69f8da4450bd8d5939e1
PR: #229 — MERGED
PR validated exact-head CI: 33302240850 — SUCCESS
exact-main CI: 33302387606 — SUCCESS
exact-main Shutdown Race CI: 33302387623 — SUCCESS
exact-main Documentation Consistency: 33302387611 — SUCCESS
release workflow: 33302514984 — SUCCESS
release id: 379226665
460 passed / 0 failed / 0 skipped
published UTC: 2026-08-30T08:49:26Z
```

Public release package:

```text
Junhyun-Helper.zip
asset id: 536370979
bytes: 80,553,167
SHA-256:
0480dca11f93472cee1396d5faae9362a8b04398a6c18bfd163dc84b9aef4e1b

SHA256SUMS.txt
asset id: 536370978
bytes: 86
asset SHA-256:
233dfca51bc7d280093da728cb76374e0f10b310e127f43139a5177d55a85b20
```

Exact-main GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9729389953
archive bytes: 241,592,817
archive SHA-256:
770d89c56f39e379438702dbfb3f15ff0b681a1cd6794503fa1d45eece5061da
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.11.1`
- release target = `6314eaf866539747eadd69f8da4450bd8d5939e1`
- tag ref object = same exact product source
- draft = false
- prerelease = false
- latest stable = true
- `Junhyun-Helper.zip` + `SHA256SUMS.txt` present

Release workflow는 exact-main CI artifact를 다시 빌드하지 않고 다운로드해 EXE ProductVersion, `FIRST_RUN_KO.txt`, package checksum을 확인한 뒤 stable release를 공개했다.

공식 공개 증거:

- `docs/RELEASE_1.11.1.md`
- `docs/.release-v1.11.1-status.json`
- `docs/RELEASE_NOTES_V1.11.1.md`

**중요:** 이후 documentation-only 또는 후속 maintenance commit은 v1.11.1 product release source가 아니다. 공개 source/tag/assets는 위 exact product source를 immutable historical identity로 사용한다.

## 3. v1.11.1 PATCH 유지보수

v1.11.1은 v1.11.0 실사용 직후 확인된 사용자 조작/피드백 회귀를 수정한다. 새 OCR 알고리즘이나 새로운 Tarkov 데이터 의미는 추가하지 않았다.

### 3.1 Scanner 탄약 판단 표시 설정

v1.11.0에서 탄약 pickup 판단은 Mini Scanner에 렌더됐지만 정보 순서 밖의 고정 마지막 줄이어서 Scanner 설정에서 표시/숨김과 순서 변경이 불가능했다.

v1.11.1:

- display field `ammo_pickup` 추가
- 사용자 표시명 `탄약 줍기 판단`
- 표시/숨김 지원
- Mini Scanner 정보 순서 변경/저장 지원
- 기존 v8 사용자는 현재 보이던 동작을 유지하도록 visible 기본 migration
- Scanner display settings schema v9

`플리마켓 최저가` compatibility 데이터/설정 필드는 유지하지만 사용자 표시와 정상 설정 목록에는 노출하지 않는다.

### 3.2 Items / Hideout 검색 clear

Items와 Hideout 검색창에 다른 검색 UI와 동일한 `×` clear 동작을 추가했다.

- 현재 검색어 즉시 삭제
- 기존 TextBox / `TextChanged` 검색·필터 계약 유지
- clear 뒤 검색창 keyboard focus 복구
- 기존 검색 ownership을 바꾸지 않음

### 3.3 `교정 데이터 추가` 저장 성공 피드백

전역 단축키로 Saved Case 저장에 성공하면 Mini Scanner에 정확히 `저장 완료`를 약 2초 표시한다.

- 기존 item snapshot은 교체하거나 지우지 않음
- Mini Scanner가 닫혀 있으면 status-only card로 잠시 표시
- no-evidence exact status `저장할 스캔 결과가 없습니다.` 유지
- evidence-only Saved Case 유지
- Ground Truth 자동 생성/추측 금지 유지
- duplicate explicit save 허용 유지

### 3.4 회귀 gate 보강

v1.11.0 published EXE smoke에는 Scanner settings row 자체를 실제 WPF control로 검사하는 경로가 없었다. v1.11.1에서 다음을 runtime smoke에 포함했다.

- Scanner settings `탄약 줍기 판단` row 존재/동작
- ammo pickup 표시 순서 변경
- ammo pickup 숨김
- Items 검색 `×` clear
- Hideout 검색 `×` clear
- Mini Scanner `저장 완료` transient status

RC 검증 중 기존 startup smoke가 Scanner schema `8`을 hard-code해 v9 제품을 시작 전에 차단하는 stale test defect도 발견했다. 제품 설정 구현 문제가 아니라 오래된 검증 기대값이었으며, schema v9와 ammo pickup order/visibility를 직접 확인하도록 수정한 뒤 전체 PR/exact-main gate가 통과했다.

## 4. v1.11.0에서 유지되는 Map / MiniMap 기반

### 4.1 첫 MiniMap activation 최신 지도 replay

실사용 증상:

```text
Main Map A
→ B로 변경
→ MiniMap 첫 ON
→ 첫 화면은 과거 A
→ 이후 selection 변화부터 정상 동기화
```

Root cause:

- MiniMap window가 없을 때 product registry가 desired map key를 보존하지 않았다.
- 첫 overlay 생성 과정에서는 donor persisted selection이 먼저 살아날 수 있었다.
- window registration 뒤 최신 Main Map B를 replay하는 보장이 없었다.

수정:

- `JunhyunMiniMapProductRegistry`가 최신 map key snapshot을 window 유무와 무관하게 보존한다.
- `Register`에서 snapshot을 lock 밖에서 새 window에 replay한다.
- `Unregister`는 최신 desired map key를 지우지 않는다.
- selection persistence ownership은 기존 계약대로 main product가 유지한다.

회귀 계약:

- window 없음 상태에서 B sync → first register가 B 수신
- active 상태 C sync
- unregister/new register → C replay

### 4.2 Extract checkbox late-load

실사용에서 지도 marker 설정의 탈출구 checkbox가 일시적으로 사라졌다가 후속 실행에서 정상화된 증상이 있었다.

확인된 lifecycle defect:

- product marker-settings bridge가 donor Extract controls보다 먼저 초기화될 수 있다.
- 기존 late retry는 일반 marker row를 다시 다뤘지만 Extract row가 늦게 생긴 경우를 완전히 복구하지 못했다.

수정:

- `TryMoveExtractRows()` readiness를 retry 조건에 포함
- donor controls가 늦게 생기면 bounded retry
- 이미 이동된 row는 idempotent 유지
- MiniMap extract projection이 비어 있으면 현재 extract presentation 재동기화

### 4.3 Player Marker Size 변경 뒤 표시 설정 보존

Player Marker Size 변경 경로의 donor `UpdateMapView()`가 marker visual tree/container transform을 다시 만들면서 MiniMap Marker Size / Name Size 등 실제 Junhyun presentation을 덮어쓸 수 있었다.

수정 후 donor rebuild 직후 현재 presentation 전체를 재적용한다.

- marker scale
- name scale
- name visibility
- hidden categories
- additional marker scale
- edge label presentation

### 4.4 marker blink → 전체 소실

확인된 donor lifecycle race:

- refresh가 marker containers를 먼저 clear
- 다른 refresh 진입으로 이전 작업 cancel 가능
- clear 뒤 cancellation이 연쇄되면 standard marker layer 0개 상태가 남을 수 있음

제품 레이어 복구:

- same map/floor에서 직전 stable standard markers 존재 여부 기억
- marker layer 0개 상태가 연속 관찰될 때만 판단
- 사용자가 모든 marker를 명시적으로 숨긴 상태가 아니면 one-shot `QueueMarkerRefresh()`
- 무한 retry 또는 deliberate hidden state 복구 금지

## 5. Scanner 기반 계약

### 5.1 flea minimum 사용자 표시 제거

`플리마켓 최저가`는 사용자 presentation에서 제거됐다. 호환성을 위해 `FleaMinimumPrice` source/model/cache 값과 legacy settings compatibility field는 유지한다.

해당 값은 Scanner recognition proof에 사용하지 않으며 scan-time network I/O도 추가하지 않는다.

### 5.2 `교정 데이터 추가` global hotkey

기본 hotkey:

```text
Ctrl+Alt+F9
```

동작 계약:

1. 최신 exact Scanner evidence snapshot 확인
2. evidence 없음 → `저장할 스캔 결과가 없습니다.` / Case 생성 없음
3. evidence 있음 → explicit save 전용 새 Case ID
4. 기존 correction dataset 경로로 Saved Case 저장
5. `GroundTruthItemName = null`
6. `UserConfirmed = false`
7. Saved Case manager 연동
8. Scanner UI 복귀

완전한 인식 결과뿐 아니라 불완전/미인식 evidence도 저장할 수 있다. 동일 최신 evidence의 반복 explicit save도 허용한다.

Hotkey는 Ground Truth를 추측·생성·자동 확정하지 않는다. Ground Truth ownership은 review UI에 남는다.

Evidence schema가 허용하는 범위에서 capture image/geometry, OCR raw/substituted/matcher text, recognition reason/confidence, candidate scores, timestamp/capture mode/Case ID를 보존한다.

## 6. Hideout FIR / Needed Items / cleanup

Hideout item requirement FIR semantics는 source 그대로 canonical content에 반영한다.

Tarkov source requirement의 FIR 의미는 `attributes.foundInRaid`에 존재한다.

- importer가 `attributes.foundInRaid`를 canonical requirement에 보존
- FIR requirement에는 non-FIR inventory가 충당되지 않음
- 같은 item의 non-FIR copy는 다른 requirement가 없다면 cleanup 후보 가능
- Quest FIR / Hideout FIR을 임의 하드코딩 규칙으로 분리하지 않음
- Game Content 변경 시 Needed Items / cleanup derived state는 현재 canonical requirement 기준 재계산

## 7. Ammo pickup evaluator

독립 domain evaluator가 same-caliber penetration ranking과 현재 프로필 구매 가능 상태를 사용한다.

직접 구매 가능으로 인정:

- offer Trader LL <= 현재 해당 Trader LL
- 현금 direct purchase
- quest unlock 필요 시 완료 quest가 현재 profile에 실제 기록됨

구매 가능으로 인정하지 않음:

- barter
- craft
- flea market
- 현재보다 높은 Trader LL
- 완료 여부를 확인할 수 없는 quest unlock

ranking 계약:

- 현재 직접 구매 가능한 penetration band 내부의 unavailable 중간 탄은 pickup 우선 대상에서 제외
- band를 벗어난 높은 penetration 탄은 pickup 대상
- 동일 penetration tie deterministic
- 해당 caliber에 직접 구매 가능한 탄이 하나도 없으면 unavailable ammo는 모두 pickup 후보

고정 예제:

```text
rank 1 2 3 4 5 / buyable 2,4
=> pickup 1 / not 3 / pickup 5

rank 1 2 3 4 5 6 7 / buyable 3,5,6
=> pickup 1,2 / not 4 / pickup 7
```

## 8. Ammo Pack → contained canonical ammo

Scanner가 Ammo Pack을 인식하면 pack 자체가 아니라 contained canonical ammo를 기준으로 pickup 판단한다.

Resolution priority:

1. authoritative `containsItems` relation
2. authoritative relation이 빈 경우에만 narrow name fallback
3. non-empty authoritative relation이 혼합/모호하면 name heuristic으로 덮어쓰지 않음

같은 탄약의 서로 다른 pack size도 실제 내부 탄약의 caliber/penetration/purchase availability를 공유한다.

## 9. Scanner 안전 경계

Scanner architecture는 external screen pixels + OCR만 사용한다.

금지/미사용:

- game process memory read
- code/DLL injection
- game/process hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

이 경계는 제품 요구사항이자 유지보수 계약이다.

## 10. Schema / compatibility

```text
Desktop target version: 1.11.1
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

```text
v1.11.0 → v1.11.1 mandatory Game Content schema migration: none
v1.11.0 → v1.11.1 user.db migration: none
v1.11.0 → v1.11.1 Scanner display settings: v8 → v9 automatic normalize
```

## 11. Architecture / ownership

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

- Core: canonical domain, deterministic calculations/policies
- Application: user use cases, workspaces, authoritative mutation orchestration
- Infrastructure: HTTP/source parsing, persistence, content/update I/O
- Desktop: WPF UI, Scanner capture/OCR/runtime/diagnostics, Map product bridge
- donor Map/MiniMap: limited compile-link exception; donor updater/content ownership은 사용하지 않음

Pinned donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

## 12. v1.11.1 검증

### PR exact-head

최종 non-draft PR #229 head는 모든 gate를 통과했다.

```text
CI: 33302240850 — SUCCESS
Shutdown Race CI: 33302240847 — SUCCESS
Documentation Consistency: 33302240842 — SUCCESS
```

### exact-main product source

```text
source: 6314eaf866539747eadd69f8da4450bd8d5939e1
CI: 33302387606 — SUCCESS
Shutdown Race CI: 33302387623 — SUCCESS
Documentation Consistency: 33302387611 — SUCCESS
Release workflow: 33302514984 — SUCCESS
```

CI coverage:

- 460 deterministic automated tests
- Windows Release build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI smoke
- Ammo smoke
- Map / Factory / MiniMap smoke
- Scanner smoke
- v1.11.1 Scanner settings/search/save-feedback runtime smoke
- graceful shutdown
- active async shutdown race
- release package root/dependency/checksum verification
- artifact upload

Dedicated Shutdown Race:

```text
active async product smoke 진행 중
→ 정상 Main Window close
→ bounded time 내 exit
→ exit code 0
→ unhandled/startup Map diagnostic 없음
```

Release workflow `33302514984`은 exact-main CI artifact `9729389953`을 다운로드해 v1.11.1을 공개했다. `/releases/latest`, release tag target, lightweight tag ref, public assets를 readback해 exact product source와 일치함을 확인했다.

## 13. 유지보수 원칙

- 사용자에게 보고된 실제 실사용 증상을 우선 회귀 증거로 취급한다.
- 현재 코드가 존재한다는 이유만으로 올바른 제품 설계라고 가정하지 않는다.
- 실제 결함 증거 없이 대규모 리팩터링하지 않는다.
- user-visible WPF lifecycle 변경은 source assertion만으로 완료 선언하지 않는다.
- external Tarkov data 의미 변경은 importer/canonical/derived state까지 추적한다.
- release는 PR CI → main merge → exact-main CI → release source/tag/assets 동일성까지 확인한다.

## 14. 사용자 실사용 상태

v1.11.1은 deterministic automation, Windows published EXE, package, exact-main CI, public release 검증까지 완료했다.

사용자의 실제 PC/Tarkov 플레이 환경에서의 v1.11.1 실사용 검증은 자동 release verification과 별개이며 현재 **PENDING**이다.

## 15. 현재 남은 작업

v1.11.1 제품 구현/검증/공개 작업은 완료됐다. release finalization 문서를 main에 병합하고 `docs/ACTIVE_WORK.md`를 `NONE`으로 닫는 문서-only 마감만 수행한다.

이후 기본 작업 방향은 유지보수, 실사용 회귀 수정, Tarkov 변화 대응이다. 제품 요구사항 변경은 사용자의 명시적 결정이 있을 때 시작한다.
