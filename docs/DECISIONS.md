# DECISIONS — 현재 유효한 장기 결정

이 문서는 준현 헬퍼의 **현재 유효한 장기 결정과 supersession 관계**를 빠르게 복구하기 위한 active index다.

2026-08-09까지 DEC-001~DEC-029 원문은 다음 역사 파일에 보존한다.

- [`DECISIONS_HISTORY_THROUGH_2026-08-09.md`](DECISIONS_HISTORY_THROUGH_2026-08-09.md)

사용자의 더 새로운 확정 요구와 더 새로운 결정이 과거 충돌 결정보다 우선한다. 상세 제품/기술 의미는 `PRODUCT.md`, `STATE.md`, `ARCHITECTURE.md` 및 전문 문서를 함께 읽는다.

## 현재 supersession 주의

DEC-050/054/057/058/059에 남아 있는 과거 Scanner `RequiredTotal` 문구는 **역사적 당시 계약**이다. 현재 사용자-facing Scanner `필요 개수` 권위는 v1.7.11 결정에 따라 다음과 같다.

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
```

v1.7.13 검색 상세의 Quest/Hideout source 권위는 같은 authoritative row의:

```text
ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

Scanner가 둘 중 어느 것도 별도 재계산하거나 Item identity evidence로 사용하지 않는다.

현재 제품 상태는 **v1.7.13 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**다. 최신 상태/릴리즈 증거는 `docs/STATE.md`가 권위다.

---

# 1. DEC-001~029 상태 인덱스

- `DEC-001` — 새 제품은 처음부터 설계한다 — **CONFIRMED**
- `DEC-002` — 기존 Tarkov-Helper는 자동으로 제품 사양이 아니다 — **CONFIRMED**, Map/MiniMap 예외는 DEC-031
- `DEC-003` — GitHub 저장소를 프로젝트 기억의 공식 기반으로 사용 — **CONFIRMED**
- `DEC-004` — 사용자는 제품 판단에 집중하고 개발 절차는 개발자가 책임 — **CONFIRMED**
- `DEC-005` — 초기 Phase 1에서는 구현보다 설계를 선행 — **PHASE-SPECIFIC / SUPERSEDED by DEC-030**
- `DEC-006` — 공식 제품명은 준현 헬퍼 — **CONFIRMED**
- `DEC-007` — 초기 상위 기능 영역 정의 — **CONFIRMED**, Scanner 의미는 이후 결정이 구체화
- `DEC-008` — 구두 의도는 의미를 맞춘 뒤 공식 요구사항으로 확정 — **CONFIRMED**
- `DEC-009` — Quest 원천은 json.tarkov.dev → 내부 canonical model — **CONFIRMED**
- `DEC-010` — 받을 수 있는 Quest는 Helper에서 이미 수락한 것으로 간주 — **CONFIRMED**
- `DEC-011` — Quest 해금에 필요한 사용자 상태는 진행 profile에서 관리 — **CONFIRMED**
- `DEC-012` — GameMode별 진행은 독립 profile — **CONFIRMED**
- `DEC-013`~`DEC-019` — Quest/Hideout/Needed Items 미래 필요·보수적 cleanup 의미 — **CONFIRMED**
- `DEC-020` — Inventory 자동 추정 금지 초기 원칙 — **PARTIALLY SUPERSEDED by DEC-025/026**
- `DEC-021`~`DEC-024` — UI/navigation/Ammo source 및 표시 경계 — **CONFIRMED**
- `DEC-025` — 고정 소모 Item은 명시적 진행 조작과 함께 자동 차감 — **CONFIRMED**
- `DEC-026` — flexible hand-in 실제 소비 Item은 자동 추정하지 않음 — **CONFIRMED**
- `DEC-027` — Wiki Ballistics membership과 effectiveness는 별도 canonical fact — **CONFIRMED**
- `DEC-028` — Prestige 기본값은 0 — **CONFIRMED**
- `DEC-029` — 제품 이미지는 Game Content update 후 prefetch — **CONFIRMED**

