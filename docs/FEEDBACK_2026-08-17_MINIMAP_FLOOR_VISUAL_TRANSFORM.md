# MiniMap 층 전환 시각 변환 고정 — 2026-08-17

## 사용자 요구

MiniMap에서 층을 바꿀 때 사용자가 보고 있던 위치와 확대 비율이 변하지 않아야 한다. 사용자의 눈에는 화면이 확대/축소되거나 이동하는 것이 아니라 **같은 위치에서 층 그림만 교체되는 것처럼** 보여야 한다.

## 구현 기준

현재 Map/MiniMap floor artwork는 층마다 별도의 좌표계나 다른 크기의 SVG를 사용하는 구조가 아니다. 동일한 map SVG/canvas 좌표계에서 floor layer를 선택해 렌더링한다.

따라서 floor 전환의 권위 상태는 추정한 map-space center가 아니라 전환 직전의 실제 live transform이다.

- `MapScale.ScaleX`
- `MapScale.ScaleY`
- `MapTranslate.X`
- `MapTranslate.Y`

층 전환 직전에 이 값을 그대로 캡처한다.

legacy renderer가 SVG layer를 교체하며 persisted setting을 다시 읽기 전에 live transform을 setting에도 동기화하여 중간 프레임의 점프를 방지한다. 새 층 렌더가 끝나면 동일한 Scale/Translate를 복원한다.

렌더 도중 MiniMap window 자체 크기가 바뀐 특수 경우에는 viewport center delta만 Translate에 반영하고 Scale은 변경하지 않는다.

## 회귀 검증

Windows runtime Map smoke에서 실제 MiniMap을 띄운 뒤 PlayerTracking 상황처럼 persisted offset을 의도적으로 stale하게 만든다.

그 상태에서:

1. 층 A → B
2. direct floor selection으로 원래 층 A를 다시 선택

을 실행하고 각 단계마다 다음을 검증한다.

- ScaleX 동일
- ScaleY 동일
- TranslateX 동일
- TranslateY 동일
- map-space viewport center 동일
- persisted Zoom/Offset과 live transform 동기화

A→B→A 검사는 반복 층 전환에서 미세 오차가 누적되는 회귀까지 막기 위한 것이다. B→A는 floor ordering이나 up/down 방향을 추측하지 않고 원래 floor를 직접 다시 선택한다.

## 제품 결과

MiniMap floor hotkey/direct floor selection은 지도 viewport를 변경하는 명령이 아니다. 같은 화면 변환을 유지한 채 floor artwork만 교체하는 명령으로 취급한다.
