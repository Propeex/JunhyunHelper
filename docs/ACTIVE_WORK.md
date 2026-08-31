# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Current work

**Farming Guide — 실사용 장착/수납/프리셋/검사 UX 보완**

```text
base public stable: v1.13.1
base main: 8efad3360efbccce504c94669ae1aeb54288fdca
branch: fix/farming-guide-equipment-inspection-2026-08-31
phase: requirement recovery + root-cause analysis
```

### User-confirmed product requirements

1. 프리셋 저장 버튼 우측에 휴지통 아이콘의 프리셋 삭제 버튼 추가.
2. 프리셋 이름 입력 창의 하단/버튼이 잘리는 레이아웃 수정.
3. 권총은 `무기 1/2`이 아니라 권총(holster) 슬롯에 장착되도록 장착 호환성 수정.
4. 방탄복·리그·가방·보안 컨테이너가 각 장비 슬롯에 정상 장착되도록 회귀 수정.
5. 수납 공간 표시 순서를 위에서부터 `리그` → `주머니(좌)+특수 슬롯(우)` → `가방` → `컨테이너`로 구성.
6. 프로필/에디션에 따라 달라지는 주머니 grid를 실제 데이터에서 판별하며 `1,2,2,1` 형태를 포함해 그대로 표현.
7. 장비를 더블클릭하면 내부 정보 창을 연다. 총은 부착물, 헬멧/방탄복은 방탄판·야투경 등 장착 구성, 리그/가방 등 수납 장비는 내부 grid 정보를 보여준다.
8. 인식표와 칼 슬롯에서 `고정` 문구를 제거하되 preset과 독립된 fixed-state 의미 자체는 유지.

### Contracts to preserve

- v1.13.1 item-icon 중심 Tarkov 유사 slot board.
- 실제 Tarkov item footprint / 회전 / 장비 호환성 / storage grid placement.
- preset dirty-state (`프리셋 선택`) 계약.
- 칼·인식표는 preset과 독립된 사용자 고정 설정.
- unrelated Quest/Hideout/Items/Ammo/Map/Scanner 동작.

### Current checkpoint

- v1.13.1 공식 Farming Guide architecture / decision 계약 확인 완료.
- Farming Guide UI 소스 위치 확인 완료 (`src/JunhyunHelper.Desktop/FarmingGuide`).
- 다음 단계: equipment-slot compatibility, carrier grid, pocket/profile source, preset persistence, existing item configuration/inspection window를 실제 코드 기준으로 대조한 뒤 구현.

완전한 구현·회귀 테스트·published EXE 검증·PR/CI/main 병합·릴리즈/문서화가 끝날 때까지 이 작업을 닫지 않습니다.
