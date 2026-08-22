# Scanner v1.2.1 Test Plan

기준일: 2026-08-22

상태: **`v1.2.0 PUBLIC VERIFIED / v1.2.1 RELEASE CANDIDATE / LIVE TARKOV CALIBRATION DEFERRED`**

이 문서는 v1.2.1의 deterministic release gate와 실제 Tarkov에서만 얻을 수 있는 후속 calibration을 분리합니다. v1.2.1은 live evidence 없이 geometry/OCR/visual confidence threshold를 조정하지 않습니다.

## 1. v1.2.1 Release blocking gate

1. exact merge source 고정
2. Windows Release Desktop build
3. 전체 automated tests 0 failure / 0 skip
4. Scanner Lab v3.8 structural regression
5. title anchor / magnifier exclusion regression
6. OCR character-policy regression
7. current catalog semantic matcher regression
8. Tarkov-font recovery parser/fallback segmentation smoke
9. font cache generation/source consistency static contract
10. visual cache generation/boundedness code audit
11. market-field regression
12. one-shot/hotkey/settings schema regression
13. one-shot previous-mode restoration regression
14. win-x64 self-contained single-file publish
15. ProductVersion = `1.2.1+<exact release SHA>`
16. FIRST_RUN first line = `준현 헬퍼 v1.2.1 — Windows x64`
17. package root/dependency/PDB/nested-archive audit
18. actual published EXE startup
19. rendered Product UI + Scanner UI assertions
20. Mini Scanner actual WPF smoke
21. Scanner schema v3/default hotkey smoke
22. synthetic magnifier-exclusion inspect-header smoke
23. Main Map / Factory / MiniMap runtime smoke
24. graceful Main Window close/process exit
25. Draft ZIP/checksum/package/ProductVersion/FIRST_RUN verification
26. Draft-downloaded EXE smoke
27. public/latest 전환
28. exact public tag → release source SHA verification
29. public ZIP/checksum/package/ProductVersion/FIRST_RUN 재검증
30. public-downloaded EXE smoke
31. public-downloaded EXE graceful shutdown

최신 live Tarkov 실행 E2E는 release blocker가 아니며 사용자 환경에서 후속 검증합니다.

## 2. Scanner Lab v3.8 structural regression

반드시 유지:

- RED-X connected-component path
- RED-X anchored outer-window reconstruction
- rectangle/edge fallback
- IoU candidate deduplication
- candidate limit 8
- structural floor 0.34
- geometry alone으로 final Item 확정 금지
- adaptive 4x/6x/8x Windows ko-KR OCR
- deep OCR fallback
- current official Korean catalog semantic validation
- confidence/top1-top2 margin 유지

고정 구조 회귀:

- cropped `Ophthalmoscope 검안경`: outer inspect/title ROI
- full `Water 0.6L 물병` screenshot: central inspect/title ROI
- strong inner rectangle coexistence
- no RED-X rectangle fallback
- uniform frame fail-closed

## 3. Title anchor regression

확인:

- red close/X evidence가 detail header 위치와 일치
- magnifier candidate가 title field 좌측에서 검출 가능
- title field refinement가 valid rectangle만 반환
- magnifier가 검출되면 refined title ROI의 left가 magnifier right보다 오른쪽
- title ROI와 magnifier bounds가 실질적으로 overlap하지 않음
- close/magnifier evidence가 불충분하면 기존 Scanner Lab geometry title ROI fallback
- anchor 실패를 이유로 arbitrary screen strip을 OCR하지 않음
- diagnostic anchor score는 merely-present=100%가 아니라 actual component score를 보존

Published EXE smoke는 synthetic Tarkov inspect-header를 생성하여 magnifier exclusion contract를 실제 WPF/Windows build에서 검사합니다.

## 4. OCR character policy

`ScannerOcrCharacterPolicy` 검증:

- current official Korean catalog에서 allowed character set 생성
- 공식 이름에 실제 존재하는 Hangul/Latin/숫자/기호 허용
- catalog에 존재하지 않는 unexpected character reject
- Han ideograph reject
- character rejection이 arbitrary replacement/correction으로 바뀌지 않음
- catalog 변경 시 allowed set 자동 재계산

OCR character reject는 Item ID를 직접 선택하는 근거가 아니라 semantic path를 보류하고 필요 시 visual recovery로 넘기는 evidence입니다.

