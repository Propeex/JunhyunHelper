# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.11.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

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
version: v1.11.1
Desktop target version: 1.11.1
exact product release source/tag target:
6314eaf866539747eadd69f8da4450bd8d5939e1
PR: #229 — MERGED
PR validated CI: 33302240850 — SUCCESS
exact-main CI: 33302387606 — SUCCESS
exact-main Shutdown Race CI: 33302387623 — SUCCESS
exact-main Documentation Consistency: 33302387611 — SUCCESS
Release workflow: 33302514984 — SUCCESS
release id: 379226665
460 passed / 0 failed / 0 skipped
published UTC: 2026-08-30T08:49:26Z
```

Public package:

```text
Junhyun-Helper.zip
asset id: 536370979
bytes: 80,553,167
SHA-256:
0480dca11f93472cee1396d5faae9362a8b04398a6c18bfd163dc84b9aef4e1b
```

Checksum asset:

```text
SHA256SUMS.txt
asset id: 536370978
bytes: 86
asset SHA-256:
233dfca51bc7d280093da728cb76374e0f10b310e127f43139a5177d55a85b20
```

Exact-main CI artifact:

```text
JunhyunHelper-win-x64
artifact id: 9729389953
archive bytes: 241,592,817
archive SHA-256:
770d89c56f39e379438702dbfb3f15ff0b681a1cd6794503fa1d45eece5061da
```

GitHub `/releases/latest`와 `refs/tags/v1.11.1` readback에서 v1.11.1이 `draft=false`, `prerelease=false`, latest stable이며 release target과 tag ref가 exact product release source와 일치함을 확인했습니다. Release workflow는 exact-main CI artifact의 EXE 버전, `FIRST_RUN_KO.txt`, ZIP checksum manifest를 검증한 뒤 공개했습니다.

공식 v1.11.1 릴리즈 기록:

- `docs/RELEASE_1.11.1.md`
- `docs/RELEASE_NOTES_V1.11.1.md`
- `docs/.release-v1.11.1-status.json`

이 README와 이후 documentation-only commit은 v1.11.1 제품 릴리즈 소스가 아닙니다. v1.11.1 product source/tag/assets는 위 `6314eaf866539747eadd69f8da4450bd8d5939e1` 기준의 immutable historical product release identity입니다.

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

## v1.11.1 — Scanner 설정 / 검색 / 교정 저장 UX

### Scanner 탄약 판단 설정

- v1.11.0에서 이미 제공하던 탄약 `주워야 함` 판단을 Scanner 설정의 정식 정보 항목 **`탄약 줍기 판단`**으로 연결했습니다.
- Mini Scanner에서 표시/숨김과 정보 순서를 사용자가 설정할 수 있습니다.
- Scanner display settings schema는 v9이며 기존 v8 설정은 탄약 판단이 보이는 상태를 보존해 자동 정규화됩니다.

### Items / Hideout 검색

- 아이템과 은신처 탭 검색창에 검색어를 즉시 지우는 `×` 버튼을 추가했습니다.
- 기존 검색 이벤트/필터 경로를 그대로 사용하며 지운 뒤 검색창으로 포커스를 복귀합니다.

### 교정 데이터 저장 피드백

- `교정 데이터 추가` 전역 단축키로 Saved Case 저장에 성공하면 Mini Scanner에 **`저장 완료`**를 잠시 표시합니다.
- 기존 아이템 표시가 있으면 유지하며, Mini Scanner가 닫혀 있어도 짧은 status-only 피드백을 표시할 수 있습니다.
- evidence-only Saved Case와 Ground Truth ownership 계약은 변경하지 않았습니다.

### 회귀 검증 강화

- 실제 published EXE smoke가 Scanner 설정의 `탄약 줍기 판단`, Items/Hideout `×` 동작, Mini Scanner `저장 완료` 렌더를 직접 확인합니다.
- RC 중 발견된 stale schema-v8 startup smoke를 v9로 갱신하고, 탄약 판단 정보의 순서 이동/숨김까지 runtime contract로 고정했습니다.

## v1.11.0 — Scanner / Ammo / Map 유지보수 기준선

### Map / MiniMap

- Main Map을 변경한 뒤 MiniMap을 **처음 켜는 순간**에도 최신 지도 선택을 replay합니다.
- donor의 Extract checkbox가 늦게 생성되어 marker 설정 목록에서 빠질 수 있던 lifecycle 경로를 보강했습니다.
- Player Marker Size 변경으로 donor visual tree가 갱신된 뒤에도 MiniMap Marker Size, Name Size, visibility/category/edge-label 등 현재 presentation을 다시 적용합니다.
- donor marker refresh가 container clear 뒤 취소되어 marker layer가 비는 경우를 확인했고, 이전에 정상 marker가 있던 같은 map/floor에서만 bounded one-shot recovery를 수행합니다.

### Scanner / Hideout FIR / Ammo

- Mini Scanner의 `플리마켓 최저가` 사용자 표시를 제거하되 underlying compatibility data는 유지합니다.
- 설정 가능한 `교정 데이터 추가` 전역 단축키를 제공하며 기본값은 `Ctrl+Alt+F9`입니다.
- 최신 evidence가 없으면 `저장할 스캔 결과가 없습니다.`만 표시하며 Ground Truth를 자동 생성하지 않습니다.
- Hideout item requirement의 `attributes.foundInRaid` 의미를 canonical content에 보존합니다.
- 같은 caliber의 penetration과 현재 Trader Loyalty Level을 기준으로 ammo pickup을 판정합니다.
- 현재 LL의 현금 직접 판매와 확인된 quest unlock만 구매 가능으로 인정하며 barter/craft/flea/higher-LL offer는 제외합니다.
- Ammo Pack은 authoritative `containsItems` 관계를 우선해 실제 contained canonical ammo로 resolve합니다.

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

CI 및 published EXE 자동 검증과 사용자의 실제 PC/Tarkov 실사용 검증은 별도로 관리합니다. v1.11.1의 사용자 실제 PC/Tarkov 플레이 검증은 아직 **PENDING**입니다.

과거 릴리즈의 상세 기록은 각 `docs/RELEASE_*.md`, `docs/RELEASE_NOTES_*.md`, GitHub Releases에 보존됩니다.
