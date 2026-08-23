# 준현 헬퍼 v1.3.3 릴리즈 후보

기준일: 2026-08-23

상태: **RELEASE CANDIDATE — PUBLIC RELEASE PENDING**

## 목적

v1.3.2 공개 후 실제 Tarkov 상세창 12개에서 재확인된 title-start / magnifier-anchor 회귀를 수정하는 PATCH다.

## Scanner 변경

- actual long neutral header frame + red close/X + bounded frame-left search-icon lane을 title ROI의 authoritative geometry로 사용
- 12개 표본 모두에서 반복된 13×13 magnifier bright core와 상대 위치를 회귀 범위로 고정
- first title glyph가 title ROI left edge를 이동시키는 경로 제거
- full `HEADER_FRAME_LOCKED` 전에는 OCR identity path로 진행하지 않음
- raw OCR / sanitized matcher input diagnostics 분리
- current-catalog symbol sanitation, normal confidence/margin, bounded unique one-edit rule 유지
- highest trader, flea avg24hPrice, RequiredTotal 의미 변경 없음

## 호환성

- Desktop: 1.3.3
- Content schema: v7 유지, readable v3~v7
- user.db: v1 유지
- Scanner display settings: v4 유지
- Scanner catalog cache: v1/v2 readable, v2 written
- mandatory Game Content update: 없음
- user migration: 없음

## 현재 검증

- v1.3.3 candidate CI `32624123821`: SUCCESS
- follow-up CI `32624855995`: SUCCESS
- automated tests: 263 passed / 0 failed / 0 skipped
- Windows Release build/publish: SUCCESS
- actual packaged EXE Product UI + Scanner + Mini Scanner + Main Map + Factory + MiniMap smoke: SUCCESS
- 12-case cleaned final-head CI: PENDING
- exact release source: PENDING MERGE
- Draft/public package re-download verification: PENDING
