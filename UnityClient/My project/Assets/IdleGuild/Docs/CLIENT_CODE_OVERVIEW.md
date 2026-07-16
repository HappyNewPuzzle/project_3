# Idle Guild Unity 클라이언트 학습 가이드

이 문서는 Unity를 처음 사용하는 개발자가 Idle Guild 클라이언트를 직접 실행하고, 코드를 읽고, 이후 기능을 함께 확장할 수 있도록 만든 누적 학습 문서입니다.

앞으로 기능 Step이 완료될 때마다 이 문서에 다음 내용을 추가합니다.

- 이번 Step에서 만든 결과
- 새로 배울 Unity/C# 개념
- 변경된 파일과 각 파일의 책임
- Unity Editor에서 직접 확인하는 방법
- 실습 과제와 정상 동작 기준
- 발생할 수 있는 오류와 해결 방법

## 1. 현재 프로젝트 한눈에 보기

| 항목 | 현재 값 |
| --- | --- |
| Unity 버전 | Unity 6 `6000.3.19f1` |
| 프로젝트 위치 | `UnityClient/My project` |
| 메인 씬 | `Assets/IdleGuild/Scenes/MainScene.unity` |
| 시작 스크립트 | `Assets/IdleGuild/Scripts/UnityClientBootstrap.cs` |
| 기본 실행 모드 | 서버가 필요 없는 Mock API 모드 |
| 실제 서버 주소 | `http://localhost:5219` |
| 입력 시스템 | Unity Input System |
| UI 시스템 | uGUI (`Canvas`, `Button`, `Text`, `InputField`) |

현재 구현된 기능은 다음과 같습니다.

1. 게스트 로그인
2. 게임 상태 조회
3. 방치 보상 수령
4. 메인 영웅 강화
5. 스테이지 도전
6. 청동 검 장착
7. 테스트용 골드 상품 구매
8. 임시 도트 영웅과 슬라임의 전투 연출
9. 서버 없이 전체 흐름을 시험하는 Mock API

현재 화면은 완성된 게임 UI가 아니라 서버 기능과 게임 흐름을 검증하는 학습용 프로토타입입니다. 스테이지 도전 결과에 따라 실제 PNG Sprite Sheet의 도트 캐릭터가 이동하고 공격합니다. 프레임 재생은 학습을 위해 Coroutine으로 구현했으며, Animator 기반 상태 전환은 다음 단계에서 발전시킵니다.

## 2. Unity Editor에서 알아야 할 화면

Unity 프로젝트를 열면 우선 아래 다섯 영역을 구분하면 됩니다.

| 창 | 역할 | 이 프로젝트에서 하는 일 |
| --- | --- | --- |
| Hierarchy | 현재 씬에 들어 있는 GameObject 목록 | `Main Camera`, `Unity Client Bootstrap` 확인 |
| Scene | 게임 공간을 편집하는 화면 | 카메라와 오브젝트 위치 확인 |
| Game | 플레이어가 실제로 보게 될 화면 | Play 후 UI와 전투 연출 확인 |
| Inspector | 선택한 오브젝트의 컴포넌트와 설정 | Mock API 사용 여부와 서버 주소 변경 |
| Project | 프로젝트의 폴더와 파일 | `Assets/IdleGuild` 아래 씬과 스크립트 열기 |
| Console | 로그와 오류 표시 | API 결과와 예외 확인 |

Unity에서 가장 중요한 기본 구조는 다음과 같습니다.

- **Scene**은 하나의 게임 화면 또는 공간입니다. 현재는 `MainScene` 하나를 사용합니다.
- **GameObject**는 씬에 존재하는 기본 단위입니다. 이름만 있는 빈 오브젝트에도 여러 기능을 붙일 수 있습니다.
- **Component**는 GameObject에 붙는 기능입니다. `Camera`, `Transform`, `MonoBehaviour` 스크립트가 모두 컴포넌트입니다.
- **Transform**은 위치, 회전, 크기와 부모-자식 관계를 보관하며 모든 GameObject가 가집니다.
- **Inspector**의 `[SerializeField]` 항목은 코드를 수정하지 않고 값을 바꾸는 통로입니다.

## 3. 서버 없이 처음 실행하기

Mock API가 기본값이므로 서버 프로그램을 켜지 않아도 됩니다.

1. Unity Hub에서 `UnityClient/My project`를 엽니다.
2. Project 창에서 `Assets/IdleGuild/Scenes/MainScene`을 더블 클릭합니다.
3. Hierarchy에서 `Unity Client Bootstrap`을 선택합니다.
4. Inspector에서 `Use Mock Api`가 체크되어 있는지 확인합니다.
5. Editor 위쪽의 Play 삼각형 버튼을 누릅니다.
6. Game 탭으로 이동합니다.
7. `Run Demo Flow`를 누르거나 번호가 붙은 버튼을 순서대로 누릅니다.

수동 실습 권장 순서는 다음과 같습니다.

1. `Check Server`: Mock 시스템 상태가 `mock-ok`인지 확인합니다.
2. `1. Guest Login`: 임시 플레이어 ID와 토큰을 만듭니다.
3. `2. State`: 골드 20, 영웅 레벨 1, 최고 스테이지 1을 확인합니다.
4. `3. Claim`: 골드가 100 이상 증가하는지 확인합니다.
5. `4. Upgrade`: 골드를 사용해 영웅 레벨과 전투력이 증가하는지 확인합니다.
6. `6. Equip Bronze`: 장비 전투력 보너스가 5 증가하는지 확인합니다.
7. Stage 입력에 `2`를 넣고 `5. Challenge Stage`를 누릅니다.
8. 영웅이 슬라임 앞으로 이동해 공격하고 결과에 따라 승리 또는 후퇴하는지 확인합니다.

Mock 데이터는 Play를 시작할 때 메모리에 새로 만들어집니다. Play를 중지하면 골드, 레벨, 장비 상태는 초기화됩니다. 로그인 정보는 `PlayerPrefs`에 저장되므로 `Clear Saved Session`으로 지울 수 있습니다.

## 4. 실제 서버에 연결하기

실제 서버 API를 시험할 때만 다음과 같이 전환합니다.

1. 서버를 `http://localhost:5219`에서 실행합니다.
2. Hierarchy에서 `Unity Client Bootstrap`을 선택합니다.
3. Inspector에서 `Use Mock Api` 체크를 해제합니다.
4. 서버 포트가 다르면 `Api Base Url`을 실제 주소로 바꿉니다.
5. Play 후 `Check Server`를 먼저 눌러 연결을 확인합니다.
6. `Guest Login`부터 기능을 순서대로 실행합니다.

`Use Mock Api` 값은 `UnityClientBootstrap`의 `[SerializeField] private bool useMockApi`와 연결됩니다. 체크 상태에 따라 같은 기능 인터페이스를 구현한 두 객체 중 하나를 선택합니다.

```text
UnityClientBootstrap
        |
        v
IIdleGuildApiClient
   |             |
   v             v
Mock API      실제 HTTP API
(메모리)      (UnityWebRequest)
```

이 구조 덕분에 버튼과 게임 흐름 코드는 그대로 두고 데이터 공급자만 교체할 수 있습니다.

## 5. 코드 구조와 파일별 책임

