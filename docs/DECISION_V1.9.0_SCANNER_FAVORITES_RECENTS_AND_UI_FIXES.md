# DECISION — v1.9.0 Scanner 즐겨찾기/최근 본 아이템 및 UI 회귀 수정

기준일: 2026-08-28 KST  
상태: **CONFIRMED / IMPLEMENTATION AUTHORITY**

## 1. 목적

v1.9.0은 v1.8.4를 기준으로 다음 사용자 확정 요구사항을 반영한다.

- 탄약 구경/즐겨찾기 드롭다운의 아이콘 순환 체감 속도 개선
- 지도 마커 창의 탈출구 필터 체크박스 회귀 복구
- Scanner 검색 상태와 현재 열람 아이템 상세 상태 분리
- Scanner 사용자용 로그 영역 제거 및 즐겨찾기/최근 본 아이템 영역으로 교체
- Scanner 아이템 즐겨찾기와 최근 열람 기록 추가

Scanner OCR/진단 로그 파이프라인 자체를 제거하는 작업은 아니다. 사용자에게 보이는 로그 UI만 제거한다.

## 2. Ammo 아이콘 애니메이션

구경 선택과 즐겨찾기 선택 ComboBox는 기존처럼 하나의 아이콘 template/state/timer를 공유한다.

- 기존 interval: `1400 ms`
- v1.9.0 interval: **`700 ms`**

필터링, 구경 즐겨찾기 persistence, 선택 semantics는 변경하지 않는다.

## 3. Map 탈출구 체크박스

지도 마커 창에서 탈출구 관련 체크박스가 사라진 것은 제품 요구사항 변경이 아니라 **회귀**로 취급한다.

- 기존 탈출구 marker/filter 의미를 복구한다.
- 단순 cosmetic checkbox 삽입이 아니라 실제 marker filter 생성/연결 경로를 복구한다.
- v1.8.3에서 확립한 marker panel body layout 및 WPF post-load lifecycle contract를 유지한다.
- Main Map / Factory / MiniMap 의미를 다른 방식으로 변경하지 않는다.

## 4. Scanner 검색 상태와 상세 상태 분리

검색어는 검색 결과 표시 상태만 소유한다.

현재 열람 중인 item detail은 별도의 상태다.

따라서:

1. 아이템 A를 연다.
2. 검색창의 내용을 지운다.
3. 검색 popup/results는 닫히지만 A의 상세 정보는 그대로 유지된다.
4. 다른 아이템 B를 실제로 열었을 때만 상세가 B로 전환된다.

검색 TextChanged 이벤트에서 basic info / relationships / acquisition detail을 지우지 않는다.

## 5. Scanner 우측 영역

기존 사용자용 `로그` 영역을 제거하고 동일한 우측 column을 다음 비율로 사용한다.

```text
즐겨찾기      약 2/3
최근 본 아이템 약 1/3
```

두 영역은 독립적으로 세로 스크롤된다.

내부 `ScannerDiagnosticLog`, support/diagnostic dataset, 교정 및 유지보수용 진단 기능은 유지한다.

## 6. Scanner 아이템 즐겨찾기

현재 item detail header에서 Wiki 버튼의 왼쪽에 Ammo 즐겨찾기와 같은 별 계열 action을 둔다.

- 미등록: 빈 별
- 등록: 채워진 별
- 클릭으로 toggle
- 등록 시 우측 즐겨찾기 목록에 즉시 반영
- 목록 행 클릭 시 동일 Scanner item detail로 이동
- 목록 우측 채워진 별 클릭 시 즐겨찾기 해제 및 즉시 제거

저장 authority는 이름이 아닌 **canonical Item ID**다.

즐겨찾기는 앱 재시작 후에도 유지한다. 정렬은 **가장 최근에 즐겨찾기에 추가한 아이템이 위**다.

## 7. Scanner 최근 본 아이템

아이템 상세를 실제로 연 모든 경로는 하나의 product-owned navigation boundary를 거친다.

포함 경로:

- 직접 검색 결과
- 제작 결과/재료 item link
- 교환 결과/재료 item link
- 즐겨찾기 목록
- 최근 본 목록
- 기타 Scanner detail 내부 related-item navigation

최근 본 기록 정책:

- 최신 열람이 위
- canonical Item ID 기준 중복 없음
- 이미 존재하는 아이템을 다시 보면 기존 위치에서 제거하고 맨 위로 이동
- 최대 **50개** 유지
- 앱 재시작 후에도 유지
- 행 우측 `×`로 한 개 제거
- 영역 우측 상단 `전부 삭제`로 최근 본 아이템만 전체 삭제

즐겨찾기는 최근 본 삭제와 독립적이다.

## 8. 즐겨찾기 / 최근 본 리스트 presentation

각 행은 하나의 compact block으로 표시한다.

```text
[아이콘] 아이템 이름                         action
```

- 행 body 클릭: item detail open
- 즐겨찾기 action: 채워진 별, favorite 제거
- 최근 본 action: `×`, 해당 history row 제거
- action 클릭은 행 navigation으로 bubbling되지 않아야 한다.
- 긴 이름은 ellipsis 처리한다.
- 수평 스크롤은 사용하지 않는다.

## 9. persistence / GameMode 정책

즐겨찾기와 최근 본 아이템은 **Regular/PvE 공통 사용자 UI 데이터**다.

저장 데이터는 canonical Item ID/order만 authority로 사용한다. 현재 이름, 아이콘, 가격, 필요 개수, 관계 정보는 저장 snapshot을 authority로 삼지 않고 현재 catalog/current GameMode에서 다시 resolve한다.

따라서 동일 favorite/history item을 열더라도 실제 item detail은 현재 선택한 GameMode의 최신 데이터로 렌더링한다.

## 10. 검증 계약

v1.9.0은 source-level contract만으로 완료로 판정하지 않는다.

Windows x64 self-contained published executable에서 최소 다음을 확인한다.

- Ammo caliber/favorite icon이 모두 실제 렌더되며 700ms shared-cycle policy가 적용됨
- Map marker 창에 탈출구 관련 checkbox가 실제 표시되고 filter 의미가 연결됨
- Scanner item open 후 search text clear 시 열린 detail이 유지됨
- 사용자용 Scanner 로그 pane이 보이지 않음
- Favorites 2/3 + Recents 1/3 layout과 독립 scroll
- detail favorite star toggle / persisted favorite list / favorite row navigation
- recent row newest-first / dedup / re-view-to-top / per-row remove / clear-all / max 50
- related item navigation도 recent 기록 경로를 공유함
- Regular/PvE 전환과 무관하게 favorite/history identity는 공통 유지됨
- Main Map / Factory / MiniMap / graceful shutdown / clean portable root 회귀 없음

Runtime smoke는 검증 전에 feature initialization을 수동 호출하여 누락된 product lifecycle을 복구해서는 안 된다.
