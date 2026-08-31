# 준현 헬퍼 v1.12.1

## 김태영 PC 진단 UX 보완

- 진단 아이콘 클릭 시 확인 문구를 `혹시 김태영 본인?`으로 단순화했습니다.
- `예`를 누른 뒤 진단 ZIP 생성이 끝날 때까지 별도 indeterminate 진행 바를 표시합니다.
- 정상 완료 메시지는 다음 두 문장만 표시합니다.
  - `진단 완료.`
  - `파일을 hyune4784@naver.com 으로 보내주세요.`
- 완료 안내를 닫은 뒤 기본 브라우저에서 `https://mail.naver.com/v2/new` 네이버 메일 쓰기 페이지를 엽니다.
- 진단 ZIP은 기존과 동일하게 바탕화면에 로컬 생성하며 자동 업로드, 웹메일 자동 첨부, 자동 이메일 전송은 하지 않습니다.

## 진단 ZIP 실사용 확인

사용자 노트북에서 v1.12.0 진단 ZIP 생성 경로를 실제 확인했습니다.

- expected top-level evidence 11개 생성
- `probe-errors.txt = none`
- display screenshot / 밝기 통계 생성 정상
- Scanner support bundle 포함 정상
- Scanner/catalog snapshot 포함 정상
- 실행 당시 Tarkov가 없어 Tarkov window dual-capture evidence는 이번 샘플에 없음

이 확인은 진단 exporter 자체의 정상 동작 증거이며 김태영 실제 PC의 밝기/capture 문제 원인 판정은 김태영 PC에서 생성한 ZIP이 필요합니다.
