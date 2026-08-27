# DATA VALIDATION — 게임 데이터 검증 및 안전한 갱신 규칙

상태: `CONFIRMED — 큰 틀`

이 문서는 준현 헬퍼가 외부 데이터를 자동으로 갱신하면서도 잘못된 DB를 활성화하지 않기 위한 검증 원칙을 정의합니다.

목표는 두 가지입니다.

1. 일반적인 Tarkov 패치의 데이터 내용 변화는 자동으로 받아들인다.
2. 변환기가 이해하지 못하는 구조/의미 변화는 조용히 잘못 처리하지 않는다.

---

# 1. 기본 업데이트 흐름

```text
외부 원천 다운로드
      ↓
원본 응답 형식 검증
      ↓
준현 헬퍼 내부 모델 변환
      ↓
참조/의미 검증
      ↓
후보 ContentSnapshot 생성
      ↓
후보 DB 자체 검증
      ↓
기존 정상 snapshot 대비 completeness 검증
      ↓
성공한 경우에만 활성 콘텐츠 교체
```

실패한 후보 데이터는 현재 정상 콘텐츠를 변경하지 않습니다.

---

# 2. 검증 수준

## 2.1 Transport 검증

- HTTP 요청 성공 여부
- 응답이 JSON인지
- 제한 시간 내 응답했는지
- 필요한 엔드포인트를 모두 받을 수 있었는지

한 엔드포인트만 실패했는데 나머지 데이터로 억지로 불완전한 DB를 만들지 않습니다.

## 2.2 Envelope/스키마 검증

`json.tarkov.dev`의 기본 계약에서 최소한 확인합니다.

- 최상위 `data` 존재
- 번역 지원 응답에서 `translations`가 있으면 배열인지
- 예상 주요 collection이 배열 또는 ID-keyed object 형태인지
- 필수 ID가 문자열로 존재하는지

현재 실제 소비 코드에서도 동일 엔드포인트의 collection이 배열/객체 형태를 모두 가질 수 있으므로, 내부 importer는 두 표현을 정규화하되 의미가 다른 타입 변화는 허용하지 않습니다.

## 2.3 Entity 검증

### 공통

- ID가 비어 있지 않음
- 내부 영구 ID로 이름을 사용하지 않음
- 동일 엔티티 영역에서 충돌하는 ID가 없음

### Quest

- quest ID 존재
- prerequisite task reference가 해석 가능
- trader/map reference가 존재하거나 명시적 null
- objective는 `(questId, objectiveId)` 기준으로 충돌하지 않음
- item 관련 objective의 item reference가 objective 의미에 맞게 해석 가능
- count가 필요한 objective에서 유효한 수량 존재

### Hideout

- station ID 존재
- level 번호가 유효
- item requirement의 item reference가 해석 가능
- station-level requirement가 존재하는 시설을 참조
- 수량이 음수가 아님

### Ammo

- 탄약으로 분류된 ItemId가 존재
- 핵심 표시/정렬에 사용하는 수치 타입이 기대 타입과 일치
- 수급처가 참조하는 trader/station/item이 해석 가능

## 2.4 관계 무결성 검증

내부 DB 활성화 전 최소 관계를 검사합니다.

예:

- Quest → Trader
- Quest → Map
- QuestPrerequisite → Quest
- material QuestObjective → Item
- QuestItemRequirement → Item
- HideoutRequirement → Item
- HideoutStationRequirement → HideoutStation
- Ammo → Item
- AmmoAcquisition → Trader/HideoutStation

표시용 선택 정보의 누락과 핵심 계산 관계의 누락을 구분합니다.

### Quest objective `item/items` / `questItem` semantic boundary

`json.tarkov.dev`의 objective 참조는 필드 의미를 구분해서 검증합니다.

- `item/items` — canonical inventory item reference. `Submit`, `FindOrCollect`, `Sell`, `Other` 등 objective kind와 관계없이 `/items`에 존재해야 하며 dangling reference는 Fatal입니다.
- `questItem` / 내부 `QuestItemId` — 별도의 quest-only entity reference. canonical `/items` 부재만으로 Fatal 처리하지 않습니다.

