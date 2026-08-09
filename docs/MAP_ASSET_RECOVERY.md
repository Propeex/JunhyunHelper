# MAP ASSET RECOVERY — 지도 자산 자동 복구

기록일: **2026-08-09**

상태: `IMPLEMENTED / VALIDATION IN PROGRESS`

## 현상

지도 탭 자체는 정상 열리지만 다음 상태가 표시될 수 있었습니다.

```text
표시할 지도 자산이 없습니다.
```

## 원인

Map gameplay data와 Map presentation asset은 의도적으로 분리되어 있습니다.

기존 구현에는 두 가지 복구 취약점이 있었습니다.

1. `content.db`가 이미 schema v4이면 active Map asset이 비어 있어도 전체 Game Content update가 자동 재실행되지 않았습니다.
2. Map SVG 하나의 다운로드/검증 실패가 전체 Map candidate를 폐기했습니다. 이전 active Map asset이 전혀 없는 최초 실패에서는 모든 지도가 빈 상태로 남을 수 있었습니다.

Map asset update 실패는 canonical Game Content를 손상시키지 않기 위해 warning으로 취급했지만, 그 결과 사용자가 다음 실행에서 자동 회복할 수 있는 경로가 부족했습니다.

## 수정

### Map 탭 진입 self-heal

Map 탭에 진입할 때 active Map asset을 검증합니다.

```text
active Map asset 있음
→ 그대로 사용

active Map asset 없음/손상
→ 현재 active Game Content를 이용해 Map asset만 자동 갱신
→ 성공 후 즉시 지도 표시
```

Game Content를 다시 내려받을 필요 없이 Map presentation asset만 복구할 수 있습니다.

### 직접 재시도

빈 지도 화면에 `지도 자산 다시 받기` 버튼을 제공합니다.

전체 데이터 업데이트 화면으로 이동할 필요 없이 지도 탭에서 즉시 재시도합니다.

### SVG source fallback

현재 Tarkov.dev metadata가 지정하는 SVG와 공개 SVG repository를 서로 fallback source로 사용합니다.

```text
assets.tarkov.dev/maps/svg/<file>
↕ fallback
the-hideout/tarkov-dev-svg-maps raw <file>
```

한 source/domain이 일시적으로 실패해도 다른 공개 원천에서 동일 artwork를 받을 수 있습니다.

### 지도별 부분 복구

한 Map의 SVG 실패가 다른 정상 Map을 막지 않습니다.

- 새 SVG 성공 → 새 layout/SVG 사용
- 새 SVG 실패 + 이전 정상본 있음 → 그 Map만 이전 정상본 유지
- 새 SVG 실패 + 이전 정상본 없음 → 그 Map만 일시 제외
- 최소 한 Map이라도 정상 준비됨 → 정상 Map들은 활성화
- 모든 Map이 실패 → 기존 active가 있으면 보존, 없으면 명시적 오류/재시도 UI

### marker icon

Marker icon은 계속 non-authoritative presentation asset입니다.

- 새 icon 실패 + 이전 icon 있음 → 이전 icon 유지
- 둘 다 없음 → 기본 marker visual 사용
- icon 하나 때문에 Map 전체 update가 실패하지 않음

## 검증 대상

- 기존 v4 `content.db` + empty `map-cache`에서 Map 탭 진입만으로 자동 복구
- SVG source fallback
- 일부 Map 실패 시 정상 Map 활성화
- 이전 Map asset 유지
- 직접 `지도 자산 다시 받기`
- 기존 marker preference / user marker / Quest progress 영향 없음
