# RELEASE 1.1.0 — Scanner

기준일: 2026-08-21

상태: **`PUBLIC RELEASE / VERIFIED`**

## 목적

v1.1.0은 v1.0.0의 안정 기능을 유지하면서 실제 Scanner + Mini Scanner를 추가하는 MINOR release입니다.

버전 근거:

- `docs/VERSIONING.md`
- DEC-048: 새 사용자 기능 = MINOR +1
- v1.0.0 → Scanner 실제 기능 추가 = v1.1.0

## 최종 공개 기준선

```text
release: v1.1.0
release id: 374188781
exact release source / target SHA: ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
asset: Junhyun-Helper-v1.1.0-win-x64.zip
bytes: 80,235,043
SHA-256: 8e7f452701f866c84e753c1c34951af64f4415947e9f56c56634e2b584d9e1ce
ProductVersion: 1.1.0+ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
automated tests: 243 passed / 0 failed / 0 skipped
public downloaded EXE smoke: SUCCESS
latest stable: v1.1.0
```

Public verification workflow:

```text
run: 32452416929
release gates through public downloaded EXE smoke: SUCCESS
```

해당 run의 GitHub Actions 최종 conclusion은 마지막 **PR 코멘트 기록 단계의 권한 403** 때문에 `failure`로 표시됐습니다. 그러나 그 단계는 release artifact나 제품 검증과 무관한 bookkeeping 단계입니다. 그 이전의 다음 release gate는 전부 성공했습니다.

- existing Draft release inspect/download
- release target / project version 검증
- ZIP checksum / size / package root / PDB / nested archive 검증
- ProductVersion ↔ exact target SHA 검증
- FIRST_RUN identity 검증
- Draft-downloaded 실제 EXE Product UI + Scanner + Map/MiniMap smoke
- Draft → public/latest 전환
- public/latest release metadata 검증
- public asset 재다운로드
- public hash / size / ProductVersion / FIRST_RUN 재검증
- public-downloaded 실제 EXE smoke

따라서 마지막 comment-only 403은 release 검증 결과를 무효화하지 않습니다.

## 사용자 기능

### 실사용 Scanner

```text
스캐너 ON
→ EscapeFromTarkov Borderless client-area
→ detail detector
→ title ROI
→ Windows ko-KR OCR
→ full-item conservative matcher
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

### 테스트 Scanner

```text
테스트 ON
→ 모든 연결 디스플레이 실시간 capture
→ 동일 detector/OCR/matcher pipeline
```

Tarkov 전체 screenshot을 바탕화면/이미지 뷰어에 띄워 게임 없이 같은 recognition pipeline을 확인할 수 있습니다.

## 안전 경계

사용하지 않음:

- game memory read
- DLL injection
- packet interception
- process-internal game data read
- icon identity
- scan-time network

identity가 확실하지 않으면 Item ID를 강제 선택하지 않습니다.

## 진단

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

state/candidate/OCR/matcher metadata를 기록합니다. screenshot/raw pixels는 저장하지 않습니다.

## 호환성

```text
Desktop Version: 1.1.0
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db schema: v1
v1.0.0 → v1.1.0 mandatory Game Content update: none
v1.0.0 → v1.1.0 user.db migration: none
```

기존 Profile / Quest / Inventory / Hideout / Map preferences / Ammo favorites는 유지됩니다.

## 완료된 release gate

- [x] final Scanner PR Windows Release build
- [x] full automated tests — 243 passed / 0 failed / 0 skipped
- [x] Scanner detector/catalog/matcher/persistence regression
- [x] win-x64 self-contained single-file publish
- [x] ProductVersion/FIRST_RUN v1.1.0 identity
- [x] package root/PDB/nested archive/dependency audit
- [x] actual published EXE rendered Product UI smoke
- [x] Scanner `스캐너 OFF` / `테스트 OFF` rendered safe-default controls
- [x] Main Map / Factory / MiniMap smoke
- [x] graceful shutdown
- [x] exact release source fixed: `ac24f7717e81cf6fa32cb2e0ade63949ed87ade5`
- [x] Draft ZIP + SHA256SUMS 생성
- [x] Draft assets 재다운로드/hash/package/ProductVersion/FIRST_RUN 검증
- [x] Draft-downloaded EXE smoke
- [x] public/latest 전환
- [x] public assets 재다운로드 검증
- [x] public downloaded EXE smoke
- [x] one-shot release/dispatcher workflow cleanup prepared
- [x] final SHA/hash/public verification 기록

## release pipeline 복구 기록

최초 release source는 Scanner PR #108의 squash merge SHA `ac24f7717e81cf6fa32cb2e0ade63949ed87ade5`입니다. 이 source로 생성된 Draft release가 존재했으나, 후속 재실행 run `32452079264`는 Draft가 이미 존재한다는 안전 장치 때문에 Draft 생성 단계에서 중단됐습니다.

그 run에서도 별도로 다음이 성공했습니다.

```text
release source checked by that rerun: df7a5aa3473447bcf0d8a42a34b39cdbd8eb9047
Release build: SUCCESS
243 automated tests: SUCCESS
publish/package audit: SUCCESS
exact published EXE smoke: SUCCESS
rebuild ZIP SHA-256: 79d669190ef9285dfea1787dc7d260e06f408644f295d20fa4b1ef77c46b1ebf
rebuild ZIP bytes: 80,235,016
```

이 재빌드 자산은 **공개 자산이 아닙니다.** 공개 자산은 이미 존재하던 exact `ac24f771...` Draft를 검증한 뒤 공개한 것이며, 권위 public hash/size는 이 문서 상단의 `8e7f4527... / 80,235,043 bytes`입니다.

## live Tarkov 검증

사용자가 2026-08-21 결정한 대로 **최신 Tarkov Borderless 인게임 E2E는 public release gate에 포함하지 않았습니다.**

공개 시점 상태:

```text
implementation: IMPLEMENTED
Windows build/package: VERIFIED
offline screenshot/OCR experiment: VERIFIED
latest live Tarkov Borderless E2E: PENDING
```

공개 후 실제 게임에서 확인할 내용:

- target-window capture route / fallback
- current detail geometry calibration
- Korean title OCR quality
- Item confidence/margin
- false positives / misses
- long-run CPU/memory/handle/OCR rate
- Alt+Tab/minimize/MiniMap coexistence

문제가 있으면 `scanner.log`를 기준으로 보정하고 새 기능 추가가 없는 한 PATCH release로 배포합니다.
