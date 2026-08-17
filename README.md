# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 공개 버전

**v0.1.11 PUBLIC RELEASE — Windows x64**

**다운로드:** https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.11

v0.1.11은 v0.1.10에서 Quest `확인 필요` 수정은 정상 동작했지만, 일부 Items / Ammo / Map UI 변경이 runtime visual-tree 후처리 방식이라 실제 화면에 안정적으로 적용되지 않던 문제를 교정한 릴리즈입니다. 해당 UI는 이제 원본 XAML / 실제 UI 생성 코드 자체에서 직접 구성됩니다.

### v0.1.11 핵심 변경

- v0.1.10의 audited EFT 1.1 Quest availability / LL1 초기 상태 개선 유지
- exact EFT profile variable 값 최우선 정책 유지
- audited LL2~LL4 task-pool reconstruction 유지
- 증명 가능한 pristine LL1 상태만 counter 0으로 확정
- future Needed Items의 unresolved Quest 보호 유지
- flexible hand-in row를 원본 XAML에서 직접 구성
  - 68px 고정 행
  - 44px icon frame
  - icon + 이름/분류 좌측 정렬
  - 인레이드/일반 보유량 우측 고정 lane
  - runtime layout rewrite 제거
- Ammo header/detail layout을 원본 XAML에 직접 구성
  - 중복 `구경`, `즐겨찾기` label 제거
  - caliber selector 160px
  - favorite `☆ / ★` button 38px
  - favorites selector 170px
  - 중앙 detail toggle + detail host 직접 배치
- Map current Quest sidebar를 생성 시점부터 3열 구조로 구성
  - `30px checkbox | 34px A·B·C marker | remaining quest title`
  - title 좌측 정렬 + ellipsis
  - runtime `LegacyMapQuestSidebarPolishBridge` 제거

공개 릴리즈 검증:

```text
release baseline: 88a732c70380b4c764634eff6fd01a16eb849b14
ProductVersion: 0.1.11+88a732c70380b4c764634eff6fd01a16eb849b14
feature PR #92 CI: 32014857527 — SUCCESS
feature main CI: 32015175679 — SUCCESS
release candidate PR #93 CI: 32015691464 — SUCCESS
release baseline main CI: 32015968523 — SUCCESS
release workflow: 32018616694 — SUCCESS
automated tests: 210 passed / 0 failed / 0 skipped
Windows x64 self-contained single-file publish: SUCCESS
startup + Main Map + Factory + MiniMap runtime smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
asset: Junhyun-Helper-v0.1.11-win-x64.zip
asset size: 74,063,248 bytes
SHA-256: 1293cc20c09240c4bdafd6fb45ecb5d0bc37857e12e58f60e31dff620e01b426
public ZIP re-download + SHA-256 verification: SUCCESS
```

## 주요 기능

- GameMode별 Profile 관리
- Quest 진행/잠김/사용 불가/완료 판정과 선행 Quest 연결
- Quest 제출 Item / 자동 소비·rollback ledger
- Hideout 레벨 / 미래 업그레이드 재료
- 미래 Quest + Hideout 기준 Needed Items
- FIR / 일반 Inventory와 안전한 cleanup 계산
- flexible hand-in 그룹 + `필요 / 전체 / 충분` 상태 필터
- Item 종류/용도/필요 상태 필터, cross-navigation, Item Wiki
- Ammo 성능/수급처/Armor Class 1~6 비교와 caliber favorites
- 온라인 Game Content 안전 업데이트와 image cache
- Map + MiniMap
  - 현재 Quest sidebar / A·B·C marker identity
  - 일반 marker / PMC·Scav·Transit 탈출구
  - floor / zoom / MiniMap 크기 hotkey
  - 타층 marker 유지 + 현재층/위층/아래층 relation 표시
  - MiniMap opacity / temporary transparency / marker scale
  - screenshot 기반 Map 전환 / player tracking
  - MiniMap 층 변경 시 같은 화면 구도에서 floor layer만 교체
- 상단 `스캐너` 탭 — 현재 `준비 중` placeholder 유지

## Quest 정확도 기준

