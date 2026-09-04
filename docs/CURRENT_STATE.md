# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-04 KST**  
상태: **v1.17.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.17.0
exact product source/tag target:
8b0e1f8f46fa3822f4cff05b7be3223d40ad7435
validated PR head: a01d61cd9957db94a7475734c1e8df66ce71f53d
merge PR: #288
PR CI / Shutdown / Docs:
33746966753 / 33746966804 / 33746966771 — SUCCESS
exact-main CI / Shutdown / Docs:
33748900315 / 33748900348 / 33748900377 — SUCCESS
Release workflow: 33749193376 — SUCCESS
release id: 381959220
published UTC: 2026-09-03T11:21:35Z
649 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 542663027
bytes: 80,766,362
SHA-256: 6ecc3a61d0b492f6b475e18f309e55790776911e5496fc704d12ffd611c629cb

SHA256SUMS.txt
asset id: 542663026
bytes: 86
asset SHA-256: 7a2fb4f7ebcb333eafd8cad6f9acbf532549118e608776786666014a24875bdf
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9890816795
bytes: 242,234,759
SHA-256: d9115f24968804fc5b4e65fa7bbaaf008f4af516e044f3b00e0ee6b4525a15dd
```


## v1.17.1 active removal work

User-confirmed target: **remove Farming Guide completely**.

Implementation branch: `product/remove-farming-guide-2026-09-04`  
Draft PR: **#290**

Removed from the target product:

- Farming Guide main page/navigation;
- loadout/preset/raid-session/optimizer/repacking/locks/weight/quantity flows;
- Scanner Farming Guide bridge, Mini Scanner instruction/quantity state and accept hotkey/settings;
- Farming Guide Core/Desktop/Infrastructure implementation and persistence;
- Farming Guide-only Game Content metadata/import coverage;
- Farming Guide-only tests and published runtime smoke.

Quest/Hideout/Items/Ammo/Map/MiniMap and independent Scanner behavior are preserved.

Legacy `farming-guide.json` is no longer read/written and is not automatically deleted.

## Validation coverage

The v1.17.0 public stable evidence remains valid for that immutable release. On PR #290, the removal implementation before the v1.17.1 version/document pass already passed Windows Release build, **485/485 deterministic tests**, self-contained publish, actual Product UI / Map / Scanner smoke, graceful shutdown, package verification, Shutdown Race and Documentation Consistency. Final v1.17.1 CI will be rerun after this documentation/version update.

## Canonical references

- `docs/.release-v1.17.0-status.json`
- `docs/RELEASE_NOTES_V1.17.0.md`
- `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`
- `docs/RELEASE_NOTES_V1.17.1.md`
- `docs/ACTIVE_WORK.md`
- `docs/PROJECT_STATE.json`
- `docs/STATE.md`

## External validation still pending

Automated implementation and release validation is complete. Separate actual-PC/Tarkov real-play validation remains `PENDING`; it does not alter the verified public v1.17.0 release identity or make the release incomplete.
