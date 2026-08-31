# RELEASE v1.12.1 — PUBLIC / VERIFIED

Date: **2026-08-31 KST**

## Release identity

```text
version: v1.12.1
status: PUBLIC STABLE / VERIFIED
exact product release source:
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

Tag `refs/tags/v1.12.1`, release `target_commitish`, GitHub `/releases/latest`, exact-main product source가 모두 `07a808f187e59f1b2b4b62ca6a947ccbed9baeaa`에 일치한다. Release는 `draft=false`, `prerelease=false`이다.

## Exact-main artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9743552872
archive bytes: 241,651,204
archive SHA-256:
f65de2b7a1da8f27302cdff815b6978d4ae291fe81964e2d131ec57fbb40050a
```

## Public assets

```text
Junhyun-Helper.zip
asset id: 537336876
bytes: 80,572,885
SHA-256 / GitHub digest:
fbbaa41bbb41843a54ccbdd16721c138d93ddea34092fd7e468bbb3d99ed9212

SHA256SUMS.txt
asset id: 537336877
bytes: 86
SHA-256 / GitHub digest:
aa63dffbea42d2b624b74b96c6acc38dbe34906186c9ea43727abac7fc8c0619
```

Release workflow는 exact-main CI artifact를 다운로드하고 package manifest/actual ZIP hash를 검증한 뒤 공개했다. 공개용 제품 바이너리를 별도로 다시 빌드하지 않았다.

## Product changes

- 김태영 PC 진단 시작 확인 문구를 정확히 `혹시 김태영 본인?`으로 고정했다.
- `예` 후 ZIP 생성 동안 별도 indeterminate progress bar를 표시한다.
- 성공 완료 문구는 정확히 `진단 완료.` 및 `파일을 hyune4784@naver.com 으로 보내주세요.` 두 문장이다.
- 완료 안내를 닫은 뒤 기본 브라우저로 `https://mail.naver.com/v2/new`를 연다.
- ZIP은 Desktop에 로컬 생성한다. 자동 업로드, 웹메일 DOM 조작, 자동 첨부, 자동 발송은 하지 않는다.
- browser compose launch 실패는 diagnostic log에만 기록하며 이미 성공한 diagnostic bundle을 실패로 바꾸지 않는다.

## User laptop diagnostic evidence

사용자가 v1.12.0에서 직접 생성한 실제 diagnostic ZIP을 검토했다.

- ZIP CRC 정상
- expected top-level evidence 11개 모두 생성
- `probe-errors.txt = none`
- display capture / luminance stats 정상
- nested Scanner support ZIP 정상
- Scanner/catalog snapshot 정상
- 실행 당시 Tarkov가 없어서 `captures/tarkov.txt = EscapeFromTarkov window not found.`이며 Tarkov dual-capture evidence는 이번 샘플에 없음
- allowlist 대상 관련 프로세스가 실행되지 않아 `relevant-processes.txt`가 헤더만 있는 것은 정상

따라서 exporter 자체는 실제 사용자 노트북에서 정상 동작했다. 김태영 PC의 밝기/capture 원인 판정은 김태영 PC에서 생성한 별도 ZIP을 사용한다.

## Regression coverage

v1.12.1 deterministic suite는 483 tests다. 신규/갱신 계약은 fixed prompt, progress overlay, fixed completion copy, Naver compose URL launch, 과거 동적 경로 안내 미사용을 고정한다. 전체 Release build/publish, actual published EXE Product UI + Map smoke, graceful shutdown, active-async Shutdown Race, package audit도 성공했다.

## Compatibility

v1.12.1은 PATCH UX 유지보수 릴리즈이며 data/schema migration은 없다.

```text
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog write: v4
Scanner catalog readable: v1~v4
Map donor: SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

후속 documentation-only commit은 v1.12.1 product release source가 아니다. v1.12.1 historical identity는 `07a808f187e59f1b2b4b62ca6a947ccbed9baeaa`에 고정한다.
