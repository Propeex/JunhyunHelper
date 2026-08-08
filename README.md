# JunhyunHelper — Tarkov Helper

이 저장소는 새로운 Tarkov Helper를 처음부터 제품 설계하고 개발하기 위한 공식 저장소입니다.

## 현재 단계

**Phase 0 — 프로젝트 기억/개발 운영 구조 구축**

아직 제품 코드 구현을 시작하지 않습니다. 먼저 사용자의 의도와 제품 요구사항을 대화로 정의하고, 확정된 내용을 저장소에 지속적으로 기록합니다.

## 가장 중요한 원칙

1. **사용자의 의도가 제품의 최상위 기준입니다.**
2. 새 Tarkov Helper는 **처음부터 설계**합니다.
3. 기존 `Propeex/Tarkov-Helper` 및 기타 구현은 필요할 때 참고할 수 있지만 **정답, 사양, 사실의 근거로 간주하지 않습니다.**
4. 대화가 바뀌어도 개발자는 저장소 문서를 읽어 현재 맥락을 복구해야 합니다.
5. 확정된 결정과 미확정 가설을 섞지 않습니다.
6. 구현보다 제품 동작과 요구사항을 먼저 확정합니다.

## 새 개발자가 가장 먼저 읽을 문서

1. [`AGENTS.md`](AGENTS.md) — 개발자/AI 작업 규약과 문서 읽기 순서
2. [`docs/STATE.md`](docs/STATE.md) — 지금 어디까지 왔는지, 바로 다음에 무엇을 해야 하는지
3. [`docs/PRODUCT.md`](docs/PRODUCT.md) — 제품 목적, 사용자 요구사항, 기능 정의
4. [`docs/DECISIONS.md`](docs/DECISIONS.md) — 확정된 제품/기술 결정의 이력
5. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 기술 구조와 구성요소 관계
6. [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md) — 개발/검증/인수인계 절차
7. [`docs/REFERENCE_POLICY.md`](docs/REFERENCE_POLICY.md) — 기존 Tarkov-Helper 등 외부 구현 참고 규칙

## 문서의 역할

- `STATE.md`는 **현재 상태와 다음 행동**을 빠르게 복구하는 문서입니다.
- `PRODUCT.md`는 **무엇을 왜 만드는지**를 정의합니다.
- `DECISIONS.md`는 **무엇을 확정했고 왜 그랬는지**를 보존합니다.
- `ARCHITECTURE.md`는 제품 요구가 정해진 뒤 **어떻게 구현할지**를 정의합니다.
- 코드와 테스트는 이 문서들의 결과물이지, 제품 의도를 대신하지 않습니다.

## 현재 다음 단계

사용자와의 제품 설계 대화를 시작하여 Tarkov Helper가 해결해야 할 문제와 실제 사용 흐름을 처음부터 정의합니다.
