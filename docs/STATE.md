# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 대화 기억이 아니라 저장소의 공식 문서, 코드, 테스트, GitHub 상태가 프로젝트의 기준입니다.

기준일: **2026-08-30 KST**  
상태: **v1.11.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

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
version: v1.11.0
exact product release source/tag target:
e0a8dd8acc86f8c5675efd0b24cb3006c19ccb1d
PR validated exact-head CI: 33298972004 — SUCCESS
exact-main CI: 33299138580 — SUCCESS
exact-main Shutdown Race CI: 33299138567 — SUCCESS
exact-main Documentation Consistency: 33299138569 — SUCCESS
release workflow: 33299258838 — SUCCESS
release id: 379210317
457 passed / 0 failed / 0 skipped
published UTC: 2026-08-30T07:28:08Z
```

Public release package:

```text
Junhyun-Helper.zip
asset id: 536298335
bytes: 80,550,542
SHA-256:
fb1d2f38ab26420d069fa8f0aab899c5e9776ffb072c83312e447289ef6f7c87

SHA256SUMS.txt
asset id: 536298334
bytes: 86
asset SHA-256:
277a5763796e0fc30f71ef959cb8a8ee18402a201c6042565a910368e70d89e8
```

Exact-main GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9728381122
archive bytes: 241,586,113
archive SHA-256:
e9f8ac2e6d0349f9b6b7a9856d7d5bae6f6af9f03a91934dacf8a5c8ad77623f
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.11.0`
- release target = `e0a8dd8acc86f8c5675efd0b24cb3006c19ccb1d`
- tag ref object = same exact product source
- draft = false
- prerelease = false
- latest stable = true
- `Junhyun-Helper.zip` + `SHA256SUMS.txt` present

공식 공개 증거:

- `docs/RELEASE_1.11.0.md`
- `docs/.release-v1.11.0-status.json`
- `docs/RELEASE_NOTES_V1.11.0.md`

**중요:** 이 release 이후 생성되는 documentation-only 또는 후속 maintenance commit은 v1.11.0 product release source가 아니다. 공개 source/tag/assets는 위 exact product source를 immutable historical identity로 사용한다.

## 3. v1.11.0 Map / MiniMap 유지보수

### 3.1 첫 MiniMap activation 최신 지도 replay

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

### 3.2 Extract checkbox late-load

실사용에서 지도 marker 설정의 탈출구 checkbox가 일시적으로 사라졌다가 후속 실행에서 정상화된 증상이 있었다.

코드 조사 결과 concrete lifecycle defect를 확인했다.

- product marker-settings bridge가 donor Extract controls보다 먼저 초기화될 수 있다.
- 기존 late retry는 일반 marker row를 다시 다뤘지만 Extract row가 늦게 생긴 경우를 완전히 복구하지 못했다.

수정:

- `TryMoveExtractRows()` readiness를 retry 조건에 포함한다.
- donor controls가 늦게 생기면 bounded retry한다.
- 이미 이동된 row는 idempotent하게 유지한다.
- MiniMap extract projection이 비어 있는 경우 현재 extract presentation을 강제 재동기화한다.

### 3.3 Player Marker Size 변경 뒤 표시 설정 보존

Root cause:

- Player Marker Size 변경 경로가 donor `UpdateMapView()`를 호출한다.
- donor가 marker visual tree/container transform을 다시 만들면서 Junhyun MiniMap의 custom Marker Size / Name Size 등 실제 렌더 projection을 덮어쓸 수 있었다.
- settings 값 자체는 유지되므로 UI에는 custom value가 남아 있지만 실제 표시만 초기값처럼 보일 수 있었다.

수정:

- donor rebuild 직후 현재 Junhyun marker presentation 전체를 다시 적용한다.
- marker scale
- name scale
- name visibility
- hidden categories
- additional marker scale
- edge label presentation

### 3.4 marker blink → 전체 소실

실사용 증상과 일치하는 donor lifecycle race를 확인했다.

- refresh가 marker containers를 먼저 clear한다.
- 다른 refresh가 들어오면 기존 작업이 cancellation될 수 있다.
- clear 이후 cancellation이 연쇄되면 standard marker layer가 0개인 상태가 노출/잔존할 수 있다.

