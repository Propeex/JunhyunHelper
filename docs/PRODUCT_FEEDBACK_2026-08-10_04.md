# Windows 실사용 피드백 — 설정 / 입력 / 성능 / 프로필 — 2026-08-10

상태: `IMPLEMENTED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

## 사용자 확정 요구사항

1. Map marker 체크, 단축키와 사용자 조정 설정은 재시작 후 유지한다.
2. 아이콘은 `데이터 업데이트`를 누르지 않아도 cold start에서 로드한다.
3. Main Map 확대/축소, 위층/아래층 단축키가 실제 Main Map에서 동작한다.
4. 별도 MiniMap 설정 진입 UI는 제거하고 필요한 조정은 Main Map `설정`에 둔다.
5. 설정 가능한 단축키로 MiniMap을 설정된 N초 동안 완전히 투명하게 한다.
6. Ammo 선택 행은 focus loss 후에도 dark theme를 유지한다.
7. Ammo 표에 세로 열 구분선을 표시한다.
8. Main Window 종료 시 JunhyunHelper 프로세스가 확실히 종료되어야 한다.
9. Map viewport가 상단 UI를 침범하지 않고, marker panel이 MiniMap 버튼을 간섭하지 않게 한다.
10. Quest/Hideout/Item 상태 변경의 불필요한 재계산과 연속 입력 stutter를 줄인다.
11. 기존 프로필 수정은 별도 저장 버튼 없이 창을 닫으면 저장한다. 새 프로필 생성은 명시적 생성 흐름을 유지한다.

---

## 구현 결과

### 1. JunhyunHelper-owned Map 설정 저장

Map 제품 설정의 권위 저장소를 다음으로 분리했다.

```text
%LocalAppData%/JunhyunHelper/map-product-settings.json
```

저장 대상:

- 일반 Map marker 체크 상태
- PMC / Scav / Transit 탈출구 표시
- Raider 표시
- 전역 Quest marker 표시
- 개별 Quest A/B/C marker 표시
- marker/player/extract 관련 사용자 조정값
- Map 설정 combo 선택값
- screenshot 폴더
- MiniMap ON/OFF, Map zoom, floor, MiniMap size 단축키
- MiniMap 일시 투명 단축키
- MiniMap 일시 투명 시간

legacy Tarkov Helper DB가 Map 초기화 후반에 단축키를 다시 읽어 JunhyunHelper 값을 덮어쓰지 못하도록 초기화 안정 구간 동안 제품 값을 재적용한다.

같은 단축키를 둘 이상의 제품 동작에 지정하면 마지막으로 지정한 동작만 남긴다.

### 2. 아이콘 cold-start 연결

Main Window 초기화 시점부터 다음 Page에 `ImageCacheService`를 연결한다.

- Hideout
- Items
- Ammo

Ammo favorite store 역시 같은 시점에 연결한다.

따라서 아이콘 로딩을 Data Update의 부수 효과에 의존하지 않는다.

### 3. Main Map 단축키 직접 실행

JunhyunHelper 전용 global hotkey dispatcher가 Main Map을 직접 조작한다.

- Map zoom in/out → 실제 Map zoom API
- floor up/down → 원본 floor selector 변경
- MiniMap ON/OFF
- MiniMap size increase/decrease
- MiniMap temporary hide

Main Map zoom/floor 동작은 MiniMap이 보이는지 여부에 의존하지 않는다. legacy zoom/floor direct hotkey는 중복 실행 방지를 위해 비활성화한다.

### 4. 별도 MiniMap 설정 UI 제거

Main Map 설정에 추가했던 별도 `미니맵 표시 설정` 버튼을 제거했다. 제품에서 필요한 사용자 조정은 Main Map `설정`과 hotkey로 제공한다.

### 5. MiniMap 일시 투명

Main Map 설정에 다음을 제공한다.

- 일시 투명 단축키
- 1~15초 duration slider

단축키 실행 시 MiniMap window 전체가 지정 시간 동안 `Opacity=0`이 된다.

기존 hover 투명화와 timed transparency는 하나의 presentation loop에서 결합한다.

```text
timed hide 활성 OR cursor hover
→ 0%

