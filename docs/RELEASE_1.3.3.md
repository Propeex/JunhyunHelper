# 준현 헬퍼 v1.3.3 공개 검증

## 릴리즈 식별자

- 버전: **v1.3.3**
- exact release source: $env:SOURCE_SHA
- public tag source: $tagSha
- asset: $env:ASSET_NAME
- bytes: $bytes
- SHA-256: $hash
- ProductVersion: $env:RELEASE_VERSION+41bf5b8374ba774866aab4b60a25376d9b5548c2

## 검증

- PR #145 final Windows CI $env:FINAL_PR_CI: SUCCESS
- automated tests: **263 passed / 0 failed / 0 skipped**
- exact-source release workflow: $releaseRunId — SUCCESS
- public/latest: VERIFIED
- exact public tag source: VERIFIED
- independent public ZIP re-download + SHA256SUMS/layout/ProductVersion/FIRST_RUN: VERIFIED
- public-downloaded Product UI + Scanner + Mini Scanner + Main Map + Factory + MiniMap smoke: SUCCESS

## Scanner v1.3.3

- 실제 Tarkov 2048x1280 상세창 12개에서 측정한 long neutral top frame, red close/X, bounded left search-icon lane, 13×13 magnifier bright core를 title ROI의 authoritative 구조로 사용합니다.
- first title glyph connected component는 더 이상 title ROI left edge를 결정하지 않습니다.
- HEADER_FRAME_LOCKED와 68% 이상의 anchor score가 없으면 OCR identity path로 진행하지 않습니다.
- raw Windows OCR과 current-catalog sanitation 후 matcher input을 진단에서 분리합니다.
- normal confidence/top1-top2 margin 및 bounded unique one-edit safety는 완화하지 않았습니다.
- 최고 상점가, flea avg24hPrice, RequiredTotal 및 사용자/content/settings schema 의미는 변경하지 않았습니다.

검증 시각(UTC): $verifiedUtc