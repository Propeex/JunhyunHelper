# 사용자 피드백 — Main Map 층 마커 / Quest availability

기록일: **2026-08-15**

상태: **USER CONFIRMED / IMPLEMENTED / FINAL VERIFICATION PENDING / PR #80**

## Main Map 관찰

사용자 실사용 및 Factory 캡처 기준:

- 지상층에서 `Gate 3`가 초록색과 회색으로 겹쳐 두 개 보임
- `3층` 선택 상태에서도 `Office Window`의 본체가 회색으로 보임
- 다른 층에 있는 marker가 Main Map에서 보이지 않음
- 같은 기능이 MiniMap에서는 정상
- 기존 `↑/↓` badge가 지나치게 커서 marker를 가림

## 확인된 원인

서로 다른 세 의미가 Main Map에서 섞여 있었습니다.

### 1. Floor를 visibility로 사용한 제품 정책

v0.1.3의 `LegacyMapInteractionPolicyBridge`가 non-current-floor 일반 marker와 extract를 `Visibility.Collapsed` 처리했습니다. 따라서 별도 floor presentation 코드가 위/아래 관계를 계산해도 사용자에게는 타층 marker가 보이지 않았습니다.

수정:

```text
category/faction OFF → 숨김
floor 다름          → 숨기지 않음, floor presentation으로 구분
```

Floor는 더 이상 visibility filter가 아닙니다.

### 2. Factory Gate 3 raw extract 중복

pinned Map DB에는 동일한 물리 탈출구가 PMC/Scav 등 여러 raw row로 존재할 수 있습니다. `Gate 3`는 같은 이름/같은 floor/사실상 같은 X/Z에 여러 record가 있으며 pinned Main Map renderer는 이를 각각 그립니다. PMC 본체는 초록, Scav 본체는 회색이라 사용자가 본 초록+회색 중복이 발생했습니다.

MiniMap은 이미 물리적으로 겹치는 extract를 display 단위로 묶는 경로가 있어 같은 현상이 두드러지지 않았습니다.

Main Map에서는 다음 조건만 중복 display로 취급합니다.

```text
same extract name
AND same normalized floor
AND X/Z distance <= 1 game unit
```

원본 record/visual 자체를 삭제하지 않습니다. 현재 활성화된 PMC/Scav/Transit filter에서 보이는 후보 중 대표 하나만 시각적으로 표시합니다. 따라서 PMC를 끄고 Scav만 켜도 제거됐던 Scav visual이 다시 필요해지는 문제가 없습니다.

### 3. Office Window의 회색은 floor 색이 아니었음

`Office Window`는 Factory `level3`에 있으며 pinned renderer에서 Scav extract 본체 색이 원래 회색입니다. 즉 `3층`에서 회색으로 보인 것은 타층 표시가 아니라 **faction 색**이었습니다.

본체의 faction/type 색을 floor 의미로 재사용하면 혼동이 생기므로 두 정보를 분리합니다.

## 확정 Floor 표현

marker 자체의 type/faction/icon 색은 유지하고, floor 관계만 작은 outline ring으로 표현합니다.

```text
현재 선택 층 = 초록 ring + 정상 opacity
위층          = 빨강 ring + 약 75% opacity
아래층        = 파랑 ring + 약 75% opacity
floor 불명확  = floor 색상/방향 추측 안 함
```

- 위/아래에는 색각 접근성을 위해 약 7px의 작은 방향 glyph만 보조적으로 사용
- 기존처럼 marker를 가리는 큰 화살표 badge 제거
- Quest / 일반 marker / Raider / extract / MiniMap에 같은 relation 의미 사용
- Main Map extract의 pinned 큰 floor badge도 제거
- legacy renderer가 타층 marker의 faction brush 자체를 흐리게 만든 부분은 정상 faction 색 강도로 복구한 뒤 root opacity와 ring으로 floor 차이를 표현

일반 marker의 서로 다른 known floor가 같은 type/거의 같은 X/Z에 포개지면 current floor를 우선하고, 없으면 `Floor.Order`가 가장 가까운 하나를 대표 표시합니다. 이 역시 원본 데이터를 삭제하지 않는 presentation 정책입니다.

## 갱신/성능 정책

transplanted renderer가 map/floor/filter 변경 직후 비동기로 marker tree를 다시 만들 수 있으므로 일정한 정합화가 필요합니다.

허용:

- map/floor/filter 실제 이벤트
- marker/extract container O(1) 구조 signature 변화
- 변경 직후 제한된 bounded stabilization

금지:

- 프로그램 실행 내내 200ms마다 전체 marker tree를 영구 순회하는 polling

## Quest availability 관찰

현재 지원하지 않는 `globalVariable`, `dialogue`, 실제 게임 완료 시각이 필요한 availability delay 등 때문에 기존 제품 정책에서는 `진행 중` Quest가 200개 이상으로 부풀어 보일 수 있었습니다.

원인은 Core가 `Indeterminate`로 정확히 판정한 Quest를 Application 경계에서 `Current`로 낙관 변환하던 정책입니다.

## 확정 Quest 방향

프로그램이 현재 User Progress만으로 참/거짓을 증명할 수 없는 availability는 `진행 중`으로 가장하지 않습니다.

```text
Core Indeterminate
→ UI: 확인 필요
→ 진행 중(Current) 수치/기본 필터에서 제외
→ Map Current Quest sidebar에서 제외
```

정확성을 위해 다음을 유지합니다.

- `확인 필요`를 `잠김`으로 거짓 확정하지 않음
- 원인(`globalVariable`, `dialogue`, `availabilityDelay` 등)을 표시
- 사용자가 실제 게임에서 Quest를 받은/완료한 상태를 알고 있으면 수동 완료 허용
- 비재시작형 영구 실패 동기화가 필요한 Quest면 `확인 필요` 상태에서도 수동 실패 허용
- Future Needed Items에서는 `IndeterminatePotential`로 계속 보수적으로 포함하여 잠재적으로 필요한 Item을 잘못 버리게 하지 않음
- 프로그램이 판별할 수 없는 서버/대화 상태를 임의 추측하지 않음

이 결정은 DEC-038의 optimistic Current 부분을 DEC-039로 대체하고, Map floor visibility/presentation 책임은 DEC-040으로 기록했습니다.

## Release gate

v0.1.4는 다음을 모두 통과한 뒤에만 공개합니다.

```text
Desktop Release build
all automated tests
Windows x64 self-contained single-file publish
real Main Map + MiniMap runtime smoke
floor hotkey viewport preservation
normal Main Window close/process exit
ProductVersion 0.1.4
package root/PDB/DLL/nested ZIP/Logs checks
final whole-change review with no unresolved release blocker
public GitHub asset re-download + SHA-256 verification
```

최종 run / release SHA / 공개 ZIP checksum은 v0.1.4 공개 완료 후 `docs/STATE.md`와 release record에 기록합니다.
