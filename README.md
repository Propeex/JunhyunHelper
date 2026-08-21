# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 릴리즈 상태

**v1.1.0 PUBLIC RELEASE / VERIFIED — Windows x64**

v1.1.0은 v1.0.0의 기존 Profile / Quest / Hideout / Items / Ammo / Map / MiniMap 동작을 유지하면서 실제 **Scanner + Mini Scanner** 기능을 추가한 MINOR 릴리즈입니다.

```text
release: v1.1.0
exact release source / target SHA: ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
asset: Junhyun-Helper-v1.1.0-win-x64.zip
bytes: 80,235,043
SHA-256: 8e7f452701f866c84e753c1c34951af64f4415947e9f56c56634e2b584d9e1ce
ProductVersion: 1.1.0+ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
automated tests: 243 passed / 0 failed / 0 skipped
public downloaded EXE smoke: SUCCESS
```

상세 공개 검증 기록: [`docs/RELEASE_1.1.0.md`](docs/RELEASE_1.1.0.md)

### v1.1.0 Scanner

실사용:

```text
스캐너 ON
→ EscapeFromTarkov Borderless client-area 감지
→ 상세창 구조 감지
→ 제목 ROI
→ Windows 한국어 OCR
→ 보수적 전체 아이템 catalog 매칭
→ Item ID
→ 기존 준현 헬퍼 데이터
→ Mini Scanner 표시
```

테스트:

```text
테스트 ON
→ 연결된 전체 디스플레이 실시간 capture
→ 동일 detector/OCR/matcher pipeline
```

따라서 Tarkov 전체 스크린샷을 바탕화면이나 이미지 뷰어에 띄운 상태에서도 게임 없이 recognition 경로를 확인할 수 있습니다.

두 모드는 동시에 켜지지 않으며 테스트 모드는 재실행 시 자동 OFF입니다.

Scanner는 게임 메모리 읽기, DLL injection, 패킷 가로채기, process-internal 데이터 접근, 아이콘 기반 식별을 사용하지 않습니다. 실제 scan 순간에는 외부 API 요청도 하지 않습니다.

정확도가 부족하거나 Item identity가 ambiguous하면 강제로 1위 후보를 표시하지 않습니다.

### Scanner 검증 상태

- 한국어 텍스트 OCR 실험: 검증
- 상세보기 이미지 detector 실험: 검증
- 전체 Tarkov screenshot detector/OCR 경로: 검증
- Windows Release build / 243 tests / publish / rendered Product UI / Scanner controls / Map smoke: 검증
- Draft/public package hash/ProductVersion 검증: 검증
- public-downloaded EXE smoke: 검증
- **최신 Tarkov Borderless 인게임 E2E: 공개 후 후속 검증 / PENDING**

