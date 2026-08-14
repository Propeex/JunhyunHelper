# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 버전

**v0.1.1 RELEASED — Windows x64**

**공개 다운로드:** https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.1

Windows 배포 파일:

```text
Junhyun-Helper-v0.1.1-win-x64.zip
SHA-256: 91394101c5011b833c2810d8857fe2e9fd59b9f42f8710b90a899fe8169f0b54
```

> **v0.1.0에서 업그레이드하는 경우:** v0.1.1을 처음 실행한 뒤 상단 **`데이터 업데이트`를 한 번 실행**해 주세요. 최신 Quest 판정 규칙이 포함된 v5 Game Content를 재구축합니다. 기존 프로필, Quest 완료 기록, Inventory, Hideout 진행(`user.db`)은 유지됩니다.

## 주요 기능

- GameMode별 Profile 관리
- Quest 진행/잠김/사용 불가/완료 판정과 선행 Quest 연결
- Quest 제출 Item / 자동 소비·rollback ledger
- Hideout 레벨 / 미래 업그레이드 재료
- 미래 Quest + Hideout 기준 Needed Items
- FIR / 일반 Inventory와 안전한 cleanup 계산
- flexible hand-in 그룹
- Item 종류/용도/필요 상태 필터와 cross-navigation
- Ammo 성능/수급처/Armor Class 1~6 비교와 caliber favorites
- 온라인 Game Content 안전 업데이트와 image cache
- Map + MiniMap
  - 현재 Quest sidebar / A·B·C marker identity
  - 일반 marker / PMC·Scav·Transit 탈출구
  - floor / zoom / MiniMap 크기 hotkey
  - MiniMap opacity / temporary hide / marker scale
  - screenshot 기반 Map 전환 / player tracking
- 상단 `스캐너` 탭 — 현재 `준비 중` placeholder 유지

## v0.1.1 Quest 정확도 패치

2026-08-15 최신 live 데이터를 기준으로 Quest prerequisite/availability를 다시 감사했습니다.

- `taskRequirements`의 `active / complete / failed` 상태 모델 재검증
- Lightkeeper / BTR Driver / Ref 상인 접근 이후 후속 Quest가 너무 일찍 열릴 수 있던 공백 수정
- `globalVariable` / `dialogue`처럼 현재 User Progress만으로 확정할 수 없는 조건은 추측하지 않고 `판정 문제`에 표시
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

1. GitHub Release에서 `Junhyun-Helper-v0.1.1-win-x64.zip`을 다운로드합니다.
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
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
