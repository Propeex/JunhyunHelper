# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-04 KST**  
상태: **v1.17.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.17.3
exact product source/tag target:
8ec677b1552f9deed55f98931c1df317e9bc4a4b
validated PR head:
230a5284f58f9d5eb8954c6042164bc5635fd35c
merge PR: #294
PR CI / Shutdown / Docs:
33846545486 / 33846545485 / 33846545484 — SUCCESS
exact-main CI / Shutdown / Docs:
33846852935 / 33846852933 / 33846852922 — SUCCESS
Release workflow:
33847077606 — SUCCESS
release id: 382534812
published UTC: 2026-09-04T07:04:53Z
503 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 543938413
bytes: 80,560,157
SHA-256:
1384f2d42b843617ed61f90d4b2b0c5aa46bc616fd54e808cafabef2eb24f1f7

SHA256SUMS.txt
asset id: 543938412
bytes: 86
asset SHA-256:
4944f6e04b6ae191272db805dd8b60c8ef82fd6d7c0e4f4629e53d41755f5b0a
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9926904439
bytes: 241,611,421
SHA-256:
ce1946f12f8da5de755ac91696f2f1ed1b137bf76da5a32b198c36c0228e12a3
```

## v1.17.3 product change

v1.17.3 is a **Stability, Optimization and UI Finishing** PATCH.

No new user-facing feature was introduced.

The pass:

- reduced repeated canonical content/search work in Quest, Hideout, Items and Scanner;
- made page workspaces derive from one authoritative profile snapshot;
- added per-path image download/decode single-flight with weak decoded-image reuse;
- removed permanent 120ms Map Quest marker scale polling;
- serialized all product content-update entry points through one operation gate;
- expanded shutdown cancellation across MainWindow and updater async work;
- repaired optimistic Hideout/mutation presentation rollback boundaries;
- added keyboard focus visibility and audited WPF clipping/scrolling/virtualization.

Preserved contracts include current Quest/Hideout/Items/Ammo behavior, Scanner recognition/search/correction/Ground Truth/diagnostics, Map/MiniMap pinned donor integration, supported schema compatibility and user-owned state.

## Farming Guide status

Farming Guide remains completely removed as established in v1.17.1. Historical `farming-guide.json` remains inert user data.

## Validation coverage

v1.17.3 exact product source passed:

- Windows Release build;
- **503/503 deterministic tests**;
- win-x64 self-contained publish;
- ProductVersion `1.17.3+8ec677b1552f9deed55f98931c1df317e9bc4a4b`;
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
- `docs/.release-v1.17.3-status.json`
- `docs/RELEASE_NOTES_V1.17.3.md`
- `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`
- `docs/STATE.md`

## External validation

Actual Tarkov play validation on the user's own PC remains separately recorded as `PENDING`. Automated implementation/release validation is complete and v1.17.3 is the current public stable release.
