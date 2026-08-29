# 준현 헬퍼 v1.10.1 릴리즈 기록

기준일: 2026-08-29 KST

상태: **PUBLIC / VERIFIED**

## 목적

v1.10.0 전체 유지보수 감사에서 확인된 WPF 초기화 수명주기 결합을 제거하고, 현재 실행 경로에서 사용되지 않는 일회성 저장소 잔재와 패키지 changelog 중복을 정리한다. 새 사용자 기능은 추가하지 않는다.

## 제품 변경

- 메인 헤더 보강 초기화를 static class-level `Loaded` handler에서 `MainWindow.OnInitialized` 소유의 explicit schedule로 이동했다.
- header `DependencyPropertyDescriptor` watcher를 MainWindow 종료 시 명시 해제한다.
- 헤더의 version-only 표시와 아이템 정리 오렌지 점 의미는 그대로 유지한다.
- `.github/scripts/finalize-v121.py`를 제거했다. v1.2.1 역사적 release evidence는 공식 docs/release에 유지된다.
- `FIRST_RUN_KO.txt`를 현재 설치 안내 + 최근 유지보수 변경 중심으로 정리했다. 과거 전체 changelog는 GitHub Releases/docs가 권위다.

## 감사에서 유지한 영역

추가 변경의 실제 이득보다 회귀 위험이 크다고 판단해 다음 계약은 유지했다.

- 사용자 진행 저장 경계 및 UI busy serialization
- atomic JSON same-directory temp / write-through / flush / readable backup promotion
- Program Update checksum / immutable stable / 실패 복구 정책
- Scanner runtime/context monitor 직렬화 및 disposal
- Scanner OCR threshold / matcher / candidate cap / visual recovery acceptance
- Scanner canonical Item ID identity policy
- Game Content v8 / readable v3~v8 / LKG / completeness / fail-closed
- Scanner Favorites / Recents / canonical item-open boundary
- Map marker / Factory floor / viewport 의미
- v1.10.0 MiniMap same-window reopen synchronization
- pinned donor `d933792b6042a51cea38dc44b686a096fe30de67`

## 검증 및 공개 증거

```text
version: v1.10.1
exact product release source/tag target:
c444a1e26793e15c075875159f6605d8a99cf7f9
PR CI: 33253141127 — SUCCESS
exact-main CI: 33253293015 — SUCCESS
release workflow: 33253438908 — SUCCESS
release id: 378982127
published UTC: 2026-08-29T12:49:03Z
automated tests: 439 passed / 0 failed / 0 skipped
stable asset: Junhyun-Helper.zip
stable asset id: 535210900
stable bytes: 80,540,164
stable SHA-256:
c37c00a5e5ecdc431d6b26775d73682cabf17e4310533065c88e2d58d8f14922
checksum asset id: 535210901
checksum asset bytes: 86
checksum asset SHA-256:
d32a6d50b60b512fa446d708d5d8ba75addad854c1e63c51378b318fbd6116c3
```

Exact-main GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9715065803
archive bytes: 241,555,171
archive SHA-256:
17fa98916dac423dd304ca59a5769f2fc61851d391ff3c4df89ceaaa25d3b663
```

PR CI와 exact-main CI 모두 다음 gate를 통과했다.

- Release build
- 439 automated tests
- Windows x64 self-contained single-file publish
- actual published EXE Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- graceful shutdown
- clean portable root
- release package root/dependency/checksum audit

Release workflow는 exact-main CI artifact를 사용해 v1.10.1을 stable로 공개했다. GitHub `/releases/latest` readback에서 `draft=false`, `prerelease=false`, latest stable을 확인했고 release target은 exact product release source와 일치한다. `refs/tags/v1.10.1`도 같은 commit을 직접 가리킨다. 공개 ZIP의 byte size와 GitHub asset digest는 위 값으로 확인했다.

## Schema / compatibility

```text
Desktop target version: 1.10.1
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v7
Scanner catalog cache: v1~v4 readable, v4 written
```

v1.10.0 → v1.10.1에서 mandatory Game Content migration과 user.db migration은 없다.

## 실사용 상태

CI 및 published EXE 자동 검증은 완료됐다. 사용자의 실제 PC/Tarkov 플레이 환경에서의 v1.10.1 실사용 검증은 아직 별도 확인 전이다.

이 문서 이후의 documentation-only commit은 v1.10.1 제품 릴리즈 소스가 아니다. v1.10.1 product source/tag/assets는 `c444a1e26793e15c075875159f6605d8a99cf7f9`에 고정한다.
