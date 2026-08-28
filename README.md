# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.10.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태입니다. 새로운 실제 회귀·Tarkov 호환성 변화·사용자가 명시적으로 확정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 Scanner 인식 기준 조정을 시작하지 않습니다.

공식 현재 상태 문서:

- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/PRODUCT.md`
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/MAINTENANCE_CONTRACTS.md`
- `docs/DECISIONS.md`

## 현재 공개 릴리즈

```text
version: v1.10.0
Desktop target version: 1.10.0
exact product release source/tag target: a99540c4ae450f9f1995e5378919ae57f41ba930
main CI: 33201929209 — SUCCESS
Release workflow: 33202187186 — SUCCESS
release id: 378705187
stable asset: Junhyun-Helper.zip
asset id: 534229631
bytes: 80,543,064
SHA-256: 65dd990e3c8b1c6faa7122ab1d809fae260c88cd10022eb7399ca6a2a3717639
439 passed / 0 failed / 0 skipped
```

GitHub `/releases/latest` 및 `refs/tags/v1.10.0` readback에서 v1.10.0이 `draft=false`, `prerelease=false`, latest stable이며 release target과 tag ref가 exact product release source와 일치함을 확인했습니다. 공개 ZIP의 byte size와 digest도 exact-main CI package와 일치합니다.

공식 릴리즈 기록:

- `docs/RELEASE_1.10.0.md`
- `docs/RELEASE_NOTES_V1.10.0.md`
- `docs/.release-v1.10.0-status.json`
- `docs/DECISION_V1.10.0_MINIMAP_REOPEN_MINISCANNER_FLEA_MINIMUM.md`

이 README와 이후 documentation-only commit은 v1.10.0 제품 릴리즈 소스가 아닙니다. v1.10.0 product source/tag/assets는 위 `a99540c4...` 기준의 immutable historical release입니다.

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
- Quest / Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth / diagnostics / regression dataset
- Scanner 아이템 정보 DB
- Scanner Favorites / Recents
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## v1.10.0 — MiniMap 재표시 동기화 / Mini Scanner 플리마켓 최저가

- Main Map을 A에서 B로 바꾼 직후 MiniMap을 처음 열거나, 이미 로드되어 숨겨져 있던 같은 MiniMap 창을 다시 표시해도 첫 visible frame부터 B를 사용하도록 수정했습니다.
- v1.9.1에서 놓친 donor `Hide()` → same loaded Window `Show()` 재사용 경로를 별도 동기화 경계로 보강했습니다.
- exact-main published EXE smoke는 실제 MiniMap에서 A SVG를 렌더한 뒤 hide → Main Map B 선택 → same Window show → 실제 `MapSvg.Source`가 B로 바뀌는 것까지 검증합니다.
- Mini Scanner에 `플리마켓 최저가` 표시 항목을 추가했습니다.
- 다른 Mini Scanner 정보와 동일하게 설정에서 표시/숨김과 순서 변경을 지원하고 재실행 후 유지합니다.
- 기존 사용자 설정은 기존 행의 상대 순서를 유지하고 새 항목을 정확히 한 번 추가합니다.
- 플리 최저가는 Scanner catalog의 `lastLowPrice`를 Item ID 확정 뒤 presentation-only 데이터로 사용합니다. Scanner 인식 기준과 scan-time network I/O는 변경하지 않았습니다.

Exact-main runtime evidence:

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
reused-minimap-show-boundary=ok
rendered-minimap-map-sync=ok
```

같은 exact-main 실행에서 439개 테스트, Product UI, Ammo, Map/Factory/MiniMap, Scanner detail/Favorites/Recents, 정상 종료와 portable root가 모두 성공했습니다.

Scanner OCR threshold/matcher/candidate cap/visual recovery acceptance, Game Content LKG/completeness/fail-closed, Factory floor/Map marker 의미는 변경하지 않았습니다.

## 유지보수

실사용 오류나 Tarkov 변화가 발생하면 실제 source/log/runtime state를 확인해 최소 수정하고 deterministic regression → published EXE smoke → exact-main release gate 순으로 검증합니다. 사용자-visible WPF lifecycle 변경은 source assertion만으로 성공을 선언하지 않습니다.
