# Scanner Current Test Plan

기준일: 2026-08-26
상태: **v1.7.8 PUBLIC STABLE / FEATURE COMPLETE / MAINTENANCE ONLY**

이 문서는 deterministic release gate와 실제 Tarkov reviewed Ground Truth calibration을 분리한다. Reviewed evidence 없이 geometry/OCR/visual confidence threshold나 candidate cap을 조정하지 않는다.

## 1. Current verified stable

```text
Desktop target: 1.7.8
automated suite: 380 tests
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
stable user package: Junhyun-Helper.zip
stable archive root: 준현 헬퍼/
exact release source: 3ba9d99c43ad143dbc8329e7d29b1d01da335b06
main CI run: 32888653630
release workflow run: 32888935292
release id: 376650517
public bytes: 80,469,671
public SHA-256: 3716d2d3c6d3c9ce2f87c759aac74f6b56b483a09016339c0d8bb6d3bc67e730
```

v1.7.8 release gate는 build/test/publish/rendered Product UI/Scanner/Map smoke, graceful shutdown, package checksum/layout, exact source tag, stable release metadata와 public asset readback을 모두 통과했다.

이후 Scanner recognition 변경은 reviewed live Ground Truth가 있으면 replay에서 `REGRESSION=0`을 확인하는 별도 calibration gate를 사용한다.

## 2. Release-blocking gate

### Build / deterministic regression

1. exact candidate source fixed
2. Windows Release build
3. full automated tests — 0 failed / 0 skipped
4. Scanner structural proposal regression
5. inspect-header semantic lock regression
6. incomplete lock fail closed
7. title ROI ownership regression
8. v1.7.8 raid horizontal-bleed recovery positive/negative regression
9. raw/substituted/normalized OCR evidence separation
10. current-catalog character/symbol policy
11. official catalog matcher
12. bounded unknown/edit recovery safety
13. visual recovery fail-soft safety
14. font source/cache generation consistency
15. bounded visual caches
16. market/dimension/RequiredTotal mapping
17. catalog load/refresh GameMode ordering
18. one-shot/profile/GameMode lifecycle
19. Scanner/Map hotkey migration + duplicate prevention
20. user OCR substitution single-pass/default-empty
21. title continuity signature
22. Mini Scanner inventory coalescing/stale-result
23. Ground Truth candidate/manual/none contracts
24. reviewed-GT retention protection
25. no durable automatic Case creation during normal monitoring
26. legacy automatic Case cleanup fail-closed contract
27. Scanner settings schema-v6 migration/order contract
28. saved Case reopen/re-edit fail-closed contract
29. item search local-data/no-network contract where testable
30. Scanner main correction action exact-frame contract

### Publish / product smoke

31. Windows x64 self-contained single-file publish
32. exact ProductVersion / FIRST_RUN identity
33. publish-root / PDB / nested-archive / forbidden dependency audit
34. actual published EXE startup
35. Scanner current normal surface smoke
36. settings schema-v6 / Mini Scanner fixed identity + ordered fields smoke
37. Main Map / Factory / MiniMap smoke
38. graceful close / process termination
39. clean portable root
40. run `packaging/New-ReleasePackage.ps1`
41. stable `Junhyun-Helper.zip` exists
42. every ZIP entry is under `준현 헬퍼/`
43. required `준현 헬퍼/준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets/tarkov_data.db`
44. release ZIP SHA-256 recorded

### Public release verification

45. exact source tag points to chosen release source
46. stable/latest release metadata points to exact source
47. public asset name exactly `Junhyun-Helper.zip`
48. checksum asset present/verified
49. public hash/size/layout readback
50. ProductVersion/FIRST_RUN correspond to target version
51. durable release record/status written after public readback

## 3. Immutable threshold / candidate budget

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

Do not change because:

- recognition rate looks low
- one or two screenshots miss
- CPU reduction is desired
- fuzzy matching should be easier

Change requires reviewed Ground Truth replay + false-positive impact evidence.

## 4. Structural proposal regression

Maintain:

- RED-X component proposals
- rectangle/edge fallback
- structural floor 0.34
- continuous 8 / one-shot 12
- aspect prior only weak ranking hint
- high IoU not sufficient for duplicate removal
- different-edge overlapping proposals preserved
- near-identical edge jitter deduped
- geometry alone never establishes Item ID

Representative cases:

- cropped inspect
- full-screen inspect
- tall/large detail
- strong inner rectangle coexistence
- high-IoU different-edge proposal
- no-RED-X fallback proposal
- uniform-frame fail closed

## 5. Inspect-header semantic lock regression

Required evidence:

- right red close-X
- neutral inspect-header/frame
- bounded magnifier/search lane
- magnifier ring/hollow/handle morphology
- dark title field
- title text evidence

Assertions:

- no absolute screen-center dependency
- live measured geometry regressions remain valid
- decoy ring/glyph cannot replace magnifier
- fragmented first glyph cannot own title ROI
- non-`HEADER_FRAME_LOCKED` candidate cannot enter production OCR identity
- runtime score >= 0.68 required
- missing magnifier/close/title evidence → fail closed
- contained-subpanel fallback re-runs same semantic gate

### v1.7.8 raid ownership regression

Reviewed raid failures showed surrounding neutral horizontal UI lines can visually join the inspect header and push the old header-left ownership 47–132px left.

Current recovery priority:

```text
primary header lock
→ live Ground Truth recovery
→ raid ownership recovery
→ contained-subpanel recovery
→ fail closed
```

Raid recovery may enter only when:

```text
candidate reason = RED_X_CANDIDATE
structural score >= 0.90
```

It must independently satisfy:

```text
close-X template >= 0.40
close relation evidence >= 0.60
candidate-owned neutral header >= 0.74
magnifier template >= 0.54
magnifier relation evidence >= 0.66
dark title field >= 0.58
title text evidence >= 0.22
final HEADER_FRAME_LOCKED >= 0.68
```

Regression smoke must include:

- positive: inspect header visually joined to a much longer outside neutral line, valid red close + magnifier + title evidence → lock succeeds
- negative: same geometry without red close-X → recovery fails closed

Coarse detail geometry remains ownership proposal only, not Item identity proof.

## 6. OCR / sanitation / matcher

- Windows ko-KR primary
- adaptive scaling/preprocessing
- normal + deep variants
- raw OCR retained
- character/symbol policy from current official catalog
- impossible glyph not globally forced to a specific character
- exact official name first
- conservative fuzzy confidence + top1/top2 margin
- ambiguity fail closed
- bounded unique unknown/edit recovery only

Unknown `?` is positional uncertainty, not a forced character replacement.

## 7. User OCR substitution / schema v6

Data flow:

```text
raw OCR
→ enabled user substitutions
→ sanitation / normalization
→ matcher
```

Regression:

- default list empty
- exact user rules
- ordered single-pass
- no recursive/chained reprocessing
- raw OCR immutable forensic evidence
- substituted text separate
- malformed/empty rule normalization
- v5→v6 migration preserves existing user substitutions
- user rule never becomes automatic product-wide alias table

## 8. Tarkov-font visual regression

- game font binary excluded from public package
- installed Tarkov resources/assets read-only source
- generation change invalidates stale rendered cache
- partial/corrupt cache fails only visual path
- candidate universe = current official full-item catalog
- visual top1 + margin required
- unavailable/ambiguous visual path does not discard healthy OCR evidence
- visual caches bounded

## 9. Exact same-cycle OCR / visual reuse

Reuse may occur only inside the same current scan-cycle proof boundary.

Relevant exact keys include current image dimensions/pixels and the associated OCR evidence required by the implementation.

- exact same evidence same cycle → deterministic result reuse allowed
- one pixel difference → no exact-image reuse
- cycle change → invalidate
- late disposed-cycle result cannot pollute new cycle
- cross-frame/cross-cycle Item identity reuse prohibited

v1.7.6 problem-PC performance baseline must remain protected.

## 10. Title continuity stabilization

- same glyph shape + harmless dark background variation may share signature
- unused trailing ROI width variation may be ignored
- visible glyph change → different signature
- no visible title ink → fail closed
- signature cannot establish Item ID
- new geometry/title identity clears old trusted result

## 11. Catalog / mapped data

Identity health remains separate from market coverage.

Catalog ordering:

- `LoadCacheAsync` / `RefreshAsync` same operation gate
- older GameMode writer cannot overwrite newer final state

Mapped presentation:

```text
best trader price = valid non-flea RUB max
best trader name = selected trusted source
flea average = positive avg24hPrice
slots = positive width × height
trader price/slot = valid trader price + slots
flea price/slot = valid flea price + slots
needed = NeededItems[itemId].RequiredTotal
```

