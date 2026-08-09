# MAP VISIBILITY ANALYSIS — 지도 시인성 점검

기록일: **2026-08-09**

상태: `READABILITY PASS IMPLEMENTED / WINDOWS USER VERIFICATION NEXT`

## 사용자 피드백

첫 실제 Map 화면 검증에서 다음 문제가 확인되었습니다.

1. Floor ComboBox가 `MapFloorDefinition { ... }` 전체 record 문자열을 표시해 상단 조작부를 비정상적으로 넓혔습니다.
2. 세관(Customs) 전체 지도에서 구조물과 도로가 배경에 묻혀 실제 플레이용 지도처럼 읽기 어려웠습니다.

## 원본 SVG 점검 결과

현재 배경은 `the-hideout/tarkov-dev-svg-maps`의 layered SVG입니다.

세관 원본 `Customs.svg`에는 `Ground_Level` 그룹 아래에 건물, 바닥, 계단/사다리, 도로, 철도, 울타리 등 실제 구조 geometry가 존재합니다. 따라서 준현 헬퍼의 floor filtering이 세관 구조물을 통째로 제거한 현상은 아닙니다.

다만 원본 공통 palette 자체가 interactive overlay 기반 도구에 맞춘 저대비 도식입니다. 현재 upstream `style_common.css`와 Customs SVG의 embedded style은 대표적으로 다음 색을 사용합니다.

```text
land     #1f5054
building #1a2632
trees    #144043
floor    #70777f
tarmac   #768089
```

특히 `land #1f5054`와 `building #1a2632`가 모두 어두운 계열이라 전체 지도를 한 화면에 맞추면 건물 footprint가 지형과 쉽게 섞입니다. 또한 이 SVG는 커뮤니티의 완성형 안내 지도처럼 지명/랜드마크 텍스트를 촘촘히 포함하는 형태가 아닙니다.

따라서 현재 첫 화면의 낮은 가독성은 두 요소가 합쳐진 결과입니다.

- source artwork 자체가 기능형/저대비 SVG
- 준현 헬퍼가 원본 palette를 그대로 표시

## 이번 결정

동적 좌표/층/마커 구조가 이미 Tarkov.dev SVG에 정합되어 있으므로 첫 대응에서 배경 source 자체를 임의의 제3자 raster map으로 교체하지 않습니다.

대신 **원본 SVG는 authoritative presentation source로 그대로 보존하고, Desktop 표시 시에만 high-contrast derivative를 생성**합니다.

```text
active raw SVG
→ floor filtering (기존)
→ JunhyunHelper readable presentation copy
→ SharpVectors render
```

원본 파일은 수정하지 않습니다.

### readable-v1 palette

구조물 우선 가독성을 위해 derivative에 별도 CSS override를 추가합니다.

```text
land        #244B4F
building    #D7DEE5 + dark outline
floor       #AEB8C2 + dark outline
cement      #D8DDE1 + dark outline
tarmac      #929DA7
road_tarmac #C3CBD2
fence       #E7F4E5
map_border  #DDE3E8
```

그 외 water / trees / railroad / gravel도 역할별 대비를 강화합니다. 이 보정은 SVG geometry와 viewBox를 변경하지 않으므로 기존 world→surface 좌표 변환과 marker alignment에는 영향을 주지 않습니다.

표시 사본은 `readable-v1` revision을 파일명에 포함해 과거 cached rendering과 섞이지 않습니다. 원본 Map과 MiniMap이 동일한 presentation transform을 사용합니다.

## Floor UI 수정

Floor ComboBox는 object `ToString()`을 사용하지 않고 `Name` property만 표시합니다.

예:

```text
기본층
2nd Floor
Basement
```

내부 `Id`, `SvgLayer`, height range, extent는 기존대로 선택/자동 층 판정에만 사용합니다.

## 향후 배경 source 교체 기준

이번 고대비 보정 후에도 사용자가 실제 레이드용으로 구조/랜드마크 파악이 부족하다고 판단하면 더 상세한 artwork source를 조사합니다.

그 경우에도 다음 조건을 충족해야 합니다.

- redistribution/derivative 사용 조건이 명확함
- 준현 헬퍼의 비상업적 사용과 호환됨
- 장기적으로 업데이트 가능한 안정적인 source
- dynamic marker 좌표와 안정적으로 calibration 가능
- static marker가 박힌 이미지 때문에 준현 헬퍼의 dynamic marker와 중복/충돌하지 않음

출처와 라이선스가 불명확한 커뮤니티 이미지를 임의로 패키징하지 않습니다.
