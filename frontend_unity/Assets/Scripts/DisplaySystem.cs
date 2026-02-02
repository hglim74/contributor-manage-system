using UnityEngine;
using NativeWebSocket; // 오픈소스 NativeWebSocket 사용 권장
using System.Collections.Concurrent;
using System.Collections;
using TMPro;
using UnityEngine.VFX;

public class DisplaySystem : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private string serverUrl = "ws://localhost:8000/ws/display";
    private WebSocket _ws;

    [Header("UI & Effect References")]
    public GameObject cardPrefab;      // 기부자 정보가 담긴 UI 프리팹
    public Transform spawnParent;      // 카드가 생성될 부모 Canvas/Panel
    public VisualEffect globalVFX;     // 등급별 파티클 효과 (VFX Graph)

    // 멀티스레드(수신)와 메인스레드(연출) 간의 안전한 데이터 교환을 위한 큐
    private ConcurrentQueue<DonorData> _displayQueue = new ConcurrentQueue<DonorData>();
    private bool _isProcessing = false;

    async void Start()
    {
        _ws = new WebSocket(serverUrl);

        _ws.OnOpen += () => Debug.Log("<color=green>Backend Connected!</color>");
        _ws.OnError += (e) => Debug.LogError($"Socket Error: {e}");
        
        _ws.OnMessage += (bytes) =>
        {
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            var msg = JsonUtility.FromJson<SocketPayload>(json);
            
            if (msg.type == "NEW_DONOR")
            {
                _displayQueue.Enqueue(msg.payload);
            }
        };

        await _ws.Connect();
    }

    void Update()
    {
        // WebSocket 메시지 큐 비우기 (메인 스레드에서 실행 필수)
        #if !UNITY_WEBGL || UNITY_EDITOR
        _ws.DispatchMessageQueue();
        #endif

        // 연출 중이 아니고 큐에 데이터가 있으면 실행
        if (!_isProcessing && _displayQueue.TryDequeue(out var nextDonor))
        {
            StartCoroutine(SequenceDonorEffect(nextDonor));
        }
    }

    private IEnumerator SequenceDonorEffect(DonorData data)
    {
        _isProcessing = true;

        // 1. 카드 생성 및 텍스트 설정
        GameObject card = Instantiate(cardPrefab, spawnParent);
        var nameTxt = card.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        var amountTxt = card.transform.Find("AmountText").GetComponent<TextMeshProUGUI>();

        nameTxt.text = data.name;
        amountTxt.text = $"{data.amount:N0}원";

        // 2. 등급별 VFX 파라미터 조절 및 재생
        if (data.grade == "VVIP")
        {
            globalVFX.SetInt("SpawnCount", 1000); // VVIP는 화려하게
            globalVFX.SetVector4("ParticleColor", new Color(1f, 0.84f, 0f)); // Gold
        }
        else
        {
            globalVFX.SetInt("SpawnCount", 200);
            globalVFX.SetVector4("ParticleColor", Color.white);
        }
        globalVFX.Play();

        // 3. 연출 대기 (애니메이션은 카드 프리팹 내 Animator가 담당한다고 가정)
        yield return new WaitForSeconds(6.0f); 

        // 4. 정리
        Destroy(card);
        _isProcessing = false;
    }

    private async void OnApplicationQuit()
    {
        if (_ws != null) await _ws.Close();
    }
}