# Scanner v1.3.1 Test Plan

기준일: 2026-08-23

상태: **`v1.3.1 PUBLIC VERIFIED / LIVE TARKOV CALIBRATION ONGOING`**

이 문서는 deterministic release gate와 실제 Tarkov 환경에서만 얻을 수 있는 calibration을 분리합니다. 실제 관측 근거 없이 geometry/OCR/visual confidence threshold를 조정하지 않습니다.

## 1. 현재 공개 기준선

```text
release source: 028bfb600f4662962a0daac1dad04b570e018275
final PR CI: 32615869812 — SUCCESS
256 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.3.1-win-x64.zip
bytes: 80,310,221
SHA-256: 5c4b79cc5d373b4a28cbeb10be18b8369086b2ee9f0edc172530028dd71b1c3f
ProductVersion: 1.3.1+028bfb600f4662962a0daac1dad04b570e018275
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세 증거: `docs/RELEASE_1.3.1.md`, `docs/.release-v1.3.1-status.json`.

## 2. Release blocking gate

정식 Scanner PATCH는 최소 다음을 모두 통과해야 합니다.

1. exact merge source 고정
2. Windows Release build
3. 전체 automated tests 0 failure / 0 skip
4. Scanner Lab v3.8 structural regression
5. title-field / red-X / magnifier / first-glyph anchor regression
6. panel-left drift + first-glyph preservation regression
7. OCR character-policy regression
8. current official catalog matcher regression
9. semantic OCR success + visual corroboration fail-soft/correction contract
10. corrupted/empty OCR full-catalog visual recovery
11. font source/cache generation consistency
12. bounded visual caches
13. market/RequiredTotal field regression
14. catalog cache-load/network-refresh ordering regression
15. one-shot/profile/GameMode lifecycle regression
16. 3 global hotkey/settings migration/duplicate prevention regression
17. raw recognition-image export contract
18. Mini Scanner inventory-probe coalescing/stale-result regression
19. Windows x64 self-contained single-file publish
20. exact ProductVersion / FIRST_RUN identity
21. package-root / debug-symbol / nested-archive / forbidden-dependency audit
22. actual published EXE Product UI / Scanner / Mini Scanner smoke
23. Main Map / Factory / MiniMap smoke
24. graceful close / clean portable root
25. Draft asset re-download verification
26. Draft-downloaded EXE smoke
27. public/latest verification
28. exact public tag-source verification
29. public asset re-download/checksum/package identity verification
30. public-downloaded EXE smoke

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
- no-RED-X fallback
- uniform-frame fail-closed

## 4. v1.3.1 inspect-header / title ROI regression

필수 evidence:

- dark neutral title field
- right red close/X
- left magnifier
- following first title glyphs

필수 회귀 조건:

- panel left가 actual magnifier보다 일부 오른쪽으로 drift
- 실제 magnifier는 ring + hollow center + lower-right handle 형태
- magnifier 오른쪽에 smaller Korean-like first glyph 존재
- old bright-square heuristic이라면 first glyph를 잘못 선택할 가능성이 있는 구성

통과 조건:

1. 실제 magnifier가 선택될 것
2. first glyph가 magnifier로 선택되지 않을 것
3. title ROI에서 magnifier pixel이 제외될 것
4. first glyph가 title ROI에 포함될 것
5. right close/X 이전의 usable title width가 유지될 것
6. anchor evidence가 불충분하면 arbitrary strip이 아니라 검증된 geometry fallback을 사용할 것

## 5. OCR / character policy regression

- Windows `ko-KR` OCR primary
- adaptive 4x/6x/8x title enlargement
- deep OCR fallback
- current official Korean catalog에서 allowed-character set 파생
- unexpected character → corrupted evidence
- Korean-title contract의 Han ideograph → invalid evidence
- exact official name 우선
- fuzzy confidence + top1/top2 margin 유지
- ambiguous candidate fail closed
- 임의 문자 치환으로 confidence 상승 금지

## 6. v1.3.1 semantic-success visual corroboration regression

기존 v1.2.x의 “semantic success는 절대 visual이 교체하지 않는다” 계약은 v1.3.1에서 supersede되었습니다.

새 계약:

- OCR semantic result와 visual result가 같은 Item ID → OCR 유지
- font unavailable → OCR success 유지
- renderer/cache failure → OCR success 유지
- visual score/margin 부족 → OCR success 유지
- strict visual evidence가 다른 current official Item ID를 명확히 지목 → current catalog 안에서만 correction 허용
- arbitrary Item/text 생성 금지
- visual layer 자체가 ambiguous하면 correction 금지

이 회귀는 visual corroboration 추가로 healthy OCR success가 불필요하게 miss로 바뀌지 않는지 함께 확인해야 합니다.

## 7. Corrupted/empty OCR visual recovery

- candidate universe = current official full-item catalog
- scan-time network 없음
- plausible OCR은 semantic shortlist에만 보조 사용
- empty/bad OCR은 full-catalog visual path 허용
- top1 visual score + top1/top2 margin 필요
- ambiguous candidate reject
- 결과 Item ID는 current catalog에 존재해야 함

## 8. Tarkov font source/cache regression

- public package에 game font binary 포함 금지
- user-installed Tarkov `resources.assets`를 read-only source로 사용
- source 전체를 단일 대형 managed buffer로 읽지 않음
- Bender/Noto required payload cache
- `font-cache.json` source identity
- actual cached font bytes generation key
- source generation 변경 시 loaded/rendered generation 재사용 금지
- partial extraction을 completed generation으로 인정하지 않음
- corrupted font cache는 visual path만 fail-soft, primary OCR은 유지
- template/aspect/mask cache bounded

## 9. Scanner catalog / data regression

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

## 10. v1.3.0 one-shot / hotkey / image-export regression

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
- one-shot duplicate invocation은 overlap하지 않음
- one-shot 종료 후 current requested mode만 restore
- one-shot 제품 버튼이 Scanner tab에 존재하지 않음
- DisplayTest one-shot은 모든 연결 display를 한 번만 처리

Image export:

- 최신 실제 recognition source frame export
- PNG
- diagnostic overlay 미합성
- 자동 screenshot 저장 없음
- `로그 삭제`가 사용자 export PNG를 삭제하지 않음

## 11. Mini Scanner regression

- matched item data only
- Topmost / no-activate
- full-card drag surface
- Arrow cursor
- inventory/stash probe single active
- latest request coalesce
- old item/context epoch result reject
- uncertain foreground/inventory context → hidden
- title OCR과 inventory OCR serialized

## 12. Product UI / version label regression

- Scanner ON/OFF / Test OFF safe defaults
- `단축키 설정`
- `아이템 목록 최신화`
- `로그 삭제`
- removed one-shot buttons가 다시 나타나지 않음
- MainWindow top status lane에 `VersionText` 존재
- 표시값은 actual assembly informational/product version에서 파생
- `+commit` build metadata는 user-facing label에서 제외
- 특정 버전을 XAML에 하드코딩하지 않음

## 13. Package / public verification

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

검증:

- PDB 없음
- unexpected root DLL/archive 없음
- known-unused legacy dependency 없음
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
- Public re-download checksum/root/ProductVersion/FIRST_RUN
- Public EXE smoke

## 14. Live Tarkov calibration protocol

사용자는 실제 게임에서 기본적으로 다음 loop를 사용합니다.

```text
아이템 상세창 열기
→ one-shot 또는 Scanner recognition
→ 결과 확인
→ miss/wrong identity면 다음 scan 전에 인식 원본 PNG 저장
→ 실제 아이템 이름과 결과를 기록
→ 필요 시 scanner.log 함께 전달
```

분류:

1. capture/window 문제
2. detail structural candidate 문제
3. inspect-header anchor/title ROI 문제
4. OCR 문제
5. font visual corroboration/recovery 문제
6. catalog identity 문제
7. presentation price/RequiredTotal 문제
8. continuous timing/stale-state 문제

특히 wrong identity는 miss보다 높은 우선순위로 처리합니다.

## 15. 현재 다음 단계

v1.3.1 자동/공개 검증은 완료됐습니다. 다음 Scanner 개선은 실제 Tarkov에서 수집되는 PNG/log evidence를 우선 사용합니다. 충분한 evidence가 없는 상태에서 threshold를 임의로 완화하거나 detector를 대규모 재작성하지 않습니다.
