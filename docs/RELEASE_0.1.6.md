# RELEASE 0.1.6 — Quest prerequisite semantics correction

기록일: **2026-08-15**

상태: **RELEASE CANDIDATE**

## 목적

v0.1.5 이후 수행한 Quest prerequisite 감사를 제품 판정에 반영합니다.

이번 릴리즈는 일반 Quest 선행조건 모델 전체를 새로 만드는 변경이 아니라, 특수 상인 compatibility overlay가 upstream의 실제 상태 의미를 훼손하던 부분을 정정하고 향후 데이터 변경에서 조용한 오판을 막는 검증을 강화하는 릴리즈입니다.

## 1. 일반 prerequisite 의미

- 서로 다른 `taskRequirements`는 AND
- 한 requirement 내부 `status[]`는 OR
- `complete` = 완료
- `active` = 진행 상태에 도달
- `failed` = 실패
- 별도 `수주 가능` 상태를 추가하지 않음
- 게임에서 받을 수 있는 Quest는 Helper에서 즉시 수락한 것으로 간주하는 기존 `DEC-010` 유지

## 2. Upstream prerequisite 우선

compatibility overlay는 source가 이미 제공한 직접 prerequisite를 덮어쓰거나 더 강한 상태로 바꾸지 않습니다.

이 규칙으로 기존 BTR Driver 회귀를 수정합니다.

- `Shipping Delay - Part 2`의 raw prerequisite인 `A Helping Hand = Active` 보존
- BTR Driver 후속 Quest에 gate가 누락된 경우에만 `A Helping Hand = Active` 보강
- A Helping Hand 완료 이후 이미 열린 BTR Quest를 다시 잠그지 않음

## 3. Ref

- source가 직접 제공한 prerequisite 보존
- 누락된 Ref 후속 Quest에만 GameMode별 검증된 unlock Quest의 `Complete` gate 보강
- 현재 GameMode에 unlock Quest가 없으면 dangling prerequisite를 만들지 않음

## 4. Lightkeeper recoverable access

Lightkeeper는 최초 해금 후 DSP transmitter 상태에 따라 접근을 잃고 Make Amends 계열로 복구할 수 있으므로 ordinary monotonic prerequisite만으로 현재 접근권을 정확히 나타낼 수 없습니다.

v0.1.6:

- `Getting Acquainted = Complete`를 모든 후속 Quest의 영구 ordinary prerequisite로 강제하지 않음
- `QuestSpecialTraderAccessRequirement`로 별도 모델링
- 최초 접근은 Getting Acquainted 완료에서 자동 추론
- 최초 unlock이 아직 종결되지 않았을 때 수동 접근 동기화로 우회 불가
- Getting Acquainted가 완료 또는 실제 영구 실패로 종결된 뒤에만 실제 게임 접근 상실/복구를 sparse user fact로 저장 가능
- Quest 상세 화면에서 해당 특수 상황에만 `접근 상실 기록` / `접근 복구 기록` action 노출
- recoverable 접근 상실은 `Unavailable`이 아니라 `Locked`

## 5. Content 저장 호환성

```text
Desktop ProductVersion: 0.1.6
Content schema: v6
Readable Content schemas: v3, v4, v5, v6
user.db SQLite schema: v1 unchanged
v0.1.5 → v0.1.6 필수 데이터 업데이트: 없음
```

v3~v5 content snapshot은 오프라인에서도 읽을 수 있으며 읽는 시점에 legacy special-trader semantics를 메모리에서 정규화합니다.

- 과거 BTR 강제 Complete → Active compatibility gate
- 과거 Lightkeeper ordinary Getting Acquainted Complete gate → recoverable special access gate
- Ref Complete 의미 유지

다음 정상 `데이터 업데이트`가 성공하면 v6 content snapshot으로 저장됩니다.

기존 `%LocalAppData%/JunhyunHelper/user.db`의 Profile / Quest 완료·실패 / Inventory / Hideout 진행은 유지됩니다.

## 6. 데이터 검증 강화

`GameContentValidator`가 다음 Quest graph 이상을 candidate activation 전에 fatal로 차단합니다.

- 빈 prerequisite status
- self prerequisite
- 동일 prerequisite target 중복
- missing prerequisite Quest
- dependency cycle
- special trader access의 빈 status
- missing/mismatched special trader
- self unlock Quest
- missing unlock Quest
- ordinary prerequisite와 special access가 같은 unlock Quest를 중복 평가

현재 live source가 위 오류를 갖고 있다는 의미가 아니라, 향후 Tarkov 패치 데이터 변경에서 조용한 잘못된 판정을 막기 위한 방어 규칙입니다.

## 7. 기존 정확도 정책 유지

- `globalVariable` / `dialogue`처럼 프로그램이 증명할 수 없는 availability는 `확인 필요(Indeterminate)` 유지
- 실제 게임 완료 시각이 필요한 delay에 가짜 countdown을 만들지 않음
- 다른 Quest 완료로 확정되는 sibling failure는 자동 추론
- 프로그램이 알 수 없는 비재시작형 영구 실패만 사용자 입력
- 재시작 가능한 raid failure는 영구 저장하지 않음
- Battery Change처럼 upstream 자체가 의심스러운 failure 데이터는 근거 없이 임의 수정하지 않음

## Release gate

공개 v0.1.6은 다음을 모두 통과한 뒤에만 생성합니다.

1. Desktop ProductVersion `0.1.6`
2. Desktop Release build
3. 전체 automated tests — 현재 기준 190 passed / 0 failed / 0 skipped
4. Content v3~v5 legacy special-trader migration 회귀 테스트
5. BTR Active / Ref Complete / Lightkeeper recoverable access 회귀 테스트
6. Quest graph validator 회귀 테스트
7. user.db special trader sparse override 저장/복원 테스트
8. Windows x64 self-contained single-file publish
9. 실제 startup + Main Map + Factory + MiniMap runtime smoke
10. 정상 Main Window close / process exit
11. release root 검증
    - `준현 헬퍼.exe`
    - `FIRST_RUN_KO.txt`
    - `Assets/`
    - root DLL 없음
    - PDB 없음
    - nested ZIP 없음
    - runtime `Logs/` 없음
12. `Junhyun-Helper-v0.1.6-win-x64.zip` + `SHA256SUMS.txt` 공개 GitHub Release
13. 공개 ZIP 재다운로드 후 SHA-256 재검증
14. draft/prerelease가 아닌 정식 공개 상태 확인

## 관련 설계 문서

- `docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md`
- `docs/QUEST_PREREQUISITE_SEMANTICS.md`
- `docs/QUEST_FAILURE_ANALYSIS.md`
- `docs/DECISIONS.md` DEC-043
- `docs/CONTENT_STORAGE.md`

최종 release workflow / SHA-256 / 공개 URL은 공개 검증 완료 후 이 문서와 `docs/STATE.md`에 기록합니다.
