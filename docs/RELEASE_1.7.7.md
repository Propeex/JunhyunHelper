# 준현 헬퍼 v1.7.7 공개 릴리즈 기록

기준일: 2026-08-26 KST  
상태: **PUBLIC STABLE / VERIFIED**

## 공개 결과

```text
version: v1.7.7
release source: b6deaaa900daa94113737f6cc8dd1cf8fcef60c8
main CI run: 32879402260
release workflow run: 32879713326
release id: 376595527
asset: Junhyun-Helper.zip
asset id: 529560085
asset bytes: 80,463,825
asset SHA-256: eab46695362bc9d1e656fb954694a681dd95066dae5210f2498387b14c163f5b
checksum asset: SHA256SUMS.txt
checksum asset id: 529560086
published UTC: 2026-08-25T17:45:29Z
```

GitHub readback에서 다음을 확인했다.

- `v1.7.7` tag target = exact release source `b6deaaa900daa94113737f6cc8dd1cf8fcef60c8`
- draft = false
- prerelease = false
- GitHub `releases/latest` = v1.7.7
- `Junhyun-Helper.zip`과 `SHA256SUMS.txt`가 모두 존재
- ZIP GitHub asset digest = `sha256:eab46695362bc9d1e656fb954694a681dd95066dae5210f2498387b14c163f5b`

## 릴리즈 목적

v1.7.6 이후 실사용에서 확인된 다음 유지보수 결함을 수정했다.

1. Scanner 연속 감시가 상세보기 창 미탐지/인식 실패 프레임을 PNG 포함 자동 Case로 지속 저장해 교정 데이터가 GB 단위로 증가할 수 있었음
2. 동일 Scanner 실패가 사용자 활동 로그에 반복되어 필요한 기록의 가시성이 낮았음
3. Scanner는 modifier 조합을 강제하고 Map은 modifier 조합을 허용하지 않아 단축키 입력 규칙이 상충했음

사용자 support 자료의 diagnostic Case 51개는 모두 `UNREVIEWED / automatic_sample`이었고, 이 자동 자료가 7GB 이상 증가 문제의 원인으로 확인됐다.

## 확정된 제품 동작

### Scanner correction / Ground Truth

정상 연속 Scanner는 더 이상 durable automatic Case를 만들지 않는다.

```text
runtime capture / recognition
→ latest exact diagnostic frame in memory
→ bounded runtime text log
→ user explicitly opens correction
→ user explicitly saves
→ reviewed durable Ground Truth
```

상세창 없음, header/OCR/matcher 실패, ambiguity, 반복 실패만으로 correction dataset이 증가하지 않는다.

이전 버전의 legacy Case는 `retention=automatic_sample` 및 `review_status=unreviewed`를 모두 증명하고 최근 쓰기 중이 아님을 확인한 경우에만 background cleanup한다. 삭제 직전 상태를 다시 확인하며 reviewed/manual/corrupt/unknown Case는 자동 삭제하지 않는다.

### Scanner log

동일 실패는 사용자 activity feed에서 30초 window로 collapse한다. 지원 분석용 `scanner.log`는 기존 bounded rotation/retention을 유지한다.

### Hotkeys

Scanner와 configurable Map actions는 공통으로 다음 gesture를 지원한다.

```text
primary non-modifier key
+ optional Ctrl
+ optional Alt
+ optional Shift
```

bare key도 허용한다. Windows modifier는 지원하지 않는다.

기존 Map key-only 설정은 modifier `None`으로 호환된다. 완전히 같은 gesture만 충돌하며 primary key가 같아도 modifier 조합이 다르면 서로 다른 binding이다.

bare NumPad0~5는 기존 직접 층 선택에 예약하고, modifier가 붙은 NumPad gesture는 configurable Map action으로 사용할 수 있다.

## 인식 알고리즘 불변

이번 PATCH에서 Scanner recognition 판단 기준은 변경하지 않았다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

OCR variants, catalog matcher acceptance, visual corroboration/recovery acceptance, stale identity 금지, cross-frame identity proof 금지, false-positive보다 miss 우선 계약을 유지한다.

## 검증

PR 최종 HEAD에서 Windows Release build, 전체 380 tests, win-x64 self-contained publish, Product UI/Scanner/Map/Factory/MiniMap smoke, graceful shutdown, package verification을 통과했다.

병합 후 exact release source `b6deaaa900daa94113737f6cc8dd1cf8fcef60c8`에서 main CI `32879402260`이 다시 동일 gate를 통과했다. 성공한 main CI artifact만 Release workflow `32879713326`이 받아 ProductVersion/FIRST_RUN/checksum identity를 검증한 뒤 v1.7.7을 공개했다.

공식 설계 결정은 `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`, 사용자 변경 설명은 `docs/RELEASE_NOTES_V1.7.7.md`, machine-readable 공개 상태는 `docs/.release-v1.7.7-status.json`을 기준으로 한다.
