# MAP RE3MR PROVIDER — 상세 지도 + 자동 업데이트 정합

기록일: **2026-08-09**

상태: `GROUND ZERO IMPLEMENTED / WINDOWS USER VALIDATION PENDING`

## 목적

준현 헬퍼의 Map은 좌표 정확도뿐 아니라 실제 레이드에서 도로, 건물, 구역, 지명을 빠르게 읽을 수 있어야 한다.

사용자 실사용 검증에서 Tarkov.dev schematic SVG 및 Official Wiki artwork는 가독성 기준을 충족하지 못했다.

따라서 presentation artwork는 gameplay coordinate source와 분리하며, 상세 지도 source를 독립 provider로 사용한다.

현재 첫 상세 provider는 **RE3MR Ground Zero**다.

---

## 핵심 원칙

RE3MR 이미지를 특정 픽셀 좌표에 고정해서 쓰지 않는다.

```text
RE3MR online page
→ current page version 확인
→ current image URL 다운로드
→ SHA256 계산
→ visual anchor 검증
→ current canonical extract world coordinates와 이름 매칭
→ image normalized coordinate → canonical Map surface affine calibration
→ residual / max error 검증
→ 통과한 candidate만 active
```

따라서 좌표 데이터가 변경된 경우에도 같은 artwork에서 현재 canonical marker를 다시 읽어 transform을 재계산한다.

---

## Artwork revision update

지도 이미지 자체가 업데이트될 수 있다.

provider state에는 다음을 저장한다.

- page version
- source image URL
- source SHA256
- image size
- validated named visual anchors
- calibration residual
- revision registration score
- validation timestamp

새 image hash가 이전과 다르면:

```text
새 이미지에서 기존 visual anchor 위치가 그대로 유효
→ 새 이미지로 다시 canonical calibration

기존 anchor 위치가 달라짐
→ 이전 validated image와 새 image 자동 registration
→ anchor 위치를 새 image 좌표로 이동
→ visual anchor 재검증
→ canonical calibration 재실행
```

현재 revision registration은 의도적으로 제한된 **global scale + translation**만 허용한다.

이 범위로 설명할 수 없는 대규모 재구성/회전/비선형 변경은 자동으로 억지 정합하지 않고 거부한다.

---

## 실패 시 동작

새 RE3MR revision의 다운로드/registration/calibration이 실패했을 때:

1. previous validated RE3MR artwork가 있으면 그대로 유지
2. previous RE3MR가 없으면 Official Wiki provider 시도
3. Wiki도 정합 검증 실패 시 calibrated Tarkov.dev schematic SVG 사용
4. Map asset 전체 refresh가 실패하면 기존 active Map 유지

잘못 정렬된 최신 지도보다 이전 정상 지도를 우선한다.

User Progress / user marker / Quest state는 Map artwork update와 분리되어 있어 손상하지 않는다.

---

## Ground Zero 1차 calibration

현재 Ground Zero provider는 상세 지도 안에서 시각적으로 식별 가능한 extraction marker를 기준점으로 사용한다.

기준점:

- Emercom Checkpoint
- Scav Checkpoint (Co-Op)
- Mira Ave
- Police Cordon V-Ex
- Nakatani Basement Stairs

provider는 이 이름을 현재 canonical Map marker와 다시 매칭한다.

단순히 과거의 world X/Z를 코드에 고정하지 않는다.

visual anchor가 사라지거나 이름/아이콘 체계가 크게 변경되어 검증할 수 없으면 새 revision을 적용하지 않는다.

---

## 자동 refresh trigger

`MapAssetRefreshPolicy` 기준:

- active Map 없음/손상
- Game Content Map/marker fingerprint 변경
- 사용자가 Data Update를 성공시킴
- Map ingestion pipeline version 변경
- 마지막 성공 온라인 source 확인 후 24시간 경과
- 수동 지도 자산 다시 받기

RE3MR provider 도입으로 pipeline version은 `map-online-sources-v4-re3mr`로 변경했다.

따라서 기존 설치도 LocalAppData cache를 직접 지우지 않아도 한 번 자동 재구축한다.

---

## 현재 범위

구현됨:

- Ground Zero
- single-plane artwork
- source version/hash 확인
- canonical extraction marker 기반 calibration
- visual anchor validation
- previous revision image registration
- previous validated artwork rollback
- Wiki / Tarkov.dev fallback
- synthetic image revision regression test
- full Desktop build / automated test 통과

아직 구현하지 않음:

- 다른 Map의 RE3MR calibration
- multi-floor Map의 floor별 상세 artwork
- Windows 실제 화면에서 Ground Zero RE3MR 표시 및 실제 marker 위치 검증

이 범위를 검증한 뒤 같은 provider 규칙을 다른 Map으로 확대한다.
