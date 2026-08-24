# DECISION — v1.7.0 Product Completion Hardening

상태: `IMPLEMENTED / PUBLIC RELEASE VERIFIED`

기준일: 2026-08-24

## 1. 목적

준현 헬퍼의 굵직한 제품 기능 구현은 Scanner까지 포함하여 완료되었다. 이번 작업은 새 기능을 확장하는 단계가 아니라 현재 제품을 장기적으로 안정적이고 정확하며 예측 가능하게 사용할 수 있도록 전체 완성도를 높이는 최종 hardening pass다.

사용자가 별도로 수집·전달할 실제 Tarkov Scanner Ground Truth를 기반으로 하는 detection/OCR/matcher 정확도 튜닝은 이번 범위에서 제외한다.

## 2. 버전

이번 작업에는 Scanner 인식 로그에서 해당 결과를 직접 선택하여 즉시 교정 흐름으로 진입하는 새로운 사용자 capability가 포함된다.

`docs/VERSIONING.md`의 SemVer 정책에 따라 목표 버전은 **v1.7.0 MINOR**다.

## 3. 확정된 사용자 기능 범위

### 3.1 Scanner 로그 → 즉시 교정

Scanner 인식 로그에서 교정 가능한 결과를 선택해 해당 결과의 증거를 다시 찾아 헤매지 않고 바로 교정 화면으로 진입할 수 있어야 한다.

원칙:

- 해당 로그와 실제 저장된 diagnostic evidence 사이의 식별 가능한 연결이 있을 때만 교정 동작을 제공한다.
- stale frame이나 다른 결과를 추정으로 연결하지 않는다.
- 교정 불가능한 로그는 버튼/동작을 비활성화하거나 명확히 처리한다.
- 기존 Ground Truth 저장 계약과 검토 상태를 재사용한다.

### 3.2 Scanner 개발 자료 ZIP

기존 `ScannerDiagnosticDataset.ExportAsync()`가 이미 Ground Truth와 Scanner 로그를 ZIP으로 내보내는 capability를 제공한다.

따라서 중복 기능은 추가하지 않는다. 이번 작업에서는 접근 경로, 포함 자료, 실패 처리, ZIP 생성 무결성을 감사하고 필요한 보완만 수행한다.

### 3.3 이번에 추가하지 않는 기능

- Scanner 상태 표시 UI
- Mini Scanner preset
- 통합 빠른 검색
- 설정 백업/복원

## 4. Data Update — 제품 핵심 신뢰 경계

준현 헬퍼의 퀘스트, 아이템, 상인, 지도, 은신처, 탄약 및 이들 사이의 파생 관계는 Data Update에서 구축되는 canonical Game Content에 의존한다. Scanner의 최신 catalog 동기화도 일반 Game Content update 이후 별도 안전 경계에서 수행된다.

따라서 Data Update를 제품의 핵심 transactional boundary로 취급한다.

새 데이터는 다음 순서를 모두 통과하기 전에는 active data를 대체할 수 없다.

```text
remote sources download
→ import/normalization
→ domain shape + invariant validation
→ cross-reference validation
→ candidate persistence
→ candidate read-back/revalidation
→ atomic activation
→ active snapshot load verification
```

검증 대상에는 최소 다음이 포함된다.

- Items: ID/중복/필수 표시명/참조 가능한 icon·wiki metadata의 형상
- Traders: ID/중복/필수 표시 정보
- Maps: ID/중복/필수 표시 정보
- Quests: ID/중복, trader/map, prerequisite, special access, failure condition, dependency cycle
- Quest objectives / quest item requirements: quest/item 연결, objective 식별자, count, accepted item 집합
- Hideout: station/level 형상, item requirement, count, station identity
- Ammunition: item identity, acquisition trader/station/quest/currency/required item 연결, 중복 및 수치 invariant
- Editions: identity와 quest rule 충돌/참조
- 전체 catalog의 critical domain이 비정상적으로 비거나 명백히 축소된 payload가 정상 candidate로 활성화되지 않도록 semantic completeness guard를 둔다.

외부 upstream이 일시적으로 partial payload를 반환하더라도 기존 정상 active snapshot은 그대로 보존한다.

## 5. 내부 hardening 범위

### 5.1 Scanner concurrency / cancellation

- Scanner ON/OFF, one-shot, 연속 frame, 종료가 겹쳐도 stale result가 최신 UI/state를 덮어쓰지 않게 한다.
- 취소된 작업은 후속 결과를 commit하지 않는다.
- 중복 OCR/scan 작업의 불필요한 병렬 실행을 제한한다.
- shutdown 시 background task와 event subscription이 안전하게 종료된다.

### 5.2 Scanner resource lifecycle

- Bitmap/frame/ROI/stream/timer/CancellationTokenSource/event subscription의 생성·소유·해제를 감사한다.
- 불필요한 clone/conversion과 동일 frame의 중복 계산을 제거할 수 있는 경우에만 최적화한다.
- 장시간 실행에서 지속적으로 자원이 증가하는 구조를 허용하지 않는다.

### 5.3 Persistence / cache safety

중요 사용자 데이터와 재생성 가능한 cache를 구분하고, 저장 경계는 가능한 한 다음 계약을 따른다.

```text
serialize candidate
→ validate/write temporary
→ flush/complete
→ atomic replace
→ read-back validation where critical
```

