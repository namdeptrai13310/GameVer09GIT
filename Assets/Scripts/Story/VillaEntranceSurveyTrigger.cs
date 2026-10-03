using UnityEngine;

namespace HorrorGame.Story
{
    [RequireComponent(typeof(Collider))]
    public class VillaEntranceSurveyTrigger : MonoBehaviour
    {
        private bool triggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
            {
                triggered = true;
                if (VillaSurveyAndRepairQuest.Instance != null)
                {
                    VillaSurveyAndRepairQuest.Instance.OnPlayerEnterVilla();
                }
            }
        }
    }
}
