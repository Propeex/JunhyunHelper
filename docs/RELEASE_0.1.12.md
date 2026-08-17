# RELEASE 0.1.12 — 공개 검증 기록

## 목적

v0.1.11 공개본에서 사용자 실제 화면 기준으로 여전히 남아 있던 Items / Ammo / Map UI 정렬·손잡이 문제를 수정하고, 이후 같은 유형의 회귀를 소스 검사나 빌드 성공만으로 통과시키지 않도록 **실제 WPF 렌더링 좌표 검증을 릴리즈 게이트에 추가**한 교정 릴리즈입니다.

## 공개 릴리즈

```text
tag: v0.1.12
name: 준현 헬퍼 v0.1.12
release baseline: cfacee6cfa893932d74d6a71725b6c711282981e
ProductVersion: 0.1.12+cfacee6cfa893932d74d6a71725b6c711282981e
release candidate PR: #95
release candidate PR CI: 32025523609 — SUCCESS
release baseline main CI: 32025837427 — SUCCESS
release workflow: 32026123215 — SUCCESS
automated tests: 210 passed / 0 failed / 0 skipped
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.12
release id: 371714349
public asset: Junhyun-Helper-v0.1.12-win-x64.zip
asset id: 518040422
asset size: 74,067,018 bytes
SHA-256: bc91f17f94c6554d09da3fed6db6ebb679c6e1d57ff7017d4a624e8dcd8eae89
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v0.1.11 → v0.1.12 mandatory data update: none
```

Release는 `draft=false`, `prerelease=false`이고 target commit은 정확히 `cfacee6cfa893932d74d6a71725b6c711282981e`입니다.

## 사용자 피드백 수정

### Items / flexible hand-in

근본 원인은 앱 전역 `Button` ControlTemplate의 `ContentPresenter`가 가운데 정렬을 강제해, 후보 행에서 `HorizontalContentAlignment=Stretch`를 지정해도 내부 Grid가 실제 행 전체 폭을 사용하지 못하던 것이었습니다.

v0.1.12에서는 `FlexibleCandidateTemplate`에 전용 stretch Button template을 적용합니다.

렌더 계약:

```text
52px icon | * name/category | 108px in-raid | 96px normal
```

- 후보 Grid가 행 전체 폭을 실제로 사용
- 아이콘/이름 영역은 좌측 축 유지
- in-raid / normal 수량은 우측 기준으로 정렬
- 68px 행과 44px 아이콘 프레임 유지

### Ammo

- `UpdateFavoriteButton` 자체가 `☆` / `★`만 설정하도록 수정
- Loaded 이후 필터/데이터 갱신에서도 `☆ 즐겨찾기` 문자열이 다시 나타나지 않음
- 하단 상세정보 손잡이는 42px 화살표 전용 버튼
- 펼쳐짐: `▼`
- 접힘: `▲`
- 상세정보 host visibility와 화살표 상태를 함께 검증

### Map current Quest sidebar

- Quest 제목을 전역 Button template의 Content 영역에서 분리
- 투명 Button은 클릭 hit surface로만 사용하고 제목은 고정 text lane에 직접 렌더
- `30px checkbox | 34px A·B·C·D marker | * Quest text` 구조 유지
- marker/check 상태가 달라도 Quest 제목의 실제 시작 X축이 동일
- 펼쳐진 sidebar의 손잡이는 패널 오른쪽 바깥 경계, 즉 지도와 패널 사이에 위치

## 렌더링 검증 게이트

`MainWindow.ProductUiLayoutSmoke`를 실제 publish된 Windows 실행 파일의 기존 Map smoke 경로에 연결했습니다.

릴리즈 성공 전에 실제 WPF `Measure` / `Arrange` 결과를 검사합니다.

- 900px probe에서 flexible candidate Grid가 820px 이상 실제 확장
- icon/name 및 FIR/general의 실제 좌·우 렌더 축 검증
- Ammo favorite Content가 단일 `☆` 또는 `★`
- Ammo detail handle 실제 상태가 expanded=`▼`, collapsed=`▲`
- 조건이 다른 Map Quest 3개의 title 시작 X 편차 `<= 0.75px`
- 펼친 Map Quest sidebar handle의 오른쪽 여백 `<= 6px`

이 검증은 소스 문자열 검사가 아니라 publish된 앱을 실행해서 수행합니다.

개발 중 첫 rendered UI smoke에서는 우측 정렬 요소의 왼쪽 좌표를 비교하는 검증식 자체가 잘못되어 실제로 실패했습니다. 실제 후보 행은 이미 약 870px까지 정상 확장된 상태였고, 검증 기준을 우측 요소의 오른쪽 끝 좌표로 바로잡은 뒤 다시 통과했습니다. 즉 v0.1.12의 rendered UI gate는 실제 실패→원인 확인→교정→통과 과정을 거쳤습니다.

## 최종 릴리즈 검증

릴리즈 workflow는 exact baseline `cfacee6cfa893932d74d6a71725b6c711282981e`를 직접 checkout했습니다.

성공 게이트:

- Release build — SUCCESS
- automated tests — `210 / 210` SUCCESS
- Windows x64 self-contained single-file publish — SUCCESS
- ProductVersion exact prefix `0.1.12` — SUCCESS
- FIRST_RUN v0.1.12 / Content schema v7 / user.db v1 / upgrade compatibility 검사 — SUCCESS
- package root hygiene / no root DLL / no PDB / no nested ZIP / no legacy forbidden dependency — SUCCESS
- 실제 publish EXE startup — SUCCESS
- rendered Product UI assertions — SUCCESS
- Main Map / Factory / MiniMap smoke — SUCCESS
- 정상 Main Window close / process exit — SUCCESS

릴리즈 로그의 명시적 성공 신호:

```text
PUBLISHED_RENDERED_UI_MAP_SMOKE=true
PUBLIC_RELEASE_VERIFIED=true
PUBLIC_SIZE=74067018
PUBLIC_SHA256=bc91f17f94c6554d09da3fed6db6ebb679c6e1d57ff7017d4a624e8dcd8eae89
```

## 공개 ZIP 무결성

GitHub Release를 만든 뒤 공개 asset을 다시 다운로드하여 로컬 패키징 결과와 비교했습니다.

```text
size: 74,067,018 bytes
SHA-256: bc91f17f94c6554d09da3fed6db6ebb679c6e1d57ff7017d4a624e8dcd8eae89
GitHub metadata digest: sha256:bc91f17f94c6554d09da3fed6db6ebb679c6e1d57ff7017d4a624e8dcd8eae89
```

`SHA256SUMS.txt`도 같은 Release에 포함합니다.

## 데이터 / 진행도 호환성

- Content schema v7 유지
- v3~v7 기존 snapshot 읽기 유지
- user.db SQLite schema v1 유지
- v0.1.11 → v0.1.12 필수 데이터 업데이트 없음
- 기존 Profile / Quest / Inventory / Hideout / Map 설정 유지
- Quest availability / Needed Items의 기존 보수적 판정 정책 변경 없음

## 정리

릴리즈 전용 `.github/workflows/release-v0.1.12.yml`은 공개 릴리즈와 재다운로드 검증 완료 뒤 제거했습니다. 상시 workflow는 `.github/workflows/ci.yml`만 유지합니다.
