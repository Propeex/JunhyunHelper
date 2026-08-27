# 준현 헬퍼 v1.7.15

v1.7.15는 기존 제품 기능의 의미를 바꾸지 않고 메인 상태 표시, 지도 마커 selector, Ammo 구경/즐겨찾기 선택 UI를 마무리하는 UI/UX PATCH입니다. Scanner 인식 정책, Game Content/User Progress 권위, Map/MiniMap donor revision은 변경하지 않습니다.

## 사용자-facing 변경

- **메인 상단 상태 영역**
  - 버전 정보만 표시합니다.
  - 기존 `정리 필요` 문자열은 표시하지 않습니다.
  - 현재 Items 계획에 정리 대상이 하나 이상 있으면 Items 탭 우측 상단에 작은 주황색 점을 표시합니다.
  - 정리 대상이 없어지면 점도 사라집니다.
  - Game Content update 진행 상황은 기존 전용 progress overlay를 사용하며 버전 영역에 별도 진행 문구를 표시하지 않습니다.

- **Map 지도 마커 selector**
  - 바깥 panel 크기만 커지고 내부 marker checkbox 영역은 작게 남던 v1.7.14 UI 회귀를 수정합니다.
  - marker content의 실제 높이와 현재 Map viewport의 가용 높이를 기준으로 내부 viewport를 계산합니다.
  - 현재 marker 목록이 가용 공간 안에 들어오면 세로 scrollbar를 표시하지 않습니다.
  - 작은 화면처럼 실제 content가 가용 높이를 초과할 때만 scrollbar를 허용합니다.
  - 기존 `지도 마커` 버튼 재클릭 toggle을 유지하며, panel 바깥을 클릭해도 닫힙니다.
  - 바깥 클릭은 dismiss만 추가하고 원래 Map/control 클릭을 소비하지 않습니다.

- **Ammo 구경 / 즐겨찾기 선택**
  - `즐겨찾기 선택`을 별도 custom popup 대신 일반 dropdown으로 표시합니다.
  - 기존 즐겨찾기 저장과 구경 filtering authority는 그대로 유지합니다.
  - 구경 dropdown과 즐겨찾기 dropdown은 같은 caliber choice presentation을 공유합니다.
  - 각 구경의 왼쪽에는 그 구경에 실제로 속한 ammo row들의 기존 item icon을 순차적으로 표시합니다.
  - 특정 탄약 하나를 caliber의 영구 대표 아이콘으로 고정하지 않습니다.
  - 두 dropdown은 같은 caliber별 순환 index와 1.4초 cadence를 공유하므로 같은 구경은 같은 animation phase로 표시됩니다.
  - animation timer는 두 dropdown이 모두 닫혀 있으면 중지합니다.
  - icon byte는 기존 Ammo `ImageCacheService` / `AmmoRow.Icon` 로딩 결과를 재사용하며 별도 네트워크/이미지 authority를 만들지 않습니다.

## 회귀 방지 / 검증

- `V1715UiRefinementsContractTests`가 다음 계약을 고정합니다.
  - header version-only + Items cleanup orange indicator
  - caliber/favorites shared animated member-ammo icon presentation
  - map marker content-height viewport + outside-click dismiss
- 최종 exact-main release gate:

```text
exact source: 4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
main CI: 33086901217 — SUCCESS
410 passed / 0 failed / 0 skipped
ProductVersion: 1.7.15+4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
win-x64 self-contained single-file publish: SUCCESS
Product UI / Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
package verification: SUCCESS
```

Release workflow:

```text
run: 33087185178
Release #48
result: SUCCESS
```

Public release readback:

```text
release id: 377926863
tag: v1.7.15
tag/release target: 4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
draft: false
prerelease: false
latest stable: true
Junhyun-Helper.zip asset id: 532481010
bytes: 80,492,565
SHA-256: 9ac3276a1a4a20905b0aa3d6452f50d5259f724ed8f960b7cfbad39f8c619f2f
SHA256SUMS.txt asset id: 532481008
SHA256SUMS.txt bytes: 86
SHA256SUMS.txt digest: 84fbabe5ef2c41d28a00305c0cd7b8ee7575fbe3c1c64fa83f7ead1c75494580
```

GitHub `/releases/latest`와 `refs/tags/v1.7.15` 모두 exact product release source를 가리키며 public ZIP digest가 exact main-CI package SHA-256과 일치합니다.

상세 공개 증거는 `docs/RELEASE_1.7.15.md`, `docs/.release-v1.7.15-status.json`, `docs/DECISION_V1.7.15_UI_REFINEMENTS.md`를 사용합니다.

## 변경하지 않은 것

- Quest / Hideout / Items 계산 의미
- 즐겨찾기 persistence 및 caliber filtering 의미
- Game Content download/validation/LKG activation 계약
- User Progress authority
- Map/MiniMap pinned donor revision `d933792b6042a51cea38dc44b686a096fe30de67`
- Scanner Item identity recognition pipeline

Scanner 안전 기준은 그대로입니다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

가격/필요 개수/slot/source/이전 프레임을 Item identity 증거로 사용하지 않으며 cross-frame OCR/visual identity cache도 추가하지 않았습니다.
