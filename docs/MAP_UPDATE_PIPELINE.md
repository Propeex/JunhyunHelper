# MAP UPDATE PIPELINE — 지도/좌표 업데이트 대응 원칙

기록일: **2026-08-09**

상태: `APPROVED PRODUCT PRINCIPLE / IMPLEMENTATION IN PROGRESS`

## 최우선 요구사항

지도 기능은 현재 패치에서만 맞는 정적 자산 묶음이 아니다.

게임 패치로 **좌표 데이터 또는 지도 이미지가 변경되어도**, 일반적인 내용 변경이라면 프로그램이 온라인 원천을 다시 읽고 동일한 변환/검증 규칙으로 정상 자산을 재생성해야 한다.

```text
사람/GPT가 패치마다 지도와 좌표를 다시 맞춤  X

온라인 source
→ importer / alignment formula
→ candidate
→ 자동 검증
→ active 교체
→ 다음 패치에도 같은 과정 반복             O
```

변환 공식 자체가 더 이상 성립하지 않을 정도로 외부 형식/지도 구조가 바뀐 경우에만 개발 변경이 필요하다.

---

# 1. 좌표와 지도 이미지는 독립 시스템

사용자에게 보이는 지도 artwork와 gameplay/world coordinate source는 같은 곳에서 가져올 필요가 없다.

오히려 다음처럼 분리한다.

```text
[Canonical coordinate pipeline]
json.tarkov.dev gameplay/map data
+ tarkov.dev layout metadata
→ world X/Y/Z
→ floor / bounds / transform
→ canonical Map model

[Presentation artwork pipeline]
licensed detailed map provider
→ source revision 다운로드
→ artwork alignment
→ alignment 검증
→ normalized presentation SVG

                         ↓
                동일 Map surface에서 결합
                         ↓
quest / extract / player / user marker overlay
```

따라서 artwork provider를 바꿔도 Quest/Extract/Player의 canonical world position은 바뀌지 않는다.
좌표 source가 갱신되어도 artwork 파일 자체를 game-data DB에 복제하지 않는다.

---

# 2. Coordinate update policy

Game Content의 Map/marker/Quest geometry는 온라인 source에서 canonical model로 재구축한다.

Map presentation은 Game Content와 별도 저장소를 사용하지만 **Data Update가 성공하면 Map source도 stale 처리**한다.

다음 중 하나면 Map source를 다시 확인한다.

- active Map asset 없음/손상
- Game Content의 Map/marker fingerprint 변경
- Map ingestion pipeline version 변경
- 명시적인 Data Update 후 refresh request 존재
- 마지막 성공 refresh 후 24시간 경과
- 사용자가 수동으로 지도 자산 다시 받기 실행

현재 정상 active Map은 refresh 전에 삭제하지 않는다.

---

# 3. Artwork update policy

Artwork는 단순 다운로드 성공만으로 적용하지 않는다.

각 provider는 결과물을 **현재 canonical Map surface에 안전하게 정렬할 수 있음을 증명**해야 한다.

```text
provider source 확인
→ source revision/hash 확인
→ 이미지/SVG 다운로드
→ 기존/온라인 기준점으로 alignment 계산
→ residual / inlier / geometry 검증
→ candidate 생성
→ 검증 성공일 때만 active
```

정합 검증에 실패하면 다음 provider를 시도한다.
모든 상세 provider가 실패하면 마지막으로 좌표가 이미 검증된 schematic SVG를 사용한다.

잘못 정렬된 예쁜 지도보다 이전 정상 지도를 유지하는 것이 우선이다.

---

# 4. Provider priority와 교체 가능성

Artwork source는 interface 뒤에 둔다.

각 provider는 최소한 다음을 책임진다.

- Provider ID
- source revision 식별
- download
- attribution/license metadata
- canonical surface alignment
- alignment validation
- 실패 사유 반환

provider별 데이터 형식이 달라도 Desktop/Quest/Player marker 코드는 provider를 알 필요가 없다.

## Official Wiki

현재 구현되어 있는 provider.

장점:

- machine-readable marker coordinates로 canonical marker와 자동 대응 가능
- robust affine alignment를 매 refresh마다 다시 계산 가능
- 자동 업데이트 대응성이 좋음

