using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SaggingMarionetteString : MonoBehaviour
{
    [Header("연결 설정")]
    public Transform playerJoint; 
    public float stringHeight = 7f; 
    public float followSpeed = 8f; 

    [Header("회전 기준")]
    [Tooltip("실이 따라 돌 기준입니다. 비워 두면 playerJoint의 루트(캐릭터)를 자동으로 사용합니다.")]
    public Transform rotationReference;
    [Tooltip("끄면 예전처럼 월드 기준으로 늘어지고 지연됩니다.")]
    public bool followRotation = true;
    [Tooltip("캐릭터가 빠르게 이동할 때 실 윗점이 뒤처지는 최대 거리입니다.")]
    public float maxFollowLag = 1.5f;
    
    [Header("실 늘어짐(Sag) 설정")]
    [Range(3, 50)]
    public int curveResolution = 20; 
    public float sagAmount = 1.5f;

    [Tooltip("기준 오브젝트의 로컬 방향입니다. (0,-1,-1)이면 캐릭터 기준 아래·뒤쪽으로 늘어집니다.")]
    public Vector3 sagDirection = new Vector3(0, -1, -1); 

    private LineRenderer lineRenderer;
    private Transform space;

    // 목표 지점 대비 실 윗점이 얼마나 뒤처져 있는지를 월드 벡터로 들고 있습니다.
    private Vector3 followLag;
    private Vector3 previousTargetTop;
    private Vector3 previousSpacePosition;
    private Quaternion previousSpaceRotation = Quaternion.identity;
    private bool initialized;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        space = rotationReference != null ? rotationReference : (playerJoint != null ? playerJoint.root : null);
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
        Vector3 topControlPoint = targetTopPosition + UpdateFollowLag(targetTopPosition);

        // 2. 가상의 중간 제어점 계산
        // [꼬임 해결] 늘어지는 방향을 캐릭터 회전에 맞춰 돌려야 180도 돌아도 실끼리 교차하지 않습니다.
        Vector3 midPoint = (topControlPoint + playerJoint.position) / 2f;
        midPoint += GetSagWorldDirection() * sagAmount;

        // 3. 베지에 곡선 그리기
        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector3 curvePoint = CalculateQuadraticBezierPoint(t, topControlPoint, midPoint, playerJoint.position);
            
            // 여기서 발생하던 오류가 윗부분의 positionCount 강제 동기화로 해결됩니다.
            lineRenderer.SetPosition(i, curvePoint);
        }
    }

    /// <summary>
    /// 뒤처짐을 갱신해 월드 오프셋으로 돌려줍니다.
    /// 회전으로 생긴 이동분은 뒤처짐에서 제외하므로, 캐릭터가 돌 때 실은 그대로 같이 돌고
    /// 걸어서 이동할 때만 끌려오는 느낌이 남습니다.
    /// </summary>
    private Vector3 UpdateFollowLag(Vector3 targetTop)
    {
        bool useRotation = followRotation && space != null;

        if (!initialized)
        {
            initialized = true;
            followLag = Vector3.zero;
            CachePreviousFrame(targetTop);
            return followLag;
        }

        Quaternion deltaRotation = useRotation
            ? space.rotation * Quaternion.Inverse(previousSpaceRotation)
            : Quaternion.identity;

        // 캐릭터가 회전만 했을 때 윗점이 도달했어야 할 위치입니다. 이 이동분은 지연시키지 않습니다.
        Vector3 pivot = useRotation ? previousSpacePosition : previousTargetTop;
        Vector3 rigidlyFollowedTop = pivot + deltaRotation * (previousTargetTop - pivot);
        Vector3 residualMove = targetTop - rigidlyFollowedTop;

        // 지수 감쇠라 프레임 레이트가 달라져도 따라오는 속도가 같습니다.
        float decay = 1f - Mathf.Exp(-Mathf.Max(followSpeed, 0f) * Time.deltaTime);
        followLag = Vector3.Lerp(deltaRotation * followLag - residualMove, Vector3.zero, decay);
        followLag = Vector3.ClampMagnitude(followLag, Mathf.Max(maxFollowLag, 0f));

        CachePreviousFrame(targetTop);
        return followLag;
    }

    private void CachePreviousFrame(Vector3 targetTop)
    {
        previousTargetTop = targetTop;

        if (space != null)
        {
            previousSpacePosition = space.position;
            previousSpaceRotation = space.rotation;
        }
    }

    private Vector3 GetSagWorldDirection()
    {
        Vector3 direction = sagDirection.normalized;

        if (followRotation && space != null)
            direction = space.rotation * direction;

        return direction;
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
