# STATUS — v1.6.0 Scanner product workflow

상태: `IMPLEMENTED / RELEASE CANDIDATE`

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
- `준현 헬퍼.zip` / `준현 헬퍼/` stable package contract 추가
- Desktop target version 1.6.0 반영
- FIRST_RUN v1.6.0 갱신
- CI stable release ZIP 생성/검증 gate 추가

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
따라서 최종 release candidate는 최신 HEAD에서 다시 전체 CI를 통과해야 한다.

## 남은 release gate

1. 최신 v1.6.0 HEAD 전체 CI 성공
2. PR #174 ready + merge
3. main push CI 성공 확인
4. exact release source 고정
5. tag `v1.6.0`
6. GitHub stable/latest release publish
7. release asset `준현 헬퍼.zip` 업로드
8. SHA-256 기록
9. anonymous/public exact-tag + latest release 확인
10. public ZIP redownload
11. ZIP 최상위 `준현 헬퍼/` 구조 확인
12. public-downloaded EXE ProductVersion / Product UI / Map / Scanner smoke
13. 최종 release status 문서 갱신

## v1.6.0 이후

Scanner 개발은 live Ground Truth maintenance 단계로 복귀한다.

실사용 실패는 capture → proposal → semantic anchors → header lock → item ROI → OCR → substitution → matcher → visual recovery → Item ID → mapped presentation → overlay timing 순으로 분류한다.

REGRESSION=0을 유지한 변경만 PATCH 후보가 된다.
