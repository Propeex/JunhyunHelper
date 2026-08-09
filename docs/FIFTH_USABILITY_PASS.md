# FIFTH USABILITY PASS — 5차 실사용 피드백

기록일: **2026-08-09**

상태: `INTENT_CAPTURED / IMPLEMENTATION STARTING`

4차 Windows 테스트 빌드 실사용에서 확인된 즉시 수정 가능한 UX 문제와, Map 기능의 데이터 공급원 조사 시작점을 기록합니다.

## 1. Ammo 즐겨찾기 이동

`CONFIRMED`

현재 즐겨찾기 ComboBox는 선택값을 상태로 유지하기 때문에, 즐겨찾기 A로 이동한 뒤 일반 구경 selector에서 B로 바꾸면 즐겨찾기 selector는 여전히 A를 선택한 상태로 남습니다. 이 상태에서 A를 다시 눌러도 selection change가 발생하지 않아 A로 돌아갈 수 없습니다.

즐겨찾기는 현재 값을 나타내는 selector가 아니라 **저장된 구경으로 즉시 이동하는 shortcut**으로 취급합니다.

동작:

- `즐겨찾기` 메뉴를 열면 저장된 구경 목록을 button/action 형태로 표시
- 항목을 누를 때마다 현재 선택 상태와 무관하게 해당 caliber로 이동
- 같은 favorite를 여러 번 눌러도 항상 이동 action으로 처리 가능
- 현재 caliber의 `☆/★ 즐겨찾기` toggle과 `ammo-favorites.json` persistence는 유지
- favorite가 없으면 메뉴에 빈 상태를 표시

## 2. Item 용도 필터

`CONFIRMED`

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
- 유동 제출 view는 본질적으로 Quest 용도이므로 별도 용도 선택을 강제하지 않음

## 3. Map 데이터 공급원 조사

`INTENT_CAPTURED / SOURCE ANALYSIS`

사용자는 기존 `Propeex/Tarkov-Helper`의 지도 사용 경험은 대체로 쓸 만했다고 보고 있으며, 새 Map 기능에서 더 중요한 문제를 **패치 후에도 유지 가능한 지도 데이터/API 공급원 확보**로 보고 있습니다.

현재 조사 방향:

- 기존 Map 구현은 UX/좌표 처리 아이디어 참고 자료로만 사용
- 게임 위치/marker 데이터는 최신 외부 원천에서 자동 갱신 가능해야 함
- 지도 배경/층/좌표 변환 metadata와 gameplay marker 데이터를 분리해서 평가
- 숨은 비공개 endpoint나 scraping에 장기 의존하지 않음
- 외부 지도 asset은 정확성뿐 아니라 license/redistribution 조건도 검증

상세 조사 결과는 `docs/MAP_DATA_SOURCE_ANALYSIS.md`에 기록합니다.
