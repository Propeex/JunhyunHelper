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

현재 public stable은 **v1.1.5**입니다.

```text
version: v1.1.5 PUBLIC RELEASE / VERIFIED
release source / public tag: 3541bab6536ff91a00f394c4f7b03d5cbf112746
PR final candidate CI: 32493986403 — SUCCESS
initial exact-source release run: 32494487841
Draft resume/public verification run: 32495042444 — SUCCESS
independent public verification run: 32495225958 — SUCCESS
automated tests: 249 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.5-win-x64.zip
bytes: 80,269,429
SHA-256: dc31177ae1bd4d152453a010dffe6cbb1e6c1d2a4a7e2eb82fb7444fa99c0748
ProductVersion: 1.1.5+3541bab6536ff91a00f394c4f7b03d5cbf112746
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
independent public-downloaded EXE smoke: SUCCESS
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v1.1.5
```

```text
Desktop Version: 1.1.5
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.1.4 → v1.1.5 mandatory Game Content schema update: none
v1.1.4 → v1.1.5 user.db migration: none
Scanner settings schema: v2
```

v1.1.5는 Scanner/Mini Scanner의 overlay 동작, 가격/아이콘 데이터 신뢰성, 한국어 상세창 제목 인식, 안정성과 성능을 보강하는 PATCH입니다. 상세 검증 기록은 `docs/RELEASE_1.1.5.md`에 있습니다.

## 3. Scanner recognition 기준

Scanner Lab v3.8 multi-candidate architecture가 production structural 기준입니다.

```text
Tarkov / Display pixels
→ RED-X candidates + rectangle/edge fallback
→ IoU deduplication
→ 최대 8 structural candidates
→ title ROI
→ adaptive 4x/6x/8x Windows ko-KR OCR
→ current official Korean full-item catalog resolver
→ 필요 시 상위 3 candidate Deep OCR
→ 기존 semantic gate 실패 시에만 optional Tarkov-font visual recovery
→ semantic identity를 확정할 수 있는 candidate만 Item ID 확정
→ existing JunhyunHelper data
→ Mini Scanner
```

장기 원칙:

- geometry/structural score는 후보 생성·순위일 뿐 Item identity 아님
- current official Korean full-item catalog가 identity 권위
- font shape는 보조 evidence이며 독립 identity source가 아님
- 기존 matcher confidence/top1-top2 margin 완화 금지
- historical alias production 누적 금지
- false positive보다 miss 선호
- scan-time network 금지
- game memory / DLL injection / packet interception / icon identity 금지

## 4. Runtime 안정화

semantic OCR 전에 candidate가 연속 frame에서 실제로 이어지는지 확인합니다.

```text
frame N candidate GeometrySignature set
∩
frame N+1 candidate GeometrySignature set
!= empty
→ stable hit 누적
```

서로 다른 후보가 번갈아 나타나는 것만으로 stable 상태가 되지 않습니다. miss/mode/reset에서 signature history를 버립니다.

이미 Item ID가 확정된 뒤 verified bounds와 title signature가 유지되면 OCR 반복을 억제합니다. 대신 1초 간격으로 presentation snapshot만 재구성해 Quest/Hideout 진행에 따른 `RequiredTotal` 등 표시 데이터를 다시 연결합니다.

v1.1.5에서는 title OCR과 inventory-context OCR이 하나의 `SerializedScannerOcrEngine` 경계를 공유해 WinRT OCR 동시 실행을 막습니다. Item title runtime만 그 위에 `FontAwareScannerOcrEngine`을 사용하며 context detector에는 font recovery를 적용하지 않습니다.

## 5. v1.1.5 inspect-title font recovery

현재 상세보기 상단 Item 이름은 `ItemInfoWindowLabels._caption` TextMeshPro text입니다. 조사한 현재 Tarkov UI font stack은 Bender 계열 primary + `Noto Sans CJK KR` Korean fallback입니다.

font-aware path는 기존 OCR을 대체하지 않습니다.

```text
normal OCR success → 그대로 사용
normal 실패 → existing Deep OCR
Deep OCR도 기존 semantic gate 실패
→ OCR과 가까운 current official-name shortlist
→ Bender Regular/Bold + Noto Sans CJK KR fallback으로 렌더링
→ 실제 title ROI glyph mask와 비교
→ semantic + visual + top1/top2 margin 모두 통과할 때만 official name 복구
```

- 기존 semantic success는 font verifier가 변경/거부하지 않음
- short name은 더 엄격한 visual/combined/margin threshold 사용
- 약하거나 ambiguous하면 LOW_CONFIDENCE 유지
- font extraction/rendering 오류는 Scanner fatal이 아님

### Font acquisition boundary

Bender 바이너리는 public ZIP에 재배포하지 않습니다.

실행 중인 사용자 Tarkov의:

```text
EscapeFromTarkov_Data/resources.assets
```

를 read-only로 확인하고 embedded SFNT의 실제 family metadata를 검증한 뒤 필요한 Bender Regular/Bold와 Noto Sans CJK KR만 app-local Scanner cache에 복사합니다.

```text
%LocalAppData%/JunhyunHelper/scanner/fonts/
```

게임 디렉터리는 수정하지 않습니다. asset을 찾거나 읽거나 검증할 수 없으면 font recovery만 비활성화되고 기존 OCR-only path는 계속 동작합니다.

## 6. Scanner 가격 / 수량 / 아이콘 계약

가격:

```text
최고 상점가 = raw traderPrices의 유효 priceRUB 최댓값
             또는 raw가 없을 때 sellFor의 fleaMarket 제외 유효 priceRUB 최댓값
플리 평균가 = avg24hPrice > 0
slots = positive width * height
price/slot = valid price와 slots가 모두 있을 때만 계산
```

