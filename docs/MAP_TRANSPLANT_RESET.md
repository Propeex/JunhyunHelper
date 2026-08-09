# MAP TRANSPLANT RESET — 기존 Tarkov Helper 지도 시스템 그대로 이식

기록일: **2026-08-09**

상태: `USER CONFIRMED / IMPLEMENTATION IN PROGRESS`

## 사용자 확정 결정

현재 JunhyunHelper에서 PR #44 이후 개발한 Map 시스템은 제품 기준에서 폐기합니다.

구현 기준은 다음과 같습니다.

1. JunhyunHelper의 현재 Map 탭 기능을 전부 제거합니다.
2. Map 기능을 처음 추가한 PR #44의 직전 기준점(`7d4d94a36c18e15dd418216ab98d68e38976759d`)을 사용해, JunhyunHelper 자체 Map 구현이 없던 상태를 복원합니다.
3. 그 깨끗한 상태 위에 `Propeex/Tarkov-Helper`의 현재 Map + MiniMap 시스템을 **하나의 완성된 subsystem으로 그대로 이식**합니다.
4. 기존 Tarkov Helper의 지도 내부 동작을 JunhyunHelper의 이전 Map 아키텍처에 맞게 재해석하거나 재구현하지 않습니다.
5. 변경을 허용하는 부분은 JunhyunHelper에서 실행하기 위한 접합부에 한정합니다. 예: namespace, assembly/resource path, DI/service registration, app navigation/storage root 연결.
6. 지도 화면, SVG 자산, map config, 좌표 변환, floor 처리, screenshot tracking, raid map switching, marker rendering, Map/MiniMap shared state, MiniMap window/setting/hotkey/lifecycle은 기존 Tarkov Helper 구현을 기준으로 가져옵니다.
7. MiniMap도 이식 범위에 포함합니다.

## 중요한 해석

이번 작업은 "기존 Tarkov Helper의 지도를 참고해서 JunhyunHelper용으로 새로 만드는 작업"이 아닙니다.

목표는 다음에 가깝습니다.

```text
기존 Tarkov Helper Map subsystem을 그대로 오려냄
→ 외부 의존성 경계 확인
→ JunhyunHelper에 필요한 최소 adapter만 작성
→ 원본 동작을 보존한 채 연결
```

JunhyunHelper에서 PR #44~#61 동안 만든 RE3MR/Wiki/Tarkov.dev presentation provider, Map asset cache, 별도 canonical Map surface, 새 MiniMap 재구현은 이식 기준이 아닙니다.

## 구현 브랜치

`agent/clean-legacy-map-transplant`

## 다음 작업

- PR #44 직전 Map 탭 상태와 이후 Map 전용 변경 inventory 작성
- 현재 `Propeex/Tarkov-Helper` Map subsystem 전체 파일/dependency inventory 작성
- JunhyunHelper Map 구현 제거
- legacy Map subsystem 원본 이식
- 최소 adapter 연결
- automated build/test
- Windows 실제 화면 검증
