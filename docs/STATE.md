# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-19

## 1. 제품 목적

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 프로그램입니다.

핵심 구조:

```text
온라인 Tarkov 데이터
→ 외부 형식/필수 의미 검증
→ canonical model 변환
→ candidate DB
→ 관계/read-back 검증
→ active Game Content 교체
→ User Progress와 결합
→ Quest / Hideout / Needed Items / Ammo / Map 표시
```

- runtime GPT/AI 의존성 없음
- Game Content update와 Program update는 별도 subsystem
- old Tarkov-Helper는 일반 제품 사양이 아님
- Map/MiniMap만 명시적으로 pinned donor source를 사용

구현 위치·입력·출력·참조 관계·변경 영향은 `docs/DEVELOPER_REFERENCE.md`를 사용합니다. Map donor runtime compatibility는 `docs/MAP_RUNTIME_COMPATIBILITY.md`를 함께 읽습니다.

---

## 2. 현재 정식 릴리즈

**v1.0.0 PUBLIC VERIFIED — Windows x64**

v1.0.0은 v0.1.14의 사용자-visible 기능을 유지하면서 내부 하드닝과 개발 문서화를 완료한 첫 정식 안정판입니다.

최종 값:

```text
Release tag: v1.0.0
Release name: 준현 헬퍼 v1.0.0
Exact release source: 3147ad1b48c3d30df529d95b148c5c444a77d649
Release workflow run: 32219746319
Release workflow head: 312ef59a0f50bf3df43c9ebbc79e8a965d35d688
Automated tests: 232 passed / 0 failed / 0 skipped
Asset: Junhyun-Helper-v1.0.0-win-x64.zip
Asset size: 74,088,334 bytes
SHA-256: 0e92787409add9dd9e1138277c3588586a04266b05ca56d7cf7fb6f79c88094c
Checksum asset: SHA256SUMS.txt
Draft: false
Prerelease: false
Latest stable: true
Public-downloaded executable smoke: passed
Removed v0.x GitHub Releases: 15
Remaining v0.x GitHub Releases: 0
```

### v1.0.0 공개 검증 순서

```text
exact source SHA 고정
→ donor exact pin 확인
→ Release build
→ 전체 자동 테스트
→ win-x64 self-contained single-file publish
→ package identity/root/dependency audit
→ actual published EXE Product UI + Main Map + Factory + MiniMap + graceful shutdown smoke
→ ZIP + SHA256SUMS 생성
→ Draft GitHub Release
→ Draft assets 재다운로드 + hash/package identity 검증
→ public/latest 전환
→ Public assets 재다운로드 + hash/ProductVersion/package 검증
→ public-downloaded EXE Product UI + Main Map + Factory + MiniMap + graceful shutdown smoke
→ 기존 v0.x Releases 15개 전부 삭제
→ latest stable가 v1.0.0인지 재확인
```

정식 release용 임시 workflow/monitor 파일은 검증 완료 후 저장소에서 제거했습니다. 상시 workflow는 `.github/workflows/ci.yml`만 유지하는 것이 원칙입니다.

---

## 3. v1.0.0에서 적용한 내부 하드닝

새 사용자 기능을 추가하지 않았고 기존 기능을 축소하지 않았습니다.

### Core / Items

- 현재 제품 규칙과 모순되는 과거 `UnenteredHideoutLevel` cleanup compatibility surface 제거
- Hideout progress 미입력 = Lv.0 규칙 유지
- 실제 Needed Items / Cleanup 계산 의미 변경 없음

### Persistence

- `UserProfileStore` schema initialization을 store instance당 한 번으로 제한
- concurrent first access는 `SemaphoreSlim` gate로 직렬화
- SQLite schema v1과 persisted payload 의미 변경 없음

### Network identity

- shared online-data HTTP User-Agent의 과거 `0.1` hardcode 제거
- Desktop assembly version에서 major/minor 파생

### Release identity / package hygiene

- csproj Version ↔ published ProductVersion boundary 검증
- FIRST_RUN 첫 줄 exact version 검증
- PDB / nested archive / root DLL / forbidden legacy dependency 차단
- 실제 published EXE smoke 유지

### Map donor reproducibility

- 과거 작업 fork 원격이 clean CI checkout에서 더 이상 재현 가능하지 않은 상태 확인
- 같은 exact Git object가 공개 upstream `SIGDrone/Tarkov-Helper`에 존재함을 확인
- `.gitmodules` fetch origin만 공개 upstream으로 변경
- Map source pin `d933792b6042a51cea38dc44b686a096fe30de67` 자체는 변경하지 않음

### Map late-suppression race

첫 exact-release smoke는 public Release 생성 전에 Factory의 타층 standard marker late-state 회귀를 검출했습니다.

원인:

- pinned donor의 legacy current-floor-only filter가 200ms × 최대 12회 동작
- JunhyunHelper product presentation 적용 이후에도 타층 marker를 뒤늦게 `Collapsed`로 덮어쓸 수 있음
- 기존 first-party settle window보다 donor filter window가 길어 race가 남아 있었음

