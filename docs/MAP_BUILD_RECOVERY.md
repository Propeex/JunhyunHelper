# MAP BUILD RECOVERY

2026-08-09: PR #44의 마지막 bulk-marker 성능 보강이 병합된 뒤 exact-head CI에서 Desktop compile failure가 확인되었다.

원인:
- `MapPage.BulkPerformance.cs`가 spatial floor helper overload를 호출했지만 해당 overload가 최종 병합본에 포함되지 않았다.
- bulk marker preference 저장 호출이 parameterless static no-op overload로 해석될 수 있었다.

복구 원칙:
- spatial floor helper를 MapPage partial에 명시적으로 구현한다.
- bulk preference 저장은 단일 instance async method로 통일한다.
- Release Desktop build, full tests, Windows x64 publish 및 artifact upload가 모두 성공한 뒤 사용자 테스트 빌드를 배포한다.
