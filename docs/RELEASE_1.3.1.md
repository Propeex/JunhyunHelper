# 준현 헬퍼 v1.3.1 공개 검증

## 릴리즈 식별자

- 버전: **v1.3.1**
- exact release source: `028bfb600f4662962a0daac1dad04b570e018275`
- public tag source: `028bfb600f4662962a0daac1dad04b570e018275`
- asset: `Junhyun-Helper-v1.3.1-win-x64.zip`
- bytes: `80310221`
- SHA-256: `5c4b79cc5d373b4a28cbeb10be18b8369086b2ee9f0edc172530028dd71b1c3f`
- ProductVersion: `1.3.1+028bfb600f4662962a0daac1dad04b570e018275`

## 검증

- PR #143 final Windows CI `32615869812`: SUCCESS
- automated tests: **256 passed / 0 failed / 0 skipped**
- exact public tag source: VERIFIED
- public/latest: VERIFIED
- public ZIP re-download: VERIFIED
- public SHA256SUMS: VERIFIED
- ZIP root `준현 헬퍼.exe / FIRST_RUN_KO.txt / Assets`: VERIFIED
- ProductVersion / FIRST_RUN identity: VERIFIED
- public-downloaded Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke: SUCCESS
- graceful shutdown / portable-root cleanliness: SUCCESS

## v1.3.1 범위

- inspect-header를 title field + magnifier shape + first glyphs + red close/X 결합 구조로 인식
- panel-left drift에서 첫 한글 글자를 magnifier로 오인하는 회귀 방지
- Windows ko-KR OCR semantic success에 local Tarkov-font/current-catalog 시각 corroboration 추가
- strict visual evidence가 명확한 경우에만 current official Item ID 안에서 OCR identity 교정
- 상단 상태 텍스트 왼쪽에 실제 실행 EXE 버전 표시
- 가격/플리/RequiredTotal, Content schema, user.db schema, Scanner settings schema 의미 변경 없음

상세 recognition 계약은 `docs/SCANNER_V1.3.1_RECOGNITION.md`를 참조합니다.
