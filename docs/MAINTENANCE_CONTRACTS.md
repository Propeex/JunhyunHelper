# MAINTENANCE CONTRACTS — 유지보수 안전 계약

상태: `CONFIRMED — MAINTENANCE POLICY`

이 문서는 기능 완성 이후의 준현 헬퍼를 수정할 때 지켜야 하는 **개발·검증 계약**을 정의합니다.
제품 요구사항을 새로 만드는 문서가 아니며, 사용자 확정 요구사항·`DECISIONS.md`·`PRODUCT.md`를 덮어쓰지 않습니다.

목표는 단순합니다.

- 이미 검증된 제품 동작을 유지한다.
- Tarkov/외부 데이터 변화는 조기에 탐지한다.
- 실패한 외부 데이터가 기존 정상 데이터를 오염시키지 못하게 한다.
- 성능 개선을 명목으로 정확도·안전 계약을 약화시키지 않는다.
- 다음 개발자가 오래된 문서나 과거 프로토타입을 현재 사양으로 오해하지 않게 한다.

---

## 1. 문서와 구현의 권위

제품 의미가 충돌할 때의 우선순위는 `AGENTS.md`의 **진실의 우선순위**를 단일 기준으로 사용합니다.

`docs/STATE.md`는 현재 릴리즈·검증 상태와 다음 작업을 복구하는 운영 인덱스입니다. 제품 요구사항 자체를 새로 정의하지 않습니다.

`docs/DEVELOPER_REFERENCE.md`는 구현 위치/관계의 지도입니다. `docs/ARCHITECTURE.md`는 현재 기술 설계를 설명합니다. 둘 중 오래된 문구가 현재 확정 요구사항·테스트·코드와 충돌하면 문구를 현재 상태에 맞게 고치며, 오래된 문구를 근거로 검증된 런타임을 되돌리지 않습니다.

기존 `Propeex/Tarkov-Helper`와 기타 참고 프로젝트는 `REFERENCE_POLICY.md`가 허용한 범위에서만 사용합니다.

---

## 2. 유지보수 변경 원칙

유지보수 작업은 먼저 다음 세 가지를 확인합니다.

1. **왜 변경이 필요한가** — 실사용 오류, 재현 가능한 회귀, 외부 계약 변화, 측정된 성능 문제, 문서/코드 불일치 등 근거가 있어야 합니다.
2. **현재 무엇이 보장되는가** — 관련 제품 요구사항, 결정, 테스트, 안전 계약을 확인합니다.
3. **무엇이 깨질 수 있는가** — 데이터 흐름과 기능 의존성을 따라 영향 범위를 확인합니다.

다음은 유지보수 사유만으로 하지 않습니다.

- 동작하는 코드를 단순히 더 깔끔해 보이게 만들기 위한 광범위 리팩터링
- 검증된 임계치/매칭/검증 규칙의 근거 없는 완화
- 이름에 `Legacy`가 있다는 이유만으로 활성 compatibility bridge 삭제
- 역사적 스크립트/fixture를 참조 조사 없이 삭제
- 외부 네트워크 결과를 일반 PR CI의 필수 성공 조건으로 만들기

Dead path 제거는 **현재 코드·워크플로·문서·테스트·릴리즈 경로에서 참조되지 않는다는 증거**가 확보된 경우에만 합니다.

---

## 3. Game Content 소유권과 안전한 갱신

준현 헬퍼 데이터는 성격이 다른 두 영역으로 분리합니다.

### 3.1 외부에서 다시 만들 수 있는 Game Content

예:

- 아이템/상인/지도
- 퀘스트와 objective/선행 관계
- 은신처 요구사항
- 탄약/획득 관계
- 번역·표시 리소스

이 데이터는 Tarkov 외부 원천에서 가져와 내부 canonical catalog/snapshot으로 변환한 **읽기 중심 콘텐츠**입니다.

### 3.2 사용자가 만든 Mutable State

예:

- 프로필 진행 상태
- 보유 아이템
- 설정/창 상태
- Scanner Ground Truth와 명시적으로 저장한 교정 자료

외부 Game Content 갱신이 이 사용자 상태를 초기화하거나 덮어쓰면 안 됩니다. 지원 번들/진단 수집도 각 문서의 개인정보·GT 제외 정책을 지킵니다.

---

## 4. Game Content 활성화 계약

외부 데이터 갱신은 **candidate → 검증 → activation** 순서로 처리합니다. 실패한 candidate가 현재 정상 snapshot을 변경해서는 안 됩니다.

활성화 전에는 두 종류의 검증을 구분합니다.

### 4.1 Candidate 자체의 의미/참조 검증

`GameContentIntegrityValidator` 계열이 현재 importer가 만든 후보의 필수 구조와 관계를 검증합니다. 이해하지 못한 핵심 의미를 추측해서 통과시키는 것보다 현재 정상 콘텐츠를 유지하는 쪽을 우선합니다.

### 4.2 Last-known-good baseline 대비 completeness 검증

`ContentUpdateCompletenessGuard`는 이미 정상 baseline이 있는 설치에서 부분 응답/상류 장애가 그럴듯한 작은 catalog로 변환되는 경우를 막습니다.

현재 계약:

- 핵심 entity/relationship 영역은 baseline의 **50% 미만으로 급감한 candidate를 Fatal**로 차단합니다.
- 한국어 번역 및 주요 표시 리소스 coverage도 충분한 baseline이 있을 때 같은 방식으로 보호합니다.
- 이 기준은 "Tarkov 데이터 개수는 고정"이라는 뜻이 아닙니다. 절대 개수가 아니라 **직전 정상 데이터 대비 비정상적인 대량 소실**을 부분 payload 신호로 취급하는 안전장치입니다.
- 첫 설치처럼 비교할 정상 baseline이 없으면 이 상대 비교만으로 후보를 거부하지 않습니다.

