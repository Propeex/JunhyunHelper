# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-26  
상태: **v1.7.9 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다.

현재 요구사항 범위의 제품은 완성 상태이며 기본 프로젝트 모드는 **유지보수**다. 새 기능은 사용자가 새로운 제품 요구사항으로 명시적으로 결정할 때만 시작한다.

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
- Scanner Ground Truth 교정 / diagnostics / regression dataset

Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper`는 불완전한 프로토타입이며 제품 요구사항의 권위가 아니다. 검증된 데이터/자산, 유지할 기능, 구현 아이디어와 시행착오 참고 용도로만 사용한다.

## 2. 현재 public stable

```text
version: v1.7.9
exact release source/tag target: bbb04e02385026eba6c77ba0a9d66bad9868cc92
main CI run: 32971976531 — SUCCESS
release workflow run: 32972267012 — SUCCESS
release id: 377149426
asset: Junhyun-Helper.zip
asset id: 530823055
asset bytes: 80,468,715
asset SHA-256: bd9285f7d8f819a1cf7f161f72baaae1c32a68f5db2e6f9a305053bbf3852946
checksum asset: SHA256SUMS.txt / id 530823056 / 86 bytes
published: 2026-08-26 KST
```

GitHub release readback:

- tag `v1.7.9`
- target commit = exact release source
- draft = false
- prerelease = false
- `releases/latest` = v1.7.9
- ZIP + checksum assets present
- ZIP GitHub digest = `sha256:bd9285f7d8f819a1cf7f161f72baaae1c32a68f5db2e6f9a305053bbf3852946`

공식 공개 증거:

- `docs/RELEASE_1.7.9.md`
- `docs/RELEASE_NOTES_V1.7.9.md`
- `docs/.release-v1.7.9-status.json`

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
- Program Update가 user.db, content/image cache, Map/Ammo/Scanner settings, Scanner logs/diagnostics/Ground Truth를 교체하지 않음
- user-reviewed Scanner Ground Truth는 자동 삭제하지 않음
- Scanner logs와 Ground Truth dataset lifetime을 분리
- 정상 Scanner monitoring은 durable automatic correction Case를 생성하지 않음

## 5. Game Content / Scanner catalog update

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

## 6. Scanner recognition production contract

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
→ conservative official-catalog matching
→ optional deep OCR / tight-title retry
→ optional current-pixel visual corroboration/recovery
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
continuous observation target = 200 ms
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
- price/flea/slots/needed는 Item ID 확정 이후 local mapped presentation data
- stale Item ID를 current identity proof로 사용 금지
- cross-frame OCR/visual identity cache를 Item proof로 사용 금지
- reviewed Ground Truth 없이 threshold/candidate cap 완화 금지
- game memory read / DLL injection / packet interception / process hook 금지

## 7. v1.7.8 raid inspect-header recovery 유지 계약

사용자 reviewed 레이드 Case 8건 중 실패 6건은 OCR 오인식이 아니라 `HEADER_CLOSE_NOT_LOCKED` / `TITLE_ANCHOR_INCOMPLETE`로 OCR 전에 중단됐다.

레이드 인벤토리의 neutral horizontal line이 inspect header와 이어지며 기존 fallback이 상세창 header-left를 실제보다 47~132px 왼쪽까지 소유한 것이 원인이었다.

현재 recovery order:

```text
primary header lock
→ live Ground Truth recovery
→ raid ownership recovery
→ contained-subpanel recovery
→ fail closed
```

Raid recovery는 강한 `RED_X_CANDIDATE >= 0.90`에서만 사용하며 red close-X, magnifier, neutral header, dark title field, title text evidence와 최종 `HEADER_FRAME_LOCKED >= 0.68`을 모두 다시 요구한다.

공식 결정: `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`

## 8. v1.7.9 Mini Scanner presentation contract

실사용에서 다음 회귀가 확인됐다.

```text
Scanner recognition log = success
Mini Scanner window = hidden
```

Scanner semantic pipeline은 Item ID를 정상 확정하고 `MiniScannerOverlayService.Show(snapshot)`까지 호출했지만, hidden Mini Scanner의 initial show가 별도의 top-band inventory/stash OCR을 다시 실행했다.

이 auxiliary OCR이 `장비/건강상태/스킬/지도/종합정보` 계열 중 2개 이상을 읽지 못하면 이미 확정된 Item 결과도 표시하지 않았다.

현재 authoritative contract:

```text
Scanner semantic success
→ Item ID 확정
→ presentation snapshot 생성
→ Mini Scanner
   ├─ preview/display-test: show
   ├─ already visible: authoritative Item result로 즉시 update
   └─ hidden real Scanner:
        Tarkov foreground yes → show
        Tarkov foreground no  → fail closed / hidden
```

**Auxiliary inventory-header OCR은 Mini Scanner 표시 권한을 가지지 않는다.**

다른 앱 위에 Mini Scanner가 갑자기 나타나는 것을 막기 위해 hidden real Scanner initial show는 실제 `EscapeFromTarkov` main window가 visible/non-minimized foreground인지 확인한다.

이 foreground guard는 화면/윈도 상태만 사용한다.

공식 결정: `docs/DECISION_V1.7.9_MINI_SCANNER_SHOW_2026-08-26.md`

## 9. Mini Scanner sticky presentation

v1.7.2부터 다음 presentation retention을 유지한다.

```text
No Item
  └─ A 확정 → Show A

