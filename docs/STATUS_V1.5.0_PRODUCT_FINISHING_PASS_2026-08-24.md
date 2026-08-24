# v1.5.0 Product Finishing Pass — Implementation Status

Date: 2026-08-24

Status: **IMPLEMENTATION COMPLETE / FINAL RELEASE GATE**

Authoritative product decision:

- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`

Working branch / PR:

- Branch: `product/v1.5.0-usability-data-hardening`
- PR: #172 — `Build v1.5.0 product finishing pass`
- GitHub HEAD and CI are authoritative if newer than this document.

## Scanner safety contracts preserved

v1.5.0 does not intentionally relax Scanner identity acceptance.

- false positive보다 miss 선호
- detail rectangle geometry는 proposal evidence이며 identity proof가 아님
- semantic header lock 필수
- `HEADER_FRAME_LOCKED >= 0.68`
- magnifier + red close-X 필수
- structural floor `0.34`
- continuous candidate cap `8`
- one-shot candidate cap `12`
- current official Korean Tarkov item catalog가 identity authority
- production OCR field는 item-name 하나
- price / flea / slots / needed는 Item ID 이후 mapped data
- scan-time network access 금지
- game memory read / DLL injection / packet interception 금지
- 자동 global r/0/한글 강제 substitution table 미사용

## Approved scope completion

### 1. Scanner mapped market data — COMPLETE

Item ID 확정 이후 아래 presentation path를 보완하고 자동 테스트로 고정했다.

- best trader sell price
- best trader name
- flea `avg24hPrice`
- slot count
- trader price per slot
- flea price per slot
- active Items workspace의 current required quantity

Market/dimension 일부가 없다고 Item identity 자체를 실패시키지 않는다.

### 2. Quest `확인 필요` live-data audit — COMPLETE

공식 감사 문서:

- `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`

최신 live data에서 Prapor/Mechanic/Ragman/Skier task-pool 구조 변화를 확인했고, `QuestTaskPoolVariableCompatibility`는 GameMode까지 포함한 감사된 구조에서만 synthetic interpretation을 허용한다. Exact profile variable은 항상 우선하며 구조가 다르면 fail closed한다.

임시 live audit의 마지막 보존 실행:

- run `32687388519` — **SUCCESS**

감사 완료 후 임시 `.github/workflows/live-audit-v1.5.0.yml`은 release candidate에서 제거했다.

### 3. Unified Game Data + Scanner catalog/market update — COMPLETE

상단 `데이터 업데이트` 한 번으로 일반 Tarkov content와 Scanner catalog/market data를 함께 갱신한다.

- 일반 데이터 성공 후 Scanner refresh만 실패하면 일반 데이터를 rollback하지 않음
- existing healthy Scanner cache 유지
- partial Scanner failure를 사용자 상태로 보고
- Scanner-only forced refresh는 `고급 / 진단`의 recovery action으로 유지

### 4. User OCR substitutions — COMPLETE

Persistent rule에 대해 add / delete / per-rule ON/OFF / reset을 지원한다.

Processing contract:

`raw OCR -> user substitution -> catalog sanitation/normalization -> matching`

치환은 single-pass/non-recursive이며 raw OCR은 forensic evidence로 별도 보존된다.

### 5. Candidate-based Ground Truth correction — COMPLETE

기본 교정 흐름은 detector evidence 선택이다.

- detail rectangle
- red close-X
- magnifier
- item-name ROI
- correct item/text

정답 후보가 없으면 manual rectangle fallback을 사용하고 `없음`도 Ground Truth로 기록할 수 있다. Candidate ID / rank / score / geometry를 Ground Truth와 함께 보존한다.

### 6. Scanner latency telemetry + accuracy-preserving optimization — COMPLETE

계측 단계:

- capture
- rectangle proposal
- semantic header validation
- normal OCR
- deep OCR
- visual recovery
- catalog matching/recovery
- presentation
- end-to-end

첫 최적화는 동일 active scan-cycle 안의 **완전히 동일한 OCR bitmap**만 SHA-256 + dimensions/format key로 재사용한다. Normal/deep cache는 분리하고 frame/cycle 사이에는 재사용하지 않는다. Threshold/candidate cap은 변경하지 않았다.

### 7. Continuous result stabilization — COMPLETE

검증된 item이 있는 동안 가장 가까운 detail candidate의 title identity signature가 유지되면 OCR을 다시 요구하지 않고 trusted snapshot을 유지한다.

v1.5의 title identity signature는 raw BGRA 완전일치 대신 밝은 title glyph shape를 사용해 dark-background/trailing-ROI 변동에 대한 안정성을 높인다.

- 새 Item identity를 만드는 증거가 아님
- visible glyph shape가 달라지면 signature 변경
- signature 계산 실패 시 기존 exact detector signature로 fail closed
- detector 단발 miss는 기존 consecutive-miss 안정화 경로로 처리
- 다른 title/identity evidence가 확인되면 기존 snapshot 즉시 폐기 후 재검증

Core tests가 background/trailing-width stability, glyph-change separation, no-ink fail-closed behavior를 검증한다.

### 8. Diagnostics/log/temp retention — COMPLETE

자동 삭제 금지:

- user-reviewed Ground Truth
- ownership/review state를 확정할 수 없는 unknown/corrupt case

자동 unreviewed diagnostic case 제한:

- 30 days
- 300 automatic cases
- 512 MiB
- 2-hour recent-case safety window

`scanner.log`와 `startup.log` 모두 bounded rotation을 사용한다.

### 9. Scanner primary/advanced UI + quick correction — COMPLETE

일반 Scanner surface:

- Scanner ON/OFF
- 1회 스캔
- 현재 결과 교정
- runtime status
- recent recognition history

`설정`:

- hotkeys
- OCR substitutions
- Mini Scanner display fields

`고급 / 진단`:

- test mode
- recognition image
- regression
- Ground Truth export/manage
- forced catalog refresh
- log clear

Mini Scanner context menu에서 `현재 결과 교정`으로 최신 Case를 바로 열 수 있다. 기능 삭제 없이 surface complexity만 낮췄다.

### 10. Whole-product UI consistency audit — COMPLETE

검토 범위:

- Main
- Quest
- Hideout
- Items
- Ammo
- Map host / Map smoke path
- Scanner
- Profile mode/editor
- Scanner correction / OCR substitution / hotkey / recognition image / diagnostic case dialogs

Main의 기존 `MinWidth=900`은 header + Items 2-pane 구조가 실제 요구하는 폭보다 작아 clipping 가능성이 있었으므로 지원 가능한 `1180`으로 맞췄다. Map은 기존 검증된 subsystem을 불필요하게 재설계하지 않고 전용 smoke 계약을 유지했다.

### 11. Full automated validation / Windows publish / product smoke — RELEASE CANDIDATE GATE

Pre-version-finalization HEAD `7cb9aea9e62b900ed2972196789e5127a405d21e`에서 CI run `32687388529`이 **SUCCESS**였다.

검증 내용:

- Desktop Release build
- Core tests
- Windows x64 self-contained single-file publish
- ProductVersion/package layout audit
- startup + rendered Product UI + Map + Scanner smoke
- graceful Main Window shutdown
- clean portable root
- release-candidate artifact upload

이후 release candidate에는 다음 housekeeping만 추가했다.

- Desktop version `1.5.0`
- `FIRST_RUN_KO.txt` v1.5.0 identity/guide
- `docs/RELEASE_NOTES_V1.5.0.md`
- temporary live-audit workflow 제거

따라서 **최종 release candidate HEAD 자체의 fresh CI success가 merge 전 마지막 필수 gate**다.

### 12. Public v1.5.0 release + independent public verification — NEXT

실제 현재 public stable/latest는 handoff의 v1.4.3이 아니라 **v1.4.4**다.

공식 repository record:

- `docs/.release-v1.4.4-status.json`
- source/tag SHA: `0c7f31e118122ffef6e5999f7a20a77d823a450d`
- asset: `Junhyun-Helper-v1.4.4-win-x64.zip`
- public redownload/hash/package/ProductVersion/smoke verification: complete

v1.5.0 release procedure:

1. final PR HEAD CI green 확인
2. PR #172 merge
3. exact main merge SHA의 normal CI green 확인
4. main에 temporary `release-v1.5.0.yml` 추가하되 release source SHA는 **workflow commit이 아니라 exact product merge SHA로 고정**
5. exact source build/test/publish/product smoke
6. `Junhyun-Helper-v1.5.0-win-x64.zip` + `SHA256SUMS.txt` draft release 생성
7. tag exact-source verification
8. draft asset redownload/hash/ProductVersion/layout/smoke verification
9. stable/latest publish
10. 인증 없이 public release URL에서 asset/SHA256SUMS 재다운로드 후 hash/layout/ProductVersion/smoke 재검증
11. `docs/.release-v1.5.0-status.json` 기록
12. temporary release workflow 제거 및 final status/housekeeping

## Current conclusion

**Approved implementation scope 1–10 is complete.**

남은 작업은 새 기능 개발이 아니라 final release gate와 public publication/independent verification이다. Final release candidate CI가 green이면 제품 PR을 merge하고 exact-source v1.5.0 release 절차로 진행한다.
