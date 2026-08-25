# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-26

상태: **`v1.7.8 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable과 현재 source

현재 공개 stable/latest는 **v1.7.8**이다.

```text
public stable/latest: v1.7.8
exact release source/tag: 3ba9d99c43ad143dbc8329e7d29b1d01da335b06
main CI run: 32888653630 — SUCCESS
release workflow run: 32888935292 — SUCCESS
release id: 376650517
stable asset: Junhyun-Helper.zip
stable bytes: 80,469,671
stable SHA-256: 3716d2d3c6d3c9ce2f87c759aac74f6b56b483a09016339c0d8bb6d3bc67e730
380 passed / 0 failed / 0 skipped
Product UI / Scanner / Map / Factory / MiniMap / graceful shutdown smoke: SUCCESS
```

GitHub release readback:

- tag target = exact release source
- draft = false
- prerelease = false
- `releases/latest` = v1.7.8
- ZIP + checksum assets present

상세 공개 증거는 `docs/RELEASE_1.7.8.md`와 `docs/.release-v1.7.8-status.json`을 기준으로 한다.

## Schema / compatibility

```text
Desktop target version: 1.7.8
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
Scanner Ground Truth: explicit user-reviewed durable cases
```

사용자 mutable data는 `%LocalAppData%/JunhyunHelper`에 둔다. Program Update는 user.db, content/image cache, Map/Ammo/Scanner 설정, Scanner logs/diagnostics/Ground Truth를 덮어쓰지 않는다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / latest task-pool audit 기준 유지 |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / stable smoke 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / verified stable ZIP contract |
| Scanner + Mini Scanner | **실사용 검증 완료 / maintenance** |

## v1.7.8 핵심 변경

사용자가 제공한 reviewed Scanner Case 8건에서 레이드 인식 저하를 분석했다.

- 6건은 실제 실패, 2건은 프로그램 결과가 Ground Truth와 일치
- 실패 6건 모두 detail rectangle과 item-name ROI proposal은 정상
- 실제 화면에는 빨간 X와 돋보기가 존재
- runtime은 `HEADER_CLOSE_NOT_LOCKED` / `TITLE_ANCHOR_INCOMPLETE`로 OCR 이전에 중단
- 원인은 레이드 인벤토리 수평선이 inspect header와 이어지며 header-left ownership이 실제 상세창보다 47~132px 왼쪽으로 확장된 것

수정은 기존 정상 경로 뒤의 fail-closed recovery로 제한한다.

```text
existing primary header lock
→ existing live Ground Truth recovery
→ v1.7.8 raid ownership recovery
→ existing contained-subpanel recovery
→ fail closed
```

raid recovery는 강한 `RED_X_CANDIDATE >= 0.90`에서만 사용하며 red close-X, neutral header, magnifier, dark title field, title text evidence와 최종 `HEADER_FRAME_LOCKED >= 0.68`을 모두 다시 요구한다.

threshold/candidate cap/OCR/matcher/visual acceptance는 완화하지 않았다.

Scanner 메인 상단 primary actions는 사용자 결정에 따라 다음 순서다.

```text
스캐너 ON/OFF
설정
고급
현재 결과 교정
```

고급 창에는 테스트 스캐너, 교정 데이터 관리, Scanner 성능 진단 자료 내보내기만 둔다.

공식 결정: `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`

## v1.7.7 유지 계약

v1.7.8은 다음 v1.7.7 동작을 그대로 유지한다.

- 정상 Scanner monitoring의 durable automatic diagnostic Case 저장 금지
- latest exact frame은 메모리에만 유지하고 사용자 명시적 교정 저장만 Ground Truth로 영구 보존
- legacy `automatic_sample + unreviewed` Case만 metadata 재확인 후 background cleanup
- reviewed/manual/corrupt/unknown Case 자동 삭제 금지
- 동일 실패를 Scanner activity feed에서 30초 동안 collapse
- Scanner와 Map을 `primary key + optional Ctrl/Alt/Shift` 공통 hotkey 계약으로 통일
- 기존 Map bare-key 설정을 modifier `None`으로 migration
- bare NumPad0~5 직접 층 선택 유지, modifier+NumPad는 configurable Map action 허용

공식 결정: `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`

## Scanner 안전 기준선

```text
Tarkov window pixels
→ detail rectangle proposals
→ red close-X + magnifier + neutral header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user substitution
→ catalog sanitation / normalization
→ conservative official-catalog matching / bounded recovery
→ optional current-pixel visual recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

불변 계약:

- false positive보다 miss 선호
- geometry는 proposal이며 identity proof가 아님
- `HEADER_FRAME_LOCKED >= 0.68`
- magnifier + red close-X 필수
- structural floor `0.34`
- continuous max 8 / one-shot max 12 candidates
- continuous observation target 200 ms
- current official Korean Tarkov item catalog가 identity authority
- production OCR field는 item-name 하나
- price / slots / needed는 Item ID 이후 local mapped data
- stale Item ID 또는 cross-frame OCR/visual 결과를 새 identity proof로 사용하지 않음
- scan-time network 없음
- game memory read / DLL injection / packet interception / process hook 없음

## Scanner UI / hotkeys

일반 surface:

- Scanner ON/OFF
- 설정
- 고급
- 현재 결과 교정
- item search
- recognition log

기본 one-shot/global hotkey:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

사용자 설정은 bare key 또는 Ctrl/Alt/Shift의 선택적 조합을 허용한다. Windows key 조합은 지원하지 않는다.

## Ground Truth / correction

현재 runtime contract:

```text
current frame evidence
→ latest exact frame in memory
→ bounded text diagnostic log
→ user chooses correction
→ user saves
→ reviewed durable Ground Truth
```

상세창 없음, header/OCR/matcher failure, ambiguity 또는 반복 실패만으로 durable dataset이 증가하지 않는다.

Legacy cleanup은 `retention=automatic_sample` + `review_status=unreviewed`를 증명하고 5분 recent-write safety window와 pre-delete state recheck를 통과한 Case만 대상으로 한다.

Reviewed/manual/corrupt/unknown Case는 자동 삭제하지 않는다.

## Runtime stability / diagnostics

- v1.7.6 same-cycle exact visual evidence reuse 유지
- cross-frame identity cache 없음
- title continuity signature는 Item identity proof가 아님
- Scanner activity 동일 실패 30초 collapse
- Scanner/startup text logs bounded rotation/retention
- performance support ZIP에 Ground Truth image/dataset 미포함

## 현재 개발 상태

현재 요구사항 범위의 제품은 완성 상태이며 기본 모드는 유지보수다.

새 작업은 다음 경우에만 시작한다.

- 사용자가 새로운 제품 요구사항을 명시적으로 결정
- 실사용 defect/regression 확인
- Tarkov UI/data 변화로 기존 기능 파손
- Windows/.NET 또는 외부 데이터 소스 호환성 변화
- 보안/데이터 무결성 문제

Scanner 새 문제는 exact evidence → failure stage 확인 → affected layer만 수정 → reviewed Ground Truth regression 확인 → full Windows CI/publish/smoke/package gate 순서로 처리한다.
