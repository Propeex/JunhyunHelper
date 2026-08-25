# DECISION — v1.7.2 Mini Scanner presentation stability

상태: `CONFIRMED / RELEASED`

기준일: 2026-08-25

## 1. 사용자 문제

실사용에서 Scanner recognition log는 같은 아이템을 계속 성공으로 식별하고 있는데도 Mini Scanner 결과가 반복적으로 나타났다 사라지는 현상이 확인되었다.

원인은 Item identity recognition과 Mini Scanner visibility가 서로 다른 생명주기를 사용한 데 있다.

- Scanner Item identity는 정상 성공할 수 있다.
- Mini Scanner는 별도로 Tarkov 상단 inventory/stash context OCR을 반복 수행했다.
- 기존 구현은 이 보조 context OCR이 한 번 `false`를 반환하면 이미 표시 중인 정상 Item 결과도 즉시 `Hide()`했다.
- continuous runtime의 일부 재확인/보류 상태도 `ShowStandby()`를 통해 현재 표시를 즉시 제거했다.

따라서 recognition 정확도와 무관하게 화면이 깜빡일 수 있었다.

## 2. 제품 동작 결정

Mini Scanner는 매 scan tick의 순간 상태를 그대로 표시하지 않고 **마지막으로 확정된 Item presentation을 안정적으로 유지하는 sticky presentation**을 사용한다.

정책:

```text
No Item
  └─ A 확정 → Show A

Show A
  ├─ A 재확정 → A 계속 표시 / miss budget reset
  ├─ B 확정 → 즉시 B로 교체 / miss budget reset
  ├─ 실제 식별 miss #1 → A 유지
  ├─ 실제 식별 miss #2 → A 유지
  └─ 실제 식별 miss #3 → Hide
```

`candidate 안정화 중`, `아이템 이름 읽는 중`, `제목 변화 확인 중` 같은 진행 상태는 실패 횟수에 포함하지 않는다.
하나의 scan attempt 내부 진행 메시지가 여러 번 발생해 miss를 중복 집계해서는 안 된다.

## 3. Runtime identity와 presentation 분리

Scanner의 보수적 identity state와 사용자에게 보이는 presentation state는 분리한다.

기존 runtime의 geometry 재확인 정책은 유지한다.

- runtime `MissesToHide = 2`는 내부 verified identity를 버리고 재탐색을 시작하는 기존 안전 경계로 유지한다.
- Mini Scanner presentation은 별도 `ScannerPresentationRetention`으로 3회 연속 실제 miss를 허용한다.
- 내부 identity가 두 번째 geometry miss에서 제거되더라도 Mini Scanner는 마지막 정상 snapshot을 세 번째 presentation miss까지 유지할 수 있다.
- 새 Item ID가 확정되면 과거 snapshot을 기다리지 않고 즉시 새 snapshot으로 교체한다.

이 분리는 stale identity를 recognition proof로 재사용하지 않으면서도 UI flicker를 제거하기 위한 것이다.

## 4. Inventory/stash context OCR 역할

`ScannerInventoryContextDetector`의 보조 context OCR 자체는 제거하지 않는다.

계약:

- hidden Mini Scanner가 처음 열릴 때는 foreground Tarkov inventory/stash context를 확인한다.
- initial context gate가 실패하면 hidden overlay는 열지 않는다.
- 한 번 initial gate를 통과해 정상 Item이 표시된 뒤에는 authoritative Scanner Item success가 liveness authority다.
- 이미 visible인 정상 Item을 단 한 번의 auxiliary context-OCR `false`/exception으로 숨기지 않는다.
- visible 상태에서 다른 Item B가 확정되면 별도 context 재검사 때문에 A를 비우지 않고 B로 즉시 갱신한다.
- presentation이 정상적으로 숨겨진 뒤 다음 initial show에는 context gate를 다시 적용한다.

따라서 context OCR은 **entry safety gate**이며 visible Item의 per-tick liveness proof가 아니다.

