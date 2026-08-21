# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable:

```text
v1.1.0 PUBLIC RELEASE / VERIFIED
release source: ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
asset: Junhyun-Helper-v1.1.0-win-x64.zip
SHA-256: 8e7f452701f866c84e753c1c34951af64f4415947e9f56c56634e2b584d9e1ce
ProductVersion: 1.1.0+ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
automated tests: 243 passed / 0 failed / 0 skipped
public-downloaded EXE smoke: SUCCESS
```

현재 release candidate:

```text
v1.1.1 — Scanner UI / 최근 인식 기록 / Mini Scanner 직접 이동
```

v1.1.1은 새 기능 확장이 아니라 v1.1.0 Scanner의 사용성 보완이므로 PATCH 릴리즈입니다.

## v1.1.1 Scanner 탭

```text
상단 bar
  왼쪽: 스캐너 / 테스트
  오른쪽: 아이템 목록 최신화
↓
표시 정보 checkboxes
↓
최근 인식 기록
```

사용자 화면에서 제거:

- 상단 Scanner 제목과 상시 설명문
- Scanner/Test/catalog/Mini Scanner 설명문
- 별도 Mini Scanner 위치 편집/초기화 controls
- Foundation verification/preview controls

Foundation의 Item ID → presentation 내부 진단 경로는 유지합니다.

### 최근 인식 기록

각 실제 OCR/matcher 시도에서 다음을 사용자 문장으로 보여줍니다.

- 시각
- 스캐너/테스트 mode
- OCR로 읽은 문자열
- 가장 가까운 공식 Item
- 유사도
- top1/top2 차이
- 식별 성공/보류
- 판단 이유

기존 `%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)`에서 최근 판정을 복원하므로 프로그램을 다시 실행한 뒤에도 최근 기록을 확인할 수 있습니다. screenshot/raw pixels는 저장하지 않습니다.

### Mini Scanner

v1.1.1부터 별도 edit mode 없이 보이는 동안 언제든 직접 left-drag할 수 있고, drag 완료 위치를 저장합니다.

- Topmost 유지
- ShowActivated=false / `WS_EX_NOACTIVATE` 유지
- always-drag를 위해 Mini Scanner 자기 영역의 click-through는 제거
- Mini Scanner 영역은 mouse hit-test를 받지만 게임 keyboard focus는 가져가지 않음

## Scanner 핵심 파이프라인

v1.1.1에서도 v1.1.0의 인식 의미를 유지합니다.

실사용:

```text
스캐너 ON
→ EscapeFromTarkov Borderless client-area
→ detail geometry detector
→ title ROI
→ Windows ko-KR OCR
→ conservative full-item catalog match
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

테스트:

```text
테스트 ON
→ 모든 연결 디스플레이
→ 동일 detector/OCR/matcher/presentation
```

- real/test는 상호 배타적
- test는 session-only
- game memory read 없음
- DLL injection 없음
- packet interception 없음
- process-internal data read 없음
- icon identity 없음
- scan-time network 없음
- low-confidence/ambiguous result는 Item ID를 확정하지 않음
- current needed = `RequiredTotal`

최신 Tarkov Borderless 실제 E2E는 사용자 결정에 따라 release blocker가 아니며 공개 후 로그 기반으로 계속 검증합니다.

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
v1.1.0 → v1.1.1 mandatory Game Content update: none
v1.1.0 → v1.1.1 user.db migration: none
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

## 정확도 / 안전성

- 증명 불가능 Quest availability = `확인 필요`
- unresolved future Quest Item은 계속 보호
- flexible hand-in 실제 소비 후보 임의 추정 금지
- presentation JSON은 atomic replacement + `.bak` recovery
- Scanner는 false positive보다 miss 선호
- 공개 ZIP은 `SHA256SUMS.txt`로 검증

## 버전 정책

- 새 사용자 기능 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/버그 수정/성능·안정성 개선 → PATCH +1

따라서 Scanner 첫 공개는 v1.1.0, 이번 Scanner UI/사용성 보완은 v1.1.1입니다.

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — canonical 현재 상태
- [`docs/CURRENT_STATE.md`](docs/CURRENT_STATE.md) — 짧은 상태 인덱스
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 현재 결정
- [`docs/SCANNER.md`](docs/SCANNER.md) — Scanner 계약
- [`docs/SCANNER_TEST_PLAN.md`](docs/SCANNER_TEST_PLAN.md) — Scanner 검증
- [`docs/SCANNER_UI_DECISION_2026-08-21.md`](docs/SCANNER_UI_DECISION_2026-08-21.md) — DEC-052
- [`docs/RELEASE_1.1.0.md`](docs/RELEASE_1.1.0.md) — v1.1.0 검증 기록
- [`docs/RELEASE_1.1.1.md`](docs/RELEASE_1.1.1.md) — v1.1.1 release record
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- [`docs/DEVELOPER_REFERENCE.md`](docs/DEVELOPER_REFERENCE.md)
- [`docs/VERSIONING.md`](docs/VERSIONING.md)
- [`docs/PROGRAM_UPDATE.md`](docs/PROGRAM_UPDATE.md)
- [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md)
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md)