이 예외는 **`QuestItemId`의 canonical `/items` 존재 요구에만** 적용합니다. 다음 참조 검증은 모두 기존처럼 fail-closed입니다.

- 모든 QuestObjective `item/items`
- 일반 `QuestItemRequirement`
- Hideout item requirement
- Ammunition item/currency/requirement

따라서 실제 material/canonical item의 dangling reference는 objective kind와 무관하게 계속 업데이트를 차단합니다.

---

# 3. 오류 등급

## 3.1 Fatal — 새 콘텐츠 활성화 금지

예:

- 필수 endpoint 다운로드 실패
- `data` envelope 자체가 사라짐
- 핵심 collection 타입이 이해할 수 없는 형태로 변경
- quest/item/hideout의 필수 ID 대량 누락
- 필요 아이템 계산의 핵심 item reference가 해석 불가능
- 내부 DB 무결성 실패
- 필수 importer에서 알 수 없는 구조 때문에 의미를 보장할 수 없음
- 기존 정상 snapshot 대비 핵심 영역이 안전 retained floor 아래로 급감

Fatal 오류가 있으면 현재 정상 콘텐츠를 그대로 유지합니다.

## 3.2 Warning — 활성화 가능하지만 기록

예:

- 일부 Wiki URL 누락
- 일부 아이콘 URL 누락
- 한국어 번역 한두 건 누락 후 영어 fallback 사용
- 현재 준현 헬퍼가 사용하지 않는 선택 필드 추가
- 표시만 하는 선택 정보의 일부 누락

Warning은 콘텐츠 manifest와 로그에 남깁니다.

## 3.3 Unknown Semantic — 보수적으로 처리

구조는 읽을 수 있지만 기존에 없던 의미가 나타난 경우입니다.

예:

- 새로운 quest requirement type
- 새로운 item objective type
- 기존과 다른 ammo acquisition relation

원칙:

- 제품 정확도에 영향을 주지 않는 필드는 무시 + warning 가능
- 퀘스트 해금/필요 아이템처럼 핵심 계산 결과에 영향을 줄 가능성이 있으면 Fatal로 승격

자동 추측보다 업데이트 중단을 선택합니다.

---

# 4. 번역 검증

한국어 표시 기본 정책:

1. 한국어 번역 사용
2. 해당 key가 없으면 영어 fallback
3. 영어도 없으면 원천의 안전한 식별 가능한 문자열 사용 또는 표시 누락 처리

중요:

- ID와 번역 key/표시 문자열을 같은 것으로 취급하지 않습니다.
- 번역 적용 과정이 quest/objective/item ID를 변경할 수 없어야 합니다.
- 위험한 object key(`__proto__`, `constructor`, `prototype`)는 번역 lookup 대상으로 사용하지 않습니다.

---

# 5. 필요 아이템 정확도 검증

준현 헬퍼의 핵심 기능이므로 일반 UI 표시보다 더 엄격하게 검사합니다.

## 5.1 Quest 요구 아이템

Importer는 objective를 최소 다음 범주로 분류합니다.

- 실제 제출/인도 요구
- 단순 획득/발견
- 판매
- 기타

`필요 아이템` 합산 규칙에 포함되는 objective type은 명시적으로 허용 목록으로 관리합니다.

새로운 item objective type이 나타나고 그 의미를 모르면 조용히 제외하지 않습니다. **필요 수량 누락 가능성이 있으므로 데이터 갱신 검증에서 발견**해야 합니다.

`Other`를 포함한 모든 objective의 `item/items`는 canonical `/items` 참조로 엄격하게 검증합니다. `questItem`만 별도의 quest-only entity 계약으로 분리하며, 새로운 objective type 자체가 필요한 수량 계산에 영향을 줄 가능성이 있으면 importer/contract 검증에서 별도로 조사합니다.

## 5.2 Hideout 요구 아이템

시설/레벨별 item requirement는 다음을 검증합니다.

- item ID
- count
- target station/level
- FIR 속성이 존재하면 보존

## 5.3 FIR 계산 회귀 규칙

예: 총 15개 필요, 그중 FIR 5개 필요.

- FIR 5 + 일반 0 → 미충족
- FIR 5 + 일반 10 → 충족
- FIR 15 + 일반 0 → 충족
- FIR 4 + 일반 11 → 미충족

FIR 아이템 하나를 FIR 요구량과 전체 요구량에서 이중 소비한 것으로 계산하지 않습니다.

---

# 6. 데이터 수량 변화와 completeness 정책

Tarkov 대형 패치에서는 실제로 데이터가 대량 추가/삭제될 수 있으므로 **절대 행 수나 특정 버전의 고정 개수**를 정상성 기준으로 사용하지 않습니다.

다만 이미 검증된 active snapshot이 존재하는 설치에서는, 외부 원천의 부분 응답이 구조적으로는 읽히지만 내용 대부분이 빠진 작은 catalog로 변환될 수 있습니다. 이를 정상 패치로 오인해 활성화하지 않도록 `ContentUpdateCompletenessGuard`가 last-known-good baseline과 candidate를 비교합니다.

현재 런타임 계약은 다음과 같습니다.

- 비교 가능한 핵심 entity/relationship 영역은 candidate가 baseline의 **50% 이상**을 유지해야 합니다.
- 구현상 안전 floor는 `max(1, floor(baselineCount × 0.50))`이며, 그보다 작은 candidate는 `Fatal`입니다.
- 보호 영역에는 item/trader/map/quest/objective/quest-item/hideout/ammo/edition뿐 아니라 quest prerequisite, map-location relation, hideout level/item relation, ammo acquisition/relation 등이 포함됩니다.
- 한국어 번역 coverage와 item/quest Wiki·icon/image 같은 주요 표시 리소스도 충분히 큰 기존 baseline이 있을 때 같은 급감 방어를 적용합니다.
- 선택/표시 coverage는 baseline 자체가 매우 작은 경우 변동성이 크므로 상대 급감 판정에서 제외할 수 있습니다.
- 첫 설치처럼 정상 baseline이 없으면 상대 수량 비교만으로 candidate를 거부하지 않습니다.

즉 활성화 판단은 다음을 함께 사용합니다.

- 구조적 무결성
- 필수 관계 해석 여부
- 변환기 지원 여부
- 기존 정상 데이터 대비 비정상적인 대량 소실 여부

이 50% 기준은 "정상 Tarkov 데이터 개수"를 뜻하는 고정 사양이 아니라 **부분 payload/상류 장애로부터 기존 정상 데이터를 보호하는 상대적 안전장치**입니다. 실제 Tarkov의 정상적인 대규모 개편이 이 안전장치에 걸리면 임계치를 즉시 완화하지 않고 upstream payload와 제품 의미를 먼저 검토한 뒤, 검증된 변경만 반영합니다.

---

# 7. Safe Activation

새 콘텐츠는 현재 사용 중인 DB를 직접 수정하면서 만들지 않습니다.

개념적 상태:

- `active` — 현재 사용 중인 검증된 콘텐츠
- `candidate` — 새로 빌드 중인 콘텐츠
- `previous` — 직전 검증 콘텐츠(복구용, 구현 필요성 확정 시 유지)

순서:

1. candidate를 별도 위치에서 완성
2. candidate 자체의 의미/참조 검증 수행
3. 기존 정상 snapshot이 있으면 completeness 검증 수행
4. manifest 생성
5. 모든 검증 성공
6. 한 번의 활성화 단계로 active 교체

실패하면 candidate만 폐기하고 active를 유지합니다.

정확한 저장/교체 구현은 현재 `TarkovContentUpdateService`와 snapshot store를 기준으로 확인하며, 이 개념 문구만 보고 별도 업데이트 경로를 새로 만들지 않습니다.

---

# 8. Content Manifest

성공한 ContentSnapshot에는 최소한 다음을 기록합니다.

- 내부 `schemaVersion`
- `builtAt`
- game mode
- 사용 언어
- source endpoint 목록
- 가능한 source hash/version
- 각 핵심 entity 수량
- Warning 목록
- DB/content hash

이 manifest는 데이터의 진실 자체가 아니라 **어떤 입력으로 어떤 콘텐츠를 만들었는지 추적하는 진단 정보**입니다.

---

# 9. 테스트 전략

## 9.1 Deterministic Contract Fixture

인터넷 없이 고정된 작은 원본 fixture를 사용합니다.

반드시 포함할 사례:

- 퀘스트 선행 조건
- 여러 accepted status
- 진영/레벨/평판/프레스티지 조건
- item objective 제출/획득/판매/기타 구분
- 실제 회귀 ID의 `QuestItemId`가 canonical `/items`에 없더라도 quest-only 참조로 허용
- `Other`를 포함한 모든 `item/items`의 실제 dangling canonical item 차단
- 대체 가능한 여러 item
- FIR/비FIR 요구
- 은신처 item/station/trader/skill requirement
- ammo item + 상인 판매/교환/제작 관계
- 한국어 번역 + 영어 fallback
- 중복 objective ID가 다른 quest에 존재하는 경우
- 정상 baseline 대비 핵심 영역의 suspicious shrink 차단

목적은 **준현 헬퍼의 변환 공식과 활성화 안전 계약이 바뀌지 않았는지** 확인하는 것입니다.

## 9.2 Live Source Contract Test

실제 최신 외부 Tarkov 데이터 원천을 대상으로 수행합니다.

확인:

- 현재 endpoint 계약을 importer가 이해하는지
- 새로운 requirement/objective/acquisition 종류가 나타났는지
- 참조 무결성이 유지되는지
- Regular/PvE 각각의 콘텐츠를 만들 수 있는지
- source warning과 주요 entity 수량이 조사 가능한 형태로 남는지

이 검사는 `.github/workflows/live-data-probe.yml`에서 일반 CI와 분리해 매일 예약 실행하고 필요할 때 수동 실행합니다. 외부 네트워크/상류 장애는 저장소 코드와 독립적으로 발생할 수 있으므로 PR/main의 결정론적 CI gate로 사용하지 않습니다.

Live Probe는 current/baseline snapshot을 보유한 설치가 아니므로 `ContentUpdateCompletenessGuard`의 상대 급감 검증을 대체하지 않습니다. Probe는 **현재 외부 계약을 importer/validator가 이해하는지**, 런타임 guard는 **기존 정상 데이터를 부분 payload로 교체하지 않는지**를 각각 검증합니다.

## 9.3 Domain Regression Test

API와 무관한 순수 계산 테스트입니다.

예:

- 퀘스트 완료 → 후속 퀘스트 Current
- 레벨 미달 → Locked
- 다른 프로필의 완료 상태가 섞이지 않음
- 은신처 레벨 변경 → 필요한 재료 재계산
- 퀘스트 완료 → 해당 Quest source 필요량 제거
- FIR 혼합 재고 계산

---

# 10. 기존 Tarkov-Helper에서 얻은 교훈의 사용 방식

기존 구현의 코드를 진실로 사용하지 않습니다.

다만 실제로 발생했던 다음 실패는 회귀 테스트 아이디어로 유지합니다.

- 획득 목표를 제출 재료로 잘못 합산
- sellItem 데이터를 필요 재료로 잘못 해석
- objective 조건 selector를 canonical material item으로 오판해 정상 업데이트를 거부
- 번역 과정에서 objective ID가 손상
- objective ID의 전역 유일성 오가정
- FIR/일반 수량 이중 계산
- API 업데이트 중단 후 불완전 콘텐츠 활성화
- 정상 DB를 실패한 업데이트로 덮어씀

이 실패들은 새 설계가 독립적으로 만들어진 뒤 **방어 검증 목록**으로만 사용합니다.
