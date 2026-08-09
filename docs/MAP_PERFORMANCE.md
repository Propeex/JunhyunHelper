# MAP PERFORMANCE — 대량 마커 렌더링

기록일: **2026-08-09**

상태: `IMPLEMENTED / FINAL CI PENDING`

## 목적

Tarkov Map의 `lootContainers`와 `lootLoose`는 지도에 따라 수천 개가 될 수 있습니다.

일반 탈출구·보스·퀘스트처럼 각 marker를 WPF `FrameworkElement` 하나씩 생성하는 방식은 대량 loot category를 켰을 때 UI thread와 visual tree를 과도하게 사용합니다.

따라서 대량 category만 별도 lightweight layer로 분리합니다.

## 구현

`BulkMapMarkerLayer`

- category 전체를 하나의 `FrameworkElement`로 보유
- `DrawingContext.OnRender`에서 icon/point를 일괄 렌더링
- click 위치에서 가까운 marker만 hit-test
- marker click detail은 유지
- 빈 지도 영역은 hit-test하지 않아 custom marker right-click을 막지 않음

적용 category:

- `LootContainer`
- `LooseLoot`

MapPage와 MiniMap 모두 동일 원칙을 사용합니다.

## 일반 marker와의 분리

다음처럼 상호작용 빈도가 높고 수가 제한적인 marker는 기존 개별 control을 유지합니다.

- extracts / transit
- PMC / Scav / sniper spawn
- Boss / special AI
- Quest objective
- hazard / artillery
- lock / switch
- stationary weapon / BTR
- user marker
- player marker

즉 성능 때문에 전체 Map interaction 모델을 희생하지 않고, 실제 병목 가능성이 있는 대량 loot만 경량화합니다.

## 기본 상태

- loot container: OFF
- loose loot: OFF

사용자가 ON으로 바꾸면 별도 recoverable UI preference로 저장되며 Game Content update와 독립입니다.

## 검증

Desktop build + full automated test + Windows x64 publish를 최종 PR head에서 다시 확인한 뒤 PR #44를 병합합니다.

실제 Windows에서는 대량 loot ON/OFF 시 UI 반응성과 zoom/pan 성능도 확인합니다.
