# RELEASE 1.1.4 — Scanner 안정성·데이터 신뢰성 보강

기준일: 2026-08-21

상태: **`RELEASE CANDIDATE — FINAL WINDOWS GATE IN PROGRESS`**

## 목적

v1.1.4는 v1.1.3에서 복원한 Scanner Lab v3.8 recognition architecture를 변경하지 않고, 실사용 전 점검에서 발견한 런타임 안정성·표시 데이터 신뢰성·진단 UX를 보강하는 PATCH 릴리즈입니다.

새로운 Item identity heuristic이나 느슨한 matcher를 추가하지 않습니다. false positive보다 miss를 선호하는 기존 안전 계약을 유지합니다.

## 분석 결과와 변경

### 1. 상세창 후보 안정화

기존 런타임은 연속 프레임에 어떤 structural candidate든 존재하면 안정화 hit를 누적할 수 있었습니다. 최종 semantic gate 때문에 곧바로 잘못된 Item ID가 확정되지는 않지만, 서로 다른 후보가 번갈아 등장할 때 불필요한 OCR/상태 흔들림을 만들 수 있었습니다.

v1.1.4에서는 연속 관측 candidate 집합 사이에 동일한 quantized `GeometrySignature`가 겹칠 때만 안정화 hit를 누적합니다. 후보가 사라지면 signature history도 초기화합니다.

### 2. 검증된 상세창의 표시 데이터 갱신

동일 title signature를 계속 보고 있을 때 OCR은 반복하지 않는 기존 최적화를 유지합니다. 대신 확정된 Item ID의 presentation snapshot을 1초 간격으로 다시 구성합니다.

따라서 같은 상세창을 열어 둔 동안에도 Quest/Hideout 진행에 의해 `ItemsWorkspace.Plan.NeededItems[].RequiredTotal`이 변하면 Mini Scanner의 `현재 필요한 수량`이 갱신됩니다.

### 3. 아이콘 decode 최적화

Scanner는 scan 중 네트워크로 아이콘을 받지 않습니다. 기존 local image-cache만 읽습니다.

v1.1.4에서는 이미 성공적으로 decode/freeze한 Scanner 아이콘을 process-local memory cache에 보관해 presentation refresh마다 같은 PNG를 다시 열고 decode하지 않습니다.

### 4. 가격 데이터 검증 강화

Scanner catalog의 market contract를 회귀 테스트로 고정했습니다.

- 최고 상점가: `sellFor` 중 `source == fleaMarket`을 제외한 `priceRUB` 최댓값
- 플리마켓 평균가: `avg24hPrice`
- 슬롯: positive `width * height`
- 가격/슬롯: 위 값과 유효 슬롯이 모두 있을 때만 계산
- 0/invalid market/dimension 값은 해당 필드만 비움

복수 상인이 존재하고 flea row가 더 높은 가짜 데이터에서도 최고 상점가가 flea를 선택하지 않는 것을 검증합니다.

### 5. 필요한 개수 검증

Scanner는 부족량이나 현재 보유량 차감값을 별도 계산하지 않습니다.

```text
Item ID
→ ScannerItemPresentationService
→ ScannerDataContext.ItemsWorkspace
→ Plan.NeededItems[itemId].RequiredTotal
→ Mini Scanner 현재 필요한 수량
```

같은 Item이 Needed Items에 없으면 0입니다. v1.1.4의 presentation refresh로 상세창을 닫지 않아도 최신 `RequiredTotal`을 다시 읽습니다.

### 6. 로그 삭제

Scanner 탭의 `최근 인식 기록` 헤더 우측 상단에 `로그 삭제` 버튼을 추가했습니다.

삭제 대상:

- 현재 process의 최근 인식 activity history
- `%LocalAppData%/JunhyunHelper/logs/scanner.log`
- `%LocalAppData%/JunhyunHelper/logs/scanner.log.1`

로그 파일 삭제 실패는 Scanner 인식/runtime fatal로 확대하지 않습니다. 실행 중 새 진단 이벤트가 발생하면 새 `scanner.log`가 다시 생성될 수 있습니다.

실제 published EXE smoke에서 activity/log를 만든 뒤 버튼을 클릭해 UI history와 두 log path가 모두 비워지는지 검사합니다.

## 유지한 Scanner Lab v3.8 계약

변경하지 않았습니다.

- RED-X + rectangle/edge candidate generation
- IoU deduplication
- 최대 8개 candidate semantic validation
- structural floor 0.34
- adaptive 4x/6x/8x Windows ko-KR OCR
- 상위 3개 deep OCR fallback
- current official Korean full-item catalog
- exact-first conservative matcher
- fuzzy confidence/top1-top2 margin
- historical alias production 누적 금지
- scan-time network 금지
- game memory / DLL injection / packet interception / icon identity 금지

## 자동 검증

v1.1.4에서 자동 테스트는 **247개**입니다.

추가 market regression:

- 복수 trader 중 RUB 최고가
- fleaMarket row 최고 상점가에서 제외
- `avg24hPrice` 독립 사용
- invalid/zero market/dimension fail-closed

기존 Scanner Lab v3.8 geometry/title ROI regressions도 그대로 유지합니다.

최종 release source/run/ZIP bytes/SHA-256/ProductVersion은 public release 검증 완료 후 이 문서에 기록합니다.

## 실제 Tarkov 후속 검증

최신 Tarkov Borderless 실사용 E2E는 기존 DEC-051 정책대로 public release blocker가 아닙니다. 실제 raid 환경에서 다음을 계속 확인합니다.

1. 상세창 structural candidate 안정성
2. current Korean title OCR
3. semantic selection / false positive / miss
4. 다양한 Item에서 최고 상점가·플리 평균가·현재 필요한 수량 표시
5. 장시간 CPU/memory/handle/OCR rate
6. Mini Scanner / MiniMap / Alt+Tab 공존

문제가 발견되면 `scanner.log`와 최근 인식 기록을 근거로 후속 PATCH에서 보정합니다.
