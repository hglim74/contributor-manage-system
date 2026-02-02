# Donor Display System - Unity Client

이 프로젝트는 Backend 서버로부터 실시간 기부자 정보를 수신하여 화려한 연출과 함께 화면에 표시하는 Unity 클라이언트입니다.

## 🛠️ 실행 준비 (Prerequisites)

1. **Unity 설치**: Unity 2021.3 LTS 이상 버전을 권장합니다.
2. **Backend 서버 실행**: Unity 클라이언트는 Backend 서버(`ws://localhost:8000/ws/display`)와 연결되어야 정상 작동합니다. 먼저 Backend 서버를 실행해주세요.

## 🚀 실행 가이드 (Execution Guide)

### 1. 프로젝트 열기
Unity Hub를 실행하고 `ADD` 버튼을 눌러 `frontend_unity` 폴더를 선택하여 프로젝트를 추가하고 엽니다.

### 2. 씬(Scene) 설정
**주의**: 현재 프로젝트에 저장된 씬 파일이 없는 경우, 다음 단계를 따라 씬을 구성해주세요.

1. **새 씬 생성**: File > New Scene (Basic (Built-in)) 선택.
2. **DisplaySystem 생성**:
   - Hierarchy 창에서 우클릭 > `Create Empty` > 이름을 `DisplayManager`로 변경.
   - `Assets/Scripts/DisplaySystem.cs` 스크립트를 `DisplayManager`에 드래그하여 추가.
3. **UI 구성**:
   - Hierarchy 창에서 우클릭 > `UI` > `Canvas` 생성.
   - Canvas 하위에 `Panel` 생성 (이것이 `Spawn Parent`가 됩니다).
   - `DisplayManager`의 `Spawn Parent` 필드에 방금 만든 Panel을 할당.
4. **Card Prefab 설정**:
   - UI > Image 등을 조합하여 카드 모양을 만들고, `NameText`, `AmountText`라는 이름의 TextMeshProUGUI 컴포넌트를 추가합니다.
   - 완성된 UI를 Project 창으로 드래그하여 Prefab으로 만들고, `DisplayManager`의 `Card Prefab` 필드에 할당합니다.
5. **VFX 설정**:
   - Visual Effect Graph 패키지가 설치되어 있다면, Visual Effect 오브젝트를 생성하고 `DisplayManager`의 `Global VFX` 필드에 할당합니다.

### 3. 실행 (Play)
- Unity 에디터 상단의 ▶️ (Play) 버튼을 누릅니다.
- Console 창에 `<color=green>Backend Connected!</color>` 메시지가 뜨면 연결 성공입니다.

### 4. 테스트
- Backend 서버의 Admin 페이지(`http://localhost:8000/static/admin.html`)에서 기부자를 등록하거나, `bulk_upload_template.csv`를 업로드하면 Unity 화면에 연출이 나타나는지 확인합니다.

---

## 📁 주요 스크립트 설명

- **DisplaySystem.cs**: 웹소켓 연결 관리 및 데이터 수신, 화면 연출을 총괄하는 메인 스크립트입니다.
- **ShineEffect.cs**: 텍스트에 빛이 지나가는 효과를 주는 연출용 스크립트입니다.
