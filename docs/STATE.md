# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-21

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램입니다.

핵심 기능:

- GameMode별 Profile / User Progress
- Quest availability / Hideout / Needed Items / Inventory
- Items / Ammo
- Map + MiniMap
- Game Content 안전 업데이트
- 사용자 동의형 Program Update
- Scanner + Mini Scanner

Runtime GPT/AI 의존성은 없습니다.

## 2. 현재 릴리즈 상태

현재 public stable은 **v1.1.4**입니다.

```text
version: v1.1.4 PUBLIC RELEASE / VERIFIED
release source: 833ac66c522632a695d106bd7ca9b1d6bfc030dc
PR final CI: 32475893012 — SUCCESS
exact-source Draft-first release run: 32476391800
public verification run: 32476952938 — SUCCESS
automated tests: 247 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.4-win-x64.zip
bytes: 80,253,044
SHA-256: 6d7a4646032c91a66d66ceac0d78b197dd112e78fa9c7a6e99d7092febc2cb54
ProductVersion: 1.1.4+833ac66c522632a695d106bd7ca9b1d6bfc030dc
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.1.4
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.1.3 → v1.1.4 mandatory Game Content update: none
v1.1.3 → v1.1.4 user.db migration: none
```

v1.1.4는 새 Scanner 기능 추가가 아니라 v1.1.3의 인식 안정성, 표시 데이터 신뢰성, 진단 UX와 성능을 보강하는 PATCH입니다. 상세 검증 기록은 `docs/RELEASE_1.1.4.md`에 있습니다.

## 3. Scanner recognition 기준

Scanner Lab v3.8에서 실제로 성공했던 구조가 production 기준입니다.

```text
Tarkov / Display pixels
→ RED-X candidates + rectangle/edge fallback
→ IoU deduplication
→ 최대 8 structural candidates
→ title ROI
→ adaptive 4x/6x/8x Windows ko-KR OCR
→ current official Korean full-item catalog resolver
→ 필요 시 상위 3개 candidate deep OCR
→ semantic gate를 통과한 candidate만 inspect window로 확정
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

장기 원칙:

- geometry/structural score는 후보 생성·순위일 뿐 Item identity 아님
- matcher confidence/top1-top2 margin 완화 금지
- historical alias production 누적 금지
- false positive보다 miss 선호
- scan-time network 금지
- game memory / DLL injection / packet interception / icon identity 금지

## 4. v1.1.4 Scanner hardening

### Candidate stability

v1.1.3은 어떤 후보든 연속 두 tick에 존재하면 안정화 hit가 누적될 수 있었습니다. v1.1.4는 연속 candidate 집합 사이에 **같은 quantized `GeometrySignature`가 겹칠 때만** 2-hit 안정화 조건을 누적합니다.

### Verified presentation refresh

같은 verified bounds/title signature를 보는 동안 OCR 반복은 계속 억제합니다. 대신 1초 간격으로 같은 Item ID의 presentation snapshot만 다시 구성합니다. 동일 상세창을 열어 둔 동안에도 Quest/Hideout 상태가 바뀌면 `RequiredTotal` 기반 현재 필요한 수량이 갱신됩니다.

### Icon optimization

Scanner local icon은 기존 image-cache만 읽으며 network를 사용하지 않습니다. 성공적으로 decode/freeze한 동일 icon은 process-local memory cache에서 재사용합니다.

## 5. Scanner 가격 / 수량 계약

가격:

```text
최고 상점가 = sellFor 중 source != fleaMarket인 유효 priceRUB 최댓값
플리 평균가 = avg24hPrice > 0
slots = positive width * height
price/slot = valid price와 slots가 모두 있을 때만 계산
```

4,000개 전체 카탈로그 fixture를 순회하는 회귀 검증으로 각 아이템의 한국어 이름, 최고 상점가, 플리 평균가, 슬롯, 슬롯당 가격 투영을 검사합니다. 복수 trader + 더 높은 flea row에서도 flea가 최고 상점가에 섞이지 않음을 고정했습니다. invalid market/dimension은 해당 field만 fail-closed합니다.

현재 필요한 수량:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

Inventory 차감 부족량을 Scanner 의미로 사용하지 않습니다. Item이 NeededItems에 없으면 0입니다.

## 6. Scanner UI / diagnostics

Scanner 탭:

```text
상단 bar
  왼쪽: 스캐너 / 테스트
  오른쪽: 아이템 목록 최신화