| 파일 | 책임 | 먼저 읽을 부분 |
| --- | --- | --- |
| `UnityClientBootstrap.cs` | 씬 진입, 버튼 동작, 기능 실행 순서, 상태와 로그 관리 | `Awake()`, `RunDemoFlow()` |
| `IdleGuildApiClient.cs` | 실제 서버 HTTP 요청, 인증/멱등 헤더, JSON 변환, 오류 처리 | `IIdleGuildApiClient`, `Send()` |
| `IdleGuildMockApiClient.cs` | 서버 없이 동작하는 메모리 기반 가짜 서버 | `CreateGameState()` |
| `IdleGuildApiModels.cs` | 서버 JSON과 C# 객체 사이의 데이터 모양(DTO) 정의 | `GameStateResponse` |
| `IdleGuildSession.cs` | 토큰과 플레이어 ID를 `PlayerPrefs`에 저장/복원 | `Load()`, `Save()`, `Clear()` |
| `IdleGuildRuntimeUi.cs` | 실행 중 Canvas, 텍스트, 버튼, 입력 필드를 코드로 생성 | `Build()`, `Refresh()` |
| `IdleGuildGameWorld.cs` | 배경, 캐릭터, Animator 상태와 전투 이동 순서 제어 | `Build()`, `PlayCombat()` |
| `IdleGuildBattlePresentation.cs` | 서버 승패를 HUD용 체력/데미지 연출 데이터로 변환 | `Create()` |
| `IdleGuildWorldHealthBar.cs` | 캐릭터를 따라다니는 Sprite 기반 체력 바 | `SetHealth()` |
| `Editor/IdleGuildAnimationAssetBuilder.cs` | Sprite 분할, Clip과 Controller 생성 자동화 | `RebuildAnimationAssets()` |
| `Resources/Sprites/*.png` | 영웅과 슬라임의 4x4 애니메이션 Sprite Sheet | 각 행의 Idle/Run/Attack/Hit 프레임 |
| `Resources/Animations` | 영웅/슬라임 Animation Clip과 Animator Controller | `HeroAnimator`, `SlimeAnimator` |

### 전체 실행 흐름

```text
MainScene 로드
  -> UnityClientBootstrap.Awake()
  -> 저장된 Session 불러오기
  -> Mock 또는 실제 ApiClient 선택
  -> Runtime UI 생성
  -> 도트 Game World 생성
  -> 버튼 클릭
  -> Bootstrap의 Coroutine 실행
  -> API 결과를 DTO로 받기
  -> 상태와 로그 갱신
  -> 필요하면 전투 연출 실행
```

## 6. 이번 프로젝트에서 배우는 C#과 Unity 개념

### MonoBehaviour와 Awake

`UnityClientBootstrap`은 `MonoBehaviour`를 상속합니다. 그래서 씬의 GameObject에 컴포넌트로 붙을 수 있고 Unity 생명주기 함수를 사용할 수 있습니다.

`Awake()`는 오브젝트가 준비될 때 Unity가 자동 호출합니다. 현재 프로젝트에서는 세션, API, UI, 게임 월드를 초기화합니다. 일반 C# 프로그램의 `Main()`과 비슷한 진입 역할이지만 Unity가 호출 시점을 관리한다는 차이가 있습니다.

### Coroutine과 IEnumerator

서버 통신과 전투 애니메이션은 즉시 끝나지 않습니다. Unity의 Coroutine은 여러 프레임에 걸쳐 작업을 이어 가게 해 줍니다.

```csharp
yield return MoveTo(hero, attackPoint, 0.55f);
yield return new WaitForSeconds(0.08f);
```

첫 줄은 영웅 이동이 끝날 때까지 기다리고, 두 번째 줄은 0.08초 기다린 뒤 다음 코드를 실행합니다. 이 동안 게임 화면 전체가 멈추지는 않습니다.

### Interface와 구현 교체

`IIdleGuildApiClient`는 API 클라이언트가 반드시 제공해야 하는 기능 목록입니다. 실제 서버용 `IdleGuildApiClient`와 오프라인용 `IdleGuildMockApiClient`가 같은 인터페이스를 구현합니다.

이 패턴은 테스트할 때 매우 유용합니다. 게임 로직은 “어느 서버인지” 알 필요 없이 `api.GetGameState()`처럼 같은 방식으로 호출할 수 있습니다.

### DTO와 JSON

DTO(Data Transfer Object)는 서버와 주고받는 데이터 전용 클래스입니다. 예를 들어 서버의 게임 상태 JSON은 `GameStateResponse`로 변환됩니다.

```text
서버 JSON -> GameStateResponse -> Bootstrap 상태 -> UI Text
```

DTO에는 게임 행동을 넣지 않고 데이터 필드만 두는 것이 현재 구조의 원칙입니다.

### PlayerPrefs

`PlayerPrefs`는 작은 값을 로컬에 저장하는 Unity 기능입니다. 이 프로젝트에서는 게스트 토큰과 플레이어 ID만 저장합니다. 대규모 게임 상태나 중요한 서버 데이터를 저장하는 데이터베이스는 아닙니다.

### Runtime UI

현재 최상위 Canvas 설정은 `RuntimeCanvas` Prefab을 우선 사용하고, 버튼과 상태 텍스트는 `IdleGuildRuntimeUi.Build()`가 Play 중 생성합니다. Prefab이 없으면 Canvas까지 코드로 생성하는 fallback이 동작합니다. 이후에는 Panel과 개별 화면도 Prefab으로 옮길 예정입니다.

### Sprite와 픽셀 텍스처

`IdleGuildGameWorld`는 `Resources/Sprites`의 1024x1024 PNG를 불러옵니다. 각 이미지는 4열x4행이고, Unity Import 설정에 각 셀이 하나의 Sprite로 저장되어 총 16개 하위 Sprite가 만들어집니다.

| 행 | 애니메이션 | 프레임 |
| --- | --- | --- |
| 1 | Idle | 4 |
| 2 | Run | 4 |
| 3 | Attack | 4 |
| 4 | Hit | 4 |

Texture 좌표는 왼쪽 아래가 원점이지만, 사람이 보는 이미지의 첫 번째 행은 위쪽입니다. 따라서 Editor 빌더는 Y 좌표를 뒤집어 위쪽 Idle 행부터 Sprite로 분할합니다.

`Resources.Load<Texture2D>("Sprites/hero-spritesheet")`처럼 확장자를 제외한 경로로 이미지를 읽습니다. `Resources`는 작은 학습 프로젝트에서 편리하지만 모든 포함 애셋을 빌드에 넣기 때문에, 프로젝트가 커지면 Inspector 참조나 Addressables로 교체하는 것이 좋습니다.

`FilterMode.Point`는 확대해도 픽셀이 흐려지지 않게 합니다. Animator 애셋이 없으면 Coroutine 프레임 재생으로, PNG까지 없으면 기존 16x16 코드 생성 Sprite로 fallback하므로 게임 흐름 자체는 계속 시험할 수 있습니다.

### Animation Clip과 Animator Controller

Animation Clip은 시간에 따라 어떤 Sprite를 표시할지 기록한 애셋입니다. 현재 캐릭터마다 `Idle`, `Run`, `Attack`, `Hit` Clip 네 개가 있습니다.

Animator Controller는 여러 Clip을 상태로 배치하고 상태 사이의 전환 규칙을 관리합니다. 영웅과 슬라임 Controller는 다음 구조입니다.

```text
State = 0 -> Idle
State = 1 -> Run
State = 2 -> Attack
State = 3 -> Hit
```