## 5. OCR / semantic matcher

- exact official name 우선
- normalized text candidate 비교
- fuzzy confidence threshold 유지
- top1/top2 margin 유지
- duplicate/ambiguous official name fail closed
- empty OCR fail closed unless visual recovery가 별도 기준 통과
- historical alias를 무제한 production source로 사용하지 않음
- corrupted OCR이 높은 구조점수만으로 Item 확정되지 않음
- successful primary semantic match를 font visual recovery가 교체하지 않음

## 6. Tarkov-font visual recovery

확인 계약:

- current official full-item catalog만 candidate universe로 사용
- actual title image를 normalized visual representation으로 비교
- scan-time HTTP/API 없음
- visual path가 successful primary OCR path를 덮어쓰지 않음
- top1 score threshold 필요
- top1/top2 margin 필요
- ambiguous visual candidates reject
- no catalog / no valid title image fail closed
- visual result도 final Item ID가 current catalog에 존재해야 함

## 7. v1.2.1 font source / cache generation

`TarkovTitleFontProvider` 계약:

- user-installed `EscapeFromTarkov_Data/resources.assets`만 read-only input으로 사용
- game font binaries를 public release에 번들하지 않음
- resources asset 전체를 `File.ReadAllBytes`로 적재하지 않음
- bounded sequential scan + validated random-access SFNT payload extraction
- invalid SFNT table directory / zero table / oversized payload reject
- required Korean font + 최소 하나의 Bender variant 없으면 recovery unavailable
- source path/length/last-write를 manifest에 기록
- actual cached Bender/Noto bytes의 SHA-256 조합으로 generation key 생성
- manifest는 font payload commit 뒤 마지막에 저장
- extraction 중단/부분 cache는 정상 generation으로 인정하지 않음
- legacy cache freshness는 존재하는 모든 Bender variant와 Noto의 oldest stamp 고려
- corrupt manifest/path/font cache는 Scanner fatal이 아니라 visual recovery unavailable

Published EXE `ScannerTitleFontSmoke`는 SFNT parser와 Korean fallback segmentation 기본 계약을 검증합니다.

## 8. v1.2.1 visual-cache boundedness

코드/CI 감사 계약:

- OCR-guided rendered template cache key에 `GenerationKey` 포함
- full-catalog mask/aspect cache key에 `GenerationKey` 포함
- font generation 변경 시 stale cache clear
- template/mask/aspect cache에 명시적 상한 존재
- cache eviction/clear가 confidence/identity 의미를 변경하지 않음
- 장시간 사용에서 catalog 이름 수에 비례해 무제한 mask가 누적되는 구조 금지

실제 장시간 메모리 곡선은 live E2E에서 추가 측정합니다.

## 9. Candidate 안정화 / OCR 억제

- candidate가 없으면 stable hit = 0
- 서로 다른 geometry signature만 이어지면 stable로 승격하지 않음
- 연속 candidate 집합에 같은 quantized `GeometrySignature`가 있을 때만 stable hit 누적
- mode/change/miss/reset에서 previous signature history clear
- verified bounds + title signature 유지 시 OCR 반복 억제
- title/geometry 변화 시 기존 Item clear 후 재검증
- presentation refresh는 OCR 재실행하지 않음

## 10. One-shot precision scan

- continuous Scanner OFF에서도 실행 가능
- local healthy catalog 없으면 fail closed
- scan-time network refresh 시작 금지
- candidate 상위 집합 평가
- original OCR pass
- deep OCR pass
- visual recovery path 허용
- best successful candidate를 combined evidence로 선택
- no successful evidence면 overlay hidden / Item ID clear
- success면 presentation snapshot 생성
- continuous mode가 없으면 result auto-hide timer 적용

## 11. Continuous / one-shot / profile concurrency

실시간 Scanner/Test, one-shot, profile/GameMode monitor가 shared runtime/catalog/presentation state를 오래된 관측으로 덮지 않아야 합니다.

검증 계약:

1. one-shot 시작 시 current active mode capture
2. runtime `StopLoop()`
3. previous loop Task completion await
4. one-shot 수행
5. one-shot coordinator gate로 duplicate invocation reject/serialize
6. 종료 시 `resumeMode == current ActiveCaptureMode`일 때만 restart
7. 사용자가 Scanner/Test를 끄거나 mode를 변경했다면 old mode restore 금지
8. profile/GameMode monitor는 same gate 획득 뒤 latest context 재조회
9. stale monitor tick이 old profile/mode를 restart하지 않음

