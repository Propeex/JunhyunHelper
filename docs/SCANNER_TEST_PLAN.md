# Scanner Current Test Plan

기준일: 2026-08-25
상태: **v1.7.0 PUBLIC RELEASE VERIFIED / LIVE GROUND TRUTH MAINTENANCE**

이 문서는 deterministic release gate와 실제 Tarkov 환경 calibration을 분리한다. Reviewed evidence 없이 geometry/OCR/visual confidence threshold나 candidate cap을 조정하지 않는다.

## 1. Current verified stable

```text
Desktop target: 1.7.0
automated suite: 348 tests
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
stable user package: Junhyun-Helper.zip
stable archive root: 준현 헬퍼/
exact release source: 56e12342e3490fd0defa5f327a03d20d4f32b3a6
public SHA-256: 1c640c80bf6113176b885a47e19478666e27dbf584f872d1a8396886334f3418
public proof run: 32745399476
```

v1.7.0 release gate는 build/test/publish/rendered Product UI/Scanner/Map smoke, graceful shutdown, package checksum/layout, exact source tag, anonymous public redownload, public ProductVersion/FIRST_RUN, public downloaded product smoke까지 모두 통과했다.

이후 Scanner recognition 변경은 reviewed live Ground Truth replay에서 regression=0을 확인하는 별도 calibration gate를 사용한다.

## 2. Release-blocking gate

### Build / deterministic regression

1. exact candidate source fixed
2. Windows Release build
3. full automated tests — 0 failed / 0 skipped
4. Scanner structural proposal regression
5. inspect-header semantic lock regression
6. incomplete lock fail closed
7. title ROI ownership regression
8. raw/substituted/normalized OCR evidence separation
9. current-catalog character/symbol policy
10. official catalog matcher
11. bounded unknown/edit recovery safety
12. visual recovery fail-soft safety
13. font source/cache generation consistency
14. bounded visual caches
15. market/dimension/RequiredTotal mapping
16. catalog load/refresh GameMode ordering
17. one-shot/profile/GameMode lifecycle
18. hotkey migration/duplicate prevention
19. user OCR substitution single-pass/default-empty
20. title continuity signature
21. Mini Scanner inventory coalescing/stale-result
22. Ground Truth candidate/manual/none contracts
23. reviewed-GT retention protection
24. Scanner settings schema-v6 migration/order contract
25. saved Case reopen/re-edit fail-closed contract
26. item search local-data/no-network contract where testable

### Publish / product smoke

27. Windows x64 self-contained single-file publish
28. exact ProductVersion / FIRST_RUN identity
29. publish-root / PDB / nested-archive / forbidden dependency audit
30. actual published EXE startup
31. Scanner current normal surface smoke
32. settings schema-v6 / Mini Scanner fixed identity + ordered fields smoke
33. Main Map / Factory / MiniMap smoke
34. graceful close / process termination
35. clean portable root
36. run `packaging/New-ReleasePackage.ps1`
37. stable `Junhyun-Helper.zip` exists
38. every ZIP entry is under `준현 헬퍼/`
39. required `준현 헬퍼/준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets/tarkov_data.db`
40. release ZIP SHA-256 recorded

### Public release verification

41. exact source tag `v1.7.0`
42. stable/latest release metadata points to exact source
43. public asset name exactly `Junhyun-Helper.zip`
44. checksum asset verified
45. fresh anonymous/public ZIP redownload
46. public hash/size/layout verification
47. public ProductVersion/FIRST_RUN verification
48. public-downloaded EXE Product UI/Map/Scanner smoke
49. graceful shutdown / clean extracted product root
50. durable `docs/.release-v1.7.0-status.json`
51. temporary one-shot release/verifier workflows cleaned up

## 3. Immutable threshold / candidate budget

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
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
- bounded frame-left search-icon lane
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

## 9. Exact same-cycle OCR reuse

Reuse requires:

```text
same active scan cycle
same normal/deep class
same width/height/BPP
same exact pixel SHA-256
```

- exact same bitmap same cycle → reuse allowed
- one pixel difference → no reuse
- normal/deep cache separate
- cycle change → invalidate
- late disposed-cycle result cannot pollute new cycle
- cross-frame reuse prohibited

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

Verify:

- MainWindow lifetime registration
- change/disable each command
- duplicate gesture blocked
- old settings migration preserves user choices
- overlapping one-shot invocation serialized
- requested continuous mode restored correctly
- current normal Scanner page **does not require a visible one-shot button**
- one-shot remains functionally available via hotkey
- no scan-time network
- one-shot cap 12 distinct from continuous cap 8

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
- reviewed status cannot be overwritten by automatic save
- raw/substituted/normalized OCR retained
- matcher top candidates retained
- mapped snapshot retained

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

## 16. Retention / logs

Reviewed Ground Truth automatic delete: **never**.

Automatic eligibility:

```text
retention == automatic_sample
AND review_status == unreviewed
```

Bounds:

- 30 days
- 300 automatic cases
- 512 MiB
- recent 2h protection

Verify:

- reviewed retained
- unknown/corrupt retained
- recent retained
- old excess automatic deleted within policy
- metadata re-read before delete
- cleanup failure does not change recognition

Logs remain bounded rotation. Current normal Scanner page does not require a log-delete button.

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

Normal surface must expose:

- Scanner ON/OFF
- Settings
- Advanced
- item search
- recognition log

Normal surface must not require/expose as primary controls:

- visible `1회 스캔` button
- visible `현재 결과 교정` button
- Display Test toggle
- catalog force-refresh
- regression/export developer buttons
- log-delete button

Settings window:

- three global hotkeys
- Mini Scanner five fields show/hide
- Mini Scanner order up/down
- icon/name fixed

Advanced window:

- Display Test
- current result correction
- correction dataset management

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

## 21. Package / version regression — v1.7.0

Published product identity:

- project version = 1.7.0
- ProductVersion starts `1.7.0`
- FIRST_RUN first line exactly `준현 헬퍼 v1.7.0 — Windows x64`
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

Release process must use exact final source.

Independent public verifier must not rely only on the build workspace artifact.

Fresh public checks:

- public latest release = v1.7.0
- exact tag resolves to chosen release source
- public `Junhyun-Helper.zip` redownload
- checksum redownload
- hash/size match
- archive root/path validation
- ProductVersion / FIRST_RUN
- public-downloaded EXE Product UI/Scanner/Mini Scanner/Map smoke
- graceful shutdown
- durable release status

Temporary one-shot release/verifier workflows, if created, are deleted afterward. Steady-state workflows are the permanent `ci.yml` + immutable-release `release.yml` pair.

## 23. Live Tarkov calibration after release

Recommended flow:

```text
item detail open
→ Scanner / one-shot hotkey
→ inspect result
→ correct representative result or failure
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

After v1.7.0 public verification:

```text
reviewed dataset
→ failure cluster
→ affected stage
→ narrow code change
→ full replay
→ REGRESSION=0
→ full build/tests/publish smoke
→ PATCH decision
```

Do not loosen threshold/candidate cap or add unrelated Scanner behavior without evidence.