제품 레이어 복구:

- same map/floor에서 직전에 stable standard markers가 있었는지 기억한다.
- marker layer가 0개인 상태가 연속 관찰될 때만 판단한다.
- 사용자가 모든 marker를 명시적으로 숨긴 상태가 아니라면 one-shot `QueueMarkerRefresh()`를 수행한다.
- 무한 retry 또는 deliberate hidden state 복구는 하지 않는다.

## 4. Scanner v1.11.0

### 4.1 flea minimum 사용자 표시 제거

`플리마켓 최저가`는 사용자 presentation에서 제거했다.

호환성 때문에 다음은 유지한다.

- `FleaMinimumPrice` source/model/cache 값
- legacy Scanner display settings compatibility field

해당 값은 Scanner recognition proof에 사용하지 않으며 scan-time network I/O도 추가하지 않는다.

### 4.2 `교정 데이터 추가` global hotkey

Scanner display settings schema는 v8이다.

기본 hotkey:

```text
Ctrl+Alt+F9
```

동작:

1. `ScannerRecognitionDebugStore`의 최신 exact evidence snapshot 확인
2. evidence 없음 → `저장할 스캔 결과가 없습니다.` 표시, Case 생성 없음
3. evidence 있음 → explicit save 전용 새 Case ID 부여
4. 기존 `ScannerDiagnosticDataset.SaveCorrectionAsync`로 Saved Case 저장
5. `GroundTruthItemName = null`
6. `UserConfirmed = false`
7. 기존 Saved Case manager 열기
8. manager 종료 뒤 Scanner section으로 복귀

완전한 인식 결과뿐 아니라 불완전/미인식 evidence도 저장할 수 있다. 같은 latest evidence를 여러 번 explicit save하는 것도 허용한다.

Hotkey는 Ground Truth를 추측·생성·자동 확정하지 않는다. Ground Truth ownership은 기존 Saved Case review UI에 남는다.

Evidence schema가 허용하는 범위에서 다음을 보존한다.

- capture image
- capture origin / selected/title/magnifier/close geometry
- OCR raw text
- user-substituted OCR text
- matcher text
- recognition result / reason / confidence
- candidate list / scores
- timestamp / capture mode / Case ID

## 5. Hideout FIR / Needed Items / cleanup

v1.11.0은 Hideout item requirement FIR semantics를 source 그대로 반영한다.

Tarkov source requirement에서 FIR 의미는 `attributes.foundInRaid`에 존재한다. top-level 필드만 읽으면 실제 FIR requirement가 non-FIR처럼 canonicalize될 수 있었다.

수정:

- importer가 `attributes.foundInRaid`를 canonical requirement에 보존한다.
- FIR requirement에는 non-FIR inventory가 충당되지 않는다.
- 같은 item의 non-FIR copy는 다른 requirement가 없다면 cleanup 후보가 될 수 있다.
- Quest FIR / Hideout FIR을 임의의 하드코딩 규칙으로 구분하지 않는다.
- Game Content가 바뀌면 Needed Items / cleanup derived state는 현재 canonical requirement를 기준으로 계산한다.

## 6. Ammo pickup evaluator

독립 domain evaluator가 same-caliber penetration ranking과 현재 프로필의 구매 가능 상태를 사용한다.

### 직접 구매 가능 판정

구매 가능으로 인정:

- offer Trader LL <= 현재 해당 Trader LL
- 현금 direct purchase
- quest unlock이 필요한 경우 완료 quest가 현재 profile에 실제로 기록됨

구매 가능으로 인정하지 않음:

- barter
- craft
- flea market
- 현재보다 높은 Trader LL
- 완료 여부를 확인할 수 없는 quest unlock

### ranking 의미

현재 직접 구매 가능한 탄약보다 penetration이 낮은 중간 탄은 pickup 우선 대상에서 제외한다. 구매 가능한 범위를 넘어서는 높은 penetration 탄은 pickup 대상으로 유지한다.

동일 penetration tie는 deterministic하게 처리한다.

해당 caliber에 현재 직접 구매 가능한 탄약이 하나도 없으면 unavailable ammo는 모두 pickup 후보로 취급한다.

사용자가 제시한 대표 예제를 deterministic test로 고정했다.

