# NEXT

기준일: 2026-08-23

현재 우선순위는 Scanner v1.3.2 live-evidence correction의 Windows release gate다.

1. real-like magnifier association hardening 적용/회귀 검증
2. current-catalog punctuation sanitation regression
3. unique single-edit bounded matcher regression
4. 전체 automated tests / win-x64 publish / actual packaged EXE Product UI + Scanner + Map smoke
5. 사용자 제공 실제 두 사례를 다시 재현 가능한 synthetic regression으로 고정
6. gate 성공 후 v1.3.2 release identity/docs 고정 및 정식 공개
7. 이후 실제 Tarkov 사용에서 새 evidence가 생기면 동일하게 capture → anchor/ROI → OCR → catalog matcher/visual 단계로 분리
