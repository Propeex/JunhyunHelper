# RELEASE 0.1.13 — 유지보수 안정화 릴리즈

## 목적

v0.1.12의 기능 범위와 사용자 경험을 유지하면서, 전체 프로그램 감사에서 확인한 **부가 설정 persistence와 런타임 failure containment의 취약 경계**를 보강합니다.

이 릴리즈는 새 기능 릴리즈가 아닙니다.

- Quest / Hideout / Needed Items / Ammo / Map의 확정된 제품 의미 유지
- Content schema v7 유지
- user.db SQLite schema v1 유지
- 기존 사용자 진행도와 설정 유지
- Scanner는 실제 기능을 구현하지 않고 `준비 중` placeholder 탭 그대로 유지

## 포함 변경

### Map / Ammo 설정 저장 안정성

- 작은 JSON preference를 same-directory temporary file에서 원자적으로 교체
- 직전 정상 preference를 `.bak` 복구본으로 유지
- primary JSON이 손상되면 정상 backup으로 fallback
- 손상된 primary를 교체할 때 기존 정상 backup을 손상본으로 덮어쓰지 않음
- Map/Ammo presentation preference 저장 실패는 전역 WPF fatal로 확대하지 않고 진단 로그로 격리

### Map 입력 / 연속 저장

- Map slider 연속 입력은 250ms 단위로 묶어서 저장
- Map runtime dispose 시 pending slider 값을 flush
- Map product hotkey 비동기 실패를 dispatcher-level fatal로 확대하지 않고 기록
- NumPad 0~5 직접 층 선택 비동기 실패도 진단 로그로 격리
- keyboard hook 설치 실패를 `%LocalAppData%/JunhyunHelper/logs/startup.log`에 기록

### Game Content 최종 검증

다음 비정상 canonical 데이터를 active DB 적용 전에 fatal validation으로 차단합니다.

- Quest item requirement의 accepted item 후보가 비어 있음
- Quest item requirement `Count <= 0`
- Hideout item requirement `Count <= 0`

정상 데이터의 Quest/Hideout/Needed Items 계산 방식은 변경하지 않습니다.

### Scanner

현재 제품 계약과 문서를 일치시켰습니다.

- 상단 `스캐너` 탭 유지
- `준비 중` 상태 유지
- 실제 스캔/인식 동작 추가 없음
- 별도 요구사항 확정 전까지 임의 구현하지 않음

`DEC-045`가 과거 Scanner 탭 숨김 결정을 대체합니다.

## 데이터 / 업그레이드 호환성

```text
ProductVersion: 0.1.13
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v0.1.12 → v0.1.13 mandatory data update: none
```

v0.1.12 사용자는 기존 `%LocalAppData%/JunhyunHelper` 데이터를 그대로 사용합니다.

- Profile 유지
- Quest 완료/실패 유지
- Inventory 유지
- Hideout 진행 유지
- Map 제품 설정 유지
- Ammo 즐겨찾기 유지

새 `.bak` 파일은 기존 JSON preference의 복구 경로이며 기존 파일 형식과 호환됩니다.

## 사전 hardening 검증

PR #96의 최종 head에서 다음을 통과했습니다.

```text
hardening PR: #96
final hardening CI: 32104689932 — SUCCESS
automated tests: 217 passed / 0 failed / 0 skipped
Release build: SUCCESS
Windows x64 self-contained single-file publish: SUCCESS
Main Map / Factory / MiniMap published-app smoke: SUCCESS
graceful shutdown: SUCCESS
```

## v0.1.13 릴리즈 후보 검증

release candidate PR #97에서 버전/첫 실행 안내/릴리즈 기록만 추가한 상태로 기존 Windows 품질 게이트를 다시 통과했습니다.

```text
release candidate PR: #97
release candidate CI: 32105275116 — SUCCESS
automated tests: 217 passed / 0 failed / 0 skipped
Release build: SUCCESS
Windows x64 self-contained single-file publish: SUCCESS
published EXE Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown: SUCCESS
release baseline: f43190494ce91b3adf389e57a3a790fd45db8b20
```

## 최종 공개 릴리즈 검증

공개 Release 생성 뒤 별도의 one-shot 검증 PR #99가 **GitHub에 실제 공개된 asset을 다시 다운로드**하여 검증했습니다.

```text
release: v0.1.13
release baseline: f43190494ce91b3adf389e57a3a790fd45db8b20
public verification PR: #99
public verification workflow: 32111533861 — SUCCESS
public tag SHA: f43190494ce91b3adf389e57a3a790fd45db8b20
ProductVersion: 0.1.13+f43190494ce91b3adf389e57a3a790fd45db8b20
asset: Junhyun-Helper-v0.1.13-win-x64.zip
asset size: 74,069,173 bytes
SHA-256: 77a8e5d70bacfa8054fb3eafbe03a892456f17fc63c00776379e2730e55c4120
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.13
```

공개 검증은 다음을 모두 확인했습니다.

- Release가 `draft=false`, `prerelease=false`
- Release target commit과 public tag가 exact release baseline과 일치
- 공개 ZIP과 `SHA256SUMS.txt`가 모두 존재
- 공개 ZIP을 다시 다운로드해 SHA-256을 직접 재계산
- 재계산 SHA-256이 `SHA256SUMS.txt`와 정확히 일치
- 공개 ZIP root가 `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets/` 계약을 유지
- 공개 ZIP에 PDB가 없음
- 공개 EXE ProductVersion이 exact v0.1.13 baseline을 포함
- 공개 FIRST_RUN_KO.txt가 v0.1.13, 무필수 데이터 업데이트, Scanner `준비 중` 계약을 유지

따라서 v0.1.13 공개 asset은 검증된 release baseline의 최종 배포본으로 확정합니다.

## 알려진 비차단 범위

- EFT 1.0 Story Chapters는 ordinary `json.tarkov.dev/tasks` 기반 progression source 범위 밖
- PvE Skier LL2 PVE ZONE seed의 실제 counter semantics는 증명 전까지 fail-closed
- Map donor/bridge 유지보수 부채는 현재 동작 안정성을 위해 단순 cleanup 목적으로 건드리지 않음

이 항목들은 v0.1.13 maintenance release blocker가 아닙니다.
