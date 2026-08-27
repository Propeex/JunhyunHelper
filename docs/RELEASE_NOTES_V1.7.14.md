# 준현 헬퍼 v1.7.14

v1.7.14는 v1.7.13의 기능 의미를 유지하면서 설정·popup·검색 interaction을 한 방식으로 정리하는 UI 일관성 PATCH입니다. Scanner 인식 정책, Map/MiniMap donor, Game Content 검증/LKG 계약은 변경하지 않습니다.

## 사용자-facing 변경

- **Ammo popup**
  - `즐겨찾기 선택`, `표시 열` 메뉴가 열린 상태에서 같은 launcher를 다시 누르면 닫힌 상태를 유지합니다.
  - `Popup.StaysOpen=False`의 자동 닫힘 뒤 기존 Click handler가 다시 여는 WPF interaction 회귀를 Preview 단계에서 차단합니다.

- **Map / MiniMap**
  - MiniMap launcher 주변의 donor padding/background와 숨긴 도움말 버튼 자리를 제거해 실제 버튼만 보이게 했습니다.
  - `지도 마커` launcher는 투명 텍스트 영역이 아니라 JunhyunHelper 기본 Button chrome을 사용합니다.
  - 접힌 지도 마커 panel은 빈 배경/최소 폭을 남기지 않습니다.
  - 펼친 panel은 일반 데스크톱 viewport에서 현재 모든 마커 checkbox를 스크롤 없이 볼 수 있도록 세로 공간을 확보합니다.
  - 지도/미니맵 제품 설정 surface는 오른쪽 drawer 대신 MainWindow 공통 in-app overlay에 표시합니다.

- **Scanner**
  - `고급`은 별도 Windows 창을 띄우지 않고 Scanner 설정과 같은 MainWindow in-app overlay에 표시합니다.
  - 고급 화면 안의 별도 `닫기` 버튼을 제거했습니다. 같은 `고급` launcher 재클릭, 공통 overlay X, backdrop click으로 닫습니다.
  - Scanner 단축키 편집을 Scanner 설정 내부로 이동했습니다.
  - 단축키 저장 authority와 중복 조합/Windows modifier/미지정 처리 규칙은 기존 동작을 유지합니다.

- **프로필 수정**
  - 기존 MainWindow overlay 동작과 저장/검증 authority는 유지하면서 내용 card의 배경·border·padding을 Scanner 설정과 같은 계열로 통일했습니다.

- **검색창**
  - Quest / Hideout / Items / Ammo / Scanner의 주요 검색창은 입력창 우측 내부에 `×` clear affordance를 표시합니다.
  - 텍스트가 없으면 숨고, 누르면 검색어를 지운 뒤 검색창 focus를 유지합니다.

## 공통 interaction 계약

사용자-facing 설정/편집 surface는 다음을 기본으로 합니다.

1. MainWindow 내부 overlay로 표시한다.
2. 같은 launcher를 다시 누르면 닫힌다.
3. backdrop 클릭으로 닫힌다.
4. 공통 overlay X로 닫을 수 있다.
5. child editor의 저장·검증·취소 의미는 overlay owner가 재구현하지 않는다.

현재 이 계약을 사용하는 주요 surface는 Profile Edit, Scanner Settings, Scanner Advanced, Map/MiniMap Settings입니다.

## 회귀 방지

- `V1714UiConsistencyContractTests`가 Ammo true-toggle, shared overlay ownership, Scanner 설정/고급, Map launcher, Profile card, 공통 search clear 계약을 고정합니다.
- deterministic test suite는 **407 passed / 0 failed / 0 skipped**까지 증가했습니다.
- actual published Windows x64 EXE smoke는 Scanner Advanced를 실제 MainWindow shared overlay에 host한 상태에서 렌더링·clipping·닫기 계약을 검증합니다.
- code-only PR CI #2045 / run `33059733240`에서 Release build, 407 tests, Windows x64 publish, Product UI + full Map/Factory/MiniMap smoke, graceful shutdown, package verification이 성공했습니다.

## 변경하지 않은 것

- Game Content / User Progress / Needed Items 계산 의미
- Game Content download/validation/LKG activation 계약
- Map/MiniMap pinned donor revision `d933792b6042a51cea38dc44b686a096fe30de67`
- Scanner Item identity recognition pipeline과 structural/header/OCR/matcher/visual acceptance

Scanner 안전 기준은 그대로입니다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

가격/필요 개수/slot/이전 프레임을 Item identity 증거로 사용하지 않으며 cross-frame OCR/visual identity cache도 추가하지 않았습니다.
