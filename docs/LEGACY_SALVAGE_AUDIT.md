# LEGACY_SALVAGE_AUDIT — 기존 Tarkov-Helper 회수 분석

이 문서는 `Propeex/Tarkov-Helper`를 새 **준현 헬퍼** 개발의 참고 자료로 분석한 결과를 기록합니다.

목적은 기존 제품을 계승하거나 벤치마킹하는 것이 아닙니다. **새 제품의 확정된 의도와 구조에 실제로 도움이 되는 코드, 테스트, 데이터 자산, 실패 경험만 선별해서 회수**하는 것입니다.

분석 기준:

1. 준현 헬퍼의 현재 제품 의도와 맞는가
2. 최신 데이터 기반·자동 변환 원칙을 강화하는가
3. 기존의 잘못된 가정이나 수작업 데이터 의존을 끌고 오지 않는가
4. 현재 게임/API에서 다시 검증할 수 있는가
5. 새 구조에서 더 단순하고 안전하게 재구성할 수 있는가

> 이 문서의 `회수 후보`는 곧바로 새 제품에 채택한다는 뜻이 아닙니다. 기술 스택과 세부 설계가 정해질 때 다시 검증하여 실제 재사용 여부를 결정합니다.

---

## 1. 전체 결론

기존 Tarkov-Helper에서 가장 가치 있는 것은 **완성된 기능/UI가 아니라 최근에 만들어진 데이터 갱신 안전장치와 수많은 실패 회귀 테스트**입니다.

특히 준현 헬퍼의 핵심 원리인

`온라인 원천 데이터 → 검증 → 변환 → 내부 DB 재구축 → 제품 기능 사용`

과 직접 맞아떨어지는 구현이 기존 저장소 후반부에 존재합니다.

반면 기존 퀘스트 판정 엔진, 프로필 처리, 거대한 WPF 코드비하인드, 레거시 호환 분기 등은 새 제품에 그대로 가져오면 오히려 설계를 오염시킬 가능성이 큽니다.

### 우선순위 요약

#### A — 높은 가치: 새 설계에 적극 반영할 후보

- 콘텐츠 스테이징 → 검증 → 안전한 교체 → 이전 정상본 복구 구조
- API → 내부 DB 변환 빌더의 책임 분리
- DB 무결성/참조/비정상 급감 검증
- 원본 API provenance/SourceJson 및 빌드 메타데이터 보존
- 결정론적 API fixture + DatabaseSmoke 회귀 테스트 체계
- 퀘스트·은신처에서 필요 아이템을 파생하는 계산 개념
- FIR/비FIR 혼합 요구량 계산 규칙과 회귀 테스트
- 필요한 아이콘만 다운로드·검증·재사용하는 이미지 동기화 구조
- 사용자 진행 데이터와 재생성 가능한 콘텐츠 DB의 분리
- 비동기 저장 직렬화/reset barrier 개념
- 탄약의 상인 판매·교환·은신처 제작 수급처를 데이터에서 파생하는 방식

#### B — 아이디어/알고리즘/테스트만 회수

- 퀘스트 내부 모델에서 확인된 조건 종류 체크리스트
- QuestDbService의 ID 기반 관계 연결 및 준비 완료 후 메모리 atomic swap 패턴
- 은신처의 station / level / item / trader / skill / station dependency 관계 구조
- 맵 스크린샷 파일명 좌표 파싱
- 맵 좌표 변환/보정 수학
- 층 감지 모델
- 게임 로그 감시를 통한 맵/이벤트 자동 감지 가능성
- 지도/미니맵의 상태 공유라는 제품 아이디어

#### C — 보존만 하고 현재는 채택 금지

- 기존 SVG 지도 자산
- `map_configs.json`의 좌표 변환값·aliases·층 정보
- 기존 맵 마커/탈출구/퀘스트 위치 데이터
- `tarkov-data-overlay` 보정 데이터 의존
- 게임 로그 정규식/하드코딩된 맵 별칭

