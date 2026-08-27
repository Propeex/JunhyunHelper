# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.7.14 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태입니다. 새로운 실제 회귀·Tarkov 호환성 변화·사용자가 명시적으로 확정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 Scanner 인식 기준 조정을 시작하지 않습니다.

공식 현재 상태:

- `docs/CURRENT_STATE.md` — 짧은 현재 상태 인덱스
- `docs/STATE.md` — 운영 기준과 exact release evidence
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/ARCHITECTURE.md` — 기술 경계/데이터 흐름
- `docs/DEVELOPER_REFERENCE.md` — 다음 개발 세션용 구현 지도
- `docs/MAINTENANCE_CONTRACTS.md` — 유지보수 불변 계약

Scanner 전문 문서:

- `docs/CURRENT_SCANNER_WORK.md`
- `docs/SCANNER.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/SCANNER_GROUND_TRUTH.md`

## 현재 공개 릴리즈

```text
version: v1.7.14
Desktop target version: 1.7.14
exact product release source/tag target: 0a51375de36cd13047216006c2c0311728b1bd89
main CI: 33060827905 — SUCCESS
Release workflow: 33061059154 — SUCCESS
release id: 377720327
stable asset: Junhyun-Helper.zip
asset id: 532104142
bytes: 80,488,363
SHA-256: 341ac502d2ace563ab2e7c8d7091a8e796cf87e7d1f5961edf869feab106e2fd
407 passed / 0 failed / 0 skipped
```

GitHub `/releases/latest` 및 `refs/tags/v1.7.14` readback에서 v1.7.14가 `draft=false`, `prerelease=false`, latest stable이며 release target과 tag ref가 exact product release source와 일치함을 확인했습니다. 공개 ZIP digest도 exact main-CI package SHA-256과 일치합니다.

공식 릴리즈 기록:

- `docs/RELEASE_1.7.14.md`
- `docs/RELEASE_NOTES_V1.7.14.md`
- `docs/.release-v1.7.14-status.json`
- `docs/DECISION_V1.7.14_UI_CONSISTENCY.md`

이 README와 이후 documentation-only commit은 v1.7.14 제품 릴리즈 소스가 아닙니다. v1.7.14 product source/tag/assets는 위 `0a51375d...` 기준의 immutable historical release입니다.

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
- self-contained single-file executable
- 별도 .NET Runtime 설치 불필요
- installer 없음
- 일반 사용에 관리자 권한 불필요

사용자 데이터는 프로그램 폴더가 아니라 `%LocalAppData%/JunhyunHelper` 아래에 저장됩니다.

## 주요 기능

- GameMode별 Profile / User Progress
- Quest availability / prerequisite / special trader / profile-variable
- Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth 교정 / diagnostics / regression dataset
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## v1.7.14 — UI 일관성 패치

v1.7.14는 기존 도메인 의미와 Scanner identity recognition을 유지하면서 popup·설정·검색 interaction을 한 제품 규칙으로 정리했습니다.

### Ammo

- `즐겨찾기 선택`, `표시 열` popup은 열린 상태에서 같은 launcher를 다시 누르면 닫힌 상태를 유지합니다.
- WPF `Popup.StaysOpen=False`의 자동 닫힘 뒤 기존 Click handler가 다시 popup을 여는 문제를 Preview 단계에서 차단했습니다.

### Map / MiniMap

- MiniMap 버튼 주변의 donor 잔여 padding/background와 숨긴 도움말 버튼 자리를 제거했습니다.
- `지도 마커`는 투명 텍스트 영역이 아니라 JunhyunHelper 일반 Button chrome을 사용합니다.
- 접힌 marker panel은 빈 배경/최소 폭을 남기지 않습니다.
- 펼친 panel은 일반 데스크톱 viewport에서 현재 marker checkbox를 가능한 한 스크롤 없이 확인할 충분한 세로 공간을 확보합니다.
- 지도/미니맵 설정은 기존 오른쪽 drawer 대신 MainWindow 공통 in-app overlay에 표시합니다.

### Scanner / Profile

- Scanner `고급`은 별도 Window로 표시하지 않고 MainWindow 공통 overlay에 표시합니다.
- 고급 화면 내부의 별도 `닫기` 버튼을 제거했습니다.
- Scanner hotkey 편집을 Scanner 설정 안으로 통합했고 기존 전용 hotkey window를 제거했습니다.
- 프로필 수정 content card를 Scanner 설정과 같은 overlay/card 계열로 정리했습니다.
- child editor의 기존 validation/save authority는 유지합니다.

### 검색창

Quest / Hideout / Items / Ammo / Scanner의 주요 검색창은 입력창 우측 내부에 `×` clear affordance를 사용합니다. 검색 filtering 의미는 바뀌지 않습니다.

### 공통 overlay interaction

현재 주요 user-facing editor/settings surface는 다음 interaction을 공유합니다.

```text
launcher
→ MainWindow shared overlay
→ same launcher / backdrop / common X → dismiss
```

현재 적용 surface:

- Profile Edit
- Scanner Settings
- Scanner Advanced
- Map / MiniMap Settings

## Scanner

Scanner는 Tarkov 화면 픽셀을 현재 공식 한국어 Tarkov full-item catalog의 Item ID에 연결하는 closed-domain recognizer입니다.

```text
Tarkov window pixels
→ detail rectangle proposals
→ inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user substitution
→ conditional environment-aware title normalization
→ conservative official-catalog matching / bounded recovery
→ optional current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

