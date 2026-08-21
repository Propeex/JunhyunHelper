# RELEASE 1.1.0 — Scanner

기준일: 2026-08-21

상태: **`RELEASE CANDIDATE`**

## 목적

v1.1.0은 v1.0.0의 안정 기능을 유지하면서 실제 Scanner + Mini Scanner를 추가하는 MINOR release입니다.

버전 근거:

- `docs/VERSIONING.md`
- DEC-048: 새 사용자 기능 = MINOR +1
- v1.0.0 → Scanner 실제 기능 추가 = v1.1.0

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

Tarkov 전체 screenshot을 바탕화면/이미지 뷰어에 띄워 게임 없이 확인할 수 있습니다.

## 안전 경계

사용하지 않음:

- game memory read
- DLL injection
- packet interception
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

기존 사용자 진행/설정은 유지됩니다.

## 공개 전 release gate

- [ ] final PR Windows Release build
- [ ] full automated tests
- [ ] Scanner detector/catalog/matcher/persistence regression
- [ ] win-x64 self-contained single-file publish
- [ ] ProductVersion/FIRST_RUN v1.1.0 identity
- [ ] package root/PDB/nested archive/dependency audit
- [ ] actual published EXE rendered Product UI smoke
- [ ] Scanner `스캐너 OFF` / `테스트 OFF` rendered safe-default controls
- [ ] Main Map / Factory / MiniMap smoke
- [ ] graceful shutdown
- [ ] main merge / exact release SHA fixed
- [ ] independent release workflow build/tests/publish/smoke
- [ ] Draft ZIP + SHA256SUMS 생성
- [ ] Draft assets 재다운로드/hash/package/ProductVersion/FIRST_RUN 검증
- [ ] public/latest 전환
- [ ] public assets 재다운로드 검증
- [ ] public downloaded EXE smoke
- [ ] one-shot release workflow 제거
- [ ] final SHA/hash/run 기록

## live Tarkov 검증

사용자가 2026-08-21 결정한 대로 **최신 Tarkov Borderless 인게임 E2E는 위 public release gate에 포함하지 않습니다.**

공개 시점 상태:

```text
implementation: IMPLEMENTED
Windows build/package: VERIFIED after gates
offline screenshot/OCR experiment: VERIFIED
latest live Tarkov Borderless E2E: PENDING
```

공개 후 실제 게임에서 확인할 내용:

- window capture route
- current detail geometry calibration
- Korean title OCR quality
- Item confidence/margin
- false positives / misses
- long-run resource behavior
- Alt+Tab/minimize/MiniMap coexistence

문제가 있으면 `scanner.log`를 기준으로 보정하고 PATCH release로 배포합니다.

## 최종 공개 기록

릴리즈 완료 후 아래 값을 이 문서에 기록합니다.

```text
release source SHA: PENDING
release workflow: PENDING
automated tests: PENDING
asset: Junhyun-Helper-v1.1.0-win-x64.zip
bytes: PENDING
SHA-256: PENDING
ProductVersion: PENDING
public downloaded EXE smoke: PENDING
```
