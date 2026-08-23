# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable은 **v1.3.2**입니다.

```text
version: v1.3.2 PUBLIC RELEASE / VERIFIED
release source: 922797a99ea221fdc4984dd6ed05df552149d6e4
final PR CI: 32619142034 — SUCCESS
automated tests: 263 passed / 0 failed / 0 skipped
release run: 32621021058
asset: Junhyun-Helper-v1.3.2-win-x64.zip
bytes: 80,311,752
SHA-256: 6e3a7af2de50dfd14f1c49ccb39753177a0bce5b22993bb8bb94ffde93086767
ProductVersion: 1.3.2+922797a99ea221fdc4984dd6ed05df552149d6e4
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.1 → v1.3.2 mandatory Game Content update: none
v1.3.1 → v1.3.2 user.db migration: none
```

상세 릴리즈 기록은 `docs/RELEASE_1.3.2.md`와 `docs/.release-v1.3.2-status.json`에 있습니다.

## 주요 기능

- GameMode별 Profile
- Quest / prerequisite / special trader / profile-variable
- Hideout
- Needed Items / FIR·일반 Inventory / cleanup safety / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## Scanner

```text
Tarkov / Display pixels
→ detail-window structural candidates
→ dark title-field + right red X + left magnifier morphology + first-glyph evidence
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog-derived character / punctuation sanitation
→ current official Korean catalog semantic matching
→ bounded unique one-edit recovery when safe
→ optional local Tarkov-font visual corroboration/recovery
→ conservative confidence + top1/top2 margin
→ Item ID or fail closed
→ local JunhyunHelper presentation data
→ Mini Scanner
```

핵심 원칙:

- false positive보다 miss 선호
- geometry/아이콘/OCR 한 조각만으로 Item 확정 금지
- current official Korean item catalog가 Item identity 기준
- matcher/visual ambiguity는 fail closed
- live evidence 없이 confidence/margin을 전역 완화하지 않음
- scan-time network 없음
- game memory / DLL injection / packet interception 없음

### v1.3.2 — live-evidence OCR 보강

v1.3.1 공개 후 실제 Tarkov/DisplayTest에서 확인된 두 title-recognition 실패를 근거로 보강했습니다.

- magnifier의 좌측 헤더 위치, 밝은 ring, hollow center, lower-right handle을 핵심 evidence로 사용
- 뒤따르는 title glyph component는 magnifier의 필수조건이 아니라 corroboration으로 사용
- OCR punctuation/symbol 허용 집합을 current official Korean item catalog에서 매 catalog generation마다 자동 파생
- current catalog에 없는 `「` 같은 punctuation/symbol은 matcher 입력 전에 제거
- normalized 길이 7 이상에서 정확히 1 edit인 후보는 current catalog 전체에서 유일하고 global runner-up과 **10%p 이상** 차이가 있을 때만 제한적으로 복구
- `Thermite 테르밋` → `Themite 테르밋` 같은 단일 누락은 위 안전 조건을 충족할 때 복구 가능
- `Gunpowder "Eagle" 화약`처럼 여러 글자가 동시에 손상된 저신뢰 OCR은 percentage만으로 확정하지 않고 strict Tarkov-font visual corroboration 필요
- 최고 상점가 / flea `avg24hPrice` / `RequiredTotal` 의미와 schema는 변경 없음

상세: `docs/SCANNER_V1.3.2_LIVE_EVIDENCE.md`, `docs/DECISION_SCANNER_LIVE_EVIDENCE_2026-08-23.md`.

### v1.3.1 — inspect-header / title ROI hardening

- 상세창 상단을 dark title field + left magnifier + first glyphs + right red close/X 구조로 판단
- structural panel-left drift가 있어도 실제 magnifier를 제한된 왼쪽 확장 영역에서 재탐색
- 첫 한글 글자가 magnifier로 오인되어 OCR ROI에서 잘리는 회귀를 packaged-EXE smoke로 고정
- OCR semantic success도 필요 시 local Tarkov title font + current catalog로 보수적으로 corroborate
- strict visual evidence가 다른 current official Item ID를 명확히 지목할 때만 identity 교정

상세: `docs/SCANNER_V1.3.1_RECOGNITION.md`.

### v1.3.0부터 유지되는 실사용/분석 워크플로

