# 준현 헬퍼 v1.7.13

v1.7.13은 기존 기능의 의미를 바꾸지 않고 반복 조작과 불필요한 UI를 줄이는 제품 정리 패치입니다. Scanner 인식 정책, Map/MiniMap donor, Game Content 검증/LKG 계약은 유지합니다.

## UI 정리

- **Items**
  - 필요한 아이템 화면의 퀘스트용/은신처용 용도 선택을 제거하고 `All` 기준으로 단순화했습니다.

- **Ammo**
  - 상단 조작을 `구경 → 즐겨찾기 토글 → 즐겨찾기 선택 → 검색` 순서로 정리하고 표시 열 메뉴는 우측에 유지합니다.
  - 상세정보는 새 실행 세션에서 기본 접힘으로 시작합니다.
  - 표 위 중복 요약 문구를 제거했습니다.
  - published EXE smoke가 초기 접힘 → 펼침 → 다시 접힘의 전체 토글 왕복을 검증합니다.

- **Map**
  - 지도 마커 선택과 설정 popup은 같은 launcher를 다시 누르면 닫힙니다.
  - 지도 마커 선택은 기본 접힘이며 펼치면 선택지를 한 번에 표시합니다.
  - 경로 표시/경로 지우기와 단축키 안내 문구를 제거했습니다.
  - donor source 자체를 변경하지 않고 JunhyunHelper first-party customization 경계에서만 적용했습니다.

- **Scanner**
  - Scanner 설정은 변경 즉시 기존 설정 저장소에 반영되고 취소/저장 버튼을 제거했습니다.
  - 단축키 설정을 display 설정에서 분리해 기본 Scanner 화면에서 접근합니다.
  - 검색한 아이템이 현재 필요한 아이템이면 기존 `ItemsWorkspace.Plan.NeededItems`의 source를 사용해 관련 Quest/Hideout을 표시하고 이동할 수 있습니다.
  - `현재 결과 교정` 버튼을 우측 정렬했습니다.

- **설정/편집 overlay**
  - 프로필 편집과 Scanner 설정 등 사용자-facing 편집 화면을 MainWindow 내부 overlay interaction으로 통일했습니다.
  - X 버튼, backdrop 클릭, 동일 launcher 재클릭으로 닫을 수 있습니다.

## 회귀 방지

- `V1713UiSimplificationContractTests`가 Ammo 기본 접힘과 실제 smoke 왕복 검증, Items 용도 필터 비활성화, Scanner needed source authority를 고정합니다.
- published Windows x64 EXE Product UI/Map smoke를 그대로 유지해 실제 WPF 렌더링과 interaction 회귀를 검출합니다.

## 변경하지 않은 것

- Game Content / User Progress / Needed Items 계산 의미
- Game Content download/validation/LKG activation 계약
- Map/MiniMap pinned donor revision
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
