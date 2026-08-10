# 준현 헬퍼

`JunhyunHelper`는 Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 상태

**v0.1.0 RELEASED — Windows x64**

현재 실제 Desktop 제품에서 다음 기능이 구현되어 있습니다.

- 게임 모드별 Profile 관리
- Quest 진행/잠김/사용 불가/완료 판정과 선행 Quest 연결
- Quest 제출 Item / 자동 소비·rollback ledger
- Hideout 현재 레벨 / 다음 upgrade / 자동 재료 소비·rollback
- 미래 Quest + 미래 Hideout 기준 Needed Items
- FIR(인레이드) / 일반 보유량과 안전한 cleanup 계산
- flexible hand-in 그룹 계산
- Item 종류/용도/필요 상태 필터와 Quest·Hideout 상호 이동
- Ammo 성능/수급처/Armor Class 1~6 비교와 caliber 즐겨찾기
- 온라인 Game Content 안전 업데이트와 이미지 cache
- Map + MiniMap
  - 진행 중 Quest sidebar / A·B·C Quest marker
  - 일반 marker / PMC·Scav·Transit 탈출구
  - 수동 floor selector + 전역 floor hotkey
  - Main Map + MiniMap zoom hotkey
  - MiniMap 크기 hotkey / 기본 투명도 / 일시 투명 / marker 크기
  - screenshot 기반 Map 전환 및 player tracking
- 전체 dark control + rounded ScrollBar theme
- 상단 `스캐너` 탭
  - 현재는 `준비 중` placeholder만 표시
  - 실제 Scanner 동작은 요구사항 확정 후 후속 구현

**공개 다운로드:** [준현 헬퍼 v0.1.0 GitHub Release](https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.0)

Windows 배포 파일: `Junhyun-Helper-v0.1.0-win-x64.zip`

v0.1.0 공식 릴리즈 기록: [`docs/RELEASE_0.1.0.md`](docs/RELEASE_0.1.0.md)

## 실행

배포 빌드는 **Windows x64 portable / self-contained single-file** 형태입니다.

1. GitHub Release에서 `Junhyun-Helper-v0.1.0-win-x64.zip`을 내려받아 원하는 폴더에 압축 해제합니다.
2. **`준현 헬퍼.exe`**를 실행합니다.
3. 처음 실행하면 프로필을 만들고 필요한 게임 데이터를 온라인에서 내려받습니다.

배포 폴더의 루트는 찾기 쉽게 다음만 둡니다.

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

.NET/WPF/SQLite/Skia 런타임 DLL은 `준현 헬퍼.exe` 내부에 묶습니다. Map이 파일 경로로 직접 읽는 검증된 지도/마커 자산만 `Assets/`에 외부 파일로 유지합니다. 프로그램 로그는 실행 폴더가 아니라 `%LocalAppData%/JunhyunHelper/Logs`에 저장합니다.

별도 .NET 설치나 관리자 권한은 필요하지 않습니다. 현재 코드 서명은 하지 않으므로 Windows SmartScreen 경고가 표시될 수 있습니다.

## 가장 중요한 데이터 원칙

온라인 게임 데이터는 다음 흐름으로 갱신합니다.

```text
온라인 source
→ 다운로드
→ 외부 형식/필수 의미 검증
→ canonical 변환
→ candidate DB
→ 관계/read-back 검증
→ active 교체
→ 이미지 cache 준비
→ User Progress와 결합
```

- 일반적인 Tarkov 데이터 내용 변경은 importer 규칙으로 자동 대응합니다.
- 외부 schema/의미가 importer가 이해할 수 없게 바뀌면 update를 실패시킵니다.
- 실패한 candidate가 마지막 정상 Game Content를 덮어쓰지 않습니다.
- Game Content update가 User Progress를 삭제하거나 덮어쓰지 않습니다.
- runtime GPT/AI 의존성은 없습니다.

현재 주요 온라인 데이터 원천은 `json.tarkov.dev`이며 Ammo 비교에는 검증된 보조 원천을 제한적으로 사용합니다.

## 사용자 데이터

기본 사용자 데이터 루트:

```text
%LocalAppData%/JunhyunHelper
```

주요 저장:

```text
user.db
content/<game-mode>/content.db
content/<game-mode>/content.candidate.db
content/<game-mode>/content.previous.db
image-cache/
map-product-settings.json
ammo-favorites.json
Logs/
```

프로그램 ZIP을 새 버전으로 교체해도 위 사용자 진행 데이터와 로그는 프로그램 폴더와 분리되어 유지됩니다.

## Map 데이터 정책

현재 Map/MiniMap은 검증한 `Propeex/Tarkov-Helper` Map subsystem과 bundled artwork/config/general-marker DB를 고정 revision으로 사용합니다. JunhyunHelper의 Quest 진행 상태와 online Quest geometry만 Map에 연결합니다.

```text
Map subsystem = 독립
└─ Quest만 JunhyunHelper current profile/content와 연결
```

Quest/Hideout/Item/Ammo의 온라인 Game Content update와 Map artwork/config bundle update는 별도 시스템입니다. v0.1.0에서는 검증된 Map bundle을 배포물에 포함합니다.

## 기술 스택

- .NET 10
- C#
- WPF
- SQLite
- SkiaSharp — source image decode / PNG normalization
- SharpVectors — SVG Map rendering
- Core / Infrastructure / Application / Desktop 계층 분리

## v0.1.0 검증

공식 v0.1.0 배포물은 다음 검증을 통과했습니다.

- Desktop Release build
- Core/Application/Infrastructure 자동 테스트 — 163 passed / 0 failed
- Windows x64 self-contained single-file publish
- `준현 헬퍼.exe` 실제 startup + Map/MiniMap smoke
- 배포 루트 DLL 0개 / PDB 0개 / 중첩 ZIP 없음
- 실행 후에도 EXE 옆에 `Logs` 등 런타임 폴더가 생기지 않음
- Main Window 정상 close 후 process 종료 확인

GitHub Release Windows ZIP SHA-256:

```text
f3c1a4208fc70b7ec7fb6612933de9d383e4c54e84a7352f529dc7de21550f91
```

Release에는 위 ZIP과 `SHA256SUMS.txt` 두 asset만 게시합니다.

## 개발 문서

새 작업은 대화 기억이 아니라 저장소 문서를 기준으로 이어갑니다.

1. [`AGENTS.md`](AGENTS.md) — 개발자/AI 작업 규약
2. [`docs/STATE.md`](docs/STATE.md) — 현재 상태와 다음 작업
3. [`docs/PRODUCT.md`](docs/PRODUCT.md) — 확정 제품 요구사항
4. [`docs/DECISIONS.md`](docs/DECISIONS.md) — 장기 결정 이력
5. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 현재 기술 구조
6. [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md) — 개발/검증 절차
7. [`docs/REFERENCE_POLICY.md`](docs/REFERENCE_POLICY.md) — 기존 구현 참고 규칙
8. [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
9. [`docs/RELEASE_0.1.0.md`](docs/RELEASE_0.1.0.md) — v0.1.0 릴리즈 기록
10. [`docs/GITHUB_RELEASE_0.1.0.md`](docs/GITHUB_RELEASE_0.1.0.md) — 실제 GitHub Release 검증 기록
