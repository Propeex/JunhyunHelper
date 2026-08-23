# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable은 **v1.3.1**입니다.

```text
version: v1.3.1 PUBLIC RELEASE / VERIFIED
release source: 028bfb600f4662962a0daac1dad04b570e018275
final PR CI: 32615869812 — SUCCESS
automated tests: 256 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.3.1-win-x64.zip
bytes: 80,310,221
SHA-256: 5c4b79cc5d373b4a28cbeb10be18b8369086b2ee9f0edc172530028dd71b1c3f
ProductVersion: 1.3.1+028bfb600f4662962a0daac1dad04b570e018275
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
v1.3.0 → v1.3.1 mandatory Game Content update: none
v1.3.0 → v1.3.1 user.db migration: none
```

상세 릴리즈 기록은 `docs/RELEASE_1.3.1.md`에 있습니다.

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
→ dark title-field + right red X + left magnifier-shape + first-glyph evidence
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ current official Korean catalog semantic matching
→ optional local Tarkov-font visual corroboration/recovery
→ conservative confidence + top1/top2 margin
→ Item ID
→ local JunhyunHelper presentation data
→ Mini Scanner
```

핵심 원칙:

- false positive보다 miss 선호
- geometry/아이콘/OCR 한 조각만으로 Item 확정 금지
- current official Korean item catalog가 Item identity 기준
- matcher/visual ambiguity는 fail closed
- scan-time network 없음
- game memory / DLL injection / packet interception 없음

### v1.3.1 — 실전 title recognition hardening

실제 Tarkov에서 발견된 “아이템 이름 첫 글자를 돋보기로 오인” 사례를 기준으로 inspect-header 인식을 보강했습니다.

- 상세창 상단을 하나의 구조로 판단
  - 어두운 title field
  - 좌측 magnifier/search icon
  - 실제 첫 글자군
  - 우측 red close/X
- structural panel-left가 일부 안쪽으로 drift해도 실제 magnifier를 제한된 왼쪽 확장 영역에서 재탐색
- magnifier 후보에 위치/크기/비율뿐 아니라 hollow center, ring perimeter, lower-right handle, following-glyph evidence 적용
- 첫 한글 글자가 magnifier로 선택되어 OCR ROI에서 잘리는 회귀를 packaged-EXE smoke로 고정
- Windows OCR이 official item으로 성공한 경우에도 필요 시 local Tarkov title font + current catalog 렌더링으로 시각 corroboration
- strict visual evidence가 다른 current official Item ID를 명확하게 지목할 때만 OCR identity 교정
- font evidence가 없거나 애매하면 기존 healthy OCR result 유지
- 상단 상태 텍스트 왼쪽에 현재 실행 EXE 버전 표시

상세: `docs/SCANNER_V1.3.1_RECOGNITION.md`.

### v1.3.0 — 실사용 분석/단축키 워크플로

- `인식 이미지`에서 최신 실제 인식 원본 frame을 PNG로 저장
- 저장 PNG에는 진단 사각형/텍스트 overlay가 합성되지 않음
- 자동 screenshot 저장 없음
- 1회 인게임 스캔: 기본 `Ctrl+Shift+F10`
- 1회 테스트 스캔: 기본 `Ctrl+Shift+F11`
- Scanner ON/OFF: 기본 `Ctrl+Shift+F12`
- Scanner 탭 `단축키 설정`에서 세 global hotkey를 변경/비활성화
- 동일 gesture 중복 지정 차단
- one-shot 인게임/테스트 버튼은 제거하고 단축키로 실행
- 기존 v1.2.x one-shot 사용자 지정 key를 schema v4로 승계
- `로그 삭제`는 사용자 export PNG를 삭제하지 않음

### Mini Scanner / 표시 데이터

- matched item 정보만 overlay에 표시
- Topmost + no-activate
- 전체 카드 drag hit surface + Arrow cursor
- 실제 Scanner mode에서 Tarkov foreground/inventory context를 보수적으로 확인
- canonical item icon은 update/cache 경로에서 준비하고 scan 순간 HTTP 없음
- 최고 상점가 = 유효한 non-flea RUB 환산 최고 판매가
- 플리마켓 평균가 = positive `avg24hPrice`
- 필요한 개수 = `NeededItems[itemId].RequiredTotal`
- 가격/크기 누락은 해당 표시 필드만 fail closed하고 Item identity를 폐기하지 않음

## Scanner font 정책

게임 폰트 파일을 JunhyunHelper 배포물에 재배포하지 않습니다.

```text
Tarkov resources.assets (read-only)
→ 필요한 SFNT font payload 발견/추출
→ %LocalAppData%/JunhyunHelper/scanner/fonts local cache
→ source/font generation 검증
→ Bender regular/bold + Korean fallback
→ official item-name visual templates/features
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

문제는 capture → structural candidate → header anchors/title ROI → OCR/font visual → catalog → presentation 단계로 분리하여 수정합니다. 실제 evidence 없이 confidence/margin을 임의로 낮추지 않습니다.

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

v1.3.0은 Scanner 분석 이미지 export, one-shot test scan, 3종 global hotkey를 추가한 MINOR 릴리즈입니다. v1.3.1은 실제 인게임 title recognition 실패 evidence와 버전 표시 UX를 반영한 PATCH 릴리즈입니다.

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 장기 결정 인덱스
- `docs/SCANNER.md` — Scanner 제품/기술 기준선
- `docs/SCANNER_V1.3.1_RECOGNITION.md` — v1.3.1 recognition 계약
- `docs/RELEASE_1.3.1.md` — v1.3.1 공개 검증
- `docs/SCANNER_TEST_PLAN.md` — Scanner 검증 gate
- `docs/ARCHITECTURE.md` — 전체 아키텍처
- `docs/DEVELOPER_REFERENCE.md` — 구현/참조 지도
