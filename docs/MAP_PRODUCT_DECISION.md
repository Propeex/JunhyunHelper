# MAP PRODUCT DECISION — 지도 공급원 및 자동 업데이트 정책

기록일: **2026-08-09**

상태: `CONFIRMED`

## 1. 제품 성격

준현 헬퍼는 **비상업적 Escape from Tarkov 플레이 보조 도구**로 운영합니다.

지도 자산 사용 시 필요한 attribution을 제품 안에 표시하는 조건을 수용합니다.

## 2. 지도 배경 자산

`the-hideout/tarkov-dev-svg-maps`의 layered SVG 지도를 준현 헬퍼 Map 배경의 우선 자산으로 채택할 수 있습니다.

적용 조건:

- CC BY-NC-SA 4.0 조건 준수
- 원저작자/프로젝트 attribution 표시
- 비상업적 사용 유지
- 필요한 경우 share-alike 의무를 배포 정책에 반영
- radar / ESP / cheat client / pixel-bot 등 부정행위 기능에 해당 자산을 사용하지 않음

Scanner 기능도 이 제한과 충돌하지 않도록 정상적인 정보 보조 범위와 실시간 부정행위 성격 기능을 구분합니다.

## 3. 지도도 Game Content Update 대상이다

`CONFIRMED`

Map은 수동으로 패치할 기능이 아니라 기존 Quest/Hideout/Ammo와 같은 원칙으로 온라인 원천에서 다시 구축합니다.

권장 갱신 단위:

```text
1. json.tarkov.dev/<game-mode>/maps
   → extract / spawn / transit / boss / loot / switch 등 gameplay/location data

2. Tarkov.dev public map metadata
   → bounds / coordinate transform / rotation / zoom / floor-layer metadata / asset reference

3. licensed SVG map artwork
   → 실제 지도 배경 asset
```

갱신 흐름:

```text
콘텐츠 업데이트 시작
→ 지도 gameplay data 다운로드
→ 지도 layout metadata 다운로드
→ 필요한 SVG asset 다운로드/갱신
→ 형식·참조·좌표·layer 검증
→ canonical Map Content 변환
→ candidate content/map asset set 구성
→ read-back 검증
→ 성공 시 active set 교체
→ 실패 시 기존 정상 지도 유지
```

따라서 게임 패치로 지도 위치 데이터나 지도 자산이 변경되어도 **외부 형식이 importer가 이해하는 범위라면 사용자가 수동으로 좌표/파일을 교체할 필요 없이 일반 데이터 업데이트로 지도도 함께 최신화**합니다.

## 4. 자동 업데이트의 경계

자동 업데이트는 무조건 모든 미래 변경을 추측해 처리한다는 뜻은 아닙니다.

다음은 자동 흡수 대상입니다.

- 기존 구조 안에서 marker가 추가/삭제/이동
- 탈출구/스폰/보스/루팅 위치 등의 값 변경
- 기존 metadata schema 안에서 transform/bounds/layer 값 변경
- 기존 asset 계약 안에서 SVG 내용 또는 파일 경로 변경
- 새 map/variant가 현재 importer 규칙으로 해석 가능한 형태로 추가

다음은 fail-safe 대상입니다.

- source schema의 의미가 비호환으로 변경
- 좌표계/투영 방식이 importer가 모르는 방식으로 추가
- SVG layer 구조가 metadata와 맞지 않음
- 필수 asset이 누락되거나 손상됨
- attribution/license 정보가 불명확해짐

이 경우 프로그램이 임의로 추측하지 않고 업데이트를 거부하거나 해당 Map의 마지막 정상본을 유지합니다.

## 5. 설계 원칙

Gameplay data와 지도 그림을 결합하되 서로 독립된 canonical 의미로 관리합니다.

```text
MapDefinition
→ 게임 사실: spawn / extract / transit / boss / loot / hazard 등

MapLayoutDefinition
→ 표시 사실: bounds / transform / rotation / floor / layer / asset reference

MapAsset
→ 실제 SVG background + provenance / attribution / hash
```

이 분리를 통해 나중에 배경 지도 공급원을 바꾸더라도 gameplay marker 모델을 다시 만들 필요가 없고, 반대로 게임 위치 데이터가 바뀌어도 SVG 자체를 수작업으로 수정할 필요가 없습니다.

## 6. 다음 단계

데이터 공급원과 비상업적 자산 사용 정책은 확정했습니다.

다음 제품 협의 대상은 **Map 탭에서 사용자가 실제로 어떻게 지도를 사용할지**입니다.

- zoom / pan
- 층 전환
- marker 종류 및 표시/숨김
- Quest marker와 현재 진행 상태 연동
- Quest ↔ Map 이동
- offline/cache 범위
- Scanner와 Map의 정상적인 연동 범위

이 사용 경험을 확정한 뒤 canonical Map importer와 Desktop Map UI를 구현합니다.
