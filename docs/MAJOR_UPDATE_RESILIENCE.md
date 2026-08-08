# MAJOR UPDATE RESILIENCE — 대형 패치 내구성 계약

상태: `CONFIRMED / VERIFIED`

기준일: **2026-08-08**

## 목적

이 문서는 준현 헬퍼를 만드는 핵심 이유를 회귀 계약으로 고정합니다.

게임 패치가 Quest/Hideout/Item 관계를 크게 바꾸더라도:

1. 최신 Game Content는 새로 구축한다.
2. User Progress는 패치 때문에 삭제하거나 덮어쓰지 않는다.
3. 새 Game Content + 기존 User Progress에서 결과를 다시 계산한다.
4. 더 이상 필요하지 않은 기존 보유품은 사라지는 대신 `정리 필요`로 드러난다.
5. 새 콘텐츠가 잘못되면 기존 정상 콘텐츠를 유지한다.

즉 패치가 데이터 의미를 바꿀 수는 있어도, **사용자가 그동안 기록한 진행과 보유 사실을 잃게 만들어서는 안 됩니다.**

---

## 검증 경계

테스트는 계산기만 직접 호출하지 않습니다.

다음 실제 저장 경계를 사용합니다.

```text
old content.db
+ user.db
    ↓
old FutureNeededItemsPlan
    ↓
new content.candidate.db
    ↓
canonical validation
    ↓
active 교체
    ↓
new content.db 재읽기
+ 같은 user.db 재읽기
    ↓
new FutureNeededItemsPlan
    ↓
Needed / Cleanup 변화 비교
```

사용 클래스:

- `ContentSnapshotStore`
- `ContentActivationService`
- `UserProfileStore`
- `FutureNeededItemsPlanner`
- `InventoryCleanupChangeDetector`
- `FlexibleQuestItemRequirementCalculator`

---

## 시나리오 1 — Quest 요구 아이템 교체 + 기존 Item metadata 삭제

패치 전:

```text
Quest A: 전선 8개 필요
사용자: 전선 8개 보유
```

패치 후:

```text
Quest A: 볼트 8개 필요
전선 metadata도 새 Game Content에서 사라짐
```

보장:

- user.db의 전선 8개는 그대로 유지
- 새 계획에서 볼트 8개가 필요
- 전선 8개는 `정리 필요`
- 화면 표시용 metadata가 없어도 stable Item ID를 통해 사용자 보유 사실을 잃지 않음
- `InventoryCleanupChangeDetector`가 전선 정리 가능 +8을 감지
- 교체 전 content.db는 `content.previous.db`로 남음

---

## 시나리오 2 — 필요 수량 감소

패치 전:

```text
전선 10개 필요
사용자 보유: FIR 2 + 일반 8 = 10
```

패치 후:

```text
전선 4개 필요
```

보장:

- user.db 수량은 FIR 2 + 일반 8 그대로 유지
- 새 필요량을 충족하고도 안전하게 남는 수량만 정리 가능으로 계산
- 검증 사례에서는 FIR 2 + 일반 4 = 총 6개가 정리 가능
- 필요량 감소분을 사용자 인벤토리에서 자동 삭제하지 않음

---

## 시나리오 3 — Edition 규칙 변경

패치 전에는 현재 에디션에서 가능한 Quest가, 패치 후 edition exclusion에 들어간 상황을 검증합니다.

보장:

- Quest 미래 도달 가능성이 `Unavailable`로 바뀜
- 해당 Quest 요구 아이템은 미래 필요량에서 제거
- 이미 모아둔 보유량은 그대로 유지하고 `정리 필요`로 이동
- 사용자의 EditionId 자체는 변경하지 않음

---

## 시나리오 4 — Hideout 미래 재료 교체

패치 전:

```text
Workbench 현재 Lv.1
Lv.2 재료: 전선 5개
```

패치 후:

```text
Workbench 현재 Lv.1
Lv.2 재료: 볼트 5개
```

보장:

- user.db의 Workbench Lv.1은 그대로 유지
- 새 계획은 볼트 5개를 요구
- 이미 모은 전선 5개는 `정리 필요`
- Hideout 진행 상태를 패치 데이터에서 다시 추측하지 않음

---

## 시나리오 5 — 유동 제출 후보 변경

패치 전:

```text
A 또는 B를 합쳐 총 5개 제출
사용자: A 5개 보유
```

패치 후:

```text
B 또는 C를 합쳐 총 5개 제출
```

보장:

- 별도 `선택한 제출 아이템` 상태가 없으므로 migration이 필요 없음
- 기존 A 5개 보유 사실은 그대로 유지
- 새 Game Content의 B/C와 같은 Inventory를 다시 결합해 남은 수량 5개로 계산
- A가 더 이상 다른 필요처가 없다면 정리 대상으로 전환
- 후보별 cleanup은 유동 그룹이 열려 있는 동안 보수적으로 보호

---

## 시나리오 6 — 잘못된 candidate update

새 candidate가 존재하지 않는 Item ID를 Quest requirement에서 참조하도록 만든 실패 사례를 검증합니다.

보장:

- candidate canonical validation 실패
- active content.db를 교체하지 않음
- 기존 정상 content.db 유지
- user.db 유지
- 실패한 업데이트 때문에 `content.previous.db`를 새로 만들거나 정상 active를 훼손하지 않음

---

## 사용자 데이터 불변 조건

Game Content update는 다음 User Progress를 직접 수정할 수 없습니다.

- Profile identity / GameMode
- level / faction / edition / prestige
- 명시적으로 입력된 trader progress
- completed Quest IDs
- explicit permanent failed Quest IDs
- Hideout station levels
- FIR / Non-FIR inventory quantities

새 패치가 이 사실들을 무효로 보이게 만들더라도, 먼저 **새 규칙에서 어떻게 해석할지 계산**합니다. 사용자가 입력한 사실 자체를 조용히 지우지 않습니다.

---

## 현재 자동 변화

Game Content 변경으로 파생 결과는 자동으로 바뀔 수 있습니다.

- Quest Current / Locked / Unavailable / Indeterminate
- 미래 Quest reachability
- Needed Items
- Cleanup Items
- 유동 제출 진행도
- Ammo 사실/수급처
- Hideout 다음 업그레이드 표시

이들은 User Progress가 아니라 계산 결과이므로 별도 migration 대상으로 저장하지 않습니다.

---

## 검증 결과

2026-08-08 GitHub Actions:

- Windows Server 2025
- .NET SDK 10.0.302
- Desktop Release build: **0 warnings / 0 errors**
- 전체 테스트: **134 passed / 0 failed / 0 skipped**

대형 패치 신규 시나리오 6개가 포함된 결과입니다.

---

## 유지보수 규칙

향후 실제 Tarkov 대형 패치에서 새로운 파손 형태가 발견되면 임시 예외를 UI에 붙이지 않습니다.

처리 순서:

```text
실제 실패 사례 확보
→ 원천 의미 확인
→ 기존 명시 규칙과 충돌 분석
→ importer/domain/storage 중 책임 위치에서 수정
→ 이 문서의 대형 패치 회귀 시나리오에 추가
```

준현 헬퍼가 다시 같은 종류의 패치에서 망가지지 않도록 **실제 실패를 영구 회귀 테스트로 남기는 것**이 원칙입니다.
