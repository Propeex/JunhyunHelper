# RELEASE 1.0.0 — 정식 안정판

상태: **`PUBLIC VERIFIED`**

날짜: 2026-08-19

## 1. 목적

v0.1.14까지 구현·검증된 준현 헬퍼를 새로운 기능 추가 없이 정리하고, 내부 하드닝·개발자 문서·배포 검증을 완료하여 첫 정식 안정판 `v1.0.0`으로 승격했습니다.

## 2. 최종 릴리즈 값

```text
Tag: v1.0.0
Name: 준현 헬퍼 v1.0.0
Exact release source SHA: 3147ad1b48c3d30df529d95b148c5c444a77d649
Release workflow run ID: 32219746319
Release workflow head SHA: 312ef59a0f50bf3df43c9ebbc79e8a965d35d688
Automated tests: 232 passed / 0 failed / 0 skipped
Asset: Junhyun-Helper-v1.0.0-win-x64.zip
Asset bytes: 74,088,334
SHA-256: 0e92787409add9dd9e1138277c3588586a04266b05ca56d7cf7fb6f79c88094c
Checksum asset: SHA256SUMS.txt
Draft: false
Prerelease: false
Latest stable: true
Public-downloaded executable smoke: passed
Removed v0.x GitHub Releases: 15
Remaining v0.x GitHub Releases: 0
```

공개된 `SHA256SUMS.txt`의 ZIP entry와 실제 public download ZIP의 SHA-256이 일치함을 별도 모니터에서 다시 확인했습니다.

## 3. 제품 범위

v1.0.0은 v0.1.14의 사용자 기능을 그대로 유지합니다.

- Profile
- Quest
- Hideout
- Items / Inventory / Needed Items / Cleanup
- Ammo
- Map / MiniMap
- Game Content update
- 사용자 동의형 Program update
- Scanner `준비 중` placeholder

Scanner 실제 기능은 v1.0.0에 추가하지 않았습니다.

## 4. 내부 하드닝

### 일반 first-party code

- 현재 제품 규칙에서 사용되지 않는 과거 Hideout cleanup compatibility API 제거
- `user.db` schema initialization을 store instance당 한 번으로 제한
- shared online-data HTTP User-Agent를 assembly version에서 파생해 버전 drift 제거
- Desktop version을 1.0.0으로 승격

### CI / packaging

- project Version ↔ published ProductVersion ↔ FIRST_RUN identity 검증
- exact FIRST_RUN first line 검증
- release tree의 PDB / nested archive / unexpected root / legacy dependency 차단
- Windows x64 self-contained single-file root contract 유지

### Map donor reproducibility

- Map source gitlink는 `d933792b6042a51cea38dc44b686a096fe30de67`로 유지
- 과거 작업 fork가 clean checkout에서 재현되지 않아, 동일 exact Git object가 존재하는 공개 upstream `SIGDrone/Tarkov-Helper`로 fetch origin만 변경

### Map runtime compatibility

첫 exact-release attempt는 public Release 생성 전에 Factory 타층 standard marker의 late-state 회귀를 검출하고 중단됐습니다.

원인은 donor의 legacy current-floor-only filter가 200ms 간격으로 최대 12회 실행되며 JunhyunHelper의 floor presentation을 뒤늦게 덮어쓸 수 있는 race였습니다.

수정:

- donor pin 변경 없음
- smoke 기준 완화 없음
- donor가 `_sharedFloorHiddenMarkers`에 직접 기록한 floor-only suppression 요소만 복구
- category/faction visibility는 donor가 계속 소유
- 복구 후 JunhyunHelper floor presentation 재적용
- page reload 시 donor timer callback 재부착
- permanent polling 추가 없음

상세: `MAP_RUNTIME_COMPATIBILITY.md`, `FINAL_AUDIT_1.0.0.md`

## 5. 데이터 호환성

```text
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v0.1.14 → v1.0.0 mandatory Game Content update: none
v0.1.14 → v1.0.0 user data migration: none
```

기존 `%LocalAppData%/JunhyunHelper`의 profile, quest progress, inventory, hideout progress, Map settings, Ammo favorites는 유지됩니다.

## 6. 배포 형태

- Windows x64
- .NET 10 self-contained
- portable
- single-file executable
- installer 없음
- 관리자 권한 불필요
- code signing 없음

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

정식 asset:

```text
Junhyun-Helper-v1.0.0-win-x64.zip
SHA256SUMS.txt
```

## 7. 실제 정식 릴리즈 gate 결과

최종 릴리즈는 다음을 전부 통과했습니다.

1. exact release source checkout
2. exact Map donor pin 확인
3. Release build
4. 232 automated tests
5. win-x64 self-contained single-file publish
6. csproj Version = 1.0.0
7. published ProductVersion의 1.0.0 identity 검증
8. FIRST_RUN = v1.0.0 exact identity
9. package root/dependency/PDB/nested archive audit
10. 실제 published EXE Product UI smoke
11. Main Map smoke
12. Factory floor/late-state smoke
13. MiniMap smoke
14. 정상 MainWindow close 및 process 종료
15. ZIP + SHA256SUMS 생성
16. Draft Release 생성
17. Draft assets 재다운로드 SHA-256/package identity 검증
18. public/latest 전환
19. public assets 재다운로드 SHA-256/ProductVersion/package identity 검증
20. public-downloaded EXE Product UI + Main Map + Factory + MiniMap + graceful shutdown smoke
21. 기존 `v0.*` GitHub Releases 15개 전부 제거
22. v0.x 잔여 Release 0개 확인
23. latest stable가 v1.0.0인지 재확인
24. 일회성 release/monitor workflow 정리

## 8. 첫 release attempt가 중단된 이유

첫 release-only attempt는 public Release를 만들기 전에 Map late-suppression race를 검출했습니다.

그 attempt에서:

- Release build PASS
- 232 tests PASS
- package audit PASS
- actual EXE Factory late-state smoke FAIL
- v1.0.0 public Release 생성 전 중단
- 기존 v0.x public Releases 유지

원인을 수정한 뒤 새 exact source baseline으로 release gate를 처음부터 다시 수행했습니다. 이 기록은 최종 릴리즈가 단순히 기존 CI 결과를 재사용한 것이 아니라 독립적인 release-package runtime 검증을 실제로 수행했다는 근거입니다.

## 9. 이후 버전 규칙

공식 규칙은 `VERSIONING.md`를 따릅니다.

- 새 기능 추가 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/버그 수정/성능·안정성 개선 → PATCH +1

예:

```text
1.0.0 + Scanner 실제 기능 → 1.1.0
1.0.0 + Quest 수정 → 1.0.1
1.0.1 + Scanner 실제 기능 → 1.1.0
```

MAJOR 증가 조건은 필요할 때 사용자와 별도로 확정합니다.