사용자 결정에 따라 live Tarkov E2E는 v1.1.0 공개 차단 조건에서 제외했습니다. 실제 게임 환경 문제는 다음 로그를 기준으로 후속 PATCH에서 보정합니다.

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
```

상세: [`docs/SCANNER.md`](docs/SCANNER.md), [`docs/SCANNER_TEST_PLAN.md`](docs/SCANNER_TEST_PLAN.md)

## 주요 기능

- GameMode별 Profile 관리
- Quest 진행/잠김/사용 불가/완료/확인 필요 판정과 선행조건 연결
- Quest 제출 Item / 자동 소비·rollback ledger
- Hideout 레벨 / 미래 업그레이드 재료
- 미래 Quest + Hideout 기준 Needed Items
- FIR / 일반 Inventory와 안전한 cleanup 계산
- flexible hand-in 후보 그룹과 보수적 Item 보호
- Item 종류/용도/필요 상태 filter, cross-navigation, Item Wiki
- Ammo 성능/수급처/Armor Class 1~6 비교와 caliber favorites
- 온라인 Game Content 안전 업데이트와 image cache
- Map + MiniMap
  - Current Quest sidebar / A·B·C·D marker identity
  - 일반 marker / PMC·Scav·Transit 탈출구
  - floor / zoom / MiniMap 크기 hotkey
  - 타층 marker 유지 + 현재층/위층/아래층 relation
  - floor 변경 시 viewport 보존
  - screenshot 기반 Map 전환 / player tracking
- Scanner + Mini Scanner
- 실행 시 사용자 동의형 프로그램 업데이트

## 프로그램 업데이트

일반 실행 시 latest public stable GitHub Release를 확인합니다.

```text
프로그램 실행
→ 최신 stable GitHub Release 확인
→ 새 버전이면 사용자 동의
→ ZIP + SHA-256/package 검증
→ program-owned files 교체
→ 새 버전 자동 재시작
```

- 최신 버전이 없거나 사용자가 거절하면 현재 버전을 그대로 사용합니다.
- GitHub/네트워크 조회 실패는 앱 실행을 막지 않습니다.
- 검증 실패 시 현재 프로그램 파일을 변경하지 않습니다.
- `%LocalAppData%/JunhyunHelper` 사용자 데이터는 프로그램 업데이트 대상이 아닙니다.

## 데이터 / 호환성

```text
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v1.0.0 → v1.1.0 mandatory Game Content update: none
v1.0.0 → v1.1.0 user.db migration: none
```

기존 Profile / Quest / Inventory / Hideout / Map 설정 / Ammo 즐겨찾기는 유지됩니다.

Runtime GPT/AI 의존성은 없습니다.

## 실행 / 배포 형태

현재 공개 asset:

```text
Junhyun-Helper-v1.1.0-win-x64.zip
```

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

Windows x64 portable / self-contained single-file 빌드이며 별도 .NET 설치나 관리자 권한은 필요하지 않습니다. 현재 code signing은 하지 않습니다.

## 정확도 / 안전성 원칙

- source가 제공하는 Quest prerequisite 의미를 보존합니다.
- 증명할 수 없는 availability는 `확인 필요`로 유지합니다.
- unresolved future Quest Item은 Needed Items에서 계속 보호합니다.
- flexible hand-in의 실제 소비 후보를 임의 추측하지 않습니다.
- 설정 JSON은 atomic replacement + `.bak` recovery를 사용합니다.
- Scanner는 false positive보다 miss를 선호합니다.
- 공개 릴리즈 ZIP은 `SHA256SUMS.txt`와 대조합니다.

## 버전 정책

- 새 기능 추가 → **MINOR +1**, PATCH = 0
- 기존 기능 수정/보완/버그 수정/성능·안정성 개선 → **PATCH +1**

따라서 v1.0.0의 Scanner 실제 기능 추가는 v1.1.0입니다. 향후 Scanner live 검증에서 발견되는 기능 보정은 새 사용자 기능이 아니라면 PATCH로 배포합니다.

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — canonical 현재 프로젝트/릴리즈 상태
- [`docs/CURRENT_STATE.md`](docs/CURRENT_STATE.md) — 짧은 현재 상태 인덱스
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 공식 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 현재 유효한 장기 결정
- [`docs/DEVELOPER_REFERENCE.md`](docs/DEVELOPER_REFERENCE.md) — 시스템별 책임/참조/변경 영향
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 기술 구조
- [`docs/VERSIONING.md`](docs/VERSIONING.md) — 버전 정책
- [`docs/SCANNER.md`](docs/SCANNER.md) — Scanner 제품/기술 계약
- [`docs/SCANNER_TEST_PLAN.md`](docs/SCANNER_TEST_PLAN.md) — Scanner 검증 계약
- [`docs/RELEASE_1.1.0.md`](docs/RELEASE_1.1.0.md) — v1.1.0 공개 검증 기록
- [`docs/PROGRAM_UPDATE.md`](docs/PROGRAM_UPDATE.md) — 프로그램 업데이트 계약
- [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) — 배포 계약
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
