# RELEASE — v1.15.1

Status: **PUBLIC VERIFIED**  
Published UTC: **2026-09-01T06:15:51Z**

## Immutable product identity

```text
version/tag: v1.15.1
exact product source/tag target:
821def285e2b4964242b50981f6ba6245e996057
release id: 380252024
draft: false
prerelease: false
latest stable: true
```

`refs/tags/v1.15.1`, the release target, and GitHub `/releases/latest` all resolve to the exact product source above. Later documentation-only main commits are not v1.15.1 product sources and must not replace its public assets.

## Candidate validation

Final non-draft PR:

```text
PR: #259
validated head: e78ca34c272ac40b8f7c6a4bfcefede59adb9d59
CI: 33476320371 — SUCCESS
Shutdown Race CI: 33476320367 — SUCCESS
Documentation Consistency: 33476320491 — SUCCESS
558 passed / 0 failed / 0 skipped
```

PR #258 carried the same implementation as a Draft. The connector's draft-to-ready GraphQL mutation failed because of a connector-side schema mismatch, so the exact validated branch/head was reopened as non-draft PR #259. One earlier PR smoke attempt hit a transient Factory-map visibility timeout while the process remained responsive; rerunning the same HEAD succeeded, and the new PR plus exact-main validation both passed the full runtime smoke.

## Exact-main validation

```text
exact product source:
821def285e2b4964242b50981f6ba6245e996057
CI: 33476586723 — SUCCESS
Shutdown Race CI: 33476586808 — SUCCESS
Documentation Consistency: 33476586819 — SUCCESS
558 passed / 0 failed / 0 skipped
```

The exact-main CI verified Release build, deterministic tests, self-contained Windows x64 publish, actual published EXE Product UI + Map + Scanner + graceful-shutdown smoke, release-package/checksum verification, and artifact upload. Shutdown Race independently verified closing the Main Window during active async product work.

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9788440065
archive bytes: 241,908,886
archive SHA-256:
e865fb395dcca353788495bbfb84f860129b39bdc6e89b51780d99db481592b8
```

## Release workflow

```text
run: 33476812315 — SUCCESS
source main commit: 821def285e2b4964242b50981f6ba6245e996057
verified artifact id: 9788440065
```

The Release workflow downloaded the exact-main artifact, verified the published executable/version and `FIRST_RUN_KO.txt`, verified the package SHA-256 against `SHA256SUMS.txt`, created the v1.15.1 release, published it as latest stable, and read the public release back successfully.

## Public assets

```text
Junhyun-Helper.zip
asset id: 539091025
bytes: 80,658,918
SHA-256:
80283d9dfc294d195d644ab12ac074b5d4698f4e500475d7435680ccb6e4fc0a

SHA256SUMS.txt
asset id: 539091026
bytes: 86
asset SHA-256:
906bde7d2c5a6e7234b3de1c21ba935c39522af84fe9f6fda352738457fb91d9
```

The release workflow reported the same ZIP size/hash before publication, and GitHub release asset metadata reports the same digest after publication.

## v1.15.1 scope

v1.15.1 is the first real-play correction pass for the v1.15.0 Farming Guide raid advisor.

- a new scan rejects an unaccepted previous recommendation without mutating raid state;
- Mini Scanner guidance is action-only and acceptance feedback is `반영 완료`;
- equipment, carrier, recursive attachment, and armor-plate targets participate in equip/replace-equip recommendations;
- accepted equip actions count toward raid-acquired Needed quantity;
- special-slot eligibility uses canonical `specialSlot` classification and compatible items occupy exactly one special slot;
- carrier locks protect the carrier itself without blocking ordinary automatic storage inside it;
- target locks expire with removed/replaced targets while reserved empty-cell locks remain independent;
- lock visuals survive rerenders and normal F lock toggles avoid full-page redraw;
- pistol/holster is displayed below eyewear and helper text is simplified;
- simulated T-scan presentation expires and cannot hide a newer real scan;
- raw assembly slot identifiers receive user-facing Korean labels;
- changed weapon/helmet imagery uses only an exact source-backed composed/preset match.

The safety boundary is unchanged: no Tarkov memory/process/packet reading, injection, input automation, or anti-cheat bypass. Recommendations commit only after explicit user acceptance.

## External validation

Automated release verification is complete. Separate real-environment evidence remains open:

- further user actual-PC/Tarkov play validation;
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis.
