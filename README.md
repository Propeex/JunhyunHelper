# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable은 **v1.2.0**입니다.

```text
version: v1.2.0 PUBLIC RELEASE / VERIFIED
release source: a7601f8498e8d75e832962fb9dd60f4112d28dc6
exact-source release run: 32514322439 — SUCCESS
automated tests: 255 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.0-win-x64.zip
bytes: 80,298,514
SHA-256: ab5e9ef35b300268d16a1c5eece86cd8c6e57c91c83364caf4b7d02cde1d27d1
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v3
Scanner cache schema: v1/v2 readable, v2 written
v1.1.6 → v1.2.0 mandatory Game Content update: none
v1.1.6 → v1.2.0 user.db migration: none
```

상세 릴리즈 기록은 `docs/RELEASE_1.2.0.md`에 있습니다.

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
→ red close + magnifier + title-field anchor refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ official Korean catalog semantic matching
   OR conservative full-catalog Tarkov-font visual recovery
→ Item ID
→ local JunhyunHelper presentation data
→ Mini Scanner
```

핵심 원칙:

- geometry만으로 Item 확정 금지
- false positive보다 miss 선호
- matcher confidence/top1-top2 margin을 편의상 완화하지 않음
- scan-time network 없음
- game memory / DLL injection / packet interception 없음
- icon 하나만으로 Item identity 확정 금지
- current official Korean item catalog가 identity 기준

v1.2.0 Scanner 보강:

- 빨간 X·돋보기·제목 필드 구조를 이용한 title ROI 보정
- 돋보기 anchor 발견 시 돋보기 픽셀을 OCR ROI에서 제외
- 현재 공식 한국어 이름에서 허용 문자 집합을 생성해 비정상 OCR을 검증
- 한자 OCR을 Korean item-title contract 위반 증거로 취급
- OCR이 비거나 손상된 경우 전체 공식 이름에 대한 보수적 Tarkov-font 시각 대조 fallback
- `인식 이미지`: 최신 진단 캡처 1장을 메모리에서만 확인
- `1회 고정밀 스캔`: 실시간 Scanner OFF에서도 한 번만 정밀 식별
- 기본 전역 단축키 `Ctrl+Shift+F10`, 변경/비활성화 가능
- 실시간 루프와 1회 스캔의 capture/OCR/presentation 상태 경합 방지

Mini Scanner/data 기준선:

- matched item 정보만 overlay에 표시
- Topmost + no-activate
- 전체 카드 drag hit surface + Arrow cursor
- 실제 모드에서 Tarkov foreground/inventory context를 보수적으로 확인
- canonical item 전체 icon prefetch
- raw `traderPrices` / derived `sellFor` 지원
- title OCR과 inventory-context OCR 직렬화
- 가격 데이터 누락은 해당 표시 필드만 비우고 identity catalog는 유지

상세: `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`, `docs/RELEASE_1.2.0.md`.

## Scanner 진단

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

`인식 이미지` 캡처는 메모리에만 존재하며 screenshot/raw pixel은 파일로 저장하지 않습니다.

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

Windows x64 portable / self-contained single-file.

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

별도 .NET 설치나 관리자 권한은 필요하지 않으며 현재 code signing은 하지 않습니다.

## 실제 Tarkov Scanner 검증

최신 Tarkov live E2E는 public release blocker가 아니며 실제 게임 환경에서 계속 검증합니다. 실제 사용 중 발견되는 문제는 `scanner.log`와 `인식 이미지`의 관측 근거를 기준으로 capture → candidate → title ROI → OCR/visual matcher → catalog → presentation → overlay 단계를 분리해 후속 PATCH에서 수정합니다.

## 버전 정책

- 새 사용자 기능 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/버그 수정/성능·안정성 개선 → PATCH +1

v1.2.0은 Scanner 진단 이미지와 1회 고정밀 스캔이라는 사용자 기능을 추가하고 제목 인식 구조를 보강한 MINOR 릴리즈입니다.

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 장기 결정
- `docs/SCANNER.md` — Scanner 계약
- `docs/SCANNER_TEST_PLAN.md` — Scanner 검증
- `docs/SCANNER_LAB_3_8_REFERENCE.md` — Scanner Lab v3.8 reference
- `docs/RELEASE_1.2.0.md` — v1.2.0 public release record
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/VERSIONING.md`
- `docs/PROGRAM_UPDATE.md`
- `docs/DEPLOYMENT.md`
