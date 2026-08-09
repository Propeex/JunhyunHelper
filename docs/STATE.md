# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `EXACT TARKOV-HELPER MAP + MINIMAP TRANSPLANT MERGED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

---

## 현재 최우선 사용자 결정 — 2026-08-09

PR #44 이후 JunhyunHelper에서 자체 개발했던 Map 시스템은 제품 기준에서 **전부 폐기**했습니다.

현재 공식 기준:

- Map 기능을 처음 만들기 전 상태를 기준으로 JunhyunHelper 자체 Map 구현 제거
- 기존 `Propeex/Tarkov-Helper`의 **지도 + MiniMap을 그대로 오려내듯 이식**
- 기존 Tarkov Helper를 참고해 비슷하게 재구현하지 않음
- 원본 Map 내부 동작은 수정하지 않는 것이 기본 원칙
- 새 앱에서 실행하기 위한 최소 host adapter만 허용

상세:

- `docs/MAP_TRANSPLANT_RESET.md`

이 결정은 이전 PR #61의 재구현형 legacy Map 이식 방향을 **완전히 대체**합니다.

---

# PR #62 — exact transplant

```text
PR: #62 — Reset Map and transplant Tarkov Helper Map subsystem
merge commit: 4b3d43051b48c3d00ab8fdba03814d24066a2fd0
validated head: 77ef3052e74c3134d7cd61994cebb29ac11d7f1e
final CI: 31309285854
```

최종 자동 검증:

```text
exact Tarkov-Helper submodule checkout: success
Desktop Release build: success
JunhyunHelper core tests: success
Windows x64 self-contained publish: success
published EXE Startup Smoke (12s): success
ZIP creation/upload: success
```

자동화는 통과했지만 **실제 Map 화면/동작 동일성은 사용자 Windows 검증 전까지 최종 완료로 간주하지 않습니다.**

---

# Map reset 기준

JunhyunHelper 자체 Map 기능이 처음 들어간 PR은 #44입니다.

기능 추가 직전 기준점:

```text
7d4d94a36c18e15dd418216ab98d68e38976759d
```

PR #62에서 다음 JunhyunHelper Map 전용 계층을 제거했습니다.

- Map domain / content schema extension
- Quest objective Map geometry extension
- Map marker importer / validator
- Tarkov.dev Map layout client
- RE3MR / Official Wiki / Tarkov.dev artwork provider
- Map asset cache / candidate / recovery / refresh policy
- JunhyunHelper MapPage / coordinate transformer / marker renderer
- JunhyunHelper MiniMap 재구현
- Map-specific settings / persistence / tests
- Game Content update와 JunhyunHelper Map asset refresh coupling

현재 Game Content snapshot schema는 Map 도입 전 계열인 **v3**입니다.

---

# 원본 Tarkov Helper source 기준

현재 Map/MiniMap 기준 revision:

```text
repository: Propeex/Tarkov-Helper
revision: 9371c4769d8da8acb9df864a2c88f83ecdd42818
submodule: vendor/Tarkov-Helper
```

JunhyunHelper는 원본 저장소를 git submodule로 고정하고 CI에서도 `submodules: recursive`로 checkout합니다.

따라서 Map 코드는 JunhyunHelper가 다시 작성한 복사본이 아니라 **해당 Tarkov Helper commit의 실제 원본 소스**입니다.

직접 사용하는 주요 원본:

- `TarkovHelper.Pages.Map.MapPage.xaml`
- `TarkovHelper.Pages.Map.MapPage.xaml.cs`
- Map partial/component classes
- Map/MiniMap models 및 runtime services
- `OverlayMiniMapWindow`
- `OverlaySettingsWindow`
- `CustomMarkerEditorWindow`
- `map_configs.json`
- `Assets/DB/Maps/*.svg`
- marker icons
- bundled `tarkov_data.db`

배포 결과에서도 원본 상대 경로를 유지합니다.

```text
Assets/tarkov_data.db
Assets/DB/Data/*.json
Assets/DB/Maps/*.svg
Assets/DB/Icons/*.svg
Assets/DB/Icons/Markers/*.svg
```

---

# JunhyunHelper host adapter

Map 좌표 계산, floor 판단, marker 배치, screenshot parsing, MiniMap view 알고리즘을 JunhyunHelper에서 다시 구현하지 않습니다.

현재 허용된 접합부:

1. **Map tab host**
   - 지도 탭 최초 진입 시 원본 `TarkovHelper.Pages.Map.MapPage`를 한 번 생성
   - 기존 `MapPlaceholder` 안에 원본 객체 자체를 삽입
   - 탭 전환 시 같은 인스턴스를 유지

2. **MainWindow full-screen contract**
   - 원본 `SetFullScreenMode(bool)` 호출을 JunhyunHelper shell 상단 행 표시/숨김에 연결

3. **legacy user-data root**

```text
%LocalAppData%/JunhyunHelper/legacy-tarkov-helper
```

4. **legacy Map DB path**

```text
<AppDirectory>/Assets/tarkov_data.db
```

5. **WPF/compiler/runtime compatibility**
   - 원본 XAML이 요구하는 공통 converter/resource 제공
   - old project와 JunhyunHelper의 WinForms implicit using, warning policy, package runtime 차이만 host/project layer에서 처리

---

# 기존 Core 제품 상태

Map 외 제품 기능은 유지합니다.

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

프로젝트 전체의 핵심 원칙은 유지합니다.

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
- 업데이트 실패 시 기존 정상 데이터/User Progress 보호

Map은 현재 **원본 시스템의 정확한 이식 검증이 우선**입니다.

사용자가 화면/동작 동일성을 확인한 뒤 Map 업데이트 대응은 원본 subsystem의 **source + config + SVG + DB가 서로 어긋나지 않는 atomic revision 단위**로 설계합니다. 검증 전에는 JunhyunHelper 방식의 Map pipeline을 다시 섞지 않습니다.

---

# Scanner

탭과 placeholder만 있습니다. 실제 Scanner 요구사항은 아직 확정 전입니다.

---

## 현재 다음 작업

1. Windows 테스트 빌드에서 지도 탭이 기존 Tarkov Helper MapPage와 동일한 구조로 표시되는지 확인
2. Ground Zero 등 원본 SVG의 도로/건물/지명 확인
3. map/floor/zoom/pan/quest drawer/extract/general marker/custom marker 확인
4. screenshot current position + heading / raid Map auto-switch 확인
5. 원본 MiniMap open/close / tracking / fixed view / zoom / pan / floor / click-through / hotkey 확인
6. 발견되는 차이는 **원본 Map 코드를 재설계하지 않고 host 접합부 차이만** 수정
7. exact transplant 검증 완료 후 Map 업데이트 대응 설계 재개
