using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace HorrorGame.UI
{
    /// <summary>
    /// Hiển thị bảng hướng dẫn phím điều khiển khi bắt đầu trò chơi.
    /// Khi người chơi bấm phím di chuyển (WASD), bảng hướng dẫn sẽ tự động mờ dần và biến mất.
    /// </summary>
    public class GameplayTutorialHint : MonoBehaviour
    {
        public static GameplayTutorialHint Instance { get; private set; }

        private CanvasGroup canvasGroup;
        private bool hasMoved = false;
        private bool isFading = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f; // Ẩn ban đầu, chờ hết cutscene/intro
        }

        private void Start()
        {
            StartCoroutine(RoutineShowAfterIntro());
        }

        private IEnumerator RoutineShowAfterIntro()
        {
            // Chờ đến khi người chơi đã ký di chúc và xong cutscene intro
            while ((TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen) ||
                   (HorrorGame.Story.CinematicIntroManager.Instance != null && HorrorGame.Story.CinematicIntroManager.Instance.IsPlayingIntro))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.8f);

            // Hiện dần bảng hướng dẫn
            float elapsed = 0f;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.6f);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            if (hasMoved || isFading || canvasGroup.alpha < 0.1f) return;

            // Kiểm tra phím di chuyển WASD hoặc mũi tên
            bool moveInput = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame ||
                    Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame ||
                    Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                    Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                {
                    moveInput = true;
                }
            }

            // Hoặc kiểm tra vận tốc nhân vật
            var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null && cc.velocity.magnitude > 0.2f)
                {
                    moveInput = true;
                }
            }

            if (moveInput)
            {
                hasMoved = true;
                StartCoroutine(RoutineFadeOutAndDismiss());
            }
        }

        private IEnumerator RoutineFadeOutAndDismiss()
        {
            isFading = true;
            yield return new WaitForSeconds(0.3f); // Delay nhẹ một nhịp cho người chơi kịp nhìn

            float elapsed = 0f;
            float dur = 0.8f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / dur);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
