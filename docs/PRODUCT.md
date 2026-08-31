# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 준현 헬퍼의 **무엇을 만들고 왜 만드는지**를 정의하는 canonical 제품 요구사항이다. 사용자가 현재 대화에서 새로 확정한 제품 의도가 기존 구현보다 우선한다. 현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않는다.

기준일: **2026-08-31 KST**  
상태: **v1.13.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

정확한 release SHA, asset, CI와 현재 schema 사실값은 `docs/PROJECT_STATE.json`, `docs/CURRENT_STATE.md`, `docs/STATE.md`를 사용한다.

## 1. 제품 정의

**준현 헬퍼**는 Escape from Tarkov 플레이에 필요한 진행, 아이템, 탄약, 지도, 화면 인식, raid-start loadout 정보를 하나의 Windows x64 데스크톱 프로그램에서 제공하는 개인용 헬퍼다.

핵심 목표:

- 플레이 중 필요한 정보를 빠르게 확인한다.
- 사용자가 직접 확인한 진행 상태를 정확히 저장한다.
- current Tarkov data를 검증 가능한 범위에서 안전하게 반영한다.
- 알 수 없는 상태를 낙관적으로 추측하지 않고 fail closed한다.
- 게임 프로세스 내부를 변조하거나 읽지 않는 외부 보조 프로그램을 유지한다.
- 사용자 데이터와 외부 Game Content의 lifecycle을 분리한다.
- 실사용 회귀를 재현 가능한 evidence/test로 축적한다.
- 기능이 늘어도 각 subsystem의 authority와 책임을 분리한다.

제품이 아닌 것:

- Tarkov bot / automation tool
- anti-cheat bypass 도구
- game memory/packet inspector
- runtime GPT/AI가 필수인 서비스
- 서버/backend가 필요한 계정 서비스

## 2. 플랫폼 / 배포

- Windows x64
- .NET 10 / WPF
- self-contained single-file executable
- portable ZIP
- 별도 .NET Runtime 설치 불필요
- installer 없음
- 일반 사용에 관리자 권한 불필요
- 별도 backend 없음
- runtime GPT/AI 없음

User-facing package:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

Mutable user data는 `%LocalAppData%/JunhyunHelper`에 저장한다. Portable executable 옆에 profile/log/settings 등의 mutable state를 생성하지 않는다.

## 3. 데이터 authority 원칙

제품 데이터는 의미에 따라 분리한다.

### Game Content

Remote Tarkov source에서 검증 후 생성한 canonical snapshot이다.

- Quest / Hideout / Item / Ammo 등 게임 기준 데이터
- Farming Guide item dimensions / storage grids / compatible slots 등 current item structure
- active snapshot은 검증된 candidate만 승격

### User Progress

사용자가 확인·입력한 개인 진행 사실이다.

- profile / GameMode
- level / faction / edition / prestige
- trader 상태
- Quest completion / explicit permanent failure
- exact observed ProfileVariables
- Hideout progression
- FIR / non-FIR inventory
- consumption ledger

### Presentation / subsystem state

- Scanner settings / favorites / recents
- Map/MiniMap settings
- Farming Guide working state / presets / fixed equipment
- diagnostics / reviewed Ground Truth

Game Content Update나 Program Update가 user-owned state를 덮어쓰지 않는다.

## 4. Game Content Update

Game Content는 다음 fail-closed lifecycle을 따른다.

```text
remote source
→ parse/import
→ required semantics/schema validation
→ canonical candidate
→ completeness / LKG guard
→ candidate DB
→ read-back/integrity validation
→ atomic active replacement
→ image prefetch
```

계약:

- candidate 완성 전 active snapshot overwrite 금지
- failed candidate 폐기
- 기존 healthy Last Known Good 유지
- suspicious partial payload / unexplained shrink 차단
- importer가 의미를 이해하지 못하는 collection/schema drift는 fail closed
- 개별 optional enrichment/image failure는 해당 범위에서 fail-soft 가능
- User Progress / Farming Guide user state / reviewed Ground Truth를 수정하지 않음