이들은 출처, 라이선스, 현재 게임 정확성 또는 새 데이터 공급원을 다시 검증해야 합니다.

#### D — 새 제품에 가져오지 않음

- 현재 `QuestProgressService` 중심의 퀘스트 해금 판정 엔진
- PVP로 고정된 현재 `ProfileService`
- 레거시 마이그레이션이 누적된 `UserDataDbService`의 스키마/구조
- `MainWindow`/`MapPage` 중심의 거대한 WPF code-behind 구조
- normalized name을 사용자 데이터의 영구 식별자로 쓰는 방식
- 탄약 방탄 등급 0~6 효율 휴리스틱을 게임 사실처럼 사용하는 방식
- 특정 퀘스트/맵을 코드에 예외로 직접 박아 넣는 방식
- `CheckDb`, `MatrixSolver` 같은 특정 시점의 임시 진단 도구
- 현재 단계의 RatScanner 통합 코드

---

## 2. 가장 가치가 큰 회수품 — 콘텐츠 갱신 파이프라인

### 기존 구현에서 확인한 것

주요 파일:

- `TarkovHelper/Services/TarkovDataDatabaseBuilder.cs`
- `TarkovHelper/Services/TarkovDataDatabaseBuilder.JsonApi.cs`
- `TarkovHelper/Services/TarkovDataDatabaseBuilder.Schema.cs`
- `TarkovHelper/Services/TarkovDataDatabaseBuilder.Writer.cs`
- `TarkovHelper/Services/TarkovDataDatabaseBuilder.Sql.cs`
- `TarkovHelper/Services/ContentStorageService.cs`
- `TarkovHelper/Services/DatabaseUpdateService.cs`
- `TarkovHelper/Services/ContentDatabaseSummary.cs`

후반부 구현은 대체로 다음 흐름을 갖습니다.

1. 온라인 데이터 다운로드
2. 언어 데이터 병합
3. 데이터 정규화/변환
4. 임시 DB 구축
5. DB 및 관계 검증
6. 필요한 아이콘 준비
7. 콘텐츠 매니페스트 작성
8. staging 세트 완성
9. 검증된 staging만 current로 교체
10. 기존 current는 previous로 보존
11. 실패/중단 시 last-known-good 복구

### 준현 헬퍼에 가져올 원칙

`CONFIRMED PRODUCT PRINCIPLE과 직접 일치`

- 활성 DB를 직접 조금씩 수정하지 말고 **새 후보 DB를 처음부터 만들어 검증한 뒤 교체**한다.
- 다운로드/변환 도중 오류가 나면 현재 정상 DB를 건드리지 않는다.
- 비호환 스키마나 비정상 데이터가 감지되면 **fail closed** 한다.
- 이전 정상본을 복구할 수 있게 한다.
- 사용자 진행 데이터는 콘텐츠 교체 대상에서 완전히 분리한다.

### 새 제품에서 개선할 점

기존 구현은 과거 수동 지도 데이터 등을 보존하기 위해 현재 DB를 staging으로 복사하는 흐름이 일부 남아 있습니다.

준현 헬퍼에서는 원칙적으로:

- **API에서 재생성 가능한 콘텐츠**
- **사용자 진행 데이터**
- **수동/정적 지도·보정 데이터**

를 별도 저장 영역으로 분리합니다.

따라서 API 콘텐츠 DB는 가능한 한 **기존 DB를 복사하지 않고 원천 데이터만으로 완전 재생성 가능**해야 합니다.

---

## 3. 콘텐츠 매니페스트 — 회수 가치 높음

`ContentStorageService`의 `ContentUpdateManifest`에는 다음과 같은 정보가 있습니다.

