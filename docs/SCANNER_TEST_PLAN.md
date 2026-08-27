# Scanner Current Test Plan

기준일: 2026-08-27
상태: **v1.7.14 PUBLIC STABLE / FEATURE COMPLETE / MAINTENANCE ONLY**

이 문서는 Scanner의 현재 release-blocking deterministic gate와 실제 Tarkov reviewed Ground Truth calibration을 분리한다. Reviewed evidence 없이 geometry/OCR/matcher/visual confidence threshold, candidate cap 또는 pacing을 조정하지 않는다.

## 1. Current verified stable

```text
Desktop target: 1.7.14
automated suite at v1.7.14 release: 407 tests
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
stable package: Junhyun-Helper.zip
exact product release source/tag target: 0a51375de36cd13047216006c2c0311728b1bd89
main CI: 33060827905 — SUCCESS
Release workflow: 33061059154 — SUCCESS
release id: 377720327
public bytes: 80,488,363
public SHA-256: 341ac502d2ace563ab2e7c8d7091a8e796cf87e7d1f5961edf869feab106e2fd
```

v1.7.14 release gate는 Release build, 407 deterministic tests, Windows x64 publish, exact ProductVersion/FIRST_RUN, actual published EXE Product UI/Scanner/Map/Factory/MiniMap smoke, graceful shutdown, package layout/checksum, exact source tag 및 public asset/tag readback을 통과했다.

v1.7.14는 Scanner identity recognition을 변경하지 않은 UI consistency PATCH다. 설정/hotkey/advanced overlay와 검색 clear interaction만 바뀌었으며 v1.7.6~v1.7.10의 performance/environment recognition 기준은 유지한다.

현재 main 또는 이후 docs-only HEAD와 immutable v1.7.14 product source를 혼동하지 않는다. 정확한 현재 운영 상태는 `docs/STATE.md`를 사용한다.

## 2. Immutable threshold / candidate budget

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

다음 이유만으로 변경하지 않는다.

- recognition rate가 낮아 보임
- 한두 screenshot이 miss함
- CPU를 더 줄이고 싶음
- fuzzy matching을 쉽게 만들고 싶음
- 한 사용자 환경에 더 잘 맞추고 싶음

변경은 user-reviewed Ground Truth replay와 false-positive 영향 evidence를 요구한다.

## 3. Release-blocking deterministic gate

### 3.1 Build / core regression

후보 release source를 먼저 고정한 뒤 다음을 모두 통과해야 한다.

1. Windows Release build
2. full automated tests — 0 failed / 0 skipped
3. Scanner structural proposal regression
4. inspect-header semantic lock regression
5. incomplete semantic lock fail closed
6. item-title ROI ownership regression
7. v1.7.8 raid horizontal-bleed recovery positive/negative regression
8. raw/substituted/normalized OCR evidence separation
9. current-catalog character/symbol sanitation
10. exact official name priority
11. conservative matcher confidence + top1/top2 ambiguity fail closed
12. bounded unknown/edit recovery safety
13. optional visual corroboration fail-soft safety
14. same-cycle exact-pixel OCR/visual reuse boundedness
15. no cross-frame identity proof cache
16. market/dimension/`RemainingTotal` same-ID presentation mapping
17. needed-source same-ID presentation mapping
18. catalog load/refresh GameMode ordering
19. one-shot/profile/GameMode lifecycle
20. Scanner/Map hotkey migration and duplicate prevention
21. user OCR substitution single-pass/default-empty
22. title continuity signature
23. Ground Truth candidate/manual/none contracts
24. reviewed Ground Truth retention protection
25. no durable automatic Case during normal monitoring
26. legacy automatic Case cleanup fail closed
27. Scanner settings schema-v6 migration/order
28. saved Case reopen/re-edit fail closed
29. item search local-data/no-network contract where testable
30. current correction exact-frame authority
31. Mini Scanner confirmed-item presentation authority
32. hidden real-overlay Tarkov foreground guard
33. v1.7.10 reference SDR compatibility
34. lifted/washed HDR→SDR-like normalization
35. compressed-contrast normalization
36. low-contrast gamma/rendering normalization
37. 1080p/1440p/4K proportional title-raster matrix
38. flat/no-contrast negative input
39. support bundle privacy exclusions

