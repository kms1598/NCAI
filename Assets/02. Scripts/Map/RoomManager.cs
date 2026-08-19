using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager instance;

    [System.Serializable]
    public class Room
    {
        public string roomId;
        public GameObject root;
        public bool tallRoom;
    }

    public Room[] rooms;

    public string startRoomId;
    public Transform startSpawn;

    private string currentRoomId;

    /// <summary>지금 활성화된 방 id입니다. 컷씬 복귀 위치 저장에 씁니다.</summary>
    public string CurrentRoomId => currentRoomId;

    void Awake()
    {
        if (instance == null) instance = this;
        if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        // 컷씬에서 돌아올 때는 AbilityManager가 조각 위치로 되돌리므로,
        // 여기서 시작 스폰으로 끌어가면 안 됩니다.
        if (AbilityManager.instance != null && AbilityManager.instance.HasReturnPose)
        {
            ApplyReturnPose(AbilityManager.instance);
            return;
        }

        ApplyStartRoom();
    }

    void ApplyStartRoom()
    {
        foreach (var r in rooms)
        {
            if (r.root != null) r.root.SetActive(r.roomId == startRoomId);
        }

        currentRoomId = startRoomId;

        var start = FindRoom(startRoomId);
        if (start != null && CameraSpin.instance != null)
            CameraSpin.instance.SetRoom(start.root.transform.position, start.tallRoom);

        if (startSpawn != null) TeleportPlayer(startSpawn.position);
    }

    /// <summary>
    /// 컷씬 복귀용입니다. 저장해 둔 방을 켜고 플레이어를 그 좌표로 옮깁니다.
    /// </summary>
    public void ApplyReturnPose(AbilityManager ability)
    {
        if (ability == null || !ability.HasReturnPose) return;

        string roomId = ability.ReturnRoomId;
        Vector3 position = ability.ReturnPosition;

        if (!string.IsNullOrEmpty(roomId))
        {
            foreach (var r in rooms)
            {
                if (r.root != null) r.root.SetActive(r.roomId == roomId);
            }

            currentRoomId = roomId;

            var room = FindRoom(roomId);
            if (room != null && room.root != null && CameraSpin.instance != null)
                CameraSpin.instance.SetRoom(room.root.transform.position, room.tallRoom);
        }

        TeleportPlayer(position);
        ability.ClearReturnPose();
    }

    public void SwitchRoom(string roomId, Vector3 teleportTo)
    {
        var next = FindRoom(roomId);
        var prev = FindRoom(currentRoomId);
        prev.root.SetActive(false);
        next.root.SetActive(true);
        currentRoomId = roomId;

        TeleportPlayer(teleportTo);

        CameraSpin.instance.SetRoom(next.root.transform.position, next.tallRoom);
    }

    void TeleportPlayer(Vector3 pos)
    {
        if (PlayerController.instance == null) return;

        var cc = PlayerController.instance.cc;
        cc.enabled = false;
        PlayerController.instance.transform.position = pos;
        cc.enabled = true;
        PlayerController.instance.ResetVelocity();
    }

    Room FindRoom(string id)
    {
        foreach (var r in rooms)
        {
            if (r.roomId == id) return r;
        }

        return null;
    }
}
