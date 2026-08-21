# Scanner v1.2.0 Test Plan

기준일: 2026-08-22

상태: **`PUBLIC RELEASE GATE PASSED / v1.2.0 PUBLIC VERIFIED / LIVE TARKOV E2E POST-RELEASE`**

이 문서는 v1.2.0의 자동/Windows/public release gate와 공개 후 실제 Tarkov 검증을 분리합니다.

## 1. Release blocking gate — 완료

1. exact release source 고정
2. Windows Release Desktop build
3. 전체 automated tests 0 failure / 0 skip
4. Scanner Lab v3.8 structural regression
5. title anchor / magnifier exclusion regression
6. OCR character-policy regression
7. current catalog semantic matcher regression
8. full-catalog visual recovery regression/contract
9. market-field regression
10. one-shot/hotkey/settings schema regression
11. win-x64 self-contained single-file publish
12. ProductVersion = 1.2.0 + exact release source
13. FIRST_RUN first line = v1.2.0
14. package root/dependency/PDB/nested-archive audit
15. actual published EXE startup
16. rendered Product UI + Scanner UI assertions
17. Mini Scanner actual WPF smoke
18. Scanner schema v3/default hotkey smoke
19. synthetic magnifier-exclusion inspect-header smoke
20. Main Map / Factory / MiniMap runtime smoke
21. graceful Main Window close/process exit
22. Draft ZIP/checksum/package/ProductVersion/FIRST_RUN verification
23. Draft-downloaded EXE smoke
24. public/latest 전환
25. exact public tag → release source SHA verification
26. public ZIP/checksum/package/ProductVersion/FIRST_RUN 재검증
27. public-downloaded EXE smoke
28. public-downloaded EXE graceful shutdown

실제 최신 Tarkov 실행 E2E는 public release blocker가 아니며 사용자 환경에서 후속 검증합니다.

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

## 3. v1.2.0 title anchor regression

확인:

- red close/X evidence가 detail header 위치와 일치
- magnifier candidate가 title field 좌측에서 검출 가능
- title field refinement가 valid rectangle만 반환
- magnifier가 검출되면 refined title ROI의 left가 magnifier right보다 오른쪽
- title ROI와 magnifier bounds가 실질적으로 overlap하지 않음
- close/magnifier evidence가 불충분하면 기존 Scanner Lab geometry title ROI fallback
- anchor 실패를 이유로 arbitrary screen strip을 OCR하지 않음

Published EXE smoke에는 synthetic Tarkov inspect-header를 생성하여 magnifier exclusion contract를 실제 WPF/Windows build에서 검사합니다.

## 4. OCR character policy

`ScannerOcrCharacterPolicy` 검증:

- current official Korean catalog에서 allowed character set 생성
- 공식 이름에 실제 존재하는 Hangul/Latin/숫자/기호 허용
- catalog에 존재하지 않는 unexpected character reject
- Han ideograph reject
- character rejection이 arbitrary replacement/correction으로 바뀌지 않음
- catalog 변경 시 allowed set이 자동으로 재계산됨

OCR character reject는 Item ID를 직접 선택하는 근거가 아니라 semantic path를 보류하고 필요 시 visual recovery로 넘기는 evidence입니다.

## 5. OCR / semantic matcher

- exact official name 우선
- normalized text candidate 비교
- fuzzy confidence threshold 유지
- top1/top2 margin 유지
- duplicate/ambiguous official name fail closed
- empty OCR fail closed unless visual recovery가 별도 기준을 통과
- historical alias를 무제한 production source로 사용하지 않음
- corrupted OCR이 높은 구조점수만으로 Item 확정되지 않음

## 6. Full-catalog Tarkov-font visual recovery

확인 계약:

- current official full-item catalog만 candidate universe로 사용
- actual title image를 normalized visual representation으로 비교
- scan-time HTTP/API 없음
- visual path가 successful primary OCR path를 무조건 덮어쓰지 않음
- top1 score threshold 필요
- top1/top2 margin 필요
- ambiguous visual candidates reject
- no catalog / no valid title image fail closed
- visual result도 최종 Item ID가 current catalog에 존재해야 함

## 7. Candidate 안정화 / OCR 억제

검증 계약:

- candidate가 없으면 stable hit = 0
- 서로 다른 geometry signature만 이어지면 stable로 승격하지 않음
- 연속 candidate 집합에 같은 quantized `GeometrySignature`가 있을 때만 stable hit 누적
- mode/change/miss/reset에서 previous signature history clear
- verified bounds + title signature가 유지되면 OCR 반복 억제
- title/geometry 변화 시 기존 Item clear 후 재검증
- presentation refresh는 OCR을 재실행하지 않음

## 8. One-shot precision scan

검증 계약:

- continuous Scanner OFF에서도 실행 가능
- local healthy catalog 없으면 fail closed
- scan-time network refresh 시작 금지
- candidate 상위 집합 평가
- original OCR pass
- deep OCR pass
- visual recovery path 허용
- best successful candidate를 combined evidence로 선택
- no successful evidence면 overlay hidden / Item ID clear
- one-shot result가 success면 presentation snapshot 생성
- continuous mode가 없으면 결과 auto-hide timer 적용

## 9. Continuous / one-shot concurrency

실시간 Scanner/Test와 one-shot이 state를 동시에 변경하지 않아야 합니다.

검증 계약:

1. 현재 active mode capture
2. runtime `StopLoop()`
3. previous loop Task completion await
4. one-shot 수행
5. one-shot coordinator gate로 duplicate invocation reject/serialize
6. 최신 user setting이 이전 mode를 여전히 요청할 때만 restart

Title/inventory OCR은 기존 `SerializedScannerOcrEngine`의 semaphore를 계속 공유합니다.

## 10. Global hotkey / settings schema

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

Published EXE smoke는 v3/default hotkey product contract를 검사합니다.

## 11. Recognition debug image

- latest frame 1개만 memory에 유지
- capture origin/source 보존
- selected detail/title/magnifier/close bounds local coordinate 변환
- OCR text / candidate / reason / confidence / second score 표시
- 최종 선택된 recognition으로 metadata 갱신
- discarded candidate score가 final debug metadata로 남지 않음
- screenshot/raw pixel disk persistence 없음
- clear 시 frame/signature/timestamp 초기화

## 12. Catalog / market data

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

## 13. 현재 필요한 수량

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

검증:

- Inventory 차감 부족량을 Scanner 의미로 사용하지 않음
- NeededItems에 없으면 0
- 동일 Item ID presentation snapshot 주기적 재구성
- Quest/Hideout 진행 변화가 같은 상세창에서도 최신 RequiredTotal로 반영

## 14. Icon / performance

- scan-time icon HTTP 없음
- local image-cache만 사용
- Game Content update에서 canonical item 전체 icon prefetch
- invalid/missing local icon은 icon 표시만 omit
- decode/freeze 성공 아이콘 process memory cache 재사용
- presentation refresh가 같은 PNG decode를 반복하지 않음

## 15. Scanner UI

유지:

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

## 16. 로그 삭제 end-to-end smoke

실제 published EXE에서:

1. Scanner diagnostic/activity baseline clear
2. diagnostic/activity 생성
3. `scanner.log` 생성 확인
4. `scanner.log.1` 회전 로그 생성
5. rendered `로그 삭제` click
6. recent activity = 0
7. current/rotated log 없음

삭제 I/O 실패를 Scanner runtime fatal로 확대하지 않습니다.

## 17. Windows capture/runtime

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

## 18. Mini Scanner actual WPF smoke

- matched-item-only
- trader price 표시
- trader price/slot 표시
- Topmost
- ShowActivated=false
- WS_EX_NOACTIVATE / WS_EX_TOOLWINDOW
- 전체 rectangular card hitbox
- Arrow cursor
- no runtime/status text
- negative/multi-monitor position persistence contract 유지
- MiniMap과 독립 lifecycle

## 19. v1.2.0 Public release verification — 완료

최종 release source:

```text
a7601f8498e8d75e832962fb9dd60f4112d28dc6
```

최종 release run:

```text
32514322439 — SUCCESS
255 passed / 0 failed / 0 skipped
```

검증 흐름:

```text
exact source checkout
→ pinned Map donor verification
→ build
→ 255 tests
→ publish
→ package audit
→ exact published EXE smoke
→ ZIP + SHA256SUMS
→ Draft release
→ Draft asset re-download/hash/root/ProductVersion/FIRST_RUN
→ Draft-downloaded EXE smoke
→ public/latest
→ exact tag verification
→ public asset re-download/hash/root/ProductVersion/FIRST_RUN
→ public-downloaded EXE smoke
```

최종 공개 패키지:

```text
asset: Junhyun-Helper-v1.2.0-win-x64.zip
bytes: 80,298,514
SHA-256: ab5e9ef35b300268d16a1c5eece86cd8c6e57c91c83364caf4b7d02cde1d27d1
ProductVersion: 1.2.0+a7601f8498e8d75e832962fb9dd60f4112d28dc6
public-downloaded EXE smoke: SUCCESS
```

첫 attempt는 기존 Main Map asynchronous off-floor marker smoke의 settle timing assertion에서 ZIP 생성 전 중단됐습니다. 제품 source는 바뀌지 않았고 동일 exact source를 clean rerun하여 같은 Map smoke 및 모든 Draft/Public gate를 통과했습니다.

## 20. 공개 후 실제 Tarkov 검증

우선순위:

1. 실제 Borderless detail candidate 안정성
2. close/magnifier/title anchor 정확도
3. 실제 title ROI가 돋보기를 제외하는지
4. current Korean title OCR
5. OCR-invalid-character 발생 패턴
6. semantic vs visual recovery 선택
7. false positive / miss
8. 최고 상점가 / 플리 평균가 / 현재 필요한 수량
9. one-shot hotkey 실전 사용성
10. Mini Scanner inventory gate
11. 장시간 CPU/memory/handles/OCR rate
12. Mini Scanner / MiniMap / Alt+Tab 공존

문제가 있으면 `scanner.log`와 `인식 이미지`를 근거로 후속 PATCH에서 보정합니다. confidence/margin을 단순히 낮춰 오탐을 늘리는 방식은 사용하지 않습니다.