`IdleGuildGameWorld`의 `ActorAnimationState` enum 값과 Controller의 int `State` 값이 같습니다. 코드는 `animator.SetInteger("State", 값)`을 호출하고, Controller의 Any State 전환이 해당 상태로 이동합니다.

Coroutine은 여전히 전투의 시간 순서와 캐릭터 위치 이동을 담당합니다. Sprite 프레임 교체는 Animator가 담당합니다. 즉, Coroutine과 Animator는 서로 대체 관계가 아니라 각자 다른 책임을 가집니다.

```text
Coroutine: 달려가기 -> 공격 기다리기 -> 결과 연출 -> 돌아오기
Animator:  Idle / Run / Attack / Hit Sprite 프레임 재생
```

`IdleGuildAnimationAssetBuilder`는 반복 작업을 자동화하는 Editor 전용 코드입니다. `Assets/IdleGuild/Editor` 폴더의 코드는 게임 빌드에 포함되지 않고 Unity Editor에서만 실행됩니다. Unity 상단 메뉴의 `Idle Guild > Rebuild Character Animation Assets`로 애셋을 다시 만들 수 있습니다.

### 월드 공간 HUD와 전투 연출 데이터

체력 바는 Canvas UI가 아니라 캐릭터 GameObject 아래에 붙는 `SpriteRenderer` 오브젝트입니다. 부모가 움직이면 자식 Transform도 함께 움직이므로 영웅이 달려갈 때 체력 바도 자동으로 따라갑니다.

현재 HP 비율은 다음처럼 계산합니다.

```text
HP 비율 = 현재 HP / 최대 HP
Fill 폭 = 전체 체력 바 폭 x HP 비율
```

Sprite의 Pivot이 중앙이므로 폭만 줄이면 양쪽에서 동시에 작아집니다. `IdleGuildWorldHealthBar.SetHealth()`는 Fill 중심도 함께 왼쪽으로 이동시켜 체력이 오른쪽에서 왼쪽 방향으로 줄어드는 것처럼 보이게 합니다.

현재 서버의 스테이지 API는 `succeeded` 또는 `failed` 같은 결과를 반환하지만 상세 HP와 공격 로그는 반환하지 않습니다. 따라서 다음 두 데이터를 구분해야 합니다.

| 데이터 | 출처 | 용도 |
| --- | --- | --- |
| 스테이지 승패 | 서버 API | 실제 진행 상태와 최고 스테이지 결정 |
| HP와 데미지 숫자 | `IdleGuildBattlePresentation` | 현재 프로토타입의 화면 연출 |

클라이언트가 보여주는 데미지 숫자로 서버의 골드, 스테이지, 영웅 레벨을 변경하면 안 됩니다. 향후 서버가 실제 전투 로그를 제공하면 Presentation 모델의 임시 계산을 서버 값 매핑으로 교체할 수 있습니다.

데미지 팝업은 `TextMesh`를 월드 위치에 만들고 Coroutine으로 위쪽 이동과 alpha 감소를 수행합니다. 타격 이펙트는 1x1 Sprite 여덟 개를 원형 방향으로 이동시킨 후 제거합니다. 둘 다 전투가 끝난 뒤 남지 않는 일회성 오브젝트입니다.

## 7. 서버 API 기능 흐름

| 버튼 | 클라이언트 호출 | 실제 서버 API |
| --- | --- | --- |
| `Check Server` | `GetSystemStatus` | `GET /api/v1/system/status` |
| `1. Guest Login` | `GuestLogin` | `POST /api/v1/accounts/guest` |
| `2. State` | `GetGameState` | `GET /api/v1/game-state` |
| `3. Claim` | `ClaimIdleReward` | `POST /api/v1/rewards/idle/claim` |
| `4. Upgrade` | `UpgradeMainHero` | `POST /api/v1/heroes/main/upgrade` |
| `5. Challenge Stage` | `ChallengeStage` | `POST /api/v1/stages/{stage}/challenge` |
| `6. Equip Bronze` | 목록 조회 후 장착 | `GET /equipment`, `PUT /equipment/{id}/equipped` |
| `7. Buy 100 Gold (Mock)` | 상품 조회 후 구매 | `GET /shop/products`, `POST /shop/products/{id}/purchase` |

상태 변경 API에는 `Idempotency-Key`를 보냅니다. 네트워크 문제로 같은 요청이 재전송되어도 보상이나 구매가 두 번 처리되지 않도록 서버가 요청을 구분하는 값입니다.

보호 API에는 로그인 후 받은 토큰을 다음 HTTP 헤더에 넣습니다.

```text
Authorization: Bearer <accessToken>
```

## 8. 자주 만나는 오류와 해결

### `Arial.ttf is no longer a valid built in font`

Unity 6에서는 예전 기본 폰트 이름인 `Arial.ttf`를 사용할 수 없습니다. 현재 UI 코드는 `LegacyRuntime.ttf`를 사용해야 합니다.

### `trying to read Input using UnityEngine.Input`

프로젝트가 새 Input System을 사용 중인데 구형 `StandaloneInputModule`을 사용하면 발생합니다. 현재 코드는 EventSystem에 `InputSystemUIInputModule`을 붙입니다.

### 버튼이 비활성화됨

`State`, `Claim`, `Upgrade`, `Challenge` 같은 보호 기능은 게스트 로그인 토큰이 있어야 활성화됩니다. 먼저 `Guest Login`을 누릅니다. 요청 중에는 중복 실행을 막기 위해 버튼이 잠시 비활성화됩니다.

### 실제 서버 연결 실패

`Use Mock Api`가 꺼져 있는지, 서버가 실행 중인지, `Api Base Url` 포트가 서버 주소와 같은지 확인합니다. Console과 화면 로그에 표시되는 HTTP 상태 및 Trace ID도 함께 확인합니다.

### `Animator Controller was not found in Resources`

Animation 애셋이 삭제되었거나 아직 생성되지 않았을 때 나오는 경고입니다. Play를 중지한 Edit Mode에서 Unity 상단 메뉴의 `Idle Guild > Rebuild Character Animation Assets`를 실행합니다. Controller가 없어도 Coroutine fallback으로 기본 애니메이션은 계속 재생됩니다.

## 9. Step별 학습 기록

### Step 1: Unity 클라이언트 기반 구조와 서버 연결

- `MainScene`과 `UnityClientBootstrap`을 클라이언트 진입점으로 구성했습니다.
- `UnityWebRequest`로 서버 API를 호출하는 클라이언트를 만들었습니다.
- 게스트 로그인부터 스테이지 도전까지 MVP 흐름을 연결했습니다.
- 학습 개념: Scene, GameObject, Component, MonoBehaviour, Coroutine, HTTP/JSON.

### Step 2: 세션과 상태 변경 요청 안정화

- 로그인 토큰과 플레이어 ID를 `PlayerPrefs`에 저장합니다.
- 인증 헤더, 멱등 키, 오류 응답, Trace ID를 처리합니다.
- 장비와 테스트 상점 기능을 추가했습니다.
- 학습 개념: 클라이언트 세션, 서버 권위 상태, 멱등성, 오류 처리.

### Step 3: 런타임 UI와 Unity 6 호환성

- 실행 중 Canvas와 조작 버튼을 생성합니다.
- Unity 6 기본 폰트를 `LegacyRuntime.ttf`로 사용합니다.
- 새 Input System용 `InputSystemUIInputModule`을 사용합니다.
- 학습 개념: Canvas, uGUI, EventSystem, Input System, Layout Group.