- manifest schema version
- 갱신 시각
- 데이터 출처
- 게임 모드
- DB SHA-256
- 아이템/퀘스트/은신처 건수
- 필요한/누락된 아이콘 수
- 아이콘별 URL/SHA-256/파일 크기
- 경고 목록

### 새 제품에서의 용도

이 개념은 유지할 가치가 큽니다.

준현 헬퍼의 한 콘텐츠 세트가:

- 어디서 만들어졌는지
- 어떤 원천 스냅샷을 사용했는지
- 어떤 내부 스키마 버전인지
- 정상 검증을 거쳤는지
- 실제 파일이 바뀌지 않았는지

를 스스로 설명할 수 있어야 합니다.

기존의 `GameMode = PVP` 고정 등은 제거하고, 새 구조에서는 게임 모드별 원천 버전과 변환기 버전까지 기록하는 방향이 적절합니다.

---

## 4. DB 검증 규칙 — 코드보다 규칙을 회수

기존 `TarkovDataDatabaseBuilder.Sql.cs`와 smoke 테스트에는 다음 검증이 존재합니다.

- SQLite integrity check
- foreign key check
- 비정상적으로 작은 DB 차단
- 아이템/퀘스트/은신처 수 급감 감지
- 중복 ID 감지
- 퀘스트 요구 아이템의 dangling reference 감지
- 은신처 요구 아이템의 dangling reference 감지
- 잘못된 대체 아이템 그룹 감지
- 선행 퀘스트 dangling reference 감지
- 미지원 선행 상태 감지
- 중복 requirement 행 감지
- 탄약 링크/수치/수급처 표현 검증
- malformed SourceJson 감지
- 빌드 메타데이터 존재 검증

### 판단

SQL 문장을 그대로 복사하지는 않습니다.

새 내부 모델/DB 구조가 확정되면 위 검증 의도를 다음 세 층으로 재구성하는 것이 좋습니다.

1. **원본 데이터 스키마 검증**
2. **정규화된 도메인 모델 검증**
3. **최종 DB 참조/무결성 검증**

그리고 새 타입/새 enum/새 objective 종류처럼 기존 변환기가 처음 보는 값은 경고 또는 업데이트 중단 사유로 명시적으로 취급해야 합니다.

---

## 5. 원본 provenance 보존 — 적극 회수

기존 DB는 정규화된 컬럼 외에도 일부 `SourceJson`과 `ContentBuildMetadata`를 보존합니다.

이 원칙은 준현 헬퍼에 매우 유용합니다.

### 이유

API 변환 오류가 발생했을 때:

`외부 원본 → 변환 결과`

를 다시 비교할 수 있습니다.

또한 나중에 변환 규칙을 수정하더라도 어떤 원천 데이터에서 현재 DB가 만들어졌는지 추적할 수 있습니다.

### 새 제품 원칙 후보

- 제품 로직은 정규화된 내부 모델만 사용
- 디버깅/감사/마이그레이션을 위해 raw source 또는 raw snapshot을 보존
- 각 빌드에 source/transport/version/hash 기록

---

## 6. DatabaseSmoke와 실패 fixture — 최우선 회수 대상

기존 저장소에서 가장 가치 있는 자산 중 하나입니다.

주요 파일:

- `TarkovHelper.DatabaseSmoke/Program.cs`
- `TarkovHelper.DatabaseSmoke/FixtureTarkovApiHandler.cs`
- `TarkovHelper.DatabaseSmoke/ItemFulfillmentRegressionSmoke.cs`
- 기타 smoke/regression 파일들

### 회수해야 할 테스트 사고방식

**결정론적 fixture 검사**와 **실제 최신 API 검사**를 분리합니다.

#### 결정론적 fixture

항상 같은 인공 API 데이터를 사용하여 변환 규칙 자체를 검증합니다.

예:

- 한국어/영문 번역
- 중복 objective ID
- prestige 객체/배열 표현
- 선행 퀘스트 관계
- alternative item group
- 잘못된 overlay
- API 중간 실패
- 은신처 requirement
- trader sale/barter/craft

