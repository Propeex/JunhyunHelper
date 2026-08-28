# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.8.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태입니다. 새로운 실제 회귀·Tarkov 호환성 변화·사용자가 명시적으로 확정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 Scanner 인식 기준 조정을 시작하지 않습니다.

공식 현재 상태:

- `docs/CURRENT_STATE.md` — 짧은 현재 상태 인덱스
- `docs/STATE.md` — 운영 기준과 exact release evidence
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/ARCHITECTURE.md` — 기술 경계/데이터 흐름
- `docs/DEVELOPER_REFERENCE.md` — 다음 개발 세션용 구현 지도
- `docs/MAINTENANCE_CONTRACTS.md` — 유지보수 불변 계약

Scanner 전문 문서:

- `docs/CURRENT_SCANNER_WORK.md`
- `docs/SCANNER.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/SCANNER_GROUND_TRUTH.md`

## 현재 공개 릴리즈

```text
version: v1.8.0
Desktop target version: 1.8.0
exact product release source/tag target: 8042e4612a54a6ec395a69d1be0700d844a1b210
main CI: 33130057533 — SUCCESS
Release workflow: 33130212711 — SUCCESS
release id: 378197672
stable asset: Junhyun-Helper.zip
asset id: 533051783
bytes: 80,520,114
SHA-256: 4ecaf65068153a38a7a8613cfe2ae673aec191563f999f1cfbd10cb93d9437e0
413 passed / 0 failed / 0 skipped
```

GitHub `/releases/latest` 및 `refs/tags/v1.8.0` readback에서 v1.8.0이 `draft=false`, `prerelease=false`, latest stable이며 release target과 tag ref가 exact product release source와 일치함을 확인했습니다. 공개 ZIP digest도 exact main-CI package SHA-256과 일치합니다.

공식 릴리즈 기록:

- `docs/RELEASE_1.8.0.md`
- `docs/RELEASE_NOTES_V1.8.0.md`
- `docs/.release-v1.8.0-status.json`
- `docs/DECISION_V1.8.0_SCANNER_ITEM_DATABASE.md`

이 README와 이후 documentation-only commit은 v1.8.0 제품 릴리즈 소스가 아닙니다. v1.8.0 product source/tag/assets는 위 `8042e461...` 기준의 immutable historical release입니다.

## 설치 / 실행

배포 형태는 Windows x64 portable ZIP입니다.

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

- Windows x64
- .NET 10 WPF
- self-contained single-file executable
- 별도 .NET Runtime 설치 불필요
- installer 없음
- 일반 사용에 관리자 권한 불필요

사용자 데이터는 프로그램 폴더가 아니라 `%LocalAppData%/JunhyunHelper` 아래에 저장됩니다.

## 주요 기능

- GameMode별 Profile / User Progress
- Quest availability / prerequisite / special trader / profile-variable
- Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth 교정 / diagnostics / regression dataset
- Scanner 아이템 정보 DB
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## v1.8.0 — Scanner 아이템 정보 DB

Scanner 탭의 기존 아이템 검색을 Item ID 중심의 로컬 정보 DB로 확장했습니다.

선택한 아이템에서 다음을 확인할 수 있습니다.

- 종류, 크기, 무게, 플리마켓 거래 가능 여부, 기본 가격
- 기존 flea 평균가, 최고 상인 판매가, 현재 필요 개수
- 퀘스트 요구 수량/FIR
- 은신처 업그레이드 요구 수량/FIR
- 제작 재료 사용처와 전체 재료·도구·결과 수량
- 상인 교환 재료 사용처와 전체 재료·결과 수량
- 상인 현금 구매 가격/화폐/충성도 레벨/구매 제한/제공되는 재고 갱신 시각
- 상인 교환과 은신처 제작 수급처
- 제작 시간과 비소모 도구
- 플리마켓 수급
- 다른 canonical 수급처가 없는 아이템의 레이드 획득 표시

제작·교환 관계에 표시된 아이템은 클릭해 같은 Scanner 상세로 이동할 수 있으며, 퀘스트/은신처 사용처는 기존 제품 화면으로 이동할 수 있습니다.

관계 데이터는 검색 순간 외부 API를 호출해 만들지 않습니다.

```text
Game Content Update
→ Items / Barters / Crafts / Traders / Tasks / Hideout
→ canonical relationship graph
→ integrity/completeness validation
→ local v8 snapshot
→ Scanner item detail
```

Content schema는 v8이며 v3~v8을 읽을 수 있습니다. 구형 snapshot에 관계 데이터가 없다는 사실과 실제 관계가 없는 아이템을 구분합니다.

## 공통 overlay interaction

현재 주요 user-facing editor/settings surface는 다음 interaction을 공유합니다.

```text
launcher
→ MainWindow shared overlay
→ same launcher / backdrop / common X → dismiss
```

현재 적용 surface:

- Profile Edit
- Scanner Settings
- Scanner Advanced
- Map / MiniMap Settings

Child editor의 validation/save authority는 overlay host가 재구현하지 않습니다.

## Scanner

Scanner는 Tarkov 화면 픽셀을 현재 공식 한국어 Tarkov full-item catalog의 Item ID에 연결하는 closed-domain recognizer입니다.

```text
Tarkov window pixels
→ detail rectangle proposals
→ inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user substitution
→ conditional environment-aware title normalization
→ conservative official-catalog matching / bounded recovery
→ optional current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