### Step 4: 기본 2D 전투 미리보기

- 코드로 임시 픽셀 영웅과 슬라임을 생성합니다.
- 스테이지 도전 시 이동, 공격, 승리/실패 연출을 재생합니다.
- 학습 개념: SpriteRenderer, Texture2D, 좌표, 프레임, Coroutine 애니메이션.

### Step 5: 서버 없는 Mock API 모드

- `IIdleGuildApiClient` 인터페이스로 API 계약을 분리했습니다.
- 메모리에서 골드, 레벨, 스테이지, 장비를 계산하는 Mock API를 추가했습니다.
- Inspector의 `Use Mock Api` 체크로 실제 서버와 Mock을 전환합니다.
- 학습 개념: Interface, 의존성 교체, 테스트 더블, 인메모리 상태.

### Step 6: 누적 학습 문서 도입

- 현재 구현과 일치하도록 프로젝트 구조와 실행 방법을 다시 정리했습니다.
- Unity Editor를 처음 사용하는 사람을 위한 기본 용어와 실습 순서를 추가했습니다.
- 앞으로 모든 기능 Step은 이 문서의 학습 기록에 계속 추가합니다.
- 학습 개념: 프로젝트 구조 읽기, 실행 흐름 추적, 검증 기준 세우기.

### Step 7: 실제 PNG Sprite Sheet와 프레임 애니메이션

- 영웅과 슬라임의 4열x4행 도트 Sprite Sheet를 추가했습니다.
- 마젠타 생성 배경을 투명 알파로 변환하고 1024x1024 PNG로 정리했습니다.
- `Resources.Load`로 Texture를 불러와 런타임에 16개 Sprite 프레임으로 나눕니다.
- 평상시에는 Idle, 이동 중에는 Run, 공격 시 Attack, 피격 시 Hit 행을 재생합니다.
- 이미지가 없을 때는 기존 코드 생성 Sprite를 사용하는 fallback을 유지합니다.
- 학습 개념: PNG alpha, Resources, Texture2D, Sprite Sheet, UV 좌표, Point Filter, 프레임 애니메이션.

직접 확인 방법:

1. Unity가 열려 있다면 이미지 Import가 끝날 때까지 기다립니다.
2. `MainScene`을 열고 Play를 누릅니다.
3. 영웅과 슬라임이 가만히 있을 때도 조금씩 움직이는지 확인합니다.
4. Mock 모드에서 `Guest Login`, `Claim`, `Upgrade`를 순서대로 실행합니다.
5. Stage `2`에 도전합니다.
6. 영웅이 Run 동작으로 이동하고 Attack 동작으로 공격하는지 확인합니다.
7. 승리하면 슬라임의 Hit 동작 후 사라지고, 실패하면 영웅의 Hit 동작이 나오는지 확인합니다.

정상 동작 기준:

- 캐릭터 주위에 마젠타 사각형 배경이 보이지 않아야 합니다.
- 확대된 도트 캐릭터가 흐릿하지 않아야 합니다.
- 한 캐릭터의 프레임이 다른 캐릭터 프레임과 섞이지 않아야 합니다.
- 전투를 여러 번 실행해도 이전 애니메이션 Coroutine이 겹치지 않아야 합니다.

### Step 8: Animation Clip과 Animator Controller

- 두 Sprite Sheet를 Unity의 Multiple Sprite 형식으로 각각 16프레임 분할했습니다.
- 영웅과 슬라임에 `Idle`, `Run`, `Attack`, `Hit` Animation Clip을 만들었습니다.
- 각 캐릭터에 int `State` 파라미터를 가진 Animator Controller를 만들었습니다.
- 전투 상태를 `ActorAnimationState` enum으로 정의하고 Animator 파라미터와 연결했습니다.
- Coroutine은 이동과 대기 순서만 담당하고 Sprite 프레임 재생은 Animator로 분리했습니다.
- 동일한 애셋을 다시 만들 수 있는 `IdleGuildAnimationAssetBuilder` Editor 메뉴를 추가했습니다.
- Animator가 없으면 Step 7의 Coroutine 프레임 방식으로 동작하는 fallback을 유지했습니다.
- 학습 개념: Multiple Sprite, Animation Clip, Animator Controller, State Machine, Parameter, enum, Editor 스크립트.

Animator 구조 확인 방법:

1. Project 창에서 `Assets/IdleGuild/Resources/Animations/Hero/HeroAnimator`를 더블 클릭합니다.
2. Animator 창에 `Idle`, `Run`, `Attack`, `Hit` 상태가 있는지 확인합니다.
3. Animator 창 왼쪽 Parameters 탭에서 int 타입 `State`를 확인합니다.
4. `heroIdle.anim`을 선택하고 Inspector의 Loop Time이 켜져 있는지 확인합니다.
5. `heroAttack.anim`을 선택하고 Loop Time이 꺼져 있는지 확인합니다.
6. 같은 방법으로 `SlimeAnimator`도 살펴봅니다.

Play 확인 방법:

1. `MainScene`을 열고 Play를 누릅니다.
2. Hierarchy에서 Play 중 생성된 `Pixel Hero`를 선택합니다.
3. Inspector에 `Sprite Renderer`와 `Animator` 컴포넌트가 모두 있는지 확인합니다.
4. Animator 창을 열어 평소에는 Idle 상태가 파란색으로 활성화되는지 봅니다.
5. Mock 모드에서 로그인, 보상 수령, 강화 후 Stage 2에 도전합니다.
6. 전투 중 활성 상태가 `Run -> Attack -> Run -> Idle` 순서로 바뀌는지 확인합니다.
7. 승리 시 슬라임이 `Hit` 상태를 거쳐 사라지는지 확인합니다.

정상 동작 기준:

- Console에 Animator 또는 Sprite 누락 경고가 없어야 합니다.
- Idle과 Run은 반복되고 Attack과 Hit는 한 번 재생되어야 합니다.
- 영웅 이동 방향 전환 시 Sprite가 왼쪽을 향했다가 복귀 후 오른쪽을 향해야 합니다.
- 같은 Stage를 여러 번 실행해도 Animator 상태와 캐릭터 위치가 초기화되어야 합니다.

### Step 9: 전투 HUD와 타격 피드백

- 영웅과 슬라임 머리 위에 월드 공간 체력 바를 추가했습니다.
- 공격 시 체력 바가 부드럽게 감소하고 데미지 숫자가 위로 떠오릅니다.
- 주황/노랑 픽셀 타격 플래시가 공격 지점에서 퍼집니다.
- 화면 상단에 현재 Stage를, 전투 후에는 `VICTORY` 또는 `DEFEAT`를 표시합니다.
- 실패 시 슬라임의 Attack 애니메이션과 영웅 피격/체력 감소를 추가했습니다.
- 서버 승패와 클라이언트 연출용 HP를 `IdleGuildBattlePresentation`으로 분리했습니다.
- 코드 생성 Sprite를 Full Rect Mesh로 바꿔 Sliced Renderer 경고를 제거했습니다.
- 학습 개념: World Space HUD, 부모/자식 Transform, HP 정규화, TextMesh, alpha, 일회성 이펙트, Presentation 모델.

Play 확인 방법:

