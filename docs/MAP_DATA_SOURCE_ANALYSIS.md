# MAP DATA SOURCE ANALYSIS — 지도 데이터 공급원 조사

기록일: **2026-08-09**

상태: `SOURCE VERIFIED / PRODUCT DESIGN PROPOSED`

## 1. 결론

준현 헬퍼의 Map 기능은 별도의 비공개 지도 API나 웹 scraping을 핵심 의존성으로 둘 필요가 없습니다.

현재 가장 적합한 구조는 다음처럼 **게임 위치 데이터**, **지도 좌표/렌더링 메타데이터**, **지도 그림 자산**을 분리하는 것입니다.

```text
json.tarkov.dev/<game-mode>/maps
→ 동적 gameplay/location data

Tarkov.dev public map metadata
→ map variant / bounds / transform / rotation / floor layers / asset path

licensed map artwork
→ SVG 또는 tile background
```

이렇게 분리하면 패치로 extract/spawn/loot 등이 바뀌어도 준현 헬퍼가 온라인 데이터를 다시 받아 canonical Map Content를 재구축할 수 있습니다.

## 2. Primary dynamic source — json.tarkov.dev

`PROPOSED AS PRIMARY SOURCE`

`https://json.tarkov.dev/endpoints`의 현재 catalog에는 다음 endpoint가 공식적으로 노출됩니다.

```text
/{{gameMode}}/maps
```

endpoint 설명에는 maps / goon reports / mobs(bosses) / loot containers / stationary weapons가 포함됩니다.

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

따라서 준현 헬퍼가 이미 사용하는 `json.tarkov.dev` 생태계 안에서 상당 부분의 interactive marker data를 자동 갱신할 수 있습니다.

### 선택 이유

- 이미 제품의 1차 Game Content source
- regular / pve / pvp-season 지원
- 제3자 도구가 직접 사용할 수 있는 static JSON endpoint
- 현재 Tarkov.dev 자체 지도도 같은 map data를 사용
- 비공개 endpoint reverse engineering이 필요하지 않음
- 기존 content update의 candidate/validation/activation 원칙을 그대로 적용 가능

## 3. Map rendering metadata — Tarkov.dev public map configuration

`PROPOSED AS SUPPLEMENTAL PRESENTATION SOURCE`

Tarkov.dev의 공개 `src/data/maps.json`에는 gameplay marker 자체와 다른 종류의 정보가 있습니다.

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

예를 들어 Ground Zero는 game coordinate와 화면 지도 사이를 맞추기 위한 transform/bounds/rotation, ground/2F/3F/garage layer, 각 layer의 height range와 asset path를 함께 정의합니다.

이 metadata를 이용하면 기존 Tarkov-Helper처럼 지도마다 좌표 보정값을 수동 관리하는 비중을 크게 줄일 수 있습니다.

### 취급 원칙

이 파일은 `json.tarkov.dev`의 정식 game-data endpoint와 같은 안정성 계약을 가진 API라고 가정하지 않습니다.

따라서 Desktop이 GitHub raw 파일을 매 화면 직접 읽는 구조는 사용하지 않습니다.

권장 pipeline:

```text
Content update
→ map metadata download
→ strict schema/semantic validation
→ canonical MapLayoutDefinition 변환
→ candidate content.db
→ read-back/relationship validation
→ active activation
```

형식이 비호환으로 바뀌면 추측해서 적용하지 않고 마지막 정상 Map layout을 보호합니다.

## 4. Map artwork / license

`OPEN — LICENSE STRATEGY REQUIRED BEFORE BUNDLING`

Tarkov.dev의 interactive map metadata는 `assets.tarkov.dev`의 SVG/tile asset을 참조합니다.

Tarkov.dev site source repository 자체는 MIT이지만, **지도 그림의 저작권/라이선스를 site source license와 동일하다고 간주하면 안 됩니다.**

현재 `the-hideout/tarkov-dev-svg-maps` 프로젝트는 지도 SVG를 community tool용 source로 제공하며 다음을 명시합니다.

- multi-floor layered SVG
- application-side label/overlay 사용 가능
- license: **CC BY-NC-SA 4.0**
- attribution 필요
- non-commercial 조건
- share-alike 조건
- radar / ESP / cheat client / pixel-bot 등 부정행위 소프트웨어에서의 사용을 명시적으로 금지

