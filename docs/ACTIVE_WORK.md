# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-08-31 KST**

현재 복구해서 이어갈 개발 작업은 없습니다.

최근 완료 작업:

```text
v1.13.3 Farming Guide — 인게임식 장비/수납 상호작용 회귀 수정
product release source: 9a0064d81dca4c2cffcb01c55742d46298d235de
public release: v1.13.3
release workflow: 33383407835 — SUCCESS
513 passed / 0 failed / 0 skipped
```

완료된 범위:

- current Secure Container 장착 판정 및 일반 case 오판 방지
- nested bag/rig storage parent-instance 모델
- orphan/cycle/duplicate/invalid placement fail-closed sanitization
- 별도 generic 장비 정보 Window 제거
- in-page storage / attachment / armor-plate workbench
- nested container subtree 이동/삭제 안전성
- upstream assembled weapon preset 검색 제외 / base weapon actual slot 사용
- 열린 workbench owner 이동 시 stale write-back 방지
- Windows Release build / self-contained publish
- actual published EXE Farming Guide / Product UI / Map / graceful shutdown smoke
- Shutdown Race / Documentation Consistency
- exact-main artifact/package/checksum 검증
- v1.13.3 public tag/release/assets/latest-stable 검증
- 공식 release/state 문서 갱신

새 작업은 현재 `main`의 공식 문서와 실제 코드를 기준으로 시작합니다.