둘 다 비활성
→ 100%
```

click-through / top-right anchor / PlayerTracking 고정 정책에는 영향을 주지 않는다.

### 6. Ammo inactive selection 백화 수정

Ammo DataGrid의 active/inactive selection resource와 selected cell style을 dark theme로 명시했다. 다른 control 또는 바탕화면으로 focus를 옮겨도 선택 행이 흰색으로 바뀌지 않게 한다.

### 7. Ammo 열 구분선

Ammo DataGrid에 horizontal + vertical grid line을 활성화하고 theme border brush를 사용한다. column resize 시에도 열 경계를 식별할 수 있다.

### 8. 프로세스 종료 보장

Main Window를 닫을 때 다음을 정리한다.

- Map product runtime/timer
- Quest/marker projection runtime
- MiniMap overlay
- global keyboard hook
- auxiliary WPF runtime

WPF `ShutdownMode.OnMainWindowClose`와 명시적 application shutdown을 사용한다.

CI도 단순 force-kill smoke에서 실제 graceful shutdown 검증으로 강화했다.

```text
published JunhyunHelper.exe 실행
→ Map subsystem 초기화
→ Main Window에 정상 close 요청
→ 7초 이내 process exit 확인
```

정상 종료가 실패할 때만 CI cleanup에서 force kill한다.

### 9. Map viewport / marker panel

- Map viewer `ClipToBounds=true`
- 상단 경계 spacing 보정
- marker overlay가 viewport 높이를 넘지 않도록 동적 최대 높이 적용
- marker content 내부 scroll 적용

Map marker panel이 길어져 MiniMap 버튼을 덮지 않게 한다.

### 10. 상태 변경 성능

지연은 불가피한 것으로 판단하지 않았다. 다음 중복 작업을 제거했다.

#### profile SQLite cache

`UserProfileStore`에 process-local immutable profile snapshot cache를 추가했다.

- Save 성공 후 persisted canonical snapshot을 cache
- Load는 cache 우선
- LoadAll/Delete와 cache 정합 유지
- disk round-trip과 cache result의 normalization semantics 동일

기존 테스트가 `PrestigeLevel null → 0` normalization 차이를 잡았고, cache도 persisted canonical snapshot을 사용하도록 수정했다.

#### workspace memoization

Quest / Hideout / Items application service는 동일한 `GameContentCatalog + GameProfileSnapshot` 조합의 workspace 계산 결과를 재사용한다.

#### mutation-specific refresh

Quest 변경:

```text
Quest mutation 결과 재사용
→ Quest UI 갱신
→ Items만 다시 계산/갱신
→ Hideout 전체 재구축 생략
```

Hideout 변경:

```text
Hideout mutation 결과 재사용
→ Hideout UI 갱신
→ inventory 영향 때문에 Quest + Items만 다시 계산
→ profile SQLite 재조회 생략
```

#### rapid click coalescing

- Item 보유량 +/-: UI 숫자는 즉시 변경, 약 160ms 연속 클릭은 마지막 값으로 1회 저장/계산
- Hideout level +/-: UI level은 즉시 변경, 약 180ms 연속 클릭은 마지막 target level로 1회 저장/계산

#### icon reuse

Items/Hideout workspace refresh에서 이미 로드한 icon을 ID 기준으로 재사용하며 같은 icon을 반복 다운로드/디코딩하지 않는다.

### 11. 기존 프로필 close-to-save

기존 프로필 수정 창에서는:

- `저장` 버튼을 `닫기` 의미로 변경
- 별도 취소 버튼 비노출
- 창 X 또는 닫기 동작 시 현재 값을 검증하여 Result 생성
- 기존 MainWindow의 중앙 profile persistence 흐름으로 저장

프로필 삭제는 기존 확인 절차를 유지한다. 새 프로필 생성은 명시적 저장/생성 절차를 유지한다.

---

## 검증

코드 구현 head에서 다음 검증을 통과했다.

- Desktop Release build
- 전체 automated tests
- Windows x64 self-contained publish
- Startup + exact Map subsystem smoke
- 실제 Main Window graceful-close 후 process exit smoke
- ZIP 생성 및 artifact upload

최종 PR 문서 commit까지 포함한 head에서 위 전체 검증을 한 번 더 통과시킨 뒤 `main` 병합한다.

## Windows 사용자 검증 항목

- Map marker/Quest marker/hotkey 설정 변경 → 앱 재시작 → 값 유지
- Data Update 없이 앱 cold start에서 기존 icon 표시
- Main Map zoom/floor hotkey
- MiniMap size/ON-OFF/timed transparency hotkey
- timed transparency duration 후 자동 복귀 및 hover 투명화 병행
- Ammo 선택 행 focus loss 색상
- Ammo vertical separators
- Main Window 종료 후 Task Manager에 JunhyunHelper process가 남지 않는지
- Map marker panel / MiniMap button 간섭 여부
- Item/Hideout 연속 +/- 체감 반응성
- Quest 완료/취소 체감 반응성
- 기존 프로필 수정 후 X/닫기로 종료했을 때 값 저장
