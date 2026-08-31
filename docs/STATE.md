# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 대화 기억이 아니라 저장소의 공식 문서, 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-08-31 KST**  
상태: **v1.12.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품과 운영 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 현재 확정된 제품 요구사항 범위와 Scanner 기능은 완성 상태이며 기본 운영 모드는 유지보수다.

현재 진행 중 작업은 없다. `docs/ACTIVE_WORK.md`는 `NONE`이다.

주요 제품 영역:

- GameMode별 Profile / User Progress
- Quest / Hideout / Needed Items / Inventory / cleanup
- Items / Ammo / cross-navigation / profile-aware pickup
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Program Update
- Scanner + Mini Scanner
- Scanner Saved Case / Ground Truth / diagnostics / regression dataset
- Scanner item database / Favorites / Recents
- opt-in PC capture/Scanner 지원 진단

Runtime GPT/AI 의존성은 없다.

## 2. 현재 public stable

```text
version: v1.12.1
exact product release source/tag target:
07a808f187e59f1b2b4b62ca6a947ccbed9baeaa
PR: #239 — MERGED
validated feature head: 7e418c7d32c945260b471d19ac43c411f15bef1b
PR exact-head CI: 33350561623 — SUCCESS
PR exact-head Shutdown Race CI: 33350561588 — SUCCESS
PR exact-head Documentation Consistency: 33350561628 — SUCCESS
exact-main CI: 33350742745 — SUCCESS
exact-main Shutdown Race CI: 33350742733 — SUCCESS
exact-main Documentation Consistency: 33350742720 — SUCCESS
release workflow: 33350893047 — SUCCESS
release id: 379473487
published UTC: 2026-08-31T02:31:04Z
483 passed / 0 failed / 0 skipped
```

Public release package:

```text
Junhyun-Helper.zip
asset id: 537336876
bytes: 80,572,885
SHA-256:
fbbaa41bbb41843a54ccbdd16721c138d93ddea34092fd7e468bbb3d99ed9212

SHA256SUMS.txt
asset id: 537336877
bytes: 86
SHA-256:
aa63dffbea42d2b624b74b96c6acc38dbe34906186c9ea43727abac7fc8c0619
```