따라서 실제 지도 배경을 준현 헬퍼에 포함하거나 cache할 때는 이 조건을 공식 attribution 및 배포 정책에 반영해야 합니다.

특히 향후 Scanner 기능은 Map 자산 라이선스의 cheating prohibition과 충돌하지 않도록 **화면 인식 보조 기능과 실시간 위치 추적/레이더 기능을 명확히 구분**해야 합니다.

Raster tile의 재배포 조건은 이번 조사에서 별도 명시 문서를 확인하지 못했으므로 SVG와 동일하다고 추정하지 않습니다. 라이선스가 명확해질 때까지 bundle 대상으로 확정하지 않습니다.

## 5. 피해야 할 공급원 구조

현재 핵심 source로 채택하지 않습니다.

- MapGenie 등 제3자 사이트의 숨은/private endpoint scraping
- 웹페이지 DOM 구조에 의존한 marker extraction
- 패치마다 사람이 수동으로 좌표 목록을 다시 만드는 구조
- 기존 Tarkov-Helper의 `map_configs.json` / marker data를 현재 사실로 그대로 승계
- TarkovTracker 내부 `/api/tarkov/*` route를 외부 integration API로 사용

TarkovTracker의 현재 API 문서도 자체 `/api/tarkov/*` route는 first-party internal surface이며 제3자 호환성을 보장하지 않고, 외부 도구는 `json.tarkov.dev`를 직접 사용하도록 안내합니다.

## 6. 기존 Tarkov-Helper에서 재검토할 부분

기존 구현은 source of truth가 아니라 UX/알고리즘 참고 자료로만 조사합니다.

기존 salvage audit에서 재검토 가치가 있다고 분류된 항목:

- map screenshot filename coordinate parsing
- coordinate transform/calibration math
- floor detection model
- map/minimap shared state idea
- game log를 통한 map/event 감지 가능성

반대로 기존 SVG, map config transform 값, marker/extract/quest-location 정적 데이터는 출처와 최신성이 검증되기 전까지 승계하지 않습니다.

## 7. 준현 헬퍼 canonical Map Content 제안

아직 제품 UI를 확정하는 단계는 아니므로 타입 이름/세부 schema는 `PROPOSED`입니다.

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
- attribution
```

Gameplay facts와 presentation layout을 별도 모델로 두면 지도 그림 공급원이 바뀌더라도 extract/spawn 등 gameplay data 모델을 다시 설계할 필요가 없습니다.

## 8. 다음 제품 설계 단계

데이터 공급원 후보는 확보되었습니다. 다음에는 기존 Tarkov-Helper Map UX를 참고하면서 사용자가 실제 Map 탭에서 필요한 기능을 정렬해야 합니다.

확정이 필요한 제품 범위 예:

- 기본 지도 열람/zoom/pan
- 층 전환
- extract/spawn/boss/loot/quest marker 종류와 on/off filter
- Quest와 Map의 상호 이동 범위
- 현재 profile 진행에 따른 Quest marker 표시 여부
- offline/cache 요구
- Scanner와 Map 사이의 허용되는 연동 범위

이 범위는 사용자의 Map 사용 의도를 확인한 뒤 `CONFIRMED`로 전환합니다.

## 9. 조사 근거

- `https://json.tarkov.dev/endpoints` — 현재 static JSON endpoint catalog
- `the-hideout/tarkov-dev/src/features/maps/do-fetch-maps.mjs` — `${gameMode}/maps` 소비 및 marker data 처리
- `the-hideout/tarkov-dev/src/features/maps/index.js` — Map UI에 병합되는 gameplay fields
- `the-hideout/tarkov-dev/src/data/maps.json` — transform / bounds / layers / SVG/tile references
- `the-hideout/tarkov-dev/LICENSE` — site source MIT
- `the-hideout/tarkov-dev-svg-maps/README.md` — SVG map project purpose/license/restrictions
- `tarkovtracker-org/TarkovTracker/docs/API.md` — third-party game-data integration은 `json.tarkov.dev` 직접 사용 권고
