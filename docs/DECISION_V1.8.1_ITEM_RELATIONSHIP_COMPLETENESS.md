# v1.8.1 Item Relationship Completeness Hardening

상태: **CONFIRMED / IMPLEMENTED / PUBLIC STABLE VERIFIED**

## 배경

v1.8.0 공개 후 release-state 문서 검토 과정에서 Scanner 아이템 정보 DB의 관계 데이터에 한 가지 LKG 보호 공백이 확인되었다.

v1.8.0은 다음을 이미 검증했다.

- trader purchase / barter / craft / flea 관계의 reference integrity
- price / count / limit의 값 무결성
- build 단계의 관계 validation

하지만 기존 `ContentUpdateCompletenessGuard`의 50% retained-floor 비교 대상에는 새 `ItemRelationshipData` 컬렉션이 포함되지 않았다.

따라서 upstream response가 JSON/schema 관점에서는 정상인데 trader purchase / barter / craft / flea 관계 대부분을 누락하는 경우, 남아 있는 entry가 자체적으로 유효하면 candidate가 LKG를 대체할 수 있었다. 이 경우 Scanner 아이템 DB에서 수급처가 대량 누락되거나 잘못된 raid fallback이 표시될 수 있다.

또한 in-memory build 뒤 관계 validator는 실행되었지만 persisted candidate read-back과 active snapshot validation boundary에서는 같은 item-relationship integrity validator가 반복되지 않았다.

공개 v1.8.0 asset은 immutable이므로 같은 version을 교체하지 않고 v1.8.1 PATCH로 수정했다.

## 결정

### 1. 관계 컬렉션에도 기존 50% LKG shrink guard 적용

v8+ healthy baseline에 `ItemRelationshipData`가 존재하면 다음을 각각 독립적으로 비교한다.

- trader direct purchases
- trader barters
- hideout crafts
- flea acquisition item IDs
- barter required-item edges
- craft required-item edges

candidate count가 baseline의 50% 미만이면 Fatal validation으로 candidate activation을 거부하고 기존 LKG를 유지한다.

관계 종류를 하나의 합산 숫자로 합치지 않는다. 특정 관계 도메인만 사라지는 partial payload도 잡아야 하기 때문이다.

### 2. v3~v7 backward compatibility 유지

v3~v7 snapshot에는 `ItemRelationshipData` 자체가 없다.

이 legacy snapshot은 첫 v8+ update의 상대 비교 baseline으로 사용하지 않는다. `null`은 `실제 empty graph`가 아니라 `이 schema에서는 아직 수집하지 않음`이라는 기존 의미를 유지한다.

### 3. fresh v8+ graph의 전면 empty는 fail closed

새로 수집한 v8+ `ItemRelationshipData`가 존재하면서 다음 critical relation collection이 통째로 비어 있으면 정상 Game Content로 보지 않는다.

- trader purchases
- barters
- crafts
- flea acquisition items

이는 첫 설치/첫 v8 update처럼 relative baseline이 없는 경우에도 명백한 전면 source loss를 차단하기 위한 최소 sanity floor다.

### 4. persisted/active snapshot에서도 관계 validation 반복

관계 검증은 in-memory build 한 번으로 끝내지 않는다.

```text
build
→ base + item relationship integrity
→ completeness vs LKG
→ write candidate
→ read candidate
→ base integrity + item relationship integrity + completeness vs LKG
→ activation boundary read
→ base canonical validation + item relationship integrity
→ active read/recovery
```

따라서 serialization/storage/read-back 또는 active-file corruption이 관계 graph를 손상시키면 activation/recovery boundary에서도 fail closed한다.

### 5. Scanner recognition 계약은 변경하지 않음

v1.8.1은 Game Content validation/LKG patch다.

다음은 변경하지 않는다.

- structural floor 0.34
- `HEADER_FRAME_LOCKED` floor 0.68
- continuous candidates 8
- one-shot candidates 12
- 200ms observation target
- OCR matcher / visual recovery acceptance
- mapped relationship data를 Item identity proof로 사용하지 않는 규칙

## 검증 완료

전용 deterministic regression 5건을 추가했고 전체 suite는 다음과 같이 통과했다.

```text
418 passed / 0 failed / 0 skipped
```

전체 PATCH release gate:

- Release build — SUCCESS
- win-x64 self-contained single-file publish — SUCCESS
- actual published EXE Product UI / Scanner / Main Map / Factory / MiniMap smoke — SUCCESS
- graceful shutdown / clean portable root — SUCCESS
- package/checksum verification — SUCCESS
- exact-main CI `33132600931` — SUCCESS
- Release workflow `33132798167` — SUCCESS
- public `v1.8.1` tag/release/assets readback — SUCCESS

Exact product release source:

```text
dade2ef4dadbf58659b75c80d421bd3738003ff8
```

Public ZIP:

```text
bytes: 80,520,704
SHA-256:
b30cbb045cc089c90108e2d3394510ef6778019ea0a50f6ae16d14de7aaafe9a
```

상세 공개 증거는 `docs/RELEASE_1.8.1.md`와 `docs/.release-v1.8.1-status.json`을 따른다.
