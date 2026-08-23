# Scanner v1.3.4 — live recognition hardening reference

기준일: 2026-08-23

상태: **PUBLIC RELEASE / VERIFIED**

## 1. 목적

v1.3.4는 v1.3.3의 실제 inspect-header lock을 유지하면서 실제 Tarkov 사용에서 확인된 recognition/diagnostics 결함을 더 보수적으로 수정합니다.

```text
capture
→ structural detail candidates
→ red close component
→ close shape template
→ long neutral header frame
→ fixed frame-left search-icon lane
→ normalized magnifier template
→ dark title field + text evidence
→ HEADER_FRAME_LOCKED
→ locked-header-based detail bounds refinement
→ title ROI
→ Windows ko-KR OCR
→ current-catalog sanitation
→ optional one-unknown-glyph current-catalog recovery
→ ordinary exact/fuzzy/bounded-one-edit matcher
→ optional strict local font visual corroboration/recovery
→ Item ID or fail closed
→ local presentation
```

## 2. Magnifier detection

v1.3.3까지 magnifier는 frame-left bounded region 안의 bright connected component를 morphology/location score로 평가했습니다. 실제 피드백에서 title glyph가 더 magnifier처럼 보이는 상황을 차단하기 위해 v1.3.4는 후보 공간 자체를 축소합니다.

`close.Height / 17`을 기준 scale로 사용하고 실제 live evidence의 약 13×13 bright core를 정규화합니다.

```text
expected x ≈ frame.Left + 12 * scale
expected y ≈ frame.Top  +  7 * scale
expected size ≈ 13 * scale
```

허용 후보는 frame-left icon lane 안의 작은 patch뿐입니다. title lane의 glyph는 shape가 비슷해도 candidate pool에 들어갈 수 없습니다.

Template score는 다음 evidence를 결합합니다.

- ring bright band
- hollow/dark center
- lower-right diagonal handle
- outside dark/background
- expected location/size

## 3. Close/X detection

red component 조건을 유지하되 다음 normalized template evidence를 추가합니다.

- red-dominant body
- red edge
- two diagonal X contrast bands
- expected header right/top alignment

최종 close candidate는 template + geometry 결합 score를 통과해야 합니다.

## 4. Full header lock only

`ScannerLab38InspectDetector`는 anchor refiner가 다음을 만족하지 않으면 해당 structural candidate를 결과 목록에 포함하지 않습니다.

```text
Reason == HEADER_FRAME_LOCKED
Score >= 0.68
Magnifier.Width > 0
CloseButton.Width > 0
```

따라서 partial lock candidate가 OCR/semantic identity로 진행하는 우회 경로를 허용하지 않습니다.

## 5. Locked detail bounds

초기 structural detector의 rectangle은 discovery seed입니다. Full header lock 후에는 authoritative header controls에서 top/left/right를 다시 계산합니다.

```text
left   = magnifier.X - 12 * scale
top    = close.Y     -  5 * scale
right  = close.X + close.Width + 4 * scale
bottom = existing structural bottom
```

아이템/stat pane에 따라 detail-window 높이가 달라질 수 있으므로 bottom은 structural detector 값을 유지합니다.

## 6. OCR one-unknown-glyph evidence

현재 공식 카탈로그 밖 symbol이 영숫자 사이에 나타난 경우 ordinary sanitation에서는 제거하지만 별도 pattern에는 `?`로 보존할 수 있습니다.

```text
Esma「ch 에스마르호 지혈대
→ ordinary: Esmach 에스마르호 지혈대
→ pattern:  Esma?ch 에스마르호 지혈대
```

`?`는 wildcard substitution이 아니라 **한 glyph 위치가 미상이라는 evidence**입니다.

복구는 complete current official catalog에서 같은 normalized 길이, 같은 나머지 character slot을 가진 candidate가 정확히 하나일 때만 허용합니다. Global runner-up과 최소 10%p margin을 추가로 요구합니다.

Short name에는 적용하지 않습니다.

## 7. Diagnostic PNG

메모리 diagnostic frame policy는 유지합니다. 자동 disk persistence는 없습니다.

사용자 explicit export 시 `RenderDiagnosticBitmap`이 raw capture 위에 다음 outline을 1:1 pixel coordinate로 합성합니다.

- Lime 3px: selected detail
- DeepSkyBlue 2px: title ROI
- Gold 2px: magnifier
- OrangeRed 2px: close/X

packaged-EXE smoke는 synthetic frame에 네 rectangle을 렌더링한 뒤 실제 output pixel의 channel dominance를 확인합니다.

## 8. Regression tests

v1.3.4 공개 소스에서 자동 테스트는 **267개 / 0 failed / 0 skipped**입니다.

추가/유지되는 핵심 regression:

- `Esma「ch` → unknown-glyph evidence 보존
- unique current-catalog wildcard candidate만 성공
- ambiguous wildcard candidate fail closed
- short wildcard title fail closed
- leading garbage는 unknown glyph로 승격하지 않음
- catalog quote는 유지하고 impossible punctuation은 ordinary matcher에서 제거
- 12개 live-derived header geometry synthetic replay
- title lane의 decoy ring이 real magnifier를 이기지 못함
- magnifier missing은 fail closed
- diagnostic PNG four-color overlay smoke

## 9. 변경하지 않는 threshold/data contract

- header runtime floor: 0.68
- ordinary fuzzy confidence/margin: unchanged
- bounded one-edit uniqueness/global margin: unchanged
- visual recovery safety: unchanged
- highest trader/flea/RequiredTotal presentation contract: unchanged
- schemas: unchanged

## 10. 공개 검증

```text
source/tag: a78ddbc649747f1320236556f17e6b908304674a
final PR CI: 32636665202 — SUCCESS
release run: 32636927134 — SUCCESS
independent public verifier: 32637159066 — SUCCESS
asset bytes: 80,319,654
SHA-256: 8c442fec81a0b993a9a6b080e59b656668a7a73d8fadd8434595545b08c82e8e
ProductVersion: 1.3.4+a78ddbc649747f1320236556f17e6b908304674a
Draft/public re-download + EXE smoke: SUCCESS
```

## 11. 후속 live calibration

v1.3.4 공개 후에도 새로운 실제 Tarkov failure는 저장된 overlay diagnostic PNG와 scanner.log를 함께 사용해 다음 단계로 분리합니다.

```text
capture
→ structural candidate
→ close template
→ frame
→ magnifier template/lane
→ locked bounds/title ROI
→ raw OCR
→ sanitation/unknown-glyph pattern
→ semantic/visual match
→ presentation/overlay
```

실제 evidence 없이 global acceptance threshold를 낮추지 않습니다.
