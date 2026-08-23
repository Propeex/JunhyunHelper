# Scanner v1.3.3 Test Plan

기준일: 2026-08-23

상태: **`v1.3.3 PUBLIC VERIFIED / LIVE TARKOV CALIBRATION ONGOING`**

이 문서는 deterministic release gate와 실제 Tarkov 환경에서만 얻을 수 있는 calibration을 분리합니다. 실제 관측 근거 없이 geometry/OCR/visual confidence threshold를 조정하지 않습니다.

## 1. 현재 공개 기준선

```text
release source/tag: 41bf5b8374ba774866aab4b60a25376d9b5548c2
final PR CI: 32625223009 — SUCCESS
263 passed / 0 failed / 0 skipped
release run: 32625403609 — SUCCESS
asset: Junhyun-Helper-v1.3.3-win-x64.zip
bytes: 80,314,373
SHA-256: 0771d3c7dee5a8f19904d52eeedc7b9abbd6027a7b000255ebd33c296bc2186f
ProductVersion: 1.3.3+41bf5b8374ba774866aab4b60a25376d9b5548c2
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세 증거: `docs/RELEASE_1.3.3.md`, `docs/.release-v1.3.3-status.json`.

## 2. Release blocking gate

정식 Scanner PATCH는 최소 다음을 모두 통과해야 합니다.

1. exact merge source 고정
2. Windows Release build
3. 전체 automated tests 0 failure / 0 skip
4. Scanner Lab v3.8 structural candidate regression
5. actual inspect-header frame lock regression
6. first-glyph title-start ownership 금지 regression
7. incomplete header lock fail-closed regression
8. OCR raw/sanitized matcher input separation regression
9. current-catalog character/symbol policy regression
10. current official catalog matcher regression
11. bounded unique one-edit safety regression
12. semantic OCR success + visual corroboration fail-soft/correction contract
13. corrupted/empty OCR full-catalog visual recovery
14. font source/cache generation consistency
15. bounded visual caches
16. market/RequiredTotal field regression
17. catalog cache-load/network-refresh ordering regression
18. one-shot/profile/GameMode lifecycle regression
19. 3 global hotkey/settings migration/duplicate prevention regression
20. raw recognition-image export contract
21. Mini Scanner inventory-probe coalescing/stale-result regression
22. Windows x64 self-contained single-file publish
23. exact ProductVersion / FIRST_RUN identity
24. package-root / debug-symbol / nested-archive / forbidden-dependency audit
25. actual published EXE Product UI / Scanner / Mini Scanner smoke
26. Main Map / Factory / MiniMap smoke
27. graceful close / clean portable root
28. Draft asset re-download verification
29. Draft-downloaded EXE smoke
30. public/latest verification
31. exact public tag-source verification
32. independent public asset re-download/checksum/package identity verification
33. public-downloaded EXE smoke

최신 실제 Tarkov 실행 E2E calibration은 공개 후 계속합니다. 다만 이미 확보한 live failure evidence는 해당 PATCH의 regression에 반영해야 합니다.

## 3. Structural detector regression

유지 계약:

- RED-X connected-component path
- RED-X 기반 outer-window reconstruction
- rectangle/edge fallback
- IoU candidate deduplication
- candidate limit 8
- structural floor 0.34
- geometry evidence만으로 Item ID 확정 금지
- stable quantized geometry before continuous semantic recognition

기본 회귀:

- cropped inspect window
- full-screen inspect window
- strong inner rectangle coexistence
- no-RED-X candidate fallback
- uniform-frame fail-closed

구조 후보 생성과 **OCR identity 허용**은 별개입니다. v1.3.3에서는 구조 candidate가 존재해도 inspect-header frame lock이 완성되지 않으면 OCR로 진행하지 않습니다.

## 4. v1.3.3 inspect-header frame-lock regression

공식 live evidence:

- 사용자 제공 실제 Tarkov 2048×1280 상세창 12개
- raw screenshot 자체는 저장소에 커밋하지 않음
- 비식별 상대 측정값: `docs/.scanner-v1.3.3-header-evidence.json`

필수 구조:

- right red close/X
- long neutral top frame
- bounded frame-left search-icon lane
- 13px-class magnifier bright core
- ring/hollow/handle morphology
- dark title field
- title text presence

통과 조건:

1. `ScannerInspectHeaderLock`가 실제 frame-relative 구조를 사용하고 screen-center absolute heuristic에 의존하지 않을 것
2. 12개 measured header geometry를 synthetic regression으로 모두 재생할 것
3. title lane 내부 decoy ring/glyph가 real magnifier를 대체하지 않을 것
4. fragmented first glyph가 title ROI left edge를 오른쪽으로 이동시키지 않을 것
5. title ROI는 magnifier 오른쪽에서 시작하고 red close/X 이전에 끝날 것
6. `HEADER_FRAME_LOCKED`가 아니면 refiner score가 0.47 이하일 것
7. runtime은 `HEADER_FRAME_LOCKED` + `TitleAnchorScore >= 0.68`을 다시 요구할 것
8. magnifier/close/title bounds 중 하나라도 불완전하면 OCR identity path에 진입하지 않을 것

## 5. OCR / sanitation / matcher regression

- Windows `ko-KR` OCR primary
- adaptive 4x/6x/8x title enlargement
- deep OCR fallback
- raw Windows OCR을 진단용으로 유지
- current official Korean catalog에서 allowed character/symbol set 파생
- current catalog 밖 punctuation/symbol → matcher evidence에서 제거
- Korean-title contract의 Han ideograph → invalid evidence
- sanitation 후 실제 matcher input을 raw OCR과 별도 기록/표시
- exact official name 우선
- fuzzy confidence + top1/top2 margin 유지
- ambiguous candidate fail closed
- 임의 문자 치환으로 confidence 상승 금지

### Bounded one-edit recovery

```text
normalized official length >= 7
AND edit distance == 1
AND candidate unique over complete current catalog
AND candidate is ordinary matcher top1
AND best - global runner-up >= 10 percentage points
```

multi-edit low-confidence OCR을 percentage만으로 허용하지 않습니다.

## 6. Tarkov-font visual regression

- public package에 game font binary 포함 금지
- user-installed Tarkov `resources.assets`를 read-only source로 사용
- source/font generation 변경 시 stale rendered cache 재사용 금지
- partial/corrupt font cache는 visual path만 fail-soft
- candidate universe = current official full-item catalog
- visual top1 + top1/top2 margin 필요
- semantic success와 visual이 다를 때 strict visual evidence가 명확할 때만 current catalog 안에서 correction
- font unavailable / renderer error / ambiguous → healthy OCR success 유지
- arbitrary Item/text 생성 금지
- template/aspect/mask cache bounded

## 7. Scanner catalog / data regression

Identity health:

```text
accepted item count >= 4000
+ non-empty Item ID
+ non-empty official name
```

Catalog transition:

- `LoadCacheAsync` / `RefreshAsync` 동일 operation gate
- older GameMode refresh가 newer state를 overwrite하지 못함
- shutdown cancellation boundary 유지

표시 데이터:

- highest trader = valid non-flea RUB maximum
- flea average = positive `avg24hPrice`
- slots = positive `width × height`
- price/slot = valid price + slots일 때만
- needed count = `NeededItems[itemId].RequiredTotal`
- market/dimension missing은 identity가 아니라 해당 field만 fail closed

## 8. One-shot / hotkey / image-export regression

Scanner settings schema v4.

기본키:

- `Ctrl+Shift+F10` in-game one-shot
- `Ctrl+Shift+F11` test one-shot
- `Ctrl+Shift+F12` Scanner ON/OFF

검증:

- MainWindow lifetime global registration
- 각 command 변경/비활성화
- 동일 gesture 중복 차단
- schema v3 one-shot custom gesture 보존
- old user gesture가 신규 default와 충돌하면 신규 command만 fallback
- one-shot duplicate invocation overlap 금지
- one-shot 종료 후 current requested mode만 restore
- one-shot 제품 버튼이 Scanner tab에 존재하지 않음
- DisplayTest one-shot은 모든 연결 display를 한 번만 처리

Image export:

- 최신 실제 recognition source frame export
- PNG
- diagnostic overlay 미합성
- 자동 screenshot 저장 없음
- `로그 삭제`가 사용자 export PNG를 삭제하지 않음

Diagnostics:

- raw OCR 표시
- sanitized matcher input 표시
- header reason/score, magnifier/close lock 상태 표시

## 9. Mini Scanner regression

- matched item data only
- Topmost / no-activate
- full-card drag surface
- Arrow cursor
- inventory/stash probe single active
- latest request coalesce
- old item/context epoch result reject
- uncertain foreground/inventory context → hidden
- title OCR과 inventory OCR serialized

## 10. Product UI / version regression

- Scanner ON/OFF / Test OFF safe defaults
- `단축키 설정`
- `아이템 목록 최신화`
- `인식 이미지`
- `로그 삭제`
- removed one-shot buttons가 다시 나타나지 않음
- MainWindow version label은 actual assembly informational/product version에서 파생
- `+commit` build metadata는 user-facing label에서 제외
- 특정 버전을 XAML에 하드코딩하지 않음

## 11. Package / public verification

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
- ProductVersion = release version + exact source SHA
- FIRST_RUN first line = release identity
- actual EXE rendered Product UI smoke
- Scanner/Mini Scanner smoke
- Main Map / Factory / MiniMap smoke
- graceful shutdown
- portable root runtime pollution 없음
- Draft re-download checksum/root/ProductVersion/FIRST_RUN
- Draft EXE smoke
- Public latest/tag exact source
- independent Public re-download checksum/root/ProductVersion/FIRST_RUN
- Public EXE smoke

## 12. v1.3.3 완료 증거

- final PR CI `32625223009`: SUCCESS
- 263/263 tests
- 12-case header lock regression: SUCCESS
- exact-source release run `32625403609`: SUCCESS
- public/latest: VERIFIED
- exact tag source: VERIFIED
- public ZIP SHA-256: `0771d3c7dee5a8f19904d52eeedc7b9abbd6027a7b000255ebd33c296bc2186f`
- public bytes: `80,314,373`
- independent public re-download/package verification: VERIFIED
- public-downloaded EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke: SUCCESS

## 13. Live Tarkov calibration protocol

```text
아이템 상세창 열기
→ one-shot 또는 Scanner recognition
→ 결과 확인
→ miss/wrong identity면 다음 scan 전에 인식 원본 PNG 저장
→ 실제 아이템 이름과 결과 기록
→ 필요 시 scanner.log 함께 전달
```

분류:

1. capture/window 문제
2. detail structural candidate 문제
3. inspect-header frame lock/title ROI 문제
4. OCR 문제
5. catalog sanitation/matcher 문제
6. font visual corroboration/recovery 문제
7. presentation price/RequiredTotal 문제
8. continuous timing/stale-state 문제

wrong identity는 miss보다 높은 우선순위로 처리합니다.

## 14. 현재 다음 단계

v1.3.3 자동/공개 검증은 완료됐습니다. 다음 Scanner 개선은 실제 Tarkov에서 수집되는 PNG/log evidence를 우선 사용합니다. 충분한 evidence가 없는 상태에서 threshold를 임의로 완화하거나 unrelated Scanner 기능을 추가하지 않습니다.