v1.1.5는 >=4,000개 valid Item 이름을 가지더라도 trader price coverage가 비정상적으로 비어 있으면 그 catalog를 unhealthy로 간주해 known-good cache를 덮지 못하게 합니다. raw `traderPrices` shape와 market-empty fixture가 회귀 테스트로 고정되어 있습니다.

현재 필요한 수량:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

Inventory 차감 부족량을 Scanner 의미로 사용하지 않습니다. Item이 NeededItems에 없으면 0입니다.

아이콘:

- scan 중 network 요청 금지
- Scanner는 local image-cache만 읽음
- 성공 decode/freeze icon은 process-memory cache 재사용
- explicit Game Content update 시 **전체 canonical Item catalog** icon을 prefetch
- 개별 image 실패는 전체 update fatal 아님

## 7. Mini Scanner — v1.1.5

- MiniMap과 독립 Window/service/settings/lifecycle
- matched Item snapshot만 표시
- standby/runtime/OCR/error/diagnostic text는 overlay에 표시하지 않고 hidden
- WPF `Topmost=True` + native `HWND_TOPMOST`
- `ShowActivated=false`, `WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`
- root card 전체가 drag hitbox
- cursor 강제 Arrow
- drag 종료 위치 저장, negative multi-monitor coordinate 허용
- Scanner display settings schema v2에서 icon/trader/trader-per-slot 기본 표시를 기존 install에 1회 정상화

### Inventory/stash auto visibility

실사용 overlay는 `ScannerInventoryContextDetector`가 foreground Tarkov client의 상단 UI band를 한국어 OCR해 semantic anchor를 **2개 이상** 확인할 때만 표시합니다.

현재 anchor set:

- `장비`
- `건강상태` / `건강 상태`
- `스킬`
- `지도`
- `종합정보` / `종합 정보`

uncertain/missing context는 hidden입니다. raw screenshot/pixel은 저장하지 않습니다. Display-test와 explicit preview는 deterministic 검증을 위해 이 gate를 bypass합니다.

Target은 Borderless/windowed Tarkov이며 exclusive fullscreen을 지원한다고 주장하지 않습니다.

## 8. Scanner UI / diagnostics

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

screenshot/raw pixel은 diagnostic에 저장하지 않습니다. v1.1.5에서 font/context 진단에는 다음 metadata event가 추가됩니다.

```text
inventory-context
title-font-extract-ready
title-font-extract-missing
title-font-extract-failed
title-font-verify-accepted
title-font-verify-rejected
title-font-recovery-error
```

## 9. Persistence

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/
%LocalAppData%/JunhyunHelper/scanner/fonts/
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

Game Content/User Progress/Scanner preferences/catalog/font cache/logs는 program package와 분리되어 있습니다.

## 10. Map / MiniMap

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

Map/MiniMap은 독립 subsystem이며 Quest projection만 JunhyunHelper와 bridge합니다. 안정적인 donor path는 구체적 defect/performance 근거 없이 broad refactor하지 않습니다.

## 11. Program Update / 배포

정식 release는 Draft-first입니다.

```text
exact release source
→ build/tests/publish/smoke
→ ZIP + SHA256SUMS
→ Draft release
→ Draft asset re-download verification
→ Draft-downloaded EXE smoke
→ public/latest
→ exact public tag verification
→ public asset re-download verification
→ public-downloaded EXE smoke
→ independent public re-verification
```

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

업데이트는 program-owned files만 교체하며 `%LocalAppData%/JunhyunHelper` 사용자 데이터는 건드리지 않습니다.

## 12. v1.1.5 release gate — 완료

- Windows Release build
- **249 automated tests / 0 failure / 0 skipped**
- Scanner Lab v3.8 geometry/title ROI regressions
- raw trader-price / market-health regressions
- Tarkov-title SFNT parser/fallback smoke in actual published EXE product smoke
- win-x64 self-contained single-file publish
- exact `ProductVersion` / `FIRST_RUN` verification
- actual published EXE Product UI / Mini Scanner / Scanner / Main Map / Factory / MiniMap smoke
- graceful shutdown / clean portable root
- Draft package re-download SHA/root/ProductVersion/FIRST_RUN verification
- Draft-downloaded EXE smoke
- public/latest transition
- tag `v1.1.5` = source `3541bab6536ff91a00f394c4f7b03d5cbf112746`
- public package re-download SHA/root/ProductVersion/FIRST_RUN verification
- public-downloaded EXE smoke
- independent public verification `32495225958` — SUCCESS

초기 exact-source run `32494487841`은 모든 build/test/publish/exact EXE smoke/ZIP 생성과 Draft 생성까지 성공한 뒤, Draft 상태에서 아직 존재하지 않는 public Git tag ref를 즉시 조회해 실패했습니다. GitHub Draft lifecycle에 대한 workflow ordering defect였으며 제품/패키지 defect가 아닙니다. `32495042444`가 기존 Draft를 재다운로드 검증한 뒤 public/latest 전환과 public package smoke를 완료했고, `32495225958`이 별도 runner에서 이를 다시 독립 검증했습니다.

## 13. 현재 남은 empirical validation

CI runner에는 실제 Tarkov 설치가 없으므로 다음은 최신 사용자 환경에서 계속 확인합니다.

- Borderless foreground inventory/stash Korean UI anchor 인식
- 현재 `resources.assets`에서 실제 Bender/Noto SFNT 추출
- 실제 Item title에서 font-aware recovery accept/reject 분포

이 영역은 public release blocker가 아니지만 실패 시 반드시 **overlay hidden 또는 OCR-only fallback**이어야 하며 false positive를 만들기 위해 matcher confidence/margin을 낮추지 않습니다.