수정:

- donor source/pin 변경 없음
- donor가 `_sharedFloorHiddenMarkers`에 직접 기록한 **floor 때문에 Visible → Collapsed한 element만** post-filter에서 복구
- category/faction visibility는 donor가 계속 소유
- 복구 직후 JunhyunHelper floor presentation을 재적용
- page unload/reload 후 donor timer가 재생성되어도 product callback 재부착
- 새 permanent polling 없음
- smoke threshold를 낮추지 않음

수정 후 PR CI와 최종 exact-release/public-downloaded smoke 모두 통과했습니다.

상세: `docs/MAP_RUNTIME_COMPATIBILITY.md`, `docs/FINAL_AUDIT_1.0.0.md`

---

## 4. 버전 정책

권위 문서: `docs/VERSIONING.md`

v1 이후:

- 새 사용자 기능 추가 → `MINOR + 1`, `PATCH = 0`
- 기존 기능 수정/보완/변경, 버그 수정, 성능/안정성 개선 → `PATCH + 1`
- 새 기능과 수정이 한 릴리즈에 함께 있으면 MINOR 규칙 우선

예:

```text
1.0.0 + Scanner 실제 기능 → 1.1.0
1.0.0 + Quest 수정 → 1.0.1
1.0.1 + Scanner 실제 기능 → 1.1.0
```

MAJOR 증가 조건은 필요할 때 사용자와 별도 확정하며 개발자가 임의로 정의하지 않습니다.

---

## 5. Program update 계약

일반 실행 시 latest public stable GitHub Release를 확인합니다.

```text
MainWindow 표시
→ latest stable GitHub Release 비동기 조회
→ latest <= current: 아무 UI 없음
→ latest > current: Yes/No 동의창
→ Yes: exact ZIP + SHA256SUMS 다운로드
→ SHA-256 + package contract 검증
→ 임시 self-copy updater 실행
→ 원래 프로세스 종료
→ program-owned files transaction 교체
→ 새 EXE 재실행
```

업데이트 대상:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

업데이트 비대상:

```text
%LocalAppData%/JunhyunHelper/user.db
content/
image-cache/
map-product-settings.json(.bak)
ammo-favorites.json(.bak)
logs/
```

실패 정책:

- latest 조회 실패 → 앱 정상 사용
- 사용자 No → 앱 정상 사용, 다음 실행 때 다시 확인
- download/checksum/package 검증 실패 → 현재 program files 미변경
- updater runner 시작 실패 → 현재 program files 미변경
- 교체 중 실패 → previous owned files rollback + 기존 EXE 재실행 시도
- diagnostic → `%LocalAppData%/JunhyunHelper/logs/startup.log`

보안/무결성:

- stable strict `vMAJOR.MINOR.PATCH`만 대상
- exact Windows ZIP + `SHA256SUMS.txt` 요구
- GitHub Release asset URL scope 검증
- SHA-256 검증
- ZIP path traversal / symlink / duplicate entry / unexpected root / PDB 거부
- 검증 완료 전 기존 program files 변경 금지

상시 `Updater.exe`는 배포하지 않습니다.

상세: `docs/PROGRAM_UPDATE.md`

---

## 6. Content / User Progress

### Content

```text
Current schema: v7
Readable schemas: v3, v4, v5, v6, v7
v0.1.14 → v1.0.0 mandatory Game Content refresh: none
```

- v3: Wiki Ballistics membership/effectiveness 분리
- v4: Quest geometry
- v5: availability metadata / opaque conditions
- v6: recoverable special-trader access와 ordinary prerequisite 분리
- v7: structured `globalVariable` requirement

### User Progress

```text
user.db SQLite schema: v1
v0.1.14 → v1.0.0 user data migration: none
```

한 GameMode당 독립 profile 하나를 사용합니다.

저장 사실:

- level / faction / edition / prestige
- trader LL / 필요한 standing
- completed Quest IDs
- 필요한 explicit failed Quest IDs
- optional exact profile variables
- sparse recoverable special-trader access facts
- Hideout levels
- FIR / non-FIR Inventory
- Quest / Hideout consumption ledgers

---

## 7. Quest availability 정확도

기본 원칙:

- 서로 다른 `taskRequirements`는 AND
- 한 requirement 내부 `status[]`는 OR
- source의 complete / active / failed 의미 보존
- 별도 “수주 가능” 상태 없음
- 받을 수 있는 Quest는 Helper에서 이미 accepted/current로 간주
- 증명할 수 없는 availability → `Indeterminate / 확인 필요`
- `Indeterminate`를 optimistic Current로 승격하지 않음

특수 규칙:

- BTR Driver: 검증된 Active gate 보강
- Ref: source gate + GameMode별 검증된 unlock Complete
- Lightkeeper: ordinary prerequisite와 recoverable access 분리
- audited dialogue exact-ID compatibility만 사용
- availability delay에 필요한 실제 completion timestamp가 없으면 확인 필요
- exact profile-variable current value가 있으면 권위값
- exact 값이 없으면 audited current compatibility 외 추측 금지

