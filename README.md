# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable은 **v1.3.4**입니다.

```text
version: v1.3.4 PUBLIC RELEASE / VERIFIED
release source/tag: a78ddbc649747f1320236556f17e6b908304674a
final PR CI: 32636665202 — SUCCESS
automated tests: 267 passed / 0 failed / 0 skipped
release run: 32636927134 — SUCCESS
independent public verifier: 32637159066 — SUCCESS
asset: Junhyun-Helper-v1.3.4-win-x64.zip
bytes: 80,319,654
SHA-256: 8c442fec81a0b993a9a6b080e59b656668a7a73d8fadd8434595545b08c82e8e
ProductVersion: 1.3.4+a78ddbc649747f1320236556f17e6b908304674a
public/latest: VERIFIED
exact public tag source: VERIFIED
Draft re-download: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.3 → v1.3.4 mandatory Game Content update: none
v1.3.3 → v1.3.4 user.db migration: none
```

상세 릴리즈 기록은 `docs/RELEASE_1.3.4.md`와 `docs/.release-v1.3.4-status.json`에 있습니다.

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
→ red close component + normalized X-shape template
→ long neutral top frame
→ fixed frame-left search-icon lane
→ normalized magnifier ring/hollow/handle template
→ dark title field + text evidence
→ full HEADER_FRAME_LOCKED only
→ locked-header-based detail bounds refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog-derived character / punctuation sanitation
→ optional one-unknown-glyph current-catalog recovery
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
- full inspect-header lock이 아니면 OCR identity path 진입 금지
- live evidence 없이 confidence/margin을 전역 완화하지 않음
- scan-time network 없음
- game memory / DLL injection / packet interception 없음

### v1.3.4 — live recognition 결합 하드닝

v1.3.3 공개 후 실제 사용에서 확인된 `Esma「ch` 계열 OCR 손실, title glyph→magnifier 오인, detail-window bounds drift, 저장 PNG의 진단 박스 유실을 함께 수정했습니다.

- `「 = r` 같은 고정 치환 금지
- 영숫자 사이의 current-catalog 외 기호 1개를 별도 `?` unknown-glyph evidence로 보존
- 같은 길이·같은 나머지 character slot을 가진 현재 공식 이름이 전체 카탈로그에서 정확히 하나이고 global runner-up과 10%p 이상 벌어질 때만 복구
- magnifier 후보는 실제 frame-left fixed search-icon lane 밖으로 나갈 수 없음
- normalized ring + hollow center + lower-right handle template를 geometry와 결합
- close/X는 red dominance + expected geometry + diagonal-X shape evidence 결합
- `HEADER_FRAME_LOCKED` + score 0.68 이상 + valid magnifier/X를 통과한 후보만 OCR identity path에 유지
- 확정된 magnifier/X에서 detail-window top/left/right를 다시 정렬
- 사용자 저장 PNG에 초록=상세창, 파랑=제목 ROI, 노랑=돋보기, 빨강=닫기 X를 실제 픽셀에 합성
- 일반 confidence/top1-top2 margin과 기존 bounded unique one-edit 조건은 완화하지 않음
- 최고 상점가 / flea `avg24hPrice` / `RequiredTotal` 의미와 schema는 변경 없음

상세:

- `docs/SCANNER_V1.3.4_LIVE_HARDENING.md`
- `docs/DECISION_SCANNER_V1.3.4_LIVE_HARDENING_2026-08-23.md`
- `docs/RELEASE_1.3.4.md`

### v1.3.3 — actual inspect-header frame lock

v1.3.2 공개 후 사용자가 제공한 실제 2048×1280 Tarkov 상세창 12개를 다시 측정해 title-start / magnifier-anchor 회귀를 수정했습니다.

