# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 상태

**v0.1.1 QUEST CORRECTNESS CANDIDATE — 2026-08-15**

현재 공개 버전은 v0.1.0이다. 2026-08-15 최신 Tarkov live 데이터 재감사에서 Quest availability 정확도 수정이 필요해 v0.1.1 패치를 준비한다.

### 이번 패치 핵심

- current taskRequirements `active / complete / failed` 모델 재검증 — 기존 evaluator 유효
- Lightkeeper / BTR Driver / Ref 후속 Quest에 누락된 상인 접근 Complete gate 보강
- `globalVariable` 162건 / `dialogue` 12건을 임의 해석하지 않고 `판정 문제`로 노출
- 각 mode 13개의 `availableDelaySecondsMin/Max`를 canonical metadata로 저장하되 가짜 UI 완료 시각 기반 타이머는 만들지 않음
- Content snapshot schema **v5**; v3/v4는 offline last-known-good로 읽기 유지
- 기존 사용자 `user.db` 변경 없음

상세: `docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md`

## 2026-08-15 live 검증

GitHub Actions run `31819603896`, job `94829428837`: SUCCESS

```text
Desktop Release build: SUCCESS
automated tests: 173 passed / 0 failed
regular:    517 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo / valid / warnings 0
pve:        513 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo / valid / warnings 0
pvp-season: 490 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo / valid / warnings 0
```

최종 test-only assertion commit 이후 PR 최종 head CI를 다시 통과시킨 뒤 main 병합 및 v0.1.1 공개 릴리즈한다.

## 사용자 업그레이드 정책

v0.1.1 설치 후 기존 사용자는 **`데이터 업데이트`를 한 번 실행**한다. 그러면 현재 online source로 v5 content DB를 재구축하고 새 Quest availability semantics를 적용한다. User Progress / inventory / 완료 기록은 `%LocalAppData%/JunhyunHelper/user.db`에 별도로 유지된다.

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / v0.1.1 최신 prerequisite audit 반영 |
| Hideout | 구현 완료 / current live validation 통과 |
| Needed Items / Inventory | 구현 완료 |
| Ammo | 구현 완료 / current live validation 통과 |
| Map + MiniMap | 구현 완료 / Windows 실사용 검증 완료 |
| Scanner | `준비 중` placeholder 탭 유지 / 실제 기능 PRODUCT OPEN |

## 핵심 데이터 원칙

```text
online source
→ download
→ external shape/semantic validation
→ canonical transform
→ candidate DB
→ relationship/read-back validation
→ active swap
→ User Progress와 결합
```

실패 candidate가 last-known-good active content를 덮지 않으며 Game Content update가 `user.db`를 삭제/덮어쓰지 않는다. Runtime GPT/AI 의존성은 없다.

## Map 기준

Map subsystem은 독립이고 Quest만 JunhyunHelper current profile/content와 연결한다. pinned submodule revision은 `d933792b6042a51cea38dc44b686a096fe30de67`. 상세는 `docs/MAP_PRODUCT_REQUIREMENTS.md`.

## 현재 공개 릴리즈

```text
v0.1.0
https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.0
public asset: Junhyun-Helper-v0.1.0-win-x64.zip
SHA-256: f3c1a4208fc70b7ec7fb6612933de9d383e4c54e84a7352f529dc7de21550f91
```

## 다음 작업

1. v0.1.1 candidate final CI
2. main 병합
3. Windows x64 single-file package 생성 / Map+MiniMap smoke
4. public GitHub v0.1.1 Release
5. 공개 ZIP/SHA256 재다운로드 검증

비차단 후속 범위: Scanner 실제 기능, Map bundle updater, code signing/installer/app updater, user.db backup UX, license/third-party notice 정책.
