# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable은 **v1.3.0**입니다.

```text
version: v1.3.0 PUBLIC RELEASE / VERIFIED
release source: f03441672d39165678fa53f57af46f103070d50e
final PR: #142
final PR CI: 32611343850 — SUCCESS
public verification status commit: 4cefa27012eafa62d40ef99f4efd630f3c53127a — SUCCESS
automated tests: 256 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.3.0-win-x64.zip
bytes: 80,306,655
SHA-256: 5880c71098d737b7ffd3447eb77a55195d09d76ea12be7ff79df4eb055ac8344
ProductVersion: 1.3.0+f03441672d39165678fa53f57af46f103070d50e
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache: v1/v2 readable, v2 written
mandatory Game Content update from v1.2.2: none
user.db migration from v1.2.2: none
```

상세 릴리즈 기록: `docs/RELEASE_1.3.0.md`.

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
   OR conservative current-catalog Tarkov-font visual recovery
→ confidence + top1/top2 margin
→ Item ID
→ local JunhyunHelper presentation data
→ Mini Scanner
```

핵심 원칙:

- geometry만으로 Item 확정 금지
- false positive보다 miss 선호
- matcher confidence/top1-top2 margin을 편의상 완화하지 않음
- current official Korean item catalog가 identity 기준
- scan-time network 없음
- game memory / DLL injection / packet interception 없음
- icon 하나만으로 Item identity 확정 금지

### v1.3.0 Scanner 분석 워크플로

- `인식 이미지`에서 최신 실제 recognition source frame을 사용자 지정 PNG로 저장 가능
- 자동 raw screenshot 저장은 하지 않음
- 모든 연결 디스플레이를 한 번만 검사하는 1회 테스트 스캔 추가
- 1회 인게임/테스트 스캔 버튼 제거; 두 기능은 global hotkey-only
- 기본 hotkey:
  - `Ctrl+Shift+F10` — 1회 인게임 스캔
  - `Ctrl+Shift+F11` — 1회 테스트 스캔
  - `Ctrl+Shift+F12` — Scanner ON/OFF
- Scanner 탭의 `단축키 설정`에서 세 명령을 각각 변경/비활성화
- 동일 gesture 중복 지정 금지
- global hotkey는 Scanner 탭 밖에서도 MainWindow lifetime 동안 유지
- Scanner settings schema v4; v3의 기존 one-shot 사용자 단축키를 인게임 one-shot으로 보존
- 기존 사용자 키와 새 기본키가 충돌하면 신규 명령 쪽만 비충돌 fallback 사용
- detector/OCR/visual confidence 및 top1/top2 margin 변경 없음
- 최고 상점가/플리 평균가/Needed Items `RequiredTotal` 의미 변경 없음

### Scanner 진단

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

최신 인식 이미지는 기본적으로 메모리에만 유지합니다. 사용자가 `이미지 저장`을 선택한 경우에만 실제 recognition source frame을 PNG로 export합니다. 저장 PNG에는 진단 사각형/텍스트 overlay를 합성하지 않습니다.

### Mini Scanner / data

- matched Item 정보만 overlay에 표시
- Topmost + no-activate
- 전체 카드 drag hit surface
- 실제 mode에서 Tarkov foreground/inventory context를 보수적으로 확인
- raw `traderPrices` / derived `sellFor` 지원
- 최고 상점가 = 유효한 non-flea RUB 판매가 최댓값
- 플리 평균가 = positive `avg24hPrice`
- 현재 필요한 수량 = `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal`

## Program Update

latest public stable을 확인하고 strictly newer stable이 있을 때만 사용자 동의 후 exact Windows ZIP + SHA256 검증을 거쳐 program-owned files를 교체합니다. `%LocalAppData%/JunhyunHelper` 사용자 데이터는 교체하지 않습니다.

## 배포 형태

Windows x64 portable / .NET 10 self-contained single-file.

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

별도 .NET 설치나 관리자 권한은 필요하지 않으며 현재 code signing은 하지 않습니다.

## 실제 Tarkov Scanner 검증

최신 Tarkov live E2E calibration은 실제 사용자 환경에서 계속 검증합니다. 문제 발생 시 `scanner.log`, `인식 이미지`, 필요하면 사용자 export PNG를 근거로 capture → candidate → title ROI → OCR/visual matcher → catalog → presentation → inventory gate → overlay → resource usage를 분리합니다. Live evidence 없이 recognition threshold를 추측 변경하지 않습니다.

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 장기 결정
- `docs/SCANNER.md` — Scanner 계약
- `docs/SCANNER_V1.3.0_WORKFLOW.md` — v1.3.0 Scanner workflow delta
- `docs/SCANNER_TEST_PLAN.md` — Scanner 검증
- `docs/DEVELOPER_REFERENCE.md` — 구현/참조 지도
- `docs/RELEASE_1.3.0.md` — current public release record
- `docs/VERSIONING.md` — 버전 정책