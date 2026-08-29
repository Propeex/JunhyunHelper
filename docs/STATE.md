# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-29 KST  
상태: **v1.10.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태이며 기본 운영 모드는 유지보수다.

주요 기능은 GameMode별 Profile/User Progress, Quest/Hideout, Needed Items/Inventory, Items, Ammo, Map+MiniMap, Game Content 안전 업데이트, 사용자 동의형 Program Update, Scanner+Mini Scanner, Ground Truth/diagnostics, Scanner 아이템 정보 DB, Favorites/Recents다. Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper`는 제품 사양의 권위가 아니다. Map/MiniMap에 한해 검증된 pinned donor source를 제한적으로 사용한다.

## 2. 현재 public stable

```text
version: v1.10.1
exact product release source/tag target:
c444a1e26793e15c075875159f6605d8a99cf7f9
PR CI run: 33253141127 — SUCCESS
exact-main CI run: 33253293015 — SUCCESS
release workflow run: 33253438908 — SUCCESS
release id: 378982127
439 passed / 0 failed / 0 skipped
published UTC: 2026-08-29T12:49:03Z
```

Public release package:

```text
Junhyun-Helper.zip
asset id: 535210900
bytes: 80,540,164
SHA-256:
c37c00a5e5ecdc431d6b26775d73682cabf17e4310533065c88e2d58d8f14922

SHA256SUMS.txt
asset id: 535210901
bytes: 86
SHA-256:
d32a6d50b60b512fa446d708d5d8ba75addad854c1e63c51378b318fbd6116c3
```

Exact-main GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9715065803
artifact archive bytes: 241,555,171
artifact archive SHA-256:
17fa98916dac423dd304ca59a5769f2fc61851d391ff3c4df89ceaaa25d3b663
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.10.1`
- release target = exact product release source
- tag ref object = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- ZIP + checksum assets present

공식 공개 증거:

- `docs/RELEASE_1.10.1.md`
- `docs/.release-v1.10.1-status.json`
- `docs/RELEASE_NOTES_V1.10.1.md`
- `docs/DECISION_V1.10.1_STABILITY_AUDIT.md`

**중요:** 공개 뒤 생성되는 documentation/test/workflow-only commit은 v1.10.1 product release source가 아니다. 공개 source/tag/assets는 `c444a1e26793e15c075875159f6605d8a99cf7f9` 기준의 immutable historical product release다.

## 3. v1.10.1 안정성 감사 결과

### MainWindow header lifecycle ownership

기존 `MainWindow.HeaderStatusPolish.cs`는 메인 헤더의 제품 UI 보강을 static `EventManager.RegisterClassHandler(... Loaded ...)`에 의존했다. 사용자-visible 결과는 맞았지만 제품 창의 초기화 책임이 routed event/type-level registration에 숨어 있었다.

v1.10.1은 다음으로 정리했다.

- `MainWindow.OnInitialized`가 `ScheduleHeaderStatusPolish()`를 명시적으로 호출한다.
- visual-tree 변경은 `DispatcherPriority.Loaded`에서 한 번 적용한다.
- static class-level Loaded handler와 registration sentinel을 제거했다.
- `DependencyPropertyDescriptor.AddValueChanged` subscription은 `MainWindow.OnClosed`에서 `RemoveValueChanged`로 해제한다.

헤더 사용자 의미는 변경하지 않았다.

- 헤더에는 버전 정보만 표시
- cleanup item 존재 시 Items 탭 우측 상단 오렌지 점 표시
- update progress는 dedicated overlay 사용

`DesktopStartupWiringContractTests`가 explicit initialization / cleanup ownership과 class handler 재유입 금지를 회귀 계약으로 고정한다.

### repository/package maintenance

- `.github/scripts/finalize-v121.py`는 v1.2.1 당시 고정 SHA/run/asset 값을 문서에 반영하던 일회성 helper였고 현재 build/test/CI/Release/current docs 실행 경로에서 사용되지 않아 제거했다.
- v1.2.1 역사적 증거는 기존 릴리즈/문서에 유지한다.
- `packaging/FIRST_RUN_KO.txt`의 장기간 누적 historical changelog를 정리했다. 패키지는 설치 안내, 현재 릴리즈, 직전 핵심 변경만 담고 전체 변경 역사/검증 authority는 GitHub Releases와 `docs/`다.

### 감사 후 유지한 구조

실제 오류 증거 없이 추가 변경할 경우 안정성 이득보다 회귀 위험이 크다고 판단해 다음 구조는 그대로 유지했다.

- 사용자 mutation의 UI busy serialization 및 DB-success 후 cache update
- atomic JSON same-directory temp + write-through + flush + readable backup promotion
- Program Update latest-stable/checksum/immutable release 및 failure recovery
- Scanner runtime/context monitor serialization/disposal
- Scanner recognition acceptance 정책
- Game Content relationship LKG/completeness/fail-closed
- Map/MiniMap donor semantics

### post-release disposal / shutdown verification

공개 뒤 장기 수명주기와 종료 경계를 추가 감사했다.

`DesktopStartupWiringContractTests.ProductLifetime_DisposesOwnedLongLivedServices`가 MainWindow → DesktopServices → Scanner/shared HTTP, App → Program Update/Scanner diagnostic retention의 정상 disposal ownership을 고정한다.

