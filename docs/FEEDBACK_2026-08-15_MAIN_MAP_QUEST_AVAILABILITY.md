# 사용자 피드백 — Main Map 층 마커 / Quest availability

기록일: **2026-08-15**

상태: **USER CONFIRMED / IMPLEMENTED / WINDOWS CI VERIFIED / PR #80**

## Main Map 관찰

사용자 실사용 기준:

- Main Map 지상층에서 사실상 같은 위치/종류의 마커가 현재층 색과 회색 타층 마커로 겹쳐 보임
- 비지상층을 선택해도 일부 마커가 계속 타층처럼 흐리게 보이는 문제가 있음
- 다른 층 마커가 충분히 보이지 않음
- MiniMap에서는 같은 문제가 관찰되지 않음
- 기존 `↑/↓` badge가 지나치게 커서 마커를 방해함

## 확인된 원인

두 경로가 동시에 문제를 만들었습니다.

1. pinned Tarkov Helper의 일반 marker renderer는 모든 floor marker를 만들고 off-floor를 흐리게 표시하므로, 같은 종류의 서로 다른 층 marker가 X/Z상 거의 같은 위치이면 회색/현재층 marker가 겹쳐 보일 수 있었습니다.
2. 더 중요한 직접 원인은 JunhyunHelper의 `LegacyMapInteractionPolicyBridge`가 200ms마다 non-current-floor 일반 marker와 extract를 `Visibility.Collapsed` 처리하던 current-floor-only 정책이었습니다. 이 때문에 별도 floor presentation에서 타층 relation을 계산해도 사용자는 실제 타층 marker를 볼 수 없었습니다.

PR #80에서 floor 기반 Visibility를 제거하고 category/faction만 visibility를 소유하도록 수정했습니다. legacy async refresh 직후 필요한 보정은 영구 full-tree polling이 아니라 제한된 bounded stabilization으로 처리합니다.

## 확정 제품 방향

마커 자체의 고유 type/icon 색은 보존한다. 층 관계만 별도의 작은 색상 ring으로 표현한다.

```text
현재 선택 층 = 초록 ring
위층          = 빨강 ring
아래층        = 파랑 ring
층 불명확     = 방향/층 색상 추측 안 함
```

알려진 타층 마커는 약 75% opacity로 유지하여 보이되 현재층보다 약하게 표현합니다. 색상 접근성을 위해 위/아래에는 7px 수준의 매우 작은 보조 방향 glyph만 사용하며 기존처럼 marker를 가리는 큰 화살표 badge는 사용하지 않습니다.

Main Map에서 같은 marker type이고 서로 다른 floor이면서 X/Z상 사실상 같은 위치에 겹치는 vertical stack은 여러 아이콘을 겹쳐 그리지 않습니다. 현재 선택 floor의 marker를 우선하고, 현재층 marker가 없으면 선택층과 Floor.Order가 가장 가까운 marker 하나를 대표로 표시합니다. 이 정책은 Main Map의 중복 가독성 문제에 적용하며 MiniMap의 기존 정상 경로를 불필요하게 재구성하지 않습니다.

## Quest availability 관찰

현재 지원하지 않는 `globalVariable`, `dialogue`, 실제 게임 완료 시각이 필요한 availability delay 등 때문에 `진행 중` Quest가 200개 이상으로 부풀어 보였습니다.

원인은 Core가 `Indeterminate`로 정확히 판정한 Quest를 Application 제품 경계에서 `Current`로 낙관 변환하던 기존 정책입니다.

## 확정 제품 방향

프로그램이 현재 User Progress만으로 참/거짓을 증명할 수 없는 availability는 더 이상 `진행 중`으로 표시하지 않습니다.

```text
Core Indeterminate
→ UI: 확인 필요
→ 진행 중(Current) 수치에서 제외
→ Map Current Quest sidebar에서 제외
```

다만 정확성을 잃지 않기 위해 다음을 유지합니다.

- `확인 필요`를 `잠김`으로 거짓 확정하지 않음
- 원인(`globalVariable`, `dialogue`, availabilityDelay 등)을 계속 표시
- 사용자가 실제 게임에서 Quest 완료를 알고 있으면 수동 완료 허용
- 비재시작형 영구 실패를 명시적으로 동기화해야 하는 Quest면 `확인 필요` 상태에서도 수동 실패 허용
- Future Needed Items에서는 `IndeterminatePotential`로 계속 포함하여 필요한 아이템을 잘못 버리게 하지 않음
- 프로그램이 판별할 수 없는 조건의 의미를 임의 추측하지 않음

이 결정은 DEC-038의 `optimistic Current fallback` 부분을 DEC-039로 대체하고, Map floor visibility/presentation 책임은 DEC-040으로 기록했습니다.

## 검증

최신 PR merge candidate 기준:

```text
CI run: 31839781212 — SUCCESS
Desktop Release build: SUCCESS
automated tests: 177 passed / 0 failed
Windows x64 self-contained single-file publish: SUCCESS
real startup + Main Map/MiniMap smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
portable root / runtime Logs contamination check: SUCCESS
artifact: JunhyunHelper-win-x64 / ID 9233872141
```

이 검증은 자동 회귀 방지 기준이며, 사용자가 처음 보고한 실제 Main Map 표시 현상의 최종 UX 확인은 다음 사용자 실사용에서 다시 확인합니다.
