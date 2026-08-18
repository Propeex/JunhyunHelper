# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 공개 버전

**v0.1.13 PUBLIC RELEASE / VERIFIED — Windows x64**

**다운로드:** https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.13

v0.1.13은 v0.1.12의 기능 범위와 실제 렌더 UI 계약을 그대로 유지하면서, **설정 저장·복구와 런타임 오류 격리, 최종 데이터 validation을 강화한 유지보수 릴리즈**입니다. 새로운 제품 기능은 추가하지 않았고, 스캐너 탭도 기존 `준비 중` placeholder 그대로 유지합니다.

### v0.1.13 핵심 변경

- Map / Ammo 설정 저장 안정성
  - 같은 디렉터리의 temporary file을 이용한 atomic replacement
  - 직전 정상 설정을 `.bak` 복구본으로 유지
  - primary JSON 손상 시 정상 backup fallback
  - 손상 primary가 정상 backup을 오염시키지 않도록 보호
  - 설정 저장 실패를 앱 전체 종료로 확대하지 않고 진단 로그로 격리
- Map 입력 안정성
  - slider 연속 변경의 파일 저장을 250ms 단위로 묶음
  - runtime dispose 시 pending 설정 flush
  - Map hotkey / NumPad 0~5 직접 층 선택의 비동기 실패 격리
  - keyboard hook 설치 실패 진단 기록
- Game Content 최종 검증
  - 비어 있는 Quest 제출 아이템 후보 차단
  - Quest / Hideout 필요 수량 `Count <= 0` 차단
  - 정상 Quest / Hideout / Needed Items 계산 의미는 변경 없음
- Scanner
  - 상단 `스캐너` 탭 유지
  - `준비 중` placeholder 유지
  - 실제 Scanner 기능 추가 없음
- v0.1.12에서 도입한 실제 WPF 렌더링 좌표 검증 게이트와 Main Map / Factory / MiniMap smoke를 그대로 유지

공개 릴리즈 검증:

```text
release baseline: f43190494ce91b3adf389e57a3a790fd45db8b20
ProductVersion: 0.1.13+f43190494ce91b3adf389e57a3a790fd45db8b20
maintenance hardening PR #96 CI: 32104689932 — SUCCESS
release candidate PR #97 CI: 32105275116 — SUCCESS
public verification workflow: 32111533861 — SUCCESS
automated tests: 217 passed / 0 failed / 0 skipped
Windows x64 self-contained single-file publish: SUCCESS
published rendered Product UI smoke: SUCCESS
startup + Main Map + Factory + MiniMap runtime smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
asset: Junhyun-Helper-v0.1.13-win-x64.zip
asset size: 74,069,173 bytes
SHA-256: 77a8e5d70bacfa8054fb3eafbe03a892456f17fc63c00776379e2730e55c4120
public ZIP re-download + SHA-256/ProductVersion/package verification: SUCCESS
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
  - 현재 Quest sidebar / A·B·C·D marker identity
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
- Map 제품 설정은 `.bak` recovery copy를 유지
- current Quest sidebar는 checkbox / marker / title 고정 lane으로 렌더하고 실제 title X축을 release smoke에서 검증
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
- **v0.1.12 → v0.1.13 필수 데이터 업데이트 없음**
- 기존 Profile / Quest / Inventory / Hideout / Map 설정과 Ammo 즐겨찾기는 그대로 유지됩니다.

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — 현재 프로젝트 상태
- [`docs/CURRENT_STATE.md`](docs/CURRENT_STATE.md) — 짧은 현재 상태 인덱스
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 공식 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 장기 설계 결정
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 기술 구조
- [`docs/QUEST_PREREQUISITE_SEMANTICS.md`](docs/QUEST_PREREQUISITE_SEMANTICS.md) — Quest 선행조건 의미
- [`docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md`](docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md) — EFT 1.1 trader task-pool 감사
- [`docs/DIALOGUE_GATE_AUDIT_2026-08-17.md`](docs/DIALOGUE_GATE_AUDIT_2026-08-17.md) — dialogue Quest 감사
- [`docs/RENDERED_UI_ALIGNMENT_FIX_2026-08-17.md`](docs/RENDERED_UI_ALIGNMENT_FIX_2026-08-17.md) — 실제 렌더 UI 정렬 수정 및 검증 계약
- [`docs/MINIMAP_FLOOR_FRAME_2026-08-17.md`](docs/MINIMAP_FLOOR_FRAME_2026-08-17.md) — MiniMap exact floor-frame 계약
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
- [`docs/RELEASE_0.1.13.md`](docs/RELEASE_0.1.13.md) — v0.1.13 공개 검증 기록
