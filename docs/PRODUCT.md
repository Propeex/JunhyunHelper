# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 준현 헬퍼의 **무엇을 만들고 왜 만드는지**를 정의하는 canonical 제품 요구사항이다. 사용자가 현재 대화에서 새로 확정한 제품 의도가 기존 구현보다 우선한다. 현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않는다.

기준일: **2026-09-04 KST**  
상태: **PRODUCT COMPLETE / MAINTENANCE MODE — v1.17.1 Farming Guide removal confirmed**

정확한 release SHA/asset/CI와 schema 사실값은 `docs/PROJECT_STATE.json`, 공개 상태는 `docs/CURRENT_STATE.md` / `docs/STATE.md`를 사용한다.

## 1. 제품 정의

준현 헬퍼는 Escape from Tarkov 플레이에 필요한 진행, 아이템, 탄약, 지도와 화면 인식을 하나의 Windows x64 데스크톱 프로그램에서 제공하는 개인용 헬퍼다.

핵심 목표:

- 플레이 중 필요한 정보를 빠르게 확인한다.
- 사용자가 직접 확인한 진행 상태를 정확히 저장한다.
- current Tarkov data를 검증 가능한 범위에서 안전하게 반영한다.
- 알 수 없는 상태를 낙관적으로 추측하지 않고 fail closed한다.
- 게임 프로세스 내부를 읽거나 변조하지 않는 외부 보조 프로그램을 유지한다.
- 사용자 데이터와 외부 Game Content의 lifecycle을 분리한다.
- 실사용 회귀를 재현 가능한 evidence/test로 축적한다.

제품이 아닌 것:

- Tarkov bot / input automation
- anti-cheat bypass 도구
- game memory/packet inspector
- runtime GPT/AI가 필수인 서비스
- backend/account service

## 2. 플랫폼 / 배포

- Windows x64
- .NET 10 / WPF
- self-contained single-file executable
- portable ZIP / installer 없음
- 일반 사용에 관리자 권한 불필요
- mutable user state는 `%LocalAppData%/JunhyunHelper`에 저장
- Program Update는 latest public stable GitHub release를 기준으로 사용자 동의 후 수행
- public stable source/tag/assets는 immutable historical identity

정확한 current public stable과 exact product source는 `docs/PROJECT_STATE.json`을 단일 기준으로 사용한다.

## 3. 데이터 authority

### Game Content

Remote Tarkov source를 import/검증해 만든 canonical snapshot이다.

- Quest / Hideout / Item / Ammo 등 게임 기준 데이터
- candidate가 validation/completeness/integrity를 통과해야 active가 됨
- Last Known Good 보존

### User Progress / user-owned state

- profile / GameMode / level / faction / edition / prestige
- trader / Quest / Hideout 진행
- exact observed ProfileVariables
- FIR/non-FIR inventory와 consumption ledger
- Scanner settings/favorites/recents/reviewed evidence
- Map/MiniMap settings

Game Content Update나 Program Update가 user-owned state를 덮어쓰지 않는다.

## 4. Quest / Hideout / Needed Items

- exact ProfileVariable 값은 compatibility inference보다 우선한다.
- unsupported/unknown prerequisite와 structural drift는 fail closed한다.
- audited staged task-pool compatibility는 증명된 범위에서만 사용한다.
- Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산한다.
- flexible candidate requirement는 실제 hand-in을 추측하지 않는다.
- Hideout FIR requirement는 source `foundInRaid` 의미를 보존한다.
- deterministic mandatory consumption은 ledger를 사용해 중복 소비와 rollback을 관리한다.

Current needed quantity/source authority:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

다른 subsystem이 별도 계산으로 새로운 truth를 만들지 않는다.

## 5. Items / Ammo

Items는 canonical content, profile, inventory, Needed Items를 결합한 탐색/조회 surface다.

Ammo는 read-only 비교와 profile-aware pickup 판단을 제공한다.

- same-caliber penetration 비교
- 현재 profile에서 증명된 direct-purchase state 사용
- flea/barter/craft/higher trader LL/unproven quest unlock은 현재 직접 구매 가능으로 취급하지 않음
- authoritative Ammo Pack `containsItems` 관계 우선

## 6. Map / MiniMap

Pinned donor:

```text
SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

Donor 전체는 제품 사양 권위가 아니다. JunhyunHelper first-party bridge/customization이 product lifecycle과 presentation 의미를 소유한다.

## 7. Scanner / Mini Scanner

Scanner는 **Tarkov 화면 픽셀을 current catalog Item ID에 연결**하는 외부 입력 subsystem이다.

대표 흐름:

```text
screen capture
→ detail/header structural validation
→ item-name ROI
→ serialized OCR
→ bounded normalization
→ current-catalog conservative matching
→ optional strict visual corroboration
→ Item ID or fail closed
```

Safety:

- external screen pixels + OCR만 사용
- game memory read / injection / hook / kernel-driver access / input automation / network manipulation / anti-cheat bypass 금지

Recognition:

- false positive보다 miss 선호
- current official catalog가 identity authority
- ambiguity면 Item ID를 내지 않음
- Item ID 확정 전에 price/needed/source/previous-frame metadata를 identity evidence로 사용하지 않음
- reviewed actual Tarkov evidence 없이 OCR/matcher/recovery acceptance를 완화하지 않음
- Ground Truth는 explicit user-reviewed save만 authoritative

Mini Scanner는 confirmed Item ID에 대한 가격, 필요 개수, 탄약 판단 등 사용자가 선택한 Scanner presentation field를 표시한다.


## 8. Removed feature — Farming Guide

Farming Guide는 **v1.17.1에서 제품에서 완전히 제거**된다.

현재 제품에는 다음이 존재하지 않는다.

- Farming Guide navigation/page
- loadout/inventory editor 또는 preset
- raid-session farming recommendation
- automatic packing/repacking/replace/discard logic
- Farming Guide lock/reserved-cell/weight/quantity flows
- Scanner Farming Guide bridge, Mini Scanner instruction row, accept hotkey
- Farming Guide-specific persistence/service/domain model
- Farming Guide-only Game Content metadata contract

과거 `farming-guide.json`은 current product state가 아니다. 프로그램은 이를 읽거나 쓰지 않으며 자동 삭제도 하지 않는다.

이전 Farming Guide 결정/릴리즈 문서는 역사 기록으로만 유지한다. Current authority: `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`.

## 9. Diagnostics

진단은 명시적 opt-in이다. 김태영 PC 지원 경로는 로컬 diagnostic ZIP 생성 후 사용자가 직접 전달하는 구조이며 자동 upload/attachment/send를 하지 않는다. 실제 원인 판정은 해당 PC evidence가 있어야 한다.

## 10. UI / interaction

- 제품 전체와 일관된 WPF interaction
- shared overlay는 presentation lifetime만 소유하고 domain truth를 재구현하지 않음
- source/XAML만 보고 user-visible 변경 완료 선언 금지
- 기존 verified behavior를 무관한 새 기능 때문에 변경하지 않음
- 실사용에서 관찰된 표시/반응 문제는 자동화 테스트가 통과하더라도 실제 회귀 evidence로 취급

## 11. Schema / compatibility

```text
Desktop: 1.15.3
Public stable: 1.15.3
Content write: v10
Content readable: v3-v10
user.db: v1
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```


## 12. Release quality gate

변경 성격에 따라 다음을 검증한다.

- deterministic tests
- Windows Release build / XAML compile
- self-contained win-x64 publish
- actual published EXE startup / relevant Product UI runtime smoke
- graceful shutdown / active-async Shutdown Race
- portable package/root audit
- ZIP/checksum equality
- CI / Documentation Consistency
- exact-main identity
- public tag/release/assets/latest readback

실사용 보고 증상은 자동화 테스트보다 높은 우선순위의 회귀 evidence다.

## 13. 유지보수 방향

현재 public 제품은 product-complete maintenance mode다. v1.17.1은 기존 Farming Guide 기능을 제거하는 PATCH이며, 이후 유지보수는 남은 제품 기능의 실사용 오류·Tarkov 변화·안정성·신뢰성·성능·회귀 방지를 우선한다.

기본 우선순위는 실사용 오류, Tarkov 변화 대응, 안정성/신뢰성, 성능, regression coverage, bounded technical debt cleanup 순이다. 추가 새 기능이나 UX 변경은 사용자의 명시적인 제품 요구사항이 있을 때만 설계한다.
