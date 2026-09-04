# 준현 헬퍼 v1.17.3

## 변경 사항

- 새 사용자 기능 없이 현재 제품의 안정성, 종료 안전성, 반복 조회 효율과 UI 일관성을 강화했습니다.
- Quest/Hideout/Items 페이지는 현재 콘텐츠의 item/quest/station lookup을 재사용하고, 세 workspace를 하나의 authoritative profile snapshot에서 파생해 불필요한 store read와 cross-page skew 가능성을 줄였습니다.
- Scanner는 catalog snapshot, canonical item/quest/trader/station index와 item→Quest/Hideout requirement 역인덱스를 재사용해 검색·상세 관계 조회의 반복 전체 순회를 제거했습니다.
- 공용 이미지 캐시는 동일 cache path의 동시 다운로드를 single-flight로 직렬화하고 decoded image는 weak cache로 재사용해 중복 I/O·decode와 장기 메모리 점유 위험을 함께 낮췄습니다.
- Map Quest marker scale 보정은 120ms polling timer 대신 실제 `ScaleTransform.Changed` 이벤트에 반응하며 현재 Map/MiniMap donor 동작을 유지합니다.
- 수동 데이터 업데이트, opportunistic schema refresh, Map-triggered refresh, 최초 데이터 생성/복구가 하나의 content-operation gate를 공유해 중복 네트워크 갱신과 busy-state 경쟁을 방지합니다.
- MainWindow lifetime cancellation을 데이터 업데이트, 프로필 I/O, Quest/Hideout/Items mutation, Scanner catalog sync, PC 진단과 updater 준비에 연결해 창 종료 후 비동기 작업과 queued progress UI가 남지 않도록 보강했습니다.
- Hideout의 빠른 레벨 변경/시설 전환과 rollback 취소에서 낙관적 UI 값이 authoritative 저장 상태와 어긋날 수 있는 경계를 수정했습니다.
- mutation 실패 시 authoritative profile-derived presentation을 재구축하도록 보강해 저장 실패 후 UI만 성공한 것처럼 남지 않게 했습니다.
- 공유 버튼 스타일에 키보드 focus 표시를 추가하고 주요 Quest/Hideout/Items/Ammo/Scanner layout, clipping, scrolling, virtualization 계약을 재검토했습니다.
- Scanner OCR recognition thresholds, pacing, matcher safety, current Map donor revision, supported schema/read compatibility와 사용자 데이터 의미는 변경하지 않았습니다.

## 검증 목표

- deterministic tests 503개 전체 통과
- Windows Release build
- win-x64 self-contained publish
- actual published EXE Product UI / Map / Scanner smoke
- graceful shutdown / active-async Shutdown Race
- stable package/checksum validation
- Documentation Consistency
- PR / exact-main / public release identity verification
