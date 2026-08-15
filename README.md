# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 공개 버전

**v0.1.6 PUBLIC RELEASE — Windows x64**

**다운로드:** https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.6

v0.1.6은 Quest 선행조건과 특수 상인 접근 판정의 의미를 정정하고, 향후 데이터 변경에서 잘못된 Quest graph가 조용히 활성화되지 않도록 검증을 강화한 정확도 패치입니다.

- upstream이 이미 제공한 prerequisite 상태를 compatibility overlay가 덮어쓰지 않음
- BTR Driver의 `A Helping Hand = Active` 의미 보존
- Ref의 GameMode별 unlock `Complete` 의미 보존
- Lightkeeper의 recoverable 접근권을 ordinary prerequisite와 분리
- Content schema v6 / v3~v6 readable
- `user.db` SQLite schema v1 유지
- **v0.1.5 → v0.1.6 필수 데이터 업데이트 없음**

공개 릴리즈 검증:

```text
release baseline: 0e4683409b62fd326c5605f1485be896e2216836
candidate CI: 31872459229 — SUCCESS
release workflow: 31872620863 — SUCCESS
190 tests passed / 0 failed / 0 skipped
Windows x64 self-contained single-file publish: SUCCESS
startup + Main Map + Factory + MiniMap runtime smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
asset: Junhyun-Helper-v0.1.6-win-x64.zip
SHA-256: be642e076d265944282ff3edd3a91323e57ced702e839b3111a0779884fd0111
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
  - floor 변경 시 Main Map + MiniMap zoom/지도 중심 위치 보존
  - 타층 marker 유지 + 현재층 초록 / 위층 빨강 / 아래층 파랑 compact ring
  - MiniMap opacity / temporary hide / marker scale
  - screenshot 기반 Map 전환 / player tracking
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
- `globalVariable`, `dialogue`, 실제 게임 완료 시각이 필요한 delay처럼 프로그램이 입증할 수 없는 조건은 `확인 필요`로 유지
- 실제 게임 완료 시각을 알 수 없는 delay에 가짜 countdown을 만들지 않음

## v0.1.5 Map 안정화 기준

v0.1.5에서 확정한 Map/MiniMap 동작은 v0.1.6에서도 그대로 유지합니다.

- 서로 다른 floor라는 이유만으로 일반 marker를 숨기지 않음
- current/above/below floor relation은 compact ring으로 표현
- 알려진 타층 marker도 유지
- 실제 동일 물리 extract의 semantic duplicate 정규화는 유지
- floor 변경 시 Main Map과 MiniMap의 live zoom + map-space viewport center 보존

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
- v0.1.6 Content schema는 v6이며 v3~v5 snapshot도 오프라인에서 읽을 수 있습니다.
- v3~v5의 과거 BTR/Lightkeeper compatibility gate는 읽는 시점에 메모리에서 새 의미로 정규화됩니다.
- 다음 정상 데이터 업데이트가 성공하면 v6 snapshot으로 저장됩니다.
- `user.db` SQLite schema는 v1 그대로입니다.

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — 현재 프로젝트 상태
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 공식 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 장기 설계 결정
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 기술 구조
- [`docs/QUEST_PREREQUISITE_SEMANTICS.md`](docs/QUEST_PREREQUISITE_SEMANTICS.md) — 현재 Quest 선행조건 의미
- [`docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md`](docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md) — Quest 선행/해금 조건 감사
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
- [`docs/RELEASE_0.1.6.md`](docs/RELEASE_0.1.6.md) — v0.1.6 공개 검증 기록
- [`docs/RELEASE_0.1.5.md`](docs/RELEASE_0.1.5.md) — v0.1.5 Map 회귀 패치 기록