```text
rank 1 2 3 4 5 / buyable 2,4
=> pickup 1 / not 3 / pickup 5

rank 1 2 3 4 5 6 7 / buyable 3,5,6
=> pickup 1,2 / not 4 / pickup 7
```

## 7. Ammo Pack → contained canonical ammo

Scanner가 Ammo Pack을 인식하면 pack 자체가 아니라 contained canonical ammo를 기준으로 pickup 판단한다.

Resolution priority:

1. authoritative `containsItems` relation
2. authoritative relation이 빈 경우에만 narrow name fallback
3. non-empty authoritative relation이 혼합/모호하면 name heuristic으로 덮어쓰지 않음

이를 통해 같은 탄약의 서로 다른 pack size도 실제 내부 탄약의 caliber/penetration/purchase availability를 공유한다.

## 8. Scanner 안전 경계

Scanner architecture는 계속 external screen pixels + OCR이다.

금지/미사용:

- game process memory read
- code/DLL injection
- game/process hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

이 경계는 제품 요구사항이자 유지보수 계약이다.

## 9. Schema / compatibility

```text
Desktop target version: 1.11.0
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v8
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

```text
v1.10.1 → v1.11.0 mandatory Game Content schema migration: none
v1.10.1 → v1.11.0 user.db migration: none
```

## 10. Architecture / ownership

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

- Core: canonical domain, deterministic calculations/policies.
- Application: user use cases, workspaces, authoritative mutation orchestration.
- Infrastructure: HTTP/source parsing, persistence, content/update I/O.
- Desktop: WPF UI, Scanner capture/OCR/runtime/diagnostics, Map product bridge.
- donor Map/MiniMap: limited compile-link exception; donor updater/content ownership은 사용하지 않는다.

Pinned donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

## 11. 검증

v1.11.0 exact product source는 다음을 통과했다.

### PR exact-head

```text
CI: 33298972004 — SUCCESS
Shutdown Race CI: 33298971995 — SUCCESS
Documentation Consistency: 33298971996 — SUCCESS
```

### exact-main

```text
CI: 33299138580 — SUCCESS
Shutdown Race CI: 33299138567 — SUCCESS
Documentation Consistency: 33299138569 — SUCCESS
```

CI coverage:

- 457 automated tests
- Windows Release build
- Windows x64 publish
- actual published EXE startup
- Product UI smoke
- Ammo smoke
- Map / Factory / MiniMap smoke
- Scanner smoke
- graceful shutdown
- release package root/dependency/checksum verification
- artifact upload

Dedicated Shutdown Race CI:

```text
active async product smoke 진행 중
→ 정상 Main Window close
→ bounded time 내 exit
→ exit code 0
→ unhandled/startup Map diagnostic 없음
```

Release workflow `33299258838`은 exact-main CI artifact를 다운로드해 v1.11.0을 게시했고 public release/tag/assets readback까지 성공했다.

## 12. 유지보수 원칙

- 사용자에게 보고된 실제 실사용 증상을 우선 회귀 증거로 취급한다.
- 현재 코드가 존재한다는 이유만으로 올바른 제품 설계라고 가정하지 않는다.
- 반대로 실제 결함 증거 없이 대규모 리팩터링하지 않는다.
- user-visible WPF lifecycle 변경은 source assertion만으로 완료 선언하지 않는다.
- external Tarkov data 의미 변경은 importer/canonical/derived state까지 추적한다.
- release는 PR CI → main merge → exact-main CI → release source/tag/assets 동일성까지 확인한다.

## 13. 사용자 실사용 상태

v1.11.0은 자동화, Windows published EXE, package, public release 검증까지 완료했다.

사용자의 실제 PC/Tarkov 플레이 환경에서의 v1.11.0 실사용 검증은 현재 **PENDING**이다.

## 14. 현재 남은 작업

v1.11.0 제품 구현/검증/공개 작업은 완료됐다. 공식 release finalization 문서가 main에 병합되고 `docs/ACTIVE_WORK.md`가 `NONE`이 되면 이 개발 배치는 완전히 종료된다.

이후 기본 작업 방향은 유지보수, 실사용 회귀 수정, Tarkov 변화 대응이다.
