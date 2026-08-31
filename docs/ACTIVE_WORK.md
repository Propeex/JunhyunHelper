# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Goal

Farming Guide의 loadout/storage/preset/inspect 실사용 회귀를 수정하고, 사용자 확정 동작을 deterministic tests와 Windows published-runtime 검증으로 고정한다.

## Base

```text
public stable: v1.13.1
base main: 8efad3360efbccce504c94669ae1aeb54288fdca
branch: fix/farming-guide-loadout-storage-inspect-2026-08-31
PR: #245
implementation/test checkpoint before documentation updates:
d5352534742baf1aa340ba12554cbb0fe89b770f
```

## Confirmed scope

1. 프리셋 저장 버튼 오른쪽에 휴지통 아이콘의 프리셋 삭제 버튼을 추가한다.
2. 프리셋 이름 입력 창 하단이 잘리는 UI 회귀를 수정한다.
3. 권총은 무기 1/2가 아니라 전용 권총(Holster) 슬롯에 정상 장착할 수 있어야 한다.
4. 방탄복, 리그, 가방, 보안 컨테이너가 정상 장착되어야 한다.
5. 수납 공간 순서는 `리그 → 주머니(좌) + 특수 슬롯(우) → 가방 → 컨테이너`다.
6. 주머니는 활성 프로필/에디션에 맞는 실제 칸 구조를 사용해야 하며 `1×1, 1×2, 1×2, 1×1` 같은 확장 주머니도 표시·배치·저장 검증 전체에서 동일하게 처리한다.
7. 장비를 더블클릭하면 내부 정보를 연다. 총기는 부착물, 헬멧/방탄복은 방탄판·NVG 등 장착 구조, 장착 전 검색 결과의 리그/가방/컨테이너는 내부 수납 구조를 확인할 수 있어야 한다.
8. 칼과 인식표의 고정 동작은 유지하되 화면의 `고정` 문구만 제거한다.

### Confirmed root causes

- Holster가 generic weapon property와 pistol key를 동시에 요구해 실제 pistol 분류를 놓칠 수 있었고, 반대로 주무기 슬롯은 pistol을 별도로 배제하지 않았다.
- Rig / Backpack / Secure Container는 equipment가 아니라 carrier 경로인데 해당 판정이 `propertiesType`에 과도하게 의존했다.
- Pockets는 UI와 persisted-state validation 모두 4개의 1×1 grid로 하드코딩되어 확장 주머니를 표현할 수 없었다.
- 기존 더블클릭 설정 창은 attachment / 교체형 armor slot이 있는 아이템만 열려 storage-only carrier와 검색 결과의 내부 구조를 볼 수 없었다.
- preset name dialog는 고정 client height를 사용해 DPI/theme에 따라 하단 버튼이 잘릴 수 있었다.

## Completed

- `FarmingGuideCompatibility`
  - pistol / revolver / handgun 계열을 Holster로 판정.
  - pistol은 PrimaryWeapon1/2에서 제외.
  - body armor / rig / backpack / secure container 판정을 canonical type/category key로 보강.
- `FarmingGuidePocketLayoutPolicy`
  - standard `1×1 / 1×1 / 1×1 / 1×1`.
  - expanded `1×1 / 1×2 / 1×2 / 1×1`.
  - Old Patterns 완료 또는 edition 기본 특전을 동일 정책으로 해석.
  - resolved pocket geometry를 UI와 `SanitizeSnapshot` 저장 검증에 공통 사용.
- Farming Guide는 활성 profile provider를 통해 edition / completed quests를 읽어 pocket geometry를 결정.
- storage UI 순서를 `Rig → Pockets + Special Slots → Backpack → Secure Container`로 고정하고 Pockets는 좌측, Special Slots는 우측 2-column row로 배치.
- melee / dogtag의 global fixed behavior는 보존하면서 슬롯 표시의 `고정` 문구 제거.
- preset delete store + 휴지통 UI + 삭제 확인 추가. 선택 preset 삭제 시 working loadout은 유지하고 selector만 미선택 상태로 전환.
- preset name dialog를 content-sized height로 전환해 하단 clipping 제거.
- 기존 item configuration window를 공통 internal structure window로 확장.
  - storage grid 실제 width/height preview.
  - attachment slot.
  - editable/locked armor slot.
  - equipped item은 편집 가능한 구조는 기존대로 수정 가능.
  - storage-only item은 read-only inspect.
  - search result도 double-click read-only inspect 지원.