1. Unity에서 `MainScene`을 열고 Play를 누릅니다.
2. Mock 모드에서 `Guest Login`, `Claim`, `Upgrade`를 순서대로 실행합니다.
3. Stage `2`에 도전해 승리 연출을 확인합니다.
4. 영웅이 공격할 때 슬라임 체력 바가 0까지 줄어드는지 확인합니다.
5. 슬라임 위에 노란 데미지 숫자와 픽셀 타격 이펙트가 나타나는지 확인합니다.
6. 화면 상단에 `STAGE 2`, 전투 후 `VICTORY`가 표시되는지 확인합니다.
7. 영웅을 강화하지 않은 새 Mock 실행에서 더 높은 Stage에 도전해 패배 연출도 확인합니다.
8. 패배 시 슬라임이 반격하고 영웅 체력 바, 붉은 데미지 숫자, `DEFEAT`가 표시되는지 확인합니다.

Hierarchy 학습 방법:

1. Play 중 `Pixel Hero` 왼쪽 화살표를 열어 `Hero Health Bar` 자식을 확인합니다.
2. `Hero Health Bar` 아래에 `Background`와 `Fill`이 있는지 확인합니다.
3. 전투 중 `Fill`의 Scale X와 Position X가 함께 변하는지 Inspector에서 관찰합니다.
4. 공격 순간 생성되는 `Damage Popup`, `Hit Effect`가 잠시 후 자동 삭제되는지 확인합니다.

정상 동작 기준:

- 체력 바가 캐릭터를 따라 움직이고 캐릭터 크기에 따라 과도하게 확대되지 않아야 합니다.
- 데미지 숫자와 타격 픽셀이 캐릭터 뒤가 아니라 앞에 보여야 합니다.
- 승리 시 몬스터 HP가 0, 패배 시 영웅 HP가 0이 되어야 합니다.
- 다음 도전을 시작하면 양쪽 체력과 Stage 표시가 새 값으로 초기화되어야 합니다.
- Console에 Sprite Tiling, Font, Animator 관련 오류나 경고가 없어야 합니다.

## 10. 다음 학습 Step

이후 단계에서는 런타임 코드로 생성하는 오브젝트를 Unity Prefab과 Scene 편집 구조로 계속 옮깁니다.

1. 런타임 UI의 Panel과 주요 화면을 Prefab 구조로 옮깁니다.
2. 코드 생성 방식과 Inspector 참조 방식의 차이를 배웁니다.
3. 런타임 fallback을 유지하면서 Scene/Prefab 기반 로딩 범위를 넓힙니다.

이 Step부터는 Unity Editor에서 직접 만드는 작업과 C# 코드 작업을 함께 진행합니다. 각 작업 후에는 이 문서의 Step 기록, 직접 실행 절차, 정상 동작 기준을 함께 갱신합니다.

### Step 10: 교체 가능한 캐릭터 외형 세트

- 기존 영웅과 슬라임 Sprite Sheet 및 Animator를 삭제하지 않고 그대로 보존했습니다.
- 단발머리, 빨간 머리띠, 큰 눈, 둥근 얼굴, 청치마의 2등신 여자 주인공 Sprite Sheet를 추가했습니다.
- 검은 복면과 검은 옷을 입은 도둑 몬스터 Sprite Sheet를 추가했습니다.
- `Unity Client Bootstrap` Inspector의 `Character Visual Set`에서 새 외형과 기존 외형을 선택할 수 있습니다.
- 기본값은 `Cute Girl And Masked Thief`이며, `Classic Hero And Slime`을 선택하면 기존 캐릭터가 다시 표시됩니다.
- Editor 빌더가 네 캐릭터의 Idle, Run, Attack, Hit Animation Clip과 Animator Controller를 각각 생성합니다.
- 학습 개념: 직렬화 enum, Inspector 설정, 비파괴 에셋 교체, 외형과 전투 로직의 분리.

#### Unity Editor에서 확인하기

1. Unity가 스크립트와 새 이미지를 Import할 때까지 기다립니다.
2. `MainScene`의 `Unity Client Bootstrap`을 선택합니다.
3. Inspector의 `Character Visual Set`을 `Cute Girl And Masked Thief`로 설정하고 Play를 누릅니다.
4. 여자 주인공과 복면 도둑이 보이는지 확인한 뒤 Stage Challenge를 실행해 네 애니메이션을 확인합니다.
5. Play를 종료하고 `Character Visual Set`을 `Classic Hero And Slime`으로 바꾼 뒤 다시 Play합니다.
6. 기존 영웅과 슬라임도 이전처럼 정상 동작하는지 확인합니다.

정상 동작 기준:

- 두 외형 세트 모두 전투 이동, 공격, 피격, 대기 애니메이션이 동작해야 합니다.
- 외형을 바꿔도 체력 바, 데미지 표시, 승패 결과와 API 흐름은 변하지 않아야 합니다.
- Console에 Sprite 또는 Animator 누락 경고가 없어야 합니다.

### Step 11: 캐릭터 Prefab 우선 로딩

- 기존 영웅, 슬라임, 여자 주인공, 복면 도둑을 각각 재사용 가능한 Prefab으로 자동 생성합니다.
- `IdleGuildCharacterPrefabBuilder`가 SpriteRenderer와 Animator가 연결된 네 Prefab을 `Resources/Prefabs/Characters`에 저장합니다.
- Animation Asset Builder가 Clip과 Controller를 만든 직후 Prefab Builder도 실행하므로 생성 순서가 보장됩니다.
- `IdleGuildGameWorld`는 선택된 외형의 Prefab을 `Resources.Load`로 먼저 불러와 복제합니다.
- Prefab이 없거나 아직 생성되지 않은 경우 기존 코드 생성 GameObject로 돌아가는 fallback을 유지합니다.
- 학습 개념: Prefab, `PrefabUtility`, `Instantiate`, 컴포넌트 재사용, Prefab 우선/fallback 로딩.

#### Unity Editor에서 확인하기

1. Unity 상단 메뉴에서 `Idle Guild > Rebuild Character Animation Assets`를 실행합니다.
2. Project 창의 `Assets/IdleGuild/Resources/Prefabs/Characters`에 Prefab 네 개가 생성됐는지 확인합니다.
3. `GirlHero` Prefab을 선택해 Sprite Renderer와 Animator가 연결됐는지 확인합니다.
4. Play 후 새 외형으로 Stage Challenge를 실행합니다.
5. Play를 종료하고 `Character Visual Set`을 기존 외형으로 바꾼 뒤 다시 실행합니다.

정상 동작 기준:

- Hierarchy의 캐릭터가 `(Clone)`이 제거된 읽기 쉬운 이름으로 표시되어야 합니다.
- 새 외형과 기존 외형 모두 Prefab 기반으로 생성되고 네 애니메이션이 재생되어야 합니다.
- Prefab을 임시로 다른 폴더로 옮겼을 때도 fallback 경고 후 전투 자체는 계속 동작해야 합니다.
- API, 체력 바, 데미지 팝업과 승패 연출은 이전 Step과 동일해야 합니다.

### Step 12: 자동 사냥과 보스 스테이지 분리

