# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable은 v1.1.3이며 **v1.1.4 Scanner hardening PATCH release candidate**를 검증 중입니다.

```text
Desktop target: 1.1.4
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
automated tests: 247
mandatory content update from 1.1.3: none
user.db migration from 1.1.3: none
```

최종 public source/run/ZIP hash는 `docs/RELEASE_1.1.4.md`에 기록합니다.

## Scanner — v1.1.4

```text
Tarkov / Display pixels
→ RED-X candidates + rectangle/edge fallback
→ IoU deduplication
→ 최대 8 structural candidates
→ adaptive 4x/6x/8x Windows ko-KR OCR
→ current official Korean full-item catalog semantic validation
→ 필요 시 상위 3개 candidate deep OCR
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

v1.1.4 보강:

- 같은 quantized geometry signature가 연속 관측될 때만 candidate 안정화 hit 누적
- verified detail은 OCR을 반복하지 않고 presentation snapshot만 1초 간격 갱신
- `현재 필요한 수량`은 최신 `NeededItems[].RequiredTotal` 재연결
- Scanner local icon decode memory cache
- 최고 상점가 = fleaMarket 제외 `sellFor.priceRUB` 최댓값
- 플리 평균가 = `avg24hPrice`
- invalid market/dimension은 필드 단위 fail-closed
- 최근 인식 기록 우측 상단 `로그 삭제`
- 로그 삭제는 UI activity + `scanner.log` + `scanner.log.1`을 함께 clear

핵심 안전 원칙:

- geometry만으로 Item 확정 금지
- matcher confidence/top1-top2 margin 완화 금지
- historical alias production 누적 금지
- false positive보다 miss 선호
- scan-time network 없음
- game memory / DLL injection / packet interception / icon identity 없음

상세: `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`, `docs/SCANNER_LAB_3_8_REFERENCE.md`.

## Scanner 탭

```text
상단 bar
  왼쪽: 스캐너 / 테스트
  오른쪽: 아이템 목록 최신화
↓
표시 정보 checkboxes
↓
최근 인식 기록                         로그 삭제
```

Mini Scanner는 별도 edit/reset mode 없이 visible 상태에서 직접 drag합니다. Foundation 개발 controls는 일반 UI에 노출하지 않습니다.

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

screenshot/raw pixel은 저장하지 않습니다.

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

최신 Tarkov Borderless live E2E는 기존 정책대로 public release blocker가 아니며 실제 게임 환경에서 계속 검증합니다. 문제는 `scanner.log`와 최근 인식 기록을 근거로 후속 PATCH에서 수정합니다.

## 버전 정책

- 새 사용자 기능 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/버그 수정/성능·안정성 개선 → PATCH +1

v1.1.4는 기존 Scanner의 안정성·데이터 신뢰성·진단 UX 보강이므로 PATCH입니다.

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 장기 결정
- `docs/SCANNER.md` — Scanner 계약
- `docs/SCANNER_TEST_PLAN.md` — Scanner 검증
- `docs/SCANNER_LAB_3_8_REFERENCE.md` — Scanner Lab v3.8 reference
- `docs/RELEASE_1.1.4.md` — v1.1.4 release record
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/VERSIONING.md`
- `docs/PROGRAM_UPDATE.md`
- `docs/DEPLOYMENT.md`
