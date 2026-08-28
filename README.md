# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.9.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

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
version: v1.9.1
Desktop target version: 1.9.1
exact product release source/tag target: 723760910ff250a515ed8db456d3f045656ecacb
main CI: 33184811972 — SUCCESS
Release workflow: 33185056113 — SUCCESS
release id: 378579142
stable asset: Junhyun-Helper.zip
asset id: 533982952
bytes: 80,540,488
SHA-256: 7a282f58d6cf2e4916c55daddf828a70643b35669bc71fbeaca1e7a4e8176f54
435 passed / 0 failed / 0 skipped
```

GitHub `/releases/latest` 및 `refs/tags/v1.9.1` readback에서 v1.9.1이 `draft=false`, `prerelease=false`, latest stable이며 release target과 tag ref가 exact product release source와 일치함을 확인했습니다. 공개 ZIP의 byte size와 digest도 exact-main CI package와 일치합니다.

공식 릴리즈 기록:

- `docs/RELEASE_1.9.1.md`
- `docs/RELEASE_NOTES_V1.9.1.md`
- `docs/.release-v1.9.1-status.json`
- `docs/DECISION_V1.9.1_FINAL_UI_MINIMAP.md`
- `docs/RELEASE_1.9.0.md` — 이전 Scanner Favorites / Recents 릴리즈

이 README와 이후 documentation-only commit은 v1.9.1 제품 릴리즈 소스가 아닙니다. v1.9.1 product source/tag/assets는 위 `723760910...` 기준의 immutable historical release입니다.

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

## v1.9.1 — 최종 UI / MiniMap 동기화 수정

- Scanner 상세의 즐겨찾기 별 버튼과 Wiki 버튼을 34px로 맞추고 별 글리프를 중앙 정렬했습니다.
- 지도 `탈출구` 그룹은 실제 donor PMC / Scav / Transit 체크박스 세 개만 표시합니다. visible master/중복 행은 제거하고 hidden master render gate와 실제 handler/persistence 의미는 유지합니다.
- Main Map에서 현재 선택한 지도를 MiniMap 초기화 전에 shared tracker에 동기화해 저장된 이전 지도 대신 현재 visible selection으로 MiniMap이 열립니다. 이미 열린 MiniMap도 이후 변경을 즉시 반영합니다.

Exact-main published EXE evidence:

```text
Scanner actions:
favorite-wiki-height=34
favorite-symbol-font=ok
favorite-content-centered=ok
wiki-content-centered=ok

Map:
real-donor-checkboxes=ok
hidden-master-render-gate=ok
approved-three-filter-layout=ok
minimap-refresh-handler-preserved=ok
pmc-filter-render-state=ok
scav-filter-render-state=ok
transit-filter-render-state=ok

MiniMap sync:
main-map-selection-boundary=ok
active-minimap-map-sync=ok
```

같은 실행에서 435개 테스트, Product UI, Ammo, Scanner detail/Favorites/Recents, Main Map, Factory, MiniMap, graceful shutdown, clean portable root가 모두 성공했습니다.

Scanner OCR threshold/matcher/candidate cap/visual recovery acceptance, Game Content LKG/completeness/fail-closed, Factory floor/Map marker 의미는 변경하지 않았습니다.

## 유지보수

실사용 오류나 Tarkov 변화가 발생하면 실제 source/log/runtime state를 확인해 최소 수정하고 deterministic regression → published EXE smoke → exact-main release gate 순으로 검증합니다. 사용자-visible WPF lifecycle 변경은 source assertion만으로 성공을 선언하지 않습니다.