파일 손상, write failure, lock, cancellation, schema mismatch, 비정상 종료가 기존 정상 데이터를 파괴하지 않아야 한다.

### 5.4 Data Update state semantics

`fresh`, `updated`, `healthy fallback`, `unavailable`, `invalid`, `cancelled`와 같은 의미가 UI/diagnostics/retry/cache 정책 사이에서 모순되지 않도록 상태 판단을 감사하고 필요한 공통 정책을 둔다.

정상 cache hit를 실패로 오분류하거나 실제 실패를 성공처럼 표시하지 않는다.

### 5.5 UI thread / reentrancy

- network/OCR/image/file I/O가 UI thread를 장시간 점유하지 않는지 확인한다.
- 동일 command의 빠른 반복 실행이 중복 update/scan/window 작업을 만들지 않도록 한다.
- background 결과의 UI 반영은 올바른 dispatcher/lifetime 경계를 따른다.

### 5.6 Mapping integrity

canonical item ID가 결정된 이후 Scanner/UI presentation에서 다음 정보가 다른 item과 섞이지 않음을 전수 가능한 테스트로 검증한다.

- 이름/아이콘/wiki
- 최고 상점 판매가와 상인
- Flea 평균가
- 크기/slot 수 및 slot당 가격
- 퀘스트/은신처 필요 개수

### 5.7 Fault / concurrency regression coverage

정상 happy-path뿐 아니라 timeout, HTTP failure, malformed payload, cache corruption, write failure, cancellation, stale completion, repeated command, shutdown 중 background work를 가능한 범위에서 자동 테스트한다.

### 5.8 Release trust boundary

성공한 `main` CI artifact만 stable release 후보가 될 수 있다는 v1.6.1 계약을 유지한다.

추가로 새 stable release publish 후 공개 GitHub asset을 다시 내려받아 다음을 검증하는 단계를 목표로 한다.

- SHA-256
- ZIP layout
- executable ProductVersion
- FIRST_RUN version identity
- 가능한 범위의 startup/product smoke

이미 공개된 동일 version의 asset은 immutable로 유지한다.

### 5.9 Official documentation consistency

현재 실제 version/release state와 공식 프로젝트 문서의 stale wording이 서로 충돌하지 않도록 정리한다. 핵심 version/state 불일치는 CI에서 가능한 범위까지 자동 검증한다.

## 6. 변경 금지 — Scanner live recognition tuning

이번 작업에서는 reviewed live Ground Truth 없이 다음을 변경하지 않는다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
```

또한 다음 안전 계약을 유지한다.

- false positive보다 miss 선호
- red close-X + magnifier semantic evidence
- official current Korean Scanner catalog identity authority
- production OCR field = item-name only
- scan-time network 금지
- automatic global OCR forced substitution 금지
- cross-frame OCR cache 금지
- game memory read / DLL injection / packet interception 금지

실사용 recognition 개선은 이후 사용자가 제공하는 reviewed evidence를 근거로 별도 작업한다.

## 7. 완료 기준

v1.7.0은 단순히 코드 변경이 끝났다고 완료하지 않는다.

최소 완료 gate:

- 모든 기존 자동 테스트 유지 + 신규 hardening regression tests
- 0 failed / 0 skipped
- Windows x64 release publish 성공
- Product UI / Scanner / Mini Scanner rendered smoke
- Main Map / Factory / MiniMap smoke
- Scanner quick-correction 동작 및 기존 correction/GT 회귀 검증
- Data Update 전체 domain/invariant/cross-reference validation 검증
- invalid/partial candidate가 active snapshot을 대체하지 않음
- persistence/cancellation/fallback 회귀 검증
- clean shutdown / portable root cleanliness
- stable package checksum/layout/version 검증
- 공개 release asset 재검증
- 공식 상태 문서 최신화

검증 중 회귀가 발견되면 원인을 해결하기 전에는 v1.7.0을 릴리즈하지 않는다.

## 8. Public Release 검증 완료

2026-08-25 기준 v1.7.0은 모든 release gate를 통과했다.

- exact source/tag: `56e12342e3490fd0defa5f327a03d20d4f32b3a6`
- automated tests: `348 passed / 0 failed / 0 skipped`
- ProductVersion: `1.7.0+56e12342e3490fd0defa5f327a03d20d4f32b3a6`
- rendered Product UI / Scanner / Scanner Advanced / Quest sidebar 검증
- Main Map / Factory / MiniMap smoke 통과
- graceful shutdown + clean portable root 통과
- public stable/latest: `v1.7.0`
- public asset: `Junhyun-Helper.zip`
- public bytes: `80,443,318`
- public SHA-256: `1c640c80bf6113176b885a47e19478666e27dbf584f872d1a8396886334f3418`
- anonymous public redownload / checksum / ZIP layout 검증 통과
- public-downloaded EXE ProductVersion / FIRST_RUN 확인
- public-downloaded rendered product/Map smoke + normal close 통과
- public proof run: `32745399476`

따라서 v1.7.0 Product Completion Hardening은 **PUBLIC RELEASE VERIFIED**로 완료한다. 다음 Scanner 단계는 기존 안전 기준을 유지한 **LIVE GROUND TRUTH MAINTENANCE**이며, reviewed evidence 없이 detection/OCR/matcher threshold 또는 candidate cap을 조정하지 않는다.
