# Map V2 Windows feedback hotfix — 2026-08-10

상태: `USER CONFIRMED / IMPLEMENTATION IN PROGRESS`

## Windows 실사용 피드백

사용자 테스트 빌드에서 다음 문제가 확인되었습니다.

1. 우측 `지도 마커`의 `퀘스트` section에 `퀘스트 마커 표시` checkbox가 보이지 않음.
2. 스크린샷 감시는 시작되지만 스크린샷으로 감지된 Map으로 UI가 전환되지 않음.
3. 새 브랜드 아이콘 요구사항:
   - 사용자가 첨부한 정사각형 얼굴 이미지를 JunhyunHelper EXE 아이콘으로 사용.
   - Main Window 좌측 상단 `준현 헬퍼` 텍스트 왼쪽에도 동일 아이콘 표시.

## 구현 기준

### Quest marker global checkbox

- `퀘스트 마커 표시`는 `지도 마커 > 퀘스트` section에 정상적인 checkbox row로 항상 표시되어야 함.
- top bar에서는 노출하지 않음.
- 기존 global Quest marker visibility handler와 V2 per-Quest checkbox state를 그대로 사용함.

### Screenshot Map switching

- 기술적으로 가능한 기능이므로 제거하지 않음.
- screenshot parser가 MapKey를 제공하면 Main Map selector와 MapTracker current map을 해당 Map으로 동기화함.
- 이미 tracker의 CurrentMapKey가 먼저 바뀐 경우에도 UI selector 전환을 건너뛰지 않아야 함.
- floor는 계속 자동 판정/전환하지 않음.

### Application icon

- 사용자 첨부 원본을 내용 변경 없이 icon asset으로 사용.
- Windows EXE에는 multi-size `.ico`를 embed.
- Main Window header에는 동일 source의 PNG를 표시.
- 이미지 생성 AI를 사용하지 않음.

## 검증 기준

- `지도 마커 > 퀘스트`에 checkbox와 label이 보이고 toggle 동작.
- screenshot MapKey가 현재 선택과 다르면 Map selector가 자동 전환.
- screenshot switching이 floor selection을 변경하지 않음.
- published `JunhyunHelper.exe`에 새 icon embed.
- Main Window `준현 헬퍼` 왼쪽에 icon 표시.
- Desktop Release build / tests / Windows x64 publish / Startup + Map smoke 통과.
