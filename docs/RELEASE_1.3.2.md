# 준현 헬퍼 v1.3.2 공개 검증

## 릴리즈 식별자

- 버전: **v1.3.2**
- exact release source: `922797a99ea221fdc4984dd6ed05df552149d6e4`
- public tag source: `922797a99ea221fdc4984dd6ed05df552149d6e4`
- asset: `Junhyun-Helper-v1.3.2-win-x64.zip`
- bytes: `80311752`
- SHA-256: `6e3a7af2de50dfd14f1c49ccb39753177a0bce5b22993bb8bb94ffde93086767`
- ProductVersion: `1.3.2+922797a99ea221fdc4984dd6ed05df552149d6e4`

## 검증

- PR #144 final Windows CI `32619142034`: SUCCESS
- automated tests: **263 passed / 0 failed / 0 skipped**
- exact-source release workflow id (workflow_run trigger일 때): `32621021058`
- public/latest: VERIFIED
- exact public tag source: VERIFIED
- public ZIP redownload + checksum/layout/ProductVersion/FIRST_RUN: VERIFIED
- public-downloaded Product UI + Scanner + Mini Scanner + Main Map + Factory + MiniMap smoke: SUCCESS

## Scanner v1.3.2

- 실제 live/DisplayTest 사례에서 돋보기 뒤 글자 component 분할이 불안정해도 ring/hollow/handle + 좌측 헤더 위치로 돋보기를 유지합니다.
- OCR 문장부호/기호는 현재 공식 한국어 아이템명 카탈로그에서 실제 쓰이는 집합만 허용합니다.
- 중간 이상 길이 제목의 정확히 1 edit 오류는 current catalog 전체에서 후보가 유일하고 global runner-up과 **10%p 이상** 벌어질 때만 제한적으로 복구합니다.
- 저 80%대 다중 오류 OCR은 퍼센트만으로 확정하지 않고 strict Tarkov-font visual corroboration이 필요합니다.
- 최고 상점가, flea avg24hPrice, RequiredTotal 및 사용자 데이터 schema 의미는 변경하지 않았습니다.

검증 시각(UTC): `2026-08-23T05:46:02.2027611Z`