Missing market/dimension data only clears affected field.

## 12. Unified Game Data refresh

- general Game Content update runs
- current GameMode Scanner catalog/market refresh runs
- both success → healthy combined state
- Scanner-only refresh failure cannot rollback general success
- healthy Scanner cache preserved where safe
- partial failure visible
- normal Scanner page does not require a force-refresh action

## 13. One-shot / hotkey regression

Scanner settings schema v6.

Defaults:

- Ctrl+Shift+F10 — in-game one-shot
- Ctrl+Shift+F11 — test one-shot
- Ctrl+Shift+F12 — Scanner ON/OFF

Configured gesture contract:

```text
bare primary key
or primary key + optional Ctrl/Alt/Shift combination
```

Windows key modifier is unsupported.

Verify:

- MainWindow lifetime registration
- bare key parse/register supported
- Ctrl/Alt/Shift combinations supported
- Windows modifier rejected
- change/disable each command
- exact duplicate gesture blocked/resolved by product contract
- old settings migration preserves user choices
- overlapping one-shot invocation serialized
- requested continuous mode restored correctly
- current normal Scanner page does not require a visible one-shot button
- one-shot remains functionally available via hotkey
- no scan-time network
- one-shot cap 12 distinct from continuous cap 8

Map regression additionally keeps bare NumPad0~5 reserved for direct floor selection while modifier+NumPad remains configurable.

## 14. Ground Truth correction regression — current

Candidate-first sequence:

1. detail
2. close-X
3. magnifier
4. item-name ROI
5. correct item/text
6. save

Verify:

- candidate ID/rank/score/geometry stored
- image candidate boxes directly clickable
- selected state visually distinguishable
- explicit `없음`
- manual rectangle fallback
- no candidate does not block correction
- reviewed status cannot be overwritten by automatic/background state
- raw/substituted/normalized OCR retained
- matcher top candidates retained
- mapped snapshot retained

### Current-frame ownership

- main `현재 결과 교정` opens latest exact in-memory Scanner frame
- if no exact current frame exists, no unrelated frame substitution
- activity correction may use a durable saved Case with matching Case ID
- stale activity with neither durable Case nor matching current frame is not silently correctable
- durable save occurs only after explicit user save

### Image scale contract

- oversized source image fits correction viewport
- displayed scale does not change saved ROI meaning
- click/hit-test maps to original image coordinate
- manual rectangle maps to original image coordinate
- original full.png dimensions remain authoritative

### Saved Case re-edit

- existing Case can reopen from dataset manager
- `case.json` + `full.png` + `candidate_selection.json` restore where valid
- existing GT item/text restored
- existing selection restored where valid
- save keeps same Case ID
- re-save updates reviewed GT rather than creating unrelated truth
- corrupt/missing source fails closed and preserves original Case

## 15. Full-pipeline replay

Reviewed `full.png`:

```text
full.png
→ proposals
→ semantic header
→ title ROI
→ OCR/deep/substitution/visual
→ current catalog match
→ final Item ID
```

Result:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

Past correct Case failing now = REGRESSION even if average improves.

Mapped price changes can be freshness changes and are not automatically identity regressions.

User reviewed image data is not committed to the public repository merely to make CI deterministic. Procedural smoke and local reviewed replay are distinct evidence layers.

## 16. Retention / logs

Reviewed Ground Truth automatic delete: **never**.

### New runtime monitoring

Normal monitoring must not create durable automatic image Cases from:

- no detail window
- header failure
- OCR/matcher failure
- ambiguity
- repeated stationary failure

Latest exact diagnostic frame may remain transiently in memory for current correction.

### Legacy cleanup eligibility

```text
retention == automatic_sample
AND review_status == unreviewed
AND recent-write safety window >= 5 minutes
AND pre-delete metadata/state re-read confirms unchanged state
```

Verify:

- reviewed/manual retained
- unknown/corrupt/unreadable retained
- state-changed retained
- recent-writing Case retained
- qualifying old legacy automatic Case can be deleted
- metadata re-read before delete
- cleanup failure does not change recognition
- cleanup runs outside recognition hot path

The obsolete 30-day / 300-case / 512MiB automatic persistence policy is not the current normal-monitoring contract.

Logs remain bounded rotation. User activity equivalent failures may collapse for 30 seconds. Current normal Scanner page does not require a log-delete button.

## 17. Mini Scanner regression — schema v6

