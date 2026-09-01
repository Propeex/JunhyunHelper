# ACTIVE WORK

Status: **NONE**

## Current task

없음. 현재 기준은 **v1.15.5 PUBLIC STABLE**이다.

## Latest completed work

v1.15.5 Farming Guide state-transition and UI maintenance가 구현·검증·병합·공개 릴리즈·공식 상태 기록까지 완료되었다.

```text
public stable: v1.15.5
exact product source/tag target:
62466a957a7e32a623a0ffcfad96bfb16504f823
validated PR head:
2d9f01da32e3e80860c5a87b2d2e73bc87c31b17
merge PR: #271
exact-main CI / Shutdown / Docs:
33520705401 / 33520705533 / 33520705395 — SUCCESS
Release workflow: 33521076146 — SUCCESS
release id: 380587916
593 passed / 0 failed / 0 skipped
```

완료된 제품 변경:

- Mini Scanner Farming Guide 지시를 짧은 장착/교체/보관/버리기 중심 문구로 정리했다.
- 같은 visible storage area 내부의 grid/X/Y/rotation 재배치는 지시에서 숨기고 실제 cross-area 이동/폐기만 부가 작업으로 표시한다.
- Key tool 같은 compact nested Workbench가 물리적으로 들어가는 경우 가로/세로 scrollbar feedback으로 하단 셀이 잘리는 문제를 수정했다.
- 장비 교체로 벗겨진 기존 장비/리그/가방을 삭제하지 않고 loot candidate로 되돌려 합법적인 보관·nesting·repacking을 우선 탐색한다.
- displaced carrier가 다른 storage 안에 보관된 상태에서도 그 내부 grid를 같은 ProposedSnapshot의 storage surface로 사용할 수 있다.
- destructive replacement는 별도 retention policy 아래 bounded multi-victim search를 허용하되 locked/populated structural state를 보존한다.
- Needed 획득량을 historical accept counter가 아니라 raid baseline 대비 현재 snapshot Item ID 수량으로 계산한다.
- 기존 source-backed equipment superiority, filters, dedicated-container preference, locks, reserved cells, nested graph safety, complete-equipment boundary, explicit accept transaction semantics를 유지했다.

## Verification

- PR final head CI / Shutdown Race / Documentation Consistency: SUCCESS
- exact-main Windows Release build: SUCCESS
- deterministic tests: 593 passed / 0 failed / 0 skipped
- self-contained win-x64 publish: SUCCESS
- published EXE product/runtime smoke: SUCCESS
- Farming Guide v1.15.5 transition/instruction/4x4 nested Workbench regression smoke: SUCCESS
- graceful shutdown / Shutdown Race: SUCCESS
- package/checksum verification: SUCCESS
- `refs/tags/v1.15.5`, release target and `/releases/latest` exact product source 일치: VERIFIED
- public assets GitHub SHA-256 digest와 exact-main package evidence 일치: VERIFIED

## Recovery point

다음 작업은 `AGENTS.md` → `docs/PROJECT_STATE.json` → 이 문서를 확인한 뒤 **v1.15.5 stable main**에서 새 요청으로 시작한다.

실제 사용자 PC/Tarkov 플레이 검증은 별도 외부 증거로 `PENDING`이며, 현재 공개 릴리즈의 자동 검증 완료 상태를 변경하지 않는다.
