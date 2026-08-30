# 준현 헬퍼 v1.11.2

## 목적

v1.11.2는 v1.11.1 실사용에서 확인된 세 가지 유지보수 문제를 수정하는 PATCH 릴리즈다.

- 레이드 중 `교정 데이터 추가` 단축키가 교정 데이터 창을 자동으로 열어 게임 입력을 방해하던 문제
- Items / Hideout 검색창의 `×` clear UI가 Quest 등 기존 검색 UI와 다르게 항상 보이던 회귀
- screenshot 기반 Map/MiniMap player marker의 바라보는 방향이 맵 좌표계 회전을 일관되게 반영하지 않던 문제

새 Scanner 인식 방식이나 새로운 게임 데이터 의미를 추가하지 않으며, v1.11.1의 기존 제품 계약과 사용자 데이터를 보존한다.

## 교정 데이터 추가 단축키

전역 `교정 데이터 추가` 단축키는 레이드 중 capture/save 전용 동작으로 유지한다.

- 현재 screenshot/evidence가 있으면 기존 Saved Case 형식으로 저장
- 저장 성공 시 Mini Scanner에 기존 `저장 완료` transient feedback 표시
- Saved Cases/교정 데이터 창을 자동으로 열지 않음
- Main Window 또는 Scanner 탭으로 focus를 강제로 이동하지 않음
- evidence가 없으면 기존 `저장할 스캔 결과가 없습니다.` 계약 유지
- hotkey가 Ground Truth를 생성하거나 추측하지 않음
- duplicate explicit save 허용 계약 유지

즉 사용자는 레이드 중 단축키만 눌러 저장하고, 교정 데이터 검토는 레이드 종료 후 원하는 시점에 직접 수행할 수 있다.

## Items / Hideout 검색창 clear UI

v1.11.1에서 Items/Hideout에 추가된 별도 always-visible clear button을 제거하고 제품의 canonical 검색 clear 동작으로 통일한다.

- 검색어가 비어 있으면 `×`가 보이지 않음
- 검색어를 입력하면 검색창 오른쪽에 inline `×` 표시
- `×` 클릭 시 검색어 즉시 삭제
- 기존 TextChanged 검색/필터 경로 그대로 사용
- 삭제 후 검색창 keyboard focus 유지
- Quest/Items/Hideout가 동일한 product-owned clear behavior를 공유

사용자가 제공한 Quest 입력 전/후 캡처의 동작과 동일한 기준으로 맞춘다.

## Map / MiniMap player marker 방향

### 원인

screenshot에서 파싱한 player position은 각 맵의 `playerMarkerTransform` affine 변환을 거쳐 지도 좌표계로 투영하고 있었다. 반면 heading은 quaternion에서 얻은 raw yaw를 그대로 사용하는 경로가 남아 있었다.

Main Map은 Factory `+90°`, Labs `-90°`를 맵 이름으로 개별 보정했지만 MiniMap은 raw yaw를 그대로 사용했다. 또한 Reserve/Labyrinth처럼 회전 성분을 가진 affine transform은 이름 기반 예외로 완전히 표현되지 않았다.

따라서 Factory MiniMap에서 사용자가 보고한 약 90° 방향 오차는 실제 구현상 발생 가능한 회귀였다.

### 수정

player 위치에 쓰는 affine transform의 선형부 `[a,b;c,d]`를 raw heading vector에도 동일하게 적용한다.

- Factory의 기존 +90° 좌표계 의미를 자동 반영
- Labs의 기존 -90° 좌표계 의미를 자동 반영
- Reserve/Labyrinth 등 회전된 player transform도 같은 일반식으로 처리
- Main Map과 MiniMap 모두 같은 projected heading을 사용
- 맵 이름별 추가 하드코딩을 늘리지 않음

player marker의 위치와 방향이 같은 map coordinate system을 사용하도록 좌표계 계약을 통일한 것이다.

## 회귀 검증

v1.11.2에는 다음 deterministic/runtime 계약을 추가하거나 강화한다.

- correction hotkey 성공 경로가 evidence 저장 + `저장 완료`만 수행하고 modal Saved Cases 창을 열지 않음
- Items/Hideout/Quest가 canonical conditional inline search clear 동작을 공유
- Factory / Labs / Reserve / Labyrinth heading projection의 알려진 회전 결과
- 현재 map config의 모든 `playerMarkerTransform`에 대해 유효한 heading projection 확인
- Main Map과 MiniMap의 donor player render 이후 projected heading이 최종 적용되는 runtime bridge 계약
- 기존 Scanner evidence-only / no automatic Ground Truth 계약 유지

## Schema / compatibility

```text
Desktop target version: 1.11.2
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache: v1~v4 readable, v4 written
```

v1.11.1 → v1.11.2에서 mandatory Game Content migration, user.db migration, Scanner settings migration은 없다.

## 릴리즈 게이트

공개 전 다음을 모두 통과해야 한다.

- deterministic Core/Maintenance tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup / Product UI / Map / Factory / MiniMap / Scanner smoke
- v1.11.2 search clear runtime behavior smoke
- player heading projection regression contracts
- active-async graceful shutdown race
- release package root/dependency/checksum audit
- exact-main CI
- Release workflow의 tag/release/assets/checksum public readback

사용자의 실제 PC/Tarkov 환경 실사용 검증은 자동 검증과 별도로 관리한다.
