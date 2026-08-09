# MAP DATA SOURCE ANALYSIS — 지도 데이터 공급원 조사

기록일: **2026-08-09**

상태: `SOURCE VERIFIED / ARTWORK POLICY CONFIRMED / PRODUCT UX OPEN`

## 1. 결론

준현 헬퍼의 Map 기능은 별도의 비공개 지도 API나 웹 scraping을 핵심 의존성으로 둘 필요가 없습니다.

지도는 다음 세 요소를 분리해서 온라인에서 갱신합니다.

```text
json.tarkov.dev/<game-mode>/maps
→ 동적 gameplay/location data

Tarkov.dev public map metadata
→ map variant / bounds / transform / rotation / floor layers / asset path

the-hideout/tarkov-dev-svg-maps 계열 licensed artwork
→ SVG background
```

사용자는 준현 헬퍼를 **비상업적 플레이 보조 도구**로 운영하고 attribution 조건을 수용하기로 확정했습니다. 따라서 CC BY-NC-SA 4.0의 layered SVG map source를 지도 배경의 우선 후보로 사용할 수 있습니다.

핵심 효과는 다음과 같습니다.

> 게임 패치로 extract/spawn/boss/loot 등의 내용이나 지도 layout/asset이 바뀌더라도, 외부 형식이 importer가 이해하는 범위라면 준현 헬퍼의 일반 콘텐츠 업데이트 과정에서 Map Content와 지도 asset도 함께 다시 구축할 수 있습니다.

## 2. Primary dynamic source — json.tarkov.dev

`CONFIRMED AS PRIMARY DYNAMIC SOURCE`

`json.tarkov.dev`의 game-mode별 `maps` endpoint는 준현 헬퍼가 이미 사용하는 데이터 생태계 안에서 지도 gameplay 사실을 제공합니다.

Tarkov.dev의 현재 공개 source도 같은 `${gameMode}/maps` JSON을 읽고 다음 map 사실을 사용합니다.

- spawns
- extracts
- transits
- bosses / boss spawn locations
- locks
- hazards
- loot containers
- loose loot
- switches
- stationary weapons
- artillery
- BTR stops

선택 이유:

- 이미 제품의 1차 Game Content source
- regular / pve / pvp-season 지원
- 현재 Tarkov.dev 자체 지도도 같은 map data를 사용
- 비공개 endpoint reverse engineering 불필요
- 기존 candidate/validation/activation 업데이트 원칙을 그대로 적용 가능

## 3. Map rendering metadata — Tarkov.dev public map configuration

`CONFIRMED AS SUPPLEMENTAL LAYOUT SOURCE`

Tarkov.dev의 공개 `src/data/maps.json`에는 gameplay marker 자체와 별개의 표시용 metadata가 있습니다.

대표 필드:

- normalized map / variant key
- projection
- minZoom / maxZoom
- coordinate `transform`
- `coordinateRotation`
- map `bounds`
- SVG path / tile path
- base SVG layer
- floor/layer definitions
- floor height ranges
- labels
- map author / author link

이 metadata를 이용하면 기존 Tarkov-Helper처럼 지도마다 좌표 보정값과 층 정보를 수동 관리하는 비중을 크게 줄일 수 있습니다.

다만 이 공개 configuration을 `json.tarkov.dev` endpoint와 동일한 안정성 계약을 가진 API라고 가정하지 않습니다. Desktop이 화면 표시 때마다 raw GitHub 파일을 직접 읽는 방식도 사용하지 않습니다.

권장 pipeline:

```text
Content update
→ map metadata download
→ strict schema/semantic validation
→ canonical MapLayoutDefinition 변환
→ candidate DB / map asset manifest
→ read-back/relationship validation
→ active activation
```

형식이 비호환으로 바뀌면 추측해서 적용하지 않고 마지막 정상 Map layout을 보호합니다.

## 4. Map artwork / license

`CONFIRMED FOR NON-COMMERCIAL HELPER USE`

`the-hideout/tarkov-dev-svg-maps`는 community tool용 layered SVG source를 제공하며 다음 특성이 있습니다.

- multi-floor layered SVG
- application-side label/overlay 사용 가능
- license: **CC BY-NC-SA 4.0**
- attribution 필요
- non-commercial 조건
- share-alike 조건
- radar / ESP / cheat client / pixel-bot 등 부정행위 소프트웨어 사용 금지

사용자는 준현 헬퍼가 비상업적 도우미이므로 이 조건을 수용하기로 확정했습니다.

따라서 구현 시:

- 지도 화면 또는 앱의 attribution 영역에서 원저작자/프로젝트 출처 표시
- 비상업적 배포 정책 유지
- share-alike 의무가 적용되는 자산 배포 범위를 문서화
- SVG provenance/hash/license 정보를 MapAsset metadata에 보존
- Scanner 기능이 실시간 radar/ESP 성격으로 발전하지 않도록 제품 범위를 구분

Tarkov.dev site source repository의 MIT license를 지도 artwork에 확대 적용하지 않습니다.

