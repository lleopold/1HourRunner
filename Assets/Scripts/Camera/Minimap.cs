using UnityEngine;

public class Minimap : MonoBehaviour
{
    public Transform player;

    private void Awake()
    {
        //player = GameObject.Find("Player").transform;

    }
    private void LateUpdate()
    {
        if (player == null)
        {
            player = GameObject.Find("Player").transform;
        }
        Vector3 newPosition = player.position;
        newPosition.y = transform.position.y;
        transform.position = newPosition;

        //transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
    }
}
