# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-26  
상태: **v1.7.8 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다.

2026-08-26 제품 사용자는 현재 요구사항 범위의 준현 헬퍼가 완성 상태에 도달했다고 최종 확정했다. 마지막 집중 개발 영역이었던 Scanner 역시 실사용 검증을 통과해 기능 개발 단계에서 유지보수 단계로 전환했다. 기본 프로젝트 모드는 **유지보수**이며, 새 기능은 사용자가 새로운 제품 요구사항으로 명시적으로 결정할 때만 시작한다.

주요 기능:

- GameMode별 Profile / User Progress
- Quest availability / prerequisite / special trader / profile-variable
- Hideout
- Needed Items / FIR·일반 Inventory / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Map + MiniMap
- Game Content 안전 업데이트 / image cache
- 사용자 동의형 Program Update
- Scanner + Mini Scanner
- Scanner Ground Truth 교정 / diagnostic dataset / full-pipeline regression

Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper`는 불완전한 프로토타입이며 새 제품 요구사항의 권위가 아니다. 유지할 기능, 검증된 데이터/자산, 구현 아이디어와 시행착오 참고 용도로만 사용한다.

제품 완성 및 유지보수 전환의 공식 결정은 `docs/DECISION_PRODUCT_COMPLETE_2026-08-26.md`에 기록한다.

v1.7.7의 Scanner durable-data ownership 및 Scanner/Map 공통 hotkey 계약은 `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`에 기록한다.

v1.7.8의 레이드 inspect-header ownership 회귀 수정 및 `현재 결과 교정` 메인 배치 결정은 `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`에 기록한다.

## 2. 현재 public stable

```text
version: v1.7.8
release source: 3ba9d99c43ad143dbc8329e7d29b1d01da335b06
release CI run: 32888653630
release workflow run: 32888935292
release id: 376650517
asset: Junhyun-Helper.zip
asset bytes: 80,469,671
asset SHA-256: 3716d2d3c6d3c9ce2f87c759aac74f6b56b483a09016339c0d8bb6d3bc67e730
checksum asset: SHA256SUMS.txt
published: 2026-08-26 KST
```

GitHub release readback:

- tag `v1.7.8`
- target commit = exact release source `3ba9d99c43ad143dbc8329e7d29b1d01da335b06`
- draft = false
- prerelease = false
- `releases/latest` = v1.7.8
- `Junhyun-Helper.zip` + `SHA256SUMS.txt` present
- ZIP GitHub asset digest = `sha256:3716d2d3c6d3c9ce2f87c759aac74f6b56b483a09016339c0d8bb6d3bc67e730`

v1.7.6의 P0 목표였던 일부 데스크탑 Scanner 장시간 인식 지연은 문제 PC의 실측 자료로 root cause를 확인하고 수정했으며, 같은 PC의 Display Test와 실제 Tarkov에서 정상화를 재검증했다.

v1.7.7은 그 인식 알고리즘을 변경하지 않고 실사용에서 확인된 Scanner 자동 교정 Case 폭증, 반복 실패 로그 가시성, Scanner/Map 단축키 정책 불일치를 수정한 유지보수 PATCH다.

v1.7.8은 사용자 reviewed 레이드 Case 8건에서 확인된 inspect-header 수평 소유권 오류를 수정했다. 실패 6건은 OCR 오인식이 아니라 `HEADER_CLOSE_NOT_LOCKED` / `TITLE_ANCHOR_INCOMPLETE`로 OCR 이전에 중단됐고, 레이드 인벤토리 수평선이 상세창 header와 이어져 기존 fallback이 실제 상세창보다 47~132px 왼쪽까지 header를 소유한 것이 원인이었다. 새 recovery는 강한 `RED_X_CANDIDATE >= 0.90`인 경우에만 사용하며 기존 red close-X, magnifier, neutral header, dark title field, text evidence와 최종 `HEADER_FRAME_LOCKED >= 0.68`을 모두 요구한다.

현재 Scanner 성능 알고리즘은 완료 상태다. 새로운 runtime evidence가 없는 한 성능을 목적으로 recognition threshold/candidate cap/recovery acceptance를 더 변경하지 않는다.

## 3. 아키텍처

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

책임:

- **Core**: canonical domain, deterministic calculation, Quest 조건 규칙, Scanner structural/identity/matcher/presentation 정책
- **Application**: 사용자 use case, authoritative mutation, workspace orchestration
- **Infrastructure**: HTTP/source parsing, SQLite/file persistence, Game Content/Scanner/update I/O
- **Desktop**: WPF UI, presentation, Scanner capture/OCR/runtime/diagnostics, Map bridge
- **Map/MiniMap donor**: 제한적 compile-link 예외. donor updater/content ownership/hidden command는 사용하지 않음

## 4. 사용자 데이터 / persistence

대표 저장 위치:

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/map-product-settings.json(.bak)
%LocalAppData%/JunhyunHelper/ammo-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
%LocalAppData%/JunhyunHelper/logs/
```

