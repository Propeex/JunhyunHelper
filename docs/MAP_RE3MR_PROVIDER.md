# MAP RE3MR PROVIDER — 상세 지도 + 자동 업데이트 정합

기록일: **2026-08-09**

상태: `GROUND ZERO FLOOR-AWARE FIX MERGED / WINDOWS USER VALIDATION PENDING`

## 목적

준현 헬퍼의 Map은 좌표 정확도뿐 아니라 실제 레이드에서 도로, 건물, 구역, 지명을 빠르게 읽을 수 있어야 한다.

사용자 실사용 검증에서 Tarkov.dev schematic SVG 및 Official Wiki artwork는 가독성 기준을 충족하지 못했다. presentation artwork는 gameplay coordinate source와 분리하며, 상세 지도 source를 독립 provider로 사용한다.

현재 첫 상세 provider는 **RE3MR Ground Zero**다.

---

## 제품 원칙

지도와 좌표는 같은 source일 필요가 없다.

```text
canonical gameplay/Quest coordinates
→ json.tarkov.dev + Tarkov.dev layout transform

presentation artwork
→ 별도 online artwork provider
→ canonical marker와 자동 calibration
```

패치마다 GPT나 사람이 새 좌표를 코드에 다시 입력하지 않는다. 온라인 source를 재다운로드하고 같은 변환/검증 공식을 재실행한다.

---

## RE3MR update pipeline

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

현재 Ground Zero visual anchors:

- Emercom Checkpoint
- Scav Checkpoint (Co-Op)
- Mira Ave
- Police Cordon V-Ex
- Nakatani Basement Stairs

과거 world X/Z를 코드에 고정하지 않는다. 좌표 source가 바뀌면 현재 canonical marker를 다시 사용해 transform을 계산한다.

---

## PR #60 — multi-floor 실제 적용 실패 수정

첫 Windows 검증에서 RE3MR 코드가 들어갔음에도 Ground Zero가 계속 Shebuka schematic으로 표시됐다.

원인:

```text
기존 Re3mrMapArtworkProvider
→ layout.Floors.Count > 1 이면 즉시 reject
```

현재 Tarkov.dev Ground Zero layout은 실제로 다음 floor를 가진다.

- Ground_Level
- Second_Floor
- Third_Floor
- Underground_Level / Garage

즉 기존 provider는 calibration을 시도하기도 전에 항상 거부되고 있었다.

PR #60에서 `GroundZeroRe3mrArtworkProviderV2`를 추가했다.

새 동작:

```text
기본층
→ RE3MR 상세 이미지
→ 현재 canonical extraction marker로 affine calibration

2층 / 3층 / Garage
→ 같은 refresh 시점에 현재 Tarkov.dev SVG 다운로드
→ 해당 SVG layer만 보이는 floor-specific SVG 생성

전체
→ 한 composite SVG에 floor group으로 저장
→ 기존 floor selector가 그대로 group show/hide
```

따라서 상세 기본층을 쓰기 위해 multi-floor 기능을 버리지 않는다.

---

## 실패 시 동작

1. floor-aware RE3MR Ground Zero provider 시도
2. 실패하면 기존 revision-aware RE3MR provider 시도
3. 실패하면 Official Wiki provider 시도
4. 실패하면 calibrated Tarkov.dev schematic SVG 사용
5. Map asset 전체 refresh가 실패하면 기존 active Map 유지

잘못 정렬된 최신 지도보다 검증된 fallback을 우선한다.

---

## 자동 refresh trigger

`MapAssetRefreshPolicy` 기준:

- active Map 없음/손상
- Game Content Map/marker fingerprint 변경
- Data Update 성공
- Map ingestion pipeline version 변경
- 마지막 성공 온라인 source 확인 후 24시간 경과
- 수동 지도 자산 다시 받기

PR #60 이후 pipeline version:

```text
map-online-sources-v5-floor-aware-re3mr
```

기존 설치는 LocalAppData cache를 직접 삭제하지 않아도 자동 재구축한다.

---

## 검증 상태

PR #60 자동 검증:

- Release Desktop build: success
- full automated tests: success
- Windows x64 self-contained publish: success
- multi-floor composite SVG regression test: success

남은 실제 검증:

- Windows에서 Ground Zero 기본층이 실제 RE3MR 상세 지도로 표시되는지
- 2층 / 3층 / Garage 전환이 기존대로 동작하는지
- PMC/Scav/Quest marker가 상세 지도 위에서 정확히 정렬되는지

실제 Windows 검증에서 floor-aware RE3MR도 안정적으로 적용되지 않으면, 사용자가 명시적으로 허용한 기존 `Propeex/Tarkov-Helper` 지도 자산을 presentation fallback 후보로 사용한다. 이 경우에도 gameplay coordinate update pipeline은 독립적으로 유지한다.
