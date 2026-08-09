# FIFTH USABILITY PASS — 5차 실사용 피드백

기록일: **2026-08-09**

상태: `UX FIXES IMPLEMENTED / SOURCE ANALYSIS COMPLETE / MAP PRODUCT DESIGN OPEN`

4차 Windows 테스트 빌드 실사용에서 확인된 즉시 수정 가능한 UX 문제와, Map 기능의 데이터 공급원 조사 결과를 기록합니다.

## 1. Ammo 즐겨찾기 이동

`CONFIRMED / IMPLEMENTED`

기존 즐겨찾기 ComboBox는 선택값을 상태로 유지하기 때문에, 즐겨찾기 A로 이동한 뒤 일반 구경 selector에서 B로 바꾸면 즐겨찾기 selector는 여전히 A를 선택한 상태로 남았습니다. 이 상태에서 A를 다시 눌러도 selection change가 발생하지 않아 A로 돌아갈 수 없는 문제가 있었습니다.

즐겨찾기는 현재 값을 나타내는 selector가 아니라 **저장된 구경으로 즉시 이동하는 shortcut**으로 취급합니다.

동작:

- `즐겨찾기 선택` 버튼을 누르면 저장된 구경 shortcut 목록을 popup으로 표시
- 각 항목은 button/action으로 동작하며 누를 때마다 현재 선택 상태와 무관하게 해당 caliber로 이동
- 같은 favorite를 여러 번 눌러도 항상 이동 action으로 처리 가능
- 현재 caliber의 `☆/★ 즐겨찾기` toggle과 `ammo-favorites.json` persistence는 유지
- favorite가 없으면 `등록된 즐겨찾기가 없습니다.` 표시

## 2. Item 용도 필터

`CONFIRMED / IMPLEMENTED`

기존 Item 종류(category)와 필요 상태(filter)와 별개로 **용도**를 구분해서 볼 수 있게 합니다.

용도:

- 모든 용도
- 퀘스트용
- 은신처용

규칙:

- Quest requirement source가 하나라도 있거나 flexible Quest candidate이면 `퀘스트용`
- Hideout requirement source가 하나라도 있으면 `은신처용`
- 양쪽에 모두 필요한 Item은 두 filter 모두에서 표시
- 용도는 기존 종류/검색/필요·정리·충분·판단보류 filter와 함께 교차 적용
- cross-navigation으로 특정 Item을 열 때는 용도 filter 때문에 대상이 숨지 않도록 `모든 용도`로 복귀
- `정리 필요 보기` shortcut도 용도를 `모든 용도`로 복귀
- 유동 제출 view는 본질적으로 Quest 용도이므로 별도 용도 filter를 중복 적용하지 않음

## 3. Map 데이터 공급원 조사

`SOURCE VERIFIED / PRODUCT DESIGN OPEN`

사용자는 기존 `Propeex/Tarkov-Helper`의 지도 사용 경험을 대체로 쓸 만한 참고점으로 보고 있으며, 새 Map 기능에서 더 중요한 문제를 **패치 후에도 유지 가능한 지도 데이터/API 공급원 확보**로 보고 있습니다.

조사 결과, 별도의 비공개 지도 API를 핵심 의존성으로 둘 필요는 없는 것으로 판단합니다.

권장 분리:

```text
json.tarkov.dev/<game-mode>/maps
→ extract / spawn / transit / boss / loot / switch 등 동적 gameplay/location data

Tarkov.dev 공개 map metadata
→ bounds / transform / rotation / zoom / floor layer / asset reference

license가 명확한 map artwork
→ 실제 시각적 지도 배경
```

원칙:

- 기존 Map 구현은 UX/좌표 처리 아이디어 참고 자료로만 사용
- gameplay marker data는 최신 온라인 원천에서 자동 갱신
- 지도 배경/층/좌표 변환 metadata와 gameplay marker를 별도 canonical 의미로 분리
- 숨은 비공개 endpoint나 DOM scraping에 장기 의존하지 않음
- 외부 지도 asset은 정확성뿐 아니라 license/redistribution 조건도 별도 검증

### 3.1 현재 확인된 artwork 후보

`the-hideout/tarkov-dev-svg-maps`는 Tarkov.dev/TarkovTracker에서도 사용되는 layered SVG map source이며 community tool 사용을 명시적으로 고려하고 있습니다.

다만 license가 **CC BY-NC-SA 4.0**이므로 다음 조건을 제품 정책에 반영해야 합니다.

- attribution
- non-commercial
- share-alike
- radar / ESP / cheat client / pixel-bot 등 부정행위 소프트웨어 사용 금지

Tarkov.dev 웹사이트 source code 자체의 MIT license와 지도 artwork license는 별개입니다. `assets.tarkov.dev`의 raster tile도 SVG repo와 동일한 조건이라고 임의 추정하지 않습니다.

따라서 **동적 map data source는 확보**, **배경 artwork 선택은 사용자 제품 판단 후 확정** 상태입니다.

상세 조사 결과: `docs/MAP_DATA_SOURCE_ANALYSIS.md`

## 4. 검증

최신 구현 checkpoint CI:

```text
CI run: 31290336689
Windows Release Desktop build: success
full automated tests: success
Windows x64 publish: success
ZIP/package creation: success
artifact upload: success
```

Map 자체의 실제 UI/DB importer는 아직 구현하지 않았습니다. 이번 pass에서 확정·구현한 범위는 Ammo favorite shortcut과 Item 용도 filter이며, Map은 source feasibility 분석까지입니다.
