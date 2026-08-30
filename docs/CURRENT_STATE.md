# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`, 상세 설계와 이력은 `docs/STATE.md`, `docs/PRODUCT.md`, `docs/DECISIONS.md`, 전문 결정/릴리즈 문서를 참조합니다.

기준일: **2026-08-30 KST**

상태: **`v1.11.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

```text
public stable/latest: v1.11.1
exact product release source/tag target:
6314eaf866539747eadd69f8da4450bd8d5939e1
PR: #229 — MERGED
PR validated exact-head CI: 33302240850 — SUCCESS
exact-main CI: 33302387606 — SUCCESS
exact-main Shutdown Race CI: 33302387623 — SUCCESS
exact-main Documentation Consistency: 33302387611 — SUCCESS
release workflow: 33302514984 — SUCCESS
release id: 379226665
published UTC: 2026-08-30T08:49:26Z
460 passed / 0 failed / 0 skipped
```

Public package:

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

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9729389953
archive bytes: 241,592,817
archive SHA-256:
770d89c56f39e379438702dbfb3f15ff0b681a1cd6794503fa1d45eece5061da
```

GitHub `/releases/latest`와 `refs/tags/v1.11.1` readback에서 release target/tag ref가 exact product source와 일치하고 `draft=false`, `prerelease=false`, latest stable임을 확인했다. Release workflow는 exact-main CI artifact의 EXE ProductVersion, `FIRST_RUN_KO.txt`, ZIP checksum manifest를 검증한 뒤 공개했다.

공개 증거:

- `docs/RELEASE_1.11.1.md`
- `docs/.release-v1.11.1-status.json`
- `docs/RELEASE_NOTES_V1.11.1.md`

## v1.11.1 핵심 변경

### Scanner 탄약 판단 설정

- v1.11.0의 기존 ammo pickup 결과를 Scanner 설정의 `탄약 줍기 판단` 항목으로 승격했다.
- Mini Scanner에서 표시/숨김 및 정보 순서를 사용자 설정으로 관리한다.
- Scanner display settings schema는 **v9**이다.
- 기존 v8 설정은 ammo pickup을 visible 상태로 유지하며 v9로 normalize된다.

### Items / Hideout 검색 clear

- Items와 Hideout 검색창에 `×` clear 버튼을 추가했다.
- 기존 TextBox `TextChanged` 검색/필터 계약을 그대로 사용한다.
- clear 후 검색창 focus를 복구한다.

### 교정 데이터 저장 성공 피드백

- `교정 데이터 추가` hotkey Saved Case 저장 성공 시 Mini Scanner에 정확히 `저장 완료`를 약 2초간 표시한다.
- 현재 Mini Scanner item snapshot은 교체하지 않는다.
- Mini Scanner가 닫힌 상태에서도 짧은 status-only card로 성공 여부를 확인할 수 있다.
- evidence-only Saved Case / no automatic Ground Truth / duplicate explicit save 계약은 변경하지 않았다.

### 회귀 gate 강화

- actual published EXE smoke가 Scanner settings `탄약 줍기 판단` row를 직접 검사한다.
- Items / Hideout `×` 버튼이 실제 query를 clear하는지 검사한다.
- Mini Scanner `저장 완료` status-only render를 검사한다.
- startup product contract가 schema v9와 ammo pickup의 실제 order/visibility를 직접 검사한다.

RC 중 오래된 startup smoke가 schema v8을 hard-code하고 있어 EXE startup/Shutdown Race를 차단한 사실을 확인했다. 제품 settings 구현 문제가 아니라 stale gate였으며 v9로 갱신하고 ammo pickup order/visibility 검증을 추가한 뒤 PR 및 exact-main 전체 gate가 통과했다.

## v1.11.0에서 유지되는 기반 변경

### Map / MiniMap

