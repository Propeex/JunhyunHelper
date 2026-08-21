# DEC-052 — Scanner 운용 UI와 Mini Scanner 직접 이동

기준일: 2026-08-21

상태: **CONFIRMED / IMPLEMENTING FOR v1.1.1**

## 사용자 확정 요구

Scanner 탭은 기능 설명을 읽는 화면이 아니라 실제 Scanner를 켜고 상태를 확인하는 운용 화면이어야 한다.

- 상단 Scanner 제목/설명문을 제거한다.
- `스캐너`, `테스트` 버튼의 상시 설명문을 제거한다.
- 전체 아이템 카탈로그 수동 갱신 설명문을 제거한다.
- Mini Scanner 설명문을 제거한다.
- Foundation 검증 도구는 사용자 화면에서 숨긴다. 개발에 유용한 내부 preview 경로는 유지할 수 있다.
- 별도 Mini Scanner 위치 편집/초기화 UI를 제거한다.
- Mini Scanner는 보이는 동안 언제든 직접 드래그해 이동할 수 있어야 한다.
- 상단 bar 왼쪽에 Scanner/Test 버튼, 오른쪽에 아이템 목록 수동 갱신 버튼을 배치한다.
- 수동 갱신 버튼의 사용자명은 **`아이템 목록 최신화`**로 한다.
- bar 아래에는 표시 정보 체크박스를 둔다.
- 화면 하단에는 최근 Scanner 판정을 사용자가 이해할 수 있는 형태로 표시한다.

## 최근 인식 기록 계약

사용자용 최근 기록은 개발자 로그 원문을 노출하지 않는다.

각 Item 식별 시도에서 다음을 보여준다.

- 시각
- 실사용/테스트 모드
- OCR로 읽은 문자열
- 가장 가까운 공식 아이템 후보
- top-1 유사도
- top-1 / top-2 차이
- 최종 `식별 성공` 또는 `식별 보류`
- exact/fuzzy/low-confidence 등의 판단 이유를 한국어로 정리한 보조 정보

예시 의미:

```text
화면에서 ‘들격소총’을 읽었고 ‘돌격소총’과 94.4% 일치해 해당 아이템으로 판단했습니다.
```

사용자용 기록은 메모리에서 bounded history로 유지한다. 기존 `%LocalAppData%/JunhyunHelper/logs/scanner.log`는 capture/runtime/OCR/matcher 상세 진단용으로 별도 유지한다.

## Mini Scanner 입력 정책 변경

기존 v1.1.0의 play-mode click-through 정책은 **Mini Scanner 직접 이동 요구에 한해 부분적으로 supersede**한다.

항상 드래그하려면 Mini Scanner Window가 자기 영역의 마우스 hit-test를 받아야 한다. 따라서 v1.1.1부터:

- `Topmost`: 유지
- `ShowActivated=false`: 유지
- `WS_EX_NOACTIVATE`: 유지
- `WS_EX_TOOLWINDOW`: 유지
- `WS_EX_TRANSPARENT` click-through: 제거
- Mini Scanner가 보이는 동안 왼쪽 마우스 드래그로 즉시 이동 가능
- 드래그 완료 좌표를 기존 atomic Scanner settings에 저장
- 음수 multi-monitor 좌표 허용

이 변경으로 Mini Scanner는 자신의 작은 표시 영역에서 마우스 클릭을 받지만 게임 창의 키보드 포커스를 가져가지 않는다.

## Foundation 도구

`ShowPreviewAsync` 등 Item ID → presentation 내부 검증 경로는 개발/회귀 진단에 유용하므로 제거하지 않는다. 다만 Scanner 사용자 탭에는 Foundation/preview UI를 노출하지 않는다.

## 버전

DEC-048에 따라 신규 Scanner 기능 추가가 아니라 기존 v1.1.0 Scanner의 UI/사용성 보완이므로 **v1.1.1 PATCH**로 배포한다.

## 관련 문서

- `docs/SCANNER.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/PRODUCT.md`
- `docs/STATE.md`
- `docs/DECISIONS.md` active index에는 v1.1.1 finalization 시 DEC-052를 추가한다.