---

# 2. DEC-030~059 장기 결정

## DEC-030 — 확정 기능 수정은 직접 진행하고 새 제품 의미는 설계를 먼저 맞춘다

- 상태: `CONFIRMED`
- 이미 확정·구현된 기능의 버그 수정, 회귀 수정, 성능 개선, 릴리즈 하드닝은 개발자가 저장소/테스트를 조사해 직접 진행한다.
- 새 기능이나 제품 의미 변경은 사용자 의도를 먼저 확정한다.
- supersedes DEC-005의 현재 단계 구현 금지 문장.

## DEC-031 — Map/MiniMap은 검증된 donor 기준선을 제한적으로 채택한다

- 상태: `CONFIRMED`
- 현재 product pin: `d933792b6042a51cea38dc44b686a096fe30de67`
- Map/MiniMap에 한해서 pinned donor source를 사용한다.
- donor updater/hidden command/기타 데이터 규칙은 승계하지 않는다.

## DEC-032 — Map subsystem은 독립이며 Quest만 JunhyunHelper 진행 데이터와 연결한다

- 상태: `CONFIRMED`
- artwork/config/general marker/MiniMap/screenshot tracking은 독립 subsystem이다.
- current Quest/Quest geometry만 JunhyunHelper bridge로 연결한다.

## DEC-033 — 미구현 Scanner를 public UI에서 숨긴다

- 상태: `SUPERSEDED by DEC-045 and later Scanner implementation decisions`

## DEC-034 — release/update와 product hotkey는 JunhyunHelper가 소유한다

- 상태: `CONFIRMED / UPDATED by DEC-046`
- legacy updater/hidden command/easter egg/legacy hidden shortcut는 제품 동작이 아니다.

## DEC-035 — Windows x64 self-contained portable release를 유지한다

- 상태: `CONFIRMED / UPDATED by DEC-046 and current deployment contract`
- installer 없는 Windows x64 self-contained portable 제품을 유지한다.

## DEC-036 — Release artifact 공급망/오염 검사를 gate로 둔다

- 상태: `CONFIRMED`
- PDB/unused legacy dependency/nested archive 등 배포 오염을 차단한다.
- 정확한 현재 package gate는 `docs/DEPLOYMENT.md`가 권위다.

## DEC-037 — Map bundle update는 같은 upstream revision의 원자적 bundle로 한다

- 상태: `CONFIRMED`
- artwork/config/general-marker data를 다른 revision과 섞지 않는다.

## DEC-038 — 불완전한 Quest availability source는 추측하지 않는다

- 상태: `CONFIRMED / PARTIALLY SUPERSEDED by DEC-039/043/044`

## DEC-039 — 입증할 수 없는 Quest availability는 `확인 필요`로 분리한다

- 상태: `CONFIRMED`
- Core `Indeterminate`를 optimistic Current로 바꾸지 않는다.
- Future Needed Items는 잠재 필요 Item을 보호한다.

## DEC-040 — Map floor 관계는 visibility가 아니라 presentation이다

- 상태: `CONFIRMED / PARTIALLY SUPERSEDED by DEC-041`

## DEC-041 — 서로 다른 floor의 일반 marker는 X/Z가 겹쳐도 숨기지 않는다

- 상태: `CONFIRMED`

## DEC-042 — 층 변경은 Main Map과 MiniMap의 현재 viewport를 보존한다

- 상태: `CONFIRMED`

## DEC-043 — 특수 상인 접근은 upstream 조건을 보존하고 recoverable access를 별도 모델링한다

- 상태: `CONFIRMED`

## DEC-044 — EFT profile-variable Quest gate는 exact read-side fact를 지원하고 미관측 값은 추측하지 않는다

- 상태: `CONFIRMED`
- exact current `ProfileVariables`가 있으면 권위값으로 사용한다.
- 없으면 제한된 audited compatibility 외에는 `Indeterminate`로 둔다.

