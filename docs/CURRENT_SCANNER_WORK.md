# Current Scanner Work

기준일: 2026-08-26
상태: **FEATURE COMPLETE / MAINTENANCE ONLY / v1.7.9 PUBLIC STABLE**

## 최종 결론

Scanner 기능 개발 단계는 종료됐다. 현재 기본 운영 모드는 **유지보수 전용**이다.

- v1.7.6: 일부 문제 데스크탑의 5~13초 장시간 인식 지연 root cause 해결
- v1.7.7: 자동 교정 Case 폭증·반복 로그·단축키 정책 불일치 수정
- v1.7.8: 실제 레이드 reviewed Ground Truth에서 확인된 inspect-header ownership 회귀 수정
- v1.7.9: 인식 성공 후 Mini Scanner만 열리지 않던 presentation 회귀 수정

새로운 실제 회귀 증거가 없는 한 threshold, candidate cap, OCR variant, matcher 또는 visual acceptance를 성능/정확도 목적으로 선제 조정하지 않는다.

## 현재 Public stable

```text
version: v1.7.9
exact release source: bbb04e02385026eba6c77ba0a9d66bad9868cc92
main CI run: 32971976531
release workflow run: 32972267012
release id: 377149426
asset: Junhyun-Helper.zip
asset id: 530823055
bytes: 80,468,715
SHA-256: bd9285f7d8f819a1cf7f161f72baaae1c32a68f5db2e6f9a305053bbf3852946
published: 2026-08-26 KST
PR: #190
```

GitHub `releases/latest` readback에서 v1.7.9가 draft=false, prerelease=false, latest stable이며 tag target이 위 exact release source와 일치함을 확인했다.

상세 공개 증거:

- `docs/RELEASE_1.7.9.md`
- `docs/.release-v1.7.9-status.json`
- `docs/RELEASE_NOTES_V1.7.9.md`
- `docs/DECISION_V1.7.9_MINI_SCANNER_SHOW_2026-08-26.md`

## v1.7.9 Mini Scanner presentation 회귀

사용자 실사용 증상:

```text
Scanner recognition log = success
Mini Scanner window = not shown
```

Scanner runtime은 Item ID를 정상 확정하고 `MiniScannerOverlayService.Show(snapshot)`까지 호출했다.

문제는 hidden Mini Scanner가 첫 표시 직전에 별도 inventory/stash top-band OCR을 다시 실행한 것이다. 이 보조 OCR이 `장비`, `건강상태`, `스킬`, `지도`, `종합정보` 계열 중 2개 이상을 읽지 못하면 이미 확정된 Item 결과도 표시하지 않았다.

따라서 recognition failure가 아니라 **presentation-only failure**였다.

### 현재 계약

```text
Scanner semantic success
→ Item ID 확정
→ presentation snapshot 생성
→ Mini Scanner
   ├─ preview/display-test: show
   ├─ already visible: authoritative Item result로 즉시 update
   └─ hidden real Scanner:
        Tarkov foreground yes → show
        Tarkov foreground no  → fail closed / hidden
```

Auxiliary inventory-header OCR은 더 이상 Mini Scanner 표시 권한을 가지지 않는다.

다른 앱 위에 갑자기 Mini Scanner가 나타나는 것을 막기 위해 real Scanner hidden initial show는 실제 `EscapeFromTarkov` window가 foreground인지 확인한다.

## Mini Scanner sticky presentation

v1.7.2 계약을 유지한다.

```text
No Item
  └─ A 확정 → Show A

Show A
  ├─ A 재확정 → A 유지 / miss budget reset
  ├─ B 확정 → 즉시 B로 교체 / reset
  ├─ 실제 miss #1 → A 유지
  ├─ 실제 miss #2 → A 유지
  └─ 실제 miss #3 → Hide
```

Candidate 안정화, title change 확인, OCR 진행 같은 progress-only 상태는 miss로 세지 않는다.

## v1.7.8 레이드 header 유지 계약

