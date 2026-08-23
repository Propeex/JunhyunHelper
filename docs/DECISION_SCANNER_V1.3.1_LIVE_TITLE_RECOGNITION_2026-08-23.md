# Decision — Scanner v1.3.1 live title recognition hardening

날짜: 2026-08-23

상태: **CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED v1.3.1**

## 배경

실제 Tarkov 인게임 상세창 검증에서 Scanner가 좌측 상단의 실제 magnifier/search icon 대신 아이템 이름 첫 한글 글자를 magnifier로 선택하는 사례가 관측되었습니다.

이 문제는 단순 OCR 정확도만의 문제가 아니라 title ROI를 만들기 전 anchor 단계의 오류였습니다. 첫 글자가 magnifier로 선택되면 title ROI의 시작점이 오른쪽으로 이동하여 실제 첫 글자가 OCR 입력에서 빠질 수 있습니다.

사용자 요구사항:

- 좌측 magnifier를 더 정확히 식별
- 우측 red X를 더 정확히 식별
- 두 anchor 사이의 item-name field를 정확히 찾음
- item-name field 배경색도 활용
- 텍스트 OCR 자체도 강화
- 업데이트/준비 시간이 늘더라도 실제 기능 정확성을 우선
- 상단 status text 왼쪽에 현재 프로그램 버전 표시

## 결정 1 — 아이콘 하나가 아니라 inspect header 전체를 구조로 인식한다

Scanner title extraction은 다음 evidence를 결합합니다.

```text
dark title field
+ right red close/X
+ left magnifier shape
+ following first title glyphs
→ title ROI
```

magnifier 단독 또는 panel-relative 위치 단독으로 title ROI를 확정하지 않습니다.

## 결정 2 — magnifier는 shape evidence를 가진다

밝고 네모난 connected component라는 이유만으로 magnifier로 인정하지 않습니다.

다음 evidence를 사용합니다.

- header 내 상대 위치
- expected icon size 대비 크기
- aspect
- hollow/dark center
- bright ring perimeter
- lower-right handle
- 오른쪽에 뒤따르는 title glyphs

이 구조는 실제 magnifier보다 작은 한글 첫 글자를 icon으로 승격시키는 오류를 줄이기 위한 것입니다.

## 결정 3 — structural panel-left drift를 허용한다

상세창 detector의 panel-left가 실제 magnifier보다 일부 안쪽으로 잡힐 수 있으므로 magnifier search 영역을 제한적으로 왼쪽으로 확장합니다.

확장 검색은 title field/red close/title glyph evidence와 결합하며 화면 전체의 임의 UI icon을 검색하지 않습니다.

## 결정 4 — 첫 glyph 보존을 regression contract로 고정한다

packaged EXE smoke에 다음 synthetic failure case를 유지합니다.

- real-ish magnifier ring + handle
- Korean-like first glyph
- intentionally inward-drifted panel-left
- dark title field
- red close/X

통과 조건은 실제 magnifier를 선택하면서 first glyph가 최종 title ROI에 포함되는 것입니다.

## 결정 5 — OCR success에도 local Tarkov-font visual corroboration을 허용한다

Windows `ko-KR` OCR은 계속 primary recognizer입니다.

다만 잘린/오독된 OCR이 우연히 다른 current official name으로 semantic success할 수 있으므로 semantic success도 필요 시 local Tarkov-font/current-catalog renderer로 시각 corroboration할 수 있습니다.

정책:

- OCR과 visual이 같은 Item ID → OCR 유지
- font unavailable/error/ambiguous → healthy OCR 유지
- strict visual score + top1/top2 margin이 다른 **current official** Item ID를 명확하게 지목할 때만 identity 교정
- current catalog 밖 arbitrary Item/text 생성 금지

visual layer는 모든 OCR success에 대한 mandatory rejection gate가 아닙니다.

## 결정 6 — 게임 font 바이너리를 public package에 재배포하지 않는다

Tarkov 설치본의 `resources.assets`를 read-only로 확인하여 필요한 SFNT font payload를 app-local Scanner cache에 확보합니다.

```text
resources.assets
→ bounded font discovery/extraction
→ scanner/fonts cache
→ source/font generation validation
→ rendered official-name templates/features
```

이 방식은 한 번 정상 cache가 준비되면 실사용 관점에서 bundled font와 유사하게 동작하면서, Tarkov 업데이트에 따른 font generation 변경도 추적할 수 있습니다.

## 결정 7 — 현재 실행 버전을 MainWindow 상단에 표시한다

상단 status text 왼쪽에 실제 executable version을 표시합니다.

- `AssemblyInformationalVersion` 우선
- `+commit` build metadata는 UI label에서 제외
- fallback assembly version
- XAML에 특정 릴리즈 버전을 하드코딩하지 않음

## 변경하지 않는 계약

v1.3.1은 다음 의미를 변경하지 않습니다.

- current official Korean item catalog identity authority
- false positive보다 miss 선호
- Scanner Lab structural floor
- 기존 semantic/visual conservative acceptance 원칙
- highest trader sell price 의미
- flea positive `avg24hPrice` 의미
- `NeededItems[itemId].RequiredTotal` 의미
- Scanner display settings schema v4
- Content schema v7
- user.db schema v1
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지

## 릴리즈 분류

새 Scanner workflow capability를 추가한 것이 아니라 기존 title recognition의 정확성/신뢰성을 실제 failure evidence에 따라 보완하고 passive version label을 추가한 변경이므로 **v1.3.1 PATCH**로 분류합니다.

## 공개 검증

```text
version: v1.3.1
exact release source: 028bfb600f4662962a0daac1dad04b570e018275
final PR CI: 32615869812 — SUCCESS
automated tests: 256 / 256
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
asset SHA-256: 5c4b79cc5d373b4a28cbeb10be18b8369086b2ee9f0edc172530028dd71b1c3f
```

상세:

- `docs/SCANNER_V1.3.1_RECOGNITION.md`
- `docs/RELEASE_1.3.1.md`
- `docs/.release-v1.3.1-status.json`
