# USABILITY REVIEW — 2026-08-08 첫 실사용 피드백

상태: `CONFIRMED USER INTENT + IMPLEMENTATION NOTES`

이 문서는 첫 Windows 실사용 테스트에서 사용자가 직접 전달한 UX/표시 개선 요구를 기록한다. 문장 자체보다 제품 의도를 우선한다.

## 전체 방향

- 현재 Core 계산 원리는 유지한다.
- 첫 실사용에서 드러난 문제는 기능 추가보다 UI/입력/표시 개선을 우선한다.
- 흰 기본 WPF control 스타일을 제거하고 준현 헬퍼 전체에 일관된 dark theme을 적용한다.
- 내부 안전/진단 정보를 사용자 주 화면에 그대로 덤프하지 않는다.
- 게임에서 쉽게 비교해야 하는 정보는 한 화면에서 빠르게 스캔할 수 있게 만든다.

## 1. 공통 UI 테마

- ComboBox popup, list item, ScrollBar, TextBox, Button, DataGrid 등 공통 control을 dark background + light text로 통일한다.
- 기본 Windows 흰 popup이 남지 않게 global style/template을 둔다.
- 모서리, hover/pressed state, padding, scrollbar thumb을 현재보다 부드럽고 정돈된 형태로 만든다.
- 화면별 개별 patch가 아니라 공통 theme resource가 소유한다.

## 2. 이미지/아이콘

- Hideout station, Item, Ammo에 source-provided image/icon을 표시한다.
- Ammo는 Item icon을 공유한다.
- URL을 UI에서 매번 직접 hotlink하지 않고 local cache를 사용한다.
- icon 실패는 Game Content 전체 실패 사유로 만들지 않으며 last valid/fallback을 사용한다.
- primary source의 실제 image URL field는 importer 변경 전 live raw probe로 다시 검증한다.

## 3. Quest/Hideout 목록 행

- 행 전체 폭을 사용하고 이름 길이에 따라 카드 폭이 달라 보이지 않게 한다.
- 일정한 row height/padding/alignment를 사용한다.
- icon/name/meta/status/action 영역을 정렬된 구조로 만든다.

## 4. 수치 입력 UX

기본 입력은 직접 text/ComboBox 선택보다 `- / 현재값 / +` 조작을 우선한다.

- player level: ±1
- prestige: ±1
- trader LL: ±1
- Hideout level: ±1
- Fence standing: ±0.1

### 일반 Trader standing

사용자 기본 UX에서는 Fence 외 trader의 standing 입력을 숨기는 방향을 선호한다.

단, 현재 live Quest data는 trader requirement에 `reputation`과 `level`을 서로 다른 의미로 제공하고 reputation에는 `>=`, `<=`, `<`가 실제 존재하므로 standing 개념 자체를 Core에서 삭제하지 않는다.

방향:
- 기본 Profile UI: 일반 trader는 LL 중심
- Fence: standing을 직접 노출
- 비-Fence standing은 실제 판정에 필요한 희귀 예외를 위해 optional/advanced 입력 경로를 보존
- 값이 필요한데 모르면 0으로 추측하지 않고 기존 Indeterminate 안전 규칙을 유지

## 5. Hideout level

이전 결정을 변경한다.

- 저장된 level이 없는 Hideout station은 제품상 Lv.0으로 취급한다.
- `미입력` UI를 제거한다.
- 따라서 unentered-hideout cleanup protection도 제거한다.
- user.db에 모든 Lv.0을 강제로 기록할 필요는 없고 missing key를 Lv.0으로 해석할 수 있다.

## 6. Item 화면 재설계

현재 첫 테스트 화면의 큰 `유동 제출 요구` 텍스트 블록은 사용자 화면으로 부적절하다.

방향:
- legacy Tarkov-Helper에서 검증된 `Item icon + FIR badge + compact item row/card` 사용성을 참고한다.
- 기존 로직은 재사용하지 않는다.
- 기본 화면은 item 중심으로 보여준다: icon, name, future required, owned, remaining/cleanup, FIR, source summary.
- flexible hand-in은 안전 계산에는 유지하되 전역 giant text dump를 제거한다.
- flexible group은 compact notice/expandable detail 또는 선택 item과 관련된 detail에서 보여준다.

