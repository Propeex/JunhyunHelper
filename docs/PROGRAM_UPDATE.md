# PROGRAM UPDATE — 제품 업데이트 계약

상태: **EVERGREEN CURRENT PROGRAM UPDATE CONTRACT**

## 1. 확정 요구사항

준현 헬퍼는 일반 실행 시 최신 **정식** 프로그램 버전을 조회한다.

1. 현재 버전보다 최신 정식 버전이 있으면 사용자에게 업데이트 동의 여부를 묻는다.
2. 사용자가 동의하면 공개 package/checksum을 검증한다.
3. 검증이 모두 성공한 뒤 program-owned files만 교체한다.
4. 완료 후 새 버전으로 자동 재시작한다.
5. 어떤 업데이트 실패도 User Progress나 기존 정상 프로그램을 선제적으로 파괴해서는 안 된다.

세부 구현과 실패 처리 정책은 JunhyunHelper가 소유한다. old `Tarkov-Helper` updater는 제품 권위가 아니다.

## 2. 최신 버전 판정

Source of truth:

```text
https://api.github.com/repos/Propeex/JunhyunHelper/releases/latest
```

계약:

- `draft=false`, `prerelease=false` stable release만 대상
- tag 형식은 exact `vMAJOR.MINOR.PATCH`
- 현재 실행 버전보다 strictly newer인 release만 업데이트 대상으로 취급
- current stable package name은 **`Junhyun-Helper.zip`**
- checksum asset은 **`SHA256SUMS.txt`**
- updater는 canonical **`Junhyun-Helper.zip`**만 설치 package로 인정한다.
- 과거 전환기 versioned package 이름은 current updater contract가 아니다.
- required release/package shape가 없거나 불명확하면 자동 추측하지 않고 업데이트를 중단한다.
- asset URL은 `github.com/Propeex/JunhyunHelper/releases/download/...` HTTPS release URL만 허용한다.

현재 구현 권위:

- `Infrastructure/Updates/GitHubProgramUpdateClient.cs`
- `Infrastructure/Updates/ProgramUpdateApplier.cs`

## 3. 실행 시 동작

업데이트 조회는 일반 startup flow에서 실행한다.

- latest가 current보다 새롭지 않으면 UI 없음
- GitHub/네트워크 조회 실패 → 진단 로그 + 현재 프로그램 정상 사용
- 새 stable이 있으면 현재 실행에서 사용자 동의 UI 표시
- 사용자 거절 → 현재 실행 유지; 다음 실행에서 다시 확인 가능

Program Update check는 Game Content update와 별도 lifecycle이다.

## 4. 동의 후 준비 단계

사용자가 동의한 뒤에도 기존 프로그램 파일을 즉시 수정하지 않는다.

```text
latest stable metadata
→ exact package + SHA256SUMS.txt
→ checksum entry selection
→ package download
→ SHA-256 verification
→ archive safety validation
→ staging validation
→ updater handoff
```

Staging root:

```text
%LocalAppData%/JunhyunHelper/updates/pending/<version-guid>/
```

Archive 안전 검증:

- absolute/path traversal 거부
- `.` / `..` path segment 거부
- duplicate relative entry 거부
- symbolic link 거부
- `.pdb` 거부
- 예상 밖 product root 거부
- zero/invalid required file 거부

## 5. Stable ZIP / extracted package contract

Canonical public asset:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

Updater는 stable wrapper folder `준현 헬퍼/`를 검증한 뒤 staging에서 program-owned root를 다음 형태로 정규화한다.

```text
staging/
├─ 준현 헬퍼.exe
├─ FIRST_RUN_KO.txt
└─ Assets/
```

Required staging facts:

- non-empty `준현 헬퍼.exe`
- non-empty `FIRST_RUN_KO.txt`
- non-empty `Assets/` tree

ZIP/folder 이름은 semantic version identity가 아니다. Version identity는 project version, EXE ProductVersion, FIRST_RUN, tag/release metadata에서 검증한다.

## 6. 파일 교체와 재시작

실행 중 EXE가 자기 파일을 직접 덮어쓰지 않는다.

- 현재 `준현 헬퍼.exe`를 temporary updater runner로 복사
- updater mode 실행
- 원래 MainWindow/process 정상 종료 대기
- 새 files를 target volume에 준비
- 기존 program-owned files를 temporary previous 위치로 이동
- program-owned files만 transaction 교체
  - `준현 헬퍼.exe`
  - `FIRST_RUN_KO.txt`
  - `Assets/`
- 교체 실패 시 previous files rollback 시도
- 성공 시 새 `준현 헬퍼.exe` 실행
- 실패 후 정상 previous EXE가 있으면 기존 버전 재실행 시도

상시 `Updater.exe`는 공개 ZIP에 포함하지 않는다.

## 7. 사용자 데이터 경계

Program Update는 `%LocalAppData%/JunhyunHelper`의 사용자/게임 상태를 교체하지 않는다.

보존 대상 예:

```text
user.db
content/
image-cache/
map-product-settings.json(.bak)
minimap-window-state.json
ammo-settings / favorites persistence
scanner-settings.json(.bak)
scanner/catalog/
scanner/fonts/
scanner/diagnostics/
Scanner reviewed Ground Truth
logs/
```

`updates/pending/`만 Program Update staging lifecycle에 사용한다.

Program Update와 Game Content update는 서로 다른 subsystem이다.

## 8. 실패 정책

- update check 실패 → 앱 정상 사용
- 사용자 거절 → 앱 정상 사용
- release metadata invalid → update 중단, 현재 프로그램 유지
- package/checksum download 실패 → 현재 프로그램 미변경
- checksum mismatch → 현재 프로그램 미변경
- archive/staging validation 실패 → 현재 프로그램 미변경
- updater runner 시작 실패 → 현재 프로그램 미변경
- 파일 교체 실패 → transaction rollback 시도
- 실패는 `%LocalAppData%/JunhyunHelper/logs/startup.log`에 진단 가능

업데이트 실패를 일반 WPF fatal로 확대하지 않는다.

## 9. Stable release 공급 계약

신규 정식 release는 다음을 유지한다.

- semantic stable tag `vMAJOR.MINOR.PATCH`
- canonical `Junhyun-Helper.zip`
- `SHA256SUMS.txt`
- inner top-level product folder `준현 헬퍼/`
- required EXE / FIRST_RUN / Assets
- exact main-CI artifact verification
- public release target/tag/source readback
- public ZIP digest가 verified main-CI package SHA-256과 일치

이미 공개된 stable version은 immutable historical release로 취급한다. 이후 documentation-only main commit 때문에 동일 tag asset을 교체하지 않는다.

현재 exact product source와 public proof는 이 문서에 복제하지 않는다. `docs/PROJECT_STATE.json`, `docs/CURRENT_STATE.md`, `docs/STATE.md`의 current release identity를 사용한다.

## 10. 검증 요구

자동/릴리즈 regression은 최소 다음을 보호한다.

- stable semantic version parsing
- canonical stable package requirement
- exact checksum selection
- approved GitHub release asset URL
- ZIP traversal/symlink/duplicate/PDB 거부
- stable product wrapper root 강제
- staging required content
- owned-file transaction replacement
- unrelated target/user file 보존
- Windows x64 publish/ProductVersion/FIRST_RUN identity
- actual published EXE Product UI/Scanner/Map smoke
- graceful shutdown / clean portable root
- exact release package SHA-256 verification
