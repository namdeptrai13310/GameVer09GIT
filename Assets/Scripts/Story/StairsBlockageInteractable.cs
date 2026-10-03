using System.Collections;
using UnityEngine;
using HorrorGame.InteractSystem;
using HorrorGame.Player;
using HorrorGame.UI;

namespace HorrorGame.Story
{
    /// <summary>
    /// Chướng ngại vật chắn cầu thang lên tầng 2 (đống bàn ghế, cây đàn cũ nát ngổn ngang).
    /// Người chơi phải dọn dẹp xong tầng 1, ra xe lấy rìu của cha rồi dùng rìu chém phá đống này.
    /// Khi chém phá:
    /// 1. Vỡ ra 3 tấm ván gỗ rơi xuống chân cầu thang.
    /// 2. Làm rơi ra một bức thư di thư cũ kẹp sau cây đàn.
    /// 3. Khơi thông lối đi lên tầng 2!
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StairsBlockageInteractable : MonoBehaviour, IPlayerInteractable
    {
        public static StairsBlockageInteractable Instance { get; private set; }

        public bool isDestroyed = false;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxWoodChop;
        public AudioClip sfxWoodCrumble;

        private void Awake()
        {
            Instance = this;
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.8f;
            }
        }

        public bool CanInteract()
        {
            if (isDestroyed) return false;
            return true;
        }

        public string GetInteractionPrompt()
        {
            if (isDestroyed) return "";

            var quest = VillaSurveyAndRepairQuest.Instance;
            if (quest == null) return "Chướng Ngại Vật Cầu Thang";

            if (quest.clutterCleanedCount < quest.clutterRequired)
            {
                return "Cầu Thang Bị Chặn (Cần dọn rác tầng 1 trước)";
            }

            if (!quest.hasAxe && (QuickSlotSystem.Instance == null || !QuickSlotSystem.Instance.HasItem("axe")))
            {
                return "Cầu Thang Bị Chặn (Cần Cây Rìu ngoài xe)";
            }

            return "Dùng Rìu Chém Phá Chướng Ngại Vật";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (isDestroyed) return;

            var quest = VillaSurveyAndRepairQuest.Instance;
            if (quest == null) return;

            string pName = PlayerPrefs.GetString("PlayerName", "An");

            // 1. Chưa dọn dẹp xong tầng 1
            if (quest.clutterCleanedCount < quest.clutterRequired)
            {
                StoryObjectiveBanner.ShowObjective(
                    pName.ToUpper(),
                    $"Đống bàn ghế với cây đàn mục nát này chặn đứng cả lối lên rồi! Phải dọn hết đồ phế liệu bừa bộn ở tầng 1 đem ra bãi sau trước đã ({quest.clutterCleanedCount}/{quest.clutterRequired}).",
                    5.5f
                );

                if (quest.currentPhase <= VillaSurveyAndRepairQuest.QuestPhase.DiscoverBlockedStairs)
                {
                    quest.StartCleanupQuest();
                }
                return;
            }

            // 2. Đã dọn xong tầng 1 nhưng chưa lấy rìu
            bool hasAxe = quest.hasAxe || (QuickSlotSystem.Instance != null && QuickSlotSystem.Instance.HasItem("axe"));
            if (!hasAxe)
            {
                StoryObjectiveBanner.ShowObjective(
                    pName.ToUpper(),
                    "Đống chướng ngại vật này kẹt chặt quá, tay không thì chịu thua! Nhớ là ngoài thùng xe bán tải có CÂY RÌU của cha để lại!",
                    5.5f
                );

                quest.TransitionToFetchAxe();
                return;
            }

            // 3. Đã có Rìu -> Tiến hành chém phá!
            DoSmashBlockage();
        }

        public void DoSmashBlockage()
        {
            if (isDestroyed) return;
            isDestroyed = true;

            var quest = VillaSurveyAndRepairQuest.Instance;
            if (quest != null)
            {
                quest.SmashStairsBlockage(this);
            }
        }
    }
}