Future Needed Items에서는 불확실한 미래 Quest도 잠재 필요 Item을 계속 보호합니다.

---

## 8. Needed Items / Inventory

- 미래 진행 가능 Quest + 미래 Hideout level 재료 포함
- Hideout station progress 없음 = Lv.0
- unresolved future Quest = `IndeterminatePotential`, Item 보호
- flexible hand-in은 후보 group으로 보존
- 실제 flexible 소비 Item 자동 추측 금지
- fixed completion material은 명시적 진행 조작 때 consumption ledger로 자동 소비
- rollback 시 exact consumed ledger 복구 가능
- cleanup은 future fixed requirement와 보호 규칙을 반영
- inventory-only mutation은 existing planning basis 재사용

---

## 9. Ammo / preference persistence

Ammo:

- read-only 비교
- name / caliber 검색
- exact caliber / Ammo navigation
- membership과 Armor effectiveness 별도 fact
- caliber favorites shortcut

Preference JSON:

```text
%LocalAppData%/JunhyunHelper/ammo-favorites.json(.bak)
%LocalAppData%/JunhyunHelper/map-product-settings.json(.bak)
```

- same-directory temp
- durable write
- atomic replacement
- last-known-good backup
- corrupt primary → good backup fallback
- preference save failure는 nonfatal

---

## 10. Map / MiniMap

Pinned source:

```text
Gitlink: d933792b6042a51cea38dc44b686a096fe30de67
Fetch origin: https://github.com/SIGDrone/Tarkov-Helper.git
```

제품 계약:

- floor는 visibility filter가 아니라 presentation relation
- enabled 타층 marker 유지
- current / above / below relation 표시
- cross-floor near-overlap 자체는 duplicate 아님
- Main Map floor 변경 시 live zoom + map-space viewport center 보존
- MiniMap floor 변경 시 exact live Scale + Translate X/Y 보존
- Main Map selector / shared map key 동기화
- Quest sidebar lane alignment 유지
- Map preference writes coalesce
- product hotkey / keyboard hook failure를 global fatal로 확대하지 않음

pinned donor를 concrete regression/performance 근거 없이 wholesale refactor하지 않습니다.

v1.0.0의 donor legacy floor-only filter compatibility는 `docs/MAP_RUNTIME_COMPATIBILITY.md`가 권위 문서입니다.

---

## 11. Scanner

**상단 `스캐너` 탭은 visible, 내용은 `준비 중` placeholder입니다.**

- 실제 scanning / recognition / import 기능 없음
- 구현된 기능처럼 가장하지 않음
- 별도 사용자 요구 확정 전 임의 구현 금지
- maintenance에서 임의 숨김/삭제 금지

---

## 12. Release / rendered UI gate

소스 inspection이나 build 성공만으로 주요 UI/Map 변경을 완료 처리하지 않습니다.

상시 CI는 최소 다음을 검사합니다.

- Release build
- 전체 자동 테스트
- self-contained single-file publish
- Version/ProductVersion/FIRST_RUN identity
- root layout
- PDB/nested archive/legacy dependency pollution
- actual published executable Product UI smoke
- Main Map / Factory / MiniMap runtime smoke
- late floor marker final-state contract
- 정상 MainWindow close/process exit
- portable root runtime pollution 없음

정식 릴리즈는 여기에 Draft asset 재다운로드 검증, public asset 재다운로드 검증, public-downloaded EXE smoke를 추가합니다.

---

## 13. 현재 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / fail-closed availability |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 / future protection / ledger |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / donor compatibility + rendered runtime gate |
| Game Content Update | 구현 완료 / candidate validation / last-known-good |
| Program Update | 구현 완료 / v1.0.0 public verified |
| Scanner | visible `준비 중` placeholder |

---

## 14. 현재 알려진 비차단 범위

- EFT Story Chapters는 ordinary current task source 범위 밖이며 임의 추측하지 않음
- audited task-pool/profile-variable 구조가 drift하면 해당 부분 fail-closed
- Map donor 내부 legacy 구조는 제품 계약 위반 근거 없이 broad refactor하지 않음
- installer/code signing은 현재 필수 범위 아님
- 별도 사용자 요구가 없는 기능 확장은 하지 않음

---

## 15. 새 작업을 시작할 때

`AGENTS.md`의 복구 순서를 따릅니다.

핵심 문서:

1. `README.md`
2. `docs/STATE.md`
3. `docs/PRODUCT.md`
4. `docs/DECISIONS.md`
5. `docs/DEVELOPER_REFERENCE.md`
6. `docs/ARCHITECTURE.md`
7. `docs/VERSIONING.md`
8. `docs/DEVELOPMENT.md`
9. `docs/REFERENCE_POLICY.md`
10. Map 작업이면 `docs/MAP_PRODUCT_REQUIREMENTS.md` + `docs/MAP_RUNTIME_COMPATIBILITY.md`
11. 관련 code/tests/issues/PR

현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않습니다. 사용자 확정 요구사항과 공식 결정이 우선합니다.
