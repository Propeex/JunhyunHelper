# RELEASE 1.4.2 — Scanner live Ground Truth recognition fix

상태: `RELEASE CANDIDATE`

기준일: 2026-08-24

## 범위

v1.4.2는 새 사용자 기능을 추가하지 않는 PATCH 릴리즈다. v1.4.1을 실제 Tarkov에서 사용한 뒤 제출된 교정 데이터 61 Case / 사용자 검증 16 Case를 분석하여 확인된 상세보기 창 탐지 실패, 아이템명 OCR/매칭 실패, Scanner 단축키 설정 창 clipping을 수정한다. Scanner 인식 속도 최적화는 정확도·안정화 이후 작업으로 남기며 이번 릴리즈 범위에 포함하지 않는다.

## 제품 변경

### 상세보기 창

- 일부 실제 화면에서 stash/inventory의 큰 구조 프레임이 coarse detail 후보로 선택되고, 실제 상세창 헤더가 그 내부 수백 px 아래에 존재해 기존 상단 주변 header 탐색으로는 도달하지 못하는 패턴을 확인했다.
- 기존 detector primary 및 v1.4.1 live header Ground Truth fallback을 그대로 우선 사용한다.
- 두 기존 경로가 모두 실패한 경우에만 oversized candidate 내부에서 contained-subpanel proposal을 제한적으로 탐색한다.
- contained proposal은 단순 border/rectangle만으로 성공하지 않는다. 기존과 동일하게 다음 semantic header evidence를 다시 요구한다.
  - close X
  - magnifier
  - dark title field
  - title text evidence
  - `HEADER_FRAME_LOCKED`
  - title anchor score `>= 0.68`
- 따라서 stash/inventory frame 자체를 실제 상세창으로 확정하는 방향으로 임계값을 완화하지 않는다.

### 아이템명 OCR / matcher

- reviewed Ground Truth에서 `Emelya 에멜야 호밀 크루통`, `Grizzly 응급 치료 키트`처럼 정답 공식 아이템이 matcher 1위 후보임에도 OCR 2~3 glyph 오류 때문에 `LOW_CONFIDENCE`로 최종 거부되는 사례를 확인했다.
- 일반 matcher threshold를 낮추지 않고 기존 ordinary matcher가 실패한 경우에만 bounded recovery를 평가한다.
- 2-edit 복구는 현재 공식 카탈로그 전체에서 후보가 유일하고 충분한 global separation을 가진 경우에만 허용한다.
- 2~3-edit long-suffix 복구는 충분히 긴 후반부 문자열이 일치하고 카탈로그 전체에서 후보가 유일한 경우에만 허용한다.
- 전역 `r`, `0`, 한글 glyph 치환표를 추가하지 않는다. 실제 문자 오인식은 카탈로그 후보 evidence와 함께 판단한다.
- 기존 다중 오인식 low-80s 사례 및 근접 runner-up이 있는 사례는 계속 fail-closed 한다.

### Scanner 단축키 설정 창

- `스캐너 ON/OFF` 세 번째 행의 텍스트가 창 하단에서 잘리던 레이아웃을 수정했다.
- 기능/단축키 계약 자체는 변경하지 않는다.

## 변경하지 않은 계약

- structural floor: `0.34`
- trusted header floor: `0.68`
- Windows ko-KR OCR primary/deep 정책
- Tarkov-font visual recovery
- 공식 한국어 full item catalog authority
- false positive가 miss보다 더 나쁘다는 fail-closed 원칙
- 가격·플리 평균가·슬롯·필요 개수는 OCR 필드가 아니라 Item ID 확정 뒤 `mapped_data`로 조회하는 구조
- 실시간 스캔 순간 network 미사용
- 게임 메모리 읽기 / DLL injection / packet interception 미사용
- 연속 Scanner 성능/속도 최적화는 이번 범위에서 제외

## Ground Truth 근거

사용자가 v1.4.1을 실제 Tarkov에서 사용하고 교정 데이터 ZIP을 제출했다.

- 전체 Case: `61`
- 사용자 검증 Case: `16`
- 상세창 실패 reviewed 사례에서는 실제 상세창이 oversized stash/inventory 구조 후보 내부에 존재하는 패턴이 반복되었다.
- OCR reviewed 사례에서는 `Grizzly`, `Emelya`, `Iskra`, `Axel` 등 영문 소문자/숫자형 glyph/복잡한 한글에서 실제 OCR 혼동이 확인되었다.
- 일부 OCR 실패는 정답 item이 이미 matcher top-1이었지만 보수적 confidence gate 때문에 최종 식별이 보류된 사례였다.

이번 변경은 위 reviewed Ground Truth에서 확인한 실패 단계만 보완하며, 실제 데이터 없이 일반 threshold를 완화하지 않는다.

## 회귀 방지

- 사용자 screenshot bytes 자체를 저장소 테스트 fixture로 넣지 않는다.
- oversized outer frame 내부의 실제 detail header 구조를 synthetic product smoke로 재현하여 contained-subpanel fallback이 기존 header-lock evidence를 다시 통과하는지 검증한다.
- OCR matcher tests는 reviewed Ground Truth에서 확인된 bounded 2-edit / long-suffix 복구를 검증하는 동시에 기존 multi-edit/ambiguous 사례가 계속 fail-closed 하는지 함께 고정한다.

## 수정 PR 검증

PR #160 final head: `a6b2a13c05f585be5c463291ed05a4fb6c29c39b`

CI run: `32656154735`

- Windows Desktop build: SUCCESS
- Core tests: `272 passed / 0 failed / 0 skipped`
- Windows x64 self-contained single-file publish: SUCCESS
- package layout audit: SUCCESS
- packaged EXE rendered Product UI + Map/Factory/MiniMap + Scanner smoke: SUCCESS
- graceful shutdown + clean portable root: SUCCESS

PR #160 merge commit: `40852863e1f897493287597e99a77174098f8a05`

## 릴리즈 게이트

아래가 모두 충족되어야 PUBLIC RELEASE / VERIFIED로 전환한다.

1. release-prep PR CI success
2. exact release source SHA 고정
3. tag `v1.4.2`가 exact release source를 가리킴
4. `Junhyun-Helper-v1.4.2-win-x64.zip` 공개
5. public latest가 v1.4.2
6. public ZIP 재다운로드 SHA-256 검증
7. 공개 `SHA256SUMS.txt`와 실제 ZIP hash 일치
8. package layout 및 ProductVersion `1.4.2+<source_sha>` 검증
9. 공개 ZIP의 실제 EXE product smoke / graceful shutdown 성공
10. 독립 public verifier 성공 및 영구 status 문서 기록