따라서 정상적인 패치의 추가/삭제를 고정 행 수로 제한해서도 안 되고, 반대로 구조 검증을 통과했다는 이유만으로 심각하게 축소된 candidate를 자동 활성화해서도 안 됩니다.

---

## 5. Schema drift 대응 계약

외부 API 변화는 세 수준으로 구분합니다.

- **표현 변화지만 의미 보존 가능** — 명시적으로 정규화하고 fixture로 고정합니다.
- **선택 정보의 미지원/누락** — 제품 정확도에 영향을 주지 않는 경우 warning/fallback으로 처리할 수 있습니다.
- **핵심 계산 의미를 보장할 수 없는 변화** — fail closed하고 현재 정상 콘텐츠를 유지합니다.

새 필드가 생겼다는 이유만으로 실패시키지 않지만, 퀘스트 해금/필요 아이템/은신처/탄약 수급 등 제품 결과에 영향을 주는 새로운 의미를 조용히 버려서는 안 됩니다.

실제 상류 변화에 대응할 때는 가능한 한 **실제 실패 payload/ID를 회귀 fixture로 축소 보존**한 뒤 importer/validator를 수정합니다. 라이브 데이터 한 시점에만 맞는 예외나 임의 임계치를 추가하지 않습니다.

---

## 6. Offline CI와 Live Data Probe의 역할 분리

### 6.1 일반 CI

PR/main CI는 재현 가능한 코드·fixture·테스트를 기준으로 합니다. 인터넷이나 `json.tarkov.dev`의 순간 상태 때문에 일반 빌드가 실패하도록 만들지 않습니다.

일반 CI가 보장하는 것은 **현재 저장소의 결정론적 계약이 깨지지 않았는가**입니다.

### 6.2 Live Data Contract Probe

`.github/workflows/live-data-probe.yml`은 일반 CI와 분리된 외부 계약 감시입니다.

- 수동 실행 가능
- 매일 예약 실행
- Regular/PvE를 각각 현재 외부 원천에서 빌드
- canonical importer/validator의 Fatal 여부를 확인
- source warning과 주요 entity 수량을 로그에 남겨 조사 단서를 제공

Live Probe 실패는 조사해야 할 강한 신호이지만, 원인이 네트워크/상류 장애일 수도 있으므로 일반 PR 병합을 자동으로 막는 hermetic gate와 동일하게 취급하지 않습니다.

Live Probe는 last-known-good 로컬 snapshot을 가지지 않으므로 런타임의 baseline-relative completeness guard를 대체하지 않습니다. 두 검증은 목적이 다릅니다.

---

## 7. Scanner 유지보수와 성능 검증

Scanner는 기능 완성 상태의 정확도 우선 파이프라인입니다. 유지보수에서는 `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`, `docs/SCANNER_GROUND_TRUTH.md`, `docs/CURRENT_SCANNER_WORK.md`, `docs/STATE.md`의 현재 계약을 먼저 확인합니다.

원칙:

- confirmed Item ID 이전에는 가격/필요량/slot 등 metadata를 결합하지 않습니다.
- stale/cross-frame 증거로 identity를 확정하지 않습니다.
- 정상 OCR 성공 경로에 불필요한 추가 분석/정규화를 삽입하지 않습니다.
- 현재 확정된 recognition threshold, candidate cap, pacing target 등을 성능 개선 명목으로 임의 완화하지 않습니다.
- 실사용 문제는 Ground Truth/재현 fixture/진단 로그처럼 다시 검증 가능한 증거로 남기는 방향을 우선합니다.

### 성능 baseline 원칙

CI에서 호스트 부하에 민감한 wall-clock 숫자를 제품 합격선으로 만들지 않습니다. 대신 다음을 조합합니다.

1. pacing/candidate/retry 같은 **결정론적 policy 계약 테스트**
2. 실제 런타임의 latency/응답성 telemetry와 trace
3. 필요할 때 대표 Ground Truth에 대한 재현 테스트

성능 회귀가 발견되면 먼저 병목 구간과 호출 횟수를 측정합니다. 정확도 계약을 완화해 시간을 줄이는 방식은 기본 해결책으로 사용하지 않습니다.

---

## 8. 회귀 테스트 원칙

버그 수정은 가능한 경우 다음 형태를 따릅니다.

`재현 테스트 실패 → 최소 수정 → 재현 테스트 성공 → 인접 계약 회귀 확인`

특히 다음은 테스트 없이 의미를 바꾸지 않습니다.

- 사용자 진행/보유량 계산
- Game Content importer/validator/activation
- Scanner identity/presentation
- Map/MiniMap donor compatibility bridge
- global hotkey precedence
- 데이터 마이그레이션/버전 호환

외부 API 장애 자체는 결정론적으로 재현할 수 없으므로, 발견된 schema/semantic drift를 작은 fixture 또는 validator 테스트로 옮겨 장기 회귀로 남깁니다.

---

## 9. 릴리즈 판단

- 제품 런타임/패키지 소스가 바뀌면 `VERSIONING.md`와 release gate를 적용합니다.
- 문서·테스트·워크플로만 바뀌고 기존 제품 바이너리가 그대로라면 기존 공개 릴리즈를 재태깅하거나 재업로드하지 않습니다.
- 공개 릴리즈는 immutable합니다.
- 릴리즈 후 문서-only merge commit과 실제 product source commit을 혼동하지 않습니다.

유지보수 완료 조건은 "코드를 바꿈"이 아니라 **필요한 근거가 저장소에 남고, 관련 자동 검증이 통과하고, 공식 문서가 실제 상태와 일치하는 것**입니다.
