# 준현 헬퍼 v1.5.0 — Public Release Verification

기준일: 2026-08-24
상태: **PUBLIC RELEASE / VERIFIED**

## 릴리즈 식별자

```text
version: v1.5.0
exact source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
asset: Junhyun-Helper-v1.5.0-win-x64.zip
bytes: 80,422,292
SHA-256: 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
ProductVersion: 1.5.0+6de738959740d12e6ccb81b65e50006e463eb699
automated tests: 296 passed / 0 failed / 0 skipped
release workflow: 32691423654 — SUCCESS
independent public verifier: 32691641614 — SUCCESS
published_at_utc: 2026-08-24T04:51:44Z
verified_at_utc: 2026-08-24T04:53:44.8811067Z
```

Machine-readable durable record:

- `docs/.release-v1.5.0-status.json`

## 검증 게이트

최종 제품 PR #172의 release-candidate HEAD `c4f2ad664de0bc3839d47acdf0a1d436634f26ff`에서 CI run `32688080850`이 다음을 모두 통과했다.

- Desktop Release build
- 296 Core tests / 0 failed / 0 skipped
- Windows x64 self-contained single-file publish
- release package identity audit
- rendered Product UI smoke
- Main Map / Factory / MiniMap smoke
- Scanner / Mini Scanner product smoke
- graceful shutdown
- clean portable root 확인

PR #172는 위 검증 후 main에 merge되었고, 릴리즈 exact source는 merge commit `6de738959740d12e6ccb81b65e50006e463eb699`로 고정했다.

## Release controller 검증

Release workflow run `32691423654`는 exact source를 다시 checkout하여 다음을 모두 수행했다.

1. exact source build
2. 296 tests
3. Windows x64 publish 및 package audit
4. packaged Product UI / Map / Scanner smoke
5. v1.5.0 draft release 생성 또는 안전한 recovery
6. exact source tag 생성 및 확인
7. draft asset 재다운로드
8. SHA-256 / ProductVersion / FIRST_RUN / Map DB 확인
9. 재다운로드 EXE smoke
10. stable/latest 공개 전환

모든 단계의 conclusion은 `success`였다.

## 독립 public verifier

Fresh Windows runner의 verifier run `32691641614`는 release controller의 local artifact를 재사용하지 않았다.

GitHub 공개 API와 공개 asset URL을 통해 익명으로 다음을 다시 검증했다.

- public latest가 `v1.5.0`
- draft=false / prerelease=false
- public tag가 exact source `6de738959740d12e6ccb81b65e50006e463eb699`를 가리킴
- `Junhyun-Helper-v1.5.0-win-x64.zip` 존재
- `SHA256SUMS.txt` 존재
- 공개 ZIP 재다운로드 성공
- SHA-256이 release-controller 결과 및 SHA256SUMS와 정확히 일치
- 파일 크기 80,422,292 bytes 일치
- package root가 `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets/` 구조를 유지
- `Assets/tarkov_data.db` 존재
- PDB / nested archive 미포함
- ProductVersion exact match
- FIRST_RUN exact version match
- public-downloaded EXE의 Product UI / Map / Scanner smoke 성공
- 정상 Main Window close 및 process termination 성공
- portable root에 runtime Logs 폴더가 생성되지 않음

따라서 v1.5.0은 **public stable/latest 및 독립 재다운로드 검증이 완료된 공식 릴리즈**다.

## 주요 제품 변경

상세 사용자 변경점은 `docs/RELEASE_NOTES_V1.5.0.md`를 기준으로 한다.

핵심 범위:

- Scanner market/mapped presentation 신뢰성 보강
- Quest task-pool 최신 live-data 감사 및 GameMode-aware fail-closed compatibility
- 일반 Game Data update와 Scanner catalog/market refresh 통합
- 사용자 OCR substitution 설정
- candidate 기반 Ground Truth correction + manual fallback
- Scanner stage latency telemetry
- same-cycle exact OCR bitmap reuse
- continuous result stabilization
- automatic diagnostic/log retention
- Scanner 일반 UI / 설정 / 고급·진단 분리
- Mini Scanner 빠른 현재 결과 교정
- 전체 UI consistency audit 및 Main 최소 폭 교정

## 유지한 Scanner 안전 계약

v1.5.0은 정확도를 희생하는 threshold 완화 릴리즈가 아니다.

- false positive보다 miss 선호
- geometry는 proposal이며 identity proof가 아님
- `HEADER_FRAME_LOCKED >= 0.68`
- magnifier + red close-X 필수
- structural floor `0.34`
- continuous max 8 / one-shot max 12 candidates
- current official Korean Tarkov item catalog가 identity authority
- production OCR field는 item-name 하나
- price / slots / needed는 Item ID 이후 local mapped data
- scan-time network 없음
- game memory read / DLL injection / packet interception 없음
- automatic global forced r/0/Korean substitution table 없음

## 릴리즈 후 운영

완료된 one-shot release controller와 public verifier workflow는 저장소에서 제거한다. 정상 steady-state CI는 `.github/workflows/ci.yml`을 유지한다.

새 Scanner 개선은 실제 reviewed Ground Truth와 regression evidence를 기준으로 수행하며, 공개 v1.5.0의 정상 Case가 REGRESSION이 되지 않는 것을 우선한다.
