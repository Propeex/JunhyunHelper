# FEEDBACK — MiniMap 층 변경 시 지도 중심 초기화

기록일: **2026-08-15**

상태: `ROOT CAUSE CONFIRMED / FIX IMPLEMENTED / VALIDATION IN PROGRESS`

## 사용자 실사용 피드백

MiniMap에서 보고 있던 위치가 있는 상태에서 층을 변경하면 지도 중심이 기존 위치를 유지하지 않고 초기/이전 위치로 돌아가는 현상이 확인되었습니다.

## 원인

MiniMap은 제품 정책상 `PlayerTracking` 모드로 사용합니다. 이 모드에서 플레이어 추적에 따른 실제 현재 중심은 legacy `CenterOnPlayer()`가 `MapTranslate.X/Y`에만 반영하고, persisted `_settings.MapOffsetX/Y`에는 쓰지 않습니다.

```text
PlayerTracking live center
→ MapTranslate.X/Y 갱신
→ MapOffsetX/Y는 이전 값 유지
```

그런데 기존 floor 변경은 SVG artwork를 교체한 뒤 `UpdateMapView()`를 호출했고, 이 메서드는 live transform이 아니라 `_settings.MapOffsetX/Y`를 다시 `MapTranslate`에 적용했습니다.

```text
현재 PlayerTracking 중심
→ floor SVG 교체
→ UpdateMapView()
→ stale MapOffsetX/Y 적용
→ MiniMap 중심이 이전/초기 위치로 점프
```

## 수정

JunhyunHelper가 MiniMap floor 변경을 별도 viewport-safe async 경로로 소유합니다.

1. floor 변경 직전 실제 `MapScale`과 `MapTranslate`에서 현재 zoom + viewport 중앙의 map-space 좌표를 캡처합니다.
2. 현재 live transform을 `_settings.MapOffsetX/Y`에도 동기화하여 legacy floor renderer가 중간에 stale 위치로 점프하지 못하게 합니다.
3. floor SVG 교체와 marker refresh 시작을 await합니다.
4. layout이 안정된 뒤 같은 zoom + 같은 map-space 중심을 복원합니다.
5. 복원된 live transform과 persisted offset도 다시 일치시킵니다.

적용 경로:

- floor up product hotkey
- floor down product hotkey
- NumPad 0~5 direct floor selection compatibility

지도 자체의 Map 변경이나 새로운 screenshot player position은 정상적으로 중심을 변경할 수 있습니다. **floor 변경만으로 현재 MiniMap viewport를 초기화해서는 안 됩니다.**

## 자동 회귀 검증

실제 WPF MiniMap runtime에서 PlayerTracking의 stale-settings 상황을 직접 재현합니다.

```text
live MapTranslate = 현재 유효한 중심
settings MapOffsetX/Y = 의도적으로 다른 stale 값
→ product floor 변경
→ floor 실제 변경 확인
→ zoom 동일
→ viewport 중앙의 map-space X/Y 동일
→ 최종 settings MapOffsetX/Y == live MapTranslate
```

기존 Main Map viewport 보존, Factory extract, off-floor marker, MiniMap marker-scale smoke도 함께 유지합니다.

## 완료 조건

- Windows Release build 성공
- 전체 automated tests 통과
- MiniMap floor viewport direct regression smoke 통과
- Main Map floor viewport smoke 유지
- 타층 일반 marker async-settle smoke 유지
- Factory / MiniMap 기존 regression smoke 유지
- 최종 diff review 후 v0.1.5 패치 릴리즈