## DEC-045 — Scanner placeholder 탭은 UI에 유지하되 실제 기능을 가장하지 않는다

- 상태: `SUPERSEDED by DEC-050/051 and completed Scanner product`

## DEC-046 — 일반 실행 시 사용자 동의형 프로그램 업데이트를 제공한다

- 상태: `CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED`
- source of truth = `Propeex/JunhyunHelper` latest public stable GitHub Release.
- current보다 strictly newer stable만 대상.
- 사용자 동의 후 exact package + SHA256 검증.
- 검증 전 current program file 변경 금지.
- temporary updater가 program-owned files만 transaction 교체.
- User Progress/LocalAppData 교체 금지.
- 현재 canonical package는 `Junhyun-Helper.zip`; versioned package는 legacy updater compatibility fallback.
- 상세: `docs/PROGRAM_UPDATE.md`, `docs/DEPLOYMENT.md`.

## DEC-047 — v1.0.0은 기능 확장이 아닌 정식 안정판 승격이다

- 상태: `CONFIRMED / PUBLIC VERIFIED`
- 역사적 v1.0.0 안정판 승격 결정.

## DEC-048 — v1 이후 새 기능=MINOR, 기존 기능 보완=PATCH

- 상태: `CONFIRMED`
- 새 사용자 기능 → MINOR.
- 기존 기능 수정/보완/버그 수정/성능·안정성 개선 → PATCH.
- 혼합 변경은 MINOR 우선.
- 상세: `docs/VERSIONING.md`.

## DEC-049 — Map donor는 source pin과 fetch origin을 분리한다

- 상태: `CONFIRMED / IMPLEMENTED`
- Map/MiniMap source identity는 gitlink commit SHA로 고정한다.
- fetch origin이 달라도 gitlink SHA가 같으면 source 변경으로 취급하지 않는다.

## DEC-050 — Scanner는 Tarkov 화면을 Item ID로 변환하는 독립 입력 subsystem이다

- 상태: `CONFIRMED / IMPLEMENTED / PARTIALLY SUPERSEDED by later Scanner decisions`
- detail/title recognition → current official Korean full-item catalog → Item ID.
- Item ID 이후 기존 JunhyunHelper data 사용.
- false positive보다 miss 선호.
- confidence 부족/ambiguity → no identity.
- 금지: game memory, DLL injection, packet interception, icon-only identity, scan-time network identity work.
- 당시 `RequiredTotal` 문구는 현재 v1.7.11 `RemainingTotal` 결정에 의해 superseded.
- 상세: `docs/SCANNER.md`.

## DEC-051 — Scanner v1.1.0은 실제 구현을 공개하고 live Tarkov 검증은 진단 evidence로 후속한다

- 상태: `CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED`
- Tarkov client capture + Display Test + Windows ko-KR OCR + Mini Scanner pipeline을 실제 제품화했다.
- runtime diagnostics와 실제 Tarkov evidence를 후속 PATCH 근거로 사용한다.
- 현재 세부 capture/diagnostic 계약은 `docs/SCANNER.md`가 우선한다.

## DEC-052 — Scanner 탭은 운용 UI와 인식 기록을 사용하고 Mini Scanner는 직접 조작 가능한 overlay다

- 상태: `CONFIRMED / IMPLEMENTED / PARTIALLY SUPERSEDED by later UI decisions`
- 당시 Scanner normal surface와 Mini Scanner interaction을 확립했다.
- 현재 v1.7.13 UI 배치/overlay/settings 계약은 최신 결정이 우선한다.

## DEC-053 — Scanner 상세창 확정은 multi-candidate semantic validation을 사용한다

- 상태: `CONFIRMED / IMPLEMENTED`
- geometry는 후보 생성/순위 역할만 한다.
- RED-X component + rectangle/edge fallback candidate를 사용한다.
- current official item으로 안전하게 resolve된 candidate만 identity path를 통과한다.
- matcher threshold/top1-top2 margin을 인식률 때문에 낮추지 않는다.

