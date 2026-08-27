# 준현 헬퍼 v1.7.13 — verified release record

기준일: 2026-08-27
상태: **PUBLIC STABLE / VERIFIED**

## 목적

v1.7.13은 새 도메인 기능이나 Scanner identity 정책을 추가하지 않고 기존 제품의 반복 조작과 불필요한 UI를 줄이는 patch release다.

주요 범위는 Items 용도 필터 단순화, Ammo 배치/기본 접힘 정리, Map marker/settings interaction 단순화, Scanner 설정 자동 저장·단축키 분리·needed source 표시, 프로필/Scanner 편집 화면의 MainWindow 내부 overlay 통일이다.

Scanner threshold/candidate cap/matcher/visual acceptance, Needed Items authority, Map/MiniMap donor revision, Game Content validation/LKG 계약은 변경하지 않았다.

## 제품 변경

1. Items
   - 퀘스트용/은신처용 용도 선택을 제거하고 canonical `All` 기준을 사용한다.
2. Ammo
   - 상단 조작 순서를 정리하고 표시 열 메뉴는 우측에 유지한다.
   - 상세정보는 기본 접힘이다.
   - 중복 요약 문구를 제거했다.
3. Map
   - marker selector와 설정은 같은 launcher 재클릭으로 닫힌다.
   - marker selector는 기본 접힘이다.
   - trail/clear-trail UI와 hotkey 설명 문구를 제거했다.
   - pinned donor source는 수정하지 않고 JunhyunHelper first-party customization boundary에서만 적용했다.
4. Scanner
   - 표시 설정은 변경 즉시 저장된다.
   - hotkey 설정을 기본 Scanner 화면으로 분리했다.
   - 검색 item이 현재 needed item이면 기존 `ItemsWorkspace.Plan.NeededItems`의 source를 표시해 Quest/Hideout으로 이동할 수 있다.
   - 현재 결과 교정 버튼은 우측 정렬한다.
5. In-app overlay
   - 프로필 편집과 Scanner 설정/hotkey 등 사용자-facing 편집 화면을 MainWindow 내부 overlay로 호스팅한다.
   - X, backdrop click, same launcher 재클릭으로 닫을 수 있다.

공식 결정:

- `docs/DECISION_V1.7.13_UI_SIMPLIFICATION.md`
- `docs/RELEASE_NOTES_V1.7.13.md`

## 회귀 검증

PR #199 final head:

```text
98da50022528d78a3c8f0448736b5785bf9de818
```

Final PR CI:

```text
run: 33051551273
CI number: #2042
result: SUCCESS
400 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
published EXE Product UI / Scanner / Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS
artifact upload: SUCCESS
```

Ammo의 첫 published EXE smoke는 과거 기본 펼침 상태를 가정해 실패했다. 제품 구현은 확정 요구사항대로 기본 접힘이었으므로 smoke를 삭제/완화하지 않고 `초기 접힘 → 펼침 → 다시 접힘` 전체 왕복 검증으로 갱신했다.

`V1713UiSimplificationContractTests` 3개가 Ammo 기본 접힘/smoke 계약, Items 용도 필터 비활성화, Scanner needed source authority를 고정한다.

## exact product release source

PR #199 merge/main release source:

```text
16198c462a6be58d77dbe2dc27aa57eabfc7b9fd
```

이 커밋이 v1.7.13 tag의 exact target이며 v1.7.13 ProductVersion metadata에도 이 SHA가 포함된다. 이후 documentation-only main commit은 제품 릴리즈 소스로 해석하지 않는다.

## main CI

```text
run: 33051890329
CI number: #2043
result: SUCCESS
400 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Scanner / Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS
artifact upload: SUCCESS
```

Published ProductVersion:

```text
1.7.13+16198c462a6be58d77dbe2dc27aa57eabfc7b9fd
```

Main-CI release package:

```text
name: Junhyun-Helper.zip
bytes: 80,486,670
SHA-256: d1cfcf1f606985485584f0e085e8821e0f62156a980f259a90144fd134a7eeb6
```

GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9638028208
artifact archive bytes: 241,391,856
artifact archive SHA-256: 2595e17f0da0a0e6d49c6889e339ef8f0626775e8b62991eef0093d1f5ef3df5
```

## Release workflow

```text
run: 33052109161
Release number: #44
result: SUCCESS
```

Release workflow는 exact main CI artifact를 내려받아 ProductVersion, FIRST_RUN identity, package checksum을 다시 검증한 뒤 stable release를 게시했다.

## public release readback

GitHub `/releases/latest` 및 tag ref readback:

```text
release id: 377652938
tag: v1.7.13
name: 준현 헬퍼 v1.7.13
target commitish: 16198c462a6be58d77dbe2dc27aa57eabfc7b9fd
tag ref object: 16198c462a6be58d77dbe2dc27aa57eabfc7b9fd
draft: false
prerelease: false
latest stable: true
published at UTC: 2026-08-27T08:00:58Z
```

Public asset:

```text
name: Junhyun-Helper.zip
asset id: 531953179
bytes: 80,486,670
GitHub asset digest: sha256:d1cfcf1f606985485584f0e085e8821e0f62156a980f259a90144fd134a7eeb6
```

공개 asset digest는 exact main CI가 생성·검증한 package SHA-256과 일치한다.

Checksum asset:

```text
name: SHA256SUMS.txt
asset id: 531953171
bytes: 86
GitHub asset digest: sha256:63ca63dd0e21d347a293a6fc45817d604bc4016d3338425a21b5ddc3e86a26f1
```

현재 도구 세션에서는 public binary asset을 별도 anonymous client로 다시 내려받아 byte-level 재검증하지 않았다. 따라서 이 문서는 실제 수행한 exact main-CI package verification, Release workflow verification, public GitHub metadata/digest/tag-ref readback의 일치만 기록한다.

## 보존된 안전 계약

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive보다 miss 선호
- stale/cross-frame identity proof 금지
- Item ID 확정 전 price/needed/slot metadata identity evidence 사용 금지
- needed quantity authority는 `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`
- pinned Map donor `d933792b6042a51cea38dc44b686a096fe30de67`
- Game Content validation/LKG 계약 유지

## 관련 문서

- `docs/DECISION_V1.7.13_UI_SIMPLIFICATION.md`
- `docs/RELEASE_NOTES_V1.7.13.md`
- `docs/.release-v1.7.13-status.json`
- `docs/MAINTENANCE_CONTRACTS.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
