# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 릴리즈

**v1.0.0 PUBLIC VERIFIED — Windows x64**

v0.1.14까지 검증된 사용자 기능을 그대로 유지하면서 first-party 내부 코드, persistence, Map runtime compatibility, 배포 검증과 개발 문서를 최종 하드닝한 첫 정식 안정판입니다.

```text
Release: v1.0.0
Exact release source: 3147ad1b48c3d30df529d95b148c5c444a77d649
Asset: Junhyun-Helper-v1.0.0-win-x64.zip
Size: 74,088,334 bytes
SHA-256: 0e92787409add9dd9e1138277c3588586a04266b05ca56d7cf7fb6f79c88094c
Automated tests: 232 passed
```

정식 배포에서는 exact release source를 다시 빌드·테스트·publish한 뒤 실제 Windows executable의 Product UI, Main Map, Factory, MiniMap, 정상 종료를 검증했습니다. Draft asset을 재다운로드해 checksum/package identity를 검증한 뒤 public/latest로 전환했고, public asset을 다시 다운로드해 동일 검증과 실제 executable smoke를 재실행했습니다.

기존 `v0.*` GitHub Releases 15개는 v1.0.0 public 검증 후 모두 제거했습니다.

### v1.0.0 하드닝

새 사용자 기능을 추가하거나 기존 기능을 축소하지 않았습니다.

- 현재 제품 규칙에서 사용되지 않는 과거 Hideout cleanup compatibility surface 제거
- `user.db` schema initialization의 반복 SQLite I/O 제거
- 온라인 데이터 요청 User-Agent를 product assembly version에서 파생
- project version / ProductVersion / FIRST_RUN / release identity 검증 강화
- release tree의 PDB / nested archive / legacy dependency 오염 차단
- 사라진 과거 Map fork 대신 동일 exact git object가 존재하는 공개 upstream을 fetch origin으로 사용; Map source pin은 유지
- exact release smoke가 발견한 donor current-floor-only late suppression race를 first-party compatibility layer에서 제거
- 전체 시스템의 책임·입력·출력·참조·data flow·변경 영향을 개발자 문서로 공식화
- v1 이후 버전 정책 공식화
- Scanner는 기존대로 상단 `스캐너` 탭의 **`준비 중` placeholder** 유지

상세 감사: [`docs/FINAL_AUDIT_1.0.0.md`](docs/FINAL_AUDIT_1.0.0.md)

## 주요 기능

- GameMode별 Profile 관리
- Quest 진행/잠김/사용 불가/완료/확인 필요 판정과 선행조건 연결
- Quest 제출 Item / 자동 소비·rollback ledger
- Hideout 레벨 / 미래 업그레이드 재료
- 미래 Quest + Hideout 기준 Needed Items
- FIR / 일반 Inventory와 안전한 cleanup 계산
- flexible hand-in 후보 그룹과 보수적 Item 보호
- Item 종류/용도/필요 상태 필터, cross-navigation, Item Wiki
- Ammo 성능/수급처/Armor Class 1~6 비교와 caliber favorites
- 온라인 Game Content 안전 업데이트와 image cache
- Map + MiniMap
  - 현재 Quest sidebar / A·B·C·D marker identity
  - 일반 marker / PMC·Scav·Transit 탈출구
  - floor / zoom / MiniMap 크기 hotkey
  - 타층 marker 유지 + 현재층/위층/아래층 relation 표시
  - Main Map floor 변경 시 zoom + map-space viewport center 보존
  - MiniMap floor 변경 시 exact Scale + Translate frame 보존
  - screenshot 기반 Map 전환 / player tracking
- 상단 `스캐너` 탭 — 현재 `준비 중` placeholder
- 실행 시 사용자 동의형 프로그램 업데이트

## 프로그램 업데이트

일반 실행 시 latest public stable GitHub Release를 확인합니다.

```text
프로그램 실행
→ 최신 정식 GitHub Release 확인
→ 새 버전이 있으면 사용자에게 업데이트 여부 질문
→ 동의 시 ZIP + SHA-256/패키지 검증
→ program-owned files 교체
→ 새 버전 자동 재시작
```

- 최신 버전이 없으면 별도 UI 없이 실행됩니다.
- 사용자가 업데이트를 거절하면 현재 버전을 그대로 사용합니다.
- GitHub/네트워크 조회 실패는 프로그램 실행을 막지 않습니다.
- 다운로드/검증 실패 시 현재 프로그램 파일을 변경하지 않습니다.
- 실제 교체 중 실패하면 기존 program-owned files rollback과 기존 EXE 재실행을 시도합니다.
- `%LocalAppData%/JunhyunHelper`의 사용자 데이터는 프로그램 업데이트 대상이 아닙니다.
- 상시 `Updater.exe` 없이 현재 single-file EXE의 임시 self-copy를 updater mode로 사용합니다.

