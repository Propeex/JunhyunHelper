# MAP BACKGROUND SOURCE ANALYSIS — 실사용 지도 배경 교체

기록일: **2026-08-09**

상태: `OFFICIAL WIKI BACKGROUND IMPLEMENTATION / SAFE CALIBRATION REQUIRED`

## 계기

Windows 실사용 화면에서 `readable-v1` 고대비 변환을 적용한 Tarkov.dev/Shebuka SVG도 실제 레이드용 배경으로는 구조물·랜드마크 파악이 충분히 쉽지 않다는 사용자 검증 결과가 나왔다.

따라서 기존 SVG의 색만 계속 조절하지 않고 **배경 artwork source 자체를 교체**한다.

## 검토한 source

### 기존 Tarkov.dev / Shebuka SVG

장점:

- 기존 world coordinate transform과 정확히 정합됨
- 층별 SVG layer가 있음
- redistribution/derivative 조건이 이미 정리됨

문제:

- 기능형 schematic 성격이 강함
- 전체 축척에서 구조와 랜드마크를 빠르게 읽기 어려움
- `readable-v1` 보정 후에도 사용자 실사용 기준을 충족하지 못함

결론: **기본 artwork에서는 후퇴시키고 안전 fallback으로 유지**.

### RE3MR

장점:

- 매우 높은 시각적 디테일
- 주요 지역을 폭넓게 제공
- CC BY-NC-SA 4.0 표기

문제:

- 제작자 FAQ에서 자신의 지도를 웹사이트/애플리케이션에 올리는 경우 직접 연락하라고 명시함
- 준현 헬퍼가 별도 확인 없이 즉시 패키징하는 source로 채택하기에는 운영 정책이 충분히 명확하지 않음

결론: **현재 기본 source로 사용하지 않음**. 향후 명시적 애플리케이션 사용 허가가 확보되면 재검토 가능.

### Escape from Tarkov Official Wiki Interactive Maps

특징:

- `Map:<location>` Interactive Map을 지도별로 운영
- Fandom Interactive Maps JSON 자체에 `mapImage`, `mapBounds`, `origin`, `coordinateOrder`, `markers`가 존재
- Wiki community content는 별도 표시가 없는 한 CC BY-NC-SA
- 사용자에게 익숙한 상세 2D 배경을 사용

결론: **새 single-plane Map artwork의 1차 기본 source로 채택**.

## 가장 중요한 정합 원칙

보기 좋은 이미지를 단순 Stretch해서 기존 marker를 올리지 않는다.

Wiki Interactive Map은 Wiki 자체 좌표계를 사용하고 준현 헬퍼 canonical Map marker는 EFT world X/Y/Z 좌표를 사용한다. 따라서 다음 과정을 통과한 배경만 적용한다.

```text
Official Wiki Map JSON
→ mapImage + Wiki marker positions

JunhyunHelper Game Content
→ canonical extracts/transits + world positions

stable marker name match
→ Wiki normalized position ↔ current calibrated surface position pair 생성
→ robust affine transform 계산
→ inlier/residual 검증
→ 검증 성공 시 Wiki image를 local SVG wrapper로 변환
→ 기존 Quest / marker / screenshot position overlay와 동일 surface에 표시
```

정합을 증명할 matching marker가 부족하거나 residual이 기준을 넘으면 **Wiki 이미지를 적용하지 않는다**.

그 경우 기존 Tarkov.dev SVG를 그대로 사용한다.

이 원칙 때문에 지도 배경 교체가 marker coordinate 정확도를 희생하지 않는다.

## 자동 업데이트 원칙

패치 때 GPT가 Wiki 이미지를 다시 보고 수동으로 좌표를 맞추지 않는다.

Wiki map source가 업데이트되어도 프로그램이 다음을 반복한다.

```text
Wiki Map source 다운로드
→ marker match
→ transform 재계산
→ 정합 검증
→ candidate 생성
→ 정상일 때만 active 교체
```

잘못 정렬된 새 배경보다 이전 정상 배경/fallback을 유지하는 것을 우선한다.

## multi-floor

하나의 평면 이미지에 여러 층이 분리 배치된 경우 global affine transform 하나로 world X/Z를 정확히 표현할 수 없다.

따라서 이번 1차 교체에서는:

- single-plane layout: Official Wiki 배경을 자동 정합해 우선 사용
- multi-floor layout: 기존 calibrated layered SVG 유지

multi-floor는 이후 floor별 artwork source/좌표계를 별도로 검증한 뒤 교체한다.

## Attribution

Wiki 배경을 실제 적용한 layout은 화면 attribution을 다음 source로 변경한다.

```text
Escape from Tarkov Wiki Interactive Map · CC BY-NC-SA
```

원본 페이지 URL도 함께 유지한다.

fallback SVG를 사용하는 layout은 기존 Tarkov.dev/Shebuka attribution을 유지한다.

## 구현 위치

- `FandomMapArtworkService`
  - Interactive Map JSON 읽기
  - source image 다운로드
  - marker matching
  - robust affine calibration
  - 정합 검증
  - self-contained local SVG wrapper 생성
- `MapAssetCacheService`
  - Wiki background 우선 시도
  - 실패/불확실 시 기존 SVG fallback
  - active/candidate/previous 안전 교체 구조 유지

## 다음 검증

1. Windows 환경에서 Customs가 Official Wiki 배경으로 실제 표시되는지 확인
2. attribution source 변경 확인
3. 기존 extract/Quest marker가 배경의 실제 위치와 일치하는지 확인
4. screenshot current-position marker 정합 확인
5. Wiki raw/API 응답 변경이나 네트워크 실패 시 기존 SVG fallback 확인
6. 다른 single-plane Map 확대 적용 결과 확인
7. multi-floor artwork 별도 설계
