# MiniMap floor-frame contract — 2026-08-17

상태: **CONFIRMED / v0.1.7**

## 제품 요구사항

같은 지도에서 MiniMap 층을 변경할 때 사용자의 눈에는 **현재 보고 있던 화면 구도는 그대로이고 층 그림만 바뀌어야 한다.**

즉 층 전환 자체는 다음 값을 바꾸는 사건이 아니다.

- 현재 live 확대율
- 화면상 X 이동량
- 화면상 Y 이동량
- 사용자가 보고 있던 위치

## 구현 기준

현재 Map bundle의 다층 지도는 층마다 별도 크기의 이미지를 사용하는 구조가 아니다. 한 지도 SVG의 동일한 canonical canvas 안에 `basement`, `main`, `level2` 같은 floor layer가 들어 있고, 층 전환은 그 layer의 표시 상태를 바꾸는 작업이다.

따라서 층별 임의 zoom 보정값을 만들지 않는다.

MiniMap floor change 직전에 실제 화면의 live transform을 캡처한다.

```text
ScaleX / ScaleY
TranslateX
TranslateY
```

새 floor layer 렌더가 끝나면 같은 transform을 정확히 복원한다.

PlayerTracking에서 live `MapTranslate`와 저장된 `MapOffsetX/Y`가 다를 수 있으므로, floor renderer가 stale 저장값을 읽기 전에 live transform을 settings에도 동기화한다.

층 전환 완료 후에는 `ClampMapOffset`으로 다시 계산하거나 map-space center에서 translation을 재구성하지 않는다. 같은 canonical canvas의 floor layer 교체에서 그런 재계산은 불필요하며 몇 픽셀의 이동 자체가 제품 요구사항 위반이 될 수 있다.

## 이 규칙의 범위

적용:

- MiniMap 수동 위층/아래층 전환
- MiniMap floor index 직접 선택

비적용:

- 다른 지도 자체로 변경
- 실제 player position 갱신에 따른 PlayerTracking 이동
- 사용자가 확대/축소 단축키를 직접 입력
- MiniMap 창 크기 변경

이 경우에는 해당 사건의 정상적인 viewport 계산을 따른다.

## 회귀 기준

기존 v0.1.5 runtime smoke의 stale persisted offset 재현을 유지한다.

- live zoom 유지
- live map-space center 유지
- floor 변경 후 persisted offset과 live translate 일치

v0.1.7 구현은 이 기존 기준보다 더 강하게 exact live scale + translation 자체를 복원한다.

## 관련 결정

- `DEC-010`: 받을 수 있는 Quest는 Helper에서 즉시 수락한 것으로 간주
- `DEC-043`: 특수 상인 prerequisite/access 의미 정정
- `DEC-044`: exact profile-variable Quest gate 지원, 미관측 값은 추측하지 않음
- 이 문서는 v0.1.5 MiniMap viewport 보존 결정을 **exact visual-frame preservation**으로 구체화한다.
