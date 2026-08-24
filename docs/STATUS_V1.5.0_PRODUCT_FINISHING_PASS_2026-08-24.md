# v1.5.0 Product Finishing Pass — Implementation Status

Date: 2026-08-24

Status: **IMPLEMENTATION IN PROGRESS**

Authoritative product decision:

- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`

Working branch / PR:

- Branch: `product/v1.5.0-usability-data-hardening`
- Draft PR: #172 — `Build v1.5.0 product finishing pass`
- This document records implementation state, not release completion. GitHub HEAD and CI remain authoritative if they are newer than the commit referenced by a conversation or handoff note.

## Safety contracts preserved

The v1.5 finishing work has not intentionally relaxed Scanner identity gates.

- Prefer misses over false positives.
- Detail geometry remains proposal evidence, not identity proof.
- Semantic header lock remains required.
- `HEADER_FRAME_LOCKED >= 0.68` remains unchanged.
- Magnifier + red close-X remain required.
- Structural floor remains `0.34`.
- Continuous candidate cap remains `8`.
- One-shot candidate cap remains `12`.
- Scan-time network access remains prohibited.
- Game memory reads, DLL injection and packet interception remain prohibited.
- Production OCR remains item-name-only; price/slots/needed values remain mapped data after Item ID resolution.

## Implementation status by approved scope

### 1. Scanner mapped market data — implemented and covered by tests

Scanner presentation/catalog paths now preserve mapped fields required after a confirmed Item ID, including:

- best trader sell price
- best trader name
- flea `avg24hPrice`
- slot count
- trader price per slot
- flea price per slot
- current required quantity from the active Items workspace

Relevant coverage includes `ScannerCatalogMarketShapeTests`.

### 2. Quest `확인 필요` live-data audit — implemented and repeatedly live-audited

Official audit record:

- `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`

`QuestTaskPoolVariableCompatibility` now includes GameMode in the audited compatibility contract and fails closed if the observed pool structure differs from the audited shape. Exact profile variables continue to take precedence over inferred values.

Temporary v1.5 live-audit workflow runs on current PR heads have continued to pass while this work proceeds.

### 3. Unified Game Data + Scanner catalog/market update — implemented

The normal top-level data update path now refreshes normal Tarkov content and Scanner catalog/market data together.

Failure policy is intentionally asymmetric:

- successful normal content is not rolled back solely because Scanner network refresh failed;
- an existing healthy Scanner cache is retained;
- partial Scanner refresh failure is surfaced as status rather than corrupting otherwise healthy content.

Scanner-only forced refresh remains available as an advanced recovery action rather than the normal user path.

### 4. User OCR substitutions — implemented and tested

Scanner settings support persistent OCR substitution rules with:

- add
- delete
- individual enable/disable
- reset

The processing contract is:

`raw OCR -> user substitution -> catalog sanitation/normalization -> matching`

Substitution is single-pass/non-recursive. Raw OCR remains separately preserved in diagnostic evidence and is not overwritten by the substituted text.

Relevant coverage includes `ScannerOcrSubstitutionTests`.

### 5. Candidate-based Ground Truth correction — implemented, product hardening ongoing

Correction now prefers selecting detector-generated candidates for:

- detail rectangle
- red close-X
- magnifier
- item-name ROI

Manual rectangle drawing remains the fallback when the correct detector candidate is absent, and `없음` can be recorded as Ground Truth.

Candidate evidence records ID/rank/score/geometry so proposal recall, ranking loss, header miss and ROI miss can be separated in future analysis.

### 6. Scanner latency telemetry + accuracy-preserving optimization — implemented, evidence collection ongoing

Per-scan latency telemetry now records:

- capture
- rectangle proposal
- semantic header validation
- normal OCR
- deep OCR
- visual recovery
- catalog matching/recovery
- presentation
- end-to-end

Continuous detector-only logging is sampled to avoid log churn; semantically active and one-shot cycles are retained.

The first optimization deliberately preserves recognition behavior:

- exact OCR input bitmaps can reuse a WinRT OCR result only within the same active scan cycle;
- the key includes pixel dimensions/format and SHA-256 of exact input pixels;
- normal/deep caches are separate;
- no result is reused across frames or scan cycles.

No Scanner acceptance threshold or candidate cap was lowered for performance.

### 7. Continuous result stabilization — implementation in progress

Existing stale-result behavior already clears on candidate loss/identity change rather than keeping an arbitrary time-based stale value.

v1.5 adds a conservative title identity signature based on the bright title-ink shape rather than exact raw BGRA bytes. Its purpose is to ignore harmless dark-background/trailing-ROI variation for an already verified item while still changing when visible glyph shape changes.

- It does not establish a new item identity.
- If the stable signature cannot be computed, the detector's existing exact signature remains the fallback.
- Core tests cover background/trailing-width stability, glyph-change separation and fail-closed no-ink behavior.

At the time this status document was created, integration into the common continuous/one-shot candidate observation path had been committed and the newest full CI run was still validating that HEAD.

### 8. Diagnostics/log/temp retention — implemented

Retention now distinguishes user-reviewed Ground Truth from automatic diagnostics.

Never auto-deleted by the new retention service:

- reviewed Ground Truth
- unknown/corrupt case metadata whose ownership/review state cannot be proven

Automatic unreviewed diagnostic cases are bounded by conservative maintenance limits:

- 30 days
- 300 automatic cases
- 512 MiB
- 2-hour recent-case safety window

`scanner.log` already has bounded rotation. `startup.log` now also rotates at 2 MiB with one backup.

### 9. Scanner primary vs advanced UI + quick correction — implemented, final smoke pending

Normal Scanner surface now emphasizes:

- Scanner ON/OFF
- 1회 스캔
- current result correction
- runtime status
- recent recognition history

User settings are grouped separately. Developer/recovery controls are grouped under `고급 / 진단`, including test mode, recognition image, regression, Ground Truth export/manage, forced catalog recovery and log clearing.

Mini Scanner supports direct `현재 결과 교정` from its context menu so a just-seen misread can enter correction without navigating through the Scanner tab first.

No diagnostic capability was removed.

### 10. Whole-product UI consistency audit — in progress

Reviewed so far:

- Main window/header
- Quest
- Hideout
- Items
- Ammo
- Scanner
- profile editor
- Scanner correction/settings dialogs

A concrete layout defect was identified: the former main-window minimum width of 900 px was below the structural minimum required by the header and Items two-pane layout. Product minimum width is now aligned to 1180 px; default width remains 1320 px.

Remaining audit work should focus on any unreviewed dialogs/map surfaces and final product smoke rather than redesigning already coherent screens.

### 11. Full automated validation / Windows publish / product smoke — continuously enforced, final gate pending

PR CI continuously runs:

- Desktop build
- Core tests
- Windows x64 publish
- Product UI / Map / Scanner startup smoke
- graceful shutdown smoke
- release-candidate artifact upload

An earlier PR failure was traced to a stale product-smoke assertion that expected Scanner settings schema 4 after OCR substitutions had legitimately advanced it to schema 5. The product smoke contract was corrected; a subsequent full run passed build, 293 Core tests, Windows publish, Product UI/Map/Scanner startup smoke and graceful shutdown.

New v1.5 changes after that green run continue to be gated by fresh CI runs. Final release requires a green run on the final release candidate HEAD.

### 12. Public v1.5.0 release + independent redownload verification — not started

Do not publish until:

- implementation scope is complete;
- final CI is green on the release candidate;
- actual current public release/tag state is re-checked directly from GitHub;
- version metadata is updated consistently;
- public ZIP is independently redownloaded and verified after release.

A handoff note that calls v1.4.3 the latest public release must not override newer GitHub release/tag evidence if GitHub has advanced since that note.

## Immediate next work

1. Finish and validate continuous title-identity stabilization.
2. Resolve any CI failure before adding further release work.
3. Complete the remaining UI/dialog/map consistency audit without adding unrelated features.
4. Remove incidental v1.5 implementation debt where practical.
5. Run the final full Windows publish/product smoke gate.
6. Re-check real GitHub releases/tags/version metadata, prepare v1.5.0, publish, independently redownload/verify, then perform housekeeping.
