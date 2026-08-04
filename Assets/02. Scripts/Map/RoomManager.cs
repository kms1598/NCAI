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

    void Awake()
    {
        if (instance == null) instance = this;
        if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        foreach(var r in rooms)
        {
            if (r.root != null) r.root.SetActive(r.roomId == startRoomId);

            currentRoomId = startRoomId;

            var start = FindRoom(startRoomId);
            if (start != null && CameraSpin.instance != null) CameraSpin.instance.SetRoom(start.root.transform.position, start.tallRoom);

            if (startSpawn != null) TeleportPlayer(startSpawn.position);
        }
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
        var cc = PlayerController.instance.cc;
        cc.enabled = false;
        PlayerController.instance.transform.position = pos;
        cc.enabled = true;
        PlayerController.instance.ResetVelocity();
    }

    Room FindRoom(string id)
    {
        foreach( var r in rooms)
        {
            if (r.roomId == id) return r;
        }

        return null;
    }
}