Window:

- matched Item data only
- Topmost / no activate / no taskbar
- full-card drag
- inventory probe single-active + latest coalesce
- stale context/epoch reject
- uncertain inventory context → hidden
- title/inventory OCR serialized
- scan-time icon/network none

Presentation:

- icon always visible
- official item name always visible
- five optional rows appear in persisted configured order
- hidden rows remain hidden
- best trader row can include trader name + price
- settings v5 migration produces valid canonical order

## 18. Scanner Product UI regression — current

Normal top surface must expose in this order:

1. Scanner ON/OFF
2. Settings
3. Advanced
4. Current Result Correction

Korean product labels are:

```text
스캐너 ON/OFF / 설정 / 고급 / 현재 결과 교정
```

Normal surface also provides:

- item search
- recognition log

Normal surface must not require/expose as primary controls:

- visible `1회 스캔` button
- Display Test toggle
- catalog force-refresh
- regression/developer export buttons
- log-delete button

Settings window:

- three Scanner global hotkeys
- bare/modifier gesture contract
- Mini Scanner five fields show/hide
- Mini Scanner order up/down
- icon/name fixed

Advanced window:

- Display Test / test Scanner
- correction dataset management
- Scanner performance diagnostic export

`현재 결과 교정` must not be duplicated in Advanced.

## 19. Scanner item-search regression

- local/memory full-item catalog only
- empty query closes results
- search result contains official name and local cached icon when available
- choosing result closes popup and does not reopen due TextChanged
- selected item details use existing `ScannerItemPresentationService`
- Wiki URL requires valid http/https
- missing icon/wiki/market field fails only that presentation field
- no search-time network request
- current needed semantics remain RequiredTotal

## 20. Latency telemetry

Measured stages:

- capture
- rectangle-proposal
- semantic-header
- ocr-normal
- ocr-deep
- visual-recovery
- catalog-matching
- presentation
- end-to-end

Telemetry cannot alter acceptance threshold.

Performance support bundle should remain useful without automatically embedding user Ground Truth image/dataset.

## 21. Package / version regression — current

Published product identity for the candidate version must agree across:

- Desktop project version
- EXE ProductVersion
- `FIRST_RUN_KO.txt` first line
- Git tag
- GitHub Release metadata

Current stable v1.7.8 identity:

```text
project version = 1.7.8
FIRST_RUN first line = 준현 헬퍼 v1.7.8 — Windows x64
exact public source = 3ba9d99c43ad143dbc8329e7d29b1d01da335b06
public asset SHA-256 = 3716d2d3c6d3c9ce2f87c759aac74f6b56b483a09016339c0d8bb6d3bc67e730
```

Package must have:

- no PDB
- no unexpected root DLL/archive
- no forbidden legacy dependency
- clean portable publish root

Stable user ZIP:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

Verify every archive entry starts `준현 헬퍼/` and no nested version folder is introduced.

## 22. Public verification protocol

Release process must use exact final main source and successful exact-main CI artifact.

Fresh public checks:

- public latest release = intended version
- exact tag target = chosen release source
- release is draft=false / prerelease=false
- public `Junhyun-Helper.zip` exists
- checksum asset exists
- GitHub asset digest/hash and size match recorded proof
- ProductVersion / FIRST_RUN correspond to intended version
- durable release status and canonical STATE updated after public readback

A later docs-only commit must never be rewritten as the product release source for an already-published stable.

## 23. Live Tarkov calibration after release

Recommended flow:

```text
item detail open
→ Scanner / one-shot hotkey
→ inspect result
→ correct representative result or failure when useful
→ reviewed GT save
→ failure stage classify
→ dataset replay
```

Failure stages:

1. capture/window
2. structural proposal recall/ranking
3. close-X semantic
4. magnifier semantic
5. header lock
6. item-name ROI
7. raw OCR
8. user substitution
9. catalog sanitation/matcher
10. visual recovery
11. Item ID
12. mapped presentation
13. overlay/stale-state

Wrong identity has higher priority than miss.

## 24. Next development rule

After v1.7.8 public verification:

```text
new user evidence
→ reviewed dataset/support evidence
→ failure cluster
→ affected stage
→ narrow code change
→ full replay where available
→ REGRESSION=0
→ full build/tests/publish smoke/package gate
→ PATCH decision
```

Do not loosen threshold/candidate cap or add unrelated Scanner behavior without evidence.
