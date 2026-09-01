# 준현 헬퍼 v1.15.2

## 파밍 가이드 장비 완제품 모델

v1.15.2는 Farming Guide의 장비 내부 개조 기능을 제거하고, 실제 레이드에서 프로그램이 신뢰성 있게 추적할 수 있는 상태만 유지하는 PATCH 릴리즈입니다.

### 장비는 완제품 하나로 처리합니다

총기·헬멧·방탄복 등은 더 이상 내부 부착물이나 방탄판 구성을 사용자가 재현하는 assembly tree로 관리하지 않습니다.

- 총기 부착물 슬롯 편집 제거
- 헬멧 부착물 편집 제거
- 방탄판 슬롯 편집 제거
- 내부 compatible-item picker 제거
- 장비 내부 Equip / ReplaceEquip 파밍 지시 제거
- 저장된 과거 assembly 상태는 로딩 시 root 장비 Item ID만 남기고 폐기

Tarkov Game Content의 원본 assembly/default-preset 메타데이터 자체는 source-backed 완제품 이미지 선택에 필요한 범위에서 유지되지만, 사용자 편집 상태나 레이드 판단 truth로 사용하지 않습니다.

### 최상위 장비 칸 판단은 유지합니다

Farming Guide는 계속 다음과 같은 완제품 장비 칸을 관리합니다.

- 헤드셋
- 헬멧
- 얼굴
- 완장
- 방탄복
- 안경
- 무기 1 / 무기 2
- 권총
- 리그
- 가방
- 보안 컨테이너

따라서 `무기 1에 장착`, `헬멧의 기존 장비와 교체` 같은 top-level Equip / ReplaceEquip 지시는 유지됩니다.

### 내부 수납 상세는 가방/리그에만 남깁니다

상세 내부 화면은 장비 개조 workbench가 아니라 **nested storage** 전용입니다.

- 저장된 가방 안 가방
- 저장된 가방/다른 허용 surface 안 리그

이러한 backpack/rig만 실제 Tarkov storage grid를 열어 내부에 아이템을 drag/drop할 수 있습니다. 일반 장비, 총기, 헬멧, 방탄복, generic case의 내부 편집 surface는 Farming Guide에 노출하지 않습니다.

루트 리그/가방/보안 컨테이너의 수납칸은 기존처럼 메인 Farming Guide 화면에 바로 표시됩니다.

### 상세 화면 크기를 실제 칸에 맞췄습니다

nested bag/rig를 열 때 중앙 수납 영역 전체를 가리던 큰 overlay 대신 실제 렌더링된 grid footprint와 제목/닫기 영역에 맞춰 상세 host 크기를 계산합니다.

작은 2×2 가방은 작은 창으로 표시되고, 큰/복수 grid 장비는 필요한 만큼만 커지며 현재 viewport를 넘지 않습니다. 메인 storage surface도 뒤에서 계속 보입니다.

### 완제품 이미지를 사용합니다

총기처럼 canonical base item 아이콘이 receiver/action 중심으로 보이는 장비는 authoritative default preset 관계가 있을 때 해당 preset의 source-backed 완제품 이미지를 우선 사용합니다.

이미지 선택 우선순위:

1. canonical default preset의 source-backed image
2. item 자체의 source-backed Farming Guide image metadata
3. canonical item icon

임의 부품 합성 이미지는 만들지 않습니다.

### 무기/장비 아이콘을 더 크게 표시합니다

장비 슬롯 내부의 큰 여백을 줄여 긴 총기와 다른 장비가 슬롯을 훨씬 더 채우도록 조정했습니다. 비율은 유지하며 장비 슬롯 안에서 안전하게 표시합니다.

## 호환성

- Farming Guide state schema: v2 유지
- Scanner display settings schema: v10 유지
- Game Content schema: v10 유지
- 저장된 legacy attachment/armor state는 현재 complete-equipment runtime projection에서 root-only state로 정리
- nested backpack/rig `ParentInstanceId` storage model 유지
- v1.15.1 Special Slot 및 lock semantics 유지

## 안전 경계

준현 헬퍼는 레이드 중 주운 장비의 알 수 없는 내부 부품 상태를 추측하지 않습니다. Scanner가 확인한 Item ID와 사용자가 직접 관리 가능한 top-level 장비/수납 상태만 파밍 판단에 사용합니다.