## 5. Hard hide와 transient hold

다음은 즉시 hide한다.

- Scanner OFF / Stop
- 명시적 preview 종료
- runtime suspend
- profile/catalog/vision unavailable 같은 hard standby
- runtime fatal/error
- one-shot 실패/종료의 기존 명시적 Hide 경로
- application dispose

다음은 현재 Item을 유지한다.

- candidate 안정화
- title change 안정화
- OCR 진행 중

다음 실제 실패는 presentation miss 1회로 집계한다.

- detail candidate 미탐
- semantic Item identity 확정 실패
- 확정 Item ID의 presentation snapshot 생성 실패
- 기존 snapshot refresh 실패

성공한 `Show(Item)`은 동일 Item/다른 Item 모두 miss budget을 즉시 0으로 되돌린다.

## 6. Recognition safety — 변경 금지

이번 PATCH는 presentation stability 수정이다.

다음 recognition 계약은 변경하지 않았다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous scan interval = 350 ms
semantic retry interval = 1200 ms
```

또한 다음을 유지한다.

- false positive보다 miss 선호
- official current catalog identity authority
- semantic header gate
- OCR/matcher confidence contract
- cross-frame OCR identity proof 재사용 금지
- Scanner correction / Ground Truth 구조

성능 최적화는 latency telemetry와 실사용 evidence를 바탕으로 별도 작업한다.

## 7. Deterministic regression tests

`ScannerPresentationRetention`은 Desktop/WPF와 분리된 pure state policy로 두고 xUnit에서 다음을 고정한다.

1. A 확정 후 miss 1/2에서는 A 유지
2. 세 번째 연속 miss에서 hide
3. A 재확정은 miss budget reset
4. A 표시 중 B 확정은 즉시 B로 교체
5. hard reset은 즉시 clear
6. Item이 없는 상태의 miss는 잘못된 state를 만들지 않음

WPF/Product smoke는 기존 Mini Scanner render/topmost/non-activation/drag/layout 계약을 계속 검증한다.

## 8. 버전

이 변경은 새 사용자 기능이 아니라 기존 Mini Scanner의 표시 안정성/UX 버그 수정이다.
`docs/VERSIONING.md`의 PATCH 규칙에 따라 **v1.7.2**로 공개했다.

## 9. 최종 검증

PR #180 final CI 및 exact main source CI를 모두 통과했다.

```text
PR: #180
release source: 8775feba23a2c9ecc6326626527cdfd54f4f0414
main CI run: 32842508995
release workflow run: 32842783940
build: SUCCESS
362 / 362 tests: SUCCESS
Windows x64 publish: SUCCESS
Product UI / Scanner / Mini Scanner smoke: SUCCESS
Map / Factory / MiniMap smoke: SUCCESS
package verification: SUCCESS
```

공개 release:

```text
tag/latest: v1.7.2
asset: Junhyun-Helper.zip
bytes: 80,444,391
SHA-256: 81d8e6a82db0f4b33ebbdd2bf7f455c1d92ffc2f8b6015f6ba6190e616be1fc0
draft: false
prerelease: false
```

GitHub public release metadata를 다시 읽어 target commit, asset size, asset SHA-256이 Release runner의 검증값과 일치함을 확인했다.

## 10. 완료 판정

- deterministic retention tests 통과: 완료
- 전체 automated tests 0 failed / 0 skipped: 완료
- Windows x64 publish 성공: 완료
- Product UI / Scanner / Mini Scanner rendered smoke 성공: 완료
- Map / Factory / MiniMap smoke 성공: 완료
- Scanner recognition threshold/candidate cap 변경 없음: 완료
- PR CI 성공 후 main 병합: 완료
- main CI 성공: 완료
- v1.7.2 public stable/latest release: 완료
- stable ZIP/hash/size public metadata readback: 완료
- 공식 상태 문서/release notes 기록: 완료
