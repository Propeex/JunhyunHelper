# STATUS — v1.6.0 Scanner product workflow

상태: `IMPLEMENTED / PUBLIC RELEASE VERIFIED`

기준일: 2026-08-24

## 구현 완료

- Scanner 일반 화면을 `스캐너 ON/OFF / 설정 / 고급` 세 동작 중심으로 재구성
- Scanner 하단을 `아이템 검색 / 로그` 좌우 2분할로 구성
- local full-item catalog 기반 Scanner 아이템 검색 추가
- 검색 결과 아이콘 + 공식 아이템명 표시
- 선택 아이템의 Wiki / 플리 평균가 / 최고 상인 판매가 / 필요 개수 표시
- 검색 중 network work 금지
- 기존 3종 전역 단축키 유지
- Scanner display settings schema v6 도입
- Mini Scanner 아이콘 + 아이템명 fixed header
- Mini Scanner 다섯 정보의 표시 여부 + 순서 저장
- 최고 상인명 + 판매가 presentation
- Scanner 고급 화면 정리
- 교정 창 자동 축소 표시 + 원본 좌표계 보존
- 상세창/X/돋보기/item-name ROI 후보 이미지 직접 클릭 선택
- `없음` / 직접 지정 fallback 유지
- 저장된 Case 재열기 및 재교정
- 기존 candidate_selection / Ground Truth 복원
- `Junhyun-Helper.zip` / `준현 헬퍼/` stable package contract 추가
- v1.6.0 updater가 stable `Junhyun-Helper.zip` package를 우선 선택하고 `준현 헬퍼/` wrapper folder를 안전하게 unwrap하도록 변경
- legacy versioned package fallback 유지
- 공백 포함 stable package filename을 정확히 읽는 SHA256SUMS parser 보완
- v1.5.0 updater용 `Junhyun-Helper-v1.6.0-win-x64.zip` 일회성 bridge package 추가
- Desktop target version 1.6.0 반영
- FIRST_RUN v1.6.0 갱신
- CI stable/bridge ZIP + SHA256SUMS 생성/검증 gate 추가

## 변경하지 않은 Scanner safety contract

- structural floor 0.34
- HEADER_FRAME_LOCKED floor 0.68
- continuous cap 8
- one-shot cap 12
- magnifier + red close-X semantic gate
- current official catalog identity authority
- scan-time network 금지
- game memory / DLL injection / packet interception 금지
- cross-frame OCR cache 금지

## 중간 검증 기록

### CI 32700507526

HEAD 이전 smoke-fix 기준에서 다음이 모두 성공했다.

- Desktop build: SUCCESS
- automated tests: 296 passed / 0 failed / 0 skipped
- Windows x64 publish: SUCCESS
- rendered Product UI smoke: SUCCESS
- Map / Factory / MiniMap smoke: SUCCESS
- graceful shutdown: SUCCESS
- artifact upload: SUCCESS

이 성공 후 release identity를 1.6.0으로 올리고 stable ZIP CI gate를 추가했다.

### CI 32703012551

Program Update stable-package 전환과 checksum parser 수정 기준에서 다음 release gate가 성공했다.

- Desktop build: SUCCESS
- automated tests: 299 passed / 0 failed / 0 skipped
- Windows x64 publish: SUCCESS
- rendered Product UI / Scanner / Mini Scanner smoke: SUCCESS
- Main Map / Factory / MiniMap smoke: SUCCESS
- graceful shutdown: SUCCESS
- stable `Junhyun-Helper.zip` package gate: SUCCESS
- v1.5 updater bridge ZIP gate: SUCCESS
- stable/bridge SHA256SUMS verification: SUCCESS
- artifact upload: SUCCESS

이후 updater transition 규칙을 release/decision/status 문서에 명시했으므로 최종 HEAD는 문서 반영 후 CI를 한 번 더 통과해야 한다.

## 최종 release verification

교정 PR #175 HEAD `ad7f4259a44c72902fba452adbbcfd4c540ae577`는 CI `32708999577`에서 전체 release gate를 통과했다.

- Desktop build: SUCCESS
- automated tests: 299 passed / 0 failed / 0 skipped
- Windows x64 publish: SUCCESS
- rendered Product UI / Scanner / Mini Scanner smoke: SUCCESS
- Main Map / Factory / MiniMap smoke: SUCCESS
- graceful shutdown: SUCCESS
- stable/bridge package + checksum gate: SUCCESS

최종 제품 source:

```text
v1.6.0 exact release source/tag: e18c108380572913552030aa677bba06ebf49355
ProductVersion: 1.6.0+e18c108380572913552030aa677bba06ebf49355
```

초기 final controller `32709414932`는 build/test/publish/package/product smoke/tag retarget/draft asset 교체까지 성공했으나 draft manifest 비교용 PowerShell 코드의 `TrimStart` 인자 오류에서 fail closed했다. 이때 stable/latest 공개는 수행하지 않았다.

Recovery/final public verification run `32710012954`는 성공했다.

```text
stable asset: Junhyun-Helper.zip
stable bytes: 80,425,013
stable SHA-256: f9384ff49d522afb5976efe291ff932d66063dcfeee64b0aed7a5daa691a12c5
v1.5 bridge: Junhyun-Helper-v1.6.0-win-x64.zip
bridge bytes: 80,424,089
bridge SHA-256: 3f05b20ccbd7463fb590889042b1b706290a88e0568cd00c3b2fa23cf966dfc8
public/latest: VERIFIED
anonymous public redownload: VERIFIED
public ProductVersion: VERIFIED
public-downloaded EXE Product UI/Scanner/Mini Scanner/Map smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
verified_at_utc: 2026-08-24T09:10:44.9630720Z
```

Durable release record:

- `docs/.release-v1.6.0-status.json`
- `docs/RELEASE_1.6.0.md`

## v1.6.0 이후

Scanner 개발은 live Ground Truth maintenance 단계로 복귀한다.

실사용 실패는 capture → proposal → semantic anchors → header lock → item ROI → OCR → substitution → matcher → visual recovery → Item ID → mapped presentation → overlay timing 순으로 분류한다.

REGRESSION=0을 유지한 변경만 PATCH 후보가 된다.
