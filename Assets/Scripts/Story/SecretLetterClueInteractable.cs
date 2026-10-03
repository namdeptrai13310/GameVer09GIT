using UnityEngine;
using HorrorGame.InteractSystem;

namespace HorrorGame.Story
{
    [RequireComponent(typeof(Collider))]
    public class SecretLetterClueInteractable : MonoBehaviour, IPlayerInteractable
    {
        public bool hasBeenRead = false;

        public bool CanInteract()
        {
            return !hasBeenRead;
        }

        public string GetInteractionPrompt()
        {
            return "Đọc Di Thư Của Người Cha";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (SecretLetterClueUI.Instance != null)
            {
                SecretLetterClueUI.Instance.OpenLetter();
            }
        }
    }
}