## 7. Ammo acquisition 표기

현재 `수급처 N개` 표시는 비교성이 낮으므로 제거한다.

표에는 compact route label을 보여준다.
예:
- `Prapor LL3`
- `Mechanic LL2 교환`
- `Workbench Lv.3`
- 여러 경로가 있으면 짧은 대표 label들 + 추가 개수 표기
- structured acquisition이 0이면 사용자 요구에 따라 `레이드 획득` 표시

선택 ammo의 상세 화면에서는 기존처럼 모든 acquisition detail을 유지한다.

## 8. Game Content update progress

- 데이터 업데이트 시 ProgressBar + 현재 단계 text를 제공한다.
- 가짜 byte-level percentage를 만들지 않는다.
- download/import/validation/candidate DB/read-back/activation/recalculation 등 실제 pipeline stage 기준으로 진행을 표시한다.

## 9. Profile controls

상단 공간을 줄인다.

- Profile selector는 유지
- `새 프로필`은 Profile dropdown/menu 안으로 이동
- `프로필 삭제`는 `프로필 수정` 화면의 danger action으로 이동
- top bar에는 독립적인 삭제/새 프로필 버튼을 두지 않는다.

## 10. Ammo table 개선

- 검색 기능 제거
- caliber selector 유지
- column visibility popup/menu 추가
- 체크를 끈 column은 main table에서 숨기되 detail에는 계속 표시
- 기본/강제 row order는 **penetration power ascending → damage ascending → name** 순으로 고정한다.

### Armor class effectiveness 1~6

사용자가 Tarkov Wiki의 `Bullet effectiveness against armor class` 0~6 시각화를 요구했다.

확인 결과:
- json.tarkov.dev의 현재 canonical ammo facts에는 penetration, armor damage 등 원시 ballistic facts가 있지만 wiki의 class 1~6 effectiveness rating 자체는 raw field가 아니다.
- Wiki는 0~6을 armor에 막히는 평균 탄 수/초기 관통 성능을 요약한 guideline으로 정의한다.

따라서:
- 단순 `penetration / 10` 같은 자체 heuristic은 금지
- Fandom HTML을 매 update마다 무검증 scrape하는 방식도 금지
- 동일한 0~6 결과를 결정론적으로 재현할 공식/검증 가능한 계산 또는 명시적 overlay source를 먼저 확정한다.
- 구현되면 class 1~6 각각 숫자 + 단계별 색상 cell을 표시하며 derived guideline임을 문서화한다.

상세 조사와 공식은 `docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`에서 관리한다.

## 11. Trader/Map display order

- alphabetic order가 아니라 실제 게임 UI order를 사용한다.
- source/게임 UI를 조사해 explicit display order mapping을 둔다.
- 불확실할 때만 사용자에게 순서를 요청한다.

## 12. Ground Zero / Ground Zero 21+

사용자 관점에서는 하나의 `Ground Zero`로 취급한다.

- Quest filter/display grouping에서 하나로 merge
- raw Map ID는 source meaning 보존을 위해 삭제하지 않는다.
- objective가 두 variant를 가리켜도 UI에서는 Ground Zero 하나로 표시한다.
- 향후 boss/loot/map-specific 기능은 variant 차이가 필요할 수 있으므로 raw identity와 display group을 분리한다.

## 13. Future tabs

Top navigation에 다음을 미리 추가한다.

- 지도
- 스캐너

아직 기능은 구현하지 않는다. 클릭 시 `준비 중` placeholder 정도만 보여주고 가짜 기능/데이터는 만들지 않는다.

## 구현 우선순위

1. 공통 dark control theme + list row layout
2. Profile/Hideout +/- input + Hideout missing=Lv.0 + top profile compaction
3. Item 화면 재설계 + icon cache foundation
4. Ammo table UX/acquisition/column selector/default ordering
5. Ammo armor-effectiveness source/formula 검증 후 1~6 cells
6. update progress visualization
7. trader/map game order + Ground Zero display grouping
8. Map/Scanner placeholder tabs
9. Windows test build 재배포 후 사용자 재검토
