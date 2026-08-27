# DECISION — v1.7.15 UI refinements

기록일: 2026-08-27  
상태: **CONFIRMED / IMPLEMENTED IN RELEASE CANDIDATE**

## 사용자 의도

v1.7.14 이후 새 기능을 추가하지 않고 기존 UI의 남은 불편을 정리한다.

확정 범위:

1. 메인 상단의 version/status 영역은 사용자에게 **버전 정보만** 보인다.
2. `정리 필요` 문자열 대신 현재 Items cleanup 대상이 있을 때 Items 탭 우측 상단에 작은 주황색 점을 표시한다.
3. Map marker selector는 바깥 panel만 커지는 것이 아니라 내부 checkbox 목록도 실제 가용 높이를 사용한다. content가 들어오면 불필요한 scrollbar를 보이지 않는다.
4. Map marker selector는 기존 launcher 재클릭뿐 아니라 panel 바깥 클릭으로 닫힌다. dismiss click은 가능한 한 원래 Map/control click을 소비하지 않는다.
5. Ammo `즐겨찾기 선택`은 일반 dropdown으로 표시한다.
6. Ammo 구경 dropdown과 즐겨찾기 dropdown 모두 `member-ammo icon animation + caliber label`을 같은 규칙으로 사용한다.
7. caliber에 속한 특정 ammo 하나를 영구 대표 icon으로 고정하지 않는다.

## 구현 결정

### Header / cleanup attention

- transient `StatusText`는 lifecycle 내부 상태로 남길 수 있으나 header에서는 숨긴다.
- Game Content update의 전용 progress overlay는 그대로 유지한다.
- cleanup indicator의 truth는 문자열 parsing이 아니라 현재 `ItemsWorkspace.Plan.CleanupItems.Count > 0`이다.
- indicator는 count/text를 추가하지 않는 작은 orange dot이다.

### Map marker selector

- donor `MapMarkersContent` 자체의 marker/check state authority를 바꾸지 않는다.
- JunhyunHelper first-party partial이 content의 measured desired height와 current Map viewport의 available height를 조합해 list viewport를 정한다.
- content가 available height 안에 들어오면 vertical scrollbar는 hidden이다.
- 실제로 넘칠 때만 scrolling을 허용한다.
- outside-click dismiss는 marker state를 변경하지 않는다.
- outside-click RoutedEvent는 handled로 소비하지 않아 원래 Map/control interaction이 이어질 수 있게 한다.

### Ammo caliber/favorites icon sequence

- 기존 `CaliberChoice`, `_favoriteCalibers`, `AmmoRow`, `ImageCacheService` authority를 재사용한다.
- visible Favorites selector만 standard ComboBox로 전환하고 저장/filter semantics는 기존 코드를 유지한다.
- 두 ComboBox는 같은 item presentation template과 caliber별 animation index를 공유한다.
- icon 후보는 해당 `RawCaliber`에 실제 속한 `AmmoRow.Icon`만 사용한다.
- 현재 cadence는 1.4초이며, dropdown 둘 다 닫히면 timer를 중지한다.
- icon byte는 기존 Ammo icon loading/cache 결과를 재사용한다. 별도 source/network authority를 만들지 않는다.

## 변경하지 않는 계약

- Scanner recognition policy/constants/matcher/visual acceptance/pacing
- Game Content / User Progress authority
- Needed Items / cleanup 계산 의미
- Ammo favorites persistence와 caliber filtering 의미
- Map/MiniMap donor revision `d933792b6042a51cea38dc44b686a096fe30de67`
- Program Update와 stable-release immutability

## 검증

`V1715UiRefinementsContractTests`가 핵심 source contract를 고정한다. Release candidate는 full Windows release gate를 통과한 뒤에만 public stable로 게시한다.
