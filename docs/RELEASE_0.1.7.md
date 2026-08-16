# v0.1.7 — Quest availability + MiniMap floor transform

날짜: 2026-08-17

## 사용자 변경

### Quest 선행/availability

EFT 1.1 `globalVariable` 조건을 더 이상 단순 미지원 조건으로 버리지 않는다.

- `variableId`
- 비교 연산자 `>=`
- 정수 임계값

을 canonical content에 보존한다.

실제 profile variable 값이 있는 경우 정확하게 조건을 판정한다. 값이 없고 공개 데이터만으로 현재 값을 증명할 수 없는 경우에는 0이나 완료 Quest 수를 추측하지 않고 `확인 필요`를 유지한다.

이 릴리즈는 DEC-044 및 `QUEST_TASK_POOL_AUDIT_2026-08-17.md`의 확정 정책을 공개 버전에 포함한다.

### MiniMap 층 전환

층을 바꿀 때 전환 직전의 실제 화면 변환을 그대로 보존한다.

- ScaleX / ScaleY 유지
- TranslateX / TranslateY 유지
- 같은 map-space viewport center 유지
- PlayerTracking에서 persisted offset이 stale해도 live transform 우선
- A→B→A 왕복 층 전환에서 누적 drift 방지

현재 floor들은 동일한 SVG/canvas 좌표계의 layer이므로 별도의 층별 배율을 추측하지 않는다. 사용자에게는 같은 위치·같은 확대 크기에서 층 artwork만 교체되는 것이 제품 기준이다.

상세: `FEEDBACK_2026-08-17_MINIMAP_FLOOR_VISUAL_TRANSFORM.md`

## 데이터 호환성

- Content schema: v7
- readable Content schemas: v3, v4, v5, v6, v7
- user.db SQLite schema: v1 유지
- 기존 사용자 진행 데이터 초기화 불필요

## 검증 기준

정식 릴리즈는 다음 검증을 모두 통과한 빌드만 사용한다.

- Windows Release build
- 전체 automated test suite
- Windows x64 self-contained single-file publish
- startup + Main Map + Factory + MiniMap runtime smoke
- MiniMap exact transform A→B / A→B→A 회귀 검증
- floor command 자체의 비동기 렌더 완료 이후 실제 floor와 transform을 즉시 검증하여 단순 UI 문자열 polling을 성공 조건으로 사용하지 않음
- graceful shutdown
- clean portable root

## 최종 릴리즈 식별자

공개 릴리즈 생성 후 baseline, CI run, asset SHA-256을 이 문서와 `STATE.md`에 기록한다.
