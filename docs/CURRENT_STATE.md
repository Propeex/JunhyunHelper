# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md` 및 개별 feedback 문서를 참조합니다.

기준일: 2026-08-10

상태: `PR #71 MERGED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

## 현재 제품 상태

- Profile / Quest / Hideout / Needed Items / Ammo 구현됨
- Map + MiniMap 구현됨
- Scanner는 placeholder 단계
- runtime GPT/AI 의존성 없음
- 온라인 Tarkov 데이터는 프로그램 importer가 내려받아 canonical DB를 재구축하는 구조

## Map / MiniMap 최신 상태

PR #68~#71의 Windows 실사용 피드백을 반영했습니다.

- Map 제품 설정 및 hotkey 재시작 영속화
- 게임 foreground에서 전역 Map hotkey 동작
- Main Map + MiniMap zoom hotkey
- Main Map + MiniMap floor hotkey
- floor hotkey는 PR #71부터 Main Map SVG/marker render 완료 후 MiniMap floor render를 실행하도록 직렬화
- MiniMap ON/OFF / size / timed transparency hotkey
- MiniMap hover transparency
- MiniMap mouse resize 비활성 / 우측 하단 resize grip 제거
- Main Map 설정에 MiniMap 기본 투명도 10%~100% slider
- MiniMap 기본 투명도 저장 및 즉시 적용
- Quest/general marker Main Map/MiniMap 동기화

## 최신 PR

```text
PR: #71 Finalize floor hotkey rendering and MiniMap opacity control
merge: 6fbd575d04fd469f2024e762958d75feda9de6c9
head: 718b7e2ab1dcad832fff3589053b57fe56c4fb3c
CI: 31351937312
artifact: 9049285539
```

상세: `docs/MAP_FINAL_FUNCTIONAL_FEEDBACK_2026-08-10_07.md`

## 다음 작업

1. Windows에서 Main Map floor hotkey 전환 후 지도 artwork가 계속 표시되는지 확인
2. MiniMap 투명도 slider가 즉시 반영되고 재시작 후 유지되는지 확인
3. 위 두 항목이 확인되면 현재 Map/MiniMap 기능 구현을 기능적으로 완료 상태로 간주
4. 이후 별도 단계로 Map artwork/config/general-marker DB atomic bundle updater 진행