↓
표시 정보 checkboxes
↓
최근 인식 기록                         로그 삭제
```

`로그 삭제`는 process activity history와 다음 파일을 함께 삭제합니다.

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

삭제 실패는 Scanner runtime fatal이 아닙니다. screenshot/raw pixel은 diagnostic에 저장하지 않습니다.

실제 packaged EXE smoke에서 diagnostic/activity와 `scanner.log.1`까지 생성하고 rendered `로그 삭제` 버튼을 눌러 memory history와 두 log path가 모두 삭제되는지 검사합니다.

## 7. Mini Scanner

- MiniMap과 독립 Window/service/settings/lifecycle
- ON 즉시 standby, Item 확정 시 정보 표시
- OFF hidden
- Topmost / `WS_EX_NOACTIVATE` / `WS_EX_TOOLWINDOW`
- visible 상태 direct left-drag
- drag 종료 위치 atomic settings 저장
- negative multi-monitor coordinate 허용

## 8. Persistence

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

Game Content/User Progress/Scanner preferences/catalog/logs는 program package와 분리되어 있습니다.

## 9. Map / MiniMap

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

Map/MiniMap은 독립 subsystem이며 Quest projection만 JunhyunHelper와 bridge합니다. 안정적인 donor path는 구체적 defect/performance 근거 없이 broad refactor하지 않습니다.

## 10. Program Update / 배포

정식 release는 Draft-first입니다.

```text
exact release source
→ build/tests/publish/smoke
→ ZIP + SHA256SUMS
→ Draft release
→ Draft asset re-download verification
→ Draft-downloaded EXE smoke
→ public/latest
→ exact tag verification
→ public asset re-download verification
→ public-downloaded EXE smoke
```

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

업데이트는 program-owned files만 교체하며 `%LocalAppData%/JunhyunHelper` 사용자 데이터는 건드리지 않습니다.

## 11. v1.1.4 release gate — 완료

- Windows Release build
- **247 automated tests / 0 failure / 0 skipped**
- Scanner Lab v3.8 detector/title ROI regressions
- Scanner market-field 4,000-item fixture regression
- win-x64 self-contained single-file publish
- ProductVersion/FIRST_RUN exact version check
- actual published EXE Product UI / Scanner log clear / Main Map / Factory / MiniMap smoke
- graceful shutdown / clean portable root
- Draft package re-download SHA-256/root/ProductVersion verification
- Draft-downloaded EXE smoke
- public/latest transition
- tag `v1.1.4` and source `833ac66c522632a695d106bd7ca9b1d6bfc030dc` identical verification
- public package re-download SHA-256/root/ProductVersion/FIRST_RUN verification
- public-downloaded EXE smoke / normal shutdown

첫 exact-source release workflow `32476391800`은 public 전환 자체까지 성공한 뒤 태그 재조회 PowerShell refspec 문자열 버그로 마지막 자동 단계가 실패했습니다. 그 전에 exact-source build, tests, package audit, Draft 검증과 Draft-downloaded EXE smoke는 모두 성공했습니다. 독립 public verification run `32476952938`에서 누락된 exact-tag/public-package/public-downloaded-EXE 검증을 전부 다시 수행해 최종 승인했습니다.

## 12. 실제 Tarkov 후속 검증

최신 Tarkov Borderless E2E는 DEC-051에 따라 public release blocker가 아니며 사용자 환경에서 계속 검증합니다.

우선 확인:

1. current detail structural candidate 안정성
2. current Korean title OCR
3. semantic candidate selection
4. 다양한 Item의 최고 상점가 / 플리 평균가 / 현재 필요한 수량
5. false positive / miss
6. 장시간 CPU/memory/handle/OCR rate
7. Mini Scanner / MiniMap / Alt+Tab 공존

문제가 있으면 `scanner.log`와 최근 인식 기록을 기준으로 후속 PATCH를 진행합니다.

## 13. 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / fail-closed availability |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / user validated baseline |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.1.4 public package verified |
| Scanner | **v1.1.4 public verified / Scanner Lab v3.8 contract preserved / live Tarkov validation ongoing** |

## 14. 새 작업 시작 순서

1. `README.md`
2. `docs/STATE.md`
3. `docs/PRODUCT.md`
4. `docs/DECISIONS.md`
5. `docs/DEVELOPER_REFERENCE.md`
6. `docs/ARCHITECTURE.md`
7. 관련 전문 문서와 코드/tests/PR

현재 코드가 존재한다는 이유만으로 제품 요구사항으로 추정하지 않습니다. 사용자 확정 요구사항과 공식 문서가 우선합니다.
