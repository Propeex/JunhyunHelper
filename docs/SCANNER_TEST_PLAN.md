# Scanner v1.2.2 Test Plan

기준일: 2026-08-23

상태: **`v1.2.2 PUBLIC VERIFIED / LIVE TARKOV CALIBRATION DEFERRED`**

이 문서는 v1.2.2의 deterministic regression gate와 실제 Tarkov 환경에서만 얻을 수 있는 후속 calibration을 분리합니다. 실제 관측 근거 없이 geometry/OCR/visual confidence threshold를 조정하지 않습니다.

## 1. 현재 공개 기준선

```text
release source: e3925cbc55215c7de0502c9b6b1ff1428d2f272b
final PR CI: 32590303579 — SUCCESS
exact-source release run: 32590701086 — SUCCESS
independent public finalizer: 32607942093 — SUCCESS
256 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.2-win-x64.zip
bytes: 80,302,910
SHA-256: 125d4a5b0e6db64f6772cc63c112f13cbcdac2fb7bc9ce501313ca2fc3645d7c
ProductVersion: 1.2.2+e3925cbc55215c7de0502c9b6b1ff1428d2f272b
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세 증거: `docs/RELEASE_1.2.2.md`, `docs/.release-v1.2.2-status.json`.

## 2. Release blocking gate

정식 Scanner PATCH는 최소 다음을 모두 통과해야 합니다.

1. exact merge source 고정
2. Windows Release build
3. 전체 automated tests 0 failure / 0 skip
4. Scanner Lab v3.8 structural regression
5. title anchor / magnifier exclusion regression
6. OCR character-policy regression
7. current official catalog matcher regression
8. Tarkov-font recovery parser/fallback smoke
9. font-cache generation/source consistency regression
10. bounded visual-cache contract
11. market-field regression
12. catalog cache-load/network-refresh ordering regression
13. one-shot/profile/GameMode lifecycle regression
14. Mini Scanner inventory-probe coalescing regression
15. Windows x64 self-contained single-file publish
16. exact ProductVersion / FIRST_RUN identity
17. package-root / debug-symbol / nested-archive / forbidden-dependency audit
18. actual published EXE Product UI / Scanner / Mini Scanner smoke
19. Main Map / Factory / MiniMap smoke
20. graceful close / clean portable root
21. Draft asset re-download verification
22. Draft-downloaded EXE smoke
23. public/latest verification
24. exact public tag-source verification
25. public asset re-download/checksum/package identity verification
26. public-downloaded EXE smoke

최신 실제 Tarkov 실행 E2E calibration은 release blocker가 아니며 공개 후 별도로 검증합니다.

## 3. Structural detector regression

유지해야 하는 계약:

- RED-X connected-component path
- RED-X 기반 outer-window reconstruction
- rectangle/edge fallback
- IoU candidate deduplication
- candidate limit 8
- structural floor 0.34
- geometry evidence만으로 Item ID 확정 금지
- adaptive 4x/6x/8x Korean OCR
- deep OCR fallback
- current official Korean catalog semantic validation
- confidence와 top1/top2 margin 유지

고정 회귀에는 cropped inspect window, full-screen inspect window, strong inner rectangle coexistence, no-RED-X fallback, uniform-frame fail-closed가 포함됩니다.

## 4. Title anchor / OCR regression

- close/X, magnifier, title-field evidence의 위치 관계 검증
- magnifier가 검출되면 실제 title ROI에서 magnifier pixels 제외
- anchor evidence가 부족하면 Scanner Lab geometry ROI로 fallback
- arbitrary screen strip OCR 금지
- diagnostic anchor score가 actual detector evidence를 보존
- current official Korean catalog에서 OCR allowed-character set 파생
- unexpected character는 corrupted OCR evidence
- exact official name 우선
- fuzzy confidence와 top1/top2 margin 유지
- ambiguous candidate fail closed
- successful primary semantic match를 visual recovery가 교체하지 않음

## 5. Tarkov-font recovery / cache regression

- current official full-item catalog만 candidate universe로 사용
- scan-time network 없음
- visual top1 score와 top1/top2 margin 모두 필요
- ambiguous visual candidate reject
- user-installed Tarkov resource를 read-only source로 사용
- source 전체를 단일 대형 managed buffer로 읽지 않음
- source path/length/last-write manifest 유지
- actual cached font bytes의 generation hash 유지
- generation 변경 시 stale rendered template 폐기
- OCR-guided/full-catalog caches는 bounded
- corrupt/unavailable font cache는 primary OCR을 fatal로 만들지 않음

## 6. Runtime / one-shot lifecycle regression

- 서로 다른 geometry signature가 연속돼도 stable hit 누적 금지
- verified geometry/title signature 유지 시 OCR 반복 억제
- presentation refresh는 OCR 없이 최신 데이터 bridge만 재계산
- one-shot 시작 전 continuous loop 실제 종료 await
- one-shot 중 duplicate invocation 직렬화/거부
- 종료 후 최신 사용자 state가 같은 mode를 여전히 요청할 때만 이전 mode 복원
- stale profile/GameMode monitor 결과가 과거 mode를 되살리지 않음
- title OCR과 inventory-context OCR은 shared serialized boundary 사용
- 종료 중 active font-aware operation이 끝난 뒤 resource 정리

## 7. Mini Scanner regression

- matched Item 정보만 표시
- 실제 mode에서 foreground/inventory context를 보수적으로 확인
- inventory context가 불확실하면 hidden
- inventory-context OCR probe 동시 최대 1개
- 반복 요청은 latest snapshot으로 coalesce
- item/visibility epoch 변경 시 stale result 폐기
- Topmost / no-activate 유지
- 전체 card drag surface
- negative multi-monitor coordinate 저장 가능
- MiniMap과 독립 lifecycle

## 8. Catalog identity / market regression

Identity health:

```text
item count >= 4000
AND every accepted item has valid Item ID/name
```

- regular / pve / pvp-season 지원
- Korean translation + English per-key fallback
- corrupt/missing cache reject
- requested mode missing 시 wrong-mode identity 사용 금지
- market coverage는 identity health와 분리
- raw `traderPrices`와 derived `sellFor` 지원
- best trader = valid non-flea RUB 가격 최댓값
- flea average = positive `avg24hPrice`
- invalid dimension은 slots/price-per-slot만 fail closed
- 4,000 valid identities + trader price 0개 허용
- 3,999 identity reject

## 9. v1.2.2 catalog mode-transition regression

`RefreshAsync`와 `LoadCacheAsync`는 같은 in-memory Scanner catalog state를 교체할 수 있으므로 동일 `_refreshGate` operation boundary를 사용합니다.

필수 계약:

- cache load와 network refresh 직렬화
- cross-mode clear는 refresh가 gate를 얻은 뒤 수행
- cache-load gate wait는 Scanner catalog lifetime cancellation과 연결
- older mode refresh가 newer mode cache load 뒤에 final state가 되는 상태 역전 금지
- matcher와 OCR character-policy가 서로 다른 mode catalog를 갖는 split state 금지

`ScannerCatalogConcurrencyTests.LoadCacheAsync_WaitsForInFlightRefreshAndKeepsNewestMode`는 다음 ordering을 강제합니다.

1. healthy PvE disk cache seed
2. older Regular refresh 시작 후 block
3. newer PvE cache load 요청
4. newer load가 operation gate 뒤에서 wait함을 확인
5. older refresh 완료
6. newer PvE load 완료
7. final LoadedMode가 PvE인지 확인
8. healthy catalog와 Item lookup 확인

이 테스트는 recognition threshold가 아니라 state-ordering correctness를 검증합니다.

## 10. 표시 데이터 regression

현재 필요한 수량:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

- Inventory 차감 shortage를 Scanner 필요 수량으로 사용하지 않음
- Needed Items에 없으면 0
- verified Item의 presentation snapshot을 주기적으로 재구성해 진행 변화 반영
- 최고 상점가, 플리 평균가, price-per-slot 계약 유지
- local icon cache만 읽고 scan 중 icon network 요청 없음

## 11. UI / diagnostics smoke

Scanner tab에 유지:

- Scanner/Test toggle
- one-shot scan
- recognition image
- one-shot hotkey 설정
- item catalog refresh
- display checkboxes
- recent recognition activity
- log clear

실제 published EXE에서 확인:

- safe defaults
- activity/log 생성
- current/rotated log clear
- latest in-memory recognition image clear
- diagnostic I/O failure가 Scanner fatal이 아님
- screenshot/raw pixels disk persistence 없음

## 12. Windows capture / performance contract

- Tarkov client-area discovery
- PrintWindow 우선 + invalid frame fallback
- multi-monitor enumeration
- Korean OCR boundary
- both continuous modes OFF면 background capture/OCR loop 없음
- one-shot은 explicit invocation에서만 수행
- PrintWindow sparse validation을 위해 전체 framebuffer managed copy를 추가 생성하지 않음
- visual template caches bounded
- Mini Scanner inventory OCR queue가 무제한 증가하지 않음

## 13. 공개 후 실제 Tarkov 검증

우선순위:

1. Borderless detail candidate 안정성
2. close/magnifier/title anchor 정확도와 diagnostic score
3. 실제 title ROI의 magnifier exclusion
4. Korean title OCR 및 corrupted-character 패턴
5. semantic vs visual recovery 선택
6. false positive / miss
7. Tarkov update 후 font generation 교체
8. 최고 상점가 / 플리 평균가 / RequiredTotal
9. one-shot과 profile/GameMode 변경의 공존
10. catalog refresh/load와 profile GameMode 전환 공존
11. Mini Scanner inventory gate와 OCR backlog
12. 장시간 CPU/memory/handle/OCR rate
13. Mini Scanner / MiniMap / Alt+Tab 공존

문제가 있으면 `scanner.log`와 `인식 이미지`를 근거로 capture → geometry → anchors → ROI → OCR/visual matcher → catalog → presentation → overlay 단계로 분리해 후속 PATCH에서 보정합니다. confidence/margin을 단순히 낮추는 방식으로 해결하지 않습니다.

## v1.3.0 verified gate

Final PR CI `32611343850 — SUCCESS`; 256/256 tests; Windows publish/root audit; rendered v1.3 Scanner UI and migration self-check; actual Product UI/Scanner/Mini Scanner/Main Map/Factory/MiniMap smoke; exact public tag source; SHA-256 `5880c71098d737b7ffd3447eb77a55195d09d76ea12be7ff79df4eb055ac8344`; independent public-downloaded EXE smoke SUCCESS.
