# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-26  
상태: **v1.7.5 PUBLIC STABLE / v1.7.6 SCANNER PERFORMANCE FIX VERIFIED — RELEASE FINALIZATION**

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다.

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

## 2. 공개 stable과 현재 개발 상태

현재 public stable/latest는 **v1.7.5**다.

```text
public stable: v1.7.5
exact release source: 215541a694459e9484716c4942a436c26defe919
stable asset: Junhyun-Helper.zip
stable bytes: 80,450,225
stable SHA-256: 6706f12e63caa2039cf3f89c6823b457d125e43f8af47779082caa843282923f
```

현재 개발 목표는 **v1.7.6**이다.

v1.7.6의 P0 목표였던 일부 데스크탑의 Scanner 5~13초 장시간 인식 지연은 root cause를 실측한 뒤 수정했고, 같은 문제 PC의 Display Test와 실제 Tarkov에서 성능 정상화를 확인했다.

따라서 현재 단계는:

```text
P0 Scanner long stall: RESOLVED
performance algorithm work: CLOSED unless new evidence appears
v1.7.6: release finalization
```

관련 문서:

- `docs/CURRENT_SCANNER_WORK.md`
- `docs/DECISION_V1.7.5_OCR_ENVIRONMENT_GUARD_2026-08-25.md`
- `docs/DECISION_V1.7.6_SCANNER_STALL_DIAGNOSTICS_2026-08-25.md`
- `docs/RELEASE_NOTES_V1.7.6.md`

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

현재 pinned Map donor는 저장소 submodule pin을 권위로 사용한다.

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
continuous scan interval = 350 ms
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

v1.7.6에서는 one-shot `ScanOnceAsync` 실행을 explicit worker에 배치해 global-hotkey WPF message-pump가 capture/OCR synchronous setup을 직접 수행할 수 있는 경로를 제거했다.

Mini Scanner와 WPF status presentation은 기존 dispatcher marshalling을 유지한다.

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

cycle이 변경되면 즉시 폐기한다. 이는 cross-frame identity cache가 아니며 동일한 현재-frame deterministic proof를 후보마다 반복 계산하지 않기 위한 성능 최적화다.

candidate count와 visual acceptance semantics는 변경하지 않았다.

## 9. Scanner P0 성능 결함 — root cause와 해결 증거

문제 데스크탑의 첫 진단 bundle에서 대표 Tarkov cycle:

```text
end-to-end                  12,540.77 ms
OCR normal                      12.26 ms
actual WinRT RecognizeAsync     10.57 ms
visual recovery             12,306.61 ms / 16 calls
```

Windows OCR 자체가 아니라 동일 current-frame visual proof가 후보별로 반복되며 latency를 증폭한 것이 root cause였다.

또한 Tarkov font provider의 unavailable retry check가 expensive process/source discovery 뒤에 있어 optional source lookup이 반복될 수 있는 구조가 있었다.

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

### 성능 정책

P0 long-stall은 해결 완료로 판정한다.

약 1초의 어려운 deep/recovery 사례는 허용 가능한 bounded recovery cost로 본다. 추가로 latency만 줄이기 위해 OCR variant, candidate count, matcher/visual threshold를 변경하지 않는다.

새로운 실측 regression이 없는 한 v1.7.6 이전에 성능 알고리즘을 다시 변경하지 않는다.

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

## 11. Scanner 일반 UI / Mini Scanner

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

대표 evidence:

- full.png
- detail/title/processed ROI
- annotated image
- case.json
- candidate_selection.json
- raw/substituted/normalized OCR
- Item ID / official name
- confidence / second / margin
- matcher top candidates
- structural/header evidence
- mapped presentation
- user Ground Truth

자동 diagnostic Case는 정답이 아니다. user-reviewed Case만 Ground Truth다.

Full-pipeline regression은 reviewed case의 보존된 `full.png`를 현재 production geometry/header/OCR/catalog path로 다시 실행한다.

결과:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

기존 정상 reviewed Case가 실패하면 평균 성능과 무관하게 REGRESSION이다.

**중요:** Scanner 성능 support ZIP은 개인정보/대용량 이미지 최소화를 위해 Ground Truth image/dataset을 포함하지 않는다. 따라서 성능 support bundle만으로 reviewed Ground Truth regression을 실행할 수 없다.

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

fine-grained trace는 bounded in-memory storage를 사용해 진단 자체가 synchronous file-I/O 병목을 만들지 않도록 한다.

`Scanner > 고급 > Scanner 성능 진단 자료 내보내기`는 환경/성능 trace/log를 ZIP 하나로 저장한다. Ground Truth image, profile DB, game account information은 포함하지 않는다.

Automatic unreviewed diagnostic retention:

```text
max age = 30 days
max cases = 300
max bytes = 512 MiB
recent protection = 2 hours
```

Corrupt/unknown metadata는 preserve fail closed한다. Logs는 bounded rotation한다.

## 14. v1.7.6 검증 현황

Root-cause fix code HEAD:

```text
d04f39697a4ea4d6ff4eabcb2acdc6bc535c8f9c
CI run: 32866068233
Desktop build: SUCCESS
Tests: 380 passed / 0 failed / 0 skipped
Windows x64 self-contained publish: SUCCESS
Product UI smoke: SUCCESS
Map / Factory / MiniMap smoke: SUCCESS
Graceful shutdown: SUCCESS
Release package verification: SUCCESS
Artifact upload: SUCCESS
```

사용자가 실제 문제 PC에서 검증한 fix candidate:

```text
bytes: 80,462,063
SHA-256: 96af948b2cd24caeb612d1d89a368bf30329606d3e934a292758292f70dcae30
```

이 package에서 Display Test와 actual Tarkov 성능 정상화를 확인했다.

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

Release gate:

- exact source build/test/publish/smoke
- exact ProductVersion/FIRST_RUN identity
- stable ZIP + top-level `준현 헬퍼/` structure verification
- reviewed Ground Truth regression `REGRESSION=0` where reviewed local dataset is available
- exact tag/source identity
- public stable/latest publication
- release asset hash/size metadata readback
- durable release status 기록

## 16. 현재 다음 작업

성능 알고리즘은 더 수정하지 않는다.

v1.7.6 finalization:

1. reviewed Ground Truth regression 가능 범위 확인
2. user-verified runtime behavior를 바꾸지 않는 선에서 diagnostic-only temporary implementation 정리 여부 결정; 위험하면 후속 기술부채로 기록
3. final HEAD CI / publish / Product UI / Scanner / Map smoke / package verification
4. PR #185 merge
5. v1.7.6 public stable publication
6. tag/source/package/hash/size readback 후 release proof 기록

새 Scanner 성능 결함이 보고되면 support bundle telemetry를 근거로 해당 stage만 수정한다. 추측 기반 threshold/candidate-cap 완화는 하지 않는다.
