# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 상태

**v0.1.4 RELEASE CANDIDATE — Windows x64**

현재 공개 버전은 **v0.1.3**입니다.

**현재 공개 다운로드:** https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.3

v0.1.4는 Main Map의 타층 marker/extract 표시와 Factory 중복 extract를 수정하고, 프로그램만으로 해금 여부를 증명할 수 없는 Quest를 `진행 중`이 아닌 `확인 필요`로 분리하는 패치입니다.

> **v0.1.3 → v0.1.4:** 필수 `데이터 업데이트`가 없습니다. Content schema v5와 `user.db`는 변경하지 않습니다.

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
  - floor hotkey 전환 시 zoom/지도 중심 위치 보존
  - 타층 marker 유지 + 현재층 초록 / 위층 빨강 / 아래층 파랑 compact ring
  - MiniMap opacity / temporary hide / marker scale
  - screenshot 기반 Map 전환 / player tracking
- 상단 `스캐너` 탭 — 현재 `준비 중` placeholder 유지

## v0.1.3 Map/MiniMap 핫픽스

v0.1.2 실사용에서 확인된 지도 탭 지연과 타층 marker 표시 회귀를 수정했습니다.

- Main Map 표준 marker 전체를 200ms마다 순회하던 영구 UI polling 제거
- marker tree/map/floor가 실제로 바뀔 때만 one-shot debounce로 floor presentation 갱신
- 신뢰 가능한 Quest height가 없는 경우 `main`으로 추측하지 않고 floor unknown으로 유지
- MiniMap의 중복 off-floor renderer/timer 제거, 기존 canonical marker/extract 경로로 통합
- Quest/Raider scale 갱신의 별도 polling을 event/signature 기반으로 전환
- MiniMap Raider가 floor/zoom/marker-scale/container reload 뒤에도 올바른 `↑/↓`, opacity, scale을 유지하도록 수정
- legacy extract refresh가 컨테이너를 비운 뒤 타층 extract가 사라진 채 남는 경우 복구
- v0.1.2의 floor-hotkey zoom + map-space viewport-center 보존 유지

최종 공개 릴리즈 검증:

```text
release baseline: 3c49d4ca5af549afb4a4a5ce376cb6f8869709fb
release workflow: 31835116544 — SUCCESS
176 tests passed / 0 failed
Windows x64 self-contained single-file publish: SUCCESS
ProductVersion: 0.1.3
Main Map + MiniMap runtime smoke: SUCCESS
floor-hotkey viewport preservation: SUCCESS
normal Main Window close / process exit: SUCCESS
public ZIP re-download + SHA-256 verification: SUCCESS
```

## v0.1.2 사용성 패치

- 층 단축키로 floor를 바꿔도 기존 zoom과 보고 있던 지도 중심 위치를 유지
- 다른 층 marker를 숨기지 않고 약 50% 투명도 + 위층 `↑` / 아래층 `↓`로 구분
- Main Map / MiniMap의 Quest·일반 marker·탈출구·Raider 층 표현 통일
- 유동 제출 상태 dropdown `필요 / 전체 / 충분`; 모두 모은 그룹은 기본 `필요` 목록에서 자동 제외
- Item 상세에 canonical Wiki URL 기반 `위키` 버튼 추가

## v0.1.1 Quest 정확도 패치

2026-08-15 최신 live 데이터를 기준으로 Quest prerequisite/availability를 다시 감사했습니다.

- `taskRequirements`의 `active / complete / failed` 상태 모델 재검증
- Lightkeeper / BTR Driver / Ref 상인 접근 이후 후속 Quest가 너무 일찍 열릴 수 있던 공백 수정
- `globalVariable` / `dialogue`처럼 현재 User Progress만으로 확정할 수 없는 조건은 추측하지 않고 `확인 필요`로 분리
- 각 GameMode의 시간 지연 Quest 13개에 대한 min/max delay metadata 보존
- 실제 게임 완료 시각을 알 수 없으므로 잘못된 가짜 countdown은 생성하지 않음
- Content snapshot schema v5

실제 current online source 전체 검증:

```text
regular:    517 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pve:        513 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pvp-season: 490 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
validation errors: 0
importer warnings: 0
```

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

## 데이터 원칙

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

실패 candidate가 마지막 정상 Game Content를 덮어쓰지 않으며 Game Content update가 `user.db`를 삭제하거나 덮어쓰지 않습니다. Runtime GPT/AI 의존성은 없습니다.

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — 현재 프로젝트 상태
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 공식 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 장기 설계 결정
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 기술 구조
- [`docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md`](docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md) — 최신 Quest 선행/해금 조건 감사
- [`docs/RELEASE_0.1.1.md`](docs/RELEASE_0.1.1.md) — v0.1.1 릴리즈 기록
- [`docs/USABILITY_REQUIREMENTS_2026-08-15.md`](docs/USABILITY_REQUIREMENTS_2026-08-15.md) — 층/유동 제출/Item Wiki 요구사항과 검증
- [`docs/RELEASE_0.1.2.md`](docs/RELEASE_0.1.2.md) — v0.1.2 릴리즈 기록
- [`docs/RELEASE_0.1.3.md`](docs/RELEASE_0.1.3.md) — v0.1.3 핫픽스 릴리즈 기록
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
