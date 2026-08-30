# DOCUMENTATION POLICY — 저장소 기반 프로젝트 기억 규약

Status: **ACTIVE**  
Date: **2026-08-30 KST**

## 1. 목적

준현 헬퍼 개발의 프로젝트 기억은 ChatGPT 대화 기록이나 특정 세션의 내부 기억이 아니라 **GitHub 저장소의 공식 문서, 코드, 테스트, PR/CI/release 상태**에 둔다.

새 대화가 시작되거나 개발자가 교체되어도 저장소만 읽고 현재 제품 의미, 진행 상태, 구현 위치, 검증 상태와 다음 행동을 복구할 수 있어야 한다.

## 2. 시작 메시지 원칙

사용자는 새 대화마다 과거 브랜치/SHA/PR/CI/진행상황을 장문으로 다시 전달할 필요가 없다.

`준현 헬퍼 작업 이어가자`, `이전 작업 계속해`처럼 프로젝트를 이어가려는 의도가 명확하면 개발자는 먼저 현재 GitHub 상태와 아래 repository memory를 복구한다.

사용자에게 기술적 인수인계 메시지를 다시 작성하거나 붙여 넣도록 요구하지 않는다.

제품 의도가 새로 바뀌거나 이전 공식 결정과 충돌하는 경우에만 사용자에게 제품 관점의 확인을 요청한다.

## 3. 권위 분리

### 기계 판독 가능한 현재 사실값

`docs/PROJECT_STATE.json`

다음처럼 여러 문서에 중복되기 쉬운 현재 사실값의 canonical source다.

- Desktop/current public stable version
- exact public product release source
- release/asset identity
- 현재 schema version
- Map donor revision
- 현재 유지보수 test count와 user real-PC validation 상태

제품 요구사항 자체를 정의하지는 않는다.

### 진행 중 작업 체크포인트

`docs/ACTIVE_WORK.md`

- `NONE`: 진행 중인 개발 작업 없음
- `ACTIVE`: 중단 시 복구해야 할 작업 존재

ACTIVE 상태에서는 최소한 Goal, Base, Confirmed scope, Completed, Current step, Remaining을 기록한다.

중요한 단계가 바뀔 때 갱신한다. 대화 종료 직전 한 번에 몰아서 기록하지 않는다.

### 사람용 현재 상태

- `docs/CURRENT_STATE.md`: 짧은 current-state index
- `docs/STATE.md`: 상세 운영/검증 상태

### 제품 의미와 장기 결정

- `docs/PRODUCT.md`
- `docs/DECISIONS.md`
- 관련 `DECISION_*.md`

### 구현 구조와 변경 영향

- `docs/ARCHITECTURE.md`
- `docs/DEVELOPER_REFERENCE.md`
- subsystem 전문 문서

이 문서들은 구조 설명이 주 목적이다. 헤더에 과거 릴리즈 번호가 남아 있더라도 **현재 release authority로 사용하지 않는다.** 현재 release/status는 반드시 `PROJECT_STATE.json`, `CURRENT_STATE.md`, `STATE.md`에서 복구한다.

장기적으로 evergreen architecture/reference 문서에는 current release 번호를 반복하지 않는 방향을 선호한다.

## 4. 새 세션 복구

기본 복구 순서:

1. `AGENTS.md`
2. `docs/PROJECT_STATE.json`
3. `docs/ACTIVE_WORK.md`
4. `README.md`
5. `docs/CURRENT_STATE.md`
6. `docs/STATE.md`
7. 작업이 ACTIVE이면 해당 PR/branch/관련 결정·코드·테스트
8. 작업 영역에 필요한 `PRODUCT` / `DECISIONS` / architecture/reference / subsystem 문서
9. current GitHub PR/CI/release state

저장소 전체를 매번 처음부터 재분석하지 않는다. 공식 문서가 충분하면 관련 코드만 확인해 incremental context recovery를 한다.

## 5. 체크포인트 규칙

의미 있는 작업은 다음 경계에서 `ACTIVE_WORK.md`를 필요에 따라 갱신한다.

- 사용자 의도/제품 동작 확정
- 설계 결정 확정
- 중요한 root cause 확인
- 구현이 의미 있는 단위로 완료
- PR 생성
- CI 결과 확정
- main 병합
- release/tag/asset 검증

사용자가 대화 길이 한계를 관리할 필요가 없도록 한다.

개발자는 대화가 실제로 곧 종료될 시간을 신뢰성 있게 예측할 수 있다고 가정하지 않는다. 대신 언제 종료되어도 repository checkpoint로 복구 가능한 상태를 유지한다.

## 6. Documentation Consistency Gate

`.github/scripts/Test-DocumentationConsistency.ps1`과 `.github/workflows/documentation-consistency.yml`이 최소한 다음을 자동 검사한다.

- 필수 project-memory 문서 존재
- Desktop project version ↔ `PROJECT_STATE.json` 일치
- public stable version/tag/source가 README/CURRENT_STATE/STATE와 일치
- FIRST_RUN 버전 일치
- `AGENTS.md`가 project-state/active-work 복구 규칙을 참조
- `ACTIVE_WORK.md` 상태 형식 유효성
- evergreen architecture/reference 문서에 current-state처럼 보이는 오래된 release marker가 있으면 warning 출력

현재 제품 의미를 바꾸는 검사가 아니라 **문서 드리프트와 인수인계 누락을 조기에 드러내는 개발 게이트**다.

## 7. 작업 종료 조건

작업은 코드가 병합되었다는 이유만으로 끝나지 않는다.

- 관련 자동/실행 검증 완료
- 필요한 결정/상태 문서 갱신
- `ACTIVE_WORK.md` Remaining 비움
- 작업이 완전히 종료되면 `Status: **NONE**`
- 다음 새 대화가 별도 시작 메시지 없이 저장소에서 복구 가능

이어야 한다.
