# Map V2 Windows feedback — Quest UI / Korean-only publish — 2026-08-10

상태: `MERGED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

## 사용자 확인 사항

1. 진행 중 Quest sidebar의 Quest 카드/내용을 왼쪽 정렬하고 각 행의 폭/기준선을 통일한다.
2. `지도 마커 > 퀘스트`에서 Quest marker를 실제로 표시/숨길 수 있어야 한다.
3. Windows 배포본에 포함된 여러 언어 satellite resource 중 제품에 불필요한 언어는 제거하고 한국어만 유지한다.

## 구현

### Quest sidebar

- 모든 Quest row가 sidebar viewport 폭을 동일하게 사용하도록 보정했다.
- checkbox lane을 고정해 좌표 유무에 따라 본문 시작점이 움직이지 않게 했다.
- A/B/C marker-code가 없는 row에도 동일 폭의 투명 placeholder를 사용해 Quest 이름 시작점을 통일했다.
- Quest명/좌표 상태를 왼쪽 정렬했다.
- 기존 click-to-open Quest 및 per-Quest marker checkbox 동작은 유지한다.

### Global Quest marker toggle

- 숨겨진 원본 top-bar checkbox visual을 재사용하지 않는다.
- `지도 마커 > 퀘스트`에 JunhyunHelper 제품용 `퀘스트 마커 표시` checkbox를 새로 생성한다.
- 제품용 checkbox는 원본 Quest visibility behavior endpoint와 양방향 동기화한다.
- 초기 상태는 Quest marker 표시 ON으로 정규화하여, 왼쪽 per-Quest checkbox가 체크됐지만 stale legacy state 때문에 모든 A/B/C marker가 숨겨지는 상태를 제거했다.
- global OFF는 per-Quest 선택 상태를 삭제하지 않는다.

### Korean-only publish

- 이전 self-contained Windows artifact를 직접 검사한 결과 `ar`, `cs`, `da`, `de`, `es`, `fr`, `it`, `ja`, `ja-JP`, `lv`, `nl`, `pl`, `pt`, `pt-BR`, `ru`, `sk`, `sv`, `th`, `tr`, `zh`, `zh-Hans`, `zh-Hant`, `zh-TW` 폴더는 모두 satellite `*.resources.dll` 전용 폴더였다.
- 제품 언어는 한국어만 지원한다.
- 파일을 publish 후 임의 삭제하지 않고 .NET SDK의 `SatelliteResourceLanguages=ko`를 사용해 빌드 시점부터 한국어 satellite resource만 출력한다.
- 최종 artifact를 직접 검사하여 기능 폴더 `Assets`, `Logs` 외 문화권 폴더가 **`ko` 하나만 존재**함을 확인했다.

## Git / 검증

```text
PR #66: Polish Quest sidebar and restore Quest marker control
merge commit: 2f9f07f64d9c6a8259504a8425c254a95673f8ea
final PR head: f55ac0a05bd9fabf180d9df5da36d430dd9181dd
final CI: 31325539763
artifact: 9041432054
artifact digest: sha256:56fc31e6230efe5488da01586a3d1732f50e142f522863e215d5c468b6a20e9a
```

검증 결과:

- Desktop Release build: success
- automated tests: 163/163 success
- Windows x64 self-contained publish: success
- Startup + Map smoke: success
- ZIP creation/upload: success
- 최종 artifact 문화권 폴더 검사: `ko` only

## Windows 사용자 검증 항목

- 펼친 Quest sidebar의 카드 폭/본문/A-B-C가 일정한 왼쪽 기준선으로 정렬되는지
- `지도 마커 > 퀘스트`에 실제 checkbox와 `퀘스트 마커 표시` label이 보이는지
- global checkbox ON 상태에서 왼쪽에 체크한 Quest의 A/B/C marker가 Main Map과 MiniMap에 표시되는지
- 배포 폴더에 한국어 외 언어 satellite 폴더가 사라졌는지