원칙:

- portable executable 옆에 mutable user data/log 생성 금지
- Program Update가 user.db, content/image cache, Map/Ammo/Scanner settings, Scanner logs/diagnostics를 교체하지 않음
- user-reviewed Scanner Ground Truth는 자동 삭제하지 않음
- Scanner logs와 Ground Truth dataset lifetime을 분리
- 정상 Scanner monitoring은 durable automatic correction Case를 생성하지 않음

## 5. Game Content / Scanner catalog update

일반 Game Content와 Scanner catalog는 사용자에게 별개의 관리 절차를 요구하지 않는다.

```text
remote Game Content
→ validate/build candidate
→ integrity validation
→ general content activation
→ Scanner official item/market catalog refresh
→ status report
```

안전 계약:

- fail closed / Last Known Good 유지
- partial payload 보호
- canonical dangling reference 검증 유지
- Scanner refresh만 실패하면 healthy general Game Content를 rollback하지 않음
- Scanner scan-time에는 network request 없음

## 6. Scanner production contract

Scanner는 게임 프로세스 내부 데이터를 읽지 않는 화면 기반 Tarkov UI recognizer다.

```text
Tarkov window / Display Test pixels
→ capture
→ detail rectangle proposals
→ red close-X + magnifier + neutral inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog sanitation / normalization
→ conservative catalog matching
→ optional deep OCR / tight-title retry
→ optional current-pixel Tarkov-font visual corroboration/recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional user correction / Ground Truth
```

핵심 불변식:

```text
structural floor = 0.34
trusted HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
deep OCR candidate limit = existing value
continuous scan target interval = 200 ms
semantic retry interval = 1200 ms
```

추가 계약:

- false positive보다 miss 선호
- geometry/structural score는 proposal evidence이며 Item identity proof가 아님
- full semantic gate 전 production OCR identity path 진입 금지
- valid magnifier + red close-X evidence 필수
- current official Korean Tarkov full-item catalog가 identity authority
- ambiguity / low confidence는 fail closed
- production OCR field는 item-name 하나
- price/flea/slots/needed는 Item ID 확정 이후 mapped presentation data
- stale Item ID를 current identity proof로 사용 금지
- cross-frame OCR/visual identity cache를 Item proof로 사용 금지
- reviewed Ground Truth 없이 threshold/candidate cap 완화 금지
- game memory read / DLL injection / packet interception / process hook 금지

## 7. Capture / one-shot / semantic header

Tarkov capture:

```text
EscapeFromTarkov process/window
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ PrintWindow 우선
→ invalid/empty이면 exact client screen rectangle fallback
```

Display Test는 같은 recognition pipeline을 사용하며 real continuous Scanner와 상호 배타적이다.

기본 hotkey:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

v1.7.7부터 Scanner와 configurable Map actions는 **primary non-modifier key + optional Ctrl/Alt/Shift** 공통 gesture contract를 사용한다. Bare key도 허용한다. Windows modifier는 지원하지 않는다.

Map의 bare NumPad0~5는 기존 direct floor selection에 예약하고, modifier가 붙은 NumPad gesture는 configurable Map hotkey로 사용할 수 있다. 기존 Map key-only 설정은 modifier `None`으로 migration한다.

v1.7.6에서는 one-shot `ScanOnceAsync`를 explicit worker에 배치해 global-hotkey WPF message-pump가 capture/OCR synchronous setup을 직접 수행할 수 있는 경로를 제거했다.

Mini Scanner와 WPF status presentation은 dispatcher marshalling을 유지한다.

## 8. OCR / matcher / visual recovery

Primary recognizer는 Windows OCR이다.

```text
locked item-name ROI
→ normal/deep OCR variants
→ raw OCR preservation
→ optional user substitution
→ current-catalog sanitation / normalization
→ exact-first conservative matching
→ bounded recovery
→ optional visual corroboration/recovery
```

- raw OCR forensic evidence는 별도 보존
- user substitution은 single ordered pass
- 기본 substitution list는 empty
- recursive/cyclic/chained reprocessing 없음
- catalog-impossible glyph를 특정 문자로 전역 강제 치환하지 않음
- ambiguous candidate는 fail closed

v1.7.6은 실제 `OcrEngine.RecognizeAsync` 호출 단위를 측정하고 slow-empty actual WinRT call을 bounded health policy로 보호한다.

### Current-cycle visual evidence reuse

v1.7.6은 동일 Scanner latency cycle 안에서 다음이 동일한 visual corroboration 결과를 재사용한다.

- cycle ID
- title bitmap dimensions
- exact current-pixel SHA-256
- OCR text

cycle이 변경되면 즉시 폐기한다. 이는 cross-frame identity cache가 아니며 동일한 현재-frame deterministic proof를 후보마다 반복 계산하지 않기 위한 최적화다.

candidate count와 visual acceptance semantics는 변경하지 않았다.

## 9. Scanner P0 성능 결함 — 해결 증거

수정 전 문제 PC 대표 Tarkov cycle:

```text
end-to-end                  12,540.77 ms
OCR normal                      12.26 ms
actual WinRT RecognizeAsync     10.57 ms
visual recovery             12,306.61 ms / 16 calls
```

Windows OCR 자체가 아니라 동일 current-frame visual proof가 후보별로 반복되며 latency를 증폭한 것이 root cause였다.

수정 후 동일 문제 PC:

```text
Display Test — 하프 마스크
10,840.877 ms → 70.603 ms
약 99.35% 감소

Display Test — USB 보안 플래시 드라이브
12,686.278 ms → 1,354.775 ms
약 89.32% 감소
```

실제 `TarkovWindow` 성공 12건의 `ReadingTitle → ShowingItem`:

```text
minimum: 38.07 ms
median:  63.92 ms
maximum:  1.05 s
mean:    211.47 ms
```

retained OCR-active full Scanner cycles 11건:

```text
minimum end-to-end: 178.04 ms
median:              210.82 ms
maximum:              517.74 ms
```

추가 evidence:

```text
visual-cycle-cache-hit: 73
repeated visual-recovery: effectively 0~0.01 ms
WPF dispatcher stall: 0
actual WinRT OCR: generally ~4~13 ms
LocalAppData diagnostic append: sub-ms
```

사용자 실사용 평가에서도 수정 후 속도가 충분히 만족스러운 수준임을 확인했다.

약 1초의 어려운 deep/recovery 사례는 허용 가능한 bounded recovery cost로 본다. latency만 줄이기 위해 OCR variant, candidate count, matcher/visual threshold를 변경하지 않는다.

## 10. Scanner mapped presentation / item search

Item ID 확정 후 local trusted data:

- highest valid non-flea trader RUB price
- best trader name where trustworthy
- flea positive `avg24hPrice`
- slots = positive `width × height`
- trader price/slot
- flea price/slot
- required total = `NeededItems[itemId].RequiredTotal`

Inventory shortage는 Scanner의 `필요 개수` 의미가 아니다.

Scanner item search는 같은 current full-item catalog와 local presentation data를 사용하며 검색 순간 network request를 만들지 않는다.

## 11. Scanner UI / Mini Scanner

Scanner 일반 화면 primary actions:

- `스캐너 ON/OFF`
- `설정`
- `고급`
- `현재 결과 교정`

하단:

- `아이템 검색`
- `Scanner 로그`

`현재 결과 교정`은 메모리에 보존된 최신 exact Scanner frame을 바로 기존 `ScannerCorrectionWindow`로 연다.

`고급`에는 Display Test, correction dataset 관리, Scanner 성능 진단 자료 export가 있다.

Mini Scanner:

- Topmost
- ShowActivated=false
- ShowInTaskbar=false
- full-surface drag
- matched Item presentation only
- stale epoch reject

identity가 확인된 Item presentation은 transient recognition miss에 대해 기존 sticky retention 정책을 유지한다. Scanner OFF/suspend/profile/catalog/vision fatal state에서는 즉시 hide한다.

## 12. Ground Truth / correction / regression

Root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

v1.7.7부터 정상 Scanner runtime은 automatic diagnostic Case를 durable storage에 만들지 않는다.

```text
current capture / recognition evidence
→ latest exact frame in memory
→ bounded runtime text log
→ user explicitly chooses correction
→ user explicitly saves
→ reviewed durable Ground Truth
```

상세창 미탐지, header lock 실패, OCR/matcher 실패, ambiguity, 반복 stationary failure만으로 correction dataset이 증가하지 않는다.

사용자가 저장한 reviewed Case만 Ground Truth다. Full-pipeline regression은 reviewed Case의 보존된 `full.png`를 현재 production geometry/header/OCR/catalog path로 다시 실행한다.

결과:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

기존 정상 reviewed Case가 실패하면 평균 성능과 무관하게 REGRESSION이다.

Scanner performance support ZIP은 Ground Truth image/dataset을 포함하지 않는다. v1.7.6 문제-PC support log에서 관측된 Case 51개는 모두 `UNREVIEWED / automatic_sample`이었으며 이 legacy 자동 Case 축적이 7GB 이상 증가 문제의 원인으로 확인됐다.

v1.7.7 legacy cleanup은 다음을 모두 증명할 때만 자동 Case를 삭제한다.

```text
retention = automatic_sample
review_status = unreviewed
recent write safety window = 5 minutes
pre-delete metadata/state recheck = required
```

reviewed/manual/corrupt/unknown/state-changed Case는 preserve fail closed한다. 새 버전은 정상 monitoring에서 새로운 durable automatic Case를 만들지 않는다.

사용자 activity feed의 동일 실패는 30초 window로 collapse한다. 지원용 `scanner.log`는 기존 bounded rotation/retention을 유지하며 Ground Truth lifetime과 분리한다.

Reviewed dataset이 존재할 때에는 기존 full-pipeline replay의 `REGRESSION=0` 계약을 적용하며, reviewed evidence가 없을 때 값을 추정하지 않는다.

## 13. Diagnostics / telemetry

Scanner latency stages:

- capture
- rectangle-proposal
- semantic-header
- ocr-normal
- ocr-deep
- visual-recovery
- catalog-matching
- presentation
- end-to-end

v1.7.6 additional trace:

- Scanner cycle start/end
- serialized OCR wait
- exact image-key CopyPixels/SHA
- OCR enlarge/deep variant creation
- BGRA conversion
- OCR CopyPixels
- SoftwareBitmap creation
- each actual WinRT RecognizeAsync
- current-cycle visual cache hit
- Tarkov font source probe
- WPF dispatcher stall/recovery