Top-level Game Data Update는 일반 content activation 뒤 current GameMode Scanner catalog/market refresh를 함께 수행한다. Scanner catalog refresh만 실패했다고 healthy general content를 rollback하지 않는다.

## 5. Program Update

프로그램 업데이트는 GitHub의 latest public stable release를 기준으로 한다.

- current보다 strictly newer stable SemVer만 대상
- 사용자 동의형
- exact release ZIP + checksum 검증
- staging/package-root 검증 전 current program files 변경 금지
- program-owned files만 transaction 방식으로 교체
- 실패 시 가능한 범위에서 rollback
- `%LocalAppData%/JunhyunHelper` user state는 update 대상이 아님
- public stable tag/source/assets는 immutable historical identity

Release workflow는 exact-main CI가 생성한 검증 artifact를 사용한다. 동일 version의 후속 documentation-only commit이 별도 bytes를 만들 수 있어도 기존 public stable asset을 교체하거나 product source를 재정의하지 않는다.

## 6. Profile / User Progress

지원 GameMode별 profile은 독립적이다.

- Regular/PvP 계열
- PvE
- 제품에서 지원하는 season/profile mode

User Progress는 사용자가 확인한 사실을 authority로 한다. Derived availability/needed/cleanup 결과를 별도 authoritative fact처럼 저장하지 않는다.

`user.db`의 현재 schema는 v1이다.

## 7. Quest

Quest 화면은 current content + profile facts를 결합해 availability/progress를 표현한다.

주요 원칙:

- 서로 다른 prerequisite requirement는 source semantics대로 결합한다.
- exact ProfileVariable 값이 있으면 compatibility inference보다 우선한다.
- 받을 수 있는 Quest를 제품 내에서 이미 수락한 것으로 다루는 기존 제품 계약을 유지한다.
- unsupported/unknown prerequisite를 낙관적으로 통과시키지 않는다.
- audited staged task-pool compatibility는 current structure가 증명되는 범위에서만 사용한다.
- structural drift는 `확인 필요`/indeterminate로 fail closed한다.
- current Quest UI compatibility를 Future Needed Items / cleanup에 낙관적으로 전파하지 않는다.

## 8. Quest / Hideout Item 소비

Fixed mandatory material은 완료/upgrade 처리와 함께 ledger 기반으로 소비할 수 있다.

- 중복 소비 방지
- rollback 시 ledger 기반 복구
- flexible candidate hand-in은 실제 선택 item을 자동 추측하지 않음
- malformed/non-positive requirement는 active content로 승격하지 않음

Hideout requirement의 FIR 의미는 source `attributes.foundInRaid`를 보존한다. FIR requirement를 non-FIR inventory로 충당하지 않는다.

## 9. Needed Items / Inventory / cleanup

Needed Items는 앞으로 실제 필요할 수 있는 item을 보수적으로 보호한다.

- future Quest requirement
- future Hideout requirement
- flexible candidate 보호
- unresolved future path는 indeterminate potential로 유지
- cleanup safety를 증명할 수 없으면 정리 가능으로 단정하지 않음
- FIR / non-FIR inventory를 구분

Current user-facing needed quantity authority:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
```

Needed source authority:

```text
ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

Scanner 등 다른 subsystem이 Quest/Hideout requirement를 별도로 재계산해 다른 truth를 만들지 않는다.

## 10. Items

Items는 canonical content, profile, inventory, Needed Items를 결합한 탐색/조회 surface다.

- 이름/분류/필요 여부 기반 탐색
- 필요한 item의 Quest/Hideout source 표시
- Inventory 상태 결합
- Quest / Hideout / Ammo cross-navigation
- Wiki navigation
- flexible candidate 의미 보존

