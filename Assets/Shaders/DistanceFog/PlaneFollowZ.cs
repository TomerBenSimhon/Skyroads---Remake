using UnityEngine;

public class PlaneFollowZOnly : MonoBehaviour
{
    public Transform player;
    private float offsetZ;

    void Start()
    {
        offsetZ = transform.position.z - player.position.z;
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;

        // שמור את X ו־Y קבועים לפי הערך ההתחלתי
        pos.z = player.position.z + offsetZ;
        transform.position = pos;

        // נעל רוטציה מוחלטת
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
