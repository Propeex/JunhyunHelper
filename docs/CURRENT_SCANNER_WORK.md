# Current Scanner Work

상태: **FEATURE COMPLETE / MAINTENANCE ONLY**

이 문서는 Scanner의 현재 유지보수 진입점과 불변식을 요약한다.

정확한 현재 제품 버전, release source, CI run, asset hash와 test count는 이 문서에 복제하지 않는다. 반드시 다음 canonical source를 사용한다.

1. `docs/PROJECT_STATE.json`
2. `docs/ACTIVE_WORK.md`
3. `docs/CURRENT_STATE.md`
4. `docs/STATE.md`

Scanner의 상세 설계/검증 authority:

- `docs/SCANNER.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/SCANNER_GROUND_TRUTH.md`
- current Scanner decision series

## 현재 운영 상태

Scanner는 기능 추가 단계가 아니라 **실사용 증거 기반 유지보수 단계**다.

새 실제 회귀 증거가 없는 한 다음을 선제적으로 변경하지 않는다.

- structural/header acceptance threshold
- candidate cap
- OCR/matcher/visual acceptance
- observation pacing
- fail-closed identity policy

특히 성능 정리나 코드 정리를 이유로 recognition 의미를 완화하지 않는다.

## Current pipeline

```text
Tarkov window/display pixels
→ capture
→ detail rectangle proposals
→ inspect-header semantic validation
→ item-name ROI
→ serialized Windows ko-KR OCR
→ bounded environment/title normalization
→ optional persisted user OCR substitution
→ current official Korean item catalog sanitation/matching
→ bounded reviewed-evidence recovery where explicitly supported
→ optional strict current-pixel visual corroboration
→ Item ID or fail closed
→ local presentation join
→ Scanner Page / Mini Scanner
→ optional explicit user correction / reviewed Ground Truth
```

Scanner는 closed-domain recognizer이며 current official Tarkov catalog가 Item identity authority다.

## Safety / identity invariants

- false positive는 miss보다 나쁘다.
- geometry/environment normalization은 Item identity proof가 아니다.
- stale/cross-frame OCR 또는 visual result를 current identity proof로 사용하지 않는다.
- Item ID 확정 전에 price/needed/source/relationship metadata를 identity evidence로 사용하지 않는다.
- scan 순간 identity 결정을 위해 network 요청을 시작하지 않는다.
- reviewed Ground Truth evidence 없이 matcher/visual acceptance를 완화하지 않는다.
- game memory read, injection, process/game hook, kernel/driver access, input automation, network manipulation, anti-cheat bypass를 사용하지 않는다.

구체적인 현재 threshold/candidate/pacing 값은 `docs/SCANNER.md`와 해당 policy/test를 함께 확인한다. 이 요약 문서에 숫자를 중복 저장하지 않는다.

## Presentation authority

Item ID 확정 뒤 필요한 개수와 source는 Scanner가 재계산하지 않는다.

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

Mini Scanner는 confirmed Scanner Item ID를 presentation authority로 사용한다.

## Ground Truth / diagnostics

- normal monitoring은 durable automatic correction truth를 만들지 않는다.
- current correction용 최신 evidence는 in-memory state로 유지할 수 있다.
- user-explicit reviewed save만 authoritative Ground Truth다.
- reviewed/manual/corrupt/unknown/state-changed Case는 임의 자동 삭제하지 않는다.
- support bundle과 Ground Truth lifetime은 분리한다.
- support export는 reviewed Ground Truth/source pixels, user.db, profile/account-identifying progress data를 포함하지 않는다.

## 유지보수 진입 조건

다음 중 하나가 있을 때 Scanner recognition maintenance를 시작한다.

1. 실제 Tarkov 화면에서 재현 가능한 인식 회귀
2. user-reviewed Ground Truth가 특정 failure stage를 입증
3. Tarkov UI/locale/rendering 변화가 기존 계약을 깨뜨림
4. telemetry/trace가 실제 runtime 문제를 입증

기본 작업 순서:

```text
runtime evidence
→ failure stage 분류
→ root cause
→ affected layer 최소 수정
→ deterministic/reviewed regression
→ applicable Windows release gate
```

현재 진행 중 Scanner 전용 작업 유무는 항상 `docs/ACTIVE_WORK.md`를 기준으로 판단한다.
