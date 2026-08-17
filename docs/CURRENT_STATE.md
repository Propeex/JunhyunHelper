# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md` 및 개별 문서를 참조합니다.

기준일: 2026-08-17

상태: `v0.1.12 PUBLIC RELEASE / VERIFIED`

## 현재 공개 제품

```text
release: v0.1.12
release baseline: cfacee6cfa893932d74d6a71725b6c711282981e
ProductVersion: 0.1.12+cfacee6cfa893932d74d6a71725b6c711282981e
release candidate PR: #95
release candidate CI: 32025523609 — SUCCESS
release baseline main CI: 32025837427 — SUCCESS
release workflow: 32026123215 — SUCCESS
tests: 210 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v0.1.12-win-x64.zip
size: 74,067,018 bytes
SHA-256: bc91f17f94c6554d09da3fed6db6ebb679c6e1d57ff7017d4a624e8dcd8eae89
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.12
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
mandatory data update from v0.1.11: none
```

공개 ZIP은 Release 생성 뒤 다시 다운로드하여 크기와 SHA-256을 재검증했습니다. Release는 `draft=false`, `prerelease=false`이며 target commit은 정확히 release baseline과 일치합니다.

상세: `docs/RELEASE_0.1.12.md`

## v0.1.12 핵심 수정

### Flexible hand-in

- 전역 Button template의 centered ContentPresenter 때문에 내부 Grid가 행 전체 폭을 사용하지 못하던 근본 원인 수정
- candidate 전용 stretch template 적용
- 실제 렌더 구조: `52px icon | * name/category | 108px in-raid | 96px normal`
- icon/name은 좌측 축, in-raid/normal은 우측 축 유지

### Ammo

- runtime data/filter refresh 이후에도 favorite Button은 `☆` / `★`만 표시
- detail handle은 42px 화살표 전용
- expanded=`▼`, collapsed=`▲`

### Map current Quest sidebar

- Quest title을 전역 centered Button ContentPresenter에서 분리
- `30px checkbox | 34px A/B/C/D | * Quest text` 고정 lane
- marker/check 상태와 관계없이 Quest title 시작 X축 유지
- expanded sidebar handle은 panel의 오른쪽 바깥 경계, 즉 지도와 panel 사이에 위치

## UI 완료 판정 기준

v0.1.12부터 위 UI 계약은 source inspection이나 build 성공만으로 완료 처리하지 않습니다.

실제 publish된 Windows 앱에서 `MainWindow.ProductUiLayoutSmoke`가 WPF Measure/Arrange 결과를 검사합니다.

- Flexible candidate가 실제 row 폭으로 확장
- icon/name 좌측 축과 in-raid/normal 우측 축
- Ammo favorite 실제 Content가 단일 `☆`/`★`
- Ammo detail expanded=`▼`, collapsed=`▲`
- 서로 다른 Map Quest 행의 title 시작 X 편차 `<= 0.75px`
- expanded Map sidebar handle right gap `<= 6px`

동일 실행에서 Main Map / Factory / MiniMap / 정상 종료 smoke도 수행합니다.

릴리즈 실행 로그에서 `PUBLISHED_RENDERED_UI_MAP_SMOKE=true`가 확인됐습니다.

## 제품 기능 상태

- Profile: 구현 완료
- Quest: 구현 완료 / conservative unknown 유지 / audited compatibility 적용
- Hideout: 구현 완료
- Needed Items / Inventory: 구현 완료 / unresolved future item 보호 / inventory mutation cache
- Ammo: 구현 완료 / v0.1.12 rendered alignment gate 적용
- Map + MiniMap: 구현 완료 / exact floor-frame / current Quest sidebar rendered alignment gate 적용
- Scanner: `준비 중` placeholder
- runtime GPT/AI 의존성 없음
- 온라인 Tarkov 데이터는 프로그램 importer가 다운로드 → 검증 → canonical DB 재구축

## 다음 작업

- 사용자 v0.1.12 실사용 피드백 처리
- Scanner 실제 기능은 별도 제품 설계 후 진행
- Map artwork/config/general-marker atomic bundle updater
- code signing / installer / updater 등 배포 UX는 후속 범위
