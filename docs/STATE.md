# STATE — 현재 프로젝트 상태

> 복구 순서는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md`입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`이 기준입니다.

기준일: **2026-09-04 KST**  
상태: **v1.17.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 공개 제품 상태

```text
public stable: v1.17.0
exact product source/tag target:
8b0e1f8f46fa3822f4cff05b7be3223d40ad7435
validated PR head:
a01d61cd9957db94a7475734c1e8df66ce71f53d
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

Public release:

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

Release workflow `33749193376` checked out exact product source `8b0e1f8f46fa3822f4cff05b7be3223d40ad7435`, downloaded exact-main artifact `9890816795`, verified ProductVersion/FIRST_RUN identity, independently matched the release ZIP hash to `SHA256SUMS.txt`, and published stable `v1.17.0`. The release is `draft=false` and `prerelease=false`.


## 2. v1.17.1 Farming Guide removal

The user explicitly removed Farming Guide from the product.

Target version: **v1.17.1 PATCH**.

Current implementation branch/PR:

- branch: `product/remove-farming-guide-2026-09-04`
- PR: **#290** (draft while final validation is running)

Removed implementation:

- all first-party `Core/FarmingGuide` domain policies/models;
- all Desktop Farming Guide page/raid/editor/smoke code;
- Farming Guide persistence and Desktop service wiring;
- main navigation/section/busy state;
- Scanner Farming Guide bridge, accept hotkey/settings, Mini Scanner instruction and quantity-input state;
- Farming Guide-only GameItem extension metadata/import logic and dedicated tests.

Legacy `%LocalAppData%/JunhyunHelper/farming-guide.json` is not read or written by the target product and is not automatically deleted.

Historical Farming Guide decisions/releases remain history only. Current decision authority is `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`.

## 3. Preserved product boundaries

The removal must preserve:

- Quest / Hideout / Needed Items;
- Items inventory/progress behavior;
- Ammo comparison/pickup/favorites;
- Map / MiniMap;
- Scanner recognition, catalog, search, Mini Scanner ordinary fields, correction, Ground Truth and diagnostics;
- content/program update safety and user-owned state isolation.

## 4. Compatibility / data cleanup

- Scanner display settings remain schema v10. Older JSON may still contain removed Farming Guide fields/order entries; current deserialization/normalization ignores or drops them.
- Content snapshot schema remains v12. Farming Guide-only item metadata is no longer imported into the canonical GameItem model.
- No Farming Guide persistence schema is part of the current active product contract.
- Historical user Farming Guide JSON is inert and left untouched.

## 5. Pre-final validation evidence

PR #290 implementation head `7901724fa7007860dc1220a667a10911bdaf4a9a` passed:

- CI run `33821768569`;
- Shutdown Race run `33821768577`;
- Documentation Consistency run `33821768568`;
- Windows Release build;
- **485 passed / 0 failed / 0 skipped** deterministic tests;
- win-x64 self-contained publish;
- actual published EXE Product UI / full Map/Factory/MiniMap / Scanner smoke;
- graceful shutdown and clean portable-root checks;
- release package/checksum verification.

Final v1.17.1 CI is required again after version/document updates.

## 6. Version / release transition

Public stable remains immutable **v1.17.0** until v1.17.1 exact-main/release verification completes.

The source target is now Desktop **1.17.1**. `docs/PROJECT_STATE.json` intentionally keeps `publicStable` on v1.17.0 while `product.desktopVersion` tracks the in-progress v1.17.1 source.

## 7. Regression and published-runtime coverage

PR #288 and exact-main both passed the full Windows gate. Coverage includes:

- removal implementation regression coverage is represented by the 485-test surviving suite plus actual Product UI / Map / Scanner smoke;
- final v1.17.1 CI/exact-main/release evidence will replace this pre-final branch evidence.

Deterministic result on exact product source: **649 passed / 0 failed / 0 skipped**.

## 8. Schema / canonical references

```text
Desktop target: 1.17.1
Public stable: 1.17.0
Content write/read: v12 / v3-v12
user.db: v1
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
Map donor revision: d933792b6042a51cea38dc44b686a096fe30de67
```

Canonical evidence:

- `docs/PROJECT_STATE.json`
- `docs/.release-v1.17.0-status.json`
- `docs/RELEASE_NOTES_V1.17.0.md`
- `docs/CURRENT_STATE.md`
- `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`
- `docs/RELEASE_NOTES_V1.17.1.md`
- `docs/ACTIVE_WORK.md`

Automated implementation, merge, exact-main and public release validation are complete. Actual Tarkov play validation on the user's own environment remains a separate `PENDING` evidence field and does not make v1.17.0 development or release incomplete.
