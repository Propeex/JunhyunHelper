# RELEASE 0.1.4 — Main Map floor/extract + Quest availability

기록일: **2026-08-15**

상태: **RELEASE CANDIDATE / PUBLIC RELEASE PENDING**

## 목적

v0.1.3 실사용에서 확인된 두 영역을 수정합니다.

1. Main Map에서 다른 층 marker/extract가 보이지 않거나 Factory `Gate 3`처럼 중복 표시되는 문제
2. 프로그램만으로 판정할 수 없는 Quest 해금 조건이 `진행 중`으로 과다 집계되는 문제

## Main Map

- floor는 visibility filter가 아니라 presentation 관계로 처리합니다.
- 알려진 타층 marker/extract도 표시합니다.
- 층 관계는 marker 본체 색과 분리합니다.
  - 현재층: 초록 ring
  - 위층: 빨강 ring
  - 아래층: 파랑 ring
  - 타층: 약 75% opacity
  - 위/아래는 아주 작은 방향 glyph만 보조적으로 사용
- Factory `Gate 3`처럼 같은 물리 탈출구의 PMC/Scav raw row가 겹치는 경우 원본을 삭제하지 않고 활성 faction filter 기준 대표 visual 하나만 표시합니다.
- `Office Window`의 회색 본체는 Scav faction 의미를 유지하며, 층은 별도 ring이 전달합니다.
- 일반 marker의 서로 다른 층 vertical stack은 실제로 남길 representative를 먼저 고른 뒤 그 representative와 직접 8 game-unit 이내인 다른-floor 후보만 floor별 하나씩 억제합니다.
- permanent 200ms full-tree polling은 사용하지 않습니다. 실제 map/floor/filter/tree 변화와 제한된 stabilization만 사용합니다.

## Quest availability

- Core `Indeterminate`를 더 이상 `Current`로 강제하지 않습니다.
- UI에서는 `확인 필요`로 표시합니다.
- `확인 필요`는 정확한 `진행 중` 수치/기본 필터/Map Current Quest sidebar에서 제외합니다.
- 사용자가 실제 게임에서 Quest를 받은 사실을 확인한 경우 수동 완료할 수 있습니다.
- 비재시작형 영구 실패 동기화가 필요한 Quest는 `확인 필요`에서도 수동 실패 처리할 수 있습니다.
- Future Needed Items의 `IndeterminatePotential` 보수 보호는 유지합니다.
- `globalVariable`, `dialogue`, 실제 게임 완료 시각이 필요한 delay는 임의 추측하지 않습니다.

## 데이터/업그레이드

```text
Desktop ProductVersion: 0.1.4
Content schema: v5 유지
user.db schema: 변경 없음
v0.1.3 → v0.1.4 필수 데이터 업데이트: 없음
```

v0.1.0에서 바로 올라오는 경우 최신 Quest availability metadata를 위해 `데이터 업데이트`를 한 번 실행합니다.

## 직접 회귀 smoke

이번 릴리즈부터 공통 floor helper 검사만으로 끝내지 않고 사용자가 캡처한 Factory 사례를 실제 Main Map UI에서 검사합니다.

```text
Factory / main
- Gate 3 raw extract row >= 2
- 화면에는 Gate 3 대표 visual 정확히 1개
- Gate 3 current-floor green relation
- Office Window visible + Scav identity + above-floor red relation

PMC extract OFF
- Gate 3가 사라지지 않고 Scav representative로 전환

Factory / level3
- Office Window visible + Scav identity + current-floor green relation
- Gate 3 visible + below-floor blue relation
```

Map smoke 완료 여부는 process liveness로 추정하지 않습니다. 앱이 Customs/Factory/Main Map/MiniMap/viewport 검사를 모두 끝낸 뒤 명시적 success marker를 기록해야 CI가 성공으로 진행합니다. diagnostic 또는 process exit가 먼저 발생하거나 제한 시간 내 success marker가 없으면 실패합니다.

## Release gate

공개 v0.1.4는 다음을 모두 통과한 뒤에만 생성합니다.

1. 최종 변경 범위 전체 review — release-blocking P1/P2 없음
2. Desktop Release build
3. 전체 automated tests
4. Windows x64 self-contained single-file publish
5. 실제 Main Map + Factory direct regression + MiniMap runtime smoke
6. floor-hotkey zoom + map-space viewport center 보존
7. 정상 Main Window close / process exit
8. ProductVersion `0.1.4`
9. 배포 root 검증
   - `준현 헬퍼.exe`
   - `FIRST_RUN_KO.txt`
   - `Assets/`
   - root DLL 없음
   - PDB 없음
   - nested ZIP 없음
   - runtime `Logs/` 없음
10. `Junhyun-Helper-v0.1.4-win-x64.zip` + `SHA256SUMS.txt` 공개 GitHub Release
11. 공개 asset 재다운로드 후 SHA-256 재검증
12. draft/prerelease가 아닌 정식 공개 상태 확인

최종 baseline / CI run / 공개 SHA-256 / URL은 release 완료 후 이 문서와 `docs/STATE.md`, `README.md`에 기록합니다.
