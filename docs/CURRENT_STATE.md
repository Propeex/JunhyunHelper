# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-02 KST**  
상태: **v1.16.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.16.3
exact product source/tag target:
89fae2e07b721b1dfd4922642412fcebf01b275d
validated PR head: 1c223a696e896e1af2ec1c35ec727eb3c70aa44d
merge PR: #282
PR CI / Shutdown / Docs:
33618363995 / 33618364028 / 33618363996 — SUCCESS
exact-main CI / Shutdown / Docs:
33618724736 / 33618724737 / 33618725069 — SUCCESS
Release workflow: 33619033186 — SUCCESS
release id: 381157194
published UTC: 2026-09-02T10:21:57Z
623 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 541000063
bytes: 80,735,580
SHA-256: eabc7c162ea583f138fbeb3bd2567145bc28c6f305bde20e049175c56580f657

SHA256SUMS.txt
asset id: 541000067
bytes: 86
asset SHA-256: c25ad9cb116c53143f1aece1a5035313d0a1176acff5b71c6366ea297d69dae5
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9842117423
bytes: 242,138,760
SHA-256: cda8d29a6dfa3499df8ba23522ed7faeb11475e726c6b8ed66566bb29eda55eb
```

Release workflow `33619033186` checked out exact main `89fae2e07b721b1dfd4922642412fcebf01b275d`, downloaded exact-main artifact `9842117423` with digest verification, verified published EXE/FIRST_RUN identity, and independently confirmed the actual ZIP SHA-256 matched `SHA256SUMS.txt` before publishing. The public release is `draft=false` and `prerelease=false`.

## v1.16.3 Farming Guide safety maintenance

v1.16.3 preserves the deterministic v1.16 rulebook while strengthening real-raid destructive/repacking boundaries.

- Secure-container-eligible high-value loot is considered for non-destructive secure promotion before ordinary free storage.
- Lower-priority secure contents are relocated to legal free storage rather than discarded when possible.
- Locked carrier roots remain protected from replacement while their legal internal storage remains usable.
- The same locked item instance may move during safe repacking as long as identity is preserved.
- Reserved-cell constraints remain intact.
- Expanded pocket geometry is used through transition/repacking paths.
- Stored stack quantity is included in destructive value/weight metrics.
- Multi-victim eviction uses a bounded deterministic subset search rather than prefix-only combinations.
- Minimum modeled food/drink and loose ammunition compatible with currently carried weapons are protected from automatic sacrifice.
- Existing loot receives special FIR priority only for actual `CurrentNeededFir`, not general non-FIR need.
- Final destructive recommendations fail closed unless locks, tactical reserves, complete sacrificed value and modeled weight constraints remain valid.

## MiniMap validation

The intermittent Player Marker Size smoke failure was traced to asynchronous donor-marker recreation, not a product behavior defect. Product behavior remains unchanged; the smoke now waits for standard-marker rendering to converge before comparing the independent marker boundary.

## Schema

```text
Desktop: 1.16.3
Content write/read: v12 / v3-v12
user.db: v1
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

Content v12 preserves source-backed `Energy`, `Hydration`, ammo/weapon caliber and weapon allowed-ammo facts required by the Farming Guide tactical-resource safety boundary.

## Validation coverage

Exact-main CI passed Release build, 623 deterministic tests, Windows x64 self-contained publish, actual published EXE Product UI / Map / Farming Guide decision smoke, graceful shutdown, clean portable-root checks, package creation and checksum verification.

The dedicated Farming Guide published-EXE smoke covers secure promotion, locked-carrier storage, expanded pockets, stored-stack total value, bounded victim selection, food/drink reserve, current-weapon ammo reserve, locked exact-instance movement and FIR-only priority semantics.

## Canonical references

- `docs/.release-v1.16.3-status.json`
- `docs/RELEASE_NOTES_V1.16.3.md`
- `docs/DECISION_FARMING_GUIDE_RULEBOOK_V1_16.md`
- `docs/PROJECT_STATE.json`
- `docs/STATE.md`

## External validation still pending

Automated development and release validation is complete. Separate actual-PC/Tarkov real-play validation remains `PENDING`; it does not alter the verified public v1.16.3 release identity or make the release incomplete.
