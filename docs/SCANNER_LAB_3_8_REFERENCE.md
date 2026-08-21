# Scanner Lab 3.8 — Recognition Reference

기준일: 2026-08-21

## 목적

사용자가 보존하고 있던 `TarkovHelper-ScannerLab-v3.8` 소스를 회수하여 현재 Scanner의 recognition 회귀를 교정하는 기준으로 삼는다.

이 문서는 Scanner Lab 전체를 제품에 다시 넣기 위한 문서가 아니다. **v3.8에서 실제로 검증된 화면 인식 구조를 JunhyunHelper Scanner에 보존하는 공식 reference**다.

## v3.8에서 확인된 핵심 구조

v3.8은 하나의 geometry rectangle을 즉시 inspect window로 확정하지 않았다.

```text
capture
→ red-X connected components
→ red-X anchored structural candidates
+
→ edge-projection rectangle candidates
→ IoU deduplication
→ 최대 8개 candidate title ROI
→ 1차 enlarged OCR
→ catalog resolver
→ 필요 시 상위 3개 candidate에 4종 OCR preprocessing
→ catalog resolver
→ official item name으로 안전하게 resolve된 candidate만 inspect window로 채택
```

즉 **structural score는 후보 순위일 뿐 최종 사실 판정이 아니다.**

## 검증된 geometry

v3.8 사용자 실험 자료에서:

- cropped `Ophthalmoscope 검안경` image: outer inspect 약 `x=3 y=3 w=672 h=514`, score 약 `0.997`
- full `Water 0.6L 물병` screenshot: inspect 약 `x=622 y=282 w=674 h=514`, score 약 `0.992`

두 샘플 모두 aspect ratio는 약 `1.30`이다.

### red-X path

- dark-red connected component 후보
  - `r >= 45`
  - `r-g >= 20`
  - `r-b >= 20`
- close component broad geometry filter
- close component에서 우측 border를 찾음
- 우측 border를 아래로 따라가 window height 추정
- aspect `1.05~1.55`, 중심 `1.30`으로 left border 탐색
- top/bottom border refinement
- four-border continuity × aspect score

### rectangle fallback

- vertical/horizontal edge projection 상위 line 후보
- rectangle aspect `1.05~1.58`
- border continuity
- aspect score
- interior darkness
- optional red-X proximity bonus
- structural floor `0.34`

## v3.8 title ROI

```text
titleX = window.Left + window.Width * 0.032
titleY = window.Top - 1
titleWidth = window.Width * 0.64
titleHeight = max(12, window.Height * 0.052)
```

따라서 약 `674x514` inspect window에서 title ROI는 약 `431x27`이다.

## v3.8 OCR

candidate title height에 따라 확대:

- `<=14px`: 8x
- `<=20px`: 6x
- 그 외: 4x

1차는 확대 원본 OCR.

1차 후보들에서 공식 item resolution이 모두 실패한 경우 상위 3개에:

1. original
2. high-contrast grayscale
3. binary white-on-black
4. inverse black-on-white

OCR을 수행한다.

OCR line은 개별 line뿐 아니라 이웃 두 line을 합친 candidate도 resolver에 전달한다.

## v3.8 semantic candidate validation

- Candidate limit: 8
- Deep OCR candidate limit: 3
- Structural floor: 0.34
- structural + semantic combined ranking
- official item name resolver를 통과하지 못한 geometry candidate는 최종 inspect window가 아님
- verified candidate의 title hash가 동일하면 OCR 반복 안 함

## 현재 회귀 원인

JunhyunHelper 통합 과정에서 위 구조가 다음처럼 단순화되었다.

```text
fixed-ratio geometry search
→ geometry 최고점 1개 확정
→ title ROI 1개
→ OCR 1개
→ matcher
```

이 과정에서 v3.8의:

- red-X anchored search
- rectangle fallback
- multiple candidates
- semantic candidate validation
- adaptive 4x/6x/8x title scaling
- deep multi-preprocessing OCR

이 빠졌다.

따라서 현재 문제는 matcher threshold를 낮출 문제가 아니라 **검증된 v3.8 recognition architecture를 현재 Scanner 경계에 맞게 복원해야 하는 integration regression**으로 취급한다.

## 복원 원칙

- v3.8 소스를 그대로 PowerShell로 실행하지 않는다.
- detector 알고리즘을 Core C#으로 이식한다.
- capture는 현재 Tarkov-window / Display Test infrastructure를 유지한다.
- OCR은 현재 replaceable `IScannerOcrEngine` 경계를 유지하되 v3.8의 scale/preprocessing 전략을 복원한다.
- current official Korean catalog와 현재 matcher를 유지한다.
- historical test alias는 production에 추가하지 않는다.
- Item ID 이후 JunhyunHelper data bridge / Mini Scanner / activity UI는 유지한다.
- fail-closed 원칙을 유지한다.