- MiniMap window가 아직 없어도 최신 Main Map selection을 registry에 보존하고 first register 시 replay한다.
- 늦게 생성되는 donor Extract checkbox를 bounded retry해 marker 설정 UI에 반영한다.
- Player Marker Size 등 donor visual rebuild 후 현재 MiniMap marker/name/category/visibility/edge-label presentation을 다시 projection한다.
- confirmed donor marker refresh empty-layer race에는 same-map/floor stable history를 전제로 bounded one-shot recovery를 수행한다.

### Scanner / Hideout / Ammo

- `플리마켓 최저가`는 presentation에서 제거했지만 compatibility data/model은 유지한다.
- `교정 데이터 추가` global hotkey 기본값은 `Ctrl+Alt+F9`이며 no-evidence exact status는 `저장할 스캔 결과가 없습니다.`이다.
- Hideout requirement의 `attributes.foundInRaid` 의미를 canonical requirement에 보존한다.
- ammo pickup evaluator는 same-caliber penetration과 현재 profile Trader LL/완료 quest를 사용한다.
- 현금 direct purchase만 현재 구매 가능으로 인정하며 barter/craft/flea/higher-LL은 제외한다.
- Ammo Pack은 authoritative `containsItems`를 우선해 contained canonical ammo로 resolve한다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout / Needed Items | 구현 완료 / maintenance |
| Items / Ammo | 구현 완료 / profile-aware pickup evaluator 포함 |
| Map + MiniMap | 구현 완료 / lifecycle repair 포함 |
| Game Content Update | 구현 완료 / relationship LKG + FIR semantics + fail-closed |
| Program Update | 구현 완료 / stable ZIP checksum 계약 |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE** |
| Scanner Saved Case / Ground Truth | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner 아이템 정보 DB | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner Favorites / Recents | **IMPLEMENTED / PUBLIC STABLE** |

## Schema / compatibility

```text
Desktop target version: 1.11.1
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache: v1~v4 readable, v4 written
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

```text
v1.11.0 → v1.11.1 mandatory Game Content schema migration: none
v1.11.0 → v1.11.1 user.db migration: none
v1.11.0 → v1.11.1 Scanner display settings: v8 → v9 automatic normalize
```

## 유지되는 핵심 계약

- Scanner false positive보다 miss를 선호한다.
- OCR/matcher/candidate cap/visual recovery acceptance는 reviewed actual Tarkov evidence 없이 완화하지 않는다.
- Scanner recognition은 외부 화면 pixels + OCR만 사용한다.
- game process memory read / injection / hook / kernel / input automation / network manipulation / anti-cheat bypass를 사용하지 않는다.
- Scanner current needed = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`.
- Scanner source = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`.
- price/needed/source/relationship metadata는 Item ID proof에 사용하지 않는다.
- correction hotkey는 Ground Truth를 생성·추측하지 않는다.
- Game Content candidate/LKG/completeness/fail-closed를 유지한다.
- Map/MiniMap donor pin은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- user-visible WPF lifecycle 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE runtime evidence로 검증한다.

## 검증 상태

v1.11.1 product release source `6314eaf866539747eadd69f8da4450bd8d5939e1`은 다음을 모두 통과했다.

- 460 automated tests
- Windows Release build
- Windows x64 self-contained publish
- actual published EXE Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- v1.11.1 Scanner settings/search/save-feedback runtime smoke
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified release workflow
- tag/release/assets/latest-stable public readback

사용자의 실제 PC/Tarkov 플레이 환경 실사용 검증은 자동화 검증과 별개이며 현재 **PENDING**이다.

## 다음 작업

v1.11.1 릴리즈 배치에 남은 제품 개발 작업은 없다. 기본 운영 모드는 유지보수다. 새 기능은 사용자가 명시적으로 제품 요구사항으로 확정할 때 시작하고, 실사용 회귀 또는 Tarkov 변화가 확인되면 현재 stable에서 최소 범위로 수정한다.

이 문서와 이후 documentation-only commit은 v1.11.1 product release source가 아니다. v1.11.1 product source/tag/assets는 `6314eaf866539747eadd69f8da4450bd8d5939e1`에 고정한다.
