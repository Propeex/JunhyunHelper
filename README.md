# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 공개 버전

**v0.1.8 PUBLIC RELEASE — Windows x64**

**다운로드:** https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.8

v0.1.8은 v0.1.7 이후 확인된 Quest availability, 지도/MiniMap 상태 동기화, Items UI/성능, Ammo 탐색 문제를 정리한 패치 릴리즈입니다.

- live `dialogue` Quest 12건 전수 감사 및 검증된 prerequisite 복원
- unknown/new dialogue는 계속 fail-closed `확인 필요`
- 지도 마커 설정 재시작 영속화 충돌 수정
- Main Map selector ↔ tracker ↔ MiniMap map key 동기화 강화
- `나들목` 표시를 `인터체인지`로 변경
- 지도 Quest sidebar / 유동 제출 아이템 행 정렬 통일
- Inventory/Hideout 변경 시 불필요한 Quest 전체 재계산 제거
- Ammo 이름·구경 검색 및 검색 결과 정확한 row 이동
- Ammo 상세정보 패널 접기/펼치기
- Content schema v7 / v3~v7 readable
- `user.db` SQLite schema v1 유지
- **v0.1.7 → v0.1.8 필수 데이터 업데이트 없음**

공개 릴리즈 검증:

```text
release baseline: 1605d4bc9838486c6290827cebc10d9f3fd57d84
candidate PR CI: 31991531760 — SUCCESS
main CI: 31999094668 — SUCCESS
release workflow: 31999304667 — SUCCESS
automated tests: 203 passed / 0 failed / 0 skipped
Windows x64 self-contained single-file publish: SUCCESS
startup + Main Map + Factory + MiniMap runtime smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
asset: Junhyun-Helper-v0.1.8-win-x64.zip
asset size: 74,057,364 bytes
SHA-256: 0a75f1a2a987e6eec41307eea6149090db90f9855e51b2e72e3a4708d22b9394
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
  - 이름/구경 검색
  - 검색 결과 클릭 시 해당 caliber table + 정확한 ammo row 선택
  - 탄약/수급 경로 상세정보 접기/펼치기
- 온라인 Game Content 안전 업데이트와 image cache
- Map + MiniMap
  - 현재 Quest sidebar / A·B·C marker identity
  - 일반 marker / PMC·Scav·Transit 탈출구
  - floor / zoom / MiniMap 크기 hotkey
  - 타층 marker 유지 + 현재층/위층/아래층 relation 표시
  - MiniMap opacity / temporary transparency / marker scale
  - screenshot 기반 Map 전환 / player tracking
  - MiniMap 층 변경 시 같은 화면 구도에서 floor layer만 교체
  - 지도 마커 설정 영속화
  - Main Map / MiniMap map identity 동기화 강화
- 상단 `스캐너` 탭 — 현재 `준비 중` placeholder 유지

## Quest 정확도 기준

- 서로 다른 `taskRequirements`는 AND
- 한 requirement 내부 `status[]`는 OR
- `complete` / `active` / `failed`의 source 의미를 보존
- 별도 `수주 가능` 상태를 만들지 않고 받을 수 있는 Quest는 Helper에서 즉시 수락한 것으로 간주
- source가 직접 제공한 prerequisite를 compatibility overlay가 더 강한 조건으로 바꾸지 않음
- BTR Driver 누락 gate는 `A Helping Hand = Active`로만 보강
- Ref 누락 gate는 현재 GameMode의 검증된 unlock Quest `Complete`로만 보강
- Lightkeeper는 최초 해금 이후 접근 상실/복구가 가능하므로 별도 special trader access로 판정
- EFT profile-variable requirement는 exact current value가 있을 때 정확 판정
- 2026-08-17 live `dialogue` 12건은 exact Quest ID 기반 감사 규칙으로 처리
  - 시작 Quest 3개는 opaque dialogue gate 제거
  - 나머지 9개는 검증된 prerequisite / minimum level 복원
  - Introduction의 `Active` prerequisite 보존
- allowlist 밖의 새 dialogue, exact 값을 알 수 없는 profile variable, 실제 완료 시각 기반 delay는 임의 추측하지 않고 `확인 필요`

## Map / MiniMap 안정화 기준

- 서로 다른 floor라는 이유만으로 일반 marker를 숨기지 않음
- current/above/below floor relation을 별도 presentation으로 표현
- 알려진 타층 marker도 유지
- 실제 동일 물리 extract의 semantic duplicate 정규화 유지
- Main Map floor 변경 시 live zoom + map-space viewport center 보존
- MiniMap floor 변경 시 **exact live Scale + Translate X/Y 보존**
- MiniMap 다층 지도는 같은 canonical SVG canvas의 floor layer이므로 층별 임의 zoom 보정값을 만들지 않음
- 제품용 Map marker 설정은 저장된 제품 설정을 권위값으로 복원
- Main Map selector와 shared tracker의 map key를 동기화하여 MiniMap의 stale map identity를 방지

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
- v0.1.8 Content schema는 v7이며 v3~v6 snapshot도 오프라인에서 읽을 수 있습니다.
- 다음 정상 데이터 업데이트가 성공하면 v7 snapshot으로 저장됩니다.
- `user.db` SQLite schema는 v1 그대로입니다.

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — 현재 프로젝트 상태
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 공식 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 장기 설계 결정
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 기술 구조
- [`docs/QUEST_PREREQUISITE_SEMANTICS.md`](docs/QUEST_PREREQUISITE_SEMANTICS.md) — 현재 Quest 선행조건 의미
- [`docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md`](docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md) — EFT 1.1 profile-variable Quest gate 감사
- [`docs/DIALOGUE_GATE_AUDIT_2026-08-17.md`](docs/DIALOGUE_GATE_AUDIT_2026-08-17.md) — live dialogue Quest 12건 감사
- [`docs/MINIMAP_FLOOR_FRAME_2026-08-17.md`](docs/MINIMAP_FLOOR_FRAME_2026-08-17.md) — MiniMap exact floor-frame 계약
- [`docs/USABILITY_STABILITY_PASS_2026-08-17.md`](docs/USABILITY_STABILITY_PASS_2026-08-17.md) — v0.1.8 usability/stability 구현 기록
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
- [`docs/RELEASE_0.1.8.md`](docs/RELEASE_0.1.8.md) — v0.1.8 공개 검증 기록
