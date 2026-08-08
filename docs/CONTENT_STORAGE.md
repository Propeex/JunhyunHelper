# CONTENT STORAGE — Game Content 저장 설계

상태: `CONFIRMED — 초기 구현`

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

초기 준현 헬퍼에서는 canonical model이 데이터 의미의 유일한 내부 계약입니다.

## 3. content.db

`content.db`는 SQLite 파일이지만 내부에는 한 번의 검증된 콘텐츠 스냅샷을 저장합니다.

현재 최소 메타데이터:

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

시작 시 snapshot을 읽어 canonical `GameContentCatalog`로 복원하고 Core 계산이 그 객체를 사용합니다.

## 4. 내부 schema version

Game Content 모델이 호환 불가능하게 바뀌면 `ContentSnapshotStore.CurrentSchemaVersion`을 올립니다.

구형 content.db를 복잡하게 migration하는 것을 기본으로 하지 않습니다.

Game Content는 온라인에서 재생성 가능하므로 새 스키마로 다시 다운로드/빌드합니다.

사용자 데이터인 `user.db`에는 이 원칙을 자동 적용하지 않습니다. 사용자 진행은 재생성 불가능하므로 별도 migration 정책을 가집니다.

## 5. 안전한 활성화

파일:

- `content.db` — 현재 active
- `content.candidate.db` — 새로 빌드한 후보
- `content.previous.db` — 직전 정상본

업데이트:

```text
API download
  → canonical import
  → semantic/reference validation
  → content.candidate.db 작성
  → SQLite integrity + deserialize + canonical validation
  → active와 파일 교체
  → 기존 active는 previous로 보존
```

candidate가 검증에 실패하면 active는 건드리지 않습니다.

가능한 경우 `File.Replace`를 사용해 같은 볼륨 안에서 active/candidate 교체를 단순한 파일 연산으로 처리합니다.

## 6. 시작 시 복구

active를 읽고 SQLite/canonical 검증에 실패했으며 previous가 정상이라면 previous를 active로 복구합니다.

candidate는 프로그램 시작 시 자동으로 active가 되지 않습니다.

즉 중단된 업데이트의 미완성 후보가 다음 실행에서 우연히 사용되는 일이 없어야 합니다.

## 7. 사용자 진행과 분리

`content.db`를 삭제하거나 교체해도 `user.db`는 영향을 받지 않습니다.

사용자 진행은 stable game IDs로 Game Content를 참조합니다.

패치로 특정 Quest/Item ID가 실제로 사라진 경우는 데이터 업데이트 오류가 아니라 콘텐츠 변화로 보고, 해당 사용자 참조를 어떻게 표시/보존할지는 User Progress 상세 설계에서 다룹니다.

## 8. 재검토 조건

다음과 같은 실제 문제가 생기기 전에는 content.db를 대규모 관계형 schema로 확장하지 않습니다.

- 전체 snapshot 로딩이 측정 가능한 성능 문제를 만듦
- 메모리 사용량이 현실적으로 문제가 됨
- 일부 콘텐츠만 독립 업데이트해야 하는 확정 요구가 생김
- SQL 질의가 없으면 구현하기 어려운 제품 요구가 생김

그 전에는 현재의 단순한 snapshot 구조를 유지합니다.
