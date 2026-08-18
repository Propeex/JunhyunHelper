# CONTENT STORAGE — Game Content 저장 설계

상태: `CONFIRMED — 현재 구현과 동기화`

## 1. 목적

준현 헬퍼의 Game Content는 온라인 데이터에서 다시 만들 수 있습니다.

따라서 일반 업무 시스템처럼 수년간 부분 migration을 누적하는 관계형 DB를 운영할 필요가 없습니다.

기본 원칙:

> 외부 데이터는 Importer에서 canonical model로 완전히 변환·검증한 뒤, 그 검증된 canonical snapshot 자체를 하나의 버전 단위로 저장한다.

## 2. 왜 수십 개 SQLite 테이블로 나누지 않는가

현재 콘텐츠 규모는 데스크톱 앱 시작 시 메모리에 올려 계산/필터하기에 충분히 작습니다.

퀘스트, 아이템, 은신처 관계를 다시 SQLite 스키마로 세밀하게 복제하면 다음 두 스키마를 동시에 유지해야 합니다.

1. C# canonical model
2. SQLite relational schema

Game Content는 어차피 새 API 데이터로 통째로 재생성할 수 있으므로 이 중복은 유지보수 비용만 늘립니다.

canonical model이 데이터 의미의 유일한 내부 계약입니다.

## 3. 모드별 content.db

게임 모드별 원천 데이터는 서로 다를 수 있으므로 한 개의 active DB를 공유하지 않습니다.

```text
content/
  regular/
    content.db
    content.candidate.db
    content.previous.db
  pve/
    content.db
    content.candidate.db
    content.previous.db
  pvp-season/
    content.db
    content.candidate.db
    content.previous.db
```

각 `content.db`는 SQLite 파일이지만 내부에는 해당 게임 모드의 검증된 콘텐츠 스냅샷 하나를 저장합니다.

현재 메타데이터:

- schema version
- game mode
- build timestamp
- canonical payload JSON
- warning 목록

SQLite를 사용하는 이유:

- 단일 파일
- 손쉬운 무결성 검사
- 안정적인 읽기/쓰기
- 이후 필요한 메타데이터 확장이 쉬움

제품 기능은 SQLite JSON을 직접 질의하지 않습니다.

선택된 프로필의 게임 모드에 맞는 snapshot을 읽어 canonical `GameContentCatalog`로 복원하고 Core 계산이 그 객체를 사용합니다.

## 4. 내부 schema version

Game Content 모델이 호환 불가능하게 바뀌면 `ContentSnapshotStore.CurrentSchemaVersion`을 올립니다.

현재 최신 schema는 **v7**입니다.

- v3: Ammo Wiki Ballistics membership/effectiveness 의미 보존
- v4: Quest Map geometry (`possibleLocations` / `zones`) 보존
- v5: opaque availability condition / delay metadata와 당시 special-trader compatibility
- v6: recoverable special-trader access를 ordinary prerequisite와 분리하고 BTR/Ref source prerequisite 상태 보존
- v7: `globalVariable` requirement를 `variableId / operator / required value` 구조로 보존

현재 읽기 가능 last-known-good 범위는 **v3~v7**입니다.

일반적으로 Game Content는 온라인에서 다시 만들 수 있으므로 최신 schema로 재빌드하는 것을 우선합니다.

### v3~v5 special-trader read normalization

과거 snapshot에 저장된 잘못된 special-trader compatibility overlay는 앱 자체에서 결정론적으로 정규화할 수 있으므로 읽는 시점에 메모리에서 다음 변환을 적용합니다.

- 과거 BTR Driver의 강제 `A Helping Hand = Complete` gate 제거 후 `Active` compatibility gate 적용
- 과거 Lightkeeper의 `Getting Acquainted = Complete` ordinary prerequisite를 recoverable special-trader access gate로 전환
- Ref의 검증된 Complete 의미 유지

