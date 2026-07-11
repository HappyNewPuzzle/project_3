# Idle Guild Unity Client Code Overview

이 문서는 Unity 클라이언트의 현재 코드 구조와 서버 API 호출 흐름을 설명합니다. 현재 클라이언트는 실제 전투 게임 화면 이전 단계의 런타임 UI 클라이언트이며, 장비·모의 상점·Trace ID를 포함한 서버 연동을 검증합니다.

## 실행 위치

- Unity 프로젝트: `UnityClient/My project`
- 메인 씬: `Assets/IdleGuild/Scenes/MainScene.unity`
- 진입 스크립트: `Assets/IdleGuild/Scripts/UnityClientBootstrap.cs`
- 기본 서버 주소: `http://localhost:5219`

## 파일별 책임

| 파일 | 책임 |
| --- | --- |
| `UnityClientBootstrap.cs` | 씬 진입점, 버튼 콜백, API 호출 순서, 로그, 데모 플로우 제어 |
| `IdleGuildApiClient.cs` | `UnityWebRequest` 기반 HTTP 요청, 인증 헤더, 멱등 키 헤더, 오류 파싱 |
| `IdleGuildApiModels.cs` | 서버 JSON 응답을 받는 DTO와 공통 API 결과 타입 |
| `IdleGuildSession.cs` | `PlayerPrefs`를 이용한 accessToken/playerId 저장과 초기화 |
| `IdleGuildRuntimeUi.cs` | Play 중 `Canvas`, 버튼, 텍스트, 입력 필드를 코드로 생성하고 상태를 표시 |

## 전체 흐름

1. `MainScene`이 열리고 `Unity Client Bootstrap` GameObject가 활성화됩니다.
2. `UnityClientBootstrap.Awake()`가 실행됩니다.
3. `IdleGuildSession.Load()`가 이전 토큰과 playerId를 불러옵니다.
4. `IdleGuildApiClient`가 생성되고 세션 토큰 조회 함수를 받습니다.
5. `IdleGuildRuntimeUi.Build()`가 런타임 UI를 생성하고 버튼 콜백을 연결합니다.
6. 사용자가 버튼을 누르면 Bootstrap의 코루틴이 실행됩니다.
7. Bootstrap은 ApiClient에 요청을 맡깁니다.
8. ApiClient는 서버 응답을 DTO로 파싱합니다.
9. Bootstrap은 결과를 상태와 로그에 반영합니다.
10. RuntimeUi는 `Refresh()`로 화면을 갱신합니다.

## 버튼별 동작

| 버튼 | 호출 흐름 | 서버 API |
| --- | --- | --- |
| `Check Server` | 서버 상태 확인 | `GET /api/v1/system/status` |
| `Run Demo Flow` | 서버 상태, 로그인, 상태 조회, 보상, 강화, 스테이지, 최종 상태를 순서대로 실행 | 여러 API 순차 호출 |
| `1. Guest Login` | 게스트 로그인 후 상태 조회 | `POST /api/v1/accounts/guest`, `GET /api/v1/game-state` |
| `2. State` | 현재 게임 상태 조회 | `GET /api/v1/game-state` |
| `3. Claim` | 방치 보상 수령 후 상태 재조회 | `POST /api/v1/rewards/idle/claim` |
| `4. Upgrade` | 영웅 강화 후 상태 재조회 | `POST /api/v1/heroes/main/upgrade` |
| `5. Challenge Stage` | 입력한 스테이지 도전 후 상태 재조회 | `POST /api/v1/stages/{stage}/challenge` |
| `6. Equip Bronze` | 장비 목록 조회 후 Bronze Sword 장착 | `GET /api/v1/equipment`, `PUT /api/v1/equipment/{id}/equipped` |
| `7. Buy 100 Gold (Mock)` | 카탈로그 조회 후 Small Gold Pack 구매 | `GET /api/v1/shop/products`, `POST /api/v1/shop/products/{id}/purchase` |
| `Clear Saved Session` | 저장된 토큰/playerId 삭제 | 서버 호출 없음 |

## 인증 처리

- 게스트 로그인 성공 시 서버가 `accessToken`과 `playerId`를 반환합니다.
- `IdleGuildSession.Save()`가 두 값을 `PlayerPrefs`에 저장합니다.
- 보호 API 호출 시 `IdleGuildApiClient.Send()`가 `Authorization: Bearer <accessToken>` 헤더를 붙입니다.
- 세션을 지우려면 UI의 `Clear Saved Session` 버튼을 사용합니다.

## 멱등 키 처리

상태 변경 API는 서버 문서 기준으로 `Idempotency-Key`가 필요합니다.

현재 클라이언트는 다음 API에 매번 새 키를 생성합니다.

- `POST /api/v1/rewards/idle/claim`
- `POST /api/v1/heroes/main/upgrade`
- `POST /api/v1/stages/{stage}/challenge`
- `PUT /api/v1/equipment/{equipmentId}/equipped`
- `POST /api/v1/shop/products/{productId}/purchase`

키 생성 위치는 `UnityClientBootstrap.CreateIdempotencyKey()`입니다.

## 오류 처리

- HTTP 실패 또는 네트워크 실패는 `IdleGuildApiResult.Failure()`로 변환됩니다.
- 서버가 `ProblemDetails` JSON을 내려주면 `title`을 로그에 표시합니다.
- JSON이 아닌 오류 응답이 오면 응답 원문을 로그에 표시합니다.
- 자동 데모 중 실패가 발생하면 `ShouldContinueDemo()`가 흐름을 중단합니다.
- 서버의 `X-Trace-Id`를 결과에 보존하고 HTTP 실패 로그에 표시합니다.

## 현재 한계

- 현재 화면은 API 검증용 런타임 UI입니다.
- 도트 캐릭터 이동, 전투 애니메이션, 맵, 몬스터 같은 실제 게임 플레이 화면은 아직 없습니다.
- 서버는 게임 결과를 판정하고, Unity는 그 결과를 시각적으로 연출하는 방향으로 확장하는 것이 좋습니다.

## 다음 확장 제안

1. `IdleGuildGameWorld`를 추가해 영웅과 몬스터 오브젝트를 배치합니다.
2. 임시 픽셀 스프라이트 또는 사각형 스프라이트로 영웅/몬스터를 만듭니다.
3. `Challenge Stage` 성공 시 영웅이 몬스터에게 이동하고 공격하는 연출을 붙입니다.
4. `Claim` 성공 시 골드 획득 이펙트를 표시합니다.
5. `Upgrade` 성공 시 영웅 레벨업 이펙트를 표시합니다.
6. 이후 실제 도트 애셋과 Animator Controller로 교체합니다.
