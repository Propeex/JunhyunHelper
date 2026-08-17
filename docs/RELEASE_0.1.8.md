# RELEASE 0.1.8 — Usability / stability pass

기록일: **2026-08-17**

상태: **PUBLIC RELEASE / VERIFIED**

## 목적

v0.1.8은 v0.1.7 이후 사용자 피드백으로 확인한 Quest availability, Map/MiniMap 상태 동기화, Items UI/성능, Ammo 탐색 문제를 정리한 패치 릴리즈입니다.

## Quest / Needed Items

- 2026-08-17 live task feed의 `dialogue` availability 12건을 regular / pve / pvp-season 세 GameMode에서 전수 감사했습니다.
- 정확히 검증된 12개 Quest ID에만 fail-closed compatibility를 적용합니다.
- 실제 시작 Quest 3개는 opaque dialogue gate를 제거합니다.
- 나머지 9개는 검증된 prerequisite / minimum level을 복원합니다.
- Introduction은 Gunsmith - MP-133의 `Active` prerequisite 의미를 보존합니다.
- upstream이 향후 ordinary `taskRequirements`를 제공하면 source rule이 자동으로 우선합니다.
- unknown/new dialogue는 allowlist 밖이므로 계속 `확인 필요(Indeterminate)`입니다.
- 기존 Content snapshot에도 read-time으로 적용되어 강제 데이터 재다운로드가 필요 없습니다.
- post-fix live audit에서 세 GameMode 모두 raw dialogue 12건 → compatibility 후 잔여 0건을 확인했습니다.
- unresolved future Quest item은 `IndeterminatePotential`로 계속 보호하여 불명확한 availability 때문에 필요한 아이템을 `정리 가능`으로 잘못 보내지 않습니다.

세부 감사: `docs/DIALOGUE_GATE_AUDIT_2026-08-17.md`

## Map / MiniMap

- 제품용 지도 마커 설정 복원 시 hidden legacy Quest toggle이 저장값을 `true`로 덮을 수 있던 초기화 충돌을 제거했습니다.
- `%LocalAppData%/JunhyunHelper/map-product-settings.json`의 제품 설정을 권위값으로 복원합니다.
- Main Map selector와 shared `MapTrackerService.CurrentMapKey`의 일관성 경계를 보강하여 MiniMap이 오래된 다른 맵 키를 유지하는 경우를 줄였습니다.
- 사용자 표시 `나들목` 명칭은 `인터체인지`로 통일했습니다.
- 지도 Quest sidebar 행 높이와 checkbox / A-B-C marker / text lane을 고정하여 행 크기 흔들림을 줄였습니다.
- 반복 layout 보정 작업을 batch 처리하여 불필요한 visual-tree 작업을 줄였습니다.
- v0.1.7의 exact MiniMap floor-frame 보존 계약은 그대로 유지합니다.

## Items / 성능

- 유동 제출 후보 아이템 행을 고정 높이와 고정 수량/status lane으로 정돈했습니다.
- 긴 이름은 ellipsis + tooltip으로 처리합니다.
- Inventory 수량 변경 때 Quest workspace 전체를 불필요하게 재계산/재렌더링하던 경로를 제거했습니다.
- Hideout level 변경도 Quest availability를 다시 계산하지 않습니다.
- Quest 완료/실패처럼 prerequisite와 Needed Items에 실제 영향을 주는 변경은 Quest + Items 재계산을 유지합니다.

## Ammo

- 탄약 이름/구경 검색을 추가했습니다.
- 검색 결과는 기존 `AmmoRow`를 직접 참조하여 클릭 시 해당 caliber table과 정확한 탄약 row를 선택합니다.
- 하단 `탄약 / 수급 경로 상세정보` 전체 패널을 접고 펼칠 수 있습니다.
- 접을 때 detail row와 splitter까지 축소되어 탄약표가 실제로 더 많은 공간을 사용합니다.

## 저장 호환성

```text
Desktop ProductVersion: 0.1.8
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1 unchanged
v0.1.7 → v0.1.8 mandatory data update: none
```

기존 Profile / Quest 진행 / Inventory / Hideout / Map 설정은 유지됩니다.

## Release gate

다음 검증을 모두 통과한 exact release baseline만 공개했습니다.

1. release candidate PR Windows Release build
2. 전체 automated tests
3. candidate Windows x64 self-contained single-file publish
4. candidate startup + Main Map + Factory + MiniMap runtime smoke
5. candidate 정상 종료
6. `main` merge baseline에서 위 CI 전체 재검증
7. release workflow에서 exact baseline SHA 재-checkout
8. release workflow Release build + 전체 automated tests 재실행
9. ProductVersion `0.1.8` 확인
10. `FIRST_RUN_KO.txt` v0.1.8 / Content schema v7 확인
11. release root 검증
    - `준현 헬퍼.exe`
    - `FIRST_RUN_KO.txt`
    - `Assets/`
    - root DLL 없음
    - PDB 없음
    - nested ZIP 없음
    - runtime `Logs/` 없음
    - 금지된 legacy dependency 없음
12. 실제 startup + Main Map + Factory + MiniMap runtime smoke
13. 정상 Main Window close / process exit
14. GitHub Release 생성
15. draft/prerelease 아님 확인
16. 공개 ZIP 재다운로드 후 SHA-256 재검증

## 최종 공개 기록

```text
release tag: v0.1.8
release baseline: 1605d4bc9838486c6290827cebc10d9f3fd57d84
candidate PR: #87
candidate PR CI run: 31991531760 — SUCCESS
main CI run: 31999094668 — SUCCESS
release workflow run: 31999304667 — SUCCESS
Desktop ProductVersion: 0.1.8
Content schema: v7
automated tests: 203 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
startup + Main Map + Factory + MiniMap smoke: SUCCESS
graceful shutdown: SUCCESS
public asset: Junhyun-Helper-v0.1.8-win-x64.zip
public asset size: 74,057,364 bytes
public SHA-256: 0a75f1a2a987e6eec41307eea6149090db90f9855e51b2e72e3a4708d22b9394
public ZIP re-download + SHA-256 verification: SUCCESS
draft: false
prerelease: false
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.8
```

임시 `.github/workflows/release-v0.1.8.yml`은 공개 검증 완료 후 제거했습니다. 상시 workflow는 `.github/workflows/ci.yml`만 유지합니다.
