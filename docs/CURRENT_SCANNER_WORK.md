# Current Scanner Work

기준일: 2026-08-26
상태: **P0 RESOLVED / v1.7.6 PUBLIC STABLE / LIVE MAINTENANCE**

## 최종 결론

v1.7.5까지 문제 데스크탑에서 재현되던 Scanner 5~13초 장시간 인식 지연은 v1.7.6에서 해결됐다.

사용자 실사용 평가와 두 번의 support bundle이 동일한 결론을 지지한다. 성능 알고리즘은 더 수정하지 않는다. 새로운 실측 regression이 생기기 전에는 threshold, candidate cap, OCR variant 또는 visual acceptance를 성능 목적으로 조정하지 않는다.

## Public stable

```text
version: v1.7.6
exact release source: 0e5240620ca0867a93f426824ff03374b93dcd1a
release CI run: 32868778549
release workflow run: 32869081513
asset: Junhyun-Helper.zip
bytes: 80,462,038
SHA-256: 1de4e203c7e219f1d995d4482fa903dc7544d208deee684b5b821f6b5c325e35
published: 2026-08-26 KST
PR: #185
```

Release/latest readback에서 v1.7.6이 draft=false, prerelease=false, latest stable이며 target commit이 위 exact release source와 일치함을 확인했다.

## Root cause

첫 문제-PC diagnostic bundle의 대표 actual Tarkov cycle:

```text
end-to-end                  12,540.77 ms
OCR normal                      12.26 ms
actual WinRT RecognizeAsync     10.57 ms
visual recovery             12,306.61 ms / 16 calls
catalog matching                75.16 ms
capture                         21.57 ms
rectangle proposal              53.57 ms
semantic header                 53.51 ms
```

Windows OCR backend가 원인이 아니었다. 동일 current-frame title bitmap/OCR text를 공유하는 구조 후보 8개가 targeted + full-catalog visual corroboration을 각각 반복하면서 동일 visual evidence를 16회 계산한 것이 장시간 지연의 주 원인이었다.

`TarkovTitleFontProvider`도 unavailable retry check 전에 expensive process/source discovery를 수행할 수 있어 optional font discovery가 hot path에서 반복될 가능성이 있었다.

WPF dispatcher stall과 LocalAppData diagnostic file I/O는 문제 PC에서 primary root cause가 아니었다.

## v1.7.6 fix

### Current-cycle exact visual evidence reuse

같은 Scanner latency cycle에서 다음 값이 모두 동일한 visual corroboration result는 한 번 계산한 결과를 재사용한다.

- cycle ID
- title bitmap width/height
- exact current-pixel SHA-256
- OCR text

cycle이 바뀌면 즉시 폐기한다. 이는 cross-frame identity cache가 아니며 동일한 현재-frame deterministic proof의 중복 계산만 제거한다.

### Tarkov font provider hot-path protection

- unavailable retry를 expensive source/process discovery 전에 확인
- failed/unavailable source attempt 30초 retry suppression
- 확인된 `resources.assets` path process-local reuse
- loaded generation live source validation 5초 cadence
- source length/timestamp change invalidation 및 re-extraction safety 유지

### 기존 hardening 유지

- actual `OcrEngine.RecognizeAsync` call별 timing
- actual slow-empty WinRT circuit breaker
- serialized OCR semaphore/image-key/preprocessing timing
- one-shot scan worker dispatch
- WPF dispatcher responsiveness probe
- one-click Scanner support bundle export

## 문제 PC 재검증

### 동일 Display Test

```text
하프 마스크
before 10,840.877 ms
 after     70.603 ms
약 99.35% 감소

USB 보안 플래시 드라이브
before 12,686.278 ms
 after  1,354.775 ms
약 89.32% 감소
```

추가 Display Test:

```text
Maska-1SCh           106.619 ms
Domontovich 우샨카     88.190 ms
Wires 전선            100.802 ms
PSU 전원공급장치        48.123 ms
```

