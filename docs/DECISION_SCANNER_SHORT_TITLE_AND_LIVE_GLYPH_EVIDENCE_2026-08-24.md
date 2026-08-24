# DECISION — v1.4.3 live OCR Ground Truth: short titles and glyph evidence

상태: `CONFIRMED`

기준일: 2026-08-24

## 근거

사용자가 public v1.4.3을 실제 Scanner 테스트에 사용한 뒤 `ScannerDiagnostics_2026-08-24.zip`을 제출했다.

Dataset 요약:

- 전체 Case: 7
- user-reviewed: 4
- reviewed final: 4
- program correct: 3
- reviewed accuracy: 75%
- reviewed error: OCR_RECOGNITION 1
- 화면: 2560x1600
- Windows system DPI: 144

이번 데이터에서는 v1.4.3의 rectangle proposal / semantic header 구조가 이전보다 안정적으로 동작했다. 확인된 주된 잔여 실패는 detail/header가 아니라 title OCR이다.

## 확인된 Case

### Awl

Reviewed Case:

`case_20260824004232199_000003`

```text
Ground Truth: Awl
pipeline: OCR_OR_PREPROCESSING_FAILED
reason: EMPTY_OCR
structural confidence: 0.972684...
structural reason: RED_X_CANDIDATE
header confidence: 0.818829...
header reason: HEADER_FRAME_LOCKED
title ROI: 985x28
```

상세창과 title ROI는 정상적으로 잠겼다. 실제 title ROI에서 `Awl` glyph는 좌측 약 30px 범위에 명확히 존재하지만 나머지 대부분이 dark trailing background다. original/deep WinRT OCR 모두 빈 문자열을 반환했다.

따라서 이 Case는 geometry/header/matcher 실패가 아니라 **wide sparse title ROI에 대한 OCR 입력 문제**로 분류한다.

결정:

- semantic title ROI 자체는 변경하지 않는다.
- 사용자 Ground Truth 좌표와 field ownership도 변경하지 않는다.
- OCR이 실패한 경우에만 OCR 전용 tight view를 만들 수 있다.
- tight view는 왼쪽과 전체 높이를 그대로 보존하고, 실측 bright glyph evidence 뒤의 긴 dark trailing background만 제거한다.
- 실제 ink가 title field 전반에 걸쳐 있거나 foreground evidence가 부족하면 crop하지 않는다.
- global OCR threshold나 matcher short-name threshold를 낮추지 않는다.

## r 계열 glyph

동일 dataset에서 WinRT OCR이 lowercase `r`을 일본어 corner bracket 형태 `「`로 출력한 실제 증거가 반복 확인됐다.

예:

```text
Esmarch -> Esma「ch
figurine -> figu「ine
```

v1.4.3 current-catalog alphabet policy는 `「`를 official catalog identity character로 신뢰하지 않고 embedded unknown-glyph evidence `?`로 보존할 수 있다. 실제 reviewed Esmarch Case는 최종적으로 올바른 Item ID를 복구했다.

결정:

- `「 -> r` 같은 global substitution table은 추가하지 않는다.
- raw OCR은 forensic evidence이므로 원문 그대로 보존한다.
- catalog-impossible embedded glyph는 계속 unknown position evidence로 취급한다.
- matcher는 current official catalog의 unique/separated evidence가 있을 때만 복구한다.
- 추가 실패 GT가 생기면 `r` 전용 추측 치환보다 unknown-glyph path의 우선순위/coverage를 먼저 검토한다.

## o / O

사용자 실사용에서 lowercase `o`가 uppercase `O`로 인식되는 현상이 관찰됐다.

현재 Scanner identity matcher의 canonical normalization은 Latin text를 invariant lowercase로 변환하므로 `o`와 `O`의 차이는 Item ID 판정에서 동일하다.

결정:

- `o -> O` 또는 `O -> o` 치환 규칙을 추가하지 않는다.
- raw OCR은 원문 case를 보존한다.
- matching은 기존 case-insensitive canonical identity를 유지한다.
- 이 계약을 자동 회귀 테스트로 고정한다.

## 구현 원칙

이번 PATCH의 목표 pipeline:

```text
trusted semantic title ROI
→ normal OCR
→ success이면 기존 path
→ 실패 + sparse trailing-background evidence
→ OCR-only tight title view
→ normal/deep OCR 재시도
→ current catalog validation
→ 기존 fail-closed matcher
```

불변:

- `HEADER_FRAME_LOCKED >= 0.68`
- false positive보다 miss 선호
- current official catalog authority
- short-name fuzzy threshold 일반 완화 없음
- arbitrary r/0/o/O replacement 없음
- scan-time network 없음
- game memory/DLL injection/packet interception 없음

## 회귀 요구

- Awl-like 985x28 sparse synthetic field가 trailing-background crop plan을 생성해야 한다.
- 마지막 glyph 뒤 padding을 보존해야 한다.
- far-right isolated noise가 crop을 무력화하면 안 된다.
- title ink가 넓게 분포하면 crop하지 않아야 한다.
- Latin case-only OCR 차이는 canonical exact identity로 취급해야 한다.
- 기존 Scanner tests는 모두 유지하며 regression 0을 요구한다.

Scanner 속도 최적화는 정확도 안정화 이후 작업으로 계속 보류한다.
