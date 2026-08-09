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

- 기존 self-contained Windows artifact를 직접 검사한 결과 `ar`, `cs`, `da`, `de`, `es`, `fr`, `it`, `ja`, `ja-JP`, `lv`, `nl`, `pl`, `pt`, `pt-BR`, `ru`, `sk`, `sv`, `th`, `tr`, `zh`, `zh-Hans`, `zh-Hant`, `zh-TW` 폴더는 모두 satellite `*.resources.dll` 전용 폴더였다.
- 제품 언어는 한국어만 지원한다.
- 파일을 publish 후 임의 삭제하지 않고 .NET SDK의 `SatelliteResourceLanguages=ko`를 사용해 빌드 시점부터 한국어 satellite resource만 출력한다.
- 첫 CI publish 결과에서도 실제 최상위 문화권 폴더가 `ko` 하나만 생성되는 것을 확인했다. 별도 삭제 스크립트는 불필요하므로 사용하지 않는다.
- Korean-only publish 상태에서 동일 Startup + Map smoke를 통과해야 한다.
