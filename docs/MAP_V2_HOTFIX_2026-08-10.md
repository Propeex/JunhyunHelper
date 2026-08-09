# Map V2 Windows feedback hotfix — 2026-08-10

상태: `MERGED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

## Windows 실사용 피드백

사용자 테스트 빌드에서 다음 문제가 확인되었습니다.

1. 우측 `지도 마커`의 `퀘스트` section에 `퀘스트 마커 표시` checkbox가 보이지 않음.
2. 스크린샷 감시는 시작되지만 스크린샷으로 감지된 Map으로 UI가 전환되지 않음.
3. 새 브랜드 아이콘 요구사항:
   - 사용자가 첨부한 정사각형 얼굴 이미지를 JunhyunHelper EXE 아이콘으로 사용.
   - Main Window 좌측 상단 `준현 헬퍼` 텍스트 왼쪽에도 동일 아이콘 표시.

## 구현

### Quest marker global checkbox

원인:

- V2 product adapter가 원본 top bar의 Quest checkbox를 먼저 숨김.
- 이후 marker section으로 동일 WPF control을 이동했지만 `Visibility.Collapsed` 상태도 그대로 유지됨.
- 결과적으로 `퀘스트` section의 container만 보이고 실제 checkbox는 보이지 않았음.

수정:

- marker section으로 이동한 뒤 `Visibility.Visible`, `IsEnabled=true`, `IsHitTestVisible=true`를 명시적으로 복구.
- label은 `퀘스트 마커 표시`로 고정.
- top bar에서는 노출하지 않음.
- 기존 global Quest marker visibility handler와 V2 per-Quest checkbox state를 그대로 사용.

### Screenshot Map switching

기술적으로 가능한 기능이며 제거하지 않았습니다.

원인:

- screenshot parser가 `MapTracker.CurrentMapKey`를 먼저 갱신한 뒤 `PositionUpdated` event를 발행함.
- V2 bridge는 tracker의 CurrentMapKey가 이미 screenshot MapKey와 같으면 UI도 전환됐다고 판단하고 return함.
- 실제 `CmbMapSelect`와 map artwork는 이전 Map에 남을 수 있었음.

수정:

- tracker 내부 값이 아니라 실제 Map selector의 selected item과 screenshot MapKey를 비교.
- selector가 다르면 해당 Map item을 찾아 `SelectedIndex`를 변경하여 기존 Map loading path를 실행.
- tracker가 아직 다를 때만 `SetCurrentMap`을 보조 호출.
- floor는 계속 screenshot으로 판정/전환하지 않음.

### Application icon

- 사용자가 첨부한 정사각형 얼굴 이미지를 스타일 변경 없이 축소한 PNG를 application brand source로 사용.
- Main Window header의 `준현 헬퍼` 왼쪽에 동일 PNG를 28px로 표시.
- WPF Window icon에도 동일 resource 사용.
- Windows build 단계에서 해당 PNG로 ICO resource를 생성하여 `JunhyunHelper.exe` application icon으로 embed.
- 이미지 생성 AI를 사용하지 않음.

## Git / 검증

```text
PR #65: Fix Map V2 Quest toggle, screenshot switching, and app icon
merge commit: 480a49ce7df5f1a17ca91d1caecbb6a81451811a
final PR head: ecde6d5167f051f53f88ef2b557240a61909e4d4
final CI: 31324134472
```

검증 결과:

- Desktop Release build: success
- automated tests: success
- Windows x64 self-contained publish: success
- published EXE Startup + Map smoke: success
- ZIP creation/upload: success

첫 JPEG intermediate asset은 CI image decoder에서 손상으로 판정되어 폐기했습니다. 사용자 첨부 원본에서 PNG를 다시 생성하고 build/publish/startup 검증을 처음부터 재통과했습니다.

## Windows 사용자 검증 항목

- `지도 마커 > 퀘스트`에 `퀘스트 마커 표시` checkbox와 label이 정상 표시되고 toggle 동작.
- screenshot MapKey가 현재 선택과 다르면 Map selector가 자동 전환.
- screenshot switching이 floor selection을 자동 변경하지 않음.
- published `JunhyunHelper.exe`에 새 icon 표시.
- Main Window title/icon 및 `준현 헬퍼` 왼쪽 brand icon 표시.
