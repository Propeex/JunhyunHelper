# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-04 KST**  
상태: **v1.17.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.17.1
exact product source/tag target:
4ad1f76ed7c2469e60d0822b229fe03f83c75816
validated PR head:
edd6fa6f5a2edc9d52be84bf1625266d5ad6abec
merge PR: #290
PR CI / Shutdown / Docs:
33826796756 / 33826796665 / 33826796667 — SUCCESS
exact-main CI / Shutdown / Docs:
33827008615 / 33827008595 / 33827008638 — SUCCESS
Release workflow:
33827205735 — SUCCESS
release id: 382428841
published UTC: 2026-09-04T01:49:57Z
485 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 543627042
bytes: 80,573,737
SHA-256: fad73f3987c04cae73c5a473ccbce6c3a70ff8ca22da04a95a942e66ebea3b6c

SHA256SUMS.txt
asset id: 543627044
bytes: 86
asset SHA-256: d665b07efa2d3e402937701f903d1eb5da8001feab0b54bcb2a9d8a93e46f9b1
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9920376580
bytes: 241,651,630
SHA-256: 94cb4670b2889c42efaeaa50874b8bb0a186c3849f09a814184b82609bb2ad22
```

## v1.17.1 product change

Farming Guide is completely removed from the current product.

Removed:

- main Farming Guide navigation/page;
- loadout/inventory editor and presets;
- raid session/advisor, packing/repacking and loot optimization;
- locks, reserved cells, weight and Farming Guide quantity flows;
- Scanner Farming Guide bridge, accept hotkey/settings, Mini Scanner instruction/quantity integration;
- Farming Guide-specific Core/Desktop/Infrastructure implementation and persistence;
- Farming Guide-only Game Content metadata/import contracts and dedicated tests/smokes.

Preserved:

- Quest / Hideout / Needed Items;
- Items;
- Ammo;
- Map / MiniMap;
- Scanner recognition, catalog, search, ordinary Mini Scanner fields, correction, Ground Truth and diagnostics;
- content/program update safety.

Legacy `farming-guide.json` is inert: current product does not read/write it and does not automatically delete it.

## Validation coverage

v1.17.1 exact product source passed:

- Windows Release build;
- **485/485 deterministic tests**;
- win-x64 self-contained publish;
- ProductVersion/FIRST_RUN identity;
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
- `docs/.release-v1.17.1-status.json`
- `docs/RELEASE_NOTES_V1.17.1.md`
- `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`
- `docs/STATE.md`

## External validation

Actual Tarkov play validation on the user's own PC remains separately recorded as `PENDING`. Automated implementation/release validation is complete and v1.17.1 is the current public stable release.
