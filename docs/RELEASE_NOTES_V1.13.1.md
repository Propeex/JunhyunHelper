# 준현 헬퍼 v1.13.1

## 파밍 가이드 UI 재구성

- v1.13.0의 텍스트 목록 중심 장비 화면을 **아이콘 중심의 Tarkov 인벤토리 유사 슬롯 화면**으로 다시 구성했습니다.
- 헤드셋, 헬멧, 얼굴 장비, 완장, 방탄복, 안경, 주무기, 권총, 근접무기, PMC 인식표가 각 장비 슬롯에서 실제 아이템 이미지로 보입니다.
- 리그·가방·보안 컨테이너는 장착 장비 아이콘과 실제 내부 grid를 한 영역에서 확인할 수 있게 정리했습니다.
- 수납 grid에 배치한 아이템도 이름 텍스트 대신 실제 아이템 이미지를 사용합니다.
- 드래그 중 따라오는 ghost 역시 실제 아이템 이미지를 사용하고, `R` 회전 상태가 이미지에도 반영됩니다.

## drag-drop 회귀 수정

- WPF mouse capture 중 장비 영역이 hit-test 대상에서 빠져 방탄복·리그·가방·보안 컨테이너에 놓을 수 없던 문제를 수정했습니다.
- 실제 화면 좌표를 기준으로 drop target을 판정하도록 보강해 장비 슬롯과 carrier 슬롯을 안정적으로 찾습니다.
- 수납 grid 인접 snap과 기존 배치 검증은 기존 로직을 유지합니다.
- 유효한 위치의 초록색, 불가능한 위치의 빨간색 강조가 다른 곳으로 커서를 옮긴 뒤 남아 있던 문제를 수정했습니다.

## 세부 UI 수정

- 잘리던 프리셋 저장 아이콘을 WPF-safe 벡터 아이콘으로 교체했습니다.
- 검색창 입력 텍스트가 위아래로 잘리지 않도록 높이와 수직 정렬을 보정했습니다.

## 유지되는 기존 계약

이번 버전은 기존 기능의 UI/interaction 회귀를 수정하는 PATCH 릴리즈입니다. 다음 동작은 그대로 유지됩니다.

- 실제 Tarkov `가로 × 세로` 아이템 크기
- 드래그 중 `R` 90도 회전
- grid bounds / overlap / contiguous-space / current filter 검증
- 현재 검증된 Tarkov 장비 슬롯·수납 grid·호환성 데이터 사용
- attachment / 교체형 armor plate 설정
- 전체 raid-start state 프리셋 저장·불러오기
- 근접무기 / PMC 인식표의 per-profile 고정 설정
- filled carrier destructive replacement fail-closed
- current Tarkov 구조와 맞지 않는 오래된 preset placement의 복원 차단

파밍 가치 판단, pickup/discard/replace 추천, Scanner 실시간 파밍 추천은 v1.13.1에서도 추가하지 않습니다.