Published EXE smoke는 `ShouldRestoreOneShotMode`의 same-mode/changed-mode/disabled-mode 계약을 검사합니다.

## 12. OCR serialization / shutdown lifetime

- Item-title OCR과 inventory-context OCR은 같은 `SerializedScannerOcrEngine` semaphore 공유
- WinRT OCR concurrent access 금지
- one-shot은 continuous loop actual completion 뒤 OCR 시작

`FontAwareScannerOcrEngine` disposal contract:

- Dispose 시작 후 new operation reject
- already-active operation count 보존
- active operation이 0이 된 뒤 Skia/font matcher/verifier/provider dispose
- disposed resource를 active recovery가 재사용하지 않음
- UI thread에서 active Scanner task 동기 wait로 deadlock 만들지 않음

## 13. Mini Scanner inventory-context coalescing

실제 mode 계약:

- foreground Tarkov + Korean inventory/stash context 없으면 hidden
- inventory navigation semantic anchor 최소 2개 요구
- 동시에 active inventory-context OCR probe 최대 1개
- 반복 `Show`는 latest pending snapshot으로 coalesce
- item change/hide가 visibility epoch 증가
- old probe cancellation
- late old-epoch result 화면 적용 금지
- test/preview deterministic path는 기존대로 context gate bypass 가능

실제 OCR latency/backlog는 live E2E에서 추가 관찰하지만, 코드상 unbounded queued probe를 만들 수 없어야 합니다.

## 14. PrintWindow allocation contract

Windows capture path:

- `PrintWindow` 우선
- invalid/empty capture면 exact client screen `CopyFromScreen` fallback
- `HasVisualContent`는 sparse sample만 필요
- sparse validation 때문에 full framebuffer managed copy 생성 금지
- locked bitmap direct sample 후 반드시 UnlockBits
- actual detector용 normalized BGRA copy는 1회 유지
- negative stride 처리 유지

## 15. Global hotkey / settings schema

- Scanner display settings current schema = v3
- 기존 settings normalize/migrate
- default one-shot hotkey = `Ctrl+Shift+F10`
- Ctrl/Alt/Shift modifier 최소 1개 필요
- valid WPF Key parse/serialize roundtrip
- disabled hotkey = empty/no registration
- `MOD_NOREPEAT`
- registration collision/failure가 status text로 노출
- Window detach/dispose에서 unregister
- handler 중복 동시 실행 방지

## 16. Recognition debug image

- latest frame 1개만 memory에 유지
- capture origin/source 보존
- selected detail/title/magnifier/close bounds local coordinate 변환
- OCR text / candidate / reason / confidence / second score 표시
- 최종 선택 recognition으로 metadata 갱신
- discarded candidate score가 final metadata로 남지 않음
- screenshot/raw pixel disk persistence 없음
- clear 시 frame/signature/timestamp 초기화
- title-anchor score는 actual detector evidence 보존

## 17. Catalog / market data

Full catalog:

- 4,000개 이상 Korean item load
- regular / pve / pvp-season
- Korean translation + English per-key fallback
- corrupt/missing cache reject
- requested mode missing 시 wrong-mode identity 사용 금지
- AtomicJson backup recovery

Identity health:

```text
item count >= 4000
AND valid Item ID/name for every accepted item
```

Market regression:

- raw `traderPrices` 지원
- derived `sellFor` 지원
- 복수 trader 중 non-flea RUB 최댓값 선택
- flea row는 best trader에서 제외
- flea average는 positive `avg24hPrice`
- zero/missing avg24hPrice → null
- invalid/non-positive dimension → slots 0, price/slot null
- valid price + slots → integer price/slot
- 4,000-item identity + trader price 0개 허용
- 3,999-item identity reject

## 18. 현재 필요한 수량

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

- Inventory 차감 부족량을 Scanner 의미로 사용하지 않음
- NeededItems에 없으면 0
- 동일 Item ID presentation snapshot 주기적 재구성
- Quest/Hideout 진행 변화가 같은 상세창에서도 최신 RequiredTotal로 반영

## 19. Icon / performance

