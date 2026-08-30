# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.11.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태입니다. 새로운 실제 회귀, Tarkov 호환성 변화, 사용자가 명시적으로 확정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 추측성 구조 변경을 시작하지 않습니다.

공식 현재 상태 문서:

- `docs/PROJECT_STATE.json` — 중복되기 쉬운 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 사람용 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 중요 결정
- `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md` — 구현 구조

## 현재 공개 릴리즈

```text
version: v1.11.0
Desktop target version: 1.11.0
exact product release source/tag target:
e0a8dd8acc86f8c5675efd0b24cb3006c19ccb1d
PR validated CI: 33298972004 — SUCCESS
exact-main CI: 33299138580 — SUCCESS
exact-main Shutdown Race CI: 33299138567 — SUCCESS
Release workflow: 33299258838 — SUCCESS
release id: 379210317
457 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 536298335
bytes: 80,550,542
SHA-256:
fb1d2f38ab26420d069fa8f0aab899c5e9776ffb072c83312e447289ef6f7c87
```

Exact-main CI artifact:

```text
JunhyunHelper-win-x64
artifact id: 9728381122
archive bytes: 241,586,113
archive SHA-256:
e9f8ac2e6d0349f9b6b7a9856d7d5bae6f6af9f03a91934dacf8a5c8ad77623f
```

GitHub `/releases/latest`와 `refs/tags/v1.11.0` readback에서 v1.11.0이 `draft=false`, `prerelease=false`, latest stable이며 release target과 tag ref가 exact product release source와 일치함을 확인했습니다.

공식 v1.11.0 릴리즈 기록:

- `docs/RELEASE_1.11.0.md`
- `docs/RELEASE_NOTES_V1.11.0.md`
- `docs/.release-v1.11.0-status.json`

이 README와 이후 documentation-only commit은 v1.11.0 제품 릴리즈 소스가 아닙니다. v1.11.0 product source/tag/assets는 위 `e0a8dd8...` 기준의 immutable historical product release identity입니다.

## 설치 / 실행

배포 형태는 Windows x64 portable ZIP입니다.

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

- Windows x64
- .NET 10 WPF
- self-contained executable
- 별도 .NET Runtime 설치 불필요
- installer 없음
- 일반 사용에 관리자 권한 불필요

사용자 데이터는 프로그램 폴더가 아니라 `%LocalAppData%/JunhyunHelper` 아래에 저장됩니다.

## 주요 기능

- GameMode별 Profile / User Progress
- Quest / Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger / cleanup
- Items / cross-navigation
- Ammo / favorites / 현재 프로필 기반 pickup 판단
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth / diagnostics / Saved Case / regression dataset
- Scanner 아이템 정보 DB
- Scanner Favorites / Recents
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## v1.11.0 — Scanner / Ammo / Map 유지보수

### Map / MiniMap

- Main Map을 변경한 뒤 MiniMap을 **처음 켜는 순간**에도 최신 지도 선택을 replay합니다.
- donor의 Extract checkbox가 늦게 생성되어 marker 설정 목록에서 빠질 수 있던 lifecycle 경로를 보강했습니다.
- Player Marker Size 변경으로 donor visual tree가 갱신된 뒤에도 MiniMap Marker Size, Name Size, visibility/category/edge-label 등 현재 presentation을 다시 적용합니다.
- donor marker refresh가 container clear 뒤 취소되어 marker layer가 비는 race를 확인했고, 이전에 정상 marker가 있던 같은 map/floor에서만 bounded one-shot recovery를 수행합니다.

### Scanner

- Mini Scanner의 `플리마켓 최저가` 사용자 표시를 제거했습니다. underlying flea minimum data/model은 호환 목적으로 유지합니다.
- 설정 가능한 `교정 데이터 추가` 전역 단축키를 추가했습니다. 기본값은 `Ctrl+Alt+F9`입니다.
- 최신 Scanner evidence가 없으면 `저장할 스캔 결과가 없습니다.`만 표시합니다.
- 인식 성공/실패/불완전 evidence를 Saved Case로 저장할 수 있지만 hotkey는 Ground Truth를 생성하거나 추측하지 않습니다.

### Hideout FIR

- Hideout item requirement의 `attributes.foundInRaid` 의미를 canonical requirement에 보존합니다.
- FIR requirement에는 non-FIR inventory를 충당하지 않으며 Needed Items/cleanup 파생 상태도 현재 canonical requirement를 기준으로 계산합니다.

### Ammo pickup / Ammo Pack

- 같은 caliber의 penetration과 현재 Trader Loyalty Level을 기준으로 `주워야 함` 여부를 계산합니다.
- 현재 LL의 **현금 직접 판매**만 구매 가능으로 인정합니다.
- barter, craft, flea, 높은 LL은 구매 가능으로 보지 않습니다.
- quest unlock이 필요한 offer는 현재 프로필에서 완료 퀘스트가 실제로 확인된 경우에만 구매 가능으로 인정합니다.
- Ammo Pack은 authoritative `containsItems` 관계를 우선해 실제 contained canonical ammo로 resolve하며, 관계가 비어 있을 때만 제한적인 이름 fallback을 사용합니다.

## Scanner 안전 경계

Scanner는 외부 화면 픽셀 캡처와 OCR만 사용합니다.

사용하지 않는 방식:

- 게임 프로세스 메모리 읽기
- DLL/code injection
- process/game hook
- kernel/driver 접근
- 입력 자동화
- 게임 네트워크 조작
- anti-cheat 우회

## 검증 원칙

실사용 오류나 Tarkov 변화가 발생하면 실제 source/log/runtime state를 확인해 최소 수정하고 deterministic regression → Windows Release build → published EXE smoke → exact-main CI → public release readback 순으로 검증합니다.

CI 및 published EXE 자동 검증과 사용자의 실제 PC/Tarkov 실사용 검증은 별도로 관리합니다. v1.11.0의 사용자 실제 PC/Tarkov 플레이 검증은 아직 **PENDING**입니다.

과거 릴리즈의 상세 기록은 각 `docs/RELEASE_*.md`, `docs/RELEASE_NOTES_*.md`, GitHub Releases에 보존됩니다.