Show A
  ├─ A 재확정 → A 유지 / miss budget reset
  ├─ B 확정 → 즉시 B로 교체 / reset
  ├─ 실제 miss #1 → A 유지
  ├─ 실제 miss #2 → A 유지
  └─ 실제 miss #3 → Hide
```

Candidate 안정화, title 변화 확인, OCR 진행 같은 progress-only 상태는 presentation miss로 세지 않는다.

## 10. Scanner 성능 기준선

v1.7.6에서 일부 문제 데스크탑의 5~13초 지연 root cause가 Windows OCR 자체가 아니라 동일 current-frame visual corroboration 반복 계산임을 실측하고 해결했다.

문제 PC 실제 Tarkov 성공 12건의 `ReadingTitle → ShowingItem`:

```text
minimum: 38.07 ms
median: 63.92 ms
maximum: 1.05 s
mean: 211.47 ms
```

같은 active latency cycle의 동일 title bitmap dimensions + exact current-pixel SHA-256 + OCR text 조합의 visual result만 재사용하며 cycle이 바뀌면 폐기한다. Cross-frame identity cache가 아니다.

새로운 runtime evidence 없이 성능만을 목적으로 threshold/candidate cap/OCR variant/visual acceptance를 변경하지 않는다.

## 11. Scanner correction / Ground Truth

현재 runtime contract:

```text
current frame evidence
→ latest exact frame in memory
→ bounded text diagnostic log
→ user chooses correction
→ user saves
→ reviewed durable Ground Truth
```

정상 monitoring은 NoDetail/header/OCR/matcher failure/ambiguity만으로 durable Case를 만들지 않는다.

Legacy automatic Case는 다음을 모두 증명할 때만 background cleanup한다.

```text
retention = automatic_sample
review_status = unreviewed
recent-write safety window = 5 minutes
pre-delete metadata/state re-read = unchanged
```

Reviewed/manual/corrupt/unknown/state-changed Case는 preserve fail closed한다.

사용자 activity feed의 동일 실패는 30초 동안 collapse한다.

## 12. Scanner UI / hotkeys

일반 Scanner 상단:

```text
스캐너 ON/OFF
설정
고급
현재 결과 교정
```

`현재 결과 교정`은 메모리에 보존된 최신 exact Scanner frame을 교정 창으로 연다.

`고급`:

- Display Test
- 교정 데이터 관리
- Scanner 성능 진단 자료 내보내기

기본 hotkeys:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

Scanner와 configurable Map actions는 `primary key + optional Ctrl/Alt/Shift` 계약을 사용한다. Windows key modifier는 지원하지 않는다. Map bare NumPad0~5 직접 층 선택은 유지하고 modifier+NumPad는 configurable action으로 사용할 수 있다.

## 13. CI / release contract

Release candidate는 다음을 모두 통과해야 한다.

- Desktop Release build
- 전체 automated tests
- Windows x64 self-contained publish
- Product UI / Scanner / Map / Factory / MiniMap smoke
- graceful shutdown
- package verification
- artifact upload
- main CI
- stable Release workflow
- `/releases/latest` exact readback

v1.7.9 proof:

```text
PR #190 final HEAD: 971c27a40566d01651cf14af0f519ceb68c3515a
PR CI: 32971624200 — SUCCESS
release source: bbb04e02385026eba6c77ba0a9d66bad9868cc92
main CI: 32971976531 — SUCCESS
release workflow: 32972267012 — SUCCESS
380 passed / 0 failed / 0 skipped
```

## 14. 현재 개발 상태 / 다음 작업 규칙

현재 제품은 **PRODUCT COMPLETE / MAINTENANCE MODE**다. 진행 중인 기능 개발은 없다.

새 작업은 다음 경우에만 시작한다.

- 사용자가 새로운 제품 요구사항을 명시적으로 결정
- 실사용 defect/regression 확인
- Tarkov UI/data 변화로 기존 기능 파손
- Windows/.NET 또는 외부 데이터 소스 호환성 변화
- 보안/데이터 무결성 문제

Scanner 문제는 다음 순서로 처리한다.

```text
exact evidence/support data
→ failure stage 확인
→ root cause
→ affected layer only 수정
→ regression/smoke
→ full Windows CI/publish/package
→ PATCH release
→ public release readback
→ canonical docs update
```

추측 기반 threshold/candidate-cap 완화나 불필요한 대규모 refactor는 하지 않는다.

## 15. 주요 공식 문서

- `README.md`
- `docs/CURRENT_STATE.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/SCANNER.md`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/DECISION_PRODUCT_COMPLETE_2026-08-26.md`
- `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`
- `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`
- `docs/DECISION_V1.7.9_MINI_SCANNER_SHOW_2026-08-26.md`
- `docs/RELEASE_1.7.9.md`
- `docs/.release-v1.7.9-status.json`