### Scanner 안전 기준

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive보다 miss를 선호합니다.
- geometry/environment normalization은 Item identity proof가 아닙니다.
- stale/cross-frame OCR 또는 visual result를 current Item identity proof로 사용하지 않습니다.
- Item ID 확정 전 price/needed/slot/source/relationship/previous-frame metadata를 identity evidence로 사용하지 않습니다.
- scan 순간 identity 결정을 위해 network 요청을 시작하지 않습니다.
- reviewed evidence 없이 threshold/candidate/matcher/visual acceptance를 낮추지 않습니다.

Scanner 표시 authority:

```text
needed quantity = ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
needed source   = ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

두 값 모두 Item ID 확정 뒤 presentation에만 사용합니다. v1.8.0 관계 DB도 동일하게 Item ID 확정 뒤 presentation에서만 사용됩니다.

## Game Content 안전 업데이트

Game Content는 User Progress와 분리합니다.

```text
remote source
→ download / parse
→ canonical build
→ integrity/completeness validation
→ activate
```

- failed candidate가 last-known-good content를 덮어쓰지 않습니다.
- normal snapshot shrink guard는 기존 healthy baseline의 50%입니다.
- collection schema drift는 fail closed합니다.
- Wiki Ballistics enrichment는 fail-soft입니다.
- v8 item relationship 참조/가격/수량 무결성도 activation 전에 검증합니다.
- update failure가 `user.db`를 변경하지 않습니다.

## Map / MiniMap donor

Map/MiniMap은 다음 public donor revision을 pinned source로 사용합니다.

```text
SIGDrone/Tarkov-Helper
d933792b6042a51cea38dc44b686a096fe30de67
```

기존 `Propeex/Tarkov-Helper` 전체 구현을 준현 헬퍼의 제품 요구사항으로 간주하지 않습니다. Map/MiniMap의 검증된 donor source만 제한적으로 사용하며 JunhyunHelper 제품 요구사항은 first-party bridge/customization 경계에서 적용합니다.

## 개발 / 유지보수 원칙

새 작업은 저장소의 현재 공식 문서와 GitHub 상태를 먼저 확인한 뒤 시작합니다.

```text
실사용 오류 / Tarkov 변화 / reviewed Scanner evidence
→ root cause와 영향 범위 확인
→ 최소한의 일관된 수정
→ deterministic regression
→ full Windows release gate
→ 필요한 경우 PATCH release
```

Published stable release는 공개 후 교체하지 않습니다. 같은 version에서 documentation-only main commit이 다른 ProductVersion metadata/bytes를 만들더라도 이미 공개된 ZIP/tag/source를 덮어쓰지 않습니다.

현재 v1.8.0 릴리즈 배치에 남은 제품 개발 작업은 없습니다. 기본 운영 모드는 유지보수입니다.
