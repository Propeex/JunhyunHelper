# Current Scanner Work

기준일: 2026-08-25
상태: **v1.7.0 PUBLIC RELEASE / VERIFIED — LIVE GROUND TRUTH MAINTENANCE**

Scanner v1.7.0 Product Completion hardening과 공개 릴리즈 검증이 완료되었다.

현재 단계는 **LIVE GROUND TRUTH MAINTENANCE**다. 실사용에서 수집된 reviewed Ground Truth를 기준으로 실패 stage만 수정하고 전체 reviewed replay에서 REGRESSION=0을 유지한다.

## v1.7.0 완료 내용

v1.7.0에서도 reviewed live Ground Truth 없이 detection/OCR/matcher threshold를 조정하지 않았다.

- recognition log → exact Case/current frame quick-correction
- 기존 Ground Truth + Scanner log ZIP export pipeline 재사용
- Scanner catalog/market last-known-good 보호 강화
- Item ID 이후 mapped presentation 동일-ID join 회귀 검증
- Scanner Advanced clipping 방지와 runtime log 7일 자동 정리 유지
- Data Update transactional hardening과 public release proof 완료

## 현재 production recognition pipeline

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ red close-X + magnifier + neutral inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog sanitation / normalization
→ conservative official-catalog matching / bounded recovery
→ optional Tarkov-font visual corroboration/recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional correction / Ground Truth
```

## 인식 안전 불변식

v1.7.0 hardening에서도 다음 값을 변경하지 않았다.

```text
structural floor = 0.34
trusted header floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
```

추가 계약:

- false positive보다 miss 선호
- geometry는 proposal이며 identity proof가 아님
- magnifier + red close-X semantic evidence 필수
- current official Korean Tarkov catalog가 identity authority
- production OCR field는 item-name only
- price/flea/slots/needed는 Item ID 이후 mapped data
- scan-time network 없음
- game memory read / DLL injection / packet interception 없음
- product-default automatic global OCR forced substitution 없음
- cross-frame OCR cache 없음

## Scanner 일반 UI — current

상단:

- `스캐너 ON/OFF`
- `설정`
- `고급`

하단 2분할:

- 왼쪽: 아이템 검색
- 오른쪽: Scanner 인식 로그

기존 전역 단축키는 유지한다.

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

일반 화면에서 1회 스캔 버튼을 제거했지만 one-shot 기능 자체를 제거한 것이 아니다.

## Scanner 아이템 검색

검색은 현재 메모리/local catalog를 사용한다.

scan-time network request를 만들지 않는다.

검색 결과:

- local cached icon
- official item name

선택 후:

- icon
- official name
- Tarkov Wiki
- flea 24h average
- best non-flea trader sell price + trader name where trusted
- current needed total

`current needed`는 inventory shortage가 아니라 `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal`이다.

## Mini Scanner — settings schema v6

고정 identity header:

- item icon
- official item name

사용자 표시/순서 설정 대상:

1. trader sell price
2. flea average price
3. trader price/slot
4. flea price/slot
5. current needed

기존 schema v5 이하 파일은 자동 migration한다.

보존 대상:

- enable state
- hotkeys
- display visibility
- window position
- font size
- user OCR substitutions

아이콘/이름은 v6부터 숨길 수 없다.

## Ground Truth correction

교정 창은 화면보다 큰 원본도 자동 축소해 전체를 보여 준다.

표시 배율은 저장 좌표계에 영향을 주지 않는다. 모든 Ground Truth ROI는 원본 image coordinate로 저장한다.

후보 선택:

- detail rectangle
- close-X
- magnifier
- item-name ROI

은 이미지 위 candidate box를 직접 클릭하는 방식이 기본이다.

Fallback:

- correct candidate 없음 → manual rectangle
- 실제 object 없음 → explicit `없음`

## 저장 Case 재교정

`교정 데이터 관리`에서 기존 Case를 다시 열 수 있다.

복원 source:

- `case.json`
- `full.png`
- `candidate_selection.json`

기존 Ground Truth와 candidate selection을 복원해 같은 editor에서 수정한다.

재저장은 동일 Case ID를 유지한다.

복원 실패 시 기존 Case를 삭제하거나 임의 변환하지 않는다.

## Diagnostics / retention

Reviewed Ground Truth는 자동 삭제하지 않는다.

Automatic unreviewed sample만 다음 범위로 제한한다.

```text
max age = 30 days
max cases = 300
max bytes = 512 MiB
recent protection = 2 hours
```

Corrupt/unknown metadata는 fail closed하여 보존한다.

로그는 bounded rotation한다.

## 현재 release package 계약

정식 ZIP:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

버전은 folder/ZIP 이름이 아니라 ProductVersion/tag/release metadata에 둔다.

CI는 `packaging/New-ReleasePackage.ps1`로 실제 ZIP을 만들고 구조를 검증한다.

## 검증 현황

### 성공한 중간 gate

CI `32700507526`:

- Desktop build SUCCESS
- 296 passed / 0 failed / 0 skipped
- Windows x64 publish SUCCESS
- Product UI smoke SUCCESS
- Scanner UI smoke SUCCESS
- Map / Factory / MiniMap smoke SUCCESS
- graceful shutdown SUCCESS
- artifact upload SUCCESS

이 성공 이후 version `1.6.0`, FIRST_RUN, stable release ZIP gate를 추가했으므로 최신 HEAD에서 최종 CI를 다시 통과해야 한다.

## release 직후 작업

```text
v1.6.0 실제 Tarkov 사용
→ 정상 representative result 확인
→ miss/wrong identity 즉시 교정
→ reviewed Ground Truth 축적
→ failure stage 분류
→ affected stage만 수정
→ full reviewed replay
→ REGRESSION = 0
→ PATCH 판단
```

Failure stage:

```text
capture
→ structural proposal recall/ranking
→ close-X semantic evidence
→ magnifier semantic evidence
→ inspect-header lock
→ item-name ROI
→ raw OCR
→ user substitution
→ catalog sanitation/matcher
→ visual recovery
→ Item ID
→ mapped presentation
→ overlay / stale-state timing
```

## 성능 개선 원칙

Telemetry evidence 없이 최적화하지 않는다.

가능한 최적화 대상:

- duplicate candidate/frame work
- unnecessary deep OCR
- bitmap copy/convert
- visual recovery early exit
- catalog recovery candidate work

금지:

- 성능을 이유로 header/matcher threshold 완화
- Ground Truth 없이 candidate cap 변경
- cross-frame OCR reuse
- stale previous Item을 현재 identity proof로 사용

## 작은 기술 부채

`src/JunhyunHelper.Desktop/Scanner/ScannerLatencyTypeAliases.cs`의 `ScannerDetectedCandidate` type alias는 여전히 작은 cleanup 후보다.

v1.6.0 release 안정성과 무관하므로 이번 MINOR release에서 억지로 제거하지 않는다.

향후 제거 시 full build/tests/publish/Product UI/Map/Scanner smoke를 다시 통과해야 한다.


## v1.7.3 Scanner Performance Pass — 2026-08-25

accuracy-neutral latency pass:

```text
continuous observation: 350 ms -> 200 ms
semantic retry: fixed 1200 ms -> 250 / 500 / 800 / 1200 ms adaptive backoff
OCR transport: PNG encode/decode round-trip -> direct BGRA SoftwareBitmap copy
verified detail: fresh small-rectangle semantic/title revalidation fast-path
```

fast-path는 새 identity를 결정하지 않는다. fresh close-X + magnifier + HEADER_FRAME_LOCKED + title signature가 모두 기존 verified frame과 일치할 때만 presentation을 유지하며, 불일치/실패 시 같은 cycle에서 full detector로 fallback한다.

변경 금지/유지: structural floor 0.34, trusted header floor 0.68, continuous candidate cap 8, one-shot candidate cap 12, matcher/deep OCR/visual recovery acceptance semantics, cross-frame OCR identity cache 금지.

결과 선택을 바꿀 수 있는 candidate early-exit, deep candidate 축소, visual recovery 생략은 이번 pass에서 의도적으로 제외한다.
