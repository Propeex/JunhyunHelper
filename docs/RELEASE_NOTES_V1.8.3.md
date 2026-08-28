# 준현 헬퍼 v1.8.3

상태: **RELEASE CANDIDATE**

v1.8.3은 v1.7.15에서 확정한 탄약/지도 UI가 실제 published executable에서도 항상 적용되도록 WPF runtime activation과 지도 마커 패널 layout을 교정하는 유지보수 PATCH입니다.

## 탄약 구경 / 즐겨찾기 드롭다운

- 구경 및 즐겨찾기 visible UI 초기화를 실제 `AmmoPage` 초기화 경계에서 보장합니다.
- 즐겨찾기 선택은 일반 ComboBox를 사용합니다.
- 구경과 즐겨찾기 selector가 동일한 구경별 탄약 icon template/state와 순환 타이밍을 공유합니다.
- legacy favorites menu는 숨김/비활성 상태를 유지합니다.
- published executable smoke 이전에 별도 initialization gate를 두어 검증 코드가 누락된 제품 초기화를 뒤늦게 보정해 숨기지 못하도록 했습니다.

구경 filtering과 즐겨찾기 저장 의미는 변경하지 않았습니다.

## 지도 마커 선택 창

- 체크박스 목록 viewport가 패널 헤더를 제외한 남은 본문 세로 공간 전체를 사용합니다.
- 세로 스크롤바는 `Auto`로 처리하며 실제 렌더링된 내용이 viewport를 넘을 때만 표시됩니다.
- WPF의 사전 `DesiredSize` 추정으로 스크롤바 표시 여부를 강제하지 않습니다.
- marker panel activation은 transient `MapMarkersContent.Parent`가 아니라 안정적인 `MapMarkersOverlay.Child` 구조를 기준으로 viewport를 해결합니다.
- 이미 제품 viewport가 있으면 재사용하고, 없으면 실제 overlay child index에 삽입합니다.
- viewport가 실제로 준비되기 전에 activation 완료 상태를 기록하지 않습니다.

## 유지되는 지도 계약

- Map/MiniMap pinned donor revision은 변경하지 않았습니다.
- 지도 마커 종류 및 on/off 의미는 변경하지 않았습니다.
- Main Map / Factory 층 표시와 다른 층 marker presentation은 변경하지 않았습니다.
- MiniMap 동작은 변경하지 않았습니다.

## 검증 계약

v1.8.3은 다음이 모두 녹색인 exact main source만 공개합니다.

```text
Release build
full test suite
Windows x64 self-contained single-file publish
Ammo real initialization gate
Ammo rendered caliber/favorites icon + shared timer-cycle smoke
Map marker body/real-overflow smoke
Main Map / Factory / MiniMap smoke
graceful shutdown
clean portable root
release package/checksum verification
exact-main CI
public tag/release/assets readback
```

기술적 원인과 lifecycle ownership 결정은 `docs/DECISION_V1.8.3_VISIBLE_UI_RUNTIME_ACTIVATION.md`를 기준으로 합니다.