레이드 인벤토리의 neutral horizontal line이 inspect header와 이어지며 header-left ownership이 실제 상세창보다 47~132px 왼쪽으로 확장되던 회귀는 v1.7.8에서 해결됐다.

Recovery order:

```text
primary header lock
→ live Ground Truth recovery
→ raid ownership recovery
→ contained-subpanel recovery
→ fail closed
```

Raid recovery는 `RED_X_CANDIDATE >= 0.90`에서만 사용하며 기존 red close-X, magnifier, neutral header, dark title field, title text evidence와 최종 `HEADER_FRAME_LOCKED >= 0.68`을 모두 요구한다.

## v1.7.7 저장·로그·단축키 유지 계약

정상 Scanner monitoring은 durable automatic diagnostic Case를 만들지 않는다.

```text
runtime capture / recognition
→ latest exact diagnostic frame in memory
→ bounded runtime text log
→ user explicitly opens correction
→ user explicitly saves
→ reviewed durable Ground Truth
```

Legacy automatic Case는 `retention=automatic_sample` + `review_status=unreviewed`, 5분 recent-write safety window, pre-delete state recheck를 모두 통과할 때만 background cleanup한다.

Reviewed/manual/corrupt/unknown/state-changed Case는 preserve fail closed한다.

Scanner activity 동일 실패는 30초 동안 collapse한다.

Scanner와 configurable Map actions는 `primary key + optional Ctrl/Alt/Shift` 공통 gesture contract를 사용한다.

## v1.7.6 성능 기준선

문제 PC 실제 Tarkov 성공 12건의 `ReadingTitle → ShowingItem`:

```text
minimum  38.07 ms
median   63.92 ms
maximum   1.05 s
mean     211.47 ms
```

문제 PC Display Test:

```text
하프 마스크: 10,840.877 ms → 70.603 ms
USB 보안 플래시 드라이브: 12,686.278 ms → 1,354.775 ms
```

같은 active latency cycle의 동일 title pixels + OCR text에 대한 visual corroboration 결과만 재사용하며 cycle이 바뀌면 폐기한다. 이는 cross-frame identity cache가 아니다.

## Scanner UI — 현재

일반 Scanner 화면 상단:

```text
스캐너 ON/OFF
설정
고급
현재 결과 교정
```

`현재 결과 교정`은 메모리에 보존된 최신 exact Scanner frame을 기존 교정 창으로 연다.

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

## v1.7.9 CI / release proof

PR #190 final HEAD:

```text
971c27a40566d01651cf14af0f519ceb68c3515a
PR CI run 32971624200: SUCCESS
```

검증:

- Desktop Release build SUCCESS
- 380 passed / 0 failed / 0 skipped
- Windows x64 self-contained publish SUCCESS
- Product UI / Scanner / Map / Factory / MiniMap smoke SUCCESS
- Mini Scanner confirmed-item initial visibility policy smoke SUCCESS
- graceful shutdown SUCCESS
- release package verification SUCCESS
- artifact upload SUCCESS

Final merged release source:

```text
bbb04e02385026eba6c77ba0a9d66bad9868cc92
main CI run 32971976531: SUCCESS
release workflow run 32972267012: SUCCESS
release id 377149426
```

Public asset:

```text
Junhyun-Helper.zip
asset id 530823055
bytes 80,468,715
SHA-256 bd9285f7d8f819a1cf7f161f72baaae1c32a68f5db2e6f9a305053bbf3852946
```

## Scanner 유지보수 원칙

앞으로 Scanner 작업은 다음 조건 중 하나가 실제로 발생했을 때만 시작한다.

1. 실사용에서 새로운 miss/wrong identity/performance/presentation regression이 확인됨
2. Tarkov UI/데이터 변경으로 기존 계약이 깨짐
3. Windows/.NET/platform 변화로 runtime compatibility 문제가 생김
4. 사용자가 새로운 Scanner 제품 요구사항을 명시적으로 결정함
5. 보안 또는 데이터 무결성 문제가 확인됨

문제가 생기면 evidence → failure stage → affected layer only → regression/smoke → full Windows CI/publish/package → PATCH 공개 → release readback 순서로 처리한다.
