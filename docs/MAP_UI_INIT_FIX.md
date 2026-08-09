# MAP UI INIT FIX — 지도 탭 초기화 오류 수정

기록일: **2026-08-09**

상태: `IMPLEMENTED / VALIDATION IN PROGRESS`

## 현상

Windows 실사용에서 앱 자체는 정상 실행되지만 지도 탭을 처음 열 때 다음 예외가 반복 발생했습니다.

```text
System.NullReferenceException
at JunhyunHelper.Desktop.Map.MapPage.RenderCurrentMap()
at JunhyunHelper.Desktop.Map.MapPage.MarkerToggle_Changed(...)
```

동일 예외가 매우 짧은 시간에 여러 번 발생했습니다.

## 원인

`MapPage.xaml`의 대부분의 marker CheckBox에 `IsChecked="True"`가 선언되어 있었습니다.

WPF XAML loader는 `InitializeComponent()`가 아직 전체 visual tree를 만들고 있는 도중에도 `IsChecked=True`가 적용되면 `Checked` routed event를 발생시킬 수 있습니다.

따라서 다음 순서가 가능했습니다.

```text
MapPage.InitializeComponent()
→ 앞쪽 marker CheckBox 생성
→ IsChecked=True 적용
→ MarkerToggle_Changed
→ RenderCurrentMap
→ 뒤쪽 MarkerCanvas / NoMapPanel 등 아직 미생성
→ NullReferenceException
```

로그에서 예외가 여러 번 연속 발생한 이유도 default-ON marker CheckBox가 순서대로 생성되며 같은 handler를 호출했기 때문입니다.

## 수정

- XAML에서 marker CheckBox의 declarative `IsChecked` 값을 모두 제거했습니다.
- CheckBox 초기 상태의 authority는 `MapUserSettings` 하나로 통일합니다.
- `EnsureInitializedAsync()`가 사용자 설정을 읽은 뒤 `ApplySettingsToCheckboxes()`에서 상태를 적용합니다.
- 이 적용 구간은 기존 `_applyingUi` guard 안에서 실행되므로 Checked/Unchecked handler가 render/save를 재진입하지 않습니다.
- 기존/손상된 `map-settings.json`이 explicit `null` dictionary를 갖더라도 기본값으로 복구하도록 preference load normalization도 추가했습니다.

## 기대 결과

- 지도 탭 최초 생성 중 marker toggle event가 premature render를 호출하지 않음
- 지도 탭 최초 진입 NRE 제거
- 기존 marker 기본값 및 사용자가 저장한 marker visibility는 그대로 유지
- corrupt/old map preference가 UI crash를 유발하지 않음