이 in-memory 정규화는 active `content.db` 파일을 몰래 재작성하지 않습니다. 다음 정상 `데이터 업데이트` 성공 시 v7 snapshot이 새로 저장됩니다.

### v3~v6 profile-variable 의미

v7 이전 snapshot에는 `globalVariable`의 정확한 structured read-side 조건이 없을 수 있습니다. 그런 snapshot은 last-known-good로 계속 읽을 수 있지만, exact `ProfileVariables` 판정을 완전히 사용하려면 정상 `데이터 업데이트`로 v7 snapshot을 재구축하는 것이 기준입니다.

프로그램은 오래된 snapshot의 opaque 정보를 보고 variable ID/value를 추측하지 않습니다.

### user.db와 분리

사용자 데이터인 `user.db`에는 Game Content 재생성 원칙을 자동 적용하지 않습니다. 사용자 진행은 재생성 불가능하므로 별도 migration 정책을 가집니다.

현재 `user.db` SQLite table schema는 **v1**입니다.

- `SpecialTraderAccessOverrides`
- `ProfileVariables`
- 기타 호환 가능한 진행 fact

같은 확장은 optional JSON property로 저장할 수 있으며 기존 v1 DB와 호환됩니다.

## 5. 안전한 활성화

각 게임 모드 디렉터리에는 다음 세 파일만 둡니다.

- `content.db` — 현재 active
- `content.candidate.db` — 새로 빌드한 후보
- `content.previous.db` — 직전 정상본

업데이트:

```text
해당 game mode API download
  → external shape / required semantics validation
  → canonical import
  → semantic/reference validation
  → final canonical validation
  → 해당 mode의 content.candidate.db 작성
  → SQLite integrity + deserialize + canonical read-back validation
  → 같은 mode의 active와 파일 교체
  → 기존 active는 previous로 보존
```

candidate가 검증에 실패하면 해당 모드의 active는 건드리지 않습니다.

PvP 콘텐츠 업데이트가 PvE/시즌 콘텐츠 파일을 수정해서는 안 되며 반대도 동일합니다.

candidate 내부의 `GameMode`가 저장 경로의 기대 모드와 다르면 활성화를 거부합니다.

가능한 경우 `File.Replace`를 사용해 같은 볼륨 안에서 active/candidate 교체를 단순한 파일 연산으로 처리합니다.

v0.1.13 final validator는 특히 다음 malformed requirement를 active 적용 전에 fatal로 차단합니다.

- Quest item requirement accepted-item 후보가 비어 있음
- Quest item requirement `Count <= 0`
- Hideout item requirement `Count <= 0`

## 6. 시작 시 복구

선택된 게임 모드의 active를 읽고 SQLite/canonical 검증에 실패했으며 같은 모드의 previous가 정상이라면 previous를 active로 복구합니다.

candidate는 프로그램 시작 시 자동으로 active가 되지 않습니다.

즉 중단된 업데이트의 미완성 후보가 다음 실행에서 우연히 사용되는 일이 없어야 합니다.

## 7. 사용자 진행과 분리

Game Content 파일을 삭제하거나 교체해도 `user.db`는 영향을 받지 않습니다.

사용자 진행은 stable game IDs로 Game Content를 참조합니다.

패치로 특정 Quest/Item ID가 실제로 사라진 경우는 데이터 업데이트 오류가 아니라 콘텐츠 변화로 보고, 해당 사용자 참조를 어떻게 표시/보존할지는 User Progress 정책에 따릅니다.

## 8. 재검토 조건

다음과 같은 실제 문제가 생기기 전에는 content.db를 대규모 관계형 schema로 확장하지 않습니다.

- 전체 snapshot 로딩이 측정 가능한 성능 문제를 만듦
- 메모리 사용량이 현실적으로 문제가 됨
- 일부 콘텐츠만 독립 업데이트해야 하는 확정 요구가 생김
- SQL 질의가 없으면 구현하기 어려운 제품 요구가 생김

그 전에는 현재의 단순한 snapshot 구조를 유지합니다.
