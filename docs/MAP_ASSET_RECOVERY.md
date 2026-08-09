# MAP ASSET RECOVERY — 지도 자산 자동 복구

기록일: **2026-08-09**

상태: `IMPLEMENTED / WINDOWS FILE-LOCK FIX MERGED / USER TESTING`

## 현상

지도 탭 자체는 정상 열리지만 초기 Map asset이 없거나 갱신이 실패하면 다음 상태가 표시될 수 있었습니다.

```text
표시할 지도 자산이 없습니다.
```

PR #48 이후에는 자동 복구가 시작되었지만 Windows에서 다음 오류로 끝났습니다.

```text
지도 SVG를 하나도 준비하지 못했습니다.
```

## 복구 구조 취약점

Map gameplay data와 Map presentation asset은 의도적으로 분리되어 있습니다.

초기 구현에는 다음 복구 취약점이 있었습니다.

1. `content.db`가 이미 schema v4이면 active Map asset이 비어 있어도 전체 Game Content update가 자동 재실행되지 않았습니다.
2. Map SVG 하나의 다운로드/검증 실패가 전체 Map candidate를 폐기했습니다. 이전 active Map asset이 전혀 없는 최초 실패에서는 모든 지도가 빈 상태로 남을 수 있었습니다.
3. Map asset update 실패는 canonical Game Content를 손상시키지 않기 위해 warning으로 취급했지만, 그 결과 사용자가 다음 실행에서 자동 회복할 수 있는 경로가 부족했습니다.

PR #48에서 이 세 문제를 self-heal / 지도별 부분 복구 / source fallback으로 보완했습니다.

## Windows에서 모든 SVG가 실패한 실제 원인 — PR #50

PR #48의 복구 경로가 실제 사용자 Windows PC에서 실행되면서 공통 파일 수명주기 오류가 드러났습니다.

기존 `MapAssetCacheService`는 다음 순서였습니다.

```text
HTTP download
→ destination FileStream(FileShare.None) 열기
→ 파일 쓰기 + Flush
→ writer가 아직 살아있는 상태에서 ValidateSvg(destination)
→ validator가 같은 파일을 다시 open
```

Windows에서는 `FileShare.None` writer가 살아있는 동안 두 번째 open이 정상적으로 거부됩니다.

따라서 실제 HTTP 다운로드가 성공했더라도 `ValidateSvg()`가 IOException 계열 오류로 실패했습니다. 모든 Map SVG가 동일한 코드를 사용하므로 결과적으로 **모든 지도 다운로드가 실패한 것처럼 보였습니다.**

Marker PNG 다운로드도 같은 수명주기 패턴을 사용하고 있어 동일하게 수정했습니다.

### 수정된 수명주기

```text
HTTP download
→ destination FileStream(FileShare.None) 열기
→ 파일 쓰기 + Flush
→ input/output stream scope 종료
→ writer 완전 dispose
→ ValidateSvg / ValidatePng가 파일을 다시 open
→ candidate 검증
→ active 적용
```

핵심 규칙은 **다운로드 파일을 다시 읽어 검증하기 전에 exclusive writer를 반드시 dispose**하는 것입니다.

## 현재 복구 정책

### Map 탭 진입 self-heal

Map 탭에 진입할 때 active Map asset을 검증합니다.

```text
active Map asset 있음
→ 그대로 사용

active Map asset 없음/손상
→ 현재 active Game Content를 이용해 Map asset만 자동 갱신
→ 성공 후 즉시 지도 표시
```

Game Content를 다시 내려받을 필요 없이 Map presentation asset만 복구할 수 있습니다.

### 직접 재시도

빈 지도 화면에 `지도 자산 다시 받기` 버튼을 제공합니다.

전체 데이터 업데이트 화면으로 이동할 필요 없이 지도 탭에서 즉시 재시도합니다.

### SVG source fallback

현재 Tarkov.dev metadata가 지정하는 SVG와 공개 SVG repository를 서로 fallback source로 사용합니다.

```text
assets.tarkov.dev/maps/svg/<file>
↕ fallback
the-hideout/tarkov-dev-svg-maps raw <file>
```

한 source/domain이 일시적으로 실패해도 다른 공개 원천에서 동일 artwork를 받을 수 있습니다.

### 지도별 부분 복구

한 Map의 SVG 실패가 다른 정상 Map을 막지 않습니다.

- 새 SVG 성공 → 새 layout/SVG 사용
- 새 SVG 실패 + 이전 정상본 있음 → 그 Map만 이전 정상본 유지
- 새 SVG 실패 + 이전 정상본 없음 → 그 Map만 일시 제외
- 최소 한 Map이라도 정상 준비됨 → 정상 Map들은 활성화
- 모든 Map이 실패 → 기존 active가 있으면 보존, 없으면 명시적 오류/재시도 UI

모든 Map이 실패하면 이제 상위 generic 오류만 표시하지 않고 **일부 Map의 구체적인 실패 원인**도 같이 표시합니다.

### marker icon

Marker icon은 계속 non-authoritative presentation asset입니다.

- 새 icon 실패 + 이전 icon 있음 → 이전 icon 유지
- 둘 다 없음 → 기본 marker visual 사용
- icon 하나 때문에 Map 전체 update가 실패하지 않음
- PNG 검증도 exclusive writer dispose 이후 수행

## 회귀 테스트

PR #50에서 Windows 파일 공유 semantics를 직접 검증하는 테스트를 추가했습니다.

테스트는 다음 사실을 확인합니다.

1. `FileShare.None` writer가 살아있는 동안 동일 파일을 다시 열면 실패
2. writer dispose 후 동일 SVG를 `XDocument.Load`로 정상 재오픈 가능
3. production `DownloadSvgCoreAsync` / `DownloadPngAsync`가 validation 전에 명시적으로 writer scope를 끝냄

PR #50 CI:

```text
CI run: 31297134490
Windows Release Desktop build: success
full automated tests: success
file-lock regression: success
Windows x64 self-contained publish: success
ZIP creation/upload: success
review threads: none
```

## 현재 사용자 검증 대상

새 PR #50 빌드에서 다음을 확인합니다.

- 기존 v4 `content.db` + empty/failed `map-cache` 상태에서 Map 탭 진입
- Map SVG 자동 다운로드 완료
- 실제 지도 표시
- marker PNG 표시 또는 fallback
- 지도별 source fallback / 부분 복구 동작
- 이후 실제 SVG 좌표 정합성, 층 전환, Quest marker, screenshot 위치 추적, MiniMap 사용감