- Play를 시작하면 주인공이 Run 애니메이션으로 계속 전진하는 자동 사냥 연출을 시작합니다.
- 작은 몬스터가 반복해서 다가오며, 주인공이 공격하고 처치할 때마다 로컬 `Hunt Gold` 3을 획득합니다.
- 기존 `Challenge Stage` 버튼은 `Boss Stage`로 변경하고 기존 서버 스테이지 API와 보스전 연출을 담당하게 했습니다.
- 보스전이 시작되면 자동 사냥을 잠시 멈추고, 보스전이 끝나면 자동 사냥을 다시 시작합니다.
- 일반 몬스터는 작게, 보스 몬스터는 원래 크기로 표시해 역할을 시각적으로 구분합니다.
- 주인공과 몬스터의 이동 목적지 Y 좌표를 `heroHome.y`로 고정해 일직선 전투가 되도록 수정했습니다.
- 산과 숲, 산길이 있는 픽셀 아트 배경을 `Resources/Backgrounds/mountain-hunt.png`로 추가했습니다.
- 학습 개념: 반복 Coroutine, 일반 사냥/보스전 상태 전환, 로컬 표시 재화와 서버 권위 재화의 차이.

#### 골드 서버 연동 기준

현재 `Hunt Gold (local)`은 화면 연출 검증용이라 Play를 종료하면 초기화됩니다. 실제 서비스에서는 클라이언트가 처치마다 골드를 직접 더하면 조작하기 쉽기 때문에 서버 변경이 필요합니다.

권장 서버 구조는 클라이언트가 매 타격마다 요청하는 방식이 아니라, 서버가 마지막 정산 시각과 전투력을 기준으로 일정 구간의 처치 수와 골드를 계산하는 `POST /api/v1/hunt/settle` 같은 정산 API를 제공하는 방식입니다. 응답에는 획득 골드, 정산 후 잔액, 처치 수, 정산 구간을 포함하고 멱등 키를 적용해야 합니다.

#### Unity Editor에서 확인하기

1. Play를 누르면 로그인 전에도 자동 사냥 연출이 시작되는지 확인합니다.
2. 작은 적이 다가오고 주인공이 공격한 뒤 `Hunt Gold (local)`이 3씩 증가하는지 확인합니다.
3. 로그인 후 `Boss Stage`를 실행해 자동 사냥이 멈추고 기존 보스전이 시작되는지 확인합니다.
4. 주인공과 적의 발 위치가 같은 수평선에 있고 접근 경로가 대각선이 아닌지 확인합니다.
5. 보스전이 끝난 뒤 자동 사냥이 다시 시작되는지 확인합니다.
6. 산속 배경이 화면을 채우고 캐릭터와 UI보다 뒤에 표시되는지 확인합니다.

### Step 13: 런타임 주인공 선택과 검은 고양이

- 빨간 리본, 큰 눈, 검은 털을 가진 귀여운 고양이 주인공의 4×4 Sprite Sheet를 추가했습니다.
- 검은 고양이도 기존 캐릭터와 동일하게 Idle, Run, Attack, Hit 애니메이션과 Prefab을 자동 생성합니다.
- 런타임 UI에 `Girl`, `Black Cat`, `Classic` 주인공 선택 버튼을 추가했습니다.
- 선택값은 `PlayerPrefs`에 저장되며 Scene을 다시 불러온 뒤에도 유지됩니다.
- 여자 주인공과 검은 고양이는 복면 도둑과 싸우고, Classic 영웅은 기존 슬라임과 싸웁니다.
- 기존 Inspector의 `Character Visual Set` 선택도 유지하며 저장된 런타임 선택값이 우선 적용됩니다.
- 학습 개념: 런타임 캐릭터 선택, `PlayerPrefs`, Scene 다시 불러오기, 선택별 Resources 경로 매핑.

#### Unity Editor에서 확인하기

1. Play 후 오른쪽 UI의 `Hero` 행에서 `Black Cat`을 누릅니다.
2. 화면이 다시 구성된 뒤 빨간 리본을 단 검은 고양이가 자동 사냥하는지 확인합니다.
3. `Girl`과 `Classic` 버튼도 눌러 기존 캐릭터가 보존되어 있는지 확인합니다.
4. Play를 종료했다가 다시 시작해 마지막 선택 캐릭터가 유지되는지 확인합니다.
5. `Boss Stage`에서도 선택한 주인공의 공격과 피격 애니메이션이 재생되는지 확인합니다.

### Step 14: 재사용 가능한 월드 체력 바 Prefab

- `WorldHealthBar` Prefab을 자동 생성하는 `IdleGuildUiPrefabBuilder`를 추가했습니다.
- Prefab은 `Background`와 `Fill` SpriteRenderer 계층을 공통 구조로 제공합니다.
- `IdleGuildWorldHealthBar`는 `Resources/Prefabs/UI/WorldHealthBar`를 우선 복제합니다.
- Prefab의 Sprite와 색상은 영웅/적군 용도에 따라 런타임에 주입하므로 하나의 Prefab을 양쪽이 공유합니다.
- Prefab이 아직 생성되지 않았거나 삭제된 경우 기존 코드 생성 방식으로 자동 fallback합니다.
- 캐릭터 크기가 달라도 체력 바가 같은 월드 크기를 유지하도록 부모 Scale의 역배율을 적용합니다.
- Animation Asset Builder 실행 시 캐릭터 Prefab 다음에 UI Prefab도 함께 생성됩니다.
- 학습 개념: UI Prefab 재사용, 자식 Transform 검색, 런타임 속성 주입, Prefab 우선/fallback 구성.

#### Unity Editor에서 확인하기

1. Unity 상단 메뉴에서 `Idle Guild > Rebuild Character Animation Assets`를 실행합니다.
2. `Assets/IdleGuild/Resources/Prefabs/UI/WorldHealthBar.prefab`이 생성됐는지 확인합니다.
3. Prefab을 열어 `Background`와 `Fill` 자식에 SpriteRenderer가 있는지 확인합니다.
4. Play 후 자동 사냥에서 주인공과 작은 적의 체력 바가 캐릭터를 따라다니는지 확인합니다.
5. `Boss Stage`를 실행해 체력 감소와 색상, Fill의 왼쪽 고정 축소가 이전과 동일한지 확인합니다.
6. Prefab을 임시로 Resources 밖으로 옮겼을 때 fallback 경고 없이 기존 체력 바가 계속 생성되는지 확인합니다.

### Step 15: MainScene 전투 배치 Anchor

- `IdleGuildBattleSceneLayout` 컴포넌트에 영웅, 적군, 배경, 바닥 Anchor 참조를 모았습니다.
- `IdleGuildSceneLayoutBuilder`가 MainScene에 `Battle Scene Layout` 오브젝트와 네 자식 Anchor를 자동 생성합니다.
- 영웅과 적군의 시작 위치를 더 이상 코드 상수에만 의존하지 않고 Scene의 Transform 위치에서 읽습니다.
- 배경과 바닥도 각각의 Anchor 아래에 생성되므로 Scene 창에서 위치를 직접 조정할 수 있습니다.
- 캐릭터별 발바닥 보정은 Hero Spawn 위치에 추가 적용되어 소녀와 고양이의 시각적 수평 정렬을 유지합니다.
- Layout이나 일부 Anchor가 없으면 기존 좌표를 사용하는 fallback을 유지합니다.
- 학습 개념: Scene 직렬화 참조, 배치 Anchor, Inspector 기반 구성, 코드 기본값 fallback.

#### Unity Editor에서 확인하기

