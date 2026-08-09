# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `MAP RESET COMPLETE / EXACT TARKOV-HELPER MAP + MINIMAP TRANSPLANT IN VALIDATION`

---

## 현재 최우선 사용자 결정 — 2026-08-09

기존 JunhyunHelper에서 PR #44 이후 개발한 Map 구현은 제품 기준에서 **전부 폐기**합니다.

사용자가 확정한 기준:

- 지도 기능을 처음 만들기 전 상태로 돌아감
- 기존 `Propeex/Tarkov-Helper`의 **지도 기능을 그대로 오려내듯 이식**
- **MiniMap 포함**
- 기존 Tarkov Helper를 참고해 JunhyunHelper용으로 다시 만드는 방식 금지
- 지도 내부 동작을 JunhyunHelper의 이전 Map 아키텍처에 맞춰 재해석하지 않음
- 새 앱에서 실행하기 위한 최소 host adapter만 허용

공식 상세 문서:

- `docs/MAP_TRANSPLANT_RESET.md`

이 결정은 이전 PR #61의 재구현형 legacy Map 이식 방향을 **대체**합니다.

---

# Map reset 기준점

JunhyunHelper의 자체 Map 기능은 PR #44에서 처음 추가됐습니다.

Map 기능 추가 직전 기준점:

```text
7d4d94a36c18e15dd418216ab98d68e38976759d
```

PR #62 작업에서 이 기준을 사용해 다음 JunhyunHelper Map 전용 요소를 제거했습니다.

- Map domain model / content schema extension
- Quest objective Map geometry extension
- Map marker importer / validator
- Tarkov.dev Map layout client
- RE3MR / Official Wiki / Tarkov.dev artwork provider
- Map asset cache / candidate / recovery / refresh policy
- JunhyunHelper MapPage / coordinate transformer / marker renderer
- JunhyunHelper MiniMap 재구현
- Map-specific settings / persistence / tests
- JunhyunHelper Game Content update와 Map asset refresh coupling

현재 Game Content snapshot schema는 Map 기능 도입 전 계열인 **v3**입니다.

---

# Exact Tarkov Helper Map transplant

구현 브랜치 / PR:

```text
branch: agent/clean-legacy-map-transplant
PR: #62 — Reset Map and transplant Tarkov Helper Map subsystem
```

기존 Tarkov Helper의 현재 `main` 기준 revision:

```text
Propeex/Tarkov-Helper
9371c4769d8da8acb9df864a2c88f83ecdd42818
```

JunhyunHelper는 이 저장소를 다음 submodule로 직접 고정합니다.

```text
vendor/Tarkov-Helper
```

따라서 Map 소스와 자산은 JunhyunHelper가 다시 작성한 복사본이 아니라 **고정된 원본 Tarkov Helper commit 자체**입니다.

CI도 `submodules: recursive`로 정확한 revision을 checkout합니다.

---

## 원본에서 직접 사용하는 범위

### UI / runtime

- `TarkovHelper.Pages.Map.MapPage.xaml`
- `TarkovHelper.Pages.Map.MapPage.xaml.cs`
- Map partial / component classes
- `OverlayMiniMapWindow`
- `OverlaySettingsWindow`
- `CustomMarkerEditorWindow`

### Map service / model

- `TarkovHelper.Models/**`
- Map / Quest objective / progress / settings / localization 중 Map runtime이 실제 요구하는 원본 service
- `MapTrackerService`
- `FloorDetectionService`
- `SharedMapFloorStateService`
- `EftRaidEventService`
- `GlobalKeyboardHookService`
- `OverlayMiniMapService`
- extract / map marker / quest marker / custom marker managers

### 원본 자산

배포 결과에도 기존 상대 경로를 유지합니다.

```text
Assets/tarkov_data.db
Assets/DB/Data/*.json
Assets/DB/Maps/*.svg
Assets/DB/Icons/*.svg
Assets/DB/Icons/Markers/*.svg
```

따라서 old Tarkov Helper Map이 사용한 SVG, `map_configs.json`, marker icon, legacy Map DB를 그대로 사용합니다.

---

# 허용된 JunhyunHelper host adapter

원본 Map 소스는 수정하지 않는 것을 기본 원칙으로 합니다.

JunhyunHelper가 제공하는 접합부는 현재 다음뿐입니다.