- `인식 이미지`에서 최신 실제 recognition 원본 frame을 PNG로 사용자 지정 저장
- 자동 screenshot 저장 없음
- 1회 인게임 스캔 기본 `Ctrl+Shift+F10`
- 1회 테스트 스캔 기본 `Ctrl+Shift+F11`
- Scanner ON/OFF 기본 `Ctrl+Shift+F12`
- Scanner 탭 `단축키 설정`에서 변경/비활성화 가능
- 동일 gesture 중복 지정 차단
- `로그 삭제`는 recent activity, scanner.log(.1), 최신 in-memory diagnostic image를 정리하되 사용자 export PNG는 삭제하지 않음

## Scanner 표시 데이터

- 최고 상점가 = 유효한 non-flea RUB 환산 판매가 최댓값
- 플리마켓 평균가 = positive `avg24hPrice`
- 슬롯 = positive `width × height`
- 가격/슬롯 = valid price와 slots가 모두 존재할 때만
- 필요한 개수 = `NeededItems[itemId].RequiredTotal`
- Inventory를 차감한 부족량은 Scanner의 `필요 개수` 의미가 아님
- 가격/크기 누락은 해당 표시 필드만 fail closed하고 Item identity를 폐기하지 않음

## Mini Scanner

- matched item 정보만 overlay에 표시
- Topmost + no-activate
- 전체 카드 drag hit surface + Arrow cursor
- 실제 Scanner mode에서는 Tarkov foreground/inventory context를 보수적으로 확인
- item/context epoch가 바뀐 stale result를 화면에 적용하지 않음
- canonical icon은 update/cache 경로에서 준비하고 scan 순간 HTTP 없음

## Scanner font 정책

게임 폰트 파일을 JunhyunHelper 배포물에 재배포하지 않습니다.

```text
Tarkov resources.assets (read-only)
→ bounded SFNT discovery/extraction
→ %LocalAppData%/JunhyunHelper/scanner/fonts
→ source/font generation 검증
→ Bender regular/bold + Korean fallback
→ current official item-name visual templates/features
```

Tarkov/font generation이 바뀌면 stale rendered template generation을 그대로 신뢰하지 않습니다.

## Scanner 진단 / 실사용 개선

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

실제 Tarkov에서 recognition 문제가 발생하면 다음 자료가 우선 근거입니다.

```text
실제 아이템 이름
+ 인식 성공/미인식/오인식 결과
+ 문제 발생 직후 저장한 인식 원본 PNG
+ 필요 시 scanner.log
```

문제는 capture → structural candidate → header anchors/title ROI → OCR → catalog matcher/visual → presentation → overlay 단계로 분리합니다. 새 실패 사례가 확보되면 해당 사례를 최소 regression으로 고정한 뒤 필요한 단계만 수정합니다.

## Program Update

```text
latest public stable 확인
→ strictly newer면 사용자 동의
→ exact Windows ZIP + SHA256SUMS
→ checksum/package 검증
→ program-owned files transaction 교체
→ 새 버전 재시작
```

사용자 데이터는 `%LocalAppData%/JunhyunHelper`에 분리되어 있으며 프로그램 업데이트가 덮어쓰지 않습니다.

## 배포 형태

Windows x64 portable / .NET 10 self-contained single-file.

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

별도 .NET Runtime 설치나 관리자 권한은 필요하지 않으며 현재 code signing은 하지 않습니다.

## 버전 정책

- 새 사용자 기능 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/버그 수정/성능·안정성·정확성 개선 → PATCH +1

v1.3.0은 Scanner 분석 이미지 export, one-shot test scan, 3종 global hotkey를 추가한 MINOR 릴리즈입니다. v1.3.1은 실제 title-recognition 실패와 버전 표시 UX를 반영한 PATCH이며, v1.3.2는 추가 live OCR evidence를 기반으로 magnifier association·catalog-derived symbol policy·bounded one-edit recovery를 보강한 PATCH입니다.

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 장기 결정 인덱스
- `docs/SCANNER.md` — Scanner 제품/기술 기준선
- `docs/SCANNER_V1.3.2_LIVE_EVIDENCE.md` — v1.3.2 live recognition 계약
- `docs/SCANNER_SYMBOL_POLICY.md` — current-catalog symbol 정책
- `docs/SCANNER_V1.3.1_RECOGNITION.md` — v1.3.1 recognition 이력
- `docs/RELEASE_1.3.2.md` — v1.3.2 공개 검증
- `docs/SCANNER_TEST_PLAN.md` — Scanner 검증 gate
- `docs/ARCHITECTURE.md` — 전체 아키텍처
- `docs/DEVELOPER_REFERENCE.md` — 구현/참조 지도
