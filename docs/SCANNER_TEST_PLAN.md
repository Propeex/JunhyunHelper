# Scanner v1.5.0 Test Plan

기준일: 2026-08-24
상태: **v1.5.0 PUBLIC RELEASE / VERIFIED / LIVE GROUND TRUTH CALIBRATION ONGOING**

이 문서는 deterministic release gate와 실제 Tarkov 환경에서만 얻을 수 있는 calibration을 분리한다. 실제 reviewed evidence 없이 geometry/OCR/visual confidence threshold나 candidate cap을 조정하지 않는다.

## 1. 현재 공개 기준선

```text
exact source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
final PR CI: 32688080850 — SUCCESS
296 passed / 0 failed / 0 skipped
release run: 32691423654 — SUCCESS
independent public verifier: 32691641614 — SUCCESS
asset: Junhyun-Helper-v1.5.0-win-x64.zip
bytes: 80,422,292
SHA-256: 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
ProductVersion: 1.5.0+6de738959740d12e6ccb81b65e50006e463eb699
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download/checksum/layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세:

- `docs/RELEASE_1.5.0.md`
- `docs/.release-v1.5.0-status.json`
- `docs/SCANNER.md`
- `docs/SCANNER_GROUND_TRUTH.md`

## 2. Release-blocking gate

Scanner 변경이 포함된 정식 release/PATCH는 최소 다음 범주를 모두 검토한다.

### Build / unit-regression

1. exact candidate source 고정
2. Windows Release build
3. full automated tests — 0 failed / 0 skipped
4. Scanner structural proposal regression
5. inspect-header semantic lock regression
6. incomplete lock fail-closed regression
7. title ROI ownership regression
8. raw/substituted/normalized OCR evidence separation
9. current-catalog character/symbol policy regression
10. current official catalog matcher regression
11. bounded unknown/edit recovery safety
12. visual corroboration/recovery fail-soft safety
13. font source/cache generation consistency
14. bounded visual caches
15. market/dimension/RequiredTotal mapping regression
16. catalog load/refresh GameMode ordering regression
17. one-shot/profile/GameMode lifecycle regression
18. hotkey/settings migration/duplicate prevention regression
19. user OCR substitution single-pass/default-empty regression
20. title continuity signature regression
21. Mini Scanner inventory-probe coalescing/stale-result regression
22. Ground Truth candidate/manual fallback contracts
23. retention reviewed-GT preservation contract

### Publish / product smoke

24. Windows x64 self-contained single-file publish
25. exact ProductVersion / FIRST_RUN identity
26. package-root / PDB / nested-archive / forbidden dependency audit
27. actual published EXE Product UI smoke
28. Scanner normal/settings/advanced UI smoke
29. `1회 스캔` / `현재 결과 교정` product contract
30. Mini Scanner smoke + quick correction entry
31. Main Map / Factory / MiniMap smoke
32. graceful close / process termination
33. clean portable root

### Public release verification

34. exact source tag
35. draft asset redownload + SHA-256 verification
36. draft package ProductVersion/FIRST_RUN/layout verification
37. draft-downloaded EXE smoke
38. public stable/latest verification
39. exact public tag source verification
40. fresh independent anonymous public ZIP + SHA256SUMS redownload
41. public hash/size/package layout verification
42. public ProductVersion/FIRST_RUN verification
43. public-downloaded EXE Product UI/Map/Scanner smoke
44. durable `docs/.release-vX.Y.Z-status.json`
45. one-shot release/verifier workflow cleanup

최신 실제 Tarkov E2E는 공개 후에도 계속한다. 이미 확보한 reviewed live failure는 관련 변경의 regression에 반영해야 한다.

## 3. 불변 threshold / candidate budget

현재 계약:

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
```

다음 이유만으로 변경하지 않는다.

- “인식률이 낮아 보여서”
- 한두 개 screenshot에서 miss가 나서
- CPU 사용량을 줄이기 위해
- fuzzy match를 더 쉽게 통과시키기 위해

변경하려면 reviewed Ground Truth replay와 false-positive impact evidence가 필요하다.

## 4. Structural proposal regression

유지 계약:

- RED-X connected-component proposal path
- rectangle/edge fallback
- structural floor 0.34
- continuous max 8
- one-shot max 12
- aspect prior는 약한 ranking hint
- high IoU 자체는 duplicate 판정이 아님
- edge가 다른 겹친 proposal 보존
- near-identical edge jitter만 dedupe
- geometry evidence만으로 Item ID 확정 금지

대표 regression:

- cropped inspect window
- full-screen inspect window
- tall/large detail window
- strong inner rectangle coexistence
- high-IoU but different-edge detail proposal
- no-RED-X proposal fallback
- uniform-frame fail-closed

구조 후보 생성과 **OCR identity 허용**은 별개다.

## 5. Inspect-header semantic lock regression

Required evidence:

- right red close-X
- long neutral inspect-header/frame
- bounded frame-left search-icon lane
- magnifier ring/hollow/handle morphology
- dark title field
- title text presence

필수 assertions:

1. screen-center absolute heuristic에 의존하지 않음
2. historical measured live header geometry regression 유지
3. title-lane decoy ring/glyph가 magnifier를 대체하지 않음
4. fragmented first glyph가 title ROI ownership을 가져가지 않음
5. `HEADER_FRAME_LOCKED`가 아닌 candidate는 production OCR identity path에 진입하지 않음
6. runtime은 score >= 0.68을 다시 요구
7. magnifier/close/title bounds 중 필수 evidence 하나가 없으면 fail closed
8. oversized/contained-subpanel fallback도 같은 semantic gate 재검증

## 6. OCR / sanitation / matcher regression

- Windows `ko-KR` OCR primary
- title-size adaptive scaling/preprocessing
- normal pass + deep variants
- raw Windows OCR 보존
- current official Korean catalog에서 character/symbol policy 파생
- catalog-impossible glyph를 특정 문자로 product-wide 치환하지 않음
- exact official name 우선
- fuzzy confidence + top1/top2 margin 유지
- ambiguous candidate fail closed
- bounded unknown/edit recovery만 허용

### Unknown-glyph recovery

- `?`는 특정 문자 치환이 아니라 unknown-position evidence
- complete current catalog에서 후보가 유일해야 함
- short/ambiguous case fail closed
- global separation 부족 fail closed

### Bounded edit recovery

Current full catalog에서 유일하고 충분히 분리된 bounded edit 후보만 허용한다.

Multi-edit low-confidence OCR을 percentage만으로 확정하지 않는다.

## 7. User OCR substitution regression — schema v5

필수 계약:

```text
raw OCR
→ enabled user substitutions
→ sanitation / normalization
→ matcher
```

검증:

- default substitution list empty
- add/delete
- enable/disable
- reset
- exact user string matching
- ordered single-pass only
- replacement 결과 recursive reprocessing 없음
- raw OCR immutable forensic evidence
- substituted text 별도 evidence
- settings schema v5 normalization/migration
- malformed/empty rule normalization
- user substitution이 automatic product-wide alias table로 승격되지 않음

Regression에는 실제 반복 오류 예시를 넣을 수 있지만, preset/global forced substitution으로 자동 적용하지 않는다.

## 8. Tarkov-font visual regression

- public package에 game font binary 포함 금지
- installed Tarkov `resources.assets` read-only source
- source/font generation 변경 시 stale rendered cache 재사용 금지
- partial/corrupt cache는 visual path만 fail-soft
- candidate universe = current official full-item catalog
- visual top1 + top1/top2 margin 필요
- semantic success와 visual conflict 시 strict evidence 없으면 healthy OCR 유지
- renderer/font unavailable → primary OCR path 유지
- arbitrary Item/text 생성 금지
- template/aspect/mask caches bounded

## 9. Exact same-cycle OCR reuse regression

`SerializedScannerOcrEngine` reuse는 다음 모두가 같아야 한다.

```text
same active scan cycle
same normal/deep class
same width
same height
same BPP
same exact pixel SHA-256
```

검증:

- exact same bitmap same cycle → reuse 가능
- one pixel difference → reuse 금지
- normal vs deep → cache 공유 금지
- cycle change → cache invalidation
- disposed/ended cycle → late result가 새 cycle cache를 오염하지 않음
- cross-frame reuse 없음

성능 최적화가 현재 frame evidence를 과거 frame 결과로 대체하지 않아야 한다.

## 10. Title continuity stabilization regression

`ScannerTitleIdentitySignature`는 already-verified detail continuity용이다.

검증:

- same glyph shape + dark background variation → same stable signature 가능
- unused trailing ROI width variation → same signature 가능
- visible glyph shape change → different signature
- no visible title ink → fail closed
- signature 자체로 Item ID 확정 금지
- new geometry/title identity evidence → old trusted result clear
- bounded detector miss policy 유지

이 regression이 false-positive 증가 없이 flicker만 줄이는지 확인한다.

## 11. Scanner catalog / mapped-data regression

