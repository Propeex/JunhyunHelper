# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Current work

**Farming Guide — loadout/storage/preset/inspect 실사용 보완**

```text
base stable: v1.13.1
base main: 8efad3360efbccce504c94669ae1aeb54288fdca
working branch: fix/farming-guide-loadout-storage-inspect-2026-08-31
```

### User-confirmed product requirements

1. 프리셋 저장 버튼 오른쪽에 휴지통 아이콘의 프리셋 삭제 버튼을 추가한다.
2. 프리셋 이름 입력 창 하단이 잘리는 UI 회귀를 수정한다.
3. 권총은 무기 1/2가 아니라 전용 권총(Holster) 슬롯에 정상 장착할 수 있어야 한다.
4. 방탄복, 리그, 가방, 보안 컨테이너가 정상 장착되어야 한다.
5. 수납 공간 순서는 `리그 → 주머니(좌) + 특수 슬롯(우) → 가방 → 컨테이너`다.
6. 주머니는 활성 프로필/에디션에 맞는 실제 칸 구조를 사용해야 하며 `1×1, 1×2, 1×2, 1×1` 같은 확장 주머니도 표시·배치·저장 검증 전체에서 동일하게 처리한다.
7. 장비를 더블클릭하면 내부 정보를 연다. 총기는 부착물, 헬멧/방탄복은 방탄판·NVG 등 장착 구조, 장착 전 검색 결과의 리그/가방/컨테이너는 내부 수납 구조를 확인할 수 있어야 한다.
8. 칼과 인식표의 고정 동작은 유지하되 화면의 `고정` 문구만 제거한다.

### Root causes confirmed so far

- `FarmingGuideCompatibility.IsEquipmentSlotCompatible`는 일반 장비 슬롯만 판정하고 Rig/Backpack/Secure Container는 별도 carrier 경로로 분리되어 있다. 사용자 보고 회귀는 UI drop target과 이 별도 carrier 계약이 실제 item 분류와 정확히 맞지 않는 지점을 점검·보강해야 한다.
- Holster는 `ItemPropertiesWeapon`과 pistol 계열 key를 동시에 요구한다. 실제 catalog 분류와 비교해 pistol 판정을 보강한다.
- `FarmingGuidePage.StorageDefinitions()`와 `FarmingGuideLoadoutPolicy.SanitizeSnapshot()`가 Pockets를 4개의 1×1 grid로 하드코딩한다. 프로필별 확장 주머니를 표현할 수 없는 명확한 결함이다.
- 더블클릭 편집 창은 equipment/carrier/stored item에 일부 존재하지만 검색 결과에는 연결되지 않고, storage-only carrier는 내부 구조 확인 창이 열리지 않는다.

### Current checkpoint

- canonical project/docs 및 v1.13.1 source 복구 완료.
- 관련 Core/Desktop/Infrastructure loadout 코드 분석 진행 중.
- 다음 단계:
  1. 실제 catalog의 pistol/body armor/rig/backpack/secure-container 분류와 pocket source를 확인한다.
  2. 중앙 compatibility/pocket-layout 정책과 deterministic tests를 먼저 수정한다.
  3. preset delete + dialog layout + storage section ordering + generic inspect UI를 구현한다.
  4. FarmingGuide targeted tests → full regression → Release/XAML build → published EXE smoke/CI 순으로 검증한다.

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

자동화 release verification과 별개로 다음은 후속 실사용 evidence입니다.

- 사용자의 실제 PC/Tarkov에서 v1.13.1 최종 실사용 확인
- 김태영 실제 PC diagnostic ZIP 수집/분석

후속 documentation-only commit은 v1.13.1 product release source가 아닙니다. historical product identity는 `302f83e88cc65b5fae9b86b5cae294b2586c85a0`에 고정합니다.