1. **Map tab host**
   - 지도 탭을 처음 열 때 원본 `TarkovHelper.Pages.Map.MapPage` 객체를 한 번 생성
   - 기존 `MapPlaceholder` 안에 원본 객체 자체를 삽입
   - 탭 전환 시 객체를 제거하지 않아 원본 MiniMap lifecycle을 보존

2. **MainWindow full-screen contract**
   - 원본 MapPage의 `SetFullScreenMode(bool)` 호출을 JunhyunHelper shell의 상단 행 표시/숨김으로 연결

3. **legacy user-data root**

```text
%LocalAppData%/JunhyunHelper/legacy-tarkov-helper
```

   - 원본 settings/user data 서비스가 사용할 외부 저장 위치만 제공

4. **legacy Map DB path**

```text
<AppDirectory>/Assets/tarkov_data.db
```

   - 원본 DB reader가 기대하는 `DatabaseUpdateService.Instance.DatabasePath` host boundary 제공
   - old content updater 전체는 이식하지 않음

5. **WPF resource compatibility**
   - 원본 MapPage가 요구하는 공통 converter/resource key만 host에서 제공

이 adapter들은 Map 좌표 계산, floor 판단, marker 배치, MiniMap view 동작을 재구현하지 않습니다.

---

# 현재 자동 검증 상태

중간 checkpoint에서 다음이 확인됐습니다.

```text
exact Tarkov-Helper submodule checkout: success
original Map/MiniMap source compilation: success
JunhyunHelper existing core tests: success
Windows x64 self-contained publish: success (pre-startup-smoke checkpoint)
ZIP creation/upload: success (pre-startup-smoke checkpoint)
```

마지막 gate로 publish된 실제 EXE가 시작 직후 종료되지 않는지 확인하는 **Startup Smoke**를 CI에 추가했습니다.

최종 CI run은 완료 후 이 문서에 기록합니다.

---

# 기존 Core 제품 상태

Map 외 Core 기능은 유지합니다.

## Profile

- 한 GameMode당 profile 하나
- 새 프로필 / 수정 / 삭제
- level / faction / edition / prestige / trader 상태
- Fence reputation 진행값

## Quest

- 진행 중 / 잠김 / 사용 불가 / 완료
- prerequisite / item requirement
- 제출 및 취소 inventory ledger

## Hideout

- 시설 레벨 추적
- next-upgrade material
- upgrade / rollback inventory ledger

## Needed Items / Item

- FIR / 일반 필요량과 보유량
- 검색 / 종류 / 용도 / 상태 필터

## Ammo

- json.tarkov.dev 기반 raw 성능
- Wiki Ballistics 비교 정보
- favorite shortcut popup

---

# 업데이트 원칙

프로젝트 전체의 최우선 원칙은 그대로 유지합니다.

```text
온라인 Tarkov 데이터
→ 다운로드
→ 외부 형식 검증
→ canonical model 변환
→ candidate
→ 검증
→ active 교체
→ User Progress와 결합
```

- 일반적인 데이터 내용 변화는 importer/변환 규칙으로 자동 재구축
- 외부 형식/의미 자체가 바뀐 경우에만 프로그램 변경
- runtime AI/GPT 없음
- 업데이트 실패 시 기존 정상 데이터와 User Progress 보호

Map은 현재 **정확한 old subsystem 이식 자체를 먼저 검증**하는 단계입니다.

사용자가 화면/동작 동일성을 확인한 뒤, Map update 대응은 이 원본 subsystem의 **source + config + SVG + DB가 서로 어긋나지 않는 atomic revision 단위**로 설계합니다. exact transplant 검증 전에 JunhyunHelper 방식으로 다시 Map data pipeline을 섞지 않습니다.

---

# Scanner

탭과 placeholder만 있습니다. 실제 Scanner 요구사항은 아직 별도 확정 전입니다.

---

## 현재 다음 작업

1. PR #62 최종 CI: build / tests / publish / Startup Smoke / ZIP 확인
2. Windows 테스트 빌드에서 지도 탭이 **원본 Tarkov Helper MapPage 그대로** 표시되는지 확인
3. Ground Zero 등 원본 SVG/도로/건물/지명 표시 확인
4. 원본 floor selector / zoom / pan / quest drawer / extracts / custom marker 확인
5. screenshot 현재 위치 / 방향 / raid Map auto-switch 확인
6. 원본 MiniMap 표시 / zoom / pan / player tracking / floor / click-through / hotkey 확인
7. 실제 화면에서 host resource/path 차이만 수정
8. exact transplant 검증 완료 후 Map 업데이트 대응 설계 재개
