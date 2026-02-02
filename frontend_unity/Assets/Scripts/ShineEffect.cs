using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ShineEffect : MonoBehaviour
{
    private TextMeshProUGUI _tmp;
    private Material _sharedMaterial;
    
    [Range(0.1f, 5f)] public float shineSpeed = 1.2f;
    [SerializeField] private string propertyName = "_ShineOffset"; // Shader Graph 내 변수명

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        // 런타임에 인스턴스화된 매테리얼을 사용하여 다른 텍스트에 영향 주지 않음
        _tmp.fontMaterial = new Material(_tmp.fontMaterial);
        _sharedMaterial = _tmp.fontMaterial;
    }

    void Update()
    {
        // -1에서 1 사이를 반복하며 빛이 훑고 지나가는 연출
        float offset = Mathf.Repeat(Time.time * shineSpeed, 2.0f) - 1.0f;
        _sharedMaterial.SetFloat(propertyName, offset);
    }
}