### Scanner 안전 기준

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive보다 miss를 선호합니다.
- geometry/environment normalization은 Item identity proof가 아닙니다.
- stale/cross-frame OCR 또는 visual result를 current Item identity proof로 사용하지 않습니다.
- Item ID 확정 전 price/needed/slot/previous-frame metadata를 identity evidence로 사용하지 않습니다.
- scan 순간 identity 결정을 위해 network 요청을 시작하지 않습니다.
- reviewed evidence 없이 threshold/candidate/matcher/visual acceptance를 낮추지 않습니다.

Scanner 표시 authority:

```text
needed quantity = ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
needed source   = ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

두 값 모두 Item ID 확정 뒤 presentation에만 사용합니다.

## Game Content 안전 업데이트

Game Content는 User Progress와 분리합니다.

```text
remote source
→ download / parse
→ canonical build
→ integrity/completeness validation
→ activate
```

- failed candidate가 last-known-good content를 덮어쓰지 않습니다.
- normal snapshot shrink guard는 기존 healthy baseline의 50%입니다.
- collection schema drift는 fail closed합니다.
- Wiki Ballistics enrichment는 fail-soft입니다.
- update failure가 `user.db`를 변경하지 않습니다.

## Map / MiniMap donor

Map/MiniMap은 다음 public donor revision을 pinned source로 사용합니다.

```text
SIGDrone/Tarkov-Helper
d933792b6042a51cea38dc44b686a096fe30de67
```

기존 `Propeex/Tarkov-Helper` 전체 구현을 준현 헬퍼의 제품 요구사항으로 간주하지 않습니다. Map/MiniMap의 검증된 donor source만 제한적으로 사용하며 JunhyunHelper 제품 요구사항은 first-party bridge/customization 경계에서 적용합니다.

## 개발 / 유지보수 원칙

새 작업은 저장소의 현재 공식 문서와 GitHub 상태를 먼저 확인한 뒤 시작합니다.

```text
실사용 오류 / Tarkov 변화 / reviewed Scanner evidence
→ root cause와 영향 범위 확인
→ 최소한의 일관된 수정
→ deterministic regression
→ full Windows release gate
→ 필요한 경우 PATCH release
```

Published stable release는 공개 후 교체하지 않습니다. 같은 version에서 documentation-only main commit이 다른 ProductVersion metadata/bytes를 만들더라도 이미 공개된 ZIP/tag/source를 덮어쓰지 않습니다.

현재 v1.7.14 릴리즈 배치에 남은 제품 개발 작업은 없습니다. 기본 운영 모드는 유지보수입니다.