## 데이터 / 호환성

```text
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v0.1.14 → v1.0.0 mandatory Game Content update: none
v0.1.14 → v1.0.0 user data migration: none
```

기존 Profile / Quest / Inventory / Hideout / Map 설정 / Ammo 즐겨찾기는 유지됩니다.

Game Content update와 Program update는 별도 subsystem입니다.

```text
Game Content update
온라인 데이터 → 검증 → canonical 변환 → candidate DB → 관계/read-back 검증 → active 교체

Program update
GitHub stable Release → 사용자 동의 → ZIP/checksum 검증 → program-owned files 교체 → 재시작
```

Runtime GPT/AI 의존성은 없습니다.

## 배포 형태

정식 asset:

```text
Junhyun-Helper-v1.0.0-win-x64.zip
SHA256SUMS.txt
```

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

Windows x64 portable / self-contained single-file 빌드이며 별도 .NET 설치나 관리자 권한은 필요하지 않습니다. 코드 서명은 현재 필수 범위가 아니므로 Windows SmartScreen 경고가 표시될 수 있습니다.

사용자 데이터와 로그는 프로그램 폴더가 아니라 `%LocalAppData%/JunhyunHelper`에 저장됩니다.

## 정확도 / 안전성 원칙

- source가 제공하는 Quest prerequisite 의미를 보존합니다.
- 증명할 수 없는 availability를 임의로 해금하지 않고 `확인 필요`로 유지합니다.
- exact EFT profile-variable 값이 있으면 해당 값이 권위값입니다.
- current-version compatibility는 감사된 구조가 정확히 일치할 때만 사용하고 drift가 있으면 fail-closed 합니다.
- unresolved future Quest Item은 Needed Items에서 계속 보호합니다.
- flexible hand-in의 실제 소비 후보를 임의 추측하지 않습니다.
- 설정/즐겨찾기 JSON은 atomic replacement + `.bak` recovery를 사용합니다.
- 공개 릴리즈 ZIP은 `SHA256SUMS.txt`와 대조하여 검증합니다.
- Map floor는 visibility filter가 아니라 presentation relation입니다. pinned donor의 legacy floor-only suppression은 first-party compatibility layer가 donor가 직접 floor 때문에 숨긴 marker에 한해서만 복구합니다.

## v1 이후 버전 정책

- 새 기능 추가 → **MINOR +1**, PATCH는 0으로 초기화
- 기존 기능 수정/보완/변경, 버그 수정, 성능·안정성 개선 → **PATCH +1**

예:

- `1.0.0`에서 Scanner 실제 기능 추가 → `1.1.0`
- `1.0.0`에서 Quest 수정 → `1.0.1`
- `1.0.1`에서 Scanner 실제 기능 추가 → `1.1.0`

상세: [`docs/VERSIONING.md`](docs/VERSIONING.md)

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — canonical 현재 프로젝트/릴리즈 상태
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 공식 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 현재 유효한 장기 결정
- [`docs/DEVELOPER_REFERENCE.md`](docs/DEVELOPER_REFERENCE.md) — 시스템별 책임, 입력/출력, 참조, data flow, 변경 영향
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 기술 구조와 layer 경계
- [`docs/VERSIONING.md`](docs/VERSIONING.md) — 공식 버전 정책
- [`docs/PROGRAM_UPDATE.md`](docs/PROGRAM_UPDATE.md) — 프로그램 업데이트 제품/실패 계약
- [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) — 배포 및 공개 검증 계약
- [`docs/RELEASE_1.0.0.md`](docs/RELEASE_1.0.0.md) — v1.0.0 정식 릴리즈 기록
- [`docs/FINAL_AUDIT_1.0.0.md`](docs/FINAL_AUDIT_1.0.0.md) — v1.0.0 전체 하드닝 감사
- [`docs/MAP_RUNTIME_COMPATIBILITY.md`](docs/MAP_RUNTIME_COMPATIBILITY.md) — pinned donor와 JunhyunHelper Map 제품 정책의 runtime compatibility
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
- [`docs/QUEST_PREREQUISITE_SEMANTICS.md`](docs/QUEST_PREREQUISITE_SEMANTICS.md) — Quest 선행조건 의미