1. MainScene을 열고 상단 메뉴에서 `Idle Guild > Create or Repair Battle Scene Layout`을 실행합니다.
2. Hierarchy에 `Battle Scene Layout`과 `Hero Spawn`, `Monster Spawn`, `Backdrop Anchor`, `Ground Anchor`가 있는지 확인합니다.
3. Scene 창에서 `Hero Spawn` 또는 `Monster Spawn`을 좌우로 이동한 뒤 Play합니다.
4. 자동 사냥과 Boss Stage가 변경한 시작 위치를 사용하는지 확인합니다.
5. 두 Spawn Anchor의 기본 Y를 같게 유지했을 때 캐릭터 발바닥이 같은 전투선에 표시되는지 확인합니다.
6. `Backdrop Anchor`와 `Ground Anchor`를 움직여 산속 배경과 바닥 위치가 함께 바뀌는지 확인합니다.

정상 동작 기준:

- Scene Anchor 수정만으로 전투 구도를 조정할 수 있어야 합니다.
- 자동 사냥과 보스전이 동일한 Hero/Monster Spawn을 공유해야 합니다.
- Layout이 없는 Scene에서도 기존 기본 좌표로 전투가 실행되어야 합니다.
- 캐릭터 선택, 체력 바, 골드 표시와 서버 API 흐름은 이전 Step과 동일해야 합니다.

### Step 16: Runtime Canvas Prefab 우선 로딩

- `IdleGuildUiPrefabBuilder`가 `RuntimeCanvas.prefab`과 `WorldHealthBar.prefab`을 함께 생성합니다.
- Runtime Canvas Prefab에 Canvas, CanvasScaler, GraphicRaycaster 설정을 저장합니다.
- 기준 해상도 `1280×720`, `Scale With Screen Size`, Match 0.5 설정을 Prefab에서 직접 확인하고 변경할 수 있습니다.
- `IdleGuildRuntimeUi`는 `Resources/Prefabs/UI/RuntimeCanvas`를 우선 복제한 뒤 동적 Panel과 버튼을 자식으로 구성합니다.
- Prefab이 없으면 기존처럼 Canvas와 필수 컴포넌트를 코드로 생성합니다.
- Prefab과 fallback 양쪽에서 중복 컴포넌트가 생기지 않도록 `GetOrAddComponent<T>`를 사용합니다.
- 학습 개념: Screen Space Canvas Prefab, 해상도 대응, 컴포넌트 조회/추가, 정적 UI 설정과 동적 데이터 연결의 분리.

#### Unity Editor에서 확인하기

1. Unity 상단 메뉴에서 `Idle Guild > Rebuild UI Prefabs`를 실행합니다.
2. `Assets/IdleGuild/Resources/Prefabs/UI/RuntimeCanvas.prefab`이 생성됐는지 확인합니다.
3. Prefab Inspector에서 Canvas, Canvas Scaler, Graphic Raycaster가 각각 하나씩 있는지 확인합니다.
4. Play 후 Hierarchy에 `IdleGuild Runtime UI` Canvas가 하나만 생성되는지 확인합니다.
5. 해상도를 변경해도 오른쪽 UI Panel이 화면 안에 유지되는지 확인합니다.
6. RuntimeCanvas Prefab을 Resources 밖으로 임시 이동해도 fallback Canvas에서 버튼이 정상 동작하는지 확인합니다.

### Step 17: 일반 몬스터의 반격과 주인공 체력

- 기존 자동 사냥은 `boss == true`일 때만 공격 Coroutine을 호출했기 때문에 일반 몬스터는 절대로 공격하지 않았습니다.
- 일반 몬스터와 보스가 공통으로 사용하는 `EnemyAttackPattern`을 만들고, 종류에 따라 공격 주기·이동 거리·피해량만 다르게 계산합니다.
- 각 전투가 시작될 때 주인공의 최대 체력과 현재 체력을 만들고 `IdleGuildWorldHealthBar`에 반영합니다.
- 적이 공격하면 적이 앞으로 이동하고, 주인공 Hit 애니메이션·붉은 피해 숫자·화면 흔들림·효과음이 함께 재생됩니다.
- 주인공 체력이 0이 되면 적이 퇴장하고 주인공이 원위치에서 체력을 회복한 뒤 같은 자동 사냥을 계속합니다.
- 지역 전용 단일 Sprite를 사용하는 적은 다른 Sprite Sheet의 공격 프레임으로 바뀌지 않도록 Animator 사용 여부를 확인합니다.
- 학습 개념: Coroutine 기반 양방향 전투, 타이머 비교, 전투 상태 변수, 공통 함수 추출, 조건부 애니메이션 fallback.

#### 코드 흐름 이해하기

1. 몬스터 등장 시 `heroMaxHealth`, `heroHealth`, `nextEnemyAttack`을 초기화합니다.
2. 전투 반복문에서 `Time.time >= nextEnemyAttack`인지 확인합니다.
3. 시간이 되면 `EnemyAttackPattern`이 이동과 피격 연출을 재생합니다.
4. 연출 후 실제 `heroHealth`를 감소시키고 체력 바를 갱신합니다.
5. 체력이 0이면 `RetreatMonster`와 주인공 재정비 흐름으로 이동합니다.
6. 살아 있다면 다음 공격 시간을 예약하고 주인공의 자동 공격을 계속합니다.

#### Unity Editor에서 확인하기

1. MainScene에서 Play를 누릅니다.
2. 첫 일반 몬스터가 접근한 후 주인공을 향해 짧게 돌진하는지 확인합니다.
3. 공격 순간 주인공의 Hit 모션, 붉은 피해 숫자와 체력 감소가 보이는지 확인합니다.
4. 일곱 번째 보스는 일반 몬스터보다 강한 피해와 큰 화면 흔들림을 사용하는지 확인합니다.
5. 주인공 체력이 0이 되면 잠시 재정비한 후 자동 사냥이 다시 시작되는지 확인합니다.

### Step 18: 적군 전투 기준 Y 좌표 조정

- 적군의 `monsterHome.y`를 `-2.75`로 변경했습니다.
- MainScene의 `Monster Spawn` Anchor도 같은 Y 값으로 저장했습니다.
- Scene이 이전 `-2.1` 값을 가지고 있으면 `IdleGuildSceneLayoutBuilder`가 새 기본값으로 이전합니다.
- 런타임에서도 Y를 명시적으로 적용하므로 열린 Scene의 이전 메모리 값 때문에 위치가 되돌아가지 않습니다.
- 바닥 Anchor는 별도 요소이므로 기존 위치를 유지합니다.
- 학습 개념: Scene 직렬화 값, 런타임 좌표, Editor 마이그레이션, 단일 기준값 유지.

#### Unity Editor에서 확인하기

1. MainScene의 `Battle Scene Layout > Monster Spawn`을 선택합니다.
2. Inspector의 Position Y가 `-2.75`인지 확인합니다.
3. Play 후 일반 적과 보스가 모두 같은 Y 기준에서 등장하는지 확인합니다.
4. 적의 접근·공격·퇴장 후에도 Y 위치가 원래 값으로 돌아가지 않는지 확인합니다.

### Step 19: 겹치지 않는 메뉴 패널과 성장 버튼

- 항상 표시되던 성장 UI를 숨기고 하단 `성장` 버튼으로 열도록 변경했습니다.
- 성장·영웅·장비·스킬·설정·임무 패널은 `ToggleExclusivePanel`을 통해 한 번에 하나만 표시됩니다.
- 다른 메뉴 버튼을 누르면 기존 패널이 먼저 닫힌 뒤 선택한 패널이 중앙에 열립니다.
- 같은 메뉴 버튼을 다시 누르거나 패널 내부의 `닫기` 버튼을 누르면 모든 메뉴 패널이 닫힙니다.
- 버튼이 많은 스킬·장비·임무 패널은 높이를 늘려 글자와 버튼이 서로 눌리지 않도록 했습니다.
- 토스트 메시지는 열린 패널보다 앞에 보이도록 마지막 UI Sibling으로 이동합니다.
- 학습 개념: UI 상태의 단일 진실 공급원, 상호 배타 토글, RectTransform Anchor, Sibling 렌더 순서.