## DEC-054 — Scanner 시장/필요 수량 표시의 권위 데이터를 명시한다

- 상태: `CONFIRMED / PARTIALLY SUPERSEDED by v1.7.11 maintenance decision`
- 최고 상점가 = trusted non-flea RUB-equivalent max.
- flea 평균 = positive `avg24hPrice`.
- market/dimension 오류는 affected field만 fail closed.
- 당시 `RequiredTotal` current-needed 의미는 **superseded**.
- 현재 사용자 표시값 = `NeededItems[itemId].RemainingTotal`.

## DEC-055 — Scanner v1.2.0은 title recognition을 anchor/visual recovery로 확장하고 진단/one-shot을 제공한다

- 상태: `CONFIRMED / IMPLEMENTED`
- Windows ko-KR OCR primary.
- OCR miss/damage에만 conservative current-catalog-bounded visual recovery.
- current catalog 밖 Item 생성 금지.
- latest diagnostic frame은 memory 중심.
- one-shot은 local healthy catalog만 사용하고 scan-time network를 시작하지 않는다.

## DEC-056 — Scanner는 live threshold를 추측하지 않고 deterministic reliability를 hardening한다

- 상태: `CONFIRMED / IMPLEMENTED`
- font/visual cache generation binding, bounded cache, lifecycle/race hardening.
- 실제 evidence 없이 detector/OCR/visual acceptance threshold를 완화하지 않는다.

## DEC-057 — Scanner catalog disk load와 network refresh는 하나의 mode-transition ordering boundary를 사용한다

- 상태: `CONFIRMED / IMPLEMENTED`
- `RefreshAsync`와 `LoadCacheAsync`가 동일 in-memory state를 교체하므로 writer ordering을 공유한다.
- older GameMode writer가 newer final state를 덮어쓰지 못하게 한다.
- 당시 `RequiredTotal` 비변경 문구는 역사적이며 current needed 의미는 v1.7.11이 supersede한다.

## DEC-058 — Scanner title ROI는 실제 inspect-header frame이 소유한다

- 상태: `CONFIRMED / IMPLEMENTED`
- `HEADER_FRAME_LOCKED >= 0.68`을 production OCR gate로 요구한다.
- red close-X + neutral header/frame + bounded magnifier lane + dark title field/text evidence를 결합한다.
- partial/failed lock은 Item identity path에 진입하지 않는다.
- 당시 `RequiredTotal` 문구는 current needed 의미의 현행 권위가 아니다.

## DEC-059 — Scanner recognition/diagnostics는 actual Tarkov evidence 기반으로 hardening한다

- 상태: `CONFIRMED / IMPLEMENTED`
- unknown glyph를 특정 문자로 product-wide 강제 치환하지 않는다.
- magnifier/close morphology와 header ownership을 강화한다.
- `HEADER_FRAME_LOCKED`, valid magnifier/close, conservative matcher/visual safety를 유지한다.
- 사용자가 명시적으로 저장할 때만 diagnostic PNG를 export한다.
- 당시 `RequiredTotal`/schema 문구는 historical v1.3.4 state이며 현재 계약은 최신 Scanner/STATE 문서가 우선한다.

---

# 3. DEC-059 이후 standalone 결정 — 현재 우선순위

DEC 번호를 과거 순번에 억지로 이어 붙이지 않고, 저장소에 별도 결정 문서로 확정된 후속 결정을 current authority chain으로 관리한다.

## Product complete / maintenance mode

- `docs/DECISION_PRODUCT_COMPLETE_2026-08-26.md`
- 현재 요구사항 범위 제품은 완성 상태이며 기본 방향은 maintenance.
- 새 사용자 기능은 사용자가 명시적으로 새 제품 요구사항으로 결정할 때 시작.

## Scanner durable data / hotkeys

- `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`
- normal monitoring은 durable automatic Ground Truth를 만들지 않음.
- user-reviewed explicit save만 durable Ground Truth.
- Scanner/Map configurable hotkey의 current storage/input 경계 확립.

## v1.7.8 raid header ownership

