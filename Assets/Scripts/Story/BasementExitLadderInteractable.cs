using System.Collections;
using UnityEngine;
using HorrorGame.InteractSystem;

namespace HorrorGame.Story
{
    /// <summary>
    /// Cầu thang leo ngược từ Tầng Hầm lên lại gian nhà kho của biệt thự.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BasementExitLadderInteractable : MonoBehaviour, IPlayerInteractable
    {
        public Vector3 villaReturnPos = new Vector3(78.8f, 11.5f, 22.3f);
        public float returnYRot = 0f;

        public bool CanInteract()
        {
            return true;
        }

        public string GetInteractionPrompt()
        {
            return "Leo Lên Lại Gian Nhà Biệt Thự";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            StartCoroutine(RoutineReturnToVilla());
        }

        private IEnumerator RoutineReturnToVilla()
        {
            var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = villaReturnPos;
                player.transform.rotation = Quaternion.Euler(0f, returnYRot, 0f);

                yield return new WaitForSeconds(0.05f);
                if (cc != null) cc.enabled = true;
            }

            StoryObjectiveBanner.ShowObjective("TRỞ LẠI BIỆT THỰ", "Đã leo lên lại tầng trệt biệt thự an toàn.", 3.0f);
        }
    }
}
