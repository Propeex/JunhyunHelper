# DATA SOURCE AUDIT — 최신 게임 데이터 공급원 검증

검증일: **2026-08-08**

상태: `PARTIALLY CONFIRMED — 핵심 endpoint 구조 확인, 구현 전 라이브 fixture 계약 고정 필요`

이 문서는 준현 헬퍼가 의존할 외부 게임 데이터가 현재 어떤 범위를 제공하는지 기록합니다.

---

# 1. 1차 원천 — json.tarkov.dev

현재 endpoint catalog에서 확인된 endpoint:

| Endpoint | 용도 | 준현 헬퍼 |
|---|---|---|
| `tasks` | tasks, quest items, achievements, prestige | 퀘스트 핵심 |
| `hideout` | hideout stations | 은신처 핵심 |
| `items` | items, categories, player levels, skills 등 | 공통 아이템 + 탄약 속성 |
| `traders` | traders | 퀘스트 조건/탄약 수급처 참조 |
| `maps` | maps 등 | 퀘스트 분류/참조 |
| `barters` | trader barter offers | 탄약 교환 수급처 후보 |
| `crafts` | hideout crafts | 탄약 제작 수급처 후보 |

현재 game mode:

- `regular`
- `pve`
- `pvp-season`

현재 언어 목록에 `ko`가 포함되어 있습니다.

따라서 준현 헬퍼의 핵심 API 데이터 영역인 **퀘스트·은신처·탄약을 하나의 원천 계열에서 구축할 수 있는 가능성이 충분히 확인**되었습니다.

---

# 2. 현재 운영 소비자 대조 — TarkovTracker

TarkovTracker의 최신 문서와 구현은 제3자 클라이언트가 오래된 GraphQL보다 `json.tarkov.dev` 정적 JSON을 직접 사용하도록 권장합니다.

현재 소비 코드에서 확인된 원천 데이터의 의미:

## Tasks

- task ID/name
- trader/map
- minimum player level
- faction
- task requirements + accepted statuses
- trader requirements
- required prestige
- objectives
- fail conditions
- start/finish/failure rewards
- quest item references

## Hideout

- station
- levels
- item requirements
- station level requirements
- skill requirements
- trader requirements
- crafts

## Items

- item ID/name/short name
- categories
- item properties
- 이미지/Wiki 관련 링크

## Prestige

`tasks` payload 내부 prestige를 소비합니다.

TarkovTracker 현재 문서에서는 upstream에 PvE prestige 데이터가 없다고 명시합니다. 따라서 준현 헬퍼의 `prestige` 프로필 값은 모든 게임 모드에서 필수라고 가정하지 않습니다.

---

# 3. 번역 구조

`json.tarkov.dev`는 번역 가능한 endpoint에서:

- base document
- `{endpoint}_ko`
- `{endpoint}_en`
- base document의 `translations` JSONPath 목록

구조를 사용합니다.

준현 헬퍼 정책:

1. base 원본의 ID/관계를 먼저 보존
2. 표시 문자열에만 한국어 번역 적용
3. 한국어 key 누락 시 영어 fallback
4. 번역이 ID/참조 관계를 변경할 수 없게 함

---

# 4. 퀘스트 데이터 적합성

현재 큰 틀 제품 요구에는 충분한 데이터가 확인됩니다.

준현 헬퍼가 계산하려는 것:

- 현재 프로필 조건 만족 여부
- 선행 퀘스트
- 레벨
- 진영
- 상인 조건
- 프레스티지
- 현재/완료 퀘스트

현재 `tasks` 구조와 소비 코드가 이 범위를 표현합니다.

### 아직 별도 확인이 필요한 것

- 에디션별 퀘스트 허용/제외 규칙의 안정적인 원천
- 이벤트성/시즌성 예외의 자동 판정 원천
- 실패/분기 세부 UX

이들은 현재 큰 틀 구현을 막지 않으며 세부 설계 단계에서 검증합니다.

---

# 5. 은신처 데이터 적합성

`hideout` endpoint는 준현 헬퍼의 현재 큰 틀 요구에 충분합니다.

현재 필요한 핵심:

- 시설 목록
- 각 시설의 레벨
- 레벨별 요구 아이템

이 정보는 직접 제공됩니다.

시설/상인/스킬 선행 조건도 원천에 존재하지만, **데이터가 존재한다는 이유만으로 사용자 프로필에 스킬 등 입력을 추가하지 않습니다.**

현재 제품 요구에서 중요한 것은 실제 시설 레벨을 사용자가 입력하고, 그 상태를 기준으로 필요한 업그레이드 재료를 계산하는 것입니다.

---

# 6. 탄약 데이터 적합성

현재 endpoint catalog상 탄약 기능에 필요한 원천 조합은:

```text
items → 탄약 기본/성능 정보
traders + barters → 상인 판매/교환 관계
crafts + hideout → 제작 관계
```

로 설계할 수 있습니다.

`items`의 `properties`는 아이템 유형별 상세 속성을 담는 구조로 현재 소비됩니다.

다만 탄약 세부 수치의 정확한 raw key/type과 `barters`, `crafts`의 현재 raw shape는 구현 시작 시 **실제 응답을 fixture로 저장하여 계약 테스트로 고정**합니다.

이것은 GPT가 패치마다 해석한다는 의미가 아니라, 최초 importer 구현 시 외부 계약을 정확히 정의하는 개발 작업입니다.

---

# 7. TarkovTracker overlay에 대한 판단

현재 TarkovTracker는 task 데이터에 community overlay corrections를 적용합니다.

준현 헬퍼는 이를 자동으로 그대로 채택하지 않습니다.

이유:

- `json.tarkov.dev`를 기본 원천으로 삼는 설계를 먼저 독립적으로 유지해야 함
- 보정원이 필요하면 그 목적/신뢰성/갱신 방식도 공식 데이터 원천으로 취급해야 함
- 숨은 수동 patch가 늘어나면 다시 유지보수 불가능한 구조가 됨

정책:

1. 먼저 `json.tarkov.dev` 원본으로 canonical DB를 구축
2. 검증 과정에서 실제 게임과 의미 있는 누락/오류가 반복 확인되면 보정원 도입 검토
3. 도입 시 `Base Source → Explicit Overlay → Validation` 순서를 공식 파이프라인으로 기록
4. 코드 내부의 개별 quest ID 하드코딩 patch는 금지

---

# 8. 현재 결론

현재 데이터 공급원을 이유로 준현 헬퍼의 핵심 설계를 변경할 필요는 없습니다.

확인된 목표 파이프라인:

```text
json.tarkov.dev
  ├─ tasks
  ├─ hideout
  ├─ items
  ├─ traders
  ├─ maps
  ├─ barters
  └─ crafts
        ↓
각 Importer + 검증
        ↓
준현 헬퍼 Game Content
        ↓
Quest / Hideout / Needed Items / Ammo
```

현재 구현 전 남은 데이터 작업은 **원천이 있는지 찾는 것**이 아니라, 각 endpoint에서 준현 헬퍼가 사용할 최소 필드의 실제 raw shape를 fixture로 고정하고 자동 계약 테스트를 만드는 것입니다.
