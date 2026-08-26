# 준현 헬퍼 v1.7.10

기준일: 2026-08-26

## Scanner 공개 배포 환경 대응 강화

v1.7.10은 특정 사용자 PC에 맞춘 보정이 아니라, 공개 배포 Scanner가 다양한 정상 Windows/Tarkov 렌더링 환경에서 더 일관되게 동작하도록 OCR 입력 계층을 hardening한 유지보수 PATCH다.

### 문제

같은 Tarkov 아이템이라도 PC별 해상도, SDR/HDR 또는 tone mapping, gamma/contrast, 렌더링/샤프닝, Windows OCR 환경 차이로 실제 캡처된 item-title 픽셀이 달라질 수 있다.

기존 deep OCR은 고정 luminance preprocessing을 포함하므로, 전체 밝기가 들뜨거나 대비가 눌린 capture에서 동일 글자라도 OCR evidence가 달라질 수 있었다.

### 변경

새 `ScannerTitleEnvironmentNormalizer`는 item-title ROI의 실제 luminance 분포를 분석한다.

- P60: dark title-field background 추정
- P99.75: sparse bright glyph foreground 추정
- usable contrast가 없는 경우 normalization 금지
- reference SDR-like profile은 기존 preprocessing 유지
- lifted / washed / compressed-contrast profile에서만 adaptive normalization 허용

Runtime 순서:

```text
proven normal OCR
→ 성공: 그대로 사용, normalization 분석도 생략
→ 실패: title luminance profile 확인
    → reference/flat: 기존 경로 유지
    → lifted/low-contrast: canonical grayscale normalized retry
→ bounded deep OCR 단계
    → 기존 deep evidence 유지
    → 환경 이상일 때만 normalized 보조 evidence 추가
→ 기존 conservative catalog matching
→ Item ID or fail closed
```

Normalization은 Item identity proof가 아니다. 새 evidence도 기존 official Tarkov Korean item catalog의 동일한 보수적 matcher/ambiguity 기준을 통과해야 한다.

### 환경 변형 회귀

Private external-user image를 요구하지 않고 deterministic procedural title matrix를 추가했다.

검증 범주:

- reference SDR-like luminance
- HDR→SDR-like lifted/washed luminance
- lifted + compressed contrast
- low-contrast gamma/rendering variation
- 1080p-class proportional title raster
- 1440p-class proportional title raster
- 4K-class proportional title raster
- effectively flat/no-contrast negative case

Washed transform의 binary glyph structure가 reference와 99.5% 이상 일치해야 하며, flat input은 contrast를 임의 생성하지 않는다.

### 변경하지 않은 안전 기준

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

또한:

- false positive보다 miss 선호
- header/geometry semantic gate 유지
- catalog matcher acceptance 완화 없음
- visual recovery acceptance 완화 없음
- stale/cross-frame Item identity proof 금지
- scan-time network 없음
- game memory read / DLL injection / packet interception / process hook 없음

### 성능 계약

정상 production normal OCR이 텍스트를 반환하면 luminance histogram/copy/normalized OCR을 전혀 수행하지 않는다.

추가 비용은 normal OCR miss 또는 기존 bounded deep pass이며, 그중에서도 adaptive environment profile이 실제로 감지된 경우로 제한한다.

## 관련 결정

- `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`
