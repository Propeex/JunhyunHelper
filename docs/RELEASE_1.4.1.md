# RELEASE 1.4.1 — Scanner live Ground Truth header fix

상태: `PUBLIC RELEASE / VERIFIED`

기준일: 2026-08-24

## 범위

v1.4.1은 새 사용자 기능을 추가하지 않는 PATCH 릴리즈다. 실제 Tarkov 1920x1080 교정 Ground Truth 4건에서 상세보기 창 후보가 존재함에도 header lock 단계에서 반복 탈락하여 OCR까지 도달하지 못하던 문제를 수정한다.

## 제품 변경

- 기존 v1.4.0 `ScannerInspectHeaderLock`을 primary/authoritative 경로로 그대로 유지한다.
- primary가 fail-closed 한 경우에만 `ScannerLiveHeaderGroundTruthRefiner`가 실제 인게임 Ground Truth에서 측정한 헤더 구조를 검증한다.
- live fallback은 다음 evidence를 함께 요구한다.
  - 어두운 red close control
  - gray 38~39를 포함할 수 있는 긴 neutral top border
  - upper-right lens + lower-left handle 형태의 magnifier
  - dark title field
  - title text evidence
- coarse 상세창 후보의 top이 실제 header보다 아래로 내려간 사례를 위해 제한된 범위에서 위쪽 header를 복구할 수 있다.
- recovered header는 coarse candidate와 left/width ownership이 일치해야 하므로 인접한 stash/inventory frame을 빌려오는 식의 오탐을 막는다.
- 1회 고정밀 스캔은 detector의 최대 12개 후보를 전부 확인한다.
- 연속 Scanner의 기존 8개 후보 CPU budget은 유지한다.

## 변경하지 않은 계약

- structural floor: `0.34`
- trusted header floor: `0.68`
- Windows ko-KR OCR 및 deep OCR 정책
- Tarkov-font visual recovery
- Item matcher confidence / top1-top2 정책
- 공식 한국어 item catalog authority
- 스캔 순간 network 미사용
- 게임 메모리 읽기 / DLL injection / packet interception 미사용

## Ground Truth 근거

사용자가 실제 인게임에서 4회 시도 후 상세보기 창 식별 실패를 교정 데이터로 제공했다. 분석 결과:

- 실제 close X의 기존 template score가 약 `0.420~0.421`로 기존 synthetic gate `0.46` 아래였다.
- 실제 magnifier handle 방향이 기존 synthetic fixture와 반대였다.
- 일부 live top-border pixel은 gray `38~39`였다.
- 각 capture에서 교정 상세창과 IoU 약 `0.995`인 구조 후보가 생성되었거나, retained overlapping candidate에서 실제 header를 복구할 수 있었다.
- 한 사례의 정답 후보는 detector rank 11에 있어 기존 one-shot 8후보 상한 밖이었다.

임계값을 임의로 완화한 것이 아니라 위 reviewed Ground Truth에서 직접 확인된 실패 단계만 보완했다.

## 회귀 방지

`ScannerLiveHeaderGroundTruthSmoke`는 사용자 screenshot bytes나 item identity를 포함하지 않고, 측정된 1920x1080 header 특성을 synthetic frame으로 재현한다. 제품 smoke 환경에서 fallback이 `HEADER_FRAME_LOCKED` 및 score `>= 0.68`을 만들지 못하면 CI가 실패한다.

## 수정 PR 검증

PR #155 final head: `b634e9b6e819013cae38f1782b5dd333f3966815`

CI run: `32648713289`

- Windows Desktop build: SUCCESS
- Core tests: `268 passed / 0 failed / 0 skipped`
- Windows x64 self-contained single-file publish: SUCCESS
- package layout audit: SUCCESS
- actual packaged EXE Product UI + Map/Factory/MiniMap + Scanner Ground Truth smoke: SUCCESS
- graceful shutdown + clean portable root: SUCCESS

PR #155 merge commit: `8659df2834b30bd31314eae0a1855c682b4bea81`

## 릴리즈 게이트

아래 릴리즈 게이트는 모두 충족되었습니다.

1. release-prep PR CI success
2. exact release source SHA 고정
3. tag `v1.4.1`이 exact release source를 가리킴
4. `Junhyun-Helper-v1.4.1-win-x64.zip` 공개
5. public latest가 v1.4.1
6. public ZIP 재다운로드 SHA-256 검증
7. 공개 `SHA256SUMS.txt`와 실제 ZIP hash 일치
8. package layout 및 ProductVersion `1.4.1+<source_sha>` 검증
9. 공개 ZIP의 실제 EXE product smoke / graceful shutdown 성공

## 공개 검증 결과

```text
release source/tag: 8ff790cbcaa3172d068200d5b34de1ea4c142ac0
fix PR #155 CI: 32648713289 — SUCCESS
release-prep PR #156 CI: 32649049071 — SUCCESS
automated tests: 268 passed / 0 failed / 0 skipped
exact-source release run: 32652350079 — SUCCESS
independent public verifier: 32652827208 — SUCCESS
asset: Junhyun-Helper-v1.4.1-win-x64.zip
bytes: 80,379,956
SHA-256: 7f666e3348b3d87aae27e22de078c1b3f36458f107a662cae1c58df8cdfa3e6f
ProductVersion: 1.4.1+8ff790cbcaa3172d068200d5b34de1ea4c142ac0
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public SHA256SUMS: VERIFIED
public package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
one-shot release/verifier/finalizer workflows: CLEANED UP
```

기계 판독 가능한 영구 증거는 `docs/.release-v1.4.1-status.json`입니다. 공개 바이너리는 release source `8ff790cbcaa3172d068200d5b34de1ea4c142ac0`에서 생성되었고, 후속 문서/housekeeping 변경은 공개 바이너리를 변경하지 않습니다.
