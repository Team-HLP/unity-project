# VR 아이트래킹·EEG 기반 ADHD 인지 훈련 콘텐츠

시선 추적과 EEG를 동시에 수집하며 주의 집중과 반응 억제 과제를 수행하는 Unity 기반 VR 콘텐츠입니다. 사용자는 우주 환경에서 두 가지 GO/NO-GO형 훈련을 진행하고, 클라이언트는 수행 중 발생한 시선·뇌파·행동 데이터를 JSON으로 저장한 뒤 백엔드 API에 전송합니다.

> 이 프로젝트는 연구용 프로토타입입니다. 수집 데이터와 산출 점수는 임상적으로 검증된 진단 결과가 아니며, 의료기기나 전문적인 진단·치료를 대신할 수 없습니다.

## 주요 기능

### 쾅! 운석 요격전

- 운석을 약 3초간 연속 응시하면 파괴하고 점수를 획득합니다.
- 운석을 놓치면 HP가 감소합니다.
- 연료는 응시하지 않고 통과시켜야 HP가 회복됩니다.

### 외계인을 물리쳐라

- 적을 응시한 상태에서 VIVE 컨트롤러 트리거를 눌러 제거합니다.
- 적을 놓치면 HP가 감소합니다.
- 연료에는 반응하지 않아야 HP가 회복됩니다.

### 생체·행동 데이터 수집

- 사전 베이스라인: 좌·우 동공 크기의 중앙값
- 시선 데이터: 좌·우 동공 크기, 양안 눈 깜빡임 횟수
- EEG 데이터: Delta, Theta, Alpha, Beta, Gamma 밴드
- 행동 데이터: 응시 여부, 대상 종류, 사용자 파괴 및 자동 파괴 시점
- 세션 결과: 성공 여부, 점수, 잔여 HP, 제거한 대상 수

### 세션 관리

- 백엔드 API를 이용한 로그인과 Bearer 토큰 인증
- 처음 실행하는 콘텐츠의 튜토리얼 표시
- 콘텐츠별 과거 플레이 결과 조회
- 세션 종료 시 결과와 생체·행동 JSON 파일을 multipart/form-data로 업로드

## 실행 환경

| 구분 | 요구 사항 |
| --- | --- |
| OS / 빌드 대상 | Windows Standalone |
| Unity | `2021.3.33f1` |
| VR 런타임 | SteamVR, OpenXR |
| HMD | VIVE Pro Eye |
| 입력 장치 | VIVE 컨트롤러 2대, 베이스 스테이션 2대 |
| EEG | Looxid Link 및 Looxid Link Core |
| 네트워크 | 프로젝트 API와 통신 가능한 환경 |

주요 SDK는 저장소에 포함되어 있습니다.

- Tobii XR SDK `3.0.1`
- VIVE OpenXR Plugin `2.5.1`
- Unity OpenXR Plugin `1.12.1` (패키지 잠금 파일 기준)
- XR Interaction Toolkit `2.5.4`
- Looxid Link Unity SDK

## 실행 방법

1. Unity Hub에서 Unity `2021.3.33f1`로 프로젝트를 엽니다.
2. SteamVR과 Looxid Link Core를 설치하고 실행합니다.
3. VIVE Pro Eye, 컨트롤러, 베이스 스테이션 및 Looxid Link를 연결합니다.
4. VIVE 콘솔에서 룸 설정과 시선 보정을 완료하고, Looxid Link의 센서 연결 상태를 확인합니다.
5. [`Assets/3.Scripts/Apiconfig.cs`](Assets/3.Scripts/Apiconfig.cs)의 `url`을 사용할 백엔드 API 주소로 설정합니다.
6. Unity에서 [`Assets/1.Scenes/MENU.unity`](Assets/1.Scenes/MENU.unity)를 열고 Play를 누릅니다.

실행 흐름은 다음과 같습니다.

```text
메인 메뉴 -> 로그인 -> 콘텐츠 선택 -> 동공 베이스라인 측정
          -> 튜토리얼 -> 90초 훈련 -> 결과 저장·전송 -> 결과 조회
```

Windows 실행 파일을 만들 때는 `File > Build Settings`에서 `PC, Mac & Linux Standalone`을 선택하고 Target Platform을 `Windows`로 설정합니다. 필요한 씬과 순서는 `ProjectSettings/EditorBuildSettings.asset`에 등록되어 있습니다.

## 백엔드 연동

이 저장소에는 백엔드, 보호자용 앱, 의료진용 웹 대시보드 소스가 포함되어 있지 않습니다. 로그인, 튜토리얼 이력 확인, 결과 업로드 및 결과 조회를 사용하려면 아래 경로와 호환되는 별도 API 서버가 필요합니다.

| 기능 | 메서드 / 경로 |
| --- | --- |
| 로그인 | `POST /user/login` |
| 플레이 이력 존재 여부 | `GET /games/exist?gameCategory={category}` |
| 운석 요격 결과 업로드 | `POST /game/meteorite` |
| 외계인 제거 결과 업로드 | `POST /game/mole` |
| 콘텐츠별 결과 조회 | `GET /games?gameCategory={category}` |

결과 업로드 요청은 `request`, `eeg_data_file`, `eye_data_file`, `behavior_file` 파트로 구성됩니다. 인증이 필요한 요청에는 로그인 응답의 `access_token`을 `Authorization: Bearer {token}` 헤더로 전달합니다.

## 로컬 데이터

플레이 중 생성되는 파일은 Unity의 `Application.persistentDataPath`에 저장되며 다음 세션에서 같은 이름으로 갱신됩니다.

| 파일 | 내용 |
| --- | --- |
| `eye_data.json` | 눈 깜빡임 횟수, 베이스라인 및 시계열 동공 크기 |
| `eeg_data.json` | 시간대별 5개 EEG 밴드 값 |
| `behavior_data.json` | 시선 상태, 대상 이름 및 파괴 이벤트 |

## 프로젝트 구조

```text
Assets/
├── 1.Scenes/       # 메뉴, 로그인, 베이스라인, 훈련, 성공·실패 및 결과 씬
├── 2.Prefabs/      # XR Rig, 시선 추적기, 훈련 대상 및 UI 프리팹
├── 3.Scripts/      # 게임 흐름, 시선·EEG 수집, API 통신 및 UI 코드
├── LooxidLink/     # Looxid Link Unity SDK
├── SteamVR/        # SteamVR Unity Plugin
├── XR/             # OpenXR 로더와 기능 설정
└── StreamingAssets/SteamVR/  # VIVE 입력 액션 및 바인딩

Packages/
├── TobiiXRSDK_3.0.1.179/     # 임베디드 Tobii XR SDK
└── vive-openxr/               # 임베디드 VIVE OpenXR Plugin
```

## 현재 제한 사항

- 집중력·충동 억제 점수와 ADHD 상태 판별 기준은 내부 테스트와 선행 연구를 바탕으로 설계되었으며, 의료 전문가 검토와 임상 타당성 검증이 완료되지 않았습니다.
- 실제 사용자 대상 동의 절차와 윤리 검토 과정은 클라이언트에 구현되어 있지 않습니다.
- 시선 및 EEG 품질은 장비 착용 상태, 시선 보정, 움직임과 외부 노이즈의 영향을 받습니다.
- API 주소가 소스 코드에 설정되므로 배포 환경에 맞게 변경해야 합니다.
