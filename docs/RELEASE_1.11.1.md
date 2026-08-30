# RELEASE v1.11.1 — PUBLIC / VERIFIED

Date: **2026-08-30 KST**

## Release identity

```text
version: v1.11.1
status: PUBLIC STABLE / VERIFIED
exact product release source:
6314eaf866539747eadd69f8da4450bd8d5939e1
PR: #229
PR validated exact-head CI: 33302240850 — SUCCESS
exact-main CI: 33302387606 — SUCCESS
exact-main Shutdown Race CI: 33302387623 — SUCCESS
exact-main Documentation Consistency: 33302387611 — SUCCESS
release workflow: 33302514984 — SUCCESS
release id: 379226665
published UTC: 2026-08-30T08:49:26Z
460 passed / 0 failed / 0 skipped
```

Tag readback:

```text
refs/tags/v1.11.1
type: commit
target: 6314eaf866539747eadd69f8da4450bd8d5939e1
```

GitHub `/releases/latest`, the release target, the lightweight tag target, and the verified exact-main product source all resolve to v1.11.1 / `6314eaf866539747eadd69f8da4450bd8d5939e1`. The public release is `draft=false`, `prerelease=false`.

## Exact-main CI artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9729389953
archive bytes: 241,592,817
archive SHA-256:
770d89c56f39e379438702dbfb3f15ff0b681a1cd6794503fa1d45eece5061da
```

This artifact was produced by exact-main CI run `33302387606`. The Release workflow downloaded this verified artifact instead of rebuilding different binaries.

## Public assets

### Junhyun-Helper.zip

```text
asset id: 536370979
bytes: 80,553,167
SHA-256 / GitHub asset digest:
0480dca11f93472cee1396d5faae9362a8b04398a6c18bfd163dc84b9aef4e1b
```

### SHA256SUMS.txt

```text
asset id: 536370978
bytes: 86
SHA-256 / GitHub asset digest:
233dfca51bc7d280093da728cb76374e0f10b310e127f43139a5177d55a85b20
```

Before publication, the Release workflow verified that the `Junhyun-Helper.zip` SHA-256 recorded in `SHA256SUMS.txt` matched the exact package downloaded from exact-main CI. Public release metadata independently exposes the ZIP digest shown above.

## Product fixes

v1.11.1 is a PATCH maintenance release for three user-reported v1.11.0 usability gaps.

### Scanner ammo pickup setting

- The existing ammo pickup decision is now a normal Mini Scanner information field named `탄약 줍기 판단` in Scanner settings.
- It can be shown/hidden and moved in the Mini Scanner information order.
- Scanner display settings schema advances from v8 to v9.
- Existing v8 users migrate with the ammo pickup field visible by default, preserving the v1.11.0 rendered behavior.

### Items / Hideout search clear

- Items and Hideout search boxes now expose the same `×` clear interaction used by other product search surfaces.
- Clearing reuses the existing TextBox change/search path and restores focus to the search box.

### Correction-data save feedback

- A successful `교정 데이터 추가` hotkey save now shows `저장 완료` in Mini Scanner for a short transient interval.
- Current Mini Scanner item content is preserved when present.
- If Mini Scanner is closed, a short status-only card can appear so the save is still observable in-game.
- Evidence-only Saved Case and Ground Truth ownership are unchanged; no Ground Truth is generated or guessed by the hotkey.

## Maintenance audit

The v1.11.0 changes and major product contracts were reviewed again before release.

- ammo pickup evaluator examples, direct-purchase band, Trader LL, completed-quest unlock, barter/craft exclusion, equal-penetration tie, and no-purchase behavior remain unchanged
- Ammo Pack `containsItems` authoritative mapping and fail-closed ambiguity handling remain unchanged
- Hideout `attributes.foundInRaid` semantics and downstream Needed Items/cleanup contracts remain unchanged
- MiniMap first-open map replay, late Extract filter recovery, marker/name presentation repair, and bounded empty-layer recovery remain unchanged
- Scanner no-evidence correction status, evidence-only Saved Case behavior, duplicate explicit saves, and no automatic Ground Truth remain unchanged
- Scanner OCR/matcher/candidate acceptance and external-screen-pixels+OCR anti-cheat boundary remain unchanged

No additional clear product defect requiring a speculative refactor was found during this audit.

## Validation

The release source passed:

- 460 deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- v1.11.1 runtime smoke for `탄약 줍기 판단`, Items/Hideout search clear, and Mini Scanner `저장 완료`
- graceful shutdown smoke
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- public tag/release/assets/latest-stable readback

During RC validation, a stale startup smoke expectation still hardcoded Scanner settings schema v8. The product implementation was valid, but the stale gate correctly blocked startup/Shutdown Race. The smoke contract was updated to schema v9 and strengthened to verify ammo pickup order/visibility at runtime; all final PR and exact-main gates then passed.

User real-PC / live Tarkov play validation remains **PENDING** and is tracked separately from deterministic CI/release verification.

## Historical note

Any later documentation-only or maintenance commit is not the v1.11.1 product release source. The immutable historical product identity for this public release is `6314eaf866539747eadd69f8da4450bd8d5939e1`.
