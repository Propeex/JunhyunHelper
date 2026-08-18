# RELEASE 0.1.14 — 사용자 동의형 프로그램 자동 업데이트

## 상태

`v0.1.14` release candidate 준비 문서입니다. 공개 릴리즈 검증이 끝나기 전에는 공개 완료로 표시하지 않습니다.

## 목적

준현 헬퍼 실행 시 최신 정식 GitHub Release를 확인하고, 새 버전이 있을 때 사용자 동의를 받은 뒤 안전하게 프로그램 파일을 교체하고 새 버전으로 자동 재시작합니다.

Scanner는 기존 `준비 중` placeholder 상태를 그대로 유지합니다.

## 사용자 동작

1. 준현 헬퍼 실행
2. 최신 정식 버전 확인
3. 새 버전이 없으면 기존처럼 바로 사용
4. 새 버전이 있으면 업데이트 동의창 표시
5. `예` 선택 시 다운로드/검증/교체
6. 준현 헬퍼 자동 재시작
7. `아니요` 선택 시 현재 버전을 그대로 사용하고 다음 실행 때 다시 확인

## 안전성

- network/GitHub 조회 실패는 프로그램 시작을 막지 않음
- stable public Release만 대상
- exact Windows ZIP + `SHA256SUMS.txt` 요구
- 다운로드 ZIP의 SHA-256 검증
- ZIP traversal/symlink/duplicate/unexpected root/PDB 거부
- 검증 전 기존 프로그램 파일 미변경
- 실행 중 EXE 교체를 위해 임시 self-copy updater mode 사용
- program-owned root files만 교체
- 교체 실패 시 previous 파일 rollback 시도
- 사용자 데이터는 `%LocalAppData%\JunhyunHelper`에 그대로 유지

상세 계약은 `docs/PROGRAM_UPDATE.md`를 따릅니다.

## 버전 / 데이터 호환성

```text
ProductVersion: 0.1.14
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v0.1.13 → v0.1.14 mandatory data update: none
```

## 검증 항목

release candidate CI에서 최소 다음을 통과해야 합니다.

- Release build
- 전체 자동 테스트
- program update parser/checksum/archive/replacement regression tests
- Windows x64 self-contained single-file publish
- publish root/dependency hygiene
- 실제 published EXE rendered UI assertions
- Main Map / Factory / MiniMap smoke
- graceful shutdown

공개 릴리즈 후에는 public ZIP과 `SHA256SUMS.txt`를 다시 다운로드하여 크기/hash/ProductVersion/package root를 검증하고 exact release baseline을 이 문서에 기록합니다.