- scan-time icon HTTP 없음
- local image-cache만 사용
- Game Content update에서 canonical item 전체 icon prefetch
- invalid/missing local icon은 icon 표시만 omit
- decode/freeze 성공 아이콘 process memory cache 재사용
- presentation refresh가 같은 PNG decode 반복하지 않음

## 20. Scanner UI / Mini Scanner actual WPF smoke

Scanner tab 유지:

- `스캐너 ON/OFF`
- `테스트 ON/OFF`
- `1회 고정밀 스캔`
- `인식 이미지`
- one-shot hotkey display/change
- `아이템 목록 최신화`
- display checkboxes
- recent recognition activity
- `로그 삭제`

없어야 함:

- Foundation verification/preview controls
- Mini Scanner 별도 위치 편집/초기화 일반 사용자 control
- Mini Scanner runtime/status text

Mini Scanner smoke:

- matched-item-only
- trader price 표시
- trader price/slot 표시
- Topmost
- ShowActivated=false
- WS_EX_NOACTIVATE / WS_EX_TOOLWINDOW
- 전체 rectangular card hitbox
- Arrow cursor
- negative/multi-monitor position persistence contract
- MiniMap과 독립 lifecycle

## 21. 로그 삭제 end-to-end smoke

실제 published EXE에서:

1. Scanner diagnostic/activity baseline clear
2. diagnostic/activity 생성
3. `scanner.log` 생성 확인
4. `scanner.log.1` 회전 로그 생성
5. rendered `로그 삭제` click
6. recent activity = 0
7. current/rotated log 없음

삭제 I/O 실패를 Scanner runtime fatal로 확대하지 않습니다.

## 22. Windows capture/runtime

Windows runner 확인:

- EscapeFromTarkov process/window discovery
- GetClientRect + ClientToScreen
- PrintWindow + CopyFromScreen fallback
- multi-monitor enumeration
- Windows ko-KR OCR boundary
- WPF BitmapSource handoff
- real/test mutual exclusion
- both OFF → no continuous capture/OCR loop
- one-shot은 continuous OFF에서도 explicit invocation 시만 수행

## 23. v1.2.0 Public baseline — 완료

```text
release source: a7601f8498e8d75e832962fb9dd60f4112d28dc6
release run: 32514322439 — SUCCESS
255 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.0-win-x64.zip
bytes: 80,298,514
SHA-256: ab5e9ef35b300268d16a1c5eece86cd8c6e57c91c83364caf4b7d02cde1d27d1
ProductVersion: 1.2.0+a7601f8498e8d75e832962fb9dd60f4112d28dc6
public-downloaded EXE smoke: SUCCESS
```

## 24. v1.2.1 pre-release evidence

Final static hardening candidate before version/docs:

```text
CI run: 32539676032 — SUCCESS
255 passed / 0 failed / 0 skipped
Windows Release build: PASS
win-x64 publish/package audit: PASS
published EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke: PASS
graceful shutdown / clean portable root: PASS
```

Final v1.2.1 version/FIRST_RUN/docs/smoke-contract head must pass the same gate before merge. Merge 후에는 exact merge SHA에서 별도 release build를 수행하고 Draft/Public asset을 각각 재다운로드해 hash/root/ProductVersion/FIRST_RUN/actual EXE smoke를 검증합니다.

## 25. 공개 후 실제 Tarkov 검증

우선순위:

1. 실제 Borderless detail candidate 안정성
2. close/magnifier/title anchor 정확도와 실제 diagnostic score
3. 실제 title ROI가 돋보기를 제외하는지
4. current Korean title OCR
5. OCR-invalid-character 발생 패턴
6. semantic vs visual recovery 선택
7. false positive / miss
8. 실제 Tarkov update 후 font source/cache generation 교체
9. 최고 상점가 / 플리 평균가 / 현재 필요한 수량
10. one-shot hotkey 실전 사용성과 mode/context 변경 중 동작
11. Mini Scanner inventory gate 및 OCR backlog 유무
12. 장시간 CPU/memory/handles/OCR rate / bounded visual cache
13. Mini Scanner / MiniMap / Alt+Tab 공존

문제가 있으면 `scanner.log`와 `인식 이미지`를 근거로 후속 PATCH에서 보정합니다. confidence/margin을 단순히 낮춰 오탐을 늘리는 방식은 사용하지 않습니다.
