# RELEASE — 준현 헬퍼 v1.7.15

기록일: 2026-08-28 KST  
상태: **PUBLIC STABLE / VERIFIED / IMMUTABLE PRODUCT RELEASE**

이 문서는 v1.7.15의 exact source, CI, Windows 배포물, GitHub Release 및 public readback 증거를 기록한다.

## 1. 제품 변경 범위

v1.7.15는 새 기능을 추가하지 않는 UI/UX PATCH다.

- Main header는 version-only presentation을 사용한다.
- cleanup 필요 상태는 Items 탭의 작은 orange dot으로 표시한다.
- Map marker selector 내부 checkbox list가 실제 available height를 사용한다.
- marker list가 들어오면 불필요한 scrollbar를 숨기고 실제로 넘칠 때만 scrolling한다.
- Map marker selector는 launcher 재클릭과 panel outside click으로 닫힌다.
- Ammo Favorites selector는 standard dropdown을 사용한다.
- caliber/Favorites dropdown은 해당 caliber의 member-ammo icon을 같은 animation state로 순환 표시한다.
- 특정 ammo 하나를 caliber 영구 대표 icon으로 고정하지 않는다.

상세 제품 결정:

- `docs/DECISION_V1.7.15_UI_REFINEMENTS.md`
- `docs/RELEASE_NOTES_V1.7.15.md`

Scanner recognition, Game Content/User Progress authority, Needed Items 의미, Map/MiniMap donor revision은 변경하지 않았다.

## 2. PR 최종 검증

PR #203 final head:

```text
4cd1a352acc7bfdcb0823a8d3345f49164232309
```

Final PR CI:

```text
run: 33072794891
result: SUCCESS
410 passed / 0 failed / 0 skipped
Release build: SUCCESS
win-x64 self-contained single-file publish: SUCCESS
Product UI / Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS
```

PR candidate는 merge-ref metadata를 포함하므로 public release artifact가 아니다. 공개 제품은 아래 exact `main` source에서 다시 생성했다.

## 3. Exact product source

```text
version: v1.7.15
exact product release source/tag target:
4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
```

이 SHA는 PR #203의 merge commit이며 공개 v1.7.15 tag와 release target의 권위 source다.

## 4. Exact-main CI

```text
run: 33086901217
CI #2066
result: SUCCESS
source: 4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
```

### Build / tests

```text
Desktop Release build: SUCCESS
410 passed / 0 failed / 0 skipped
```

컴파일 warning은 pinned donor source의 기존 허용 warning 범위만 존재했고 JunhyunHelper build error는 0이다.

### Publish identity

```text
project version: 1.7.15
ProductVersion:
1.7.15+4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
FIRST_RUN first line:
준현 헬퍼 v1.7.15 — Windows x64
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
bytes: 80,492,565
SHA-256:
9ac3276a1a4a20905b0aa3d6452f50d5259f724ed8f960b7cfbad39f8c619f2f
```

Main-CI GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9652666398
artifact archive bytes: 241,410,930
artifact archive SHA-256:
cf3802eb6cba359e46eaa55bf48cc89bed33bf7902129e17f31b48065cf94e04
```

## 6. Release workflow

```text
run: 33087185178
Release #48
result: SUCCESS
source: 4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
input artifact id: 9652666398
input artifact digest:
cf3802eb6cba359e46eaa55bf48cc89bed33bf7902129e17f31b48065cf94e04
```

Release workflow가 exact main-CI artifact를 다시 내려받아 digest를 검증한 뒤 ProductVersion, FIRST_RUN, package checksum을 확인하고 v1.7.15 stable release를 게시했다.

`actions/download-artifact@v8`에서 upstream Node `Buffer()` deprecation warning이 1회 출력되었으나 artifact digest 검증과 release publication은 성공했다. 이는 현재 repo product/CI blocker가 아니며 upstream action 경고로 monitor-only 취급한다.

## 7. Public GitHub Release readback

```text
release id: 377926863
name: 준현 헬퍼 v1.7.15
tag: v1.7.15
release target: 4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
draft: false
prerelease: false
latest stable: true
published UTC: 2026-08-27T15:19:55Z
```

Public asset:

```text
Junhyun-Helper.zip
asset id: 532481010
bytes: 80,492,565
digest:
sha256:9ac3276a1a4a20905b0aa3d6452f50d5259f724ed8f960b7cfbad39f8c619f2f
```

Checksum asset:

```text
SHA256SUMS.txt
asset id: 532481008
bytes: 86
digest:
sha256:84fbabe5ef2c41d28a00305c0cd7b8ee7575fbe3c1c64fa83f7ead1c75494580
```

Public ZIP digest는 exact main-CI package SHA-256과 정확히 일치한다.

## 8. Tag readback

```text
refs/tags/v1.7.15
→ commit 4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
```

GitHub `/releases/latest`도 v1.7.15를 반환한다. 따라서 tag ref, release target, exact main source, public package가 같은 제품 release identity를 가리킨다.

## 9. Immutable release rule

v1.7.15 공개 후의 documentation-only commit은 **v1.7.15 product release source가 아니다**.

공개 product source/tag/assets는 다음 기준으로 고정한다.

```text
product source/tag target:
4bf5e3a567d3ce9563657bbb3b90bec0871c06b4

public Junhyun-Helper.zip SHA-256:
9ac3276a1a4a20905b0aa3d6452f50d5259f724ed8f960b7cfbad39f8c619f2f
```

후속 docs-only CI가 같은 assembly version에서 다른 ProductVersion/bytes를 만들어도 이미 공개된 v1.7.15 asset을 교체하지 않는다.

## 10. 결론

v1.7.15는 full Windows release gate와 public readback을 통과했다.

```text
PUBLIC STABLE
PRODUCT COMPLETE
MAINTENANCE MODE
```

이 릴리즈 배치에 남은 제품 개발 작업은 없다. 이후 작업은 실사용 오류, Tarkov 변화, reviewed Scanner evidence 또는 사용자가 명시적으로 확정한 새 요구사항이 있을 때 시작한다.