## 11. Ammo

Ammo는 read-only 비교와 profile-aware pickup 판단을 제공한다.

- name / caliber 검색
- caliber favorites / favorites selector
- configurable visible columns
- Ammo detail
- Items/Ammo cross-navigation

Pickup 의미:

- same-caliber penetration 비교
- 현재 profile에서 **증명된 direct purchase state** 사용
- flea/barter/craft/higher trader LL/unproven quest unlock을 현재 직접 구매 가능으로 취급하지 않음
- authoritative Ammo Pack `containsItems` 관계를 우선
- 자체 임의 armor-effectiveness heuristic을 새 truth로 만들지 않음

## 12. Map / MiniMap

Map/MiniMap은 pinned donor source를 제한적으로 사용한다.

```text
SIGDrone/Tarkov-Helper
d933792b6042a51cea38dc44b686a096fe30de67
```

기존 donor 전체는 제품 사양 권위가 아니다. JunhyunHelper first-party bridge/customization이 제품 의미와 lifecycle을 소유한다.

유지 계약:

- current Quest와 Map navigation bridge
- general / PMC / Scav / Transit marker presentation
- Main Map selection과 fresh/reused MiniMap synchronization
- player position/heading의 동일 map transform 좌표계 사용
- floor relation 의미 보존
- MiniMap window/settings lifecycle 보존
- product settings/editor UI는 shared in-app overlay 원칙 유지
- 검증된 donor source를 concrete defect 없이 broad refactor하지 않음

## 13. Scanner / Mini Scanner

Scanner는 **Tarkov 화면 픽셀을 current catalog Item ID에 연결**하는 외부 입력 subsystem이다.

대표 흐름:

```text
screen capture
→ detail/header structural validation
→ item-name ROI
→ serialized ko-KR OCR
→ optional bounded normalization/substitution
→ current-catalog conservative matching
→ optional strict current-pixel visual corroboration
→ Item ID or fail closed
→ local item/market/needed presentation
```

Safety contract:

- external screen pixels + OCR만 사용
- game process memory read 금지
- DLL/code injection 금지
- process/game hook 금지
- kernel/driver 접근 금지
- input automation 금지
- game network manipulation 금지
- anti-cheat bypass 금지

Recognition contract:

- false positive보다 miss 선호
- geometry/structure/normalization은 Item identity proof 자체가 아님
- current official catalog가 identity authority
- ambiguity면 Item ID를 내지 않음
- scan-time network identity work 금지
- Item ID 확정 전에 price/needed/source/previous-frame metadata를 identity evidence로 사용하지 않음
- reviewed actual Tarkov evidence 없이 OCR/matcher/candidate/visual acceptance를 완화하지 않음

Ground Truth는 explicit user-reviewed save만 authoritative하다. 자동 correction/evidence 저장이 사용자 검토 없이 Ground Truth를 생성하지 않는다.

Canonical specialist document는 `docs/SCANNER.md`다.

## 14. Farming Guide

v1.13.0에서 Scanner 오른쪽에 `파밍 가이드` first-class section을 추가했다.

제품 의미:

**레이드 시작 상태를 구성하는 Loadout / Inventory Editor**다.

### 포함 기능

- 헤드셋, 헬멧/headwear, face/eyewear, armor/armored rig, armband, weapon, sidearm 등 장비 구성
- Pocket / Rig / Backpack / Secure Container / Special Slot 표현
- current Tarkov `width × height` footprint
- 검색 결과 기반 drag-and-drop
- drag 중 `R` 90도 회전
- bounded grid snap
- bounds / overlap / contiguous-space / current filter 검증
- storage grid / equipment slot / attachment slot / armor plate slot / conflict 구조를 current validated Game Content에서 사용
- attachment / 교체형 armor plate 설정
- 전체 raid-start state preset save/load
- melee / PMC dogtag는 per-profile preset과 분리된 fixed setting
- 총 무게 / 사용 storage cell / 전체 storage cell 요약
- filled carrier destructive replacement fail-closed
- old preset이 current Tarkov grid/filter와 충돌하면 impossible placement를 복원하지 않음

