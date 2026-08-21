# Scanner v1.1.4 Test Plan

기준일: 2026-08-21

상태: **`PUBLIC RELEASE GATE PASSED / v1.1.4 PUBLIC VERIFIED / LIVE TARKOV E2E POST-RELEASE`**

이 문서는 v1.1.4의 자동/Windows/public release gate와 공개 후 실제 Tarkov 검증을 분리합니다.

## 1. Release blocking gate — 완료

1. Windows Release Desktop build
2. 전체 automated tests 0 failure
3. Scanner Lab v3.8 structural/title ROI regression
4. current catalog/matcher regression
5. market-field regression
6. win-x64 self-contained single-file publish
7. ProductVersion = 1.1.4 + exact release source
8. FIRST_RUN first line = v1.1.4
9. package root/dependency/PDB/nested-archive audit
10. actual published EXE startup
11. rendered Product UI + Scanner UI assertions
12. Scanner `로그 삭제` end-to-end smoke
13. Main Map / Factory / MiniMap runtime smoke
14. graceful Main Window close/process exit
15. Draft ZIP/checksum/package/ProductVersion verification
16. Draft-downloaded EXE smoke
17. public/latest 전환
18. exact public tag → release source SHA verification
19. public ZIP/checksum/package/ProductVersion/FIRST_RUN 재검증
20. public-downloaded EXE smoke
21. public-downloaded EXE graceful shutdown

실제 최신 Tarkov 실행 E2E는 DEC-051에 따라 public release blocker가 아니며 사용자 환경에서 후속 검증합니다.

## 2. Scanner Lab v3.8 recognition regression

반드시 유지:

- RED-X connected-component path
- RED-X anchored outer-window reconstruction
- rectangle/edge fallback
- IoU candidate deduplication
- candidate limit 8
- structural floor 0.34
- geometry alone으로 final inspect 확정 금지
- adaptive 4x/6x/8x Windows ko-KR OCR
- 상위 3개 deep OCR fallback
- current official Korean catalog semantic validation
- confidence/top1-top2 margin 유지

고정 구조 회귀:

- cropped `Ophthalmoscope 검안경`: outer inspect/title ROI
- full `Water 0.6L 물병` screenshot: central inspect/title ROI
- strong inner rectangle coexistence
- no RED-X rectangle fallback
- uniform frame fail-closed

## 3. Candidate 안정화 — v1.1.4

검증 계약:

- candidate가 없으면 stable hit = 0
- 서로 다른 geometry signature만 이어지면 2-hit stable로 승격하지 않음
- 연속 candidate 집합에 같은 quantized `GeometrySignature`가 있을 때만 stable hit 누적
- mode/change/miss/reset에서 previous signature history clear
- verified bounds + title signature가 유지되면 OCR 반복 억제
- title/geometry 변화 시 기존 Item clear 후 재검증

## 4. Catalog / market data

Full catalog:

- 4,000개 이상 Korean item load
- regular / pve / pvp-season
- Korean translation + English per-key fallback
- corrupt/missing cache reject
- requested mode missing 시 wrong-mode identity 사용 금지
- AtomicJson backup recovery

Market regression:

- 4,000개 fixture 전체 Item 순회
- 각 Item official Korean name 확인
- 복수 trader가 있으면 non-flea `priceRUB` 최댓값 선택
- `source == fleaMarket`는 최고 상점가 계산에서 제외
- flea row가 모든 trader보다 높아도 trader price에 사용하지 않음
- 플리 평균가는 `avg24hPrice`만 사용
- zero/missing `avg24hPrice` → null
- invalid/non-positive dimension → slots 0, price/slot null
- valid price + slots → integer price/slot

## 5. 현재 필요한 수량

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

검증:

- Inventory 차감 부족량을 Scanner 의미로 사용하지 않음
- NeededItems에 없으면 0
- Item ID가 동일해도 presentation snapshot을 주기적으로 다시 구성
- Quest/Hideout 진행으로 `RequiredTotal`이 바뀌면 같은 상세창을 열어 둔 상태에서 최신 값 반영
- presentation refresh 자체는 OCR을 재실행하지 않음

## 6. Icon / performance