### 3.2 v1.7.13 presentation regressions retained

- Ammo detail initial collapsed → expand → collapse round-trip remains in actual Product UI smoke.
- Items Quest/Hideout purpose selector does not reappear as an active product filter.
- Scanner searched needed-item source derives from existing `ItemsWorkspace.Plan.NeededItems[itemId].Sources` rather than reconstructing Quest/Hideout requirements.
- Scanner recognition threshold/candidate/matcher/visual policy is untouched.

### 3.3 v1.7.14 UI consistency regressions

`V1714UiConsistencyContractTests` protects:

- Ammo `즐겨찾기 선택` / `표시 열` true-toggle behavior
- MainWindow shared overlay owner for Window-backed and existing UIElement surfaces
- Scanner Settings owns hotkey configuration
- Scanner Advanced uses shared overlay and has no content-local close button
- old `ScannerHotkeySettingsWindow.xaml/.cs` does not reappear
- Map MiniMap launcher chrome cleanup
- Map marker launcher/collapsed/expanded panel product contract
- Map/MiniMap Settings shared-overlay route
- Profile editor product card presentation
- Quest/Hideout/Items/Ammo/Scanner in-field search clear behavior

Actual Product UI smoke complements source-level tests by constructing the real published WPF surface. Scanner Advanced is tested while hosted in the actual MainWindow shared overlay, not as a standalone Window.

## 4. Publish / actual product smoke

Release-blocking publish checks:

1. Windows x64 self-contained single-file publish
2. exact EXE ProductVersion matches project version
3. FIRST_RUN first line matches target version exactly
4. publish root contains only allowed product files/directories
5. no unexpected root DLL clutter
6. no PDB/debug symbols
7. no nested archive
8. forbidden legacy dependencies absent
9. actual published EXE startup
10. Scanner normal surface render
11. Scanner Advanced shared-overlay render/dismiss contract
12. Mini Scanner render / Topmost / confirmed-item presentation policy
13. Main Map smoke
14. Factory floor/marker regression smoke
15. MiniMap smoke
16. normal MainWindow close
17. process terminates within graceful-shutdown budget
18. no runtime `Logs` folder beside portable executable
19. release package creation
20. every ZIP entry lives under `준현 헬퍼/`
21. required EXE / FIRST_RUN / Tarkov assets present
22. ZIP SHA-256 recorded and checksum manifest matches

## 5. Public release verification

After exact main source passes the full gate:

1. release workflow downloads the exact verified main-CI artifact
2. artifact archive digest is verified by Actions
3. ProductVersion is re-read from downloaded published EXE
4. FIRST_RUN identity is re-read
5. `Junhyun-Helper.zip` hash matches `SHA256SUMS.txt`
6. stable release tag is created/used for the exact main SHA
7. release is `draft=false`, `prerelease=false`
8. required `Junhyun-Helper.zip` and `SHA256SUMS.txt` assets exist
9. `/releases/latest` points to the intended version
10. tag ref points to exact product source
11. public asset size/digest matches the main-CI package
12. durable `RELEASE_*` and status record is written after readback

Published stable releases are immutable. Later docs-only main commits may produce different bytes due to ProductVersion commit metadata; Release workflow must not overwrite the already published version.

## 6. Structural proposal regression

Maintain:

- RED-X component proposals
- rectangle/edge fallback
- structural floor 0.34
- continuous 8 / one-shot 12
- aspect prior only weak ranking hint
- high IoU alone is not sufficient for duplicate removal
- different-edge overlapping proposals preserved
- near-identical edge jitter deduped
- geometry alone never establishes Item ID

Representative procedural cases:

- cropped inspect
- full-screen inspect
- tall/large detail
- strong inner rectangle coexistence
- high-IoU different-edge proposal
- no-RED-X fallback proposal
- uniform-frame fail closed

## 7. Inspect-header semantic lock regression

Required evidence includes:

- right red close-X
- neutral inspect header/frame
- bounded magnifier/search lane
- magnifier morphology
- dark title field
- title text evidence

Assertions:

- no absolute screen-center dependency
- non-`HEADER_FRAME_LOCKED` candidate cannot enter production OCR identity
- final runtime score >= 0.68 required
- missing close-X/magnifier/title evidence → fail closed
- contained-subpanel fallback re-runs the same semantic gate

### Raid ownership recovery

Recovery priority:

```text
primary header lock
→ live Ground Truth recovery
→ raid ownership recovery
→ contained-subpanel recovery
→ fail closed
```

Raid recovery entry remains strongly gated:

```text
candidate reason = RED_X_CANDIDATE
structural score >= 0.90
```

It must independently re-satisfy close-X, relation, neutral-header, magnifier, dark-title, title-text evidence and final `HEADER_FRAME_LOCKED >= 0.68`.

## 8. OCR / sanitation / matcher

- Windows ko-KR primary OCR
- normal + bounded deep variants
- raw OCR retained separately
- optional user substitution applied once
- current official catalog character/symbol policy
- impossible glyph is not globally forced to a guessed character
- exact official name first
- conservative fuzzy confidence + top1/top2 margin
- ambiguity fail closed
- bounded unique unknown/edit recovery only

## 9. Cross-environment normalization regression

Normalization is input canonicalization, not identity proof.

### Reference compatibility

Reference SDR-like title profile must keep historical behavior.

- normal OCR success performs no unnecessary luminance histogram/copy/additional OCR
- reference input does not spuriously enable adaptive normalization
- matcher/semantic gates remain unchanged

### Adaptive cases

Procedural luminance cases cover:

- lifted/washed background + bright glyphs
- lifted + compressed contrast
- low-contrast gamma/rendering variation

Assertions:

- adaptive threshold remains between measured background and foreground
- foreground/background separation remains deterministic
- normalized grayscale restores usable contrast
- reference and transformed inputs preserve equivalent glyph structure for OCR evidence

### Resolution classes

Proportional title raster cases cover 1080p / 1440p / 4K.

- environment classification is not tied to one absolute raster size
- no user-PC/GPU/HDR-specific branch is introduced

### Flat negative

Effectively flat/no-contrast input:

```text
HasUsableContrast = false
adaptive normalization disabled
no invented foreground/background separation
downstream remains fail closed
```

## 10. Presentation regression

### Mini Scanner

Confirmed Item ID is presentation authority.

```text
recognition success
→ Item ID confirmed
→ presentation snapshot
→ Mini Scanner
```

Auxiliary inventory-header OCR cannot veto an already confirmed Item.

Sticky display:

```text
success → show/update + miss budget reset
miss #1 → last good
miss #2 → last good
miss #3 → hide
```

### Needed quantity / source

After Item ID confirmation only:

```text
needed quantity = ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
source list = ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

Scanner does not independently recalculate Quest/Hideout/Inventory demand.

## 11. Ground Truth calibration

Deterministic CI protects known contracts. Real Tarkov reviewed Ground Truth is required to change recognition policy.

Evidence collection order:

```text
real failure frame
→ classify proposal / header / OCR / match / visual / presentation stage
→ preserve exact reviewed evidence
→ replay current implementation
→ identify root cause
→ add minimal regression fixture
→ change affected layer only
```

Do not tune against unreviewed automatic samples.

A change that improves one screenshot but weakens fail-closed behavior across the catalog is not acceptable.

## 12. Performance acceptance

Continuous mode target remains one observation every ~200 ms under normal conditions. This is a pacing target, not permission to overlap OCR or run unbounded work.

Historical latency baseline from v1.7.6 actual successful Tarkov observations remains diagnostic context:

```text
minimum ≈ 38 ms
median  ≈ 64 ms
mean    ≈ 211 ms
maximum ≈ 1.05 s
```

Do not optimize repeated profile loads speculatively: `UserProfileStore` already has an immutable in-process snapshot cache after authoritative load/save. Require runtime trace before architecture changes.

## 13. Current release decision

v1.7.14 passed the full gate with **407 passed / 0 failed / 0 skipped** and did not change Scanner recognition policy.

The next Scanner code change requires actual runtime evidence or reviewed Ground Truth. Until then Scanner remains maintenance-only.