- title ROI의 수평 기준을 **실제 long neutral top frame + red close/X + bounded left search-icon lane + magnifier**로 고정
- first Korean/title glyph connected component는 더 이상 title ROI left edge를 결정하지 않음
- `HEADER_FRAME_LOCKED` + anchor score **0.68 이상**이 아니면 OCR identity path로 진행하지 않음
- partial/failed lock은 fail closed
- 실제 12개 표본의 비식별 header-relative geometry를 packaged-EXE smoke regression으로 재생
- raw Windows OCR과 current-catalog sanitation 후 실제 matcher input을 진단에서 분리
- current catalog 밖 punctuation/symbol은 ordinary matcher evidence에서 제거
- 기존 confidence/top1-top2 margin과 bounded unique one-edit 조건은 완화하지 않음

상세:

- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`
- `docs/.scanner-v1.3.3-header-evidence.json`
- `docs/DECISION_SCANNER_HEADER_LOCK_2026-08-23.md`

### v1.3.2 — live-evidence OCR 보강

- magnifier의 좌측 헤더 위치, 밝은 ring, hollow center, lower-right handle을 핵심 evidence로 사용
- 뒤따르는 title glyph component는 magnifier의 필수조건이 아니라 corroboration으로 사용
- OCR punctuation/symbol 허용 집합을 current official Korean item catalog에서 자동 파생
- current catalog에 없는 punctuation/symbol은 ordinary matcher 입력 전에 제거
- normalized 길이 7 이상에서 정확히 1 edit인 후보는 current catalog 전체에서 유일하고 global runner-up과 **10%p 이상** 차이가 있을 때만 제한적으로 복구
- multi-edit low-confidence OCR은 percentage만으로 확정하지 않고 strict Tarkov-font visual corroboration 필요

상세: `docs/SCANNER_V1.3.2_LIVE_EVIDENCE.md`, `docs/DECISION_SCANNER_LIVE_EVIDENCE_2026-08-23.md`.

### v1.3.0부터 유지되는 실사용/분석 워크플로

- `인식 이미지`에서 최신 실제 recognition frame을 사용자 지정 PNG로 저장
- 자동 screenshot 저장 없음
- v1.3.4부터 저장 PNG에는 상세창/title/magnifier/close 진단 rectangle 포함
- 진단창에서 raw OCR과 실제 matcher input을 구분
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
+ 문제 발생 직후 저장한 진단 PNG
+ 필요 시 scanner.log
```

문제는 capture → structural candidate → close/frame/magnifier template lock → detail bounds/title ROI → OCR → catalog sanitation/unknown-glyph/matcher/visual → presentation → overlay 단계로 분리합니다. 새 실패 사례가 확보되면 해당 사례를 regression으로 고정한 뒤 필요한 단계만 수정합니다.

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

v1.3.0은 Scanner 분석 이미지 export, one-shot test scan, 3종 global hotkey를 추가한 MINOR 릴리즈입니다. v1.3.1~v1.3.4는 실제 title/OCR/header/diagnostics evidence를 반영한 PATCH 릴리즈입니다.

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 장기 결정 인덱스
- `docs/SCANNER.md` — Scanner 제품/기술 기준선
- `docs/SCANNER_V1.3.4_LIVE_HARDENING.md` — v1.3.4 live hardening 계약
- `docs/DECISION_SCANNER_V1.3.4_LIVE_HARDENING_2026-08-23.md` — v1.3.4 결정
- `docs/RELEASE_1.3.4.md` — v1.3.4 공개 검증
- `docs/SCANNER_V1.3.3_HEADER_LOCK.md` — v1.3.3 header lock 계약
- `docs/SCANNER_V1.3.2_LIVE_EVIDENCE.md` — v1.3.2 live recognition 이력
- `docs/SCANNER_SYMBOL_POLICY.md` — current-catalog symbol 정책
- `docs/SCANNER_TEST_PLAN.md` — Scanner 검증 gate
- `docs/ARCHITECTURE.md` — 전체 아키텍처
- `docs/DEVELOPER_REFERENCE.md` — 구현/참조 지도
