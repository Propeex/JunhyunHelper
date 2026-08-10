# Windows 실사용 피드백 — 설정 / 입력 / 성능 / 프로필 — 2026-08-10

상태: `USER CONFIRMED / IMPLEMENTATION IN PROGRESS`

## 사용자 확인 사항

이번 Windows 실사용에서 이전 Map marker / floor 수정이 반영된 것을 확인했다. 추가로 다음 제품 요구사항과 결함을 확정한다.

1. **Map 설정 영속화**
   - 지도 marker 체크 상태, 단축키, Map 탭에서 사용자가 바꾸는 설정은 앱 재시작 후에도 유지되어야 한다.
   - 현재 재시작 시 초기값으로 되돌아가는 현상은 결함이다.

2. **아이콘 초기 로딩**
   - 앱 시작 직후 기존 정상 Game Content와 icon cache/source를 이용해 아이콘이 보여야 한다.
   - 아이콘을 보기 위해 매번 `데이터 업데이트`를 실행하도록 요구하지 않는다.

3. **Map 단축키**
   - 지도 확대/축소와 위층/아래층 전환 단축키는 MiniMap 표시 여부와 무관하게 실제 Main Map에서 동작해야 한다.
   - 사용자가 설정한 키가 런타임 동작에 즉시 반영되어야 한다.

4. **별도 MiniMap 설정 제거**
   - 사용자는 MiniMap을 주로 단축키로 직접 조작한다.
   - Main Map의 별도 `미니맵 표시 설정` 진입 UI는 제거한다.
   - 필요한 사용자 조정값은 Main Map `설정` 안에 둔다.

5. **MiniMap 일시 투명**
   - 지정 단축키를 누르면 MiniMap 전체가 N초 동안 완전히 투명해져야 한다.
   - N초와 단축키 모두 Main Map `설정`에서 변경 가능해야 한다.
   - 목적은 인게임에서 MiniMap 뒤의 탈출구/레이드 시간 등 HUD를 잠깐 확인하는 것이다.
   - 시간이 끝나면 현재 정상 표시 상태로 자동 복귀한다.

6. **Ammo 선택 행 백화 결함**
   - Ammo 행을 선택한 뒤 다른 컨트롤/바탕으로 포커스를 옮겨도 선택 행이 흰 배경으로 바뀌면 안 된다.
   - 활성/비활성 selection 모두 JunhyunHelper dark theme를 유지한다.

7. **Ammo 표 열 구분선**
   - DataGrid에 세로 column separator를 표시한다.
   - column resize 중/후에도 열 경계를 명확하게 읽을 수 있어야 한다.

8. **앱 종료 보장**
   - Main Window를 닫으면 JunhyunHelper의 keyboard hook, MiniMap/보조 창, watcher/timer 등 runtime resource를 종료한다.
   - 사용자에게 창이 사라졌는데 background process가 계속 남는 상태를 허용하지 않는다.

9. **Map UI 경계/marker panel**
   - Map viewport가 상단 UI 영역을 침범해 보이지 않도록 clip/spacing을 보장한다.
   - `지도 마커` panel이 세로로 과도하게 길어 MiniMap 버튼/상단 control을 간섭하지 않도록 최대 높이 + 내부 scroll/compact layout을 적용한다.

10. **상태 변경 성능**
    - Quest 완료, Hideout level 변경, Item 보유량 변경마다 모든 workspace를 순차적으로 다시 불러오는 현재 구조는 개선 대상이다.
    - 데이터 일관성은 유지하되 독립 workspace 계산은 병렬화하고, 연속 입력의 중복 refresh/re-render를 합쳐 UI stutter를 줄인다.
    - 이 지연은 제품 특성상 불가피한 것으로 간주하지 않는다.

11. **프로필 수정 자동 저장**
    - 기존 프로필 수정에서는 별도 `저장` 버튼을 누르는 절차를 제거한다.
    - 변경된 값을 editor 종료 시 자동 저장하여 수정 후 창을 닫는 것만으로 반영한다.
    - 새 프로필 생성은 의도치 않은 profile 생성 방지를 위해 기존 명시적 생성 흐름을 유지할 수 있다.

## 구현 원칙

- 설정은 JunhyunHelper가 소유하는 안정된 user-data 경로에 저장한다. old Tarkov Helper의 실행 위치/legacy config 경로에 의존하지 않는다.
- Main Map hotkey는 MiniMap visible 조건에 묶지 않는다.
- MiniMap 일시 투명은 click-through / top-right anchor / PlayerTracking 고정 정책과 독립된 presentation 상태다.
- 연속 상태 변경 성능 개선은 사용자 진행 데이터를 낙관적으로 추측하거나 유실하는 방식으로 하지 않는다. 저장 완료와 UI refresh를 분리하고 중복 계산만 제거한다.
- 종료 시 강제 프로세스 kill을 첫 수단으로 사용하지 않는다. 소유한 resource를 정상 dispose/close한 뒤 WPF application shutdown을 보장한다.

## 검증 기준

- 재시작 전후 marker/hotkey/Map 설정 동일.
- cold start에서 Data Update 없이 icon 표시.
- zoom/floor hotkey Main Map 동작.
- 별도 MiniMap settings 진입 UI 제거.
- temporary transparency hotkey + duration 동작 및 자동 복귀.
- Ammo selected row가 focus loss 후 dark theme 유지.
- Ammo vertical grid lines 표시.
- window close 후 JunhyunHelper process 종료.
- Map viewport/header 경계 정상, marker panel이 상단 control을 가리지 않음.
- 연속 item +/- 등에서 불필요한 순차 full refresh 감소.
- existing profile editor는 save button 없이 close-to-save.
- Desktop Release build / automated tests / Windows x64 publish / Startup+Map smoke 통과.
