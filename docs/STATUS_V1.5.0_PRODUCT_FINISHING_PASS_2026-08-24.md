# v1.5.0 Product Finishing Pass — Final Status

Date: 2026-08-24

Status: **PUBLIC RELEASE / VERIFIED**

## Release identity

```text
version: v1.5.0
exact source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
asset: Junhyun-Helper-v1.5.0-win-x64.zip
bytes: 80,422,292
SHA-256: 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
ProductVersion: 1.5.0+6de738959740d12e6ccb81b65e50006e463eb699
tests: 296 passed / 0 failed / 0 skipped
release run: 32691423654 — SUCCESS
public verifier: 32691641614 — SUCCESS
```

Durable evidence:

- `docs/.release-v1.5.0-status.json`
- `docs/RELEASE_1.5.0.md`
- `docs/RELEASE_NOTES_V1.5.0.md`

## Completed scope

| Stage | Result |
|---|---|
| Scanner mapped market data | COMPLETE |
| Quest `확인 필요` live-data audit | COMPLETE |
| Unified Game Data + Scanner catalog/market refresh | COMPLETE |
| User OCR substitutions | COMPLETE |
| Candidate-based Ground Truth correction + manual fallback | COMPLETE |
| Scanner latency telemetry | COMPLETE |
| Accuracy-preserving OCR duplicate-work optimization | COMPLETE |
| Continuous result stabilization | COMPLETE |
| Diagnostics/log retention | COMPLETE |
| Scanner normal/settings/advanced UI separation | COMPLETE |
| Mini Scanner quick current-result correction | COMPLETE |
| Whole-product UI consistency audit | COMPLETE |
| Windows build/test/publish/product smoke | COMPLETE |
| Public stable release | COMPLETE |
| Independent public redownload verification | COMPLETE |
| One-shot release workflow cleanup | COMPLETE |

## Scanner mapped-data contract

After Item ID is established, Scanner presentation resolves data from local trusted content rather than OCR.

- best non-flea trader RUB-equivalent sell price
- best trader name when available
- positive flea `avg24hPrice`
- positive `width × height` slot count
- trader/flea price per slot when both inputs are valid
- `NeededItems[itemId].RequiredTotal`

Missing market or dimension data clears only the affected presentation field and does not invalidate an otherwise healthy Item ID.

## Quest live-data audit

The 2026-08-24 live audit covered `regular`, `pve`, and `pvp-season` data. Task-pool compatibility is GameMode-aware and remains fail-closed when audited structure differs. Unknown requirements are not converted into guessed availability.

Reference: `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`.

## OCR substitutions

Scanner display settings schema is **v5**.

- default substitution list is empty
- exact user-owned substitutions are persistent
- substitution is applied once after raw OCR and before catalog normalization/matching
- recursive or chained rewriting is not performed
- raw OCR remains separately preserved as forensic evidence
- this feature does not create a product-wide automatic r/0/Korean substitution table

## Ground Truth correction

Correction is candidate-first:

1. detail rectangle candidate
2. close-X candidate
3. magnifier candidate
4. item-name ROI candidate
5. correct item/text
6. save

Manual rectangle selection and explicit `없음` remain fallback paths when detector proposals do not contain the truth. Candidate identity/rank/score/geometry is saved with reviewed Ground Truth.

## Performance and continuous stability

Stage telemetry records:

- capture
- rectangle proposal
- semantic header validation
- OCR normal/deep
- visual recovery
- catalog matching/recovery
- presentation
- end-to-end

Optimization is conservative: only an exactly identical OCR bitmap inside the same active scan cycle may reuse OCR output. There is no cross-frame OCR cache.

Continuous Scanner uses a stable title-ink identity signature to tolerate harmless dark-background pixel variation while keeping an already verified result. This signature never establishes Item identity. Different title evidence clears stale trusted results.

## Retention

Reviewed Ground Truth is never automatically removed.

Automatic unreviewed diagnostic samples are bounded by:

- maximum age: 30 days
- maximum automatic cases: 300
- maximum automatic bytes: 512 MiB
- recent-case safety window: 2 hours

Scanner and startup logs use bounded rotation.

## UI finishing

Scanner normal surface prioritizes:

- Scanner ON/OFF
- 1회 스캔
- 현재 결과 교정
- runtime status
- recent recognition history

Settings and developer/recovery functions are separated into `설정` and `고급 / 진단`. Mini Scanner offers right-click `현재 결과 교정`.

Whole-product UI review found a real Main-window structural clipping risk at the old 900 px minimum width; minimum width is now 1180 px. Map/MiniMap architecture and validated smoke contracts were preserved rather than unnecessarily redesigned.

## Safety contracts preserved

No v1.5.0 finishing work relaxed these contracts:

- prefer miss over false positive
- geometry is proposal evidence only
- `HEADER_FRAME_LOCKED >= 0.68`
- magnifier + red close-X required
- structural floor `0.34`
- continuous max 8 candidates
- one-shot max 12 candidates
- current official Korean Tarkov item catalog is identity authority
- production OCR field is item-name only
- mapped price/slot/needed fields are resolved after Item ID
- no scan-time network
- no game memory read
- no DLL injection
- no packet interception

## Validation

Final PR release-candidate CI run `32688080850` passed Release build, 296 tests, Windows x64 publish, package identity audit, rendered Product UI/Map/Scanner smoke, graceful shutdown, and artifact upload.

Release workflow run `32691423654` rebuilt exact source, repeated package/smoke verification, created the exact tag, redownloaded the draft asset, revalidated it, and published stable/latest.

Independent public verifier run `32691641614` anonymously redownloaded the public ZIP and checksum on a fresh Windows runner and verified hash, size, layout, ProductVersion, FIRST_RUN, Product UI/Map/Scanner smoke, and normal shutdown.

## Post-release development rule

v1.5.0 is the public baseline. Further Scanner tuning must be driven by reviewed real-world Ground Truth and must preserve existing normal cases with `REGRESSION=0`. Thresholds/candidate caps are not to be loosened without evidence.
