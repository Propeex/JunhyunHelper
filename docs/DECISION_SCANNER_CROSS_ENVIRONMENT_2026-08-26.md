# DECISION — Scanner cross-environment public reliability

기준일: 2026-08-26
상태: PRODUCT REQUIREMENT CONFIRMED

## 1. 사용자 결정

준현 헬퍼 Scanner는 특정 사용자 PC, 특정 해상도, 특정 HDR/SDR 설정 또는 특정 렌더링 환경에 맞춘 도구가 아니라 공개 배포 가능한 범용 제품이어야 한다.

친구/외부 사용자의 Ground Truth 또는 교정 데이터 제공을 제품 정상 동작의 전제조건으로 두지 않는다.

공개 stable Scanner는 별도 수동 튜닝 없이 다양한 정상 Windows/Tarkov 환경에서 가능한 한 일관되게 동작해야 한다.

## 2. 지원해야 할 환경 변화

Scanner는 최소한 다음 variation에 대한 환경 의존도를 줄이는 방향으로 설계한다.

- 1920x1080 / 2560x1440 / 3840x2160 등 일반적인 해상도 변화
- Windows display scaling / DPI variation
- SDR / HDR / Auto HDR / tone-mapping 차이
- native / DLSS / FSR 및 sharpening 등 Tarkov 렌더링 차이
- GPU/driver에 따른 미세한 픽셀 렌더링 차이
- Windows ko-KR OCR backend variation
- Borderless 환경에서의 capture-path variation

이 목록은 환경별 예외 분기 목록이 아니다. 목표는 가능한 한 입력을 canonical representation으로 정규화해 downstream recognition의 환경 민감도를 낮추는 것이다.

## 3. 설계 방향

금지하는 접근:

```text
if HDR -> threshold A
if 1440p -> threshold B
if 특정 GPU -> threshold C
```

목표 구조:

```text
Tarkov pixels
→ capture-path/environment observation
→ color/luminance normalization
→ detail/header semantic lock
→ item-title ROI
→ scale/glyph-size normalization
→ local contrast/background normalization
→ bounded OCR variants
→ conservative catalog matching
→ optional current-pixel visual corroboration
→ Item ID or fail closed
```

환경 차이는 identity proof가 아니다. 환경 보정은 입력 canonicalization에만 사용하며 false-positive safety floor를 낮추지 않는다.

## 4. OCR/normalization 원칙

- 고정 absolute luminance threshold에 대한 의존도를 줄인다.
- ROI 내부 통계에 기반한 percentile/local contrast/adaptive threshold 후보를 검토한다.
- 원본 title pixel evidence는 diagnostics용으로 보존한다.
- normalization 결과는 bounded deterministic variants로 제한한다.
- normalization 자체가 Item identity를 결정하지 않는다.
- 기존 official Tarkov Korean item catalog가 identity authority다.
- ambiguous result는 fail closed한다.

## 5. Capture/HDR 원칙

현재 PrintWindow + screen-copy fallback 경로가 HDR/WCG 환경에서 실제 사용자 화면과 다른 SDR-like pixel representation을 만들 수 있는지 검증한다.

필요하면 Windows Graphics Capture / DXGI 계열의 color-aware capture 가능성을 기술적으로 평가한다. 단, 새로운 capture path를 도입할 경우에도 다음을 유지한다.

- 게임 메모리 읽기 없음
- DLL injection 없음
- packet interception 없음
- process hook 없음
- scan-time network 없음

Capture backend 변경은 동일-frame pixel evidence 및 regression gate를 통해 검증한다.

## 6. 검증 전략

외부 사용자 교정 데이터를 필수로 요구하지 않는다.

대신 공개 배포 준비 과정에서 synthetic/procedural environment matrix를 구축한다.

예:

- resolution scaling variants
- luminance/gamma/contrast shifts
- blur/sharpen variants
- mild chroma/tone shifts
- downscale/upscale resampling
- HDR-like washed-out transforms
- clipping/letter-box-free title ROI variations

이 matrix는 실제 reviewed Ground Truth를 대체하는 정답 데이터가 아니라 robustness regression용 파생 입력이다. 원본 reviewed Case가 있을 때는 원본 정답을 유지한 채 파생 이미지를 생성해 같은 Item ID가 유지되는지 검증한다.

## 7. 기존 Scanner 안전 계약 유지

다음은 변경하지 않는다.

```text
structural floor = 0.34
trusted HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

또한:

- false positive보다 miss 선호
- reviewed evidence 없이 recognition threshold 완화 금지
- cross-frame OCR/visual identity cache 금지
- Item ID 확정 전 price/needed를 identity evidence로 사용 금지
- current official catalog authority 유지

## 8. 제품 완료 기준

Cross-environment hardening은 단순히 한 PC에서 정확도가 좋아지는 것으로 완료하지 않는다.

최소 완료 기준:

1. 기존 reviewed regression에서 REGRESSION=0
2. synthetic/procedural environment matrix에서 기존 정상 Item identity 유지율이 유의미하게 개선
3. false-positive 증가 없음
4. 1080p/1440p/4K scale classes에 대한 deterministic smoke 또는 replay coverage
5. SDR/HDR-like luminance variation에 대한 normalization regression
6. Windows ko-KR OCR unavailable/degraded path fail-closed 유지
7. 전체 Windows build/test/publish/Product UI/Scanner/Map package gate 통과

## 9. 운영 원칙

외부 사용자가 문제를 보고하면 해당 evidence는 개선 자료로 사용할 수 있지만, 프로그램이 정상 작동하기 위해 사용자가 교정 데이터를 제공해야 하는 구조는 제품 목표에 맞지 않는다.

준현 헬퍼 Scanner는 공개 배포 사용자에게 가능한 한 환경 설정 변경이나 수동 캘리브레이션을 요구하지 않는 방향으로 유지한다.