- 서로 다른 `taskRequirements`는 AND
- 한 requirement 내부 `status[]`는 OR
- `complete` / `active` / `failed`의 source 의미 보존
- 별도 `수주 가능` 상태를 만들지 않고 받을 수 있는 Quest는 Helper에서 즉시 수락한 것으로 간주
- source가 직접 제공한 prerequisite를 compatibility overlay가 더 강한 조건으로 바꾸지 않음
- BTR Driver 누락 gate는 `A Helping Hand = Active`로만 보강
- Ref 누락 gate는 현재 GameMode의 검증된 unlock Quest `Complete`로만 보강
- Lightkeeper는 최초 해금 이후 접근 상실/복구가 가능하므로 별도 special trader access로 판정
- 12개 audited dialogue gate는 exact-ID compatibility를 적용하고 새/변경 dialogue는 추측하지 않음
- EFT profile-variable requirement는 exact current value가 있으면 정확 판정
- exact 값이 없으면 current EFT 1.1의 감사된 27-ID 구조가 완전히 일치하는 LL2~LL4 task-pool만 runtime에서 복원
- LL1은 audited 구조 + 현재 LL1 + 해당 trader 완료 Quest 0개의 초기 상태만 counter 0으로 확정
- 그 밖의 LL1 task-pool과 실제 완료 시각 기반 delay는 증명할 수 없으면 `확인 필요`
- compatibility 구조가 upstream과 달라지면 자동으로 fail-closed

## Needed Items 안전성

Quest 화면의 current task-pool compatibility는 future item cleanup을 낙관적으로 바꾸지 않습니다.

- missing future profile-variable fact는 계속 `IndeterminatePotential`로 보호
- unresolved future Quest의 Item도 Needed Items에 포함
- flexible hand-in 후보도 cleanup protection 유지

따라서 `확인 필요`를 줄이기 위해 실제 필요한 Item을 잘못 `정리 가능`으로 판단하도록 완화하지 않습니다.

## Map / MiniMap 안정화 기준

- 서로 다른 floor라는 이유만으로 일반 marker를 숨기지 않음
- current/above/below floor relation을 별도 presentation으로 표현
- 알려진 타층 marker도 유지
- 실제 동일 물리 extract의 semantic duplicate 정규화 유지
- Main Map floor 변경 시 live zoom + map-space viewport center 보존
- MiniMap floor 변경 시 **exact live Scale + Translate X/Y 보존**
- Main Map selector와 MiniMap shared map key 동기화
- `퀘스트 마커 표시`를 포함한 product setting은 `%LocalAppData%/JunhyunHelper/map-product-settings.json`에서 복원
- current Quest sidebar는 생성 시점부터 checkbox / marker / title lane으로 고정 정렬
- MiniMap hover transparency는 dedicated lightweight 16ms input check 사용

## 실행

1. GitHub Release에서 최신 `Junhyun-Helper-vX.Y.Z-win-x64.zip`을 다운로드합니다.
2. 원하는 폴더에 압축을 풉니다.
3. **`준현 헬퍼.exe`**를 실행합니다.

배포 루트:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

Windows x64 portable / self-contained single-file 빌드이므로 별도 .NET 설치나 관리자 권한은 필요하지 않습니다. 코드 서명은 아직 적용하지 않아 Windows SmartScreen 경고가 표시될 수 있습니다.

사용자 데이터와 로그는 프로그램 폴더가 아니라 `%LocalAppData%/JunhyunHelper`에 저장됩니다.

## 데이터 업데이트 / 호환성

```text
online source
→ download
→ 외부 형식/필수 의미 검증
→ canonical 변환
→ candidate DB
→ 관계/read-back 검증
→ active 교체
→ User Progress와 결합
```

- 실패 candidate가 마지막 정상 Game Content를 덮어쓰지 않습니다.
- Game Content update가 `user.db`를 삭제하거나 덮어쓰지 않습니다.
- Runtime GPT/AI 의존성은 없습니다.
- Content schema는 v7이며 v3~v7 snapshot을 오프라인에서 읽을 수 있습니다.
- `user.db` SQLite schema는 v1 그대로입니다.
- **v0.1.10 → v0.1.11 필수 데이터 업데이트 없음**

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — 현재 프로젝트 상태
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 공식 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 장기 설계 결정
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 기술 구조
- [`docs/QUEST_PREREQUISITE_SEMANTICS.md`](docs/QUEST_PREREQUISITE_SEMANTICS.md) — Quest 선행조건 의미
- [`docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md`](docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md) — EFT 1.1 trader task-pool 감사 및 current-version compatibility 경계
- [`docs/DIALOGUE_GATE_AUDIT_2026-08-17.md`](docs/DIALOGUE_GATE_AUDIT_2026-08-17.md) — dialogue Quest 감사
- [`docs/FEEDBACK_FIXES_2026-08-17.md`](docs/FEEDBACK_FIXES_2026-08-17.md) — 성능/UI/Quest feedback 수정 기록
- [`docs/MINIMAP_FLOOR_FRAME_2026-08-17.md`](docs/MINIMAP_FLOOR_FRAME_2026-08-17.md) — MiniMap exact floor-frame 계약
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
- [`docs/RELEASE_0.1.11.md`](docs/RELEASE_0.1.11.md) — v0.1.11 공개 검증 기록
