# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **PRODUCT COMPLETE / PUBLIC STABLE / MAINTENANCE MODE**입니다.

현재 요구사항 범위의 제품과 Scanner 기능 개발은 완료되었으며, 새로운 실제 회귀·Tarkov/Windows/.NET 호환성 변화·보안/무결성 문제 또는 사용자가 명시적으로 결정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 위험한 구조 변경을 시작하지 않습니다.

상세 상태:

- `docs/STATE.md`
- `docs/CURRENT_STATE.md`
- `docs/CURRENT_SCANNER_WORK.md`

## 현재 공개 릴리즈

현재 공개 stable/latest는 **v1.7.9**입니다.

```text
Desktop target version: 1.7.9
Content schema: v7
Readable content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
automated test suite: 380 tests
stable user ZIP: Junhyun-Helper.zip
```

공개 검증 기준선:

```text
exact release source/tag target: bbb04e02385026eba6c77ba0a9d66bad9868cc92
main CI run: 32971976531
release workflow run: 32972267012
release id: 377149426
stable asset id: 530823055
stable bytes: 80,468,715
stable SHA-256: bd9285f7d8f819a1cf7f161f72baaae1c32a68f5db2e6f9a305053bbf3852946
public stable/latest: VERIFIED
published: 2026-08-26 KST
```

공식 기록:

- `docs/RELEASE_1.7.9.md`
- `docs/RELEASE_NOTES_V1.7.9.md`
- `docs/.release-v1.7.9-status.json`

## 주요 기능

- GameMode별 Profile
- Quest availability / prerequisite / special trader / profile-variable
- Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth 교정 / diagnostics / regression dataset
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## Scanner

Production Scanner는 게임 화면 픽셀만 사용합니다.

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ red close-X + magnifier + neutral inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog sanitation / normalization
→ conservative official-catalog matching / bounded recovery
→ optional current-pixel visual corroboration/recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional correction / Ground Truth
```

핵심 안전 계약:

- false positive보다 miss 선호
- rectangle geometry는 proposal이며 identity proof가 아님
- `HEADER_FRAME_LOCKED >= 0.68`
- magnifier + red close-X 필수
- structural floor `0.34`
- continuous max 8 candidates
- one-shot max 12 candidates
- continuous observation target 200 ms
- current official Korean Tarkov item catalog가 identity authority
- price / slots / needed는 Item ID 확정 이후 local mapped data
- scan-time network 없음
- game memory read / DLL injection / packet interception / process hook 없음
- stale Item ID 및 cross-frame OCR/visual 결과를 새 Item identity proof로 사용하지 않음

## Scanner 성능 기준선

v1.7.6에서 일부 실제 데스크톱의 5~13초 Scanner 지연을 실측해 해결했습니다.

문제 PC 실제 Tarkov 성공 12건의 `ReadingTitle → ShowingItem`:

```text
minimum: 38.07 ms
median: 63.92 ms
maximum: 1.05 s
mean: 211.47 ms
```

동일 active Scanner cycle의 exact current-pixel visual evidence만 안전하게 재사용하며, cycle이 바뀌면 폐기합니다. Cross-frame identity cache가 아닙니다.

새 runtime evidence 없이 성능 수치만 낮추기 위해 recognition threshold, candidate cap, OCR variant 또는 visual acceptance를 변경하지 않습니다.

## v1.7.8 레이드 Scanner 수정

사용자가 검토한 레이드 Case에서 상세창과 item-name ROI는 맞게 잡았지만, 주변 인벤토리 수평선이 inspect header와 이어져 header-left ownership이 실제보다 47~132px 왼쪽으로 밀리면서 magnifier lane을 놓치는 문제가 확인됐습니다.

현재는 기존 정상 경로 뒤에 제한된 raid ownership recovery를 사용하며, 강한 `RED_X_CANDIDATE >= 0.90`에서만 red close-X, magnifier, neutral header, dark title field, text evidence와 최종 `HEADER_FRAME_LOCKED >= 0.68`을 모두 다시 검증합니다.

상세: `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`

## v1.7.9 Mini Scanner 수정

v1.7.8 실사용에서 **Scanner 로그에는 인식 성공이 기록되지만 Mini Scanner 창이 열리지 않는** presentation 회귀가 확인됐습니다.

Scanner는 Item ID를 정상 확정하고 있었지만, hidden Mini Scanner가 첫 표시 전에 별도의 상단 inventory/stash OCR을 다시 실행해 `장비/건강상태/스킬/지도/종합정보` 계열 중 2개 이상을 읽지 못하면 이미 확정된 결과도 표시하지 않았습니다.

현재 계약:

```text
Scanner semantic success
→ Item ID 확정
→ presentation snapshot
→ Mini Scanner
   ├─ preview/display-test: show
   ├─ already visible: authoritative Item result로 즉시 update
   └─ hidden real Scanner:
        Tarkov foreground yes → show
        Tarkov foreground no  → fail closed / hidden
