# 준현 헬퍼 v1.16.4

상태: **RELEASE CANDIDATE / NOT YET PUBLIC STABLE**  
기준일: **2026-09-02 KST**

v1.16.4는 v1.16.3에서 확인된 Farming Guide 잠금 아이템 위치 회귀를 수정하는 PATCH hotfix다.

실사용에서 `Wires 전선`을 판단하는 과정에 잠금 상태의 `Grizzly 응급 치료 키트`를 이동하라는 지시가 포함되는 증상이 확인됐다. 원인은 v1.16.3이 exact item lock을 **인스턴스 생존 보호**로만 해석하고, 같은 `InstanceId`가 유지되면 자동 재배치를 허용한 데 있다.

## 수정된 잠금 계약

명시적으로 잠근 stored item은 자동 Farming Guide 판단에서 **위치까지 고정**된다.

자동 지시는 해당 아이템에 대해 다음을 할 수 없다.

- 버리기
- 다른 아이템으로 교체
- 다른 수납 공간으로 이동
- 같은 수납 공간 안에서 좌표 이동
- 회전 변경
- 부모 컨테이너 변경
- 상위 stored container 이동을 통한 간접 이동
- 장착 중인 root carrier 교체를 통한 간접 이동

사용자가 파밍 가이드 화면에서 직접 편집하는 동작은 계속 허용된다. 잠금은 자동 recommendation에 대한 제약이다.

## 컨테이너 잠금과 내부 공간

장착 중인 가방·리그·보안 컨테이너 자체를 잠그는 기존 계약은 유지된다.

- 잠긴 carrier root는 자동 교체하지 않는다.
- carrier 내부의 합법적인 빈칸은 계속 사용할 수 있다.
- 내부의 잠기지 않은 아이템은 별도 item lock이 없다면 정상 판단 대상이다.

stored container 자체를 item lock한 경우에도 그 container의 물리적 위치는 고정되지만, 내부의 독립적으로 잠기지 않은 내용물까지 자동으로 같은 위치 잠금으로 간주하지 않는다. 단, 잠긴 descendant를 담고 있는 상위 stored container는 그 descendant를 간접 이동시키지 않도록 이동할 수 없다.

## 보안 컨테이너 우선 보호 수정

v1.16.3의 secure-promotion은 고가치 secure-eligible incoming item을 보호하기 위해 낮은 우선순위 secure contents를 일반 공간으로 옮길 수 있었다.

v1.16.4에서는 그 계획이 명시적으로 잠긴 기존 item의 위치를 바꾸면 채택하지 않는다. 잠금 상태를 그대로 유지한 채 가능한 ordinary storage 판단으로 넘어간다.

따라서 잠긴 Grizzly를 밖으로 빼고 Wires를 보안 컨테이너에 넣는 식의 자동 지시는 생성되지 않는다.

## 일반 재배치와 carrier 교체

lock-aware repacking에서 다음 항목은 hard geometry obstacle로 취급한다.

- exact locked stored item
- locked descendant를 포함해 이동 시 그 descendant를 간접 이동시키는 stored ancestor

carrier upgrade가 locked descendant의 실제 물리적 위치를 바꾸는 경우에도 해당 upgrade는 자동 recommendation으로 채택하지 않는다.

## 최종 fail-closed 검증

모든 자동 recommendation은 최종 지시 전에 각 locked stored item에 대해 다음을 다시 비교한다.

- `InstanceId`
- item identity
- storage kind
- grid index
- X/Y 좌표
- rotation
- `ParentInstanceId`
- quantity
- stored ancestor chain의 placement
- root Rig / Backpack / SecureContainer identity

하나라도 달라지면 recommendation은 거부하고 현재 상태를 유지한다.

## 회귀 검증

published EXE Farming Guide decision smoke에 다음 경계를 추가했다.

- 사용자 보고와 같은 secure-container locked-item + ordinary free storage 상황
- locked blocker를 옮겨야만 성립하는 일반 repacking 차단
- moved-lock proposal을 최종 safety가 fail closed 하는지 검증
- root carrier 교체로 locked descendant가 간접 이동하는 경우 차단

v1.16.3의 나머지 secure promotion, locked-carrier internal storage, expanded pockets, stack total value, victim subset, food/drink reserve, current-weapon ammo reserve, FIR-only need 검증은 그대로 유지한다.

공개 stable은 실제 PR 검증·main 병합·exact-main 검증·공개 Release asset 검증이 완료되기 전까지 v1.16.3이다.
