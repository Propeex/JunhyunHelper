# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 상태

**v0.1.1 RELEASED — `준현 헬퍼` Windows x64 single-file portable**

릴리즈일: **2026-08-15**

```text
Quest correctness: PR #75 MERGED
version metadata: PR #76 MERGED
release code baseline: fc9d098c1312e837a205b5d08ba44ba6d516e779
release workflow run: 31821096591 — SUCCESS
public asset: Junhyun-Helper-v0.1.1-win-x64.zip
public SHA-256: 91394101c5011b833c2810d8857fe2e9fd59b9f42f8710b90a899fe8169f0b54
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.1
```

현재 확인된 기능/패키징 blocker는 없습니다.

## v0.1.1 Quest 정확도 수정

- current `taskRequirements`의 `active / complete / failed` 상태 모델을 최신 live source로 재검증
- prerequisite missing/self/duplicate reference: 0
- Lightkeeper / BTR Driver / Ref 상인 접근 이후 후속 Quest가 너무 일찍 열릴 수 있던 공백 보강
- `globalVariable` / `dialogue`처럼 자동 확정할 수 없는 조건을 추측하지 않고 `판정 문제`에 원래 Indeterminate 진단 보존
- 각 GameMode 13개의 `availableDelaySecondsMin/Max`를 canonical metadata에 보존
- 실제 게임 완료 시각을 알 수 없으므로 UI 완료 클릭 시각 기반 가짜 countdown은 만들지 않음
- Content snapshot schema **v5**
- v3/v4는 offline last-known-good read 유지
- `user.db` schema/progress 변경 없음

상세: `docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md`

## 최신 데이터 전체 검증

```text
regular:    517 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pve:        513 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pvp-season: 490 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
validation errors: 0
importer warnings: 0
```

v0.1.1 Release gate:

```text
Desktop Release build: SUCCESS
automated tests: 173 passed / 0 failed
Windows x64 self-contained single-file publish: SUCCESS
EXE ProductVersion: 0.1.1
real Map + MiniMap startup smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
root DLL / PDB / nested ZIP: 0 / 0 / 0
runtime Logs beside EXE: 0
GitHub Release draft/prerelease: false / false
public assets: exactly 2
published checksum re-download: VERIFIED
```

## 사용자 업그레이드 정책

**v0.1.0 사용자는 v0.1.1로 교체한 뒤 상단 `데이터 업데이트`를 한 번 실행합니다.** 그러면 현재 online source에서 v5 Game Content를 새로 만들고 최신 Quest availability semantics를 적용합니다.

`%LocalAppData%/JunhyunHelper/user.db`의 Profile / Quest 완료 / Inventory / Hideout 진행은 그대로 유지됩니다.

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / v0.1.1 current live prerequisite audit 반영 |
| Hideout | 구현 완료 / current live validation 통과 |
| Needed Items / Inventory | 구현 완료 |
| Ammo | 구현 완료 / current live validation 통과 |
| Map + MiniMap | 구현 완료 / Windows 실사용 및 release smoke 검증 |
| Scanner | `준비 중` placeholder 탭 유지 / 실제 기능 PRODUCT OPEN |

## 핵심 데이터 원칙

```text
online source
→ download
→ external shape/semantic validation
→ canonical transform
→ candidate DB
→ relationship/read-back validation
→ active swap
→ User Progress와 결합
```

실패 candidate는 last-known-good active content를 덮지 않으며 Game Content update는 `user.db`를 삭제/덮어쓰지 않습니다. Runtime GPT/AI 의존성은 없습니다.

## Map 기준

Map subsystem은 독립이고 Quest만 JunhyunHelper current profile/content와 연결합니다. pinned submodule revision은 `d933792b6042a51cea38dc44b686a096fe30de67`입니다.

## 현재 공개 릴리즈

```text
v0.1.1
https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.1
asset: Junhyun-Helper-v0.1.1-win-x64.zip
SHA-256: 91394101c5011b833c2810d8857fe2e9fd59b9f42f8710b90a899fe8169f0b54
```

## 비차단 후속 범위

- Scanner 실제 기능 설계/구현
- Map artwork/config/general-marker atomic bundle updater
- code signing / installer / application updater
- user.db backup/restore UX
- repository license / third-party notice 정책
