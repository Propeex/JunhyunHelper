# DECISION — v1.7.9 Mini Scanner confirmed-item presentation

상태: `IMPLEMENTING`

기준일: 2026-08-26

## 1. 사용자 실사용 결함

v1.7.8 실사용에서 Scanner 사용자 로그에는 아이템 인식 성공이 기록되지만 Mini Scanner 창이 열리지 않는 현상이 확인되었다.

이 증상은 recognition과 presentation이 분리되어 있기 때문에 가능했다.

- Scanner runtime은 상세창과 Item identity를 정상 확정한다.
- `semantic-selected` / 성공 activity가 기록된다.
- `MiniScannerOverlayService.Show(snapshot)`까지 호출된다.
- 그러나 hidden Mini Scanner의 initial show에서 별도의 inventory/stash top-band OCR gate가 다시 실행된다.
- 이 auxiliary OCR이 `장비`, `건강상태`, `스킬`, `지도`, `종합정보` 계열 중 2개 이상을 인식하지 못하면 이미 확정된 Item도 표시하지 않는다.

따라서 사용자에게는 "인식 성공인데 Mini Scanner가 안 뜸"으로 보인다.

## 2. Root cause

Mini Scanner initial visibility가 authoritative Scanner Item identity보다 약한 별도 OCR에 의존한 것이 원인이다.

기존 구조:

```text
Scanner semantic success
  -> Item ID 확정
  -> presentation snapshot 생성
  -> MiniScannerOverlayService.Show(snapshot)
  -> 별도 inventory/stash top-band OCR
      -> anchor >= 2: Show
      -> anchor < 2: hidden 유지
```

이 top-band OCR은 Item identity proof가 아니며, 레이드 UI 배치/가림/한글 OCR variation에 따라 실패할 수 있다.

## 3. 제품 동작 결정

확정된 Item presentation의 권위는 Scanner semantic success에 둔다.

새 구조:

```text
Scanner semantic success
  -> Item ID 확정
  -> presentation snapshot 생성
  -> MiniScannerOverlayService.Show(snapshot)
      -> preview/display-test: Show
      -> already visible: authoritative Item result로 즉시 update
      -> hidden real Scanner:
           Tarkov foreground yes -> Show
           Tarkov foreground no  -> fail closed / hidden 유지
```

즉 auxiliary inventory-header OCR은 Mini Scanner 표시를 veto하지 않는다.

## 4. 안전성

초기 hidden Mini Scanner가 다른 앱 위에 갑자기 나타나는 것은 방지한다.

real Scanner의 initial show에는 다음 guard를 유지한다.

- `EscapeFromTarkov` main window 존재
- 실제 Tarkov window가 foreground
- minimized 아님
- visible window

이 검사는 화면/윈도 상태만 사용하며 게임 메모리, DLL injection, packet interception을 사용하지 않는다.

## 5. 기존 presentation stability 유지

v1.7.2의 sticky presentation 계약은 유지한다.

- 성공한 동일 Item -> 계속 표시 / miss budget reset
- 성공한 새 Item -> 즉시 교체 / miss budget reset
- 실제 miss #1 -> 마지막 정상 Item 유지
- 실제 miss #2 -> 마지막 정상 Item 유지
- 실제 miss #3 -> Hide
- progress-only 상태는 miss로 세지 않음

## 6. Recognition 안전 계약 — 변경 금지

이번 수정은 presentation-only hotfix다.

다음은 변경하지 않는다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
```

또한 다음을 유지한다.

- false positive보다 miss 선호
- OCR/matcher/visual acceptance 유지
- cross-frame Item identity proof 재사용 금지
- Item ID 확정 전 mapped price/needed data를 identity proof로 사용 금지
- scan-time network 없음
- game memory/DLL injection/packet interception/process hook 없음
- 200ms continuous observation target 유지

## 7. 회귀 검증

Product smoke에 Mini Scanner confirmed-item initial visibility policy를 추가한다.

반드시 만족해야 한다.

- explicit preview는 foreground 여부와 무관하게 허용
- display-test는 real Scanner enabled가 아니므로 허용
- real Scanner + foreground Tarkov는 허용
- real Scanner + non-foreground Tarkov는 거부

기존 rendered Mini Scanner smoke도 유지한다.

- 실제 Window Render 성공
- Topmost 유지
- ShowActivated=false
- taskbar 미노출
- Item icon/name 표시
- 가격/필요 개수 정보 순서 유지

## 8. 릴리즈

릴리즈 후보: `v1.7.9`

릴리즈 조건:

1. final PR HEAD Desktop Release build 성공
2. 전체 automated tests 성공
3. Windows x64 publish 성공
4. Product UI / Scanner / Map / MiniMap smoke 성공
5. package verification 성공
6. main merge 후 main CI 성공
7. stable GitHub release 생성 및 `/releases/latest` readback 성공
