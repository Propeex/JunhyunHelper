# 준현 헬퍼 v1.15.4

상태: **RELEASE CANDIDATE**  
기준일: **2026-09-01 KST**

v1.15.4는 v1.15.3의 파밍 가이드를 실전 레이드 상황에 맞게 강화하는 유지보수 PATCH다. 저장 공간이 단편화됐거나 더 좋은 장비를 발견했을 때 단순히 `버리기` 또는 일반 보관으로 떨어지기 전에, 현재 장비·수납·잠금·source-backed 제약을 보존하는 합법적인 전환을 먼저 찾는다.

## 수납 재배치

- 기존 unlocked 아이템을 이동·회전·재배치하면 새 아이템이 들어갈 수 있는 경우 비파괴 repacking을 먼저 시도한다.
- 하나 또는 여러 blocker, 서로 다른 root/nested storage surface 간 이동을 bounded deterministic search로 처리한다.
- `F` 잠금, 잠긴 ancestor, 예약 칸, allowed/excluded item/category filter, dedicated-container preference, nested parent/descendant 관계를 보존한다.
- populated nested container는 parent 자체의 가치만 보고 자동 파괴 교체하지 않는다.
- 비파괴 보존 경로가 실패한 뒤에만 필요도/가치 기반 파괴 교체를 검토하며, `버리기`는 마지막 수단이다.

## Nested storage Workbench

- Key tool을 포함해 current Game Content에 실제 `StorageGrids`가 있는 source-backed container의 상세 grid를 계속 지원한다.
- grid가 실제 center-column viewport 안에 들어가는 경우 불필요한 horizontal scrollbar가 생기거나 우측/하단 셀이 잘리지 않도록 WPF 측정·스크롤 정책을 수정했다.
- 실제 내용이 viewport보다 큰 경우에만 horizontal scrolling을 fallback으로 사용한다.

## 장비 업그레이드

- 시장가/상점가와 장비 성능을 분리한다. 값비싼 장비라는 이유만으로 성능 우위를 추정하지 않는다.
- 방탄복·헬멧 등 비교 가능한 보호 장비는 source-backed 대표 `properties.class`가 엄격히 높을 때만 성능 업그레이드로 판단한다.
- Backpack/Rig은 실제 source-backed storage capacity가 엄격히 우수하고 현재 모델링된 내용물을 전부 합법적으로 보존할 수 있을 때만 교체한다.
- Armored Rig끼리는 방어등급과 수납 capacity가 모두 비열화되지 않고 하나 이상 엄격히 개선되어야 한다.
- Headset은 `distanceModifier`가 비열화되지 않고 `distortion`도 비열화되지 않으며 둘 중 하나 이상 엄격히 개선될 때만 객관적 업그레이드로 판단한다. trade-off는 자동 순위화하지 않는다.

## 방탄복 + 일반 리그 → 아머드 리그

- incoming armored rig가 현재 body armor보다 엄격히 높은 대표 방어등급을 가지는 경우 atomic 전환을 검토한다.
- body armor와 rig가 모두 자동 교체 가능한 상태여야 한다.
- 현재 rig의 모든 modeled content가 incoming armored rig의 실제 grid/filter/reservation/lock 제약을 만족하도록 보존·재배치되어야 한다.
- 전환은 body armor 제거 + armored rig 장착 + rig 내용물 이전을 하나의 revision-bound pending transaction으로 제안하며, 사용자가 수락하기 전에는 상태를 변경하지 않는다.
- 일부만 가능한 전환은 fail closed한다. 기존 body armor를 남겨 둔 채 incoming armored rig를 일반 rig처럼 자동 장착하지 않는다.
- armored rig → body armor + 존재하지 않는 ordinary rig 역전환은 한 아이템 scan으로 추정하지 않는다.

## Complete equipment 경계

- 총기 부착물, 헬멧 부착물, 방탄판 등의 equipment-internal user state는 다시 도입하지 않는다.
- Tarkov source의 top-level armor class는 현재 완제품 모델이 사용할 수 있는 대표 방호 fact이며, 사용자가 인게임에서 교체한 개별 plate 상태를 안다고 가정하지 않는다.

## Game Content v11

- Game Content write schema를 v10에서 **v11**로 올렸다.
- v11은 Farming Guide가 사용하는 `ArmorClass`, `HeadsetDistanceModifier`, `HeadsetDistortion` source-backed fact를 canonical snapshot에 보존한다.
- v3~v10은 last-known-good offline snapshot으로 계속 읽을 수 있다.
- Desktop이 정상 구버전 snapshot을 읽으면 active content가 준비된 뒤 정상 transactional Data Update를 이용해 v11 갱신을 opportunistically 시도한다.
- 네트워크/업데이트 실패 시 구버전 정상 snapshot을 삭제하거나 시작을 막지 않으며 다음 실행/업데이트 때 다시 갱신할 수 있다.
- published-product smoke에서는 외부 네트워크 의존성을 만들지 않도록 opportunistic schema refresh를 건너뛴다.

## 검증 기준

공개 v1.15.4는 다음이 모두 통과한 exact product source만 릴리즈한다.

- deterministic test suite
- Windows Release build
- self-contained win-x64 publish
- 실제 published EXE Product UI / Map / Farming Guide smoke
- fragmented-capacity repacking smoke
- nested Workbench viewport smoke
- body armor + populated rig → armored rig preservation/repacking smoke
- graceful shutdown
- Shutdown Race CI
- package/checksum verification
- Documentation Consistency
- PR final-head CI
- exact-main CI

구현 및 제품 결정의 canonical 문서:

- `docs/DECISION_V1.15.4_FARMING_GUIDE_REPACKING_EQUIPMENT_UPGRADES.md`
- `docs/ACTIVE_WORK.md`
- `docs/PROJECT_STATE.json`
