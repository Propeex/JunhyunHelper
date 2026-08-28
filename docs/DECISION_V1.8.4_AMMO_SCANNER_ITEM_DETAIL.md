# v1.8.4 Ammo Toolbar / Scanner Item Detail Decision

상태: `CONFIRMED / IMPLEMENTED / RELEASE CANDIDATE VERIFIED`

기준일: 2026-08-28 KST

## 목적

v1.8.4는 새 Scanner 인식 기능을 추가하는 릴리즈가 아니다. 기존 Ammo UI와 v1.8.x Scanner 아이템 정보 DB의 표시 구조를 사용자가 요청한 형태로 정리하고, 실제 공개 실행 파일에서 그 UI가 보인다는 검증 계약을 강화하는 PATCH다.

## Ammo 결정

- `즐겨찾기 선택`은 탄약 탭 왼쪽 선택 영역에 둔다.
- `표시 열` 버튼은 툴바 오른쪽 끝에 유지한다.
- 구경과 즐겨찾기 ComboBox는 기존 v1.8.3의 같은 아이콘 template/state를 공유한다.
- 같은 구경의 탄약 아이콘 순환 애니메이션과 timing은 변경하지 않는다.
- filtering/favorite persistence 의미는 변경하지 않는다.

## Scanner 아이템 상세 결정

아이템 상세은 한 줄기 세로 흐름으로 구성한다.

1. 기본 정보
2. 사용하는 곳
3. 수급처

기본 정보는 다음 네 항목만 사용자-facing으로 표시한다.

- 크기
- 플리마켓 평균가
- 최고 상인 판매가
- 현재 필요한 개수

Quest/Hideout 사용처는 기존 navigation authority를 유지한다.

제작/교환은 단순 텍스트 목록이 아니라 result/item과 전체 material을 함께 보여주는 recipe card로 표시한다. 재료가 가로 폭을 넘으면 `WrapPanel` 기반으로 자연스럽게 다음 줄로 이동한다.

관련 item name/icon은 같은 Scanner item-detail navigation으로 이동할 수 있어야 한다. 별도 네트워크 lookup이나 별도 Item identity 계산을 하지 않는다.

수급처는 canonical 관계 데이터에 따라 다음 그룹만 표시한다.

- 제작
- 교환
- 구매
- 레이드 획득

실제 row가 없는 그룹은 빈 shell을 표시하지 않는다.

레이드 문구는 canonical 관계 graph에 다른 획득 source가 함께 존재하는지에 따라 `레이드 획득 가능` 또는 `레이드에서만 획득 가능`로 구분한다.

## 유지되는 권위

이번 PATCH는 다음을 변경하지 않는다.

- Scanner Item ID recognition proof
- OCR threshold / matcher / candidate cap / visual recovery acceptance
- current needed authority = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`
- Quest/Hideout needed-source authority
- Game Content candidate/LKG/completeness/fail-closed 계약
- v3~v7 legacy snapshot compatibility
- Map/MiniMap donor revision 및 ownership boundary

## 검증 결정

source-level assertion만으로 사용자-visible UI 성공을 선언하지 않는다.

v1.8.4 release 후보는 실제 self-contained single-file Windows EXE를 실행해 다음 runtime evidence를 만들어야 한다.

```text
Ammo animated dropdown:
rendered-caliber-image=ok
rendered-favorite-image=ok
shared-timer-cycle=ok

Ammo toolbar:
favorite-selector-left=ok
displayed-columns-visible=ok
displayed-columns-right-edge=ok

Scanner item detail:
basic-four-fields=ok
empty-sections-hidden=ok
recipe-wrap=ok
related-item-buttons=ok
acquisition-groups=ok
```

CI는 각 marker 파일이 실제로 생성되었는지 확인한다. marker가 없으면 전체 product smoke가 성공했더라도 릴리즈 후보는 실패로 취급한다.

최종 기능 후보 검증 evidence:

```text
PR head before version/docs finalization: 5ec8126ba3ea90824d6f21117f38ad59b8d72d9a
CI run: 33151060896 — SUCCESS
424 passed / 0 failed / 0 skipped
published EXE Product UI / Main Map / Factory / MiniMap smoke — SUCCESS
all v1.8.4 Ammo/Scanner runtime markers — SUCCESS
```

현재 live Game Content release-readiness probe:

```text
workflow run: 33151060959 — SUCCESS
Regular: items=5312 quests=517 objectives=1457 questItems=305 hideout=26 ammo=200 fatal=0
PvE:     items=5312 quests=514 objectives=1434 questItems=293 hideout=26 ammo=200 fatal=0
```

이 live probe는 공개 직전 상류 계약 확인이며 일반 PR CI의 상시 네트워크 gate로 유지하지 않는다.