- deterministic regression coverage 추가:
  - holster vs primary weapon classification.
  - carrier classification fallback.
  - standard / expanded pocket resolution.
  - expanded 1×2 pocket persisted placement validation.
  - preset deletion persistence / working-state preservation.
- PR #245 생성 및 CI 시작.

## Current step

PR exact-head 검증 중이다.

- 최초 PR head `ca931a989dff7b8536d442c7b42f272560a627f9`에서 CI / Shutdown Race CI가 실행 중이었다.
- Documentation Consistency는 ACTIVE_WORK required headings 형식 불일치로 실패했으며 제품 코드 실패가 아니다.
- `.github/scripts/Test-DocumentationConsistency.ps1` 계약에 맞춰 현재 문서를 `Goal / Base / Confirmed scope / Completed / Current step / Remaining` canonical ACTIVE 형식으로 교정했다.
- 현재 런타임의 외부 DNS 제한으로 로컬 clone/build는 시작되지 못했다. 제품 또는 test failure 증거는 아니며 Windows compile/runtime 검증은 repository CI가 담당한다.

## Remaining

1. 문서 교정 이후 새 exact PR head의 Documentation Consistency가 통과하는지 확인한다.
2. exact-head CI / Shutdown Race CI의 C#/XAML compile, deterministic tests, Release publish, Product UI/Farming Guide smoke 결과를 확인한다.
3. 실패가 있으면 로그 기준으로 코드 또는 테스트를 수정하고 exact-head 검증을 반복한다.
4. 검증된 동작을 `docs/PRODUCT.md`, `docs/CURRENT_STATE.md`, `docs/STATE.md`, Farming Guide architecture/decision 문서에 반영한다.
5. 모든 required exact-head checks가 green인 동일 HEAD만 merge한다.
6. exact-main CI를 다시 확인하고 현재 release policy에 따라 PATCH release 필요 여부/버전을 확정한다.
7. release를 수행하는 경우 public tag/release/asset digest까지 검증한 뒤 ACTIVE_WORK를 NONE으로 닫는다.

## Last completed work

**v1.13.1 Farming Guide — 실사용 UI / drag-drop 회귀 수정**

```text
public stable: v1.13.1
exact product release source/tag target:
302f83e88cc65b5fae9b86b5cae294b2586c85a0
fix branch: fix/v1.13.1-farming-guide-ui-regressions-2026-08-31
PR: #243 — MERGED
validated PR head:
314ce0501c0f680aacb13d2b3c61b20487c4eb15
PR exact-head CI: 33364597514 — SUCCESS
PR exact-head Shutdown Race CI: 33364597501 — SUCCESS
PR exact-head Documentation Consistency: 33364597497 — SUCCESS
exact-main CI: 33364865109 — SUCCESS
exact-main Shutdown Race CI: 33364865123 — SUCCESS
exact-main Documentation Consistency: 33364865134 — SUCCESS
release workflow: 33365070880 — SUCCESS
release id: 379553485
494 passed / 0 failed / 0 skipped
```

## Canonical records

- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/PRODUCT.md`
- `docs/RELEASE_1.13.1.md`
- `docs/RELEASE_NOTES_V1.13.1.md`
- `docs/.release-v1.13.1-status.json`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## External real-world evidence still pending

자동화 release verification과 별개로 다음은 후속 실사용 evidence다.

- 사용자의 실제 PC/Tarkov에서 최종 Farming Guide 실사용 확인
- 김태영 실제 PC diagnostic ZIP 수집/분석

v1.13.1 historical product identity는 `302f83e88cc65b5fae9b86b5cae294b2586c85a0`에 고정한다.