#### live API smoke

현재 실제 API로 전체 DB를 재구축하여:

- 파서가 최신 데이터에서 깨지지 않는지
- 예상하지 못한 타입이 등장했는지
- 데이터 규모가 갑자기 무너지지 않았는지

를 확인합니다.

이 두 종류 검사를 준현 헬퍼에서도 유지하는 것이 좋습니다.

---

## 7. 과거에 실제로 터진 데이터 버그 — 반드시 회귀 테스트로 회수

기존 PR/커밋 이력에서 다음 실패가 확인되었습니다.

### 7.1 `findItem` / `collect`를 제출 필요 아이템으로 잘못 계산

과거에는 단순 획득 목표가 `QuestRequiredItems`에 들어가 실제로 보유해야 할/제출할 수량처럼 계산되었습니다.

새 제품에서는 **퀘스트 목표 종류와 ‘필요 아이템’ 의미를 분리**해야 합니다.

예:

- 획득/발견 목표
- 실제 제출 목표
- 설치/사용 목표
- 판매 목표

는 같은 방식으로 집계하면 안 됩니다.

### 7.2 `sellItem` 목표가 상인 전체 카탈로그를 요구 아이템처럼 노출

`tarkov.dev` 구조상 sell-item objective의 item 목록이 “제출해야 할 고정 아이템 목록”이 아닌 의미를 가질 수 있어 별도 정규화가 필요했습니다.

### 7.3 localization이 objective ID를 번역 문자열로 덮어씀

ID와 번역 키의 충돌로 식별자가 문장으로 변하는 문제가 있었습니다.

새 변환기에서는 **식별자와 표시 문자열의 namespace/처리를 완전히 분리**해야 합니다.

### 7.4 objective ID가 퀘스트 간 중복될 수 있음

로컬 DB가 objective ID를 전역 유일키로 가정하면 깨질 수 있습니다.

새 모델에서는 `(questId, objectiveId)` 같은 parent-scoped identity를 우선 검토합니다.

### 7.5 prestige 데이터 표현 형태 차이

실제 데이터에서 prestige 참조가 객체/배열 등 예상과 다른 형태로 등장해 조건이 손실된 적이 있습니다.

새 변환기는 schema variation을 명시적으로 테스트해야 합니다.

### 7.6 FIR + 일반 요구량 중복 계산

FIR 5 / 전체 15 요구에서 FIR 5개만 보유했는데 완료로 판정되는 버그가 있었습니다.

확인된 올바른 규칙:

- FIR 최소 수량을 충족해야 함
- 전체 수량도 충족해야 함
- FIR 수량을 FIR bucket과 일반 bucket에 동시에 중복 계산하면 안 됨
- FIR 초과분은 unrestricted remainder에 사용할 수 있음

이 테스트는 준현 헬퍼의 필요 아이템 계산에서 거의 그대로 회수할 가치가 있습니다.

### 7.7 콘텐츠 교체 도중 프로세스 중단

`current → previous` 이동 직후 앱이 죽으면 활성 콘텐츠가 사라질 수 있는 crash window가 실제로 발견되었습니다.

새 콘텐츠 업데이트 트랜잭션은 이런 중단 지점을 테스트해야 합니다.

### 7.8 초기화 직전 예약된 저장이 삭제 데이터를 되살림

비동기/디바운스 저장이 초기화 후 실행되면서 삭제한 진행도를 복원하는 경쟁 조건이 있었습니다.

새 사용자 저장 계층에는 reset barrier 또는 generation/version 기반 무효화가 필요합니다.

### 7.9 UI 생명주기 핸들러가 앱 시작 자체를 깨뜨림

미니맵 상태 보존용 전역 UI 이벤트 핸들러가 XAML 초기화 도중 실행되어 프로그램이 시작 직후 종료되는 문제가 있었습니다.

