# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-02 KST**  
상태: **v1.16.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.16.4
exact product source/tag target:
5886d8f97abd060d398d4c50d3dd3b720e4ace09
validated PR head: d55e138c962e87dc8691f82c81d36a516db52941
merge PR: #285
PR CI / Shutdown / Docs:
33623459284 / 33623459290 / 33623459267 — SUCCESS
exact-main CI / Shutdown / Docs:
33623824030 / 33623824052 / 33623824027 — SUCCESS
Release workflow: 33624248788 — SUCCESS
release id: 381192920
published UTC: 2026-09-02T11:22:47Z
623 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 541072599
bytes: 80,738,891
SHA-256: 2ceddbd3cc805bc8de2cdb5eddcef72c2001a6724a43ec7fdd993781af649fb4

SHA256SUMS.txt
asset id: 541072598
bytes: 86
asset SHA-256: 2a07506d6c84048940a35beb7aa637de9e27dd51bea25600a9b62a5a93f6017f
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9844117414
bytes: 242,151,516
SHA-256: f2aea11845611012d26bc135f8d6386200ea5007382d441b652ef6d1b3f86477
```

Release workflow `33624248788` checked out exact main `5886d8f97abd060d398d4c50d3dd3b720e4ace09`, re-downloaded exact-main artifact `9844117414` with digest verification, verified published EXE/FIRST_RUN identity, and independently confirmed the actual ZIP SHA-256 matched `SHA256SUMS.txt` before publishing. The public release is `draft=false` and `prerelease=false`.

## v1.16.4 Farming Guide locked-position hotfix

The v1.16.3 identity-only interpretation of exact item locks is superseded. An explicitly locked stored item is now a hard automatic-position constraint.

- The locked instance cannot be discarded, replaced, relocated, rotated or re-parented by automatic advice.
- A stored ancestor cannot move when doing so would indirectly move a locked descendant.
- A root Rig / Backpack / SecureContainer replacement is rejected when it would indirectly move a locked descendant.
- Secure-container promotion and ordinary repacking fail closed when a valid plan requires changing locked placement.
- Final safety re-checks exact storage kind, grid, coordinates, rotation, parent, quantity, ancestor placement and root-carrier identity.
- Manual user editing remains authoritative.
- A locked root carrier remains protected from replacement while its legal internal free storage remains usable; independently unlocked contents remain ordinary planning candidates.
- Reserved-cell behavior is unchanged.

The published-EXE regression suite contains the real-play shape that exposed the bug: secure-container loot evaluation must not instruct moving an explicitly locked existing item such as the reported Grizzly case.

## Retained v1.16.3 decision safety

v1.16.4 retains secure-container promotion, quantity-aware destructive economics, bounded victim-subset search, expanded-pocket geometry, source-backed food/drink and current-weapon ammunition protection, FIR-only special priority, and the final fail-closed destructive boundary. Content schema v12 remains the current source-backed data contract.

## Schema

```text
Desktop: 1.16.4
Content write/read: v12 / v3-v12
user.db: v1
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

## Validation coverage

Exact-main CI passed Release build, **623/623 deterministic tests**, Windows x64 self-contained publish, actual published EXE Product UI / Map / Farming Guide decision smoke, graceful shutdown, clean portable-root checks, package creation and checksum verification. Dedicated Shutdown Race and Documentation Consistency workflows also passed on the exact product source.

## Canonical references

- `docs/.release-v1.16.4-status.json`
- `docs/RELEASE_NOTES_V1.16.4.md`
- `docs/DECISION_FARMING_GUIDE_RULEBOOK_V1_16.md`
- `docs/PROJECT_STATE.json`
- `docs/STATE.md`

## External validation still pending

Automated development and release validation is complete. Separate actual-PC/Tarkov real-play validation remains `PENDING`; it does not alter the verified public v1.16.4 release identity or make the release incomplete.
