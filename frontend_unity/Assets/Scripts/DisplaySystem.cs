using UnityEngine;
using System.Net.WebSockets;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections;
using TMPro;
using UnityEngine.VFX;

public class DisplaySystem : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private string serverUrl = "ws://localhost:8000/ws/display";
    private ClientWebSocket _ws = new ClientWebSocket();
    private CancellationTokenSource _cts = new CancellationTokenSource();

    [Header("UI & Effect References")]
    public GameObject cardPrefab;      // 기부자 정보가 담긴 UI 프리팹
    public Transform spawnParent;      // 카드가 생성될 부모 Canvas/Panel
    public VisualEffect globalVFX;     // 등급별 파티클 효과 (VFX Graph)

    // 멀티스레드(수신)와 메인스레드(연출) 간의 안전한 데이터 교환을 위한 큐
    private ConcurrentQueue<DonorData> _displayQueue = new ConcurrentQueue<DonorData>();
    private bool _isProcessing = false;

    private async void Start()
    {
        await ConnectToServer();
    }

    private async Task ConnectToServer()
    {
        try
        {
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(new Uri(serverUrl), _cts.Token);
            Debug.Log("<color=green>Backend Connected!</color>");
            
            // Start receiving messages
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"Socket Connection Error: {e.Message}");
            // Optional: Retry logic could be added here
        }
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (_ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    Debug.Log("Socket Closed by Server");
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleMessage(json);
                }
            }
        }
        catch (Exception e)
        {
            if (!_cts.IsCancellationRequested)
            {
                Debug.LogError($"Socket Receive Error: {e.Message}");
            }
        }
    }

    private void HandleMessage(string json)
    {
        try
        {
            var msg = JsonUtility.FromJson<SocketPayload>(json);
            if (msg != null && msg.type == "NEW_DONOR")
            {
                _displayQueue.Enqueue(msg.payload);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to parse message: {json}. Error: {e.Message}");
        }
    }

    void Update()
    {
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
        GameObject card = null; 
        
        if (cardPrefab != null && spawnParent != null)
        {
            card = Instantiate(cardPrefab, spawnParent);
            var nameTxt = card.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var amountTxt = card.transform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();

            if (nameTxt != null) nameTxt.text = data.name;
            if (amountTxt != null) amountTxt.text = $"{data.amount:N0}원";
        }
        else
        {
            Debug.LogWarning("CardPrefab or SpawnParent is not assigned!");
        }

        // 2. 등급별 VFX 파라미터 조절 및 재생
        if (globalVFX != null)
        {
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
        }

        // 3. 연출 대기
        yield return new WaitForSeconds(6.0f); 

        // 4. 정리
        if (card != null)
        {
            Destroy(card);
        }
        _isProcessing = false;
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        if (_ws != null) _ws.Dispose();
    }
}