단점:

- 실제 사용자 검증에서 background 자체의 도로/건물/랜드마크 가독성이 부족했음

결론: **정합 가능한 fallback provider로 유지**.

## RE3MR

상세하고 실전 가독성이 높은 presentation artwork의 현재 우선 후보.
사이트는 여러 EFT Map을 지속 갱신하고 CC BY-NC-SA 계열 라이선스를 명시한다.

단, RE3MR artwork 자체에는 EFT world coordinates가 포함되어 있지 않으므로 단순 hard-coded Stretch는 금지한다.

채택 조건:

1. 첫 validated revision을 canonical Map surface에 보정
2. source revision/hash 저장
3. 이후 일반적인 artwork revision은 이전 validated image와 자동 registration
4. registration 결과를 canonical anchor/residual로 검증
5. 검증 실패 시 새 revision 거부 + 이전 정상 artwork 유지

즉 **초기 변환 규칙은 개발할 수 있지만, 패치마다 사람이 새 좌표를 입력하는 구조는 채택하지 않는다.**

## Tarkov Market 계열 상세 SVG

기존 Tarkov Helper가 사용하던 가독성 높은 SVG의 출처를 추적한 결과, 과거 구현에서 Tarkov Market SVG를 추출한 뒤 기존 tarkov.dev transform을 viewBox 비율로 수동 마이그레이션한 이력이 확인됐다.

이 방식은 현재 제품 원칙에 부적합하다.

- map artwork 재배포 권한이 명확하지 않음
- 신규 SVG마다 사람이 추출/검토하는 흐름
- 단순 viewBox scale은 구조 변경 시 정합성을 증명하지 못함

따라서 기존 파일은 **목표 UX와 과거 시행착오 확인용 reference**로만 사용하며 새 JunhyunHelper의 온라인 artwork source로 복제하지 않는다.

---

# 5. Cache / rollback

Map cache 구조:

```text
map-cache/
├─ active/
├─ candidate/
├─ previous/
├─ update-state.json
└─ refresh.requested
```

`update-state.json`은 active/candidate 디렉터리 밖에 둔다.
자산 swap이 update metadata를 실수로 삭제하지 않도록 하기 위함이다.

state에는 최소한 다음을 기록한다.

- update-state schema version
- Map ingestion pipeline version
- current Game Content Map fingerprint
- last successful source refresh UTC

Artwork provider별 source revision/hash/alignment state는 provider metadata로 확장한다.

---

# 6. 실패 정책

다음은 update 실패로 간주하지만 기존 정상 사용을 막지 않는다.

- source network failure
- HTTP/schema 변경
- 이미지 손상/비지원 형식
- 기준점 부족
- alignment residual 초과
- coordinate transform invalid
- 한 Map만 다운로드 실패

정책:

```text
candidate 실패
→ candidate 폐기
→ active 유지
→ 해당 Map은 previous/fallback 사용
→ 사용자 progress/user marker는 절대 손상시키지 않음
```

---

# 7. 현재 구현 상태

이번 작업에서 추가한 기반:

- `MapAssetRefreshPolicy`
  - pipeline version
  - 24시간 freshness
  - Game Content Map fingerprint
  - Data Update refresh request
  - successful refresh state
- `MainWindow.Map`
  - Map 탭이 아직 생성되지 않았어도 Content activation 시 Map stale 표시
  - Map이 열려 있으면 새 content로 즉시 Map asset 재검증
  - refresh 실패 시 기존 active Map 유지
- `IMapArtworkProvider`
  - coordinate data와 presentation artwork provider 경계
- `MapArtworkProviderPipeline`
  - 우선 provider 실패 시 다음 provider로 안전 fallback
  - provider가 남긴 partial candidate 제거

다음 구현:

1. provider pipeline을 `MapAssetCacheService`에 연결
2. RE3MR provider source revision/hash importer 구현
3. Ground Zero를 첫 calibrated detailed artwork 대상으로 구축
4. source revision 변경 시 자동 image registration + 검증
5. Windows 실사용에서 coordinate overlay 검증
6. 성공 후 다른 Map으로 확대
