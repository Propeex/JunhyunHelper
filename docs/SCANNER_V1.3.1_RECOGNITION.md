# Scanner v1.3.1 Recognition Contract

기준일: 2026-08-23

## 목적

v1.3.1은 새 Scanner 기능을 추가하는 릴리즈가 아니라 실제 Tarkov 상세창에서 발견된 title-anchor/텍스트 인식 실패를 보수적으로 줄이는 PATCH입니다.

실제 실패 패턴:

- 상세창 좌측 상단의 실제 magnifier/search icon 대신 아이템 이름 첫 한글 글자가 magnifier 후보로 선택됨
- 그 결과 title ROI 시작점이 첫 글자 뒤로 이동하여 OCR 입력에서 첫 글자가 잘림
- 잘린 OCR이 miss 또는 잘못된 official-name match로 이어질 수 있음

## 제품 원칙

1. false positive보다 miss를 선호한다.
2. current official Korean item catalog만 Item identity authority로 사용한다.
3. geometry/anchor/OCR/font evidence는 모두 보조 evidence이며 단독으로 Item ID를 임의 생성하지 않는다.
4. scan-time external API request를 하지 않는다.
5. game memory read, DLL injection, packet interception을 사용하지 않는다.
6. Tarkov font binary를 JunhyunHelper 배포물에 재배포하지 않는다.
7. local Tarkov installation에서 필요한 font payload를 read-only로 확보하고 app-local cache로 사용한다.

## Title extraction pipeline

```text
captured frame
→ Scanner Lab structural candidates
→ inspect-header search band
→ dark title-field evidence
→ right red close/X evidence
→ left magnifier evidence
→ following title-glyph evidence
→ first glyph start
→ magnifier-free title ROI
```

### Dark title field

상세창 상단의 어두운 neutral 배경 strip을 별도 evidence로 사용합니다.

목적:

- 아이콘/텍스트가 존재하는 상단 header lane을 구조적으로 제한
- magnifier와 close control이 같은 header에 속하는지 교차 검증
- panel-relative 좌표 하나에 의존하지 않음

### Red close/X

- panel 우측 상단 영역에서 red-dominant connected component를 탐색
- 우측 edge proximity와 shape compactness를 함께 사용
- title ROI의 최대 우측 경계에 사용

### Magnifier

단순히 `밝고 네모난 component`를 magnifier로 인정하지 않습니다.

평가 evidence:

- header field 내부 상대 위치
- expected icon size 대비 크기
- width/height aspect
- 내부 dark/empty center 비율
- ring perimeter 밝기
- 우하단 handle 밝기
- icon 오른쪽에 실제 title glyph evidence가 뒤따르는지

특히 한글 첫 글자는 일반적으로 실제 magnifier보다 작고 내부/둘레/handle 구조가 다르므로 shape evidence에서 감점됩니다.

### Panel-left drift 대응

structural candidate의 panel left가 실제 magnifier보다 안쪽으로 잡힐 수 있으므로 magnifier search 범위는 panel left 기준으로 왼쪽까지 제한적으로 확장합니다.

이 확장은 title field/right close evidence와 함께 사용되며 화면 전체의 임의 밝은 아이콘을 검색하지 않습니다.

## OCR / visual corroboration

Windows `ko-KR` OCR이 primary text recognizer입니다.

### OCR failure/corruption

기존처럼:

- current catalog character policy로 OCR text 품질 평가
- plausible text가 있으면 semantic shortlist + Tarkov-font visual verifier
- OCR이 비거나 심하게 손상되면 strict full-catalog visual matcher

를 사용합니다.

### OCR semantic success

v1.3.1부터 semantic OCR success도 필요 시 local Tarkov title-font renderer로 corroborate할 수 있습니다.

의도:

- 잘못 잘린/오독된 OCR 문자열이 우연히 다른 official name으로 semantic success한 경우 시각 evidence를 한 번 더 확인

정책:

- OCR과 visual result가 같은 Item ID → OCR 결과 유지
- targeted visual verifier가 불확실하면 strict full-catalog visual matcher를 1회 사용할 수 있음
- visual evidence가 다른 current official Item ID를 strict threshold + margin으로 명확하게 지목할 때만 official name을 교정
- font unavailable, renderer error, ambiguous visual result → 기존 OCR success 유지

즉 font corroboration은 healthy OCR을 무조건 거부하는 새로운 mandatory gate가 아니라, 명확한 시각 모순이 있을 때만 교정하는 보수적 hardening입니다.

## Tarkov font source/cache

배포물에 게임 font 파일을 포함하지 않습니다.

```text
Tarkov resources.assets
→ bounded read-only SFNT payload discovery
→ app-local scanner/fonts cache
→ source/font generation manifest
→ Bender regular/bold + Korean Noto fallback
→ rendered official-name templates/features
```

Tarkov source generation이 변경되면 이전 rendered template generation을 그대로 신뢰하지 않습니다.

## Regression contract

packaged-EXE smoke에는 다음 실패 조건을 합성합니다.

- 실제 magnifier와 비슷한 ring + handle component
- 그 오른쪽에 더 작은 Korean-like first glyph
- structural panel left를 실제 magnifier보다 일부러 안쪽으로 drift
- dark title field
- right red close/X

통과 조건:

1. 실제 magnifier가 선택될 것
2. Korean-like first glyph가 magnifier로 선택되지 않을 것
3. 최종 title ROI가 magnifier를 제외할 것
4. 최종 title ROI가 first glyph를 포함할 것
5. usable title width가 유지될 것

## UI version contract

MainWindow의 상태 텍스트 왼쪽에 현재 executable version을 표시합니다.

- `AssemblyInformationalVersion` 우선
- build metadata(`+sha`)는 UI label에서 제외
- fallback은 assembly version
- 예: `v1.3.1`
- UI XAML에 특정 릴리즈 버전을 하드코딩하지 않음

## 변경하지 않는 데이터 의미

v1.3.1은 다음 의미를 변경하지 않습니다.

- highest trader sell price
- flea `avg24hPrice`
- per-slot 계산
- `NeededItems[itemId].RequiredTotal`
- Content schema
- user.db schema
- Scanner display settings schema v4
- Scanner hotkey semantics
- one-shot/test capture semantics

## 실사용 검증 방식

정식 릴리즈 이후 실제 Tarkov에서 다음 loop를 반복합니다.

```text
live scan
→ success / miss / wrong identity 확인
→ 문제가 있으면 즉시 인식 원본 PNG 저장
→ actual item name + observed result 수집
→ scanner.log/diagnostic image와 함께 root cause 분류
→ detector / header / ROI / OCR / visual / catalog 단계 중 원인 수정
→ 동일 사례 regression
```

confidence/margin 완화는 실제 evidence 없이 수행하지 않습니다.