Exact-main artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9743552872
archive bytes: 241,651,204
archive SHA-256:
f65de2b7a1da8f27302cdff815b6978d4ae291fe81964e2d131ec57fbb40050a
```

GitHub `/releases/latest`, release target, `refs/tags/v1.12.1`, exact-main source가 모두 `07a808f187e59f1b2b4b62ca6a947ccbed9baeaa`에 일치한다. Release는 `draft=false`, `prerelease=false`이다.

공식 공개 증거:

- `docs/RELEASE_1.12.1.md`
- `docs/.release-v1.12.1-status.json`
- `docs/RELEASE_NOTES_V1.12.1.md`

## 3. v1.12.1 — 김태영 PC 진단 사용자 흐름

정상 성공 경로:

```text
메인 헤더 좌측 프로필 이미지 클릭
→ “혹시 김태영 본인?”
→ 예
→ indeterminate progress overlay 표시
→ local diagnostic exporter 실행
→ Desktop ZIP 생성
→ “진단 완료.\n파일을 hyune4784@naver.com 으로 보내주세요.”
→ 완료창 닫기
→ 기본 브라우저에서 https://mail.naver.com/v2/new 열기
```

계약:

- `아니오`면 진단하지 않는다.
- 실행 중 중복 진단 entry를 막는다.
- progress는 exporter task가 끝날 때까지 표시한다.
- 성공 문구는 위 두 문장만 사용한다.
- ZIP은 자동 업로드하지 않는다.
- 웹메일 DOM/UI를 자동 조작하지 않는다.
- 파일을 자동 첨부하지 않는다.
- 이메일을 자동 발송하지 않는다.
- compose page launch 실패는 내부 diagnostic log에 기록하고 이미 성공한 ZIP 생성 결과를 실패 처리하지 않는다.

구현 authority:

- `src/JunhyunHelper.Desktop/MainWindow.xaml`
- `src/JunhyunHelper.Desktop/MainWindow.KimTaeyoungDiagnostic.cs`
- `src/JunhyunHelper.Desktop/Scanner/KimTaeyoungPcDiagnosticExporter.cs`
- `tests/JunhyunHelper.Tests/Maintenance/V120QuestDiagnosticsUiContractTests.cs`

## 4. 김태영 진단 evidence 수집 계약

진단은 Scanner/capture 결과에 영향을 줄 수 있는 환경을 폭넓게 수집하되 불필요한 식별/secret 정보를 배제한다.

수집 대상:

- Windows/runtime/process architecture
- display bounds/resolution/bpp/DPI/virtual screen
- GPU/driver/monitor
- dxdiag HDR support / color space / luminance / current mode 등 allowlist display fields
- Discord/OBS/NVIDIA/AMD/RTSS/Game Bar 등 allowlisted capture/overlay process 존재/버전
- Scanner settings/runtime/catalog/support bundle
- 각 display screen copy와 RGB/휘도/clipping 통계
- Tarkov 실행 중일 때 client screen-copy와 PrintWindow capture 비교
- optional probe failure 목록

명시적 제외:

- Windows 사용자명
- 컴퓨터명
- IP/MAC
- 네트워크 목록
- 환경변수 전체
- token/password/credential
- 임의 전체 process inventory
- 설치 경로

단, 명시적으로 진단을 실행하면 실제 화면 PNG에는 당시 화면에 보이는 내용이 포함될 수 있다.

각 optional probe는 fail-soft다. 핵심 ZIP 작성 실패만 전체 실패로 처리한다.

## 5. 사용자 노트북 실사용 smoke evidence

사용자가 v1.12.0에서 직접 생성한 `JunhyunHelper-KimTaeyoung-Diagnostic-20260831-110826.zip`을 검토했다.

- ZIP CRC 정상
- expected top-level evidence 11개 모두 존재
- `probe-errors.txt = none`
- display screenshot + luminance stats 정상
- nested `scanner/scanner-support.zip` CRC 및 구성 정상
- Scanner/catalog snapshot 정상
- `captures/tarkov.txt = EscapeFromTarkov window not found.`
- 당시 Tarkov가 실행되지 않았으므로 Tarkov dual-capture comparison이 없는 것은 정상
- allowlist 관련 process가 없어서 `relevant-processes.txt`가 헤더만 있는 것도 정상

이 evidence는 exporter의 실제 사용자 환경 정상 동작을 확인한 것이며 김태영 PC의 밝기/capture 문제 원인을 판단하는 자료는 아니다. 원인 판정은 김태영 실제 PC에서 생성한 ZIP을 사용한다.

## 6. Scanner 유지 계약

- false positive보다 miss를 선호한다.
- OCR/matcher/candidate/recovery acceptance는 reviewed actual Tarkov evidence 없이 완화하지 않는다.
- recognition proof에 price/needed/source/relationship metadata를 사용하지 않는다.
- scan-time network I/O를 proof에 추가하지 않는다.
- recognition은 external screen pixels + OCR만 사용한다.

사용하지 않음:

- game process memory read
- code/DLL injection
- process/game hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

Correction hotkey는 evidence-only Saved Case를 저장하고 Ground Truth를 자동 생성/추측하지 않는다.

## 7. Quest / Needed Items 계약

v1.12.0의 staged task-pool compatibility를 유지한다.

- exact ProfileVariable 값은 항상 최우선이다.
- 현재 trader LL이 audited pool stage보다 낮으면 잠금 의미 유지
- current stage는 기존 보수적 reconstruction / fail-closed 유지
- current trader LL이 audited stage보다 높으면 과거 stage의 runtime-only threshold floor 사용
- 이 floor는 숨은 server counter의 exact fact로 저장하지 않는다.
- structural drift는 fail-closed한다.
- Future Needed Items / cleanup은 current Quest UI compatibility를 낙관적으로 전파하지 않고 기존 보수적 reachability를 유지한다.

## 8. Hideout / Ammo 계약

- Hideout FIR은 source `attributes.foundInRaid`를 canonical requirement에 보존한다.
- FIR requirement에는 non-FIR inventory가 충당되지 않는다.
- Ammo pickup은 same-caliber penetration 및 현재 profile에서 증명된 direct purchase 상태를 기준으로 한다.
- flea/barter/craft/higher LL/unproven quest unlock은 현재 직접 구매 가능으로 취급하지 않는다.
- Ammo Pack은 authoritative `containsItems` 관계를 우선한다.

## 9. Map / MiniMap 계약

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper product bridge가 selection/lifecycle/presentation 의미를 소유한다. Main Map selection은 fresh/reused MiniMap에 동기화되고 player heading은 map별 affine transform과 동일한 좌표계를 사용한다. 표시 대상 marker data가 존재하지만 standard layer만 비는 bounded empty-layer recovery와 Player Marker Size isolation 계약을 유지한다.

## 10. Game Content / Program Update 계약

Game Content:

- candidate download/build
- schema/completeness/integrity validation
- validated active 승격
- Last Known Good 보존
- validation 실패 시 기존 정상 데이터 유지

Program Update:

- GitHub public stable release 확인
- 사용자 동의 없이 자동 교체하지 않음
- stable ZIP + checksum 검증
- release workflow는 exact-main CI artifact 사용
- 이미 공개된 stable release asset은 immutable historical product로 취급

## 11. Schema / compatibility

```text
Desktop version: 1.12.1
Content schema write: v8
Readable Content schemas: v3, v4, v5, v6, v7, v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog write: v4
Scanner catalog readable: v1, v2, v3, v4
```

v1.12.0 → v1.12.1:

- mandatory Game Content migration: none
- user.db migration: none
- Scanner display settings migration: none

## 12. v1.12.1 검증

Exact product source `07a808f187e59f1b2b4b62ca6a947ccbed9baeaa`은 다음을 통과했다.

- 483 deterministic tests
- Windows Release build
- Windows x64 self-contained publish
- actual published EXE Product UI / Map smoke
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback
- public ZIP digest 검증

PR 작업 중 최초 Documentation Consistency 1회는 `ACTIVE_WORK` required heading 누락만 검출했다. 제품/runtime 실패가 아니었고 heading을 수정한 exact PR head에서 전체 gate가 성공했다.

## 13. 사용자 실사용 상태

사용자 노트북에서는 diagnostic exporter 자체의 실제 ZIP 생성이 정상임을 확인했다. v1.12.1 공개 바이너리 전체를 사용한 최종 실제 Tarkov 플레이 검증과 김태영 실제 PC diagnostic ZIP 수집/분석은 자동화 검증과 별개이며 현재 **PENDING**이다.

## 14. 다음 작업

현재 남은 릴리즈 작업은 없다. `docs/ACTIVE_WORK.md`는 `NONE`이다. 새 사용자 요구사항, 실사용 회귀, Tarkov 변화, 또는 김태영 실제 diagnostic evidence가 들어오면 v1.12.1 stable에서 필요한 범위만 분석·수정한다.

후속 documentation-only commit은 v1.12.1 제품 릴리즈 source가 아니다. historical identity는 `07a808f187e59f1b2b4b62ca6a947ccbed9baeaa`에 고정한다.
