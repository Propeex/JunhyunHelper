# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-26  
상태: **v1.7.6 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

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

## 2. 현재 public stable

```text
version: v1.7.6
release source: 0e5240620ca0867a93f426824ff03374b93dcd1a
release CI run: 32868778549
release workflow run: 32869081513
release id: 376532454
asset: Junhyun-Helper.zip
asset bytes: 80,462,038
asset SHA-256: 1de4e203c7e219f1d995d4482fa903dc7544d208deee684b5b821f6b5c325e35
checksum asset: SHA256SUMS.txt
published: 2026-08-26 KST
```

GitHub release readback:

- tag `v1.7.6`
- target commit = exact release source
- draft = false
- prerelease = false
- latest stable
- required assets present

v1.7.6의 P0 목표였던 일부 데스크탑 Scanner 장시간 인식 지연은 문제 PC의 실측 자료로 root cause를 확인하고 수정했으며, 같은 PC의 Display Test와 실제 Tarkov에서 정상화를 재검증했다.

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

하단:

- `아이템 검색`
- `Scanner 로그`

`고급`에는 Display Test, current-result correction, correction dataset 관리, Scanner 성능 진단 자료 export가 있다.

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

자동 diagnostic Case는 정답이 아니다. user-reviewed Case만 Ground Truth다.

Full-pipeline regression은 reviewed Case의 보존된 `full.png`를 현재 production geometry/header/OCR/catalog path로 다시 실행한다.

결과:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

기존 정상 reviewed Case가 실패하면 평균 성능과 무관하게 REGRESSION이다.

Scanner performance support ZIP은 Ground Truth image/dataset을 포함하지 않는다. 최근 v1.7.6 문제-PC support log에서 관측된 Case는 `UNREVIEWED` 자동 Case였다. Reviewed dataset이 존재할 때에는 기존 full-pipeline replay의 `REGRESSION=0` 계약을 적용하며, reviewed evidence가 없을 때 값을 추정하지 않는다.

Automatic unreviewed diagnostic retention:

```text
max age = 30 days
max cases = 300
max bytes = 512 MiB
recent protection = 2 hours
```

Corrupt/unknown metadata는 preserve fail closed한다. Logs는 bounded rotation한다.

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

## 14. v1.7.6 CI / release proof

PR #185 root-cause fix code HEAD `d04f39697a4ea4d6ff4eabcb2acdc6bc535c8f9c` CI run `32866068233`:

```text
Desktop build: SUCCESS
Tests: 380 passed / 0 failed / 0 skipped
Windows x64 self-contained publish: SUCCESS
Product UI smoke: SUCCESS
Map / Factory / MiniMap smoke: SUCCESS
Graceful shutdown: SUCCESS
Release package verification: SUCCESS
Artifact upload: SUCCESS
```

PR #185 final HEAD도 전체 CI를 성공했고 main으로 merge했다.

Final release source:

```text
0e5240620ca0867a93f426824ff03374b93dcd1a
main CI run 32868778549: SUCCESS
Release run 32869081513: SUCCESS
```

Public asset:

```text
Junhyun-Helper.zip
bytes: 80,462,038
SHA-256: 1de4e203c7e219f1d995d4482fa903dc7544d208deee684b5b821f6b5c325e35
```

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

### v1.7.6 FIRST_RUN wording

공개 v1.7.6 ZIP의 `FIRST_RUN_KO.txt` 첫 줄 version identity는 정확하지만 본문 일부가 개발 중 작성된 `진단 후보` 표현을 유지한다.

- runtime/Scanner behavior에는 영향 없음
- release asset/hash에는 문제 없음
- published stable asset은 immutable 원칙에 따라 덮어쓰지 않음
- 다음 patch에서 현재 resolved 상태에 맞게 사용자 안내 문구 수정

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
