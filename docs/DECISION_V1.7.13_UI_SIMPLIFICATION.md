# v1.7.13 UI Simplification — 제품 결정

기준일: 2026-08-27
상태: `APPROVED REQUIREMENTS / IMPLEMENTATION IN PROGRESS`

## 사용자 결정

이번 PATCH는 새 데이터/Scanner 인식 기능을 추가하지 않고, 현재 준현 헬퍼의 반복 조작과 불필요한 UI를 줄이는 제품 정리 작업이다.

확정 요구사항:

1. Items의 퀘스트용/은신처용 용도 필터 제거.
2. Ammo 상단을 `구경 → 즐겨찾기 토글 → 즐겨찾기 선택 → 검색` 순서로 좌측 정렬하고 표시 열은 우측 정렬.
3. Ammo 상세정보는 기본 접힘.
4. Map의 지도 마커 선택은 기본 접힘. `지도 마커` 버튼 자체로 토글하며 펼치면 모든 선택지가 한 번에 보여야 함.
5. Map 경로(trail) 표시와 `경로 지우기` 제거.
6. Map 설정 버튼은 다시 누르면 설정을 닫음.
7. 버튼으로 연 dropdown/popup은 같은 버튼을 다시 누르면 닫힘.
8. Map 단축키 안내 설명 제거.
9. Ammo 표 위 요약 텍스트 제거.
10. Scanner 설정은 변경 즉시 저장하며 취소/저장 버튼 제거.
11. Scanner 설정의 아이콘/아이템 이름 `항상 표시` 안내 행 제거.
12. Scanner 단축키 설정을 Scanner display 설정에서 분리해 Scanner 기본 화면으로 이동.
13. Mini Scanner 표시 정보 설명 텍스트 제거.
14. 사용자-facing 설정/편집 창은 MainWindow 내부 popup/overlay로 표시하고 바깥 클릭으로 닫을 수 있는 공통 interaction을 사용.
15. Scanner 검색 상세에서 needed item이면 관련 Quest/Hideout source list를 표시하고 해당 화면으로 이동 가능.
16. Scanner `현재 결과 교정` 버튼은 우측 정렬.

## 유지되는 계약

- Game Content / User Progress / Needed Items 계산 의미는 변경하지 않는다.
- Scanner Item identity recognition pipeline과 structural/header/OCR/matcher/visual acceptance는 변경하지 않는다.
- Scanner `필요 개수` authority는 `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`이다.
- Map/MiniMap donor revision은 유지하고 JunhyunHelper first-party customization boundary에서 제품 요구사항을 적용한다.
- public v1.7.12 tag/source/assets는 immutable historical release로 유지한다.

## 구현 원칙

- 단순히 숨김만 해서 stale control state가 동작에 개입하지 않도록 제거된 필터/요약/경로 기능의 code path도 정리한다.
- settings/edit overlay는 MainWindow의 한 공통 owner에서 열고, 같은 launcher 재클릭과 backdrop click으로 닫는다.
- settings auto-save는 기존 atomic settings service를 재사용한다.
- Scanner searched-item source list는 기존 ItemsWorkspace `NeededItems[].Sources`를 presentation에 join할 뿐 Quest/Hideout 요구량을 재계산하지 않는다.
