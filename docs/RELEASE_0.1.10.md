# RELEASE 0.1.10 — 2026-08-17

## Status

**PUBLIC RELEASE / VERIFIED — Windows x64**

```text
release tag: v0.1.10
release baseline: cc8d968deb6cbb07029fa35186ec3a3881d5c97f
Desktop ProductVersion: 0.1.10+cc8d968deb6cbb07029fa35186ec3a3881d5c97f
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
feature PR: #90
feature PR CI: 32007776178 — SUCCESS
feature main CI: 32008009801 — SUCCESS
release candidate PR: #91
release candidate PR CI: 32011089823 — SUCCESS
release baseline main CI: 32011299363 — SUCCESS
release workflow: 32011564563 — SUCCESS
automated tests: 210 passed / 0 failed / 0 skipped
public asset: Junhyun-Helper-v0.1.10-win-x64.zip
public asset size: 74,067,151 bytes
public SHA-256: 0d32f2344feb1e9088460830e6cff4bbd527198b1e191a177f7a8652e6efd998
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.10
```

공개 ZIP은 GitHub Release 생성 뒤 다시 다운로드했고 SHA-256이 빌드 직후 값과 정확히 일치했습니다. Release metadata도 `draft=false`, `prerelease=false`, target commit=`cc8d968deb6cbb07029fa35186ec3a3881d5c97f`로 확인했습니다.

## User-facing changes

### Quest `확인 필요`

v0.1.9에서 audited EFT 1.1 trader task-pool의 LL2~LL4는 runtime reconstruction을 지원했지만, LL1 48 Quest는 exact profile variable이 없으면 보수적으로 `확인 필요`를 유지했습니다.

v0.1.10은 이 중 **명백한 초기 상태**만 추가로 판정합니다.

- exact profile variable 값은 계속 최우선입니다.
- audited current-version pool 구조가 완전히 일치해야 합니다.
- 해당 trader의 현재 loyalty가 LL1이어야 합니다.
- 그 trader에 대해 Helper가 알고 있는 completed Quest가 0개여야 합니다.
- 위 조건을 모두 만족할 때만 해당 LL1 pool counter를 0으로 확정합니다.
- completed Quest가 하나라도 있거나 trader가 LL2 이상이면 LL1 current counter를 추측하지 않습니다.
- upstream 구조가 감사값에서 벗어나면 기존처럼 fail-closed `Indeterminate`로 돌아갑니다.

이 규칙은 새 캐릭터/초기 프로필의 false `확인 필요`를 줄이기 위한 것이며, 진행된 캐릭터의 값을 임의로 역산하는 generic heuristic이 아닙니다.

실제 completion timestamp가 필요한 availability delay는 timestamp를 모르면 계속 `확인 필요`입니다.

### Flexible hand-in Items

- 후보 행을 일반 Item 목록의 레이아웃 계약으로 다시 구성했습니다.
- 44px icon frame + 44px image를 사용해 이전 36px frame / 44px image clipping을 제거했습니다.
- `아이콘 + 이름/분류`는 좌측 정렬합니다.
- `인레이드 / 일반` 보유량은 우측 고정 lane으로 분리합니다.
- Needed Items / cleanup protection semantics는 변경하지 않았습니다.

### Ammo

- header의 중복 `구경`, `즐겨찾기` label을 제거했습니다.
- caliber selector 폭을 축소했습니다.
- favorite toggle은 `☆ / ★`만 표시합니다.
- favorites 선택 버튼은 별도 유지합니다.
- 상세정보 접기/펼치기를 중앙 정렬된 전용 toggle로 변경했습니다.

### Map Quest sidebar

v0.1.9의 polish bridge가 동적으로 생성되는 실제 current Quest sidebar에 연결되지 않던 누락을 수정했습니다.

- checkbox lane
- A/B/C/D marker lane
- Quest title star lane

을 분리하고 Quest 이름이 marker 공간 바로 뒤에서 좌측 정렬되도록 했습니다.

## Safety / compatibility

- Content schema는 **v7** 유지
- v3~v7 snapshot 읽기 지원 유지
- `user.db` SQLite schema **v1** 유지
- v0.1.9 → v0.1.10 필수 데이터 업데이트 없음
- 기존 Profile / Quest / Inventory / Hideout / Map 설정 유지
- future Needed Items는 unresolved Quest를 계속 `IndeterminatePotential`로 보호
- runtime GPT/AI 의존성 없음

## Verification

### Feature pass

PR #90의 최종 head에 대해:

- Release build SUCCESS
- automated tests SUCCESS
- Windows x64 self-contained publish SUCCESS
- startup + Main Map + Factory + MiniMap smoke SUCCESS
- graceful shutdown SUCCESS

Feature PR CI: `32007776178`

병합 후 main CI: `32008009801` — SUCCESS

### Release candidate

PR #91에서 ProductVersion/FIRST_RUN을 v0.1.10으로 고정한 뒤:

- Release build SUCCESS
- automated tests SUCCESS
- Windows x64 publish SUCCESS
- Map/MiniMap runtime smoke SUCCESS

Release candidate CI: `32011089823`

Release baseline main CI: `32011299363` — SUCCESS

### Public package verification

Release workflow `32011564563`은 정확히 release baseline `cc8d968deb6cbb07029fa35186ec3a3881d5c97f`를 checkout했습니다.

검증 결과:

```text
ProductVersion=0.1.10+cc8d968deb6cbb07029fa35186ec3a3881d5c97f
Passed: 210
Failed: 0
Skipped: 0
Public size: 74,067,151 bytes
Public SHA-256: 0d32f2344feb1e9088460830e6cff4bbd527198b1e191a177f7a8652e6efd998
PUBLIC_RELEASE_VERIFIED=true
```

공개 자산을 GitHub Release에서 재다운로드한 SHA-256도 동일했습니다.

## Remaining conservative boundaries

다음은 release-blocking defect가 아니라 정확성을 위한 현재 제품 경계입니다.

- 진행된 LL1 task-pool에서 exact profile variable 값이 없으면 일부 Quest는 계속 `확인 필요`일 수 있습니다.
- 실제 Quest completion timestamp가 필요한 delay는 timestamp를 모르면 `확인 필요`입니다.
- 새로운/변경된 unsupported availability condition은 자동 통과시키지 않습니다.
- Scanner 실제 기능은 아직 PRODUCT OPEN입니다.
