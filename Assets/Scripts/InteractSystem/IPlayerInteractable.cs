namespace HorrorGame.InteractSystem
{
    public interface IPlayerInteractable
    {
        bool CanInteract();
        string GetInteractionPrompt();
        string GetInteractionKey();
        void OnInteract();
    }
}
