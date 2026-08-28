# v1.8.0 Scanner 아이템 정보 DB 결정

상태: **CONFIRMED / IMPLEMENTED / PUBLIC STABLE VERIFIED**

## 목적

Scanner 탭의 아이템 검색을 단순 가격/필요 개수 조회가 아니라, 한 아이템의 기본 정보·수급처·사용처를 한 화면에서 확인할 수 있는 로컬 아이템 정보 DB로 확장한다.

이 기능은 Scanner OCR/인식 정책을 확장하거나 완화하는 기능이 아니다. Scanner가 확정한 Tarkov item ID 또는 사용자가 검색에서 선택한 item ID를 기준으로 이미 검증된 Game Content와 Scanner catalog를 결합해 표시한다.

## 사용자 요구사항

선택한 아이템에 대해 다음 정보를 제공한다.

### 기본 정보
- 아이템 이미지/이름
- 종류
- 크기
- 무게
- 플리마켓 거래 가능 여부
- 기본 가격
- 기존 플리마켓 평균가
- 기존 최고 상인 판매가
- 기존 현재 필요 개수

### 수급처
- 상인 현금 구매: 상인, 충성도 레벨, 가격/화폐, 구매 제한, upstream이 제공하는 재고 갱신 시각
- 상인 교환: 상인, 충성도 레벨, 요구 재료/수량, 결과 수량, 구매 제한
- 은신처 제작: 시설, 레벨, 재료/수량, 비소모 도구 구분, 결과 수량, 제작 시간
- 플리마켓: 현재 획득 소스로 확인 가능한 경우 평균가와 함께 표시
- 다른 canonical 수급 관계가 없는 경우 레이드 획득으로 표시

### 사용처
- 퀘스트 요구 아이템: 퀘스트명, 요구 수량, FIR 요구 여부
- 은신처 업그레이드: 시설명, 목표 레벨, 요구 수량/FIR
- 제작 재료 사용처: 시설/레벨, 결과 아이템/수량, 전체 재료
- 상인 교환 재료 사용처: 상인/충성도 레벨, 결과 아이템/수량, 전체 재료
- 기존 `필요한 곳`은 현재 프로필의 ItemsWorkspace 계산 결과를 계속 사용하며 별도로 재계산하지 않는다.

관련 제작·교환 아이템은 같은 Scanner 아이템 상세로 이동할 수 있고, 퀘스트/은신처 사용처는 기존 제품 화면으로 이동할 수 있다.

## 데이터 소유권과 갱신

관계 데이터는 별도 Scanner API나 검색 시 네트워크 요청으로 만들지 않는다.

정상 Game Content 업데이트가 이미 내려받는 `Items`, `Barters`, `Crafts`, `Traders`, `Tasks`, `Hideout` 데이터에서 canonical relationship graph를 만든다.

업데이트 순서는 기존 계약을 유지한다.

`download -> parse -> canonical build -> integrity/completeness validation -> activate`

관계 데이터 참조/수량/가격이 잘못된 후보는 활성화하지 않으며 기존 LKG를 덮지 않는다.

Snapshot schema는 v8이며 v3-v7은 계속 읽을 수 있다. 구형 snapshot에는 관계 그래프가 없다는 사실을 유지하고, 이를 실제 관계가 없는 것으로 오해해 `레이드`라고 추정하지 않는다. 정상적인 새 Game Content 업데이트 후 관계 DB가 활성화된다.

## 구조

- `src/JunhyunHelper.Core/Items/ItemRelationshipCatalog.cs`
  - 제작/교환/상인 구매 관계의 단일 canonical 정의
  - 역방향 사용처는 원본 관계에서 계산
- `src/JunhyunHelper.Infrastructure/TarkovJson/Items/TarkovItemRelationshipImporter.cs`
  - 이미 다운로드된 static JSON을 관계 그래프로 변환
- `src/JunhyunHelper.Infrastructure/Validation/ItemRelationshipIntegrityValidator.cs`
  - item/trader/station/quest/currency reference 및 수량·가격·제한 무결성 검증
- `src/JunhyunHelper.Infrastructure/Storage/ContentSnapshotStore.cs`
  - v8 relationship graph 영속화, v3-v7 읽기 호환
- `src/JunhyunHelper.Desktop/Scanner/ScannerCoordinator.ItemRelationships.cs`
  - canonical 관계와 Quest/Hideout 사용처를 표시 모델로 projection
- `src/JunhyunHelper.Desktop/Scanner/ScannerPage.ItemRelationships.cs`
  - 기본 정보/사용처/수급처 표시 및 관련 항목 이동

## Scanner 안전 경계

다음은 변경하지 않는다.
- structural floor 0.34
- `HEADER_FRAME_LOCKED` floor 0.68
- continuous candidate cap 8
- one-shot candidate cap 12
- 200ms observation target
- OCR matcher / visual recovery acceptance 정책
- 이전 프레임이나 mapped data를 identity proof로 사용하지 않는 규칙

아이템 DB 데이터는 item identity가 확정된 이후에만 표시 계층에서 사용한다.

## 검증 결과

v1.8.0은 요구한 전체 release gate를 통과했다.

```text
exact product source/tag: 8042e4612a54a6ec395a69d1be0700d844a1b210
exact-main CI: 33130057533 — SUCCESS
413 passed / 0 failed / 0 skipped
Release build: SUCCESS
win-x64 self-contained single-file publish: SUCCESS
actual published EXE Product UI / Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / portable root / package checksum: SUCCESS
Release workflow: 33130212711 — SUCCESS
public release: v1.8.0 / id 378197672
public ZIP SHA-256: 4ecaf65068153a38a7a8613cfe2ae673aec191563f999f1cfbd10cb93d9437e0
tag/ref/release/latest/public asset readback: SUCCESS
```

정확한 공개 릴리즈 증거는 `docs/RELEASE_1.8.0.md`와 `docs/.release-v1.8.0-status.json`에 기록한다.