#### 코드 흐름 이해하기

1. 하단 메뉴 버튼이 각 `Toggle...Panel` 함수를 호출합니다.
2. 함수는 공통 `ToggleExclusivePanel`에 열 대상 패널을 전달합니다.
3. `CloseAllPanels`가 모든 메뉴 패널을 비활성화합니다.
4. 선택한 패널만 다시 활성화하고 가장 앞쪽 Sibling으로 이동합니다.
5. 패널의 `닫기` 버튼도 같은 `CloseAllPanels`를 사용합니다.

#### Unity Editor에서 확인하기

1. Play 직후 성장 패널이 화면에 항상 떠 있지 않은지 확인합니다.
2. 하단 `성장`을 누르면 중앙에 성장 정보와 강화 버튼이 표시되는지 확인합니다.
3. 성장 패널이 열린 상태에서 `스킬`을 눌러 성장 패널이 닫히고 스킬만 열리는지 확인합니다.
4. 스킬 강화 버튼과 세 스킬 버튼이 패널 안에서 겹치지 않는지 확인합니다.
5. 각 패널의 `닫기` 버튼과 같은 하단 메뉴 버튼 재클릭으로 패널이 닫히는지 확인합니다.
6. Game View 해상도를 `1280×720`과 세로형 해상도로 바꿔 패널이 Safe Area 안에 유지되는지 확인합니다.

### Step 20: 스킬별 캐릭터 애니메이션

- 세 스킬이 색상만 다른 동일 이펙트를 사용하던 구조를 스킬별 코루틴 애니메이션으로 분리했습니다.
- `STAR BURST`는 캐릭터가 살짝 뛰어오르며 청록색 별을 방사한 뒤 몬스터에게 집중시킵니다.
- `SWIFT STRIKE`는 금빛 잔상을 남기며 몬스터 앞으로 빠르게 돌진하고 원래 자리로 복귀합니다.
- `GUARDIAN LIGHT`는 캐릭터가 제자리에서 커졌다 작아지며 녹색·금색 보호 오라를 펼칩니다.
- 스킬 번호를 지역 변수로 보관하므로 연속 입력에서도 다른 스킬의 종류가 덮어써지지 않습니다.
- `skillAnimationPlaying` 잠금 동안 일반 공격과 적 공격을 잠시 대기시켜 스킬 모션이 중간에 끊기지 않게 했습니다.
- 연출 종료 시 위치, 크기, 색을 저장된 값으로 복원하여 캐릭터의 Y 좌표가 누적 변경되지 않습니다.

#### 코드 흐름 이해하기

1. 스킬 버튼이 `ActivateSkill`에 스킬 번호를 전달합니다.
2. 공격 피해는 `pendingSkillDamage`에 더하고, 연출은 `PlaySkillAnimation` 코루틴으로 시작합니다.
3. 공통 디스패처가 번호에 따라 `PlayStarBurst`, `PlaySwiftStrike`, `PlayGuardianLight` 중 하나를 실행합니다.
4. 각 연출은 `CreateSkillParticle`과 `FlySkillParticle`을 재사용해 파티클을 생성하고 이동·페이드아웃합니다.
5. 연출이 끝나면 저장해 둔 Transform과 Renderer 상태를 복구하고 달리기 또는 대기 애니메이션으로 돌아갑니다.

#### Unity Editor에서 확인하기

1. Play 후 하단 `스킬` 메뉴를 열고 세 액티브 스킬을 각각 사용합니다.
2. 별 폭발, 돌진, 보호 오라가 서로 다른 캐릭터 동작으로 보이는지 확인합니다.
3. 자동 전투 도중 스킬을 눌러 일반 공격이나 적 공격이 스킬 모션을 중간에 끊지 않는지 확인합니다.
4. 연속으로 서로 다른 스킬을 눌러 입력 순서대로 연출되는지 확인합니다.
5. 모든 연출 후 주인공의 기준 Y 좌표가 바뀌지 않고 원래 전투선으로 돌아오는지 확인합니다.

### Step 21: 스킬 사용 시 패널 자동 닫기

- 스킬 패널에서 액티브 스킬 사용에 성공하면 `CloseAllPanels`를 먼저 호출하도록 변경했습니다.
- 패널이 닫힌 다음 `skillAction`이 실행되므로 캐릭터의 스킬 애니메이션을 전투 화면에서 바로 볼 수 있습니다.
- 쿨타임이 남은 스킬은 사용에 실패하므로 패널을 닫지 않고 남은 시간을 안내합니다.
- 스킬 강화 버튼은 기존처럼 패널을 유지하여 여러 강화 결과를 계속 확인할 수 있습니다.

#### Unity Editor에서 확인하기

1. Play 후 하단 `스킬` 버튼을 눌러 스킬 패널을 엽니다.
2. 사용 가능한 스킬을 누르면 패널이 즉시 닫히고 캐릭터 애니메이션이 보이는지 확인합니다.
3. 같은 스킬을 쿨타임 중 다시 누르면 패널은 유지되고 재사용 대기 안내가 표시되는지 확인합니다.
4. 스킬 강화 버튼을 눌렀을 때는 패널이 자동으로 닫히지 않는지 확인합니다.

### Step 22: Unity 변경사항과 임시 파일 정리

- `.utmp/`는 Android 빌드 과정에서 CMake와 Gradle이 만드는 중간 산출물이므로 소스 코드가 아니며 Git에 저장하지 않습니다.
- 프로젝트 내부 `.gitignore`에 `.utmp/` 규칙을 추가해 다음 빌드에서 다시 생성돼도 변경사항에 나타나지 않게 했습니다.
- Unity Editor가 자동 재직렬화한 렌더 파이프라인, 볼륨 프로필, Unity Services 설정은 이번 기능과 관계가 없어 기존 Git 상태로 복원했습니다.
- 설정 파일은 실제로 품질, 렌더링 또는 서비스 연결을 의도적으로 변경한 경우에만 기능 커밋과 분리해 저장하는 것이 좋습니다.

#### Git에서 확인하기

1. `git status --short`를 실행해 의도한 파일만 표시되는지 확인합니다.
2. `git check-ignore -v .utmp/example.txt`로 `.utmp/` ignore 규칙이 적용되는지 확인합니다.
3. Unity가 열려 있을 때 자동 생성된 변경이 보이면 바로 커밋하지 말고 `git diff`로 실제 내용을 먼저 확인합니다.

## 12. 매 Step 완료 체크리스트

앞으로 Step 완료 시 아래 항목을 기준으로 마무리합니다.

- [ ] 기능이 코드에 구현됨
- [ ] Unity Editor에서 확인하는 절차가 문서화됨
- [ ] 새 Unity/C# 개념이 초보자 관점으로 설명됨
- [ ] 변경 파일과 데이터 흐름이 문서화됨
- [ ] 가능한 범위의 컴파일/동작 검증이 완료됨
- [ ] `CLIENT_CODE_OVERVIEW.md`의 Step 기록이 갱신됨
- [ ] 해당 Step을 별도 git commit으로 기록함
- [ ] 원격 `main` 브랜치에 push함
