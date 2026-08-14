# 준현 헬퍼 v0.1.2

Release date: **2026-08-15**

Status: **RELEASED / PUBLIC GITHUB RELEASE VERIFIED**

## 공개 다운로드

https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.2

```text
Junhyun-Helper-v0.1.2-win-x64.zip
SHA-256: 163a2a33184a6f5d8abcefa542239cd2f29a686d924cf4d784081c47939398ab
size: 74,030,297 bytes
```

Release는 draft/prerelease가 아닌 정식 공개 상태이며 Windows ZIP과 `SHA256SUMS.txt` 두 asset만 게시합니다. 공개 ZIP을 다시 다운로드해 위 SHA-256과 일치함을 검증했습니다.

## 주요 변경

- floor up/down hotkey에서 Main Map zoom + viewport center 보존
- NumPad 0~5 direct floor도 viewport-safe Main Map render 사용
- 타층 marker 약 50% opacity + 위 `↑` / 아래 `↓`
- Main Map/MiniMap의 Quest·일반 marker·extract·Raider floor 의미 통일
- flexible hand-in 상태 `필요 / 전체 / 충분`
- flexible objective를 모두 충족하면 기본 `필요` 목록에서 자동 제외
- Item 상세 Wiki 버튼
- floor badge reuse / MiniMap off-floor extract signature cache

## 검증

```text
release baseline: b974d942dbddf09ebe91c6c2af337b66ae1e1ba0
main verification run: 31829061453
release workflow run: 31829344223
automated tests: 176 passed / 0 failed
ProductVersion: 0.1.2
Windows x64 single-file publish: SUCCESS
Main Map floor/viewport regression smoke: SUCCESS
MiniMap regression smoke: SUCCESS
graceful shutdown: SUCCESS
public ZIP re-download checksum: VERIFIED
```

## 데이터 업데이트

- v0.1.1 사용자는 이번 패치 때문에 데이터를 다시 받을 필요가 없습니다.
- v0.1.0에서 바로 업그레이드하면 v0.1.1의 최신 Quest prerequisite semantics를 받기 위해 `데이터 업데이트`를 한 번 실행합니다.
- 사용자 `user.db`는 유지됩니다.
