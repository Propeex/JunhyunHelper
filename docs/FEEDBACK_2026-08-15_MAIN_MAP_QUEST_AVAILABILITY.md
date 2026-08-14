# 사용자 피드백 — Main Map 층 마커 / Quest availability

기록일: **2026-08-15**

상태: **USER CONFIRMED / IMPLEMENTATION IN PROGRESS**

## Main Map 관찰

사용자 실사용 기준:

- Main Map 지상층에서 사실상 같은 위치/종류의 마커가 현재층 색과 회색 타층 마커로 겹쳐 보임
- 비지상층을 선택해도 일부 마커가 계속 타층처럼 흐리게 보이는 문제가 있음
- 다른 층 마커가 충분히 보이지 않음
- MiniMap에서는 같은 문제가 관찰되지 않음
- 기존 `↑/↓` badge가 지나치게 커서 마커를 방해함

## 확정 제품 방향

마커 자체의 고유 type/icon 색은 보존한다. 층 관계만 별도의 작은 색상 ring으로 표현한다.

```text
현재 선택 층 = 초록 ring
위층          = 빨강 ring
아래층        = 파랑 ring
층 불명확     = 방향/층 색상 추측 안 함
```

큰 `↑/↓` 텍스트 badge는 제거한다. 알려진 타층 마커는 약 75% opacity로 유지하여 보이되 현재층보다 약하게 표현한다.

Main Map에서 같은 marker type이고 서로 다른 floor이면서 화면상 사실상 같은 위치에 겹치는 vertical stack은 여러 아이콘을 겹쳐 그리지 않는다. 현재 선택 floor의 marker를 우선하고, 현재층 marker가 없으면 선택층과 Floor.Order가 가장 가까운 marker 하나를 대표로 표시한다. 이 정책은 Main Map의 중복 가독성 문제에만 적용하며 MiniMap의 정상 동작을 불필요하게 재구성하지 않는다.

## Quest availability 관찰

현재 지원하지 않는 `globalVariable`, `dialogue`, 실제 게임 완료 시각이 필요한 availability delay 등 때문에 `진행 중` Quest가 200개 이상으로 부풀어 보인다.

원인은 Core가 `Indeterminate`로 정확히 판정한 Quest를 Application 제품 경계에서 `Current`로 낙관 변환하던 기존 정책이다.

## 확정 제품 방향

프로그램이 현재 User Progress만으로 참/거짓을 증명할 수 없는 availability는 더 이상 `진행 중`으로 표시하지 않는다.

```text
Core Indeterminate
→ UI: 확인 필요
→ 진행 중(Current) 수치에서 제외
→ Map Current Quest sidebar에서 제외
```

다만 정확성을 잃지 않기 위해 다음을 유지한다.

- `확인 필요`를 `잠김`으로 거짓 확정하지 않음
- 원인(`globalVariable`, `dialogue`, availabilityDelay 등)을 계속 표시
- 사용자가 실제 게임에서 Quest를 완료했음을 아는 경우 수동 완료 허용
- Future Needed Items에서는 `IndeterminatePotential`로 계속 포함하여 필요한 아이템을 잘못 버리게 하지 않음
- 프로그램이 판별할 수 없는 조건의 의미를 임의 추측하지 않음

이 결정은 DEC-038의 `optimistic Current fallback` 부분을 대체한다.
