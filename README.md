# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable은 **v1.1.6**입니다.

```text
version: v1.1.6 PUBLIC RELEASE / VERIFIED
release source: 8efee02e5966adb9b67b47847f95a12dfc357d0a
exact-source release run: 32500707112 — SUCCESS
automated tests: 250 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.6-win-x64.zip
bytes: 80,271,024
SHA-256: 986d0d2855381060267f63d2902317eabedc5d5738448fbd6c2b09e764c3477e
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
Scanner cache schema: v1/v2 readable, v2 written
v1.1.5 → v1.1.6 mandatory Game Content update: none
v1.1.5 → v1.1.6 user.db migration: none
```

상세 릴리즈 기록은 `docs/RELEASE_1.1.6.md`에 있습니다.

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
→ title ROI
→ Windows ko-KR OCR
→ current official Korean full-item catalog matching
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

v1.1.5 이후 Mini Scanner/data 기준선:

- matched item 정보만 overlay에 표시
- Topmost + no-activate
- 전체 카드 drag hit surface + Arrow cursor
- 실제 모드에서 Tarkov foreground/inventory context를 보수적으로 확인
- canonical item 전체 icon prefetch
- raw `traderPrices` / derived `sellFor` 지원
- title OCR과 inventory-context OCR 직렬화

v1.1.6 catalog 수정:

- identity catalog health = 4,000개 이상 유효 Item ID/공식 이름
- trader/flea coverage는 identity health와 분리
- 가격 누락은 해당 표시 필드만 비움
- 4,000개 identity + trader price 0개도 식별 가능
- 3,999개 identity는 계속 거부
- `아이템 목록 최신화` 결과를 `scanner.log`의 `catalog-sync` 진단으로 기록

상세: `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`, `docs/RELEASE_1.1.6.md`.

## Scanner 진단

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

screenshot/raw pixel은 저장하지 않습니다.

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

최신 Tarkov live E2E는 public release blocker가 아니며 실제 게임 환경에서 계속 검증합니다. 실제 사용 중 발견되는 문제는 `scanner.log`와 재현 조건을 기준으로 capture → candidate → OCR → matcher → presentation → overlay 단계를 분리해 후속 PATCH에서 수정합니다.

## 버전 정책

- 새 사용자 기능 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/버그 수정/성능·안정성 개선 → PATCH +1

v1.1.6은 기존 Scanner catalog synchronization 회귀를 수정한 PATCH입니다.

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 장기 결정
- `docs/SCANNER.md` — Scanner 계약
- `docs/SCANNER_TEST_PLAN.md` — Scanner 검증
- `docs/SCANNER_LAB_3_8_REFERENCE.md` — Scanner Lab v3.8 reference
- `docs/RELEASE_1.1.6.md` — v1.1.6 public release record
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/VERSIONING.md`
- `docs/PROGRAM_UPDATE.md`
- `docs/DEPLOYMENT.md`
