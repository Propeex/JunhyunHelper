# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.12.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태입니다. 새로운 실제 회귀, Tarkov 호환성 변화, 또는 사용자가 명시적으로 확정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 추측성 대규모 구조 변경을 시작하지 않습니다.

공식 프로젝트 기억은 대화가 아니라 저장소 문서와 코드입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태와 유지 계약
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 설계·제품 결정

## 현재 공개 릴리즈

```text
version: v1.12.1
Desktop target version: 1.12.1
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
Release workflow: 33350893047 — SUCCESS
release id: 379473487
483 passed / 0 failed / 0 skipped
published UTC: 2026-08-31T02:31:04Z
```

Public package:

```text
Junhyun-Helper.zip
asset id: 537336876
bytes: 80,572,885
SHA-256:
fbbaa41bbb41843a54ccbdd16721c138d93ddea34092fd7e468bbb3d99ed9212
```

Checksum asset:

```text
SHA256SUMS.txt
asset id: 537336877
bytes: 86
asset SHA-256:
aa63dffbea42d2b624b74b96c6acc38dbe34906186c9ea43727abac7fc8c0619
```

Exact-main CI artifact:

```text
JunhyunHelper-win-x64
artifact id: 9743552872
archive bytes: 241,651,204
archive SHA-256:
f65de2b7a1da8f27302cdff815b6978d4ae291fe81964e2d131ec57fbb40050a
```

GitHub `/releases/latest`, release target, `refs/tags/v1.12.1`, exact-main product source가 모두 `07a808f187e59f1b2b4b62ca6a947ccbed9baeaa`에 일치합니다. 공개 release는 `draft=false`, `prerelease=false`입니다.

공식 v1.12.1 공개 기록:

- `docs/RELEASE_1.12.1.md`
- `docs/RELEASE_NOTES_V1.12.1.md`
- `docs/.release-v1.12.1-status.json`
- `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`

후속 documentation-only commit은 v1.12.1 제품 릴리즈 소스가 아닙니다. product source/tag/assets는 위 exact source에 고정된 historical identity입니다.

## v1.12.1 — 김태영 PC 진단 UX

메인 헤더 좌측 프로필 이미지를 클릭하면 김태영 PC 진단을 실행할 수 있습니다.

```text
프로필 이미지 클릭
→ “혹시 김태영 본인?”
→ 예
→ indeterminate progress bar 표시
→ 진단 ZIP 생성
→ “진단 완료.”
   “파일을 hyune4784@naver.com 으로 보내주세요.”
→ 기본 브라우저에서 https://mail.naver.com/v2/new 열기
```

- ZIP은 Desktop에 로컬 생성합니다.
- 자동 업로드하지 않습니다.
- 웹메일 화면을 자동 조작하지 않습니다.
- ZIP을 자동 첨부하거나 메일을 자동 발송하지 않습니다.
- browser compose launch 실패는 진단 결과를 실패로 바꾸지 않고 diagnostic log에만 남깁니다.

사용자가 v1.12.0에서 자신의 노트북으로 생성한 실제 diagnostic ZIP은 CRC, expected evidence 11개, `probe-errors.txt = none`, display capture/stats, nested Scanner support bundle이 모두 정상임을 확인했습니다. 당시 Tarkov가 실행되지 않아 Tarkov dual-capture evidence만 생성되지 않았습니다.

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
- self-contained executable
- 별도 .NET Runtime 설치 불필요
- installer 없음
- 일반 사용에 관리자 권한 불필요

사용자 데이터는 `%LocalAppData%/JunhyunHelper` 아래에 저장됩니다.

## 주요 기능

- GameMode별 Profile / User Progress
- Quest / Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger / cleanup
- Items / cross-navigation
- Ammo / favorites / 현재 프로필 기반 pickup 판단
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth / diagnostics / Saved Case / regression dataset
- Scanner 아이템 정보 DB / Favorites / Recents
- opt-in PC capture/Scanner 지원 진단
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## 주요 안전·유지 계약

- Scanner는 external screen pixels + OCR만 사용하며 game memory read, injection, hook, kernel/driver 접근, input automation, network manipulation, anti-cheat bypass를 사용하지 않습니다.
- false positive보다 miss를 선호하며 actual Tarkov evidence 없이 OCR/matcher/candidate acceptance를 임의 완화하지 않습니다.
- Game Content update는 candidate → validation → active/LKG 전환의 fail-closed 계약을 유지합니다.
- Quest exact ProfileVariable은 runtime compatibility보다 항상 우선합니다.
- Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산합니다.
- Hideout FIR은 source `attributes.foundInRaid` 의미를 보존합니다.
- Ammo pickup은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 합니다.
- Map/MiniMap donor는 pinned revision `d933792b6042a51cea38dc44b686a096fe30de67`입니다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE smoke까지 검증합니다.

## Schema / compatibility

```text
Desktop target version: 1.12.1
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.12.0 → v1.12.1 mandatory data/schema migration은 없습니다.

## 검증

v1.12.1 exact product source `07a808f187e59f1b2b4b62ca6a947ccbed9baeaa`은 483 deterministic tests, Windows Release build, Windows x64 self-contained publish, actual published EXE Product UI / Map smoke, graceful shutdown, active-async Shutdown Race, package/checksum audit, exact-main Documentation Consistency, artifact upload, verified Release workflow, public tag/release/assets/latest-stable readback을 통과했습니다.

사용자의 실제 PC/Tarkov 최종 실사용 확인과 김태영 실제 PC diagnostic ZIP의 수집·분석은 자동화 검증과 별개이며 현재 `PENDING`입니다.
