# Scanner Current Test Plan

기준일: 2026-08-27
상태: **v1.7.11 PUBLIC STABLE / FEATURE COMPLETE / MAINTENANCE ONLY**

이 문서는 deterministic release gate와 실제 Tarkov reviewed Ground Truth calibration을 분리한다. Reviewed evidence 없이 geometry/OCR/matcher/visual confidence threshold나 candidate cap을 조정하지 않는다.

## 1. Current verified stable

```text
Desktop target: 1.7.11
automated suite at v1.7.11 release: 392 tests
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
stable package: Junhyun-Helper.zip
exact product release source: 0f97c6e5340ae91581a9242ec236bbd7885b34d5
main CI: 33033282963 — SUCCESS
Release workflow: 33033434877 — SUCCESS
release id: 377531277
public bytes: 80,477,565
public SHA-256: f1ad15debc29b7a167a13448c8df65785f57139a91d8b5d246205a14f9a5800d
```

v1.7.11 release gate는 build/test/publish/rendered Product UI/Scanner/Map/Factory/MiniMap smoke, graceful shutdown, package checksum/layout, exact source tag, stable release metadata와 public asset readback을 모두 통과했다.

릴리즈 이후 maintenance-only main은 deterministic regression을 추가할 수 있으므로 현재 main의 test 수와 immutable v1.7.11 release source의 test 수를 혼동하지 않는다. 빠르게 변하는 현재 main/CI 상태는 `docs/STATE.md`를 기준으로 한다.

## 2. Release-blocking deterministic gate

### Build / automated regression

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
11. official catalog matcher and ambiguity fail closed
12. bounded unknown/edit recovery safety
13. visual corroboration fail-soft safety
14. bounded same-cycle visual/OCR caches
15. market/dimension/RequiredTotal same-ID mapping
16. catalog load/refresh GameMode ordering
17. one-shot/profile/GameMode lifecycle
18. Scanner/Map hotkey migration + duplicate prevention
19. user OCR substitution single-pass/default-empty
20. title continuity signature
21. Ground Truth candidate/manual/none contracts
22. reviewed-GT retention protection
23. no durable automatic Case creation during normal monitoring
24. legacy automatic Case cleanup fail-closed contract
25. Scanner settings schema-v6 migration/order contract
26. saved Case reopen/re-edit fail-closed contract
27. item search local-data/no-network contract where testable
28. Scanner main correction action exact-frame contract
29. v1.7.9 Mini Scanner confirmed-item presentation authority
30. v1.7.9 initial hidden real-overlay foreground-Tarkov guard
31. v1.7.10 reference SDR normalization compatibility
32. v1.7.10 lifted/washed HDR→SDR-like normalization
33. v1.7.10 compressed-contrast normalization
34. v1.7.10 low-contrast gamma/rendering normalization
35. v1.7.10 1080p/1440p/4K proportional title raster matrix
36. v1.7.10 flat/no-contrast negative case
37. Scanner support bundle excludes Ground Truth/source pixels, `user.db`/profile DB, game-account information and user-progress/account-identifying data

### Publish / product smoke

38. Windows x64 self-contained single-file publish
39. exact ProductVersion / FIRST_RUN identity
40. publish-root / PDB / nested-archive / forbidden dependency audit
41. actual published EXE startup
42. Scanner normal surface smoke
43. Mini Scanner rendered/Topmost/confirmed-item policy smoke
44. Main Map / Factory / MiniMap smoke
45. graceful close / process termination
46. clean portable root
47. release package creation
48. stable `Junhyun-Helper.zip` exists
49. every ZIP entry under `준현 헬퍼/`
50. required EXE / FIRST_RUN / Tarkov data asset present
51. release ZIP SHA-256 recorded

### Public release verification

52. exact tag target points to chosen product release source
53. stable/latest metadata points to exact source
54. public asset name exactly `Junhyun-Helper.zip`
55. checksum asset present/verified
56. public hash/size readback
57. ProductVersion/FIRST_RUN correspond to target version
58. durable release record/status written after public readback

## 3. Immutable threshold / candidate budget

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

변경은 reviewed Ground Truth replay와 false-positive 영향 evidence를 요구한다.

## 4. Structural proposal regression

Maintain:

- RED-X component proposals
- rectangle/edge fallback
- structural floor 0.34
- continuous 8 / one-shot 12
- aspect prior only weak ranking hint
- high IoU alone not sufficient for duplicate removal
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

## 5. Inspect-header semantic lock regression

Required evidence:

- right red close-X
- neutral inspect-header/frame
- bounded magnifier/search lane
- magnifier morphology
- dark title field
- title text evidence

Assertions:

- no absolute screen-center dependency
- non-`HEADER_FRAME_LOCKED` candidate cannot enter production OCR identity
- runtime score >= 0.68 required
- missing magnifier/close/title evidence → fail closed
- contained-subpanel fallback re-runs same semantic gate

