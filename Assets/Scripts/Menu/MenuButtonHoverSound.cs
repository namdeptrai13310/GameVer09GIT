using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TestamentMenuController.Instance != null)
        {
            TestamentMenuController.Instance.PlayHoverSound();
        }
    }
}
