# 준현 헬퍼 v0.1.1

Release date: **2026-08-15**

Status: **RELEASED / PUBLIC GITHUB RELEASE VERIFIED**

## 공개 다운로드

https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.1

```text
Junhyun-Helper-v0.1.1-win-x64.zip
SHA-256: 91394101c5011b833c2810d8857fe2e9fd59b9f42f8710b90a899fe8169f0b54
```

Release는 draft/prerelease가 아닌 정식 공개 상태이며 Windows ZIP과 `SHA256SUMS.txt` 두 asset만 게시합니다.

## 기존 v0.1.0 사용자

v0.1.1 설치 후 프로그램 상단 **`데이터 업데이트`를 한 번 실행**합니다. 최신 online source에서 Content schema v5를 재구축합니다. `user.db`는 별도 저장소이므로 Profile, Quest 완료 기록, Inventory, Hideout 진행은 유지됩니다.

## Quest 정확도 수정

- current live `taskRequirements` (`active / complete / failed`) 재검증
- Lightkeeper / BTR Driver / Ref 상인 접근 이후 후속 Quest가 너무 일찍 열릴 수 있던 prerequisite 공백 보강
- opaque `globalVariable` / `dialogue` availability는 임의 추정하지 않고 `판정 문제`에 Indeterminate 진단 유지
- timed Quest의 `availableDelaySecondsMin/Max` canonical 보존
- 실제 인게임 완료 시각을 모르는 상태에서 가짜 delay countdown을 생성하지 않음
- Content snapshot schema v5

## live source 감사

2026-08-15 실제 online source를 product importer/validator로 전부 통과시켰습니다.

| GameMode | Quest | Item | Trader | Map | Hideout | Ammo |
|---|---:|---:|---:|---:|---:|---:|
| regular | 517 | 5312 | 16 | 17 | 26 | 200 |
| pve | 513 | 5312 | 16 | 17 | 26 | 200 |
| pvp-season | 490 | 5312 | 16 | 17 | 26 | 200 |

- validation errors: 0
- importer warnings: 0
- prerequisite missing/self/duplicate references: 0

상세 감사: `docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md`

## Release 검증

```text
Quest correctness merge: 0cfbb5108f5e6992d0f87d0e890fa635d30688a0
release baseline: fc9d098c1312e837a205b5d08ba44ba6d516e779
release workflow run: 31821096591
Desktop Release build: SUCCESS
Automated tests: 173 passed / 0 failed
Windows x64 single-file publish: SUCCESS
EXE ProductVersion: 0.1.1
Map + MiniMap startup smoke: SUCCESS
Normal Main Window close/process exit: SUCCESS
Release root DLL / PDB / nested ZIP: 0 / 0 / 0
Runtime Logs beside EXE: 0
GitHub Release final/public: VERIFIED
Published checksum re-download: VERIFIED
```

배포 루트:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```