새 제품에서는 장기 실행 서비스의 생명주기를 UI code-behind에 과도하게 묶지 않아야 합니다.

---

## 8. 필요 아이템 계산 — 개념과 일부 순수 로직 회수

주요 참고:

- `ItemsDataService.cs`
- `ItemInventory.cs`
- `ItemFulfillmentRegressionSmoke.cs`

### 좋은 부분

기존에도 개념적으로:

`퀘스트 요구 + 은신처 요구 + 사용자 보유 수량 → 남은 필요 아이템`

을 계산하고 있습니다.

이는 현재 준현 헬퍼의 확정된 핵심 구조와 정확히 일치합니다.

### 새 제품에서 바꿔야 할 점

기존 `ItemsDataService`는 WPF view model, localization, singleton 서비스에 깊게 결합되어 있습니다.

새 제품에서는 순수 계산 계층으로 분리합니다.

예상 책임:

`NeededItemCalculator(questRequirements, hideoutRequirements, profileProgress, inventory) -> NeededItem[]`

이 계산기는 UI, 파일, DB, 네트워크를 몰라야 합니다.

### 식별자

기존 사용자 인벤토리는 normalized item name을 lookup key로 쓰는 흔적이 있습니다.

새 제품은 가능하면 **안정적인 BSG/API ID를 영구 식별자로 사용**해야 합니다.

이름은 표시/검색용이지 관계/저장의 primary identity가 되어서는 안 됩니다.

---

## 9. 은신처 데이터 모델 — 관계 구조 참고 가치 있음

`HideoutDbService.cs`에서 확인한 관계:

- Station
- Station Level
- Item Requirement
- Station Level Requirement
- Trader Requirement
- Skill Requirement

이 관계 분리는 새 내부 Hideout 모델을 설계할 때 좋은 체크리스트입니다.

다만 기존 `HideoutProgressService`의 세부 동작, 예를 들어 레벨 변경 시 아이템 차감/환불 같은 UX는 새 제품에서 아직 합의되지 않았으므로 승계하지 않습니다.

---

## 10. 퀘스트 — 데이터 구조 체크리스트만 회수, 엔진은 폐기

### 참고 가치가 있는 필드/조건

기존 `TarkovTask`와 DB 구조에서 다음이 확인됩니다.

- ID / 이름 / 설명 / trader / map
- 최소 레벨
- faction
- edition include/exclude
- prestige
- game mode
- task prerequisite
- prerequisite status
- prerequisite AND/OR group
- trader standing/loyalty requirement
- objective
- required item / FIR / alternative group
- wiki link
- 기타 특수 조건

이것은 **새 변환기가 놓치지 말아야 할 데이터 종류 체크리스트**로 쓸 수 있습니다.

### 현재 엔진을 버려야 하는 이유

기존 `QuestProgressService`는 여러 시대의 요구와 예외가 누적된 상태입니다.

감사 과정에서 특히 중요한 문제:

- DB에는 `QuestTraderRequirements`를 로드하지만 실제 현재 퀘스트 해금 판정 경로에서 trader requirement를 일관되게 적용하지 않는 구조가 확인됨
- 특정 퀘스트/분기/특수 상태를 별도 코드 예외로 보정한 이력이 많음
- 프로필/수주/로그 호환 로직이 뒤섞여 있음

### 새 제품 방향

**generic condition evaluator**를 별도 도메인 계층으로 설계합니다.

외부 데이터를 내부의 명시적인 조건 객체로 변환한 뒤, 조건 엔진이 동일한 규칙으로 판정하도록 합니다.

특정 퀘스트 이름을 코드에 박아 넣는 예외는 최후의 수단으로만 허용합니다.

---

## 11. 탄약 — 수급처 파생 방식은 회수 가치 큼

기존 데이터 빌더는 탄약/아이템 데이터에서 다음 수급처를 결합합니다.