USB 사례는 corrupted normal OCR 뒤 deep/recovery가 필요한 어려운 사례다. 약 1초 수준의 bounded recovery는 허용하며 기존 5~13초 serial amplification과 구분한다.

### Actual Tarkov

성공 12건의 `ReadingTitle → ShowingItem`:

```text
minimum  38.07 ms
median   63.92 ms
maximum   1.05 s
mean     211.47 ms
```

retained OCR-active full Scanner cycle 11건:

```text
minimum end-to-end 178.04 ms
median             210.82 ms
maximum            517.74 ms
```

추가 evidence:

```text
visual-cycle-cache-hit: 73
repeated visual-recovery: effectively 0~0.01 ms
WPF dispatcher stall: 0
actual WinRT OCR: generally ~4~13 ms
ScannerDiagnosticLogWriteProbeMs: 0.30 ms
DiagnosticFileAppendAverageMs: 0.14 ms
DiagnosticFileAppendMaximumMs: 0.25 ms
```

사용자는 fixed candidate를 `엄청 괜찮아졌다`고 평가했다.

## 정확도·안전 불변식

변경 없음:

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
deep OCR candidate limit = existing value
```

- false positive보다 miss 우선
- current official Tarkov catalog가 identity authority
- stale Item ID current identity proof 금지
- cross-frame OCR/visual identity cache 금지
- matcher / targeted visual / full-catalog visual acceptance 완화 없음
- Item ID 확정 전 price/needed mapped data를 identity proof로 사용 금지
- scan-time network 없음
- game memory read / DLL injection / packet interception / process hook 없음

최근 성능 support log에 기록된 diagnostic Case는 `UNREVIEWED` 자동 Case였으며, support ZIP은 설계상 Ground Truth image/dataset을 포함하지 않는다. 따라서 존재하지 않는 reviewed baseline에 대한 regression 결과를 추정하지 않는다. Reviewed dataset이 실제 존재할 때에는 기존 full-pipeline replay의 `REGRESSION=0` 계약을 그대로 적용한다.

## CI / release proof

Root-cause fix code HEAD `d04f39697a4ea4d6ff4eabcb2acdc6bc535c8f9c`의 PR CI run `32866068233`:

- Desktop build SUCCESS
- 380 passed / 0 failed / 0 skipped
- Windows x64 publish SUCCESS
- Product UI / Map / Factory / MiniMap smoke SUCCESS
- graceful shutdown SUCCESS
- release package verification SUCCESS

Final merged main source `0e5240620ca0867a93f426824ff03374b93dcd1a`의 CI run `32868778549`도 같은 전체 gate를 SUCCESS로 통과했고, Release run `32869081513`이 그 CI artifact를 검증해 v1.7.6 stable을 게시했다.

## Known non-functional release-note issue

v1.7.6 공개 ZIP의 `FIRST_RUN_KO.txt` 본문 일부는 개발 중 작성된 `진단 후보` 표현을 그대로 포함한다.

- 첫 줄 version identity `준현 헬퍼 v1.7.6 — Windows x64`는 정확하다.
- 실행 파일/Scanner behavior/asset checksum에는 영향이 없다.
- 게시된 stable asset은 immutable 원칙에 따라 덮어쓰지 않는다.
- 다음 patch에서 사용자 안내 문구를 현재 resolved 상태에 맞게 갱신한다.

## 다음 Scanner 작업

현재 P0는 종료한다.

앞으로는:

1. 실사용에서 새로운 miss/wrong identity/performance regression이 생기면 exact Case/support bundle 확보
2. failure stage를 측정
3. affected stage만 수정
4. reviewed Ground Truth가 있으면 full replay에서 REGRESSION=0 확인
5. 추측 기반 threshold/candidate-cap 완화 금지

성능 수치를 더 낮추기 위한 선제적 Scanner 구조 변경은 하지 않는다.
