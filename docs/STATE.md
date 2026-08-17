# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 공개 상태

**v0.1.7 PUBLIC RELEASE / VERIFIED — Windows x64**

```text
release tag: v0.1.7
release baseline: 8cf2f76003bf2603b8c0f8c0a7d9297bfc62bd43
Desktop ProductVersion: 0.1.7
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
candidate PR CI: 31986395934 — SUCCESS
main CI: 31986585081 — SUCCESS
release workflow: 31986801215 — SUCCESS
public asset: Junhyun-Helper-v0.1.7-win-x64.zip
public SHA-256: b1f935ba47a48e66a46fc028f2d7f631ffb795dada0f3d50b1c42b57ca7caceb
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.7
```

공개 ZIP은 Release 생성 뒤 다시 다운로드해 SHA-256을 재검증했습니다. Release는 draft/prerelease가 아닌 정식 공개 상태입니다.

상세: `docs/RELEASE_0.1.7.md`

---

## Quest prerequisite / availability 기준

### 일반 prerequisite

- 서로 다른 `taskRequirements` 항목은 AND
- 한 requirement의 `status[]`는 OR
- `complete` = 해당 Quest 완료
- `active` = 해당 Quest가 진행 상태에 도달
- `failed` = 해당 Quest 실패
- 별도 `수주 가능` 상태를 만들지 않음
- `DEC-010` 유지: 게임에서 받을 수 있는 Quest는 Helper에서 즉시 수락한 것으로 간주
- source가 직접 제공한 prerequisite 상태는 compatibility overlay가 더 강한 상태로 덮어쓰지 않음

### BTR Driver / Ref / Lightkeeper

- BTR Driver는 `A Helping Hand = Active` 의미를 보존하고 누락된 후속 Quest에만 Active gate를 보강
- Ref는 source gate를 보존하고 누락된 후속 Quest에만 GameMode별 검증된 Complete gate를 보강
- Lightkeeper는 ordinary monotonic prerequisite와 recoverable access state를 분리
- 최초 접근은 Getting Acquainted 결과에서 추론하고, 실제 접근 상실/복구가 필요한 특수 상황에서만 sparse user fact 사용
- recoverable 접근 상실은 영구 `Unavailable`이 아니라 `Locked`
- 상세: `docs/QUEST_PREREQUISITE_SEMANTICS.md`, `DEC-043`

### EFT profile-variable gate

v0.1.7에서 `globalVariable` availability를 opaque 문자열로만 취급하지 않고 structured requirement로 보존합니다.

- `variableId / operator / value`를 canonical Content에 저장
- exact current profile variable 값이 있으면 정확히 판정
- 값이 부족하면 `Locked`
- exact current 값이 없으면 0이나 완료 Quest 수를 추측하지 않고 해당 fact만 `확인 필요(Indeterminate)`
- 미래 source가 현재 지원 계약을 벗어나면 fail-closed
- 공개 source로 증명할 수 없는 server-side variable write rule은 임의 복원하지 않음
- 상세: `docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md`, `DEC-044`

### 실패 / 불명확 availability

- 다른 Quest 완료로 확정되는 sibling failure는 자동 추론
- 프로그램이 알 수 없는 비재시작형 영구 실패만 사용자 입력
- 재시작 가능한 raid failure는 영구 저장하지 않음
- dialogue / 미관측 profile variable / 실제 완료 시각 기반 delay는 추측하지 않고 `확인 필요`
- Battery Change처럼 upstream 자체가 의심스러운 데이터는 근거 없이 임의 보정하지 않음

---

## Content / User Progress 호환성

```text
Current Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1 unchanged
v0.1.6 → v0.1.7 mandatory data update: none
```

다음 정상 `데이터 업데이트`가 성공하면 v7 snapshot으로 저장합니다.

기존 `%LocalAppData%/JunhyunHelper/user.db`의 Profile / Quest 완료·실패 / Inventory / Hideout 진행은 유지됩니다. profile-variable exact value와 special trader access override는 optional user facts로 저장됩니다.

`GameContentValidator`는 prerequisite missing/self/duplicate/cycle/empty status 및 잘못된 special-trader gate를 candidate activation 전에 차단합니다.

---

## Map / MiniMap 기준

Map subsystem은 독립이고 Quest만 JunhyunHelper current profile/content와 연결합니다. pinned legacy Map revision은 `d933792b6042a51cea38dc44b686a096fe30de67`입니다.

- floor는 marker visibility filter가 아니라 presentation relation
- enabled 타층 일반 marker는 same-type/near-XZ라도 각각 유지
- current/above/below compact ring + known off-floor opacity
- semantic duplicate extract 정규화 유지
- Main Map floor 변경은 live zoom + map-space viewport center를 보존
- MiniMap floor 변경은 **exact live visual frame**을 보존
  - 같은 SVG/canonical canvas에서 floor layer만 교체
  - live Zoom/Scale 보존
  - live Translate X/Y 보존
  - PlayerTracking live transform과 stale persisted offset이 달라도 live 화면 우선
  - floor-only change 후 불필요한 re-center/re-clamp를 하지 않음
- 상세: `docs/MINIMAP_FLOOR_FRAME_2026-08-17.md`

v0.1.7 Windows runtime smoke에서 startup + Main Map + Factory + MiniMap floor 전환 + stale persisted offset + 정상 종료를 실제 publish 실행본으로 검증했습니다.

---

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / `확인 필요` 분리 / special trader + exact profile-variable gate 지원 |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / v0.1.7 exact MiniMap floor-frame 보존 |
| Scanner | `준비 중` placeholder / 실제 기능 PRODUCT OPEN |

## 비차단 후속 범위

- Scanner 실제 기능 설계/구현
- Map artwork/config/general-marker atomic bundle updater
- pinned Map renderer deeper refactor는 concrete regression/performance value가 있을 때만 수행
- code signing / installer / application updater
- user.db backup/restore UX
- repository license / third-party notice 정책

## 이번 릴리즈 후 저장소 상태

임시 `.github/workflows/release-v0.1.7.yml`은 공개 검증 후 제거했습니다. 상시 workflow는 `.github/workflows/ci.yml`만 유지합니다.
