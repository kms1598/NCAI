using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SaggingMarionetteString : MonoBehaviour
{
    [Header("연결 설정")]
    public Transform playerJoint; 
    public float stringHeight = 7f; 
    public float followSpeed = 8f; 
    
    [Header("실 늘어짐(Sag) 설정")]
    [Range(3, 50)]
    public int curveResolution = 20; 
    public float sagAmount = 1.5f;

    [Tooltip("탑다운 뷰에서는 Y축(-1)만 쓰면 곡선이 안 보일 수 있습니다. Z축이나 X축 값을 섞어주세요. (예: 0, -1, -1)")]
    public Vector3 sagDirection = new Vector3(0, -1, -1); 

    private LineRenderer lineRenderer;
    private Vector3 topControlPoint;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (playerJoint != null)
        {
            topControlPoint = playerJoint.position + Vector3.up * stringHeight;
        }
    }

    void LateUpdate()
    {
        if (playerJoint == null) return;

        // [오류 해결] 다른 요인으로 인해 점 개수가 꼬이는 것을 방지하기 위해 매 프레임 개수를 강제 고정합니다.
        if (lineRenderer.positionCount != curveResolution)
        {
            lineRenderer.positionCount = curveResolution;
        }

        // 1. 조종자 손 위치 계산
        Vector3 targetTopPosition = playerJoint.position + Vector3.up * stringHeight;
        topControlPoint = Vector3.Lerp(topControlPoint, targetTopPosition, Time.deltaTime * followSpeed);

        // 2. 가상의 중간 제어점 계산 
        // [시점 문제 해결] 무조건 아래가 아니라, sagDirection에 설정된 방향으로 선을 늘어뜨립니다.
        Vector3 midPoint = (topControlPoint + playerJoint.position) / 2f;
        midPoint += sagDirection.normalized * sagAmount;

        // 3. 베지에 곡선 그리기
        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector3 curvePoint = CalculateQuadraticBezierPoint(t, topControlPoint, midPoint, playerJoint.position);
            
            // 여기서 발생하던 오류가 윗부분의 positionCount 강제 동기화로 해결됩니다.
            lineRenderer.SetPosition(i, curvePoint);
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0; 
        p += 2 * u * t * p1; 
        p += tt * p2; 
        return p;
    }
}