- 상인 직접 판매
- 상인 교환(barter)
- 은신처/Workbench 제작

과거 수정 이력에서는:

- 여러 상인 경로와 제작 경로를 함께 보존
- 최소 상인/시설 레벨 보존
- flea market을 영구 수급처에서 제외
- 퀘스트 보상을 일반적인 상시 수급처로 취급하지 않음

같은 규칙을 검증했습니다.

이는 사용자가 원하는 **탄약의 수급처 정보**와 직접 연결됩니다.

다만 정확히 어떤 경로를 UI에서 “수급처”로 취급할지는 탄약 기능 세부 설계 때 사용자 의도와 다시 맞춥니다.

### 폐기할 것

`AmmoItem.cs`의 방탄 등급별 0~6 효율 계산은 penetration과 armorDamage를 이용한 자체 휴리스틱입니다.

이를 게임의 공식 사실처럼 새 제품에 가져오지 않습니다.

필요하다면 최신 검증된 공식/커뮤니티 산식을 별도로 조사하거나, 명시적으로 ‘도움용 추정치’로 설계합니다.

---

## 12. 아이콘 동기화 — 높은 가치

`ItemIconUpdateService.cs`에서 회수할 원칙:

- 실제 제품에서 필요한 아이템만 다운로드
- 제한된 동시 다운로드
- retry
- 최대 응답 크기 제한
- 이미지 decode 검증
- PNG 정규화
- 임시 파일에 쓴 뒤 정상 검증 후 교체
- 새 다운로드 실패 시 기존 정상 아이콘 유지
- URL이 동일하고 기존 파일이 정상이라면 재사용
- 아이콘별 hash/size 매니페스트
- 더 이상 필요 없는 파일 정리

이 기능은 데이터 DB와 동일한 **버전드 콘텐츠 세트** 안에서 staging/activation하는 구조가 가장 적절합니다.

---

## 13. 사용자 진행 데이터 — 분리 원칙만 회수

기존 후반부는 다운로드 콘텐츠와 `user_data.db`를 분리했습니다.

이 원칙은 준현 헬퍼에 그대로 적용할 가치가 큽니다.

### 가져오지 않을 것

현재 `UserDataDbService` 자체는:

- 과거 PVE/PVP 호환 흔적
- 레거시 컬럼/마이그레이션
- 오래된 저장 규칙
- 이름 기반 식별자

가 누적되어 있으므로 새 제품에서 처음부터 깨끗한 스키마로 다시 만듭니다.

---

## 14. `PersistenceWriteQueue` — 직접 재사용 가능성이 있는 소형 부품

`PersistenceWriteQueue.cs`는 비교적 작고 독립적입니다.

기능:

- fire-and-forget 저장을 순서대로 직렬화
- generation을 이용해 오래된 예약 저장 무효화
- reset 중 새 저장 차단
- reset 전에 이미 실행 중인 저장 완료 대기
- 중첩 reset barrier 지원

새 제품이 유사한 비동기/디바운스 저장을 사용한다면 코드 또는 설계를 직접 회수할 후보입니다.

실제 재사용 여부는 사용자 저장 구조가 정해질 때 결정합니다.

---

## 15. 지도/미니맵 — 순수 부품은 보존, 데이터는 재검증

### 회수 가능성이 있는 부품

#### `ScreenshotCoordinateParser.cs`

EFT 스크린샷 파일명에서:

- X/Y/Z
- quaternion
- yaw

를 파싱합니다.

현재 Tarkov에서도 동일한 파일명 규칙이 유지되는지 확인 후 재사용 가능성이 있습니다.

#### `ScreenshotWatcherService.cs`

- FileSystemWatcher
- debounce
- 파일 쓰기 완료 대기
- 중복 이벤트 억제
- watcher 오류 후 복구

등 범용적인 구조는 재사용 가치가 있습니다.

