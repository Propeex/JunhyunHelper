# MAP STARTUP RECOVERY — 지도 빌드 실행 무반응 복구

기록일: **2026-08-09**

상태: `IMPLEMENTED / CI PENDING`

## 문제

Map 테스트 빌드에서 사용자가 `JunhyunHelper.exe`를 실행해도 창이나 오류 메시지가 보이지 않는 현상을 확인했습니다.

직전 Map 구현에서는 `MainWindow.xaml`이 숨겨진 Map 탭까지 시작 시 즉시 생성했습니다. `MapPage.xaml`은 SharpVectors WPF SVG control을 포함하므로 Map presentation 초기화 실패가 앱 전체 startup을 막을 수 있는 결합이 있었습니다.

또한 CI test package가 모든 managed/native dependency를 single-file executable에 묶고 있었습니다. 기존 기능만 있을 때는 동작했지만 SharpVectors WPF dependency가 추가된 뒤에는 single-file packaging 자체도 startup 문제의 새로운 변수입니다.

정확한 사용자 PC 예외가 아직 확보되지 않았으므로 둘 중 하나를 근거 없이 단정하지 않습니다.

## 복구 원칙

### 1. Map lazy creation

- MainWindow startup에서는 `MapPage`를 생성하지 않음
- Map 탭 host만 생성
- 사용자가 Map 탭을 처음 열었을 때 `MapPage` 생성
- MapPage 생성/SharpVectors 초기화가 실패해도 Quest/Hideout/Items/Ammo는 계속 사용 가능
- 지도 host 안에 오류를 표시하고 diagnostic log 기록

### 2. Startup diagnostics

`App`이 MainWindow를 명시적으로 생성합니다.

startup 또는 처리되지 않은 dispatcher/AppDomain 예외는 다음 파일에 기록합니다.

```text
%LocalAppData%/JunhyunHelper/logs/startup.log
```

기록 항목:

- UTC timestamp
- failure context
- OS/runtime
- AppContext base directory
- executable process path
- full exception / stack trace

startup 실패는 조용히 종료하지 않고 MessageBox로 사용자에게 표시합니다.

### 3. Windows test packaging

Map 안정화 전 test build는 single-file이 아니라 **self-contained folder publish**를 ZIP으로 전달합니다.

```text
JunhyunHelper.exe
JunhyunHelper.dll
.NET runtime files
SharpVectors.*.dll
SkiaSharp/native dependencies
...
```

사용자는 ZIP 전체를 한 폴더에 압축 해제한 후 그 폴더의 `JunhyunHelper.exe`를 실행합니다. .NET 별도 설치는 필요하지 않습니다.

CI는 최소한 다음 SVG runtime assemblies가 실제 publish folder에 존재하는지도 검증합니다.

- `SharpVectors.Converters.Wpf.dll`
- `SharpVectors.Rendering.Wpf.dll`

## 검증 기준

이 복구 build는 다음을 모두 만족해야 전달합니다.

1. Release Desktop build success
2. full automated tests success
3. self-contained win-x64 folder publish success
4. required SharpVectors runtime files present
5. ZIP creation/upload success
6. 실제 artifact를 다운로드한 뒤 ZIP integrity 확인

실제 Windows에서 여전히 실행 실패가 발생하면 사용자에게 기술적 추측을 요구하지 않고 `startup.log` 또는 화면에 표시된 오류 메시지만 받아 다음 원인을 확정합니다.
