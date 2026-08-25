# Decision — Scanner durable data ownership and configurable hotkeys

Date: 2026-08-26
Status: ACCEPTED / v1.7.7 maintenance patch

## Context

v1.7.6 이후 실사용에서 Scanner를 장시간 켜 두면 교정 데이터가 7GB 이상 증가하는 사례가 확인됐다.

사용자가 전달한 support data의 Case 51개는 모두 `UNREVIEWED / automatic_sample`이었으며, 상세보기 창이 없거나 인식에 실패한 연속 관측이 이미지 포함 durable Case로 저장되고 있었다.

이 자료는 사용자가 정답을 검토한 Ground Truth가 아니다. 런타임 관측과 사용자 소유 Ground Truth가 같은 durable storage에 자동 누적되는 것은 제품 목적과 맞지 않는다.

동시에 Scanner 사용자 로그에는 동일 실패가 반복되어 가시성이 낮았고, Scanner와 Map의 hotkey 입력 정책이 서로 반대였다.

## Decision 1 — durable correction data is user-selected

정상 Scanner runtime은 automatic diagnostic Case를 durable storage에 생성하지 않는다.

Runtime diagnostics contract:

```text
current capture / recognition evidence
→ latest exact frame in memory
→ runtime text diagnostic log
→ user explicitly chooses correction
→ reviewed durable Ground Truth
```

다음 상태만으로 durable Case를 만들지 않는다.

- no detail window
- structural/header failure
- OCR failure
- matcher failure
- low confidence / ambiguity
- repeated stationary failure

사용자가 교정 UI에서 명시적으로 저장한 Case만 Ground Truth dataset의 장기 자산이다.

## Decision 2 — legacy automatic data cleanup is proof-based and fail-closed

이전 버전의 자동 Case는 metadata로 아래 두 조건을 모두 증명할 수 있을 때만 자동 정리한다.

```text
retention = automatic_sample
review_status = unreviewed
```

추가 안전 조건:

- 최근 쓰기 중인 Case는 건드리지 않는다.
- 삭제 직전에 metadata/state를 다시 읽는다.
- 상태가 변경됐으면 보존한다.
- reviewed/manual Case는 자동 삭제하지 않는다.
- corrupt/unknown metadata는 보존한다.
- I/O 오류나 lock이 있으면 보존하고 나중에 다시 판단한다.

따라서 자동 정리는 사용자 Ground Truth의 retention policy가 아니다. legacy runtime noise의 migration cleanup이다.

## Decision 3 — runtime log and Ground Truth have separate lifetimes

Scanner text diagnostic log는 support/debugging을 위한 bounded ephemeral stream이다.

사용자 화면의 activity feed는 동일 실패를 짧은 window에서 collapse해 가시성을 유지한다. 원인 분석에 필요한 작은 rotated text log는 별도 bounded policy로 유지한다.

Ground Truth lifetime은 text log retention과 연결하지 않는다.

## Decision 4 — Scanner and Map share one hotkey contract

Scanner와 configurable Map actions는 다음 계약을 사용한다.

```text
one non-modifier primary key
+ optional Ctrl
+ optional Alt
+ optional Shift
```

따라서 bare key와 Ctrl/Alt/Shift의 임의 조합을 모두 지원한다.

Windows modifier는 지원하지 않는다.

Map 설정은 full gesture `(virtual key + modifiers)`를 JunhyunHelper-owned settings에 저장한다. 기존 key-only 설정에는 modifiers가 없으므로 `None`으로 migration하여 동작을 유지한다.

같은 primary key라도 modifier 조합이 다르면 서로 다른 binding이다. 완전히 같은 gesture가 중복 지정되면 마지막 지정 동작만 남긴다.

Bare `NumPad0~5`는 기존 direct floor selection에 예약한다. Modifier가 붙은 NumPad gesture는 configurable Map action에 사용할 수 있다.

## Non-goals

v1.7.7에서 다음은 변경하지 않는다.

- Scanner geometry/semantic detection thresholds
- OCR variants
- matcher acceptance
- visual corroboration/recovery acceptance
- candidate caps
- continuous scan target interval
- Mini Scanner presentation semantics
- game-data mapping semantics

이번 변경은 Scanner identity accuracy/performance 알고리즘 수정이 아니라 persistence/UX/input maintenance patch다.

## Safety invariants

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous target = 200 ms
```

false positive보다 miss를 선호하는 기존 계약과 scan-time network/game-memory/DLL-injection/packet-interception 금지도 유지한다.