#### `MapCoordinateTransformer.cs`

- affine transform
- Leaflet-style coordinate transform
- alias resolution
- API 좌표 → 지도 좌표

수학/구조 참고 가치가 있습니다.

#### `MapCalibrationService.cs`

- calibration point 기반 affine 회귀
- partial pivoting Gaussian elimination
- IDW residual correction

이라는 아이디어가 있습니다.

새 제품에서는 수치 안정성이 검증된 라이브러리를 쓸 수 있다면 직접 구현보다 그쪽을 우선합니다.

#### `FloorDetectionService.cs`

- Y 범위
- 선택적 X/Z 영역
- priority

를 이용해 층을 판정하는 모델은 다층 미니맵에 유용할 수 있습니다.

### 보존만 할 데이터

`Assets/DB/Data/map_configs.json`에는:

- map aliases
- SVG dimensions
- player marker affine transform
- SVG bounds
- floor 목록

이 들어 있습니다.

이 값들은 유용할 가능성이 높지만 **현재는 신뢰 데이터로 채택하지 않습니다.**

필요한 검증:

- 현재 Tarkov에서 정확한가
- 어떤 출처/측정으로 생성됐는가
- 포함된 SVG/좌표 데이터의 라이선스가 명확한가
- 새 지도 공급원과 충돌하지 않는가

### 지도 UI 구조는 폐기

현재 `MapPage.xaml.cs`에는 tracking, objective, extract, marker, custom marker, calibration, floor, overlay, UI 상태가 과도하게 결합되어 있습니다.

과거에는 탭 이동·전역 이벤트·overlay 생명주기 보정이 연속적으로 추가되었고, 전역 UI 핸들러가 앱 시작 crash를 만든 이력도 있습니다.

새 지도는 기능이 확정되면 최소한 다음 책임을 분리합니다.

- map content/data
- coordinate transform
- player position tracker
- floor detector
- marker providers
- overlay window lifecycle
- presentation/view state

---

## 16. 게임 로그 감시 — 가능성만 보존

`LogSyncService.cs`에는 게임 로그에서:

- 퀘스트 이벤트
- 맵/transit 정보
- 세션/모드 정보

를 읽으려는 구현이 있습니다.

그러나 맵 별칭/패턴이 하드코딩되어 있고 현재 게임 로그 형식이 유지되는지 보장할 수 없습니다.

따라서 지금은 제품 핵심에 포함하지 않습니다.

나중에 **사용자 수동 입력을 줄이는 보조 자동화**가 필요할 때, 현재 Tarkov 로그를 다시 실측해 유효하면 새로 설계합니다.

---

## 17. 기존 데이터 보정 overlay — 교훈은 중요, 의존은 미확정

최근 기존 프로젝트는 raw `json.tarkov.dev regular/tasks`만으로 실제 퀘스트 목록을 맞추는 데 문제가 있어 `tarkovtracker-org/tarkov-data-overlay`를 추가 보정원으로 사용했습니다.

당시 발견된 문제에는:

- 누락된 퀘스트
- 잘못된 번역
- prestige 조건 손실
- 종료 퀘스트 노출

등이 있었습니다.

### 준현 헬퍼에 주는 교훈

**“API 하나가 있다고 해서 그것이 항상 완전한 진실이라고 가정하지 않는다.”**

그러나 overlay를 지금 새 제품의 공식 2차 원천으로 확정하지는 않습니다.

구현 직전에 최신 `json.tarkov.dev`와 overlay의 현재 관계를 검증하고 필요할 경우:

- base source
- correction source
- correction version
- 적용 결과

를 명시적인 provenance로 관리합니다.

---

## 18. 빌드/검증 운영 — 사고방식 회수

기존 `.github/workflows/build.yml`은 다음을 분리했습니다.

- 일반 빌드
- deterministic database smoke
- live API database smoke
- 검증된 live DB를 사용한 release candidate
- 아이콘 다운로드/변환 검사

