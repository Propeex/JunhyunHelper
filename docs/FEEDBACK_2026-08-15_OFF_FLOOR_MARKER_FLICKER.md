# FEEDBACK — Main Map 다른 층 일반 마커 깜박임 후 소실

기록일: **2026-08-15**

상태: `ROOT CAUSE CONFIRMED / FIX IMPLEMENTED / VALIDATION IN PROGRESS`

## 사용자 실사용 피드백

Main Map에서 다른 층의 일반 마커가 처음에는 보이지만 잠시 깜박인 뒤 완전히 사라지는 현상이 확인되었습니다.

## 원인

`LegacyStandardMarkerFloorPresentationBridge`의 Main Map vertical-stack 정리 로직이 다음 조건의 일반 마커를 같은 위치의 중복으로 취급했습니다.

```text
같은 marker type
AND 서로 다른 known floor
AND X/Z가 일정 거리 이내
→ 대표 하나만 남기고 나머지 Canvas.Opacity = 0
```

legacy `MapMarkersManager`는 마커를 비동기로 순차 추가하므로 초기에는 타층 마커가 정상 표시됩니다. 이후 관련 마커가 모두 로드되면 vertical-stack 로직이 다시 실행되어 일부 타층 마커를 `Opacity=0`으로 만들었습니다. 이 타이밍 때문에 사용자는 `표시됨 → 깜박임 → 사라짐`으로 보게 됩니다.

이는 이미 확정한 핵심 규칙인 **Floor는 visibility filter가 아니며, enabled 타층 marker는 계속 보여야 한다**는 계약과 충돌합니다.

## 제품 규칙 정정

일반 marker는 서로 다른 floor라는 이유만으로 중복 제거하지 않습니다.

```text
category ON
AND marker가 현재 Map에 속함
→ 각 marker visual 유지
→ Current / Above / Below floor relation만 presentation으로 적용
```

같은 type의 서로 다른 floor marker가 X/Z상 겹치거나 가까워도 대표 하나만 남기지 않습니다. 알려진 타층 marker는 약 75% opacity와 floor ring/작은 방향 glyph를 유지합니다.

실제로 **같은 물리 항목의 source 중복**이라고 확인할 수 있는 경우만 semantic duplicate 규칙을 적용합니다. 기존 Factory `Gate 3`처럼 같은 이름·같은 정규화 floor·거의 같은 좌표를 가진 PMC/Scav extract 대표 visual 정규화는 그대로 유지합니다.

## 구현 변경

- `LegacyStandardMarkerFloorPresentationBridge`
  - 일반 marker vertical-stack suppression 제거
  - 타층 marker에 `Opacity=0`을 쓰는 경로 제거
  - event-driven + bounded stabilization 유지
- Main Map runtime smoke
  - 실제 `MapMarkersContainer`의 표준 `MapMarker`를 검사
  - async marker build와 bounded settle 이후에도 known off-floor marker가 `Visibility.Visible`이고 opacity 70% 이상인지 검증
  - 과거처럼 잠깐 보인 뒤 뒤늦게 0 opacity가 되는 회귀를 차단

## 완료 조건

- Windows Release build 성공
- 기존 automated tests 전체 통과
- 실제 WPF Main Map smoke에서 async settle 이후 타층 표준 marker visibility 유지
- Factory extract/floor 기존 smoke 유지
- MiniMap/floor hotkey/viewport smoke 유지
- 최종 diff review 후 패치 릴리즈
