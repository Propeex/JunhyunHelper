# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable:

```text
v1.1.3 PUBLIC RELEASE / VERIFIED
release source: 8803f899341859887281ad50135911f4625a64f3
release verification run: 32470606548
asset: Junhyun-Helper-v1.1.3-win-x64.zip
bytes: 80,251,960
SHA-256: 419f6288aa3202f10868f2fe6a4ccac40475753ce4ba8c8c2d9985396c4bf493
ProductVersion: 1.1.3+8803f899341859887281ad50135911f4625a64f3
automated tests: 245 passed / 0 failed / 0 skipped
Draft downloaded EXE smoke: SUCCESS
public-downloaded EXE smoke: SUCCESS
```

v1.1.3은 실제로 잘 작동했던 **Scanner Lab v3.8의 인식 구조를 JunhyunHelper Scanner에 복원한 PATCH 릴리즈**입니다.

## Scanner recognition — v1.1.3

```text
Tarkov / Display pixels
→ RED-X candidates
+
→ rectangle/edge fallback candidates
→ candidate deduplication
→ 최대 8개 title ROI
→ adaptive 4x / 6x / 8x Windows ko-KR OCR
→ current official Korean full-item catalog semantic validation
→ 필요 시 상위 3개 candidate deep OCR
→ 안전하게 Item으로 resolve된 candidate만 inspect window로 확정
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

핵심 원칙:

- geometry 점수만으로 상세창을 즉시 확정하지 않음
- current official Korean full-item catalog를 semantic validator로 사용
- matcher threshold/top1-top2 margin을 인식률 때문에 완화하지 않음
- historical alias 누적 금지
- false positive보다 miss 선호
- scan-time network 없음
- game memory / DLL injection / packet interception / icon identity 없음

상세: [`docs/SCANNER.md`](docs/SCANNER.md), [`docs/SCANNER_LAB_3_8_REFERENCE.md`](docs/SCANNER_LAB_3_8_REFERENCE.md)

## Scanner 탭

```text
상단 bar
  왼쪽: 스캐너 / 테스트
  오른쪽: 아이템 목록 최신화
↓
표시 정보 checkboxes
↓
최근 인식 기록
```

- 상시 설명문 제거
- Foundation 검증 controls는 일반 UI에서 비노출
- Mini Scanner는 별도 edit/reset mode 없이 보이는 동안 직접 drag
- 최근 인식 기록에 OCR/candidate/confidence/성공·보류 판단 표시
- 개발자 로그: `%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)`
- screenshot/raw pixels는 로그에 저장하지 않음

### 실사용 Scanner

```text
스캐너 ON
→ EscapeFromTarkov Borderless client-area
→ Scanner Lab v3.8 candidate/OCR/semantic validation
→ Item ID
→ Mini Scanner
```

### 테스트 Scanner

```text
테스트 ON
→ 모든 연결 디스플레이
→ 동일 recognition/presentation pipeline
```

Tarkov 전체 screenshot을 이미지 뷰어에 띄워 게임 없이 테스트할 수 있습니다. real/test는 상호 배타적이고 test는 session-only입니다.

최신 Tarkov Borderless 실제 E2E는 사용자 결정에 따라 release blocker가 아니며, 공개 후 `scanner.log`를 기준으로 계속 검증합니다.

## 주요 기능

- GameMode별 Profile
- Quest 진행/잠김/사용 불가/완료/확인 필요와 prerequisite
- Quest Item / consumption ledger
- Hideout 진행/미래 재료
- Needed Items / FIR·일반 Inventory / cleanup safety
- Items filter/cross-navigation/Wiki
- Ammo 비교/favorites
- Game Content 안전 업데이트/image cache
- Map + MiniMap
- Scanner + Mini Scanner
- 사용자 동의형 Program Update

## Program Update

```text
latest public stable 확인
→ newer이면 사용자 동의
→ exact Windows ZIP + SHA256SUMS
→ checksum/package 검증
→ program-owned files transaction 교체
→ 새 버전 재시작
```

네트워크/검증 실패는 현재 프로그램 실행을 막지 않고 사용자 데이터는 교체하지 않습니다.

## 데이터 / 호환성

```text
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v1.1.2 → v1.1.3 mandatory Game Content update: none
v1.1.2 → v1.1.3 user.db migration: none
```

기존 Profile / Quest / Inventory / Hideout / Scanner settings/catalog / Map 설정 / Ammo favorites는 유지됩니다.

Runtime GPT/AI 의존성은 없습니다.

## 배포 형태

Windows x64 portable / self-contained single-file.

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

별도 .NET 설치나 관리자 권한은 필요하지 않으며 현재 code signing은 하지 않습니다.

## 버전 정책

- 새 사용자 기능 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/버그 수정/성능·안정성 개선 → PATCH +1

Scanner 첫 공개는 v1.1.0, UI/사용성 보완은 v1.1.1, 인식 회귀 보정은 v1.1.2, Scanner Lab v3.8 recognition restoration은 v1.1.3입니다.

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — canonical 현재 상태
- [`docs/CURRENT_STATE.md`](docs/CURRENT_STATE.md) — 짧은 상태 인덱스
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 현재 결정
- [`docs/SCANNER.md`](docs/SCANNER.md) — Scanner 계약
- [`docs/SCANNER_TEST_PLAN.md`](docs/SCANNER_TEST_PLAN.md) — Scanner 검증
- [`docs/SCANNER_LAB_3_8_REFERENCE.md`](docs/SCANNER_LAB_3_8_REFERENCE.md) — 검증된 Scanner Lab v3.8 reference
- [`docs/RELEASE_1.1.3.md`](docs/RELEASE_1.1.3.md) — v1.1.3 public release 검증 기록
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- [`docs/DEVELOPER_REFERENCE.md`](docs/DEVELOPER_REFERENCE.md)
- [`docs/VERSIONING.md`](docs/VERSIONING.md)
- [`docs/PROGRAM_UPDATE.md`](docs/PROGRAM_UPDATE.md)
- [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md)
