# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.11.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태입니다. 새로운 실제 회귀, Tarkov 호환성 변화, 또는 사용자가 명시적으로 확정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 추측성 대규모 구조 변경을 시작하지 않습니다.

공식 프로젝트 기억은 대화가 아니라 저장소 문서와 코드입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 사람이 읽는 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태와 유지 계약
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 주요 설계/제품 결정
- `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md` — 구현 구조

## 현재 공개 릴리즈

```text
version: v1.11.2
Desktop target version: 1.11.2
exact product release source/tag target:
5822757f6490ec82aab33793752e48de14490628
PR: #232 — MERGED
PR exact-head CI: 33307979144 — SUCCESS
exact-main CI: 33308162829 — SUCCESS
exact-main Shutdown Race CI: 33308162797 — SUCCESS
exact-main Documentation Consistency: 33308162850 — SUCCESS
Release workflow: 33308291656 — SUCCESS
release id: 379257951
470 passed / 0 failed / 0 skipped
published UTC: 2026-08-30T11:11:52Z
```

Public package:

```text
Junhyun-Helper.zip
asset id: 536514791
bytes: 80,554,866
SHA-256:
d013ac2d423d2a83c49e1e6483dcad038a3792a5b865c1400085fd56e25592a9
```

Checksum asset:

```text
SHA256SUMS.txt
asset id: 536514792
bytes: 86
asset SHA-256:
4860aceab06843707951dcd50951a62843d40ef7a2ea2a9d8efa7972847aa657
```

Exact-main CI artifact:

```text
JunhyunHelper-win-x64
artifact id: 9731167378
archive bytes: 241,597,223
archive SHA-256:
5eef3f620d46f3ac3c7990ec18fdcf46877741fc2c1647a856b3accb2fa26c8b
```

GitHub `/releases/latest`, release target, `refs/tags/v1.11.2`, exact-main product source가 모두 `5822757f6490ec82aab33793752e48de14490628`로 일치함을 확인했습니다. 공개 release는 `draft=false`, `prerelease=false`입니다. Release workflow는 exact-main CI에서 검증·업로드한 artifact를 사용하며 별도의 다른 바이너리를 재빌드하지 않습니다.

공식 v1.11.2 공개 기록:

- `docs/RELEASE_1.11.2.md`
- `docs/RELEASE_NOTES_V1.11.2.md`
- `docs/.release-v1.11.2-status.json`

이 README와 이후 documentation-only commit은 v1.11.2 제품 릴리즈 소스가 아닙니다. v1.11.2의 product source/tag/assets는 위 exact source에 고정된 historical release identity입니다.

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

## v1.11.2 — 실사용 유지보수

### `교정 데이터 추가` 단축키

레이드 중 전역 단축키는 **capture/save 전용**입니다.

- 최신 Scanner evidence가 있으면 Saved Case로 저장합니다.
- 저장 성공 시 Mini Scanner에 `저장 완료`를 잠시 표시합니다.
- Saved Cases/교정 데이터 창을 자동으로 열지 않습니다.
- Main Window나 Scanner 탭으로 포커스를 강제로 이동하지 않습니다.
- hotkey는 Ground Truth를 자동 생성·추측하지 않습니다.

따라서 레이드 중에는 저장만 하고, 교정 데이터 검토는 사용자가 나중에 직접 열어 수행할 수 있습니다.

### Items / Hideout 검색창

Items와 Hideout 검색창은 Quest 검색창과 같은 canonical inline clear UI를 사용합니다.

- 검색어가 비어 있으면 `×`가 보이지 않습니다.
- 텍스트를 입력할 때만 검색창 오른쪽에 `×`가 표시됩니다.
- 클릭하면 기존 검색 이벤트 경로를 통해 검색어가 삭제됩니다.
- 삭제 후 검색창으로 keyboard focus가 돌아옵니다.

v1.11.1에서 중복으로 삽입됐던 always-visible 별도 버튼은 제거했습니다.

### 지도 플레이어 위치/방향

screenshot에서 얻은 player 위치는 각 맵의 `playerMarkerTransform`을 사용해 지도 좌표로 투영합니다. v1.11.1까지 방향은 같은 좌표계 변환을 일관되게 사용하지 않았습니다.

v1.11.2부터는 방향 벡터에도 위치와 동일한 affine transform의 선형부를 적용합니다.

- Factory의 약 90° 방향 오차 경로 수정
- Labs 회전 의미도 같은 일반식으로 처리
- Reserve / Labyrinth처럼 회전된 map transform도 동일 규칙 적용
- Main Map과 MiniMap이 같은 projected heading 사용
- 맵 이름별 임시 angle 보정을 늘리지 않음

위치는 기존 검증된 affine placement 계약을 유지하며, 방향만 같은 map coordinate system에 맞췄습니다.

## Scanner 안전 경계

Scanner는 외부 화면 pixels와 OCR만 사용합니다.

사용하지 않습니다.

- game process memory read
- DLL/code injection
- game/process hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

false positive보다 miss를 선호하며, actual Tarkov evidence 없이 OCR/matcher/candidate acceptance를 임의 완화하지 않습니다.

## 주요 유지 계약

- Game Content update는 candidate → validation → active/LKG 전환의 fail-closed 계약을 유지합니다.
- Hideout FIR은 source `attributes.foundInRaid` 의미를 canonical requirement에 보존합니다.
- Ammo pickup은 same-caliber penetration과 현재 profile의 직접 구매 가능 상태를 기준으로 합니다.
- barter/craft/flea/higher-LL/unproven quest unlock은 현재 직접 구매 가능으로 취급하지 않습니다.
- Ammo Pack은 authoritative `containsItems` 관계를 우선합니다.
- correction hotkey는 evidence-only Saved Case를 저장하고 Ground Truth를 자동 생성하지 않습니다.
- Map/MiniMap donor는 pinned revision `d933792b6042a51cea38dc44b686a096fe30de67`입니다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE smoke까지 검증합니다.

## Schema / compatibility

```text
Desktop target version: 1.11.2
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.11.1 → v1.11.2에서 mandatory Game Content migration, user.db migration, Scanner display settings migration은 없습니다.

## 검증

v1.11.2 exact product source `5822757f6490ec82aab33793752e48de14490628`은 다음을 통과했습니다.

- 470 deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- Items / Hideout conditional inline search clear runtime smoke
- player heading projection deterministic contracts
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback

사용자의 실제 PC/Tarkov 플레이 환경 실사용 검증은 자동화 검증과 별개이며 현재 `PENDING`입니다.

## 개발 원칙

기존 코드를 단순히 현재 동작한다는 이유로 올바른 설계로 간주하지 않습니다. 반대로 근거가 없는 전면 리팩터링도 하지 않습니다. 실제 사용자 증상, 공식 제품 요구사항, 현재 코드와 테스트를 함께 확인해 문제 범위에 비례한 수정을 수행합니다.

새 작업이 시작되면 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 상태를 복구합니다.
