# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md` 및 개별 feedback 문서를 참조합니다.

기준일: 2026-08-17

상태: `v0.1.11 PUBLIC / POST-RELEASE UI CORRECTION MERGED TO MAIN / NOT YET RELEASED`

## 현재 공개 제품

- 공개 릴리즈: `v0.1.11`
- release baseline: `88a732c70380b4c764634eff6fd01a16eb849b14`
- Content schema: v7
- readable Content schemas: v3~v7
- user.db schema: v1
- Quest `확인 필요`의 audited dialogue / task-pool 호환 판정은 v0.1.11에 포함되어 있음

## 현재 main — 미배포 수정

사용자가 v0.1.11 실제 화면 캡처로 확인한 UI 회귀를 다시 수정했습니다.

```text
PR: #94 Fix rendered UI alignment and compact ammo controls
merge: 64f353dd71ee69ec4e474a73fa94717b015e9c4b
PR CI: 32022249988 — SUCCESS
main CI: 32022514487 — SUCCESS
tests: 210 passed / 0 failed / 0 skipped
release status: NOT RELEASED
```

수정 내용:

- Flexible hand-in item row
  - 전역 Button template의 centered ContentPresenter 때문에 candidate Grid가 실제 행 폭을 사용하지 못하던 원인 수정
  - 52px icon | * name/category | 108px in-raid | 96px normal 실제 렌더 lane 사용
  - 아이콘/이름은 공통 좌측 축, in-raid/normal은 공통 우측 축
- Ammo
  - runtime refresh에서도 favorite Button은 `☆` / `★`만 표시
  - detail handle은 화살표만 표시
  - expanded=`▼`, collapsed=`▲`
  - 42px compact centered handle
- Map current-Quest sidebar
  - Quest text를 전역 centered Button ContentPresenter에서 분리
  - 30px checkbox | 34px A/B/C/D | * Quest text 고정 lane
  - marker/checkbox 유무와 무관하게 Quest title actual X start 고정
  - expanded sidebar handle은 panel 오른쪽 바깥 경계에 유지

상세: `docs/UI_ALIGNMENT_FEEDBACK_2026-08-17.md`

## UI 검증 기준 변경

v0.1.11 이후 UI 정렬 변경은 build/source inspection만으로 완료 처리하지 않습니다.

`MainWindow.ProductUiLayoutSmoke`가 실제 published Windows 앱의 WPF Measure/Arrange 결과를 검사합니다.

현재 gate:

- Flexible candidate Grid가 실제 row 폭으로 확장되는지 확인
- icon/name 좌측 축과 in-raid/normal 우측 축의 실제 좌표 확인
- Ammo favorite 실제 Content가 단일 `☆`/`★`인지 확인
- Ammo detail expanded=`▼`, collapsed=`▲` 및 host visibility 확인
- marker/checkbox 조합이 다른 Map Quest 3종의 title X 편차 `<= 0.75px`
- expanded Map Quest sidebar handle right-edge gap `<= 6px`

이 rendered UI gate는 기존 Main Map / Factory / MiniMap runtime smoke와 같은 published-app 실행 경로에서 수행됩니다.

## 제품 기능 상태

- Profile: 구현 완료
- Quest: 구현 완료 / conservative unknown 유지 / audited compatibility 적용
- Hideout: 구현 완료
- Needed Items / Inventory: 구현 완료 / unresolved future item 보호 / inventory mutation cache
- Ammo: 구현 완료 / 위 main UI correction 미배포
- Map + MiniMap: 구현 완료 / 위 main Quest sidebar correction 미배포
- Scanner: `준비 중` placeholder
- runtime GPT/AI 의존성 없음
- 온라인 Tarkov 데이터는 프로그램 importer가 다운로드→검증→canonical DB 재구축

## 다음 작업

1. 현재 main의 UI correction을 사용자에게 별도 패치 릴리즈로 제공할지 결정
2. 릴리즈할 경우 exact release baseline에서 버전/첫 실행 안내를 갱신하고 rendered UI smoke를 다시 통과
3. 공개 ZIP 생성 후 재다운로드 SHA-256 검증
4. 이후 사용자 실사용 화면 피드백으로 최종 확인
