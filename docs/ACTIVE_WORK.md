# ACTIVE WORK

Status: **NONE**

## Current task

None.

## Current stable baseline

```text
public stable: v1.16.1
exact product source/tag target: 7fb148434d22fac823d57d88021f9615081c47cd
merge PR: #276
release id: 380969416
```

## Latest completed work

**v1.16.1 product quality / resilience maintenance pass**

Completed scope:

- audited high-risk internal correctness, persistence, asynchronous profile/content state, lifecycle/shutdown, Scanner/Map settings, Farming Guide nested state, rendered WPF smoke, packaging and release paths;
- hardened `FarmingGuidePresetStore` so syntactically valid but semantically partial/null state is normalized while salvageable presets/equipment/stored items are preserved;
- added deterministic partial Farming Guide state load/save/reload regression coverage;
- hardened opportunistic startup content-schema refresh against stale async continuation across profile/game-mode changes by pinning and revalidating `ProfileId + GameMode`;
- added maintenance regression coverage for those profile identity guards;
- found and corrected the missing `docs/RELEASE_NOTES_V1.16.1.md` through the release-identity gate before publication;
- completed PR, exact-main, published EXE Product UI/Map/graceful-shutdown, Shutdown Race, package/checksum and public release verification;
- recorded exact public release/tag/asset/action evidence in canonical project memory.

The maintenance audit did not reproduce another user-visible UI defect strongly enough to justify speculative layout or behavior changes. Explicit minimum-main-window containment remains a possible future verification improvement if real evidence warrants it.

## Validation

```text
validated PR head:
7d7cf002aa4f1d61c891b340ff73c56781655d64
PR CI / Shutdown / Docs:
33589038565 / 33589038575 / 33589038576 — SUCCESS

exact product source:
7fb148434d22fac823d57d88021f9615081c47cd
exact-main CI / Shutdown / Docs:
33589274983 / 33589275133 / 33589275021 — SUCCESS
Release workflow: 33589497077 — SUCCESS
612 passed / 0 failed / 0 skipped

public release: v1.16.1
release id: 380969416
published UTC: 2026-09-02T04:06:31Z
Junhyun-Helper.zip:
80,717,818 bytes
8599645a2d0a38c6b74f4f79cab71120b26e378da254a98605610f1c7493b3c3
```

Public v1.16.1 release/tag/assets were verified against the exact product source. The Release workflow re-downloaded the exact-main artifact with digest verification and independently matched the checksum manifest to the actual release ZIP hash before publication.

Actual user-PC/Tarkov real-play validation remains separately `PENDING` and does not change the verified public stable identity.