후기 PR에서는 추가로:

- 실제 publish된 EXE startup smoke
- 공개 release ZIP 재다운로드
- SHA-256 비교
- 사용자 데이터 미포함 검사
- DB integrity 검사

까지 수행했습니다.

기술 스택이 아직 정해지지 않았으므로 workflow 파일을 복사하지는 않지만, **검증 단계 분리 원칙**은 새 프로젝트 개발 절차에 반영할 가치가 큽니다.

---

## 19. 기존 저장소에서 폐기할 대표적인 것

### 제품/도메인

- 기존 프로그램의 화면 배치와 정보 우선순위
- 기존 퀘스트 상태 엔진 전체
- PVP 고정 profile facade
- 레거시 PVE/PVP 호환 코드
- 특정 퀘스트 이름 기반 예외 처리
- 하드코딩된 게임 수치

### 기술 구조

- 거대한 singleton 중심 서비스망
- UI code-behind가 서비스 생명주기를 직접 관리하는 구조
- 동적 컬럼 존재 검사로 오래된 DB를 계속 끌고 가는 방식
- 새 제품에서도 과거 DB 호환을 기본 요구로 두는 방식

### 데이터/자산

- 출처가 확인되지 않은 지도 변환값을 그대로 공식 데이터로 사용
- 출처/라이선스가 확인되지 않은 지도 SVG를 곧바로 복사
- 오래된 하드코딩 맵 aliases를 최신 사실로 간주

### 임시 도구

- `CheckDb`
- `MatrixSolver`

특정 과거 문제를 해결하기 위해 만든 일회성 성격이 강하며, 새 테스트/진단 구조를 만드는 편이 낫습니다.

---

## 20. 새 준현 헬퍼에서 우선 회수할 작업 순서

실제 구현 단계가 시작되면 다음 순서를 권장합니다.

### 1차 — 데이터 기반 핵심

1. legacy deterministic/live API fixture와 실패 사례를 새 테스트 사양으로 옮김
2. 새 Source Client / Importer / Validator 설계
3. 완전 재생성 가능한 candidate DB 구축
4. staging / validation / atomic activation / rollback 구현
5. content manifest/provenance 구현

### 2차 — 핵심 기능용 도메인

1. Quest 조건 모델/판정 엔진을 새로 설계
2. Hideout 관계 모델 설계
3. Needed Item 순수 계산기 설계
4. FIR/비FIR 회귀 규칙 이식
5. Ammo acquisition source 파생 로직 재설계

### 3차 — 콘텐츠 부속 자산

1. 아이콘 동기화 구조 회수
2. 지도 공급원이 확정되면 기존 map transforms/aliases를 검증 자료로 대조
3. 스크린샷/로그 자동화가 여전히 유효한지 최신 게임에서 재검증

---

## 21. 최종 판단

기존 Tarkov-Helper를 통째로 고치는 것보다 새로 만드는 결정은 타당합니다.

하지만 기존 저장소가 완전히 무가치한 것은 아닙니다.

가장 좋은 회수품은:

1. **안전한 온라인 콘텐츠 업데이트 구조**
2. **데이터 검증 규칙**
3. **실제 실패에서 나온 회귀 테스트**
4. **필요 아이템/FIR 계산의 검증된 일부 도메인 규칙**
5. **아이콘·콘텐츠 세트 동기화 구조**
6. **지도 위치 추적의 일부 순수 유틸리티**

입니다.

반대로 새 제품의 제품 로직, 사용자 프로필, 퀘스트 판정, UI, 지도 화면 구조는 기존 구현을 승계하기보다 **준현 헬퍼의 확정된 요구사항을 기준으로 새로 설계**해야 합니다.

이 문서는 향후 구현 시 “기존 저장소에서 무엇을 다시 조사할 가치가 있는가”를 찾는 인덱스로 사용합니다.