Identity health:

```text
accepted item count >= 4000
+ non-empty Item ID
+ non-empty official name
```

Catalog transition:

- `LoadCacheAsync` / `RefreshAsync` 동일 operation gate
- older GameMode writer가 newer state overwrite 금지
- shutdown cancellation boundary 유지

Mapped presentation:

```text
best trader price = valid non-flea RUB max
best trader name = selected valid source when available
flea average = positive avg24hPrice
slots = positive width × height
trader price/slot = valid trader price + slots
flea price/slot = valid flea price + slots
needed count = NeededItems[itemId].RequiredTotal
```

Market/dimension missing은 identity가 아니라 해당 field만 fail closed한다.

Source payload shape regression은 raw `traderPrices`/derived `sellFor` 등 지원 경로를 실제 parser tests에서 고정한다.

## 12. Unified Game Data + Scanner refresh regression

사용자 top-level data update 후:

- general Game Content update 수행
- current GameMode Scanner catalog/market refresh 수행
- 둘 다 성공하면 combined healthy state
- Scanner refresh만 실패하면 general content success rollback 금지
- existing healthy Scanner cache 보존 가능
- partial failure status가 사용자에게 식별 가능
- Scanner 전용 강제 refresh는 일반 필수 절차가 아님

## 13. One-shot / hotkey regression

Scanner settings schema v5.

기본키:

- `Ctrl+Shift+F10` in-game one-shot
- `Ctrl+Shift+F11` test one-shot
- `Ctrl+Shift+F12` Scanner ON/OFF

검증:

- MainWindow lifetime global registration
- 각 command 변경/비활성화
- 동일 gesture 중복 차단
- old settings migration 시 사용자 선택 보존
- one-shot duplicate invocation overlap 금지
- one-shot 종료 후 current requested mode만 restore
- **일반 Scanner UI에 `1회 스캔` 버튼 존재**
- DisplayTest one-shot은 연결 display를 one-shot contract로 처리
- scan-time network refresh 없음
- continuous candidate cap과 one-shot candidate cap 분리

## 14. Ground Truth correction regression

Candidate-first UX:

1. detail candidate
2. close-X candidate
3. magnifier candidate
4. item-name ROI candidate
5. correct item/text
6. save

검증:

- candidate ID/rank/score/geometry 저장
- candidate 선택 가능
- explicit `없음` 저장 가능
- manual rectangle fallback 유지
- candidate가 없다고 correction 자체가 막히지 않음
- reviewed status가 automatic save에 의해 overwrite되지 않음
- raw/substituted/normalized OCR evidence 유지
- matcher top candidates 유지
- mapped_data snapshot 유지

Mini Scanner `현재 결과 교정`은 latest debug snapshot과 current coordinator로 같은 correction flow를 열어야 한다.

## 15. Ground Truth full-pipeline replay regression

Reviewed `full.png`를 current production path에 재투입한다.

```text
full.png
→ proposals
→ semantic header
→ title ROI
→ OCR/deep/user substitution/visual recovery
→ current catalog match
→ final Item ID
```

결과:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

과거 정상 Case가 현재 실패하면 평균 accuracy가 올라가도 `REGRESSION`이다.

Mapped price 변화는 source freshness 때문일 수 있으므로 `MAPPED_DATA_CHANGED` 자체를 identity regression으로 보지 않는다.

## 16. Retention / log regression

Reviewed Ground Truth:

- automatic delete 절대 금지

Automatic deletion eligibility:

```text
retention == automatic_sample
AND review_status == unreviewed
```

Bounds:

- 30 days
- 300 automatic cases
- 512 MiB
- recent 2-hour protection

검증:

- reviewed Case retained
- unknown/corrupt metadata retained
- recent Case retained
- old excess automatic sample deleted within policy
- deletion 직전 metadata re-read
- retention cleanup failure가 recognition result를 바꾸지 않음

Logs:

- scanner.log rotation bounded
- startup.log rotation bounded
- `로그 삭제`가 Ground Truth dataset을 삭제하지 않음

## 17. Mini Scanner regression

- matched Item data only
- Topmost / no-activate
- full-card drag surface
- inventory/stash probe single-active
- latest request coalesce
- old item/context epoch result reject
- uncertain foreground/inventory context → hidden
- title OCR과 inventory OCR serialized
- quick correction context menu exists
- quick correction이 latest Case를 사용
- scan-time icon/network 없음

## 18. Scanner Product UI regression

Normal surface에 다음이 있어야 한다.

