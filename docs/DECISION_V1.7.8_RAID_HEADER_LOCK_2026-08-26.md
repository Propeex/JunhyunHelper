# v1.7.8 Raid Scanner Header Lock 유지보수 결정

기준일: 2026-08-26 KST

## 결론

레이드 중 Scanner 인식 저하는 OCR 엔진이나 item-name ROI 검출 자체의 문제가 아니라, **inspect header semantic lock의 수평 소유권 오류**로 확인했다.

v1.7.8은 기존 인식 기준을 완화하지 않고, 레이드 인벤토리 UI의 중립색 수평선이 상세보기 header와 시각적으로 이어질 때 잘못 확장되던 header left ownership만 교정한다.

사용자 UX 결정으로 `현재 결과 교정`은 Scanner 고급 창이 아니라 메인 상단에 배치한다.

## 사용자 실사용 증거

사용자가 제공한 `JunhyunHelper-Scanner-Diagnostics-20260826-032555.zip`의 reviewed Case 8건을 분석했다.

- 6건: 사용자 교정 Ground Truth
- 2건: 프로그램 결과가 Ground Truth와 일치
- 실패 6건 모두 detail rectangle과 item-name ROI proposal은 정상
- 실패 6건 모두 `close_button = null`, `magnifier = null`
- header reason은 `HEADER_CLOSE_NOT_LOCKED`
- recognition reason은 `TITLE_ANCHOR_INCOMPLETE`
- raw OCR은 empty

따라서 사용자 화면에서는 텍스트 인식 실패처럼 보였지만 실제 실패 지점은 OCR 호출 이전 semantic header gate였다.

실제 full image에는 실패 6건 모두 빨간 X와 돋보기가 존재했다.

## Root cause

기존 live header fallback은 빨간 X 주변의 긴 neutral horizontal run을 inspect header로 사용한다.

레이드 인벤토리에서는 주변 UI의 수평선이 inspect header와 같은 높이에서 이어져 보여 하나의 긴 run으로 합쳐지는 사례가 있었다. 이 경우 fallback이 run의 가장 왼쪽 점을 header left로 사용하여 실제 상세창보다 왼쪽을 소유했다.

reviewed 실패 Case에서 관측한 left drift:

```text
001098: -132 px
001099:  -84 px
001100: -125 px
001101: -125 px
001102: -125 px
001103:  -47 px
```

정상 Case에서는 drift가 사실상 없었다.

이 잘못된 frame left에서 magnifier 예상 위치를 계산하면서 실제 돋보기 lane도 동일하게 왼쪽으로 밀렸고, 결과적으로 magnifier semantic evidence를 찾지 못해 OCR 단계에 진입하지 못했다.

## 수정 설계

기존 인식 경로의 우선순위는 유지한다.

```text
existing primary header lock
→ existing live Ground Truth recovery
→ v1.7.8 raid ownership recovery
→ existing contained-subpanel recovery
→ fail closed
```

새 recovery는 기존 경로가 실패한 경우에만 사용한다.

진입 조건:

- candidate reason = `RED_X_CANDIDATE`
- structural score >= `0.90`

coarse detail rectangle은 **header left ownership proposal**에만 사용하며 Item identity evidence로 사용하지 않는다.

새 recovery도 다음 evidence를 모두 독립적으로 요구한다.

- red close component
- existing close-X template score >= `0.40`
- close relation evidence >= `0.60`
- candidate-owned neutral header frame score >= `0.74`
- live magnifier template >= `0.54`
- magnifier relation evidence >= `0.66`
- dark title field >= `0.58`
- title text evidence >= `0.22`
- final `HEADER_FRAME_LOCKED` score >= `0.68`

특히 magnifier는 bleed된 horizontal run의 left가 아니라, 이미 검출된 item-name title proposal 바로 앞이라는 실제 UI 관계를 사용해 좁은 search lane을 구성한다.

## Reviewed evidence 대조

사용자 이미지 픽셀을 기준으로 동일한 evidence 계산을 대조한 결과 8 reviewed Case 모두 기존 `0.68` semantic floor를 넘는 header evidence를 보였다.

```text
001097: 0.835
001098: 0.794
001099: 0.838
001100: 0.815
001101: 0.795
001102: 0.817
001103: 0.794
001104: 0.838
```

실패 Case에서 close evidence는 0.631 이상, magnifier evidence는 0.667 이상이었다.

이 값은 threshold를 새로 낮춰 얻은 결과가 아니라, 잘못된 header-left ownership을 실제 상세창 경계에 맞춘 뒤 기존 semantic evidence를 재계산한 결과다.

사용자 Ground Truth 이미지 자체는 저장소에 커밋하지 않는다.

## 회귀 보호

CI Product smoke에 procedural raid case를 추가한다.

positive smoke:
- inspect panel보다 훨씬 왼쪽까지 이어지는 neutral line 구성
- red close + magnifier + dark title field + text evidence 존재
- `HEADER_FRAME_LOCKED >= 0.68` 요구

negative smoke:
- 동일 geometry에서 red close 제거
- recovery는 반드시 fail closed

실제 사용자 8건은 로컬 reviewed evidence이므로 공개 저장소/CI artifact에 이미지를 포함하지 않는다. 따라서 CI의 procedural smoke와 사용자 픽셀 evidence 대조를 구분해 기록한다.

## 변경하지 않는 Scanner 계약

- structural floor `0.34`
- trusted `HEADER_FRAME_LOCKED` floor `0.68`
- continuous candidate cap `8`
- one-shot candidate cap `12`
- OCR variants / Windows OCR behavior
- catalog matcher acceptance
- visual recovery acceptance
- 200ms continuous observation target
- false positive보다 miss 우선
- cross-frame identity proof 금지
- scan-time network 없음
- game memory read / DLL injection / packet interception / process hook 없음

## UX 결정

Scanner 메인 상단 primary actions를 다음 순서로 한다.

```text
스캐너 ON/OFF
설정
고급
현재 결과 교정
```

`현재 결과 교정`은 현재 메모리에 보존된 exact Scanner frame을 기존 `ScannerCorrectionWindow`로 연다.

고급 창에서는 해당 중복 버튼을 제거하고 다음 기능만 유지한다.

- 테스트 스캐너
- 교정 데이터 관리
- Scanner 성능 진단 자료 내보내기

## 릴리즈

이 변경은 기존 제품 기능의 실사용 회귀를 수정하는 PATCH이므로 목표 버전은 `v1.7.8`이다.

병합/공개 조건:

- final PR Windows Desktop build success
- 전체 자동 테스트 success
- Windows x64 publish success
- Product UI / Scanner / Map / Factory / MiniMap smoke success
- graceful shutdown success
- release package/checksum verification success
- main push CI success
- stable release workflow success
- public release tag/asset/hash readback verification