- `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`
- raid inventory horizontal-line bleed recovery는 strong RED-X evidence 뒤에서만 진입하고 기존 semantic gate를 모두 다시 요구.

## v1.7.9 Mini Scanner presentation authority

- `docs/DECISION_V1.7.9_MINI_SCANNER_SHOW_2026-08-26.md`
- Item ID 확정 뒤 auxiliary inventory-header OCR은 confirmed Item presentation을 veto하지 못함.

## v1.7.10 cross-environment Scanner normalization

- `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`
- 특정 PC/GPU/HDR 분기 대신 measured title luminance profile 기반 bounded normalization.
- normal OCR success path는 추가 normalization/OCR 비용 없음.
- normalization은 identity proof가 아님.

## v1.7.11 maintenance

- `docs/DECISION_V1.7.11_MAINTENANCE.md`
- Scanner current needed = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`.
- configurable Map/Scanner hotkey는 extra Ctrl/Alt/Shift compatibility + most-specific-wins.
- MiniMap first-open sync / size persistence.
- standard explanatory WPF ToolTip 전역 비표시.

## Long-term maintenance audit / v1.7.12

- `docs/DECISION_LONG_TERM_MAINTENANCE_AUDIT_2026-08-27.md`
- `docs/DECISION_V1.7.12_MAINTENANCE.md`
- shared Desktop presentation infrastructure owner = `MainWindow.OnInitialized`.
- Ammo internal presentation initialization = `AmmoPage.OnInitialized` + Loaded-priority dispatcher.
- evidence 없는 speculative performance/cache/reflection cleanup 금지.

## v1.7.13 UI simplification

- `docs/DECISION_V1.7.13_UI_SIMPLIFICATION.md`
- 상태: **IMPLEMENTED / PUBLIC VERIFIED v1.7.13**.
- Items Quest/Hideout purpose selector 제거.
- Ammo controls 정리 + detail 기본 접힘.
- Map marker/settings same-launcher toggle, trail surface 제거, hotkey explanation 제거.
- Scanner display settings 즉시 저장; hotkey editing을 normal Scanner surface로 분리.
- Scanner needed search source = existing `NeededItems[itemId].Sources`.
- user-facing settings/edit surface는 가능한 경우 MainWindow internal overlay interaction 사용.
- Scanner current correction 우측 command 영역.
- Scanner recognition constants/matcher/visual/pacing, Map donor, Game Content/LKG 계약은 변경하지 않음.

---

# 4. 현재 결정 확인 방법

현재 상태를 복구할 때 다음 순서로 읽는다.

1. 제품 요구사항: `docs/PRODUCT.md`
2. 현재 구현/릴리즈 상태: `docs/STATE.md`, `docs/CURRENT_STATE.md`
3. 최신 UI 제품 결정: `docs/DECISION_V1.7.13_UI_SIMPLIFICATION.md`
4. 기술 경계: `docs/ARCHITECTURE.md`
5. 개발자 구현 지도: `docs/DEVELOPER_REFERENCE.md`
6. 유지보수 안전 계약: `docs/MAINTENANCE_CONTRACTS.md`
7. Scanner 제품/기술 계약: `docs/SCANNER.md`
8. Scanner 검증 gate: `docs/SCANNER_TEST_PLAN.md`
9. Program Update: `docs/PROGRAM_UPDATE.md`
10. 배포: `docs/DEPLOYMENT.md`
11. Quest prerequisite: `docs/QUEST_PREREQUISITE_SEMANTICS.md`
12. Map 세부 계약: `docs/MAP_PRODUCT_REQUIREMENTS.md`
13. 기존 구현 참고 정책: `docs/REFERENCE_POLICY.md`
14. DEC-001~029 원문: `docs/DECISIONS_HISTORY_THROUGH_2026-08-09.md`

과거 release/decision 문서의 당시 값은 역사적 evidence다. 현재 제품 의미와 충돌하면 최신 confirmed decision + current canonical docs가 우선한다.
