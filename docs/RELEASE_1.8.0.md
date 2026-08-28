# RELEASE — 준현 헬퍼 v1.8.0

기록일: 2026-08-28 KST  
상태: **PUBLIC STABLE / VERIFIED / IMMUTABLE PRODUCT RELEASE**

이 문서는 v1.8.0의 exact source, CI, Windows 배포물, GitHub Release 및 public readback 증거를 기록한다.

## 1. 제품 변경 범위

v1.8.0은 Scanner 탭의 기존 아이템 검색을 **로컬 아이템 정보 DB**로 확장한 사용자 기능 릴리즈다.

선택한 아이템에 대해 다음 정보를 제공한다.

- 기본 정보: 종류, 크기, 무게, 플리마켓 거래 가능 여부, 기본 가격
- 기존 Scanner 표시 정보: 아이콘/공식 이름, 플리마켓 평균가, 최고 상인 판매가, 현재 필요 개수
- 퀘스트 사용처: 퀘스트명, 요구 수량, FIR 여부
- 은신처 업그레이드 사용처: 시설, 목표 레벨, 요구 수량, FIR 여부
- 제작 재료 사용처: 시설/레벨, 결과 아이템/수량, 전체 재료와 비소모 도구
- 상인 교환 재료 사용처: 상인/충성도 레벨, 결과 아이템/수량, 전체 재료
- 수급처: 상인 현금 구매, 상인 교환, 은신처 제작, 플리마켓, 다른 canonical 수급처가 없을 때 레이드 획득
- 상인 현금 구매의 가격/화폐/충성도 레벨/구매 제한/upstream 제공 재고 갱신 시각
- 상인 교환의 요구 재료/수량, 결과 수량, 구매 제한
- 은신처 제작의 재료/수량, 비소모 도구, 결과 수량, 제작 시간
- 관계 아이템을 눌러 같은 Scanner 상세로 이동
- 퀘스트/은신처 사용처에서 기존 제품 화면으로 이동
- 긴 상세 정보의 세로 스크롤

관계 데이터는 Scanner 검색 시 외부 API를 호출하지 않는다. 기존 Game Content 업데이트가 내려받는 Items/Barters/Crafts/Traders/Tasks/Hideout에서 canonical relationship graph를 만들고 검증·저장한다.

Content snapshot schema는 v8로 올라갔고 기존 v3~v7은 계속 읽을 수 있다. 구형 snapshot의 `관계 데이터 없음`과 실제 `관계가 없음`을 구분해 잘못된 raid fallback을 표시하지 않는다.

기존 `필요 개수`와 `필요한 곳`의 authority는 계속 `ItemsWorkspace`가 소유한다.

Scanner recognition의 structural/header/matcher/visual policy, Map/MiniMap donor revision, Game Content LKG 계약은 변경하지 않았다.

상세 제품 결정:

- `docs/DECISION_V1.8.0_SCANNER_ITEM_DATABASE.md`
- `docs/RELEASE_NOTES_V1.8.0.md`

## 2. PR 최종 검증

PR #205 final head:

```text
86bc8354bca5176d5ea02dcf22283e1399070ecb
```

Final PR CI:

```text
run: 33129842327
CI #2079
result: SUCCESS
413 passed / 0 failed / 0 skipped
Release build: SUCCESS
win-x64 self-contained single-file publish: SUCCESS
Product UI / Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS
```

PR merge-ref artifact는 merge metadata를 포함하므로 public release artifact가 아니다. 공개 제품은 아래 exact `main` source에서 다시 생성했다.

## 3. Exact product source

```text
version: v1.8.0
exact product release source/tag target:
8042e4612a54a6ec395a69d1be0700d844a1b210
```

이 SHA는 PR #205의 merge commit이며 공개 v1.8.0 tag와 release target의 권위 source다.

## 4. Exact-main CI

```text
run: 33130057533
CI #2080
result: SUCCESS
source: 8042e4612a54a6ec395a69d1be0700d844a1b210
```

### Build / tests

```text
Desktop Release build: SUCCESS
413 passed / 0 failed / 0 skipped
```

컴파일 warning은 pinned donor source의 기존 허용 warning 범위만 존재했고 JunhyunHelper build error는 0이다.

### Publish identity

