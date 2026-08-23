# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-23

상태: **`v1.3.0 RELEASE CANDIDATE — Scanner 분석 이미지 export / one-shot test / 3종 전역 단축키`**

## 현재 공개 기준선

현재 public stable은 아직 **v1.2.2**입니다. v1.3.0은 PR/Windows release gate를 통과한 뒤 public stable로 전환합니다.

```text
public stable: v1.2.2
release source: e3925cbc55215c7de0502c9b6b1ff1428d2f272b
asset: Junhyun-Helper-v1.2.2-win-x64.zip
SHA-256: 125d4a5b0e6db64f6772cc63c112f13cbcdac2fb7bc9ce501313ca2fc3645d7c
```

v1.3.0 release candidate identity:

```text
Desktop Version: 1.3.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner cache schema: v1/v2 readable, v2 written
v1.2.2 → v1.3.0 mandatory Game Content update: none
v1.2.2 → v1.3.0 user.db migration: none
```

## v1.3.0 Scanner 워크플로

- 최신 recognition 원본 frame을 `인식 이미지` 창에서 사용자 지정 PNG로 export 가능
- PNG는 실제 인식 원본이며 diagnostic overlay를 합성하지 않음
- 자동 screenshot 저장은 하지 않음
- `로그 삭제`는 사용자 export PNG를 삭제하지 않음
- one-shot TarkovWindow scan 유지
- one-shot DisplayTest scan 추가: 모든 연결 디스플레이를 한 번만 동일 인식 pipeline으로 검사
- Scanner 탭의 one-shot 버튼 제거
- `단축키 설정`에서 다음 세 global hotkey를 각각 변경/비활성화
  - 1회 인게임 스캔: `Ctrl+Shift+F10`
  - 1회 테스트 스캔: `Ctrl+Shift+F11`
  - Scanner ON/OFF: `Ctrl+Shift+F12`
- 세 단축키는 MainWindow lifetime에 등록되어 Scanner 탭 밖에서도 동작
- 동일 gesture 중복 지정 금지
- schema v3의 기존 one-shot 사용자 지정값을 인게임 one-shot으로 우선 보존
- 기존 사용자 키와 새 기본키가 충돌하면 신규 명령 쪽만 비충돌 fallback으로 이동

상세 계약: `docs/SCANNER_V1.3.0_WORKFLOW.md`.

## Scanner recognition 계약 — 변경 없음

```text
Tarkov / Display pixels
→ detail structural candidates
→ red close + magnifier + title-field anchors
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ conservative official-name matching
   OR conservative current-catalog Tarkov-font visual recovery
→ confidence + top1/top2 margin
→ Item ID
→ local presentation data
→ Mini Scanner
```

- false positive보다 miss 선호
- detector/OCR/visual confidence 및 top1/top2 margin 변경 없음
- current official Korean item catalog가 identity 권위
- scan-time network 없음
- game memory / DLL injection / packet interception 없음
- icon 하나만으로 Item identity 확정 금지
- 최고 상점가 = 유효한 non-flea RUB 판매가 최댓값
- 플리마켓 평균가 = positive `avg24hPrice`
- 현재 필요한 수량 = `NeededItems[itemId].RequiredTotal`

## 유지되는 v1.2.x hardening

- Scanner catalog disk cache load/network refresh 직렬화
- `resources.assets` title-font bounded streaming scan
- source/font generation-aware bounded visual caches
- Mini Scanner inventory/stash OCR single-active/coalesced/stale-result reject
- one-shot/profile/GameMode lifecycle serialization + latest-mode restore rule
- shutdown-safe font-aware OCR active-operation lifetime
- PrintWindow sparse validation duplicate full-frame allocation 제거
- title-anchor diagnostic evidence score 보존

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout | 구현 완료 |
| Needed Items / Inventory / Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / steady-state smoke 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.2.2 public package verified |
| Scanner + Mini Scanner | **v1.3.0 release candidate / Windows gate 진행 중** |

실제 Tarkov에서 발견되는 recognition 문제는 `scanner.log`와 `인식 이미지`/사용자 export PNG를 근거로 capture → candidate → anchors/ROI → OCR/visual matcher → catalog → presentation → inventory gate → overlay → performance 단계로 분리합니다. Live evidence 없이 confidence/margin을 임의로 낮추지 않습니다.