fine-grained trace는 bounded in-memory storage를 사용한다.

`Scanner > 고급 > Scanner 성능 진단 자료 내보내기`는 환경/성능 trace/log를 ZIP 하나로 저장한다. Ground Truth image, profile DB, game account information은 포함하지 않는다.

## 14. v1.7.8 CI / release proof

PR #188 final HEAD `52fbeaf6d56cf01631325ba3d65a1f018e9eb138` CI run `32886379050`:

```text
Desktop build: SUCCESS
Tests: 380 passed / 0 failed / 0 skipped
Windows x64 self-contained publish: SUCCESS
Product UI / Scanner smoke: SUCCESS
Map / Factory / MiniMap smoke: SUCCESS
Graceful shutdown: SUCCESS
Release package verification: SUCCESS
Artifact upload: SUCCESS
```

Final release source:

```text
3ba9d99c43ad143dbc8329e7d29b1d01da335b06
main CI run 32888653630: SUCCESS
Release run 32888935292: SUCCESS
release id: 376650517
```

Public asset:

```text
Junhyun-Helper.zip
asset id: 529666832
bytes: 80,469,671
SHA-256: 3716d2d3c6d3c9ce2f87c759aac74f6b56b483a09016339c0d8bb6d3bc67e730
```

Release workflow은 성공한 exact main CI artifact의 ProductVersion, `FIRST_RUN_KO.txt`, package checksum을 검증한 뒤 v1.7.8을 공개했다. GitHub `releases/latest` readback도 v1.7.8이다.

상세 공개 기록은 `docs/RELEASE_1.7.8.md` 및 `docs/.release-v1.7.8-status.json`에 둔다. 이전 v1.7.7 공개 증거는 `docs/RELEASE_1.7.7.md`에 역사 기록으로 유지한다.

## 15. Release / Program Update contract

배포 형태:

```text
Windows x64
.NET 10 self-contained single-file
installer 없음
관리자 권한 불필요
```

stable user package:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

ZIP/folder name에는 version을 넣지 않는다. Version identity는 Desktop project / ProductVersion / tag / GitHub Release metadata에 둔다.

Program Update는 public stable의 exact asset/checksum/package identity를 검증한 뒤 program-owned files만 transaction 교체한다. 사용자 LocalAppData는 유지한다.

## 16. Known issue / technical debt

### Diagnostic OCR adapter

v1.7.6의 fine-grained WinRT timing을 위해 diagnostic adapter가 production `ScannerLab38OcrEngine`의 기존 engine instance를 재사용한다. 사용자 검증된 실행 behavior를 바꾸는 cleanup은 v1.7.6 직전에 강행하지 않았다.

향후 구조 정리 시 exact telemetry/health policy를 raw OCR owner로 이동하고 adapter/reflection 의존을 제거하되, Ground Truth와 문제-PC performance evidence를 유지해야 한다.

## 17. 유지보수 상태

현재 요구사항 범위의 제품 개발은 종료됐다. 활성 기능 개발 backlog를 기본적으로 만들지 않는다.

향후 작업은 다음 조건에서만 시작한다.

- 사용자가 새로운 제품 요구사항을 명시적으로 결정함
- 실사용 defect/regression이 확인됨
- Tarkov UI/데이터 변경으로 기존 기능이 깨짐
- Windows/.NET/platform 또는 외부 데이터 소스 변화로 호환성 문제가 생김
- 보안 또는 데이터 무결성 문제를 수정해야 함

Scanner 관련 새 문제가 생기면:

1. exact support bundle / Case 확보
2. failure stage 측정
3. affected stage만 수정
4. reviewed Ground Truth가 있으면 full replay REGRESSION=0 확인
5. full Windows CI/publish/smoke/package 검증

추측 기반 threshold/candidate-cap 완화, 선제적 Scanner 성능 재설계, 코드 미관만을 위한 위험한 대규모 refactor는 하지 않는다.
