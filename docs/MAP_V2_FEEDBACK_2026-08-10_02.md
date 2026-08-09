# Map V2 Windows feedback — Quest UI / Korean-only publish — 2026-08-10

상태: `USER CONFIRMED / IMPLEMENTATION IN PROGRESS`

## 사용자 확인 사항

1. 진행 중 Quest sidebar의 Quest 카드/내용을 왼쪽 정렬하고 각 행의 폭/기준선을 통일한다.
2. `지도 마커 > 퀘스트`에서 Quest marker를 실제로 표시/숨길 수 있어야 한다.
3. Windows 배포본에 포함된 여러 언어 satellite resource 중 제품에 불필요한 언어는 제거하고 한국어만 유지한다.

## 구현 기준

### Quest sidebar

- 모든 Quest row는 sidebar viewport 폭을 동일하게 사용한다.
- checkbox lane과 marker-code lane을 고정하여 좌표 유무/A-B-C 표시 유무 때문에 Quest 이름 시작점이 흔들리지 않게 한다.
- Quest명, 좌표 상태, marker code를 왼쪽 기준으로 정렬한다.
- 기존 click-to-open Quest 및 per-Quest marker checkbox 동작은 유지한다.

### Global Quest marker toggle

- 숨겨진 원본 top-bar checkbox visual을 재사용하지 않는다.
- `지도 마커 > 퀘스트`에 JunhyunHelper 제품용 checkbox를 새로 만든다.
- 제품용 checkbox는 원본 Quest visibility behavior endpoint와 양방향 동기화한다.
- 초기 상태는 Quest marker 표시 ON으로 정규화하여, 왼쪽의 per-Quest checkbox가 체크되어 있는데 모든 마커가 숨겨지는 legacy stale-state를 허용하지 않는다.
- global OFF는 per-Quest 선택 상태를 삭제하지 않는다.

### Korean-only publish

- 현재 self-contained Windows publish에서 확인된 `ar`, `cs`, `da`, `de`, `es`, `fr`, `it`, `ja`, `ja-JP`, `lv`, `nl`, `pl`, `pt`, `pt-BR`, `ru`, `sk`, `sv`, `th`, `tr`, `zh`, `zh-Hans`, `zh-Hant`, `zh-TW` 폴더는 모두 satellite `*.resources.dll` 전용 폴더임을 확인했다.
- 제품 언어는 한국어만 지원하므로 `ko`/`ko-KR`만 유지한다.
- 안전을 위해 폴더 내부가 전부 `*.resources.dll`인 경우에만 자동 삭제한다. Assets/runtimes/Logs 등 기능 폴더는 건드리지 않는다.
- publish 정리 후 동일 Startup + Map smoke를 통과해야 한다.
