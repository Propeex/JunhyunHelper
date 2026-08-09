# MAP SOURCE DECISION — 2026-08-09

## 사용자 검증

Windows 실사용에서 현재 Tarkov.dev/Official Wiki 기반 Map은 좌표 기능은 동작하지만 도로·구조물·랜드마크를 빠르게 읽기 어렵다고 확정했다.
기존 Tarkov Helper처럼 도로/건물/지명이 명확한 실전용 presentation이 필요하다.

## 기존 Tarkov Helper 조사

기존 상세 SVG는 과거 Tarkov Market에서 추출한 artwork를 사용했고, tarkov.dev의 기존 transform을 새 SVG viewBox 비율에 맞춰 수동 스케일링한 이력이 확인됐다.

이 자산은 목표 UX reference로는 유용하지만 새 JunhyunHelper 기본 source로 그대로 사용하지 않는다.

이유:

- 제3자 artwork의 재배포 권한이 명확하지 않음
- 과거 migration은 새 SVG를 사람이 가져오고 viewBox scale을 계산하는 절차였음
- 구조가 바뀐 새 지도에서 단순 viewBox scale만으로 좌표 정합성을 증명할 수 없음

## 현재 우선 후보: RE3MR

RE3MR는 사용자가 원하는 상세/실전형 지도에 가까우며 여러 EFT Map을 지속 갱신하고 있다.
사이트는 Creative Commons Attribution-NonCommercial-ShareAlike 계열 라이선스를 표시한다.

단, RE3MR 이미지는 자체 EFT world coordinate metadata를 제공하지 않으므로 **정적 hard-coded 좌표만 붙이는 방식은 금지**한다.

채택 방식:

```text
RE3MR page
→ current non-deprecated revision / image URL 확인
→ image download + hash
→ validated previous artwork와 registration
→ canonical world anchors로 residual 검증
→ 통과한 candidate만 active
```

첫 revision만 초기 calibration이 필요하다. 이후 같은 지도 계열의 일반적인 이미지 업데이트는 프로그램이 image registration으로 새 transform을 계산한다.
registration이 실패할 정도로 지도의 전체 구도/좌표계가 바뀐 경우에는 새 이미지를 거부하고 이전 정상 artwork를 유지한다.

이때만 importer/registration formula의 새 버전 개발이 필요하며, 일반 패치마다 GPT가 좌표를 손으로 다시 넣지는 않는다.

## Fallback

1. 상세 licensed provider (RE3MR; 정합 검증 성공한 Map)
2. Official Wiki provider (machine-readable marker 기반 자동 affine 정합)
3. calibrated Tarkov.dev schematic SVG
4. refresh 실패 시 previous active asset

좌표 source는 위 artwork source와 독립적으로 json.tarkov.dev/tarkov.dev canonical data를 따른다.