- Scanner ON/OFF
- `1회 스캔`
- `현재 결과 교정`
- runtime status
- recent recognition history

`설정` 아래:

- hotkey settings
- OCR substitutions
- Mini Scanner display options

`고급 / 진단` 아래:

- Display Test
- 인식 이미지
- regression
- Ground Truth export/manage
- `아이템 목록 최신화` recovery action
- 로그 삭제
- diagnostics storage information

제품 smoke는 old v1.3 UI를 강제하지 않는다.

## 19. Latency telemetry regression

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

검증:

- stage timing이 scan cycle에 연결
- disposed cycle에 late async stage가 잘못 기록되지 않음
- continuous detector-only sampling이 excessive log churn을 만들지 않음
- one-shot/semantic cycles에서 useful timing evidence 확보
- telemetry 추가가 acceptance threshold를 변경하지 않음

## 20. Product UI / version / package regression

- MainWindow user-facing version은 assembly/product version에서 파생
- `+commit` build metadata는 user-facing label에 강제 노출하지 않음
- 특정 release version을 XAML에 하드코딩하지 않음
- MainWindow minimum width 1180 contract가 actual two-pane/header structure를 보호

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

검증:

- PDB 없음
- unexpected root DLL/archive 없음
- nested archive 없음
- forbidden unused legacy dependency 없음
- ProductVersion = version + exact source SHA
- FIRST_RUN first line exact identity
- actual EXE rendered Product UI smoke
- Scanner/Mini Scanner smoke
- Main Map / Factory / MiniMap smoke
- graceful shutdown
- portable root runtime pollution 없음

## 21. Public verification protocol

Release controller:

- exact source checkout
- build + full tests
- publish/package audit
- packaged EXE smoke
- exact source tag
- draft asset upload
- draft redownload hash/identity/layout
- draft-downloaded EXE smoke
- stable/latest publication

Independent public verifier는 **release controller artifact를 재사용하지 않는다.**

Fresh runner에서:

- anonymous GitHub latest API check
- anonymous public tag resolution
- anonymous ZIP + SHA256SUMS download
- hash/size verification
- package layout/ProductVersion/FIRST_RUN verification
- public-downloaded EXE Product UI/Map/Scanner smoke
- graceful shutdown
- durable release status persistence

검증 완료 후 one-shot release/verifier workflow는 저장소에서 제거하고 steady-state `ci.yml`만 남긴다.

## 22. v1.5.0 완료 증거

- final PR #172 CI `32688080850`: SUCCESS
- 296 tests / 0 failed / 0 skipped
- exact source/tag `6de738959740d12e6ccb81b65e50006e463eb699`
- release run `32691423654`: SUCCESS
- independent public verifier `32691641614`: SUCCESS
- public/latest: VERIFIED
- public ZIP SHA-256: `6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9`
- public bytes: `80,422,292`
- public ProductVersion exact match
- public package layout: VERIFIED
- public-downloaded EXE Product UI/Scanner/Mini Scanner/Main Map/Factory/MiniMap smoke: SUCCESS
- graceful shutdown: SUCCESS
- durable status: `docs/.release-v1.5.0-status.json`
- temporary release/verifier workflows: REMOVED

## 23. Live Tarkov calibration protocol

권장 사용자 동선:

```text
아이템 상세창 열기
→ Scanner 또는 1회 스캔
→ 결과 확인
→ 정상 대표 결과면 `현재 결과 교정` → 맞음
→ miss/wrong identity면 즉시 `현재 결과 교정`
→ candidate/영역/text truth 저장
→ reviewed Ground Truth 축적
→ 필요 시 diagnostics export
```

분류:

1. capture/window
2. structural proposal recall/ranking
3. close-X/magnifier semantic evidence
4. header lock/title ROI
5. OCR
6. user substitution
7. catalog sanitation/matcher
8. font visual recovery
9. mapped presentation
10. continuous timing/stale-state

Wrong identity는 miss보다 높은 우선순위로 처리한다.

## 24. 다음 개발 기준

v1.5.0 deterministic/public verification은 완료됐다.

다음 Scanner 개선은 실제 reviewed Ground Truth를 우선한다.

```text
reviewed dataset 확보
→ failure cluster 분석
→ stage 특정
→ 필요한 stage만 수정
→ full replay regression
→ REGRESSION=0
→ full build/tests/publish smoke
→ PATCH 여부 결정
```

추가 evidence가 없는 상태에서 threshold/candidate cap을 임의 완화하거나 unrelated Scanner 기능을 추가하지 않는다.