```text
project version: 1.8.0
ProductVersion:
1.8.0+8042e4612a54a6ec395a69d1be0700d844a1b210
FIRST_RUN first line:
준현 헬퍼 v1.8.0 — Windows x64
```

Publish root audit:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

- self-contained single-file publish
- root DLL clutter 없음
- PDB 없음
- nested archive 없음
- forbidden legacy dependency 없음
- mutable runtime Logs folder가 portable root에 남지 않음

### Actual published EXE smoke

```text
Startup: SUCCESS
rendered Product UI: SUCCESS
Main Map: SUCCESS
Factory map path: SUCCESS
MiniMap: SUCCESS
graceful Main Window close: SUCCESS
process termination: SUCCESS
clean portable root: SUCCESS
```

## 5. Main-CI release package

```text
Junhyun-Helper.zip
bytes: 80,520,114
SHA-256:
4ecaf65068153a38a7a8613cfe2ae673aec191563f999f1cfbd10cb93d9437e0
```

Main-CI GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9669936143
artifact archive bytes: 241,489,980
artifact archive SHA-256:
42021da59b8486511b1a3f6d0fd5b2b601185c4e4ce4e714818b987a68ef7545
```

## 6. Release workflow

```text
run: 33130212711
Release #50
result: SUCCESS
source: 8042e4612a54a6ec395a69d1be0700d844a1b210
input main-CI run: 33130057533
input artifact id: 9669936143
input artifact bytes: 241,489,980
input artifact digest:
42021da59b8486511b1a3f6d0fd5b2b601185c4e4ce4e714818b987a68ef7545
```

Release workflow가 exact main-CI artifact를 다시 내려받아 digest를 검증한 뒤 ProductVersion, FIRST_RUN, package checksum을 확인하고 v1.8.0 stable release를 게시했다.

`actions/download-artifact@v8`에서 upstream Node `Buffer()` deprecation warning이 1회 출력되었으나 artifact digest 검증과 release publication은 성공했다. 현재 repo product/CI blocker가 아니며 upstream action 경고로 monitor-only 취급한다.

## 7. Public GitHub Release readback

```text
release id: 378197672
name: 준현 헬퍼 v1.8.0
tag: v1.8.0
release target: 8042e4612a54a6ec395a69d1be0700d844a1b210
draft: false
prerelease: false
latest stable: true
published UTC: 2026-08-28T00:36:14Z
```

Public asset:

```text
Junhyun-Helper.zip
asset id: 533051783
bytes: 80,520,114
digest:
sha256:4ecaf65068153a38a7a8613cfe2ae673aec191563f999f1cfbd10cb93d9437e0
```

Checksum asset:

```text
SHA256SUMS.txt
asset id: 533051782
bytes: 86
digest:
sha256:6432c08261b1ca6dd093ff9e1864619951162300585d5cb2db082731bff3d3a1
```

Public ZIP digest는 exact main-CI package SHA-256과 정확히 일치한다.

## 8. Tag / latest readback

```text
refs/tags/v1.8.0
→ commit 8042e4612a54a6ec395a69d1be0700d844a1b210
```

GitHub `/releases/latest`도 v1.8.0을 반환한다. 따라서 tag ref, release target, exact main source, public package가 같은 제품 release identity를 가리킨다.

## 9. Immutable release rule

v1.8.0 공개 후의 documentation-only commit은 **v1.8.0 product release source가 아니다**.

공개 product source/tag/assets는 다음 기준으로 고정한다.

```text
product source/tag target:
8042e4612a54a6ec395a69d1be0700d844a1b210

public Junhyun-Helper.zip SHA-256:
4ecaf65068153a38a7a8613cfe2ae673aec191563f999f1cfbd10cb93d9437e0
```

후속 docs-only CI가 같은 assembly version에서 다른 ProductVersion/bytes를 만들어도 이미 공개된 v1.8.0 asset을 교체하지 않는다.

## 10. 결론

v1.8.0은 full Windows release gate와 public readback을 통과했다.

```text
PUBLIC STABLE
PRODUCT COMPLETE
MAINTENANCE MODE
```

이 릴리즈 배치에 남은 제품 개발 작업은 없다. 이후 작업은 실사용 오류, Tarkov 변화, reviewed Scanner evidence 또는 사용자가 명시적으로 확정한 새 요구사항이 있을 때 시작한다.
