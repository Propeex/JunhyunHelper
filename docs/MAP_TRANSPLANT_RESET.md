# MAP TRANSPLANT RESET — 기존 Tarkov Helper 지도 시스템 그대로 이식

기록일: **2026-08-09**

상태: `EXACT SOURCE TRANSPLANT IMPLEMENTED / WINDOWS VALIDATION NEXT`

## 사용자 확정 결정

현재 JunhyunHelper에서 PR #44 이후 개발한 Map 시스템은 제품 기준에서 폐기합니다.

구현 기준:

1. JunhyunHelper의 현재 Map 탭 기능을 전부 제거합니다.
2. Map 기능을 처음 추가한 PR #44 직전 기준점 `7d4d94a36c18e15dd418216ab98d68e38976759d`를 사용해 자체 Map 구현이 없던 상태를 복원합니다.
3. 그 상태 위에 `Propeex/Tarkov-Helper`의 Map + MiniMap 시스템을 **하나의 subsystem으로 그대로 이식**합니다.
4. 기존 Tarkov Helper를 참고해 JunhyunHelper용으로 다시 만드는 방식은 사용하지 않습니다.
5. 변경 허용 범위는 새 앱에서 실행하기 위한 host adapter에 한정합니다.
6. MiniMap도 동일한 이식 범위입니다.

## 구현 방식

수동 복사는 사용하지 않습니다.

JunhyunHelper가 기존 Tarkov Helper 저장소를 git submodule로 직접 고정합니다.

```text
vendor/Tarkov-Helper
→ Propeex/Tarkov-Helper
→ 9371c4769d8da8acb9df864a2c88f83ecdd42818
```

따라서 실제 빌드 입력은 기존 저장소의 원본 파일입니다.

```text
원본 MapPage.xaml / MapPage.xaml.cs
원본 Map component / service / model
원본 OverlayMiniMapWindow
원본 OverlaySettingsWindow
원본 CustomMarkerEditorWindow
원본 map_configs.json
원본 Assets/DB/Maps/*.svg
원본 marker icons
원본 tarkov_data.db
```

CI에서도 submodule을 checkout한 뒤 이 파일들을 직접 컴파일/배포합니다.

## JunhyunHelper에서 제거한 것

PR #44~#61 동안 만든 JunhyunHelper Map 계층은 이식 전에 제거했습니다.

- Map domain / schema extension
- Quest Map geometry
- online Map marker importer
- Tarkov.dev layout client
- RE3MR / Wiki / Shebuka presentation pipeline
- Map cache / refresh / self-heal
- JunhyunHelper MapPage
- JunhyunHelper coordinate transformer
- JunhyunHelper MiniMap 재구현
- Map-specific tests

따라서 원본 subsystem과 이전 Map 구현이 혼합되지 않습니다.

## 허용된 host adapter

원본 Map 내부 알고리즘은 수정하지 않습니다.

현재 host adapter는 다음 경계만 제공합니다.

- 지도 탭에 원본 `TarkovHelper.Pages.Map.MapPage` 객체를 직접 삽입
- 원본 full-screen 호출을 JunhyunHelper shell에 연결
- legacy settings 저장 root 제공
- 원본 Map DB reader가 사용할 bundled `Assets/tarkov_data.db` 경로 제공
- 원본 XAML이 기대하는 공통 WPF resource 제공
- JunhyunHelper의 stricter compiler와 old source의 기존 warning 정책 차이 처리

## Map tab lifecycle

원본 MapPage는 지도 탭을 처음 열 때 한 번 생성합니다.

```text
Map tab first open
→ new TarkovHelper.Pages.Map.MapPage()
→ MapPlaceholder에 원본 객체 직접 삽입
→ 이후 같은 인스턴스 유지
```

탭 전환만으로 MapPage 인스턴스를 제거하지 않습니다. 기존 Tarkov Helper에서 해결했던 MiniMap lifecycle 회귀를 다시 만들지 않기 위한 host 동작입니다.

## 자동 검증

확인된 checkpoint:

- pre-Map clean reset Desktop build: success
- pre-Map clean reset core tests: 163 passed
- exact Tarkov Helper submodule checkout: success
- original Map/MiniMap source Desktop build: success
- JunhyunHelper core tests with original subsystem linked: success
- Windows x64 self-contained publish / ZIP: success at pre-startup-smoke checkpoint

현재 최종 CI에는 publish된 실제 EXE가 일정 시간 살아 있는지 확인하는 Startup Smoke도 포함합니다.

## 완료 기준

자동 빌드만으로 exact transplant를 완료 처리하지 않습니다.

Windows에서 다음을 확인해야 합니다.

- 지도 탭 UI가 기존 Tarkov Helper MapPage와 동일한 구조로 표시
- 기존 SVG가 그대로 표시
- map/floor/zoom/pan
- quest drawer / extracts / general markers / custom markers
- screenshot current position + heading
- raid Map auto-switch
- MiniMap open/close
- MiniMap player tracking / fixed view / zoom / pan
- floor shared state
- click-through / hotkeys / settings
- 탭 전환 중 MiniMap lifecycle

## 업데이트 대응

업데이트 대응은 exact transplant 검증 후 진행합니다.

향후 Map 업데이트도 Map source/config/SVG/DB를 서로 따로 갱신하지 않고 **검증된 Tarkov Helper revision 단위로 원자적으로 갱신**하는 것이 기본 원칙입니다.
