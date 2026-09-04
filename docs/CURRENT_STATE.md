# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-04 KST**  
상태: **v1.17.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.17.2
exact product source/tag target:
73f0386a45818408c2a68530b90de7946ecaf1d1
validated PR head:
121d060db102eed0f4af241ef5f37c51164c6a04
merge PR: #292
PR CI / Shutdown / Docs:
33840328932 / 33840328963 / 33840329237 — SUCCESS
exact-main CI / Shutdown / Docs:
33840553320 / 33840553329 / 33840553303 — SUCCESS
Release workflow:
33840780902 — SUCCESS
release id: 382500195
published UTC: 2026-09-04T05:31:31Z
488 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 543847934
bytes: 80,554,487
SHA-256:
a64d202046505273964b0735976d71e382624c68f16699c6844b193599b43971

SHA256SUMS.txt
asset id: 543847933
bytes: 86
asset SHA-256:
a105826dcc518a58412a521b221a2e7842ccfb716662418981005b4d276505a0
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9924825161
bytes: 241,595,886
SHA-256:
864f971ebe799df881ac4d69318ae331cd3c4c4e783013836bceaacb33232ba4
```

## v1.17.2 product change

v1.17.2 is a **Product Purity Cleanup** PATCH.

No new user feature and no performance optimization were introduced.

Cleanup included:

- dead/unreachable first-party code and removed-feature remnants;
- hidden/superseded UI ownership and runtime repair/rebinding paths;
- obsolete Profile/Ammo/Scanner/search-clear lifecycle shims;
- retired Scanner standalone debug/settings/hotkey UI and dead Mini Scanner preview/position-edit paths;
- transitional updater/package compatibility no longer required by the current stable updater contract;
- stale current-looking documentation and duplicated canonical release/schema facts;
- regression tests that still required removed structures.

A real cleanup-indicator refresh regression discovered during the audit was fixed.

Preserved current product contracts include Quest, Hideout, Items, Ammo, Map/MiniMap, Scanner recognition/search/correction/Ground Truth/diagnostics, supported schema compatibility and the pinned Map donor integration.

## Farming Guide status

Farming Guide remains completely removed as established in v1.17.1. There is no current Farming Guide UI/runtime subsystem.

Historical `farming-guide.json` is inert and is not read, written or automatically deleted.

## Validation coverage

v1.17.2 exact product source passed:

- Windows Release build;
- **488/488 deterministic tests**;
- win-x64 self-contained publish;
- ProductVersion `1.17.2+73f0386a45818408c2a68530b90de7946ecaf1d1`;
- actual published EXE Product UI / full Map/Factory/MiniMap / Scanner smoke;
- graceful shutdown;
- active-async Shutdown Race;
- clean portable-root audit;
- release package/checksum validation;
- Documentation Consistency;
- exact-main artifact identity;
- Release workflow re-download/hash verification;
- public latest/tag/release/asset readback.

## Canonical references

- `docs/PROJECT_STATE.json`
- `docs/.release-v1.17.2-status.json`
- `docs/RELEASE_NOTES_V1.17.2.md`
- `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`
- `docs/STATE.md`

## External validation

Actual Tarkov play validation on the user's own PC remains separately recorded as `PENDING`. Automated implementation/release validation is complete and v1.17.2 is the current public stable release.
