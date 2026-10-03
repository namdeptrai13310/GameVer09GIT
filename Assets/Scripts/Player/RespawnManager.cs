using UnityEngine;
using HorrorGame.Story;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance; // Để sau này quái cắn thì gọi hàm dễ dàng

    [Header("Kéo thả vào đây")]
    public Transform spawnPoint;
    public GameObject player;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Nếu đang có cutscene mở đầu thì không respawn
        // vì CinematicIntroManager sẽ tự đặt vị trí nhân vật bên cạnh xe
        var introManager = FindAnyObjectByType<HorrorGame.Story.CinematicIntroManager>();
        if (introManager != null && introManager.gameObject.activeInHierarchy)
        {
            return; // Cutscene intro sẽ tự xử lý vị trí player
        }
        RespawnPlayer();
    }

    public void RespawnPlayer()
    {
        // Bắt buộc phải tắt Character Controller trước khi đổi tọa độ
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Ép tọa độ và góc xoay của Player bằng đúng với SpawnPoint
        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;

        // Bật lại Character Controller để đi lại bình thường
        if (cc != null) cc.enabled = true;
    }
}