- scan-time icon HTTP 없음
- local image-cache만 사용
- invalid/missing local icon은 해당 icon 표시만 omit
- 성공적으로 decode/freeze한 동일 stableId+URL icon은 process memory cache 재사용
- presentation refresh가 같은 PNG file decode를 반복하지 않음

## 7. Scanner UI

유지:

- `스캐너 OFF`
- `테스트 OFF`
- `아이템 목록 최신화`
- 7개 display checkbox
- recent recognition activity
- activity header 우측 `로그 삭제`

없어야 함:

- 위치 편집/초기화
- Foundation verification/preview controls
- 상시 설명문

## 8. 로그 삭제 end-to-end smoke

실제 published EXE에서:

1. 기존 Scanner diagnostic/activity baseline clear
2. `ocr-result`와 `match-result` diagnostic 생성
3. recent activity가 생성됐는지 확인
4. `scanner.log`가 실제 생성됐는지 확인
5. `scanner.log.1` 회전 로그도 실제 파일로 생성
6. rendered `로그 삭제` Button Click event 실행
7. recent activity = 0 확인
8. `scanner.log` 없음 확인
9. `scanner.log.1` 없음 확인

삭제 I/O 실패를 Scanner runtime fatal로 확대하지 않는 코드 경계도 유지합니다.

## 9. Windows capture/runtime

Windows runner에서:

```text
dotnet build src/JunhyunHelper.Desktop/JunhyunHelper.Desktop.csproj -c Release
dotnet test tests/JunhyunHelper.Tests/JunhyunHelper.Tests.csproj -c Release
```

확인:

- EscapeFromTarkov process/window discovery
- GetClientRect + ClientToScreen
- PrintWindow + CopyFromScreen fallback
- multi-monitor enumeration
- Windows ko-KR OCR boundary
- WPF BitmapSource handoff
- real/test mutual exclusion
- both OFF → no capture/OCR loop

## 10. Mini Scanner

- Topmost
- ShowActivated=false
- WS_EX_NOACTIVATE / WS_EX_TOOLWINDOW
- direct left-drag
- drag 종료 위치 저장
- negative monitor coordinate
- MiniMap과 독립 lifecycle
- ON standby / OFF hidden

## 11. Public release verification — 완료

최종 release source:

```text
833ac66c522632a695d106bd7ca9b1d6bfc030dc
```

최종 검증 흐름:

```text
exact source checkout
→ build
→ 247 tests
→ publish
→ package audit
→ actual EXE smoke including Scanner log clear
→ ZIP + SHA256SUMS
→ Draft release
→ Draft asset re-download/hash/ProductVersion
→ Draft-downloaded EXE smoke
→ public/latest
→ exact tag verification
→ public asset re-download/hash/root/ProductVersion/FIRST_RUN
→ public-downloaded EXE smoke
```

최종 공개 패키지:

```text
asset: Junhyun-Helper-v1.1.4-win-x64.zip
bytes: 80,253,044
SHA-256: 6d7a4646032c91a66d66ceac0d78b197dd112e78fa9c7a6e99d7092febc2cb54
ProductVersion: 1.1.4+833ac66c522632a695d106bd7ca9b1d6bfc030dc
PR final CI: 32475893012 — SUCCESS
public verification run: 32476952938 — SUCCESS
public-downloaded EXE smoke: SUCCESS
```

첫 exact-source Draft-first workflow `32476391800`은 public 전환 뒤 태그 재조회 refspec 문자열 버그로 마지막 자동 단계가 failure 처리되었습니다. 그 전의 제품/package/Draft gate는 모두 통과했으며, 독립 public verification run에서 exact tag 및 공개 자산 검증을 다시 수행했습니다.

최종 run/source/hash/bytes는 `docs/RELEASE_1.1.4.md`와 `docs/STATE.md`에 고정되어 있습니다.

## 12. 공개 후 실제 Tarkov 검증

우선순위:

1. 실제 Borderless detail candidate 안정성
2. current Korean title OCR
3. candidate semantic selection
4. 다양한 Item의 최고 상점가
5. 플리 평균가
6. 현재 필요한 수량
7. false positive / miss
8. 장시간 CPU/memory/handles/OCR rate
9. Mini Scanner / MiniMap / Alt+Tab 공존

문제가 있으면 `scanner.log`와 최근 인식 기록을 근거로 후속 PATCH에서 보정합니다.
