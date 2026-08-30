# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-30 KST**

이 문서는 대화 기억과 무관하게 **현재 진행 중인 작업의 정확한 중단 지점**을 복구하기 위한 체크포인트다.

작업이 없을 때는 `Status: **NONE**`으로 유지한다. 의미 있는 작업을 시작하면 즉시 `ACTIVE`로 바꾸고, 요구사항/설계/구현/검증 단계가 바뀔 때마다 필요한 부분만 갱신한다. 작업이 완전히 병합·검증·문서화되면 다시 `NONE`으로 닫는다.

## Goal

ChatGPT 대화나 장문의 시작 메시지에 프로젝트 기억을 의존하지 않도록 저장소 기반 개발 복구 체계를 강화한다.

## Base

```text
base main: e10140d2f2756eb3ab51d4c3e140556ce0c0a927
branch: maintenance/project-memory-consistency-2026-08-30
PR: pending
```

## Confirmed scope

- `AGENTS.md`를 새 세션 복구의 최상위 규칙으로 유지하되, `PROJECT_STATE`와 `ACTIVE_WORK`를 우선 읽도록 보강한다.
- 사용자가 새 대화마다 장문의 인수인계/시작 메시지를 작성할 필요가 없도록 명시한다.
- 기계 판독 가능한 `docs/PROJECT_STATE.json`을 현재 안정 버전/제품 source/schema/donor pin 등 중복되기 쉬운 사실값의 canonical source로 둔다.
- `docs/ACTIVE_WORK.md`를 진행 중 작업 전용 체크포인트로 둔다.
- 별도 Documentation Consistency GitHub Actions gate를 추가한다.
- 기존 `ARCHITECTURE.md` / `DEVELOPER_REFERENCE.md`의 오래된 릴리즈 헤더는 현재 상태 authority가 아님을 명확히 하고 자동 검사에서 drift warning으로 노출한다. 해당 대형 문서의 내용 자체는 이번 프로세스 작업에서 축약·재작성하지 않는다.

## Completed

- current main / public stable 상태 재확인
- 기존 `AGENTS.md`, `CURRENT_STATE.md`, `STATE.md`, `ARCHITECTURE.md`, `DEVELOPER_REFERENCE.md`, CI 구조 확인
- `docs/PROJECT_STATE.json` 추가
- `docs/ACTIVE_WORK.md` 추가

## Current step

개발 규칙/문서 정책/CI consistency gate 구현 중.

## Remaining

1. `AGENTS.md` 복구/체크포인트 규칙 보강
2. Documentation policy 문서 추가
3. consistency 검사 스크립트 + workflow 추가
4. PR 생성 및 전체 검증
5. 병합 후 exact-main consistency/기존 CI 확인
6. `ACTIVE_WORK`를 `NONE`으로 닫고 공식 상태 문서에 개발 프로세스 변경 기록

## User decisions required

없음. 이번 변경은 제품 기능/UX가 아니라 개발 기억·복구·문서 일관성 체계 개선이다.