### v1.7.8 raid ownership

Recovery priority:

```text
primary header lock
→ live Ground Truth recovery
→ raid ownership recovery
→ contained-subpanel recovery
→ fail closed
```

Raid recovery entry:

```text
candidate reason = RED_X_CANDIDATE
structural score >= 0.90
```

It must independently satisfy the existing close-X, relation, neutral-header, magnifier, dark-title and title-text evidence floors plus final `HEADER_FRAME_LOCKED >= 0.68`.

Procedural smoke:

- positive: long external neutral line visually joins inspect header but valid close-X/magnifier/title exist → recovery can lock
- negative: same geometry without valid red close-X → fail closed

## 6. OCR / sanitation / matcher

- Windows ko-KR primary
- normal + bounded deep variants
- raw OCR retained
- optional user substitution applied once
- current official catalog character/symbol policy
- impossible glyph not globally forced to a specific character
- exact official name first
- conservative fuzzy confidence + top1/top2 margin
- ambiguity fail closed
- bounded unique unknown/edit recovery only

## 7. v1.7.10 cross-environment normalization regression

Normalization is input canonicalization, not identity proof.

### Reference compatibility

Reference SDR-like title profile must keep historical behavior.

- normal OCR success path performs no luminance histogram/copy/additional OCR
- reference profile must not spuriously enable adaptive normalization
- existing matcher/semantic gates unchanged

### Adaptive cases

Procedural luminance pairs cover:

- lifted/washed background + bright glyphs
- lifted + compressed contrast
- lower-contrast gamma/rendering variation

Assertions:

- adaptive threshold remains between measured background and foreground
- binary foreground/background separation is preserved
- normalized grayscale restores strong deterministic contrast
- reference vs washed binary glyph structure remains effectively equivalent

### Resolution classes

Same procedural title structure is generated at proportional 1080p/1440p/4K title-raster classes.

Assertions:

- luminance profile classification remains stable
- adaptive/reference behavior is not tied to one absolute raster size
- no PC/GPU/HDR-specific branch is introduced

### Flat negative

Effectively flat/no-contrast input:

- `HasUsableContrast = false`
- adaptive normalization disabled
- no invented foreground/background separation
- downstream remains fail closed

## 8. Mini Scanner presentation regression

Confirmed Scanner Item identity is presentation authority.

Assertions:

```text
preview → allowed
Display Test / scannerEnabled=false → allowed
real Scanner + Tarkov foreground → allowed
real Scanner + Tarkov not foreground + overlay hidden → blocked
already-visible overlay + new confirmed Item → immediate update
```

Auxiliary inventory-header OCR must not veto a confirmed Item.

Sticky presentation:

- success resets miss budget
- miss #1 retains
- miss #2 retains
- miss #3 hides
- progress-only states do not count as misses

## 9. Ground Truth gate

Only user-reviewed/corrected explicit saves are Ground Truth.

For recognition changes where runnable reviewed dataset exists:

```text
REGRESSION = 0
```

Procedural/synthetic environment matrix does not replace reviewed Ground Truth. Private user images are not committed to the public repository merely to satisfy CI.

Legacy auto cleanup negative cases must preserve:

- reviewed
- manual
- corrupt
- unknown retention/review state
- state changed between scan and delete
- recent write inside safety window

## 10. Support-bundle privacy regression

`Scanner 성능 진단 자료 내보내기`가 만드는 support ZIP은 환경/성능 trace와 bounded diagnostic log만 포함한다.

Release regression은 최소한 다음이 ZIP에 포함되지 않음을 검증한다.

- Scanner Ground Truth image / source pixel dataset
- `user.db` 또는 profile database
- Tarkov/game account information
- 사용자 진행도나 계정 식별에 해당하는 데이터

Exporter 구현 변경 시 이 privacy exclusion을 유지하지 못하면 release blocker다.

## 11. Performance gate

v1.7.6 problem-PC baseline remains the reference.

Actual Tarkov successful `ReadingTitle → ShowingItem`:

```text
minimum 38.07 ms
median  63.92 ms
maximum 1.05 s
mean    211.47 ms
```

Current performance requirement (v1.7.10+):

- healthy normal OCR success must not pay normalization analysis or second OCR cost
- environment-normalized extra work is bounded to normal miss / existing deep path and only adaptive profiles
- same-cycle exact-pixel evidence reuse remains cycle-local
- no cross-frame Item identity cache

## 12. Maintenance incident procedure

```text
capture runtime evidence
→ classify failure stage
→ confirm root cause
→ change affected layer only
→ reviewed replay where runnable
→ deterministic procedural regression
→ full Windows CI/publish/product smoke/package
→ PATCH release
→ public release readback
→ canonical docs sync
```
