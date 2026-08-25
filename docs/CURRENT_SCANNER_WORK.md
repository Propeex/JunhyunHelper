# Current Scanner Work

기준일: 2026-08-26
상태: **FEATURE COMPLETE / MAINTENANCE ONLY / v1.7.8 PUBLIC STABLE**

## 최종 결론

Scanner 기능 개발 단계는 종료됐다. 현재 기본 운영 모드는 **유지보수 전용**이다.

v1.7.6에서 일부 문제 데스크탑의 5~13초 장시간 인식 지연을 실측 자료로 해결했고, v1.7.7에서 자동 교정 Case 폭증·반복 로그·단축키 불일치를 수정했으며, v1.7.8에서 실제 레이드 reviewed Ground Truth로 확인된 inspect-header ownership 회귀를 수정했다.

새로운 실제 회귀 증거가 없는 한 threshold, candidate cap, OCR variant, matcher 또는 visual acceptance를 성능/정확도 목적으로 선제 조정하지 않는다.

## 현재 Public stable

```text
version: v1.7.8
exact release source: 3ba9d99c43ad143dbc8329e7d29b1d01da335b06
main CI run: 32888653630
release workflow run: 32888935292
release id: 376650517
asset: Junhyun-Helper.zip
asset id: 529666832
bytes: 80,469,671
SHA-256: 3716d2d3c6d3c9ce2f87c759aac74f6b56b483a09016339c0d8bb6d3bc67e730
published: 2026-08-26 KST
PR: #188
```

GitHub `releases/latest` readback에서 v1.7.8이 draft=false, prerelease=false, latest stable이며 tag target이 위 exact release source와 일치함을 확인했다.

상세 공개 증거:

- `docs/RELEASE_1.7.8.md`
- `docs/.release-v1.7.8-status.json`
- `docs/RELEASE_NOTES_V1.7.8.md`

## v1.7.6 성능 기준선

v1.7.5까지 문제 데스크탑에서 재현되던 Scanner 장시간 인식 지연의 대표 actual Tarkov cycle:

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

Windows OCR backend가 원인이 아니었다. 동일 current-frame title bitmap/OCR text를 공유하는 구조 후보에서 targeted + full-catalog visual corroboration이 반복되며 동일 visual evidence를 여러 번 계산한 것이 주 원인이었다.

v1.7.6은 같은 Scanner latency cycle에서 다음 값이 모두 동일한 visual corroboration result만 재사용한다.

- cycle ID
- title bitmap width/height
- exact current-pixel SHA-256
- OCR text

cycle이 바뀌면 즉시 폐기한다. 이는 cross-frame identity cache가 아니다.

문제 PC 재검증:

```text
하프 마스크
10,840.877 ms → 70.603 ms
약 99.35% 감소

USB 보안 플래시 드라이브
12,686.278 ms → 1,354.775 ms
약 89.32% 감소
```

실제 Tarkov 성공 12건의 `ReadingTitle → ShowingItem`:

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

사용자 실사용에서도 수정 후 반응성이 충분한 수준임을 확인했다. 약 1초의 어려운 deep/recovery 사례는 bounded recovery cost로 허용하며 성능 수치만 더 낮추기 위해 인식 기준을 완화하지 않는다.

## v1.7.7 저장·로그·단축키 유지보수

사용자 support 자료에서 Scanner 교정 데이터가 7GB 이상 증가한 문제를 분석한 결과, 자동 저장된 51개 Case가 모두 `UNREVIEWED / automatic_sample`이었다.

현재 계약:

```text
runtime capture / recognition
→ latest exact diagnostic frame in memory
→ bounded runtime text log
→ user explicitly opens correction
→ user explicitly saves
→ reviewed durable Ground Truth
```

정상 monitoring은 상세창 미탐지, header/OCR/matcher 실패, ambiguity 또는 반복 실패만으로 durable Case를 만들지 않는다.

이전 버전 legacy automatic Case는 다음을 모두 증명할 때만 background cleanup한다.

```text
retention = automatic_sample
review_status = unreviewed
recent-write safety window = 5 minutes
pre-delete metadata/state re-read = unchanged
```

reviewed/manual/corrupt/unknown/state-changed Case는 preserve fail closed한다.

사용자 activity feed의 동일 실패는 30초 동안 collapse하며, 지원용 `scanner.log`는 작은 bounded rotation을 유지한다. Text log lifetime과 Ground Truth lifetime은 분리한다.

Scanner와 configurable Map actions는 `primary key + optional Ctrl/Alt/Shift` 공통 gesture contract를 사용한다. Bare key도 허용하고 Windows key modifier는 지원하지 않는다. Map의 bare NumPad0~5 직접 층 선택은 유지하며 modifier+NumPad는 configurable action으로 사용할 수 있다.

공식 결정: `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`.

## v1.7.8 레이드 header 회귀

사용자가 직접 검토한 8개 Scanner Case를 분석했다.

```text
reviewed cases: 8
actual failures: 6
program-correct cases: 2
```

실패 6건 모두:

- detail rectangle proposal 정상
- item-name ROI proposal 정상
- 실제 full image에 red close-X 존재
- 실제 full image에 magnifier 존재
- runtime close/magnifier = null
- header reason = HEADER_CLOSE_NOT_LOCKED
- recognition reason = TITLE_ANCHOR_INCOMPLETE
- raw OCR = empty

따라서 OCR 오인식이 아니라 **OCR 이전 inspect-header semantic lock 실패**였다.

레이드 인벤토리의 neutral horizontal line이 inspect header와 같은 높이에서 이어져 기존 live fallback이 header left를 실제 상세창보다 왼쪽으로 소유했다.

reviewed 실패 Case의 left drift:

```text
-132 px
 -84 px
-125 px
-125 px
-125 px
 -47 px
```

잘못된 header-left를 기준으로 magnifier lane까지 왼쪽으로 밀려 실제 돋보기를 놓친 것이 root cause다.

### v1.7.8 수정

기존 정상 경로의 우선순위와 의미는 유지한다.

```text
primary header lock
→ live Ground Truth recovery
→ v1.7.8 raid ownership recovery
→ contained-subpanel recovery
→ fail closed
```

raid recovery 진입은 다음으로 제한한다.

```text
candidate reason = RED_X_CANDIDATE
structural score >= 0.90
```

coarse geometry는 header-left ownership proposal로만 사용하며 Item identity proof가 아니다.

독립적으로 다시 요구하는 evidence:

```text
close-X template >= 0.40
close relation evidence >= 0.60
candidate-owned neutral header >= 0.74
magnifier template >= 0.54
magnifier relation evidence >= 0.66
dark title field >= 0.58
title text evidence >= 0.22
final HEADER_FRAME_LOCKED >= 0.68
```

사용자 reviewed 8 Case의 픽셀 evidence를 수정된 ownership 모델로 대조했을 때 모두 기존 final semantic floor 0.68을 넘었다. 사용자 Ground Truth 이미지는 공개 저장소/CI artifact에 포함하지 않는다.

CI에는 raid horizontal bleed positive smoke와 red close-X가 없는 경우 반드시 거부하는 negative smoke를 추가했다.

공식 결정: `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`.

## Scanner UI — 현재

일반 Scanner 화면 상단:

```text
스캐너 ON/OFF
설정
고급
현재 결과 교정
```

`현재 결과 교정`은 메모리에 보존된 최신 exact Scanner frame을 바로 기존 교정 창으로 연다.

`고급`에는 다음만 둔다.

- 테스트 스캐너 / Display Test
- 교정 데이터 관리
- Scanner 성능 진단 자료 내보내기

## 정확도·안전 불변식

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
deep OCR candidate limit = existing value
continuous observation target = 200 ms
```

- false positive보다 miss 우선
- current official Tarkov catalog가 identity authority
- stale Item ID current identity proof 금지
- cross-frame OCR/visual identity cache 금지
- matcher / targeted visual / full-catalog visual acceptance 완화 없음
- Item ID 확정 전 price/needed mapped data를 identity proof로 사용 금지
- scan-time network 없음
- game memory read / DLL injection / packet interception / process hook 없음
- reviewed Ground Truth 없이 threshold/candidate cap 완화 금지

## v1.7.8 CI / release proof

PR #188 final HEAD:

```text
52fbeaf6d56cf01631325ba3d65a1f018e9eb138
PR CI run 32886379050: SUCCESS
```

검증:

- Desktop Release build SUCCESS
- 380 passed / 0 failed / 0 skipped
- Windows x64 self-contained publish SUCCESS
- Product UI / Scanner / Map / Factory / MiniMap smoke SUCCESS
- graceful shutdown SUCCESS
- release package verification SUCCESS
- artifact upload SUCCESS

Final merged release source:

```text
3ba9d99c43ad143dbc8329e7d29b1d01da335b06
main CI run 32888653630: SUCCESS
release workflow run 32888935292: SUCCESS
release id 376650517
```

Public asset:

```text
Junhyun-Helper.zip
asset id 529666832
bytes 80,469,671
SHA-256 3716d2d3c6d3c9ce2f87c759aac74f6b56b483a09016339c0d8bb6d3bc67e730
```

## Scanner 유지보수 원칙

앞으로 Scanner 작업은 다음 조건 중 하나가 실제로 발생했을 때만 시작한다.

1. 실사용에서 새로운 miss/wrong identity/performance regression이 확인됨
2. Tarkov UI/데이터 변경으로 기존 인식 계약이 깨짐
3. Windows/.NET/platform 변화로 runtime compatibility 문제가 생김
4. 사용자가 새로운 Scanner 제품 요구사항을 명시적으로 결정함
5. 보안 또는 데이터 무결성 문제가 확인됨

문제가 생기면:

1. exact Case/support bundle 확보
2. failure stage 측정
3. affected stage만 수정
4. reviewed Ground Truth가 있으면 full replay에서 `REGRESSION=0` 확인
5. 전체 Windows CI/publish/smoke/package gate 통과
6. PATCH 공개 후 release readback 검증

성능 수치를 더 낮추기 위한 선제적 Scanner 구조 변경, 추측 기반 threshold/candidate-cap 완화, 코드 미관만을 위한 위험한 대규모 refactor는 하지 않는다.