### v1.13.0 비포함

- loot 가치 판단
- pickup 추천
- discard 추천
- replace 추천
- Scanner 실시간 recommendation
- 실제 raid inventory grid 좌표의 지속적인 1:1 동기화

Farming Guide는 향후 recommendation engine의 입력 기반이 될 수 있지만, recommendation-derived truth를 editor의 canonical state와 혼합하지 않는다.

제품 결정:

- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`

기술 계약:

- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## 15. Farming Guide persistence / schema

사용자 상태:

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema v1
```

Game Content item structure 확장:

```text
Content write schema: v9
Readable Content schemas: v3~v9
```

기존 v1.12.x user.db/Scanner 설정에 mandatory migration은 없다. Old readable Content snapshot에 Farming Guide 구조가 없으면 그 구조를 추측해 만들지 않는다.

## 16. 진단 / 지원

제품 진단은 사용자가 명시적으로 실행하는 opt-in 경로다.

김태영 PC 진단의 현재 계약:

- 명시적 확인 후 local diagnostic ZIP 생성
- allowlist 기반 display/GPU/HDR/capture/Scanner evidence
- 자동 upload 없음
- 자동 email attachment/send 없음
- 완료 후 Naver Mail 작성 페이지를 기본 browser로 여는 수준
- credential/불필요한 host identity/network inventory를 수집하지 않음
- optional probe failure는 핵심 ZIP 생성과 분리해 fail-soft

실제 김태영 PC 원인 판정은 해당 PC에서 수집된 evidence가 들어온 뒤 수행한다.

## 17. UI / interaction 원칙

- 사용자-facing settings/editor는 제품 전체와 일관된 WPF interaction을 사용한다.
- shared MainWindow overlay는 presentation lifetime만 소유하고 child domain/save semantics를 재구현하지 않는다.
- search clear 등 공통 affordance는 presentation-only behavior를 재사용한다.
- user-visible 동작은 source/XAML만 보고 완료 선언하지 않는다.
- 기존 verified behavior를 새 기능 때문에 무관하게 변경하지 않는다.

## 18. 검증 / 릴리즈 품질 게이트

사용자에게 보이는 기능 변경과 중요한 유지보수는 변경 성격에 따라 다음을 검증한다.

- deterministic tests
- Windows Release build / XAML compile
- Windows x64 self-contained publish
- actual published EXE startup
- 관련 Product UI runtime smoke
- Map/Scanner 등 주요 비회귀 smoke
- normal shutdown
- active-async Shutdown Race
- portable package/root audit
- ZIP/checksum equality
- CI / Documentation Consistency
- exact-main identity
- public tag/release/assets/latest readback

외부 Tarkov source semantics/structure가 변하는 작업에는 필요한 범위에서 live-data 검증을 추가한다.

실사용에서 보고된 실제 증상은 자동화 테스트보다 높은 우선순위의 회귀 evidence로 취급한다.

## 19. 유지보수 방향

현재 제품은 **product-complete maintenance mode**다.

기본 우선순위:

1. 실사용 오류/회귀 수정
2. Tarkov 변화 대응
3. 안정성/신뢰성 강화
4. 성능 개선
5. deterministic regression 강화
6. 유지보수성/기술 부채 정리

새 기능이나 사용자 경험 변경은 사용자의 명시적인 제품 요구사항이 있을 때만 설계한다. 정상 동작하는 subsystem을 미관상 이유나 추측성 최적화를 위해 대규모 재작성하지 않는다.

현재 공개 제품의 정확한 historical identity는 `docs/PROJECT_STATE.json`과 `docs/RELEASE_1.13.0.md`를 사용한다.