```

**Auxiliary inventory-header OCR은 Mini Scanner 표시를 veto하지 않습니다.**

다른 앱 위에 Mini Scanner가 갑자기 나타나는 것을 막기 위해 real Scanner initial show는 실제 Tarkov client가 foreground인지 확인합니다.

상세: `docs/DECISION_V1.7.9_MINI_SCANNER_SHOW_2026-08-26.md`

## Mini Scanner

항상 표시:

- 아이템 icon
- official item name

사용자가 표시 여부와 순서를 지정:

- 상인 판매가
- 플리 평균가
- 상인 가격/칸
- 플리 가격/칸
- 필요 개수

Presentation은 sticky 정책을 사용합니다.

```text
성공 Item → 표시 / miss budget reset
새 Item → 즉시 교체
실제 miss #1 → 마지막 정상 Item 유지
실제 miss #2 → 마지막 정상 Item 유지
실제 miss #3 → Hide
```

Candidate 안정화, 제목 변화 확인, OCR 진행 같은 progress-only 상태는 miss로 세지 않습니다.

## Scanner UI

일반 Scanner 화면 상단:

```text
스캐너 ON/OFF
설정
고급
현재 결과 교정
```

하단에는 아이템 검색과 최근 Scanner 인식 로그가 있습니다.

기본 전역 단축키:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

Scanner와 configurable Map 단축키는 일반 키 하나 + 선택적 Ctrl/Alt/Shift 조합을 공통 계약으로 사용합니다. Windows 키 조합은 지원하지 않습니다.

## Ground Truth / 교정

정상 연속 Scanner는 실패 프레임을 durable Case로 자동 저장하지 않습니다.

```text
current frame evidence
→ latest exact frame in memory
→ bounded text diagnostic log
→ user opens correction
→ user saves
→ reviewed durable Ground Truth
```

`현재 결과 교정`은 메모리에 보존된 최신 exact Scanner frame을 교정 창으로 엽니다.

이전 버전의 legacy automatic Case는 `retention=automatic_sample` + `review_status=unreviewed`, 5분 recent-write safety와 pre-delete recheck를 모두 통과할 때만 background cleanup합니다.

Reviewed/manual/corrupt/unknown Case는 자동 삭제하지 않습니다.

기본 저장 위치:

```text
%LocalAppData%\JunhyunHelper\scanner\diagnostics
```

## 사용자 데이터

대표 mutable data는 `%LocalAppData%/JunhyunHelper` 아래에 저장합니다.

- user.db
- Game Content cache
- image cache
- Map/Ammo/Scanner settings
- Scanner logs/diagnostics/Ground Truth

Program Update는 이 사용자 데이터를 덮어쓰지 않습니다.

## Program Update

```text
latest public stable 확인
→ strictly newer면 사용자 동의
→ exact Windows release asset + checksum
→ checksum/package 검증
→ program-owned files transaction 교체
→ 새 버전 재시작
```

## 배포 형태

Windows x64 portable / .NET 10 self-contained single-file.

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

별도 .NET Runtime 설치나 관리자 권한은 필요하지 않으며 현재 code signing은 하지 않습니다.

## 유지보수 원칙

Scanner 문제는 다음 순서로 처리합니다.

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

추측 기반 threshold/candidate-cap 완화나 불필요한 대규모 refactor는 하지 않습니다.

공식 프로젝트 기준은 `docs/STATE.md`입니다.