또한 `.github/workflows/shutdown-race-ci.yml`이 실제 Release publish EXE를 대상으로 다음 상황을 재현한다.

```text
async Product/Map smoke 진행 중
→ full smoke success marker가 아직 없음 확인
→ 정상 Main Window CloseMainWindow 요청
→ 7초 이내 process 종료
→ exit code 0
→ Map smoke/startup unhandled diagnostic 없음
```

현재 구조가 이 테스트를 통과했으므로, 결함 증거 없는 global lifetime CTS/cancellation 전파 리팩터링은 하지 않는다. 정상 경계를 더 복잡하게 바꾸기보다 현 ownership을 테스트와 실제 runtime evidence로 고정한다.

Post-release maintenance evidence:

```text
PR #220 CI: 33254932421 — SUCCESS
#220 exact-main CI: 33255074971 — SUCCESS
#220 Release verification: 33255208324 — SUCCESS
PR #221 CI: 33255650930 — SUCCESS
PR #221 Shutdown Race CI: 33255651032 — SUCCESS
latest non-documentation maintenance head:
22701e5419bca2995d442599fad646abcd484007
exact-main CI: 33258220788 — SUCCESS
exact-main Shutdown Race CI: 33258220786 — SUCCESS
Release immutable verification: 33258352426 — SUCCESS
current deterministic tests: 440 passed / 0 failed / 0 skipped
```

`/releases/latest`와 `refs/tags/v1.10.1`을 다시 읽어 공개 release ID, target/tag, asset IDs, ZIP bytes/digest가 원래 v1.10.1 공개 값과 동일함을 확인했다.

## 4. Scanner / Game Content 유지 계약

v1.10.1은 다음을 변경하지 않았다.

- Scanner OCR threshold
- matcher / candidate cap
- visual corroboration / recovery acceptance
- capture geometry / Ground Truth
- Scanner Item ID identity policy
- Game Content schema / LKG / 50% completeness / fail-closed
- Scanner Favorites / Recents 의미
- canonical item-open boundary
- Ammo filtering / favorite persistence
- Factory floor / Map marker / viewport 의미
- v1.10.0 MiniMap same-window reopen synchronization

Scanner 표시 authority:

```text
needed quantity = ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
needed source   = ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

Price/needed/source/relationship metadata는 recognition identity proof에 사용하지 않는다.

Game Content schema는 v8이며 v3~v8 read compatibility를 유지한다. v1.10.1은 external Game Content importer/schema/validator 의미를 변경하지 않았다.

## 5. Schema / compatibility

```text
Desktop target version: 1.10.1
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v7
Scanner catalog cache: v1~v4 readable, v4 written
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

```text
v1.10.0 → v1.10.1 mandatory Game Content update: none
v1.10.0 → v1.10.1 user.db migration: none
```

## 6. 아키텍처 / ownership

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

- Core: canonical domain과 deterministic calculation/policy.
- Application: 사용자 use case와 authoritative mutation/workspace orchestration.
- Infrastructure: HTTP/source parsing, persistence, content/update I/O, relationship import/validation.
- Desktop: WPF UI, Scanner capture/OCR/runtime/diagnostics, Map bridge.
- Map/MiniMap donor: 제한적 compile-link 예외. donor updater/content ownership은 사용하지 않는다.

Pinned donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

## 7. 검증 원칙

```text
실사용 오류 / Tarkov 변화 / reviewed Scanner evidence
→ actual source/log/runtime 확인
→ failure stage/영향 범위 분류
→ 최소 수정
→ deterministic regression
→ published executable runtime smoke
→ 외부 schema/meaning 변경 시 current Regular/PvE live probe
→ exact-main release gate
```

사용자-visible WPF 변경은 source assertion만으로 완료 선언하지 않는다. 실제 published executable control tree/runtime evidence를 확보한다. 장기 async/lifecycle 종료 경계도 관련 변경 시 정상 Main Window close를 실제 published EXE에서 가능한 범위까지 검증한다.

v1.10.1 제품 릴리즈는 PR CI `33253141127`과 exact-main CI `33253293015`에서 439/439 tests, Release publish, actual Product UI/Ammo/Map/Factory/MiniMap/Scanner smoke, graceful shutdown, clean portable root, package audit를 모두 통과했다. Release workflow `33253438908`도 성공했다.

그 뒤 tests/workflow-only maintenance까지 반영한 current main 검증에서는 440/440 deterministic tests, 기존 full published-EXE smoke, 별도 active-async shutdown-race smoke가 모두 성공했다. 제품 source/tag/assets는 재발행하지 않았다.

## 8. 사용자 실사용 상태

v1.10.1의 CI 및 published EXE 자동 검증은 완료됐다. 사용자의 실제 PC/Tarkov 플레이 환경에서의 직접 실사용은 아직 별도 확인 전이다. 실사용에서 확인되는 문제는 자동 smoke보다 높은 우선순위의 회귀 증거로 처리한다.

## 9. 다음 작업

v1.10.1 릴리즈 배치에 남은 제품 작업은 없다. 기본 운영 모드는 유지보수다. 새 기능은 사용자가 명시적으로 제품 요구사항으로 결정할 때만 시작한다.

다음 세션은 `README.md` → `docs/CURRENT_STATE.md` → `docs/STATE.md` → `docs/PRODUCT.md` → `docs/DECISIONS.md` → 관련 전문 문서 → current GitHub state 순으로 복구한다.