## 5. 지도도 일반 콘텐츠 업데이트에 포함한다

`CONFIRMED PRODUCT PRINCIPLE`

Map을 수동 관리 자산으로 두지 않습니다.

권장 갱신 흐름:

```text
콘텐츠 업데이트
→ map gameplay data 다운로드
→ map layout metadata 다운로드
→ 필요한 SVG asset 다운로드/갱신
→ 형식/참조/좌표/layer/asset 검증
→ canonical MapDefinition / MapLayoutDefinition 변환
→ candidate DB + candidate map asset set
→ read-back 검증
→ 성공 시 active set 교체
→ 실패 시 기존 정상 Map 유지
```

### 자동 흡수할 수 있는 변화

- 기존 구조 안에서 marker 추가/삭제/이동
- extract/spawn/boss/loot 등의 값 변경
- 기존 metadata schema 안에서 bounds/transform/layer 값 변경
- 현재 asset 계약 안에서 SVG 내용/경로 변경
- 현재 importer가 이해 가능한 형식의 새 map/variant 추가

### 자동 추측하지 않는 변화

- source schema 의미가 비호환으로 변경
- 새 projection/coordinate model이 등장
- metadata와 SVG layer 구조가 불일치
- 필수 SVG asset 손상/누락
- license/attribution provenance가 불명확해짐

이 경우 해당 업데이트를 안전하게 중단하거나 마지막 정상 Map을 유지합니다. 일반적인 데이터 내용 변경 때문에 GPT가 다시 좌표를 해석해서 수작업으로 패치하는 구조는 만들지 않습니다.

## 6. 피해야 할 공급원 구조

핵심 source로 채택하지 않습니다.

- MapGenie 등 제3자 사이트의 숨은/private endpoint scraping
- 웹페이지 DOM 구조에 의존한 marker extraction
- 패치마다 사람이 좌표 목록을 다시 만드는 구조
- 기존 Tarkov-Helper의 `map_configs.json` / marker data를 현재 사실로 그대로 승계
- TarkovTracker 내부 `/api/tarkov/*` route를 외부 integration API로 사용

기존 Tarkov-Helper는 source of truth가 아니라 UX/알고리즘 참고 자료로만 조사합니다.

## 7. 기존 Tarkov-Helper에서 재검토할 부분

재검토 가치가 있는 항목:

- map screenshot filename coordinate parsing
- coordinate transform/calibration math
- floor detection model
- map/minimap shared state idea
- game log를 통한 map/event 감지 가능성

반대로 기존 SVG, map config transform 값, marker/extract/quest-location 정적 데이터는 새 source에서 다시 생성할 수 있는지 우선합니다.

## 8. 준현 헬퍼 canonical Map Content 제안

타입 이름/세부 schema는 구현 단계에서 조정할 수 있지만 책임 분리는 유지합니다.

```text
MapDefinition
- stable map id
- normalized name / display name
- gameplay locations
  - spawn
  - extract
  - transit
  - boss
  - lock
  - hazard
  - loot
  - switch
  - stationary weapon
  - artillery
  - BTR stop

MapLayoutDefinition
- map/variant identity
- projection
- bounds
- transform
- coordinate rotation
- zoom range
- floor/layer definitions + height ranges
- background asset reference

MapAsset
- asset URI / cache path
- source project / author attribution
- license
- source hash / downloaded hash
- active/candidate provenance
```

Gameplay facts와 presentation layout/artwork를 분리하면 지도 그림 공급원이 바뀌어도 gameplay marker 모델을 다시 설계할 필요가 없고, 위치 데이터가 바뀌어도 SVG를 사람이 다시 그릴 필요가 없습니다.

## 9. 다음 제품 설계 단계

데이터 공급원과 artwork 사용 정책은 확정되었습니다.

다음에는 기존 Tarkov-Helper Map UX를 참고하면서 사용자가 실제 Map 탭에서 필요한 기능을 정렬합니다.

확정 대상:

- 기본 지도 열람 / zoom / pan
- 층 전환
- extract/spawn/boss/loot/quest marker 종류와 on/off filter
- Quest와 Map의 상호 이동 범위
- 현재 profile 진행에 따른 Quest marker 표시 여부
- offline/cache 요구
- Scanner와 Map 사이의 허용되는 연동 범위

이 사용 경험을 확정한 뒤 canonical Map importer + Desktop Map UI 구현을 시작합니다.

## 10. 조사 근거

- `https://json.tarkov.dev/endpoints`
- `the-hideout/tarkov-dev/src/features/maps/do-fetch-maps.mjs`
- `the-hideout/tarkov-dev/src/features/maps/index.js`
- `the-hideout/tarkov-dev/src/data/maps.json`
- `the-hideout/tarkov-dev/LICENSE`
- `the-hideout/tarkov-dev-svg-maps/README.md`
- `tarkovtracker-org/TarkovTracker/docs/API.md`

제품 결정 상세: `docs/MAP_PRODUCT_DECISION.md`
