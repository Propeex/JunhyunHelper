# v0.1.8 사용자 피드백 수정 — 2026-08-17

## 범위

v0.1.8 공개 후 사용자 실사용 피드백 10건을 기준으로 원인 분석과 제품 수정을 진행했다.

## 1. Quest `확인 필요` 과다

### 원인

v0.1.8은 `globalVariable`의 read-side 조건 자체는 구조화했지만, 현재 값을 profile에서 직접 관측하지 못하면 162개 사용처를 전부 `Indeterminate`로 남겼다. 이 때문에 실제 EFT 1.1 trader side-task pool 구조가 명확한 경우까지 `확인 필요`가 과도하게 노출됐다.

### 재감사

2026-08-17 current regular feed를 다시 감사했다.

- task 517개
- `globalVariable` 사용 Quest 162개
- unique variable 27개
- 7개 핵심 상인별 LL1~LL4 staged pool 구조. Ragman은 현재 3단계.
- LL2~LL4 direct seed Quest 수를 별도 감사해 각 pool의 current-version 구조와 교차검증했다.

### 제품 정책

- exact profile `Variables` 값이 있으면 항상 그것이 최우선이다.
- exact 값이 없을 때에만 현재 EFT 1.1의 **정확히 감사된 27 variable ID / trader / pool Quest count / threshold set / direct LL seed count**가 모두 일치하는 경우 current Quest 표시용 runtime compatibility를 허용한다.
- LL2~LL4는 저장된 trader LL과 완료 Quest를 이용해 현재 pool progress를 재구성한다.
- 아직 도달하지 않은 future LL pool은 현재 값 0으로 확정한다.
- LL1은 public feed에 initial seed/write rule이 없으므로 값을 임의 합성하지 않는다.
- 구조가 하나라도 달라지면 compatibility를 적용하지 않고 원래 `확인 필요`로 fail closed한다.
- synthetic value는 `user.db`에 저장하지 않는다.
- Needed Items future protection은 기존 conservative reachability를 그대로 사용한다. 즉 UI의 과도한 `확인 필요`를 줄이기 위해 아이템 cleanup 안전성을 약화시키지 않는다.

현재 raw 구조 기준으로 task-pool compatibility가 해결 가능한 global-variable Quest는 LL2~LL4의 114개이고, LL1 pool 48개는 exact profile variable 값이 없는 한 보수적으로 남는다. availability delay 13건도 실제 completion timestamp가 없으면 계속 `확인 필요`다. 실제 UI 숫자는 완료/잠김/사용불가 상태 우선순위 때문에 이 raw 합계와 다를 수 있다.

상세: `docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md`

## 2. 아이템 수량 변경 버벅임

### 실제 병목

v0.1.8은 inventory mutation에서 Quest page rebuild만 제거했다. 그러나 수량 한 번 변경할 때마다 여전히 다음 작업을 반복했다.

1. 전체 Quest future reachability 계산
2. 모든 future Quest item requirement 재수집
3. 모든 Hideout station의 future level requirement 재수집
4. cleanup protection 재구성
5. Items 전체 row 재생성 / filter 재적용
6. 전체 icon load pipeline 취소 후 재시작

Inventory quantity는 1~4의 구조 자체를 바꾸지 않으므로 불필요한 전역 재계산이었다.

### 수정

- `FutureNeededItemsBasis`를 분리해 Quest reachability / fixed requirements / alternative requirements / cleanup protection을 static planning basis로 캐시한다.
- inventory-only mutation에서는 basis를 재사용하고 Needed/cleanup/flexible-owned 값만 다시 계산한다.
- Items page에 inventory-only refresh 경로를 추가해 이미 decode된 icon을 보존한다.
- inventory 수량 변경 때 전체 icon pipeline을 취소/재시작하지 않는다.
- Quest 완료/실패, Hideout level, profile prerequisite fact처럼 실제 planning basis가 바뀌는 사건만 full rebuild한다.

## 3. 유동 제출 아이템 행 크기

일반 Item list를 기준으로 다음 규격을 강제한다.

- row 68px
- icon lane 52px
- icon 44px
- quantity lane 118px
- 이름 한 줄 + ellipsis
- 동일 padding / vertical alignment

기존에는 candidate 생성 시점 한 번만 visual polish를 시도해 virtualization/layout 재생성 후 놓치는 행이 있었다. 현재는 Flexible ItemsControl 범위의 layout 변경을 batch 처리해 실제 생성된 모든 candidate row에 동일 규격을 재확인한다.

## 4. Ammo 검색

- header 최좌측에 위치
- query는 이름/구경으로 검색 가능
- 결과 표시는 `탄약 이미지 + 이름`만 사용
- 결과 선택 시 기존대로 해당 caliber table + exact ammo row 선택
- 검색창 우측 × 지원

## 5. Ammo 하단 상세 접기

v0.1.8 구현은 row 4의 outer Border 안에 `DetailGrid`가 직접 들어있다고 가정했지만 실제 XAML은 outer Grid를 한 단계 더 가진다. 따라서 Expander 생성 조건이 절대 성립하지 않았다.

현재는 실제 row 4 detail host Border 자체를 찾아 Expander로 감싼다.

- expanded: 기존 detail row + splitter
- collapsed: detail row Auto/MinHeight 0 + splitter 숨김

## 6. Ammo 즐겨찾기

버튼 문구를 제거한다.

- 미즐겨찾기: `☆`
- 즐겨찾기: `★`
- 설명은 tooltip으로만 제공한다.

## 7. `퀘스트 마커 표시` 저장

`map-product-settings.json`의 product toggle을 권위값으로 한다. legacy Map startup이 늦게 기본값을 다시 넣어도 저장값을 덮어쓰지 못하도록 Loaded + 초기 안정화 구간에서 저장값을 재적용한다.

## 8. Map Quest sidebar 행 정돈

v0.1.8은 특정 RGB background를 가진 Border만 Quest row로 인식해 일부 row를 놓쳤다. 현재는 `Border -> Grid -> CheckBox + Button` 구조로 Quest row를 식별한다.

- row 68px
- checkbox lane 30px
- marker badge lane 34px
- text star lane
- badge 28px
- text single-line ellipsis

## 9. MiniMap hover 투명 반응

기존 hover 확인은 80ms product sync timer에 묶여 있었고 이 timer는 Quest/general marker/extract 동기화도 수행했다. hover만 빠르게 하기 위해 map rendering과 분리된 lightweight 16ms Input-priority timer를 추가했다.

무거운 map sync를 60Hz로 올리지는 않는다.

## 10. 검색창 ×

Quest / Hideout / Items / Ammo 검색창에 우측 × 버튼을 추가한다.

- 검색어가 없으면 숨김
- 클릭 시 전체 지우기
- 지운 뒤 검색창 focus 유지
- 각 탭의 기존 TextChanged filter logic을 그대로 사용

## 검증 계약

PR CI는 다음을 모두 통과해야 병합한다.

- Windows Release build
- 전체 automated tests
- Windows x64 self-contained single-file publish
- startup + Main Map + Factory + MiniMap runtime smoke
- normal close / portable-root cleanliness

Task-pool compatibility unit tests는 exact value 우선, LL2 reconstruction, future pool=0, structural drift fail-closed, LL1 conservative behavior를 고정한다.
