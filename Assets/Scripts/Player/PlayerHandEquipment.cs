using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorGame.Player
{
    public class PlayerHandEquipment : MonoBehaviour
    {
        public static PlayerHandEquipment Instance { get; private set; }

        [Header("Socket References (Full Body / Shadows)")]
        public Transform handSocket;
        public Animator animator;

        [Header("In-Hand Socket Models (civilian_girl/hand_r)")]
        public GameObject handFlashlight;
        public GameObject handAxe;
        public GameObject handWoodPlank;
        public GameObject handToolbox;
        public GameObject handKey;
        public GameObject handTalisman;

        [Header("FPS Viewmodel References (Attached to Camera)")]
        public Transform fpsHolder;
        public GameObject fpsFlashlight;
        public GameObject fpsAxe;
        public GameObject fpsWoodPlank;
        public GameObject fpsToolbox;
        public GameObject fpsKey;
        public GameObject fpsTalisman;

        [Header("Flashlight Light Reference")]
        public Light inHandSpotlight;

        [Header("Viewmodel Sway & Bobbing")]
        public bool enableSway = true;
        public float swayAmount = 1.2f;
        public float maxSwayAngle = 4f;
        public float swaySmoothness = 6f;

        public bool enableBobbing = true;
        public float bobbingSpeed = 8f;
        public float bobbingAmount = 0.012f;

        private string currentEquippedId = "";
        private Vector3 holderInitialPos;
        private Quaternion holderInitialRot;
        private float bobTimer = 0f;
        private CharacterController characterController;
        private Coroutine axeSwingRoutine;
        private Coroutine pickupRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            characterController = GetComponent<CharacterController>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            FindOrCreateSocketAndModels();
        }

        private void Start()
        {
            if (fpsHolder != null)
            {
                holderInitialPos = fpsHolder.localPosition;
                holderInitialRot = fpsHolder.localRotation;
            }
        }

        private void Update()
        {
            HandleFpsSwayAndBobbing();

            // Cho phép vung rìu khi click chuột trái và đang cầm rìu
            if (currentEquippedId == "axe")
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (TestamentMenuController.Instance == null || !TestamentMenuController.Instance.isMenuOpen)
                    {
                        PlayAttackAnimation();
                    }
                }
            }
        }

        private void HandleFpsSwayAndBobbing()
        {
            if (fpsHolder == null) return;

            // 1. Mouse Sway
            Quaternion targetSwayRot = holderInitialRot;
            if (enableSway && Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.05f;
                float mouseX = Mathf.Clamp(-mouseDelta.x * swayAmount, -maxSwayAngle, maxSwayAngle);
                float mouseY = Mathf.Clamp(mouseDelta.y * swayAmount, -maxSwayAngle, maxSwayAngle);

                Quaternion swayRotation = Quaternion.Euler(mouseY, mouseX, -mouseX * 0.5f);
                targetSwayRot = holderInitialRot * swayRotation;
            }

            fpsHolder.localRotation = Quaternion.Slerp(fpsHolder.localRotation, targetSwayRot, Time.deltaTime * swaySmoothness);

            // 2. Walking Bobbing
            Vector3 targetBobPos = holderInitialPos;
            if (enableBobbing && characterController != null)
            {
                Vector3 horizontalVel = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
                float speed = horizontalVel.magnitude;

                if (speed > 0.1f && characterController.isGrounded)
                {
                    bobTimer += Time.deltaTime * (bobbingSpeed * (speed / 3.5f));
                    float wavesliceX = Mathf.Cos(bobTimer * 0.5f);
                    float wavesliceY = Mathf.Sin(bobTimer);

                    float totalTranslateX = wavesliceX * bobbingAmount * 0.5f;
                    float totalTranslateY = Mathf.Abs(wavesliceY) * bobbingAmount;

                    targetBobPos += new Vector3(totalTranslateX, totalTranslateY, 0f);
                }
                else
                {
                    bobTimer = 0f;
                }
            }

            fpsHolder.localPosition = Vector3.Lerp(fpsHolder.localPosition, targetBobPos, Time.deltaTime * 8f);
        }

        public void FindOrCreateSocketAndModels()
        {
            // 1. Full-body hand socket
            if (handSocket == null)
            {
                Transform girl = transform.Find("civilian_girl");
                if (girl != null)
                {
                    Transform handR = FindBoneRecursive(girl, "hand_r");
                    if (handR != null)
                    {
                        Transform found = handR.Find("RightHand_ItemSocket");
                        if (found == null)
                        {
                            GameObject socketGo = new GameObject("RightHand_ItemSocket");
                            socketGo.transform.SetParent(handR, false);
                            socketGo.transform.localPosition = new Vector3(0.08f, 0.04f, -0.02f);
                            socketGo.transform.localRotation = Quaternion.Euler(0f, 90f, -90f);
                            handSocket = socketGo.transform;
                        }
                        else
                        {
                            handSocket = found;
                        }
                    }
                }
            }

            if (handSocket != null)
            {
                if (handFlashlight == null)
                {
                    Transform t = handSocket.Find("Hand_Flashlight");
                    if (t != null) handFlashlight = t.gameObject;
                }
                if (handFlashlight != null && inHandSpotlight == null)
                {
                    inHandSpotlight = handFlashlight.GetComponentInChildren<Light>();
                }
                if (handAxe == null)
                {
                    Transform t = handSocket.Find("Hand_Axe");
                    if (t != null) handAxe = t.gameObject;
                }
                if (handWoodPlank == null)
                {
                    Transform t = handSocket.Find("Hand_WoodPlank");
                    if (t != null) handWoodPlank = t.gameObject;
                }
                if (handToolbox == null)
                {
                    Transform t = handSocket.Find("Hand_Toolbox");
                    if (t != null) handToolbox = t.gameObject;
                }
                if (handKey == null)
                {
                    Transform t = handSocket.Find("Hand_Key");
                    if (t != null) handKey = t.gameObject;
                }
                if (handTalisman == null)
                {
                    Transform t = handSocket.Find("Hand_Talisman");
                    if (t != null) handTalisman = t.gameObject;
                }
            }

            // 2. FPS Viewmodels under Camera
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                if (fpsFlashlight == null)
                {
                    Transform f = cam.transform.Find("Flashlight_Model");
                    if (f != null) fpsFlashlight = f.gameObject;
                }

                if (fpsHolder == null)
                {
                    Transform h = cam.transform.Find("FPS_InHand_Holder");
                    if (h != null) fpsHolder = h;
                }

                if (fpsHolder != null)
                {
                    if (fpsAxe == null)
                    {
                        Transform t = fpsHolder.Find("FPS_Axe");
                        if (t != null) fpsAxe = t.gameObject;
                    }
                    if (fpsWoodPlank == null)
                    {
                        Transform t = fpsHolder.Find("FPS_WoodPlank");
                        if (t != null) fpsWoodPlank = t.gameObject;
                    }
                    if (fpsToolbox == null)
                    {
                        Transform t = fpsHolder.Find("FPS_Toolbox");
                        if (t != null) fpsToolbox = t.gameObject;
                    }
                    if (fpsKey == null)
                    {
                        Transform t = fpsHolder.Find("FPS_Key");
                        if (t != null) fpsKey = t.gameObject;
                    }
                    if (fpsTalisman == null)
                    {
                        Transform t = fpsHolder.Find("FPS_Talisman");
                        if (t != null) fpsTalisman = t.gameObject;
                    }
                }
            }
        }

        private Transform FindBoneRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform c in parent)
            {
                Transform r = FindBoneRecursive(c, name);
                if (r != null) return r;
            }
            return null;
        }

        public void EquipItem(string itemId)
        {
            currentEquippedId = string.IsNullOrEmpty(itemId) ? "" : itemId.ToLower();

            bool isFlash = currentEquippedId == "flashlight";
            bool isAxe = currentEquippedId == "axe";
            bool isPlank = currentEquippedId.Contains("plank") || currentEquippedId.Contains("wood");
            bool isToolbox = currentEquippedId == "toolbox";
            bool isKey = currentEquippedId.Contains("key");
            bool isTalisman = currentEquippedId.Contains("talisman");

            // 1. Cập nhật mô hình trên xương bàn tay (nhìn xuống chân và đổ bóng)
            if (handFlashlight != null) handFlashlight.SetActive(isFlash);
            if (handAxe != null) handAxe.SetActive(isAxe);
            if (handWoodPlank != null) handWoodPlank.SetActive(isPlank);
            if (handToolbox != null) handToolbox.SetActive(isToolbox);
            if (handKey != null) handKey.SetActive(isKey);
            if (handTalisman != null) handTalisman.SetActive(isTalisman);

            // 2. Cập nhật mô hình góc nhìn thứ nhất (FPS Viewmodel ngay trước màn hình)
            if (fpsFlashlight != null) fpsFlashlight.SetActive(isFlash);
            if (fpsAxe != null) fpsAxe.SetActive(isAxe);
            if (fpsWoodPlank != null) fpsWoodPlank.SetActive(isPlank);
            if (fpsToolbox != null) fpsToolbox.SetActive(isToolbox);
            if (fpsKey != null) fpsKey.SetActive(isKey);
            if (fpsTalisman != null) fpsTalisman.SetActive(isTalisman);

            // 3. Cập nhật Animator
            if (animator != null)
            {
                animator.SetBool("HoldingFlashlight", isFlash || isKey || isTalisman);
                animator.SetBool("HoldingAxe", isAxe);
                if (isToolbox) animator.SetBool("IsCarrying", true);
            }
        }

        public void SetFlashlightLight(bool isOn)
        {
            if (inHandSpotlight != null)
            {
                inHandSpotlight.enabled = isOn && (currentEquippedId == "flashlight");
            }
        }

        public void PlayPickupAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger("PickUp");
            }

            if (pickupRoutine != null) StopCoroutine(pickupRoutine);
            pickupRoutine = StartCoroutine(RoutineFpsPickupDraw());
        }

        private IEnumerator RoutineFpsPickupDraw()
        {
            if (fpsHolder == null) yield break;

            Vector3 startPos = holderInitialPos + Vector3.down * 0.22f;
            float elapsed = 0f;
            float duration = 0.32f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                fpsHolder.localPosition = Vector3.Lerp(startPos, holderInitialPos, t);
                yield return null;
            }

            fpsHolder.localPosition = holderInitialPos;
        }

        public void PlayAttackAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            if (fpsAxe != null && currentEquippedId == "axe")
            {
                if (axeSwingRoutine != null) StopCoroutine(axeSwingRoutine);
                axeSwingRoutine = StartCoroutine(RoutineFpsAxeSwing());
            }
        }

        private IEnumerator RoutineFpsAxeSwing()
        {
            if (fpsAxe == null) yield break;

            Vector3 basePos = new Vector3(0.24f, -0.20f, 0.44f);
            Quaternion baseRot = Quaternion.Euler(12f, 35f, -12f);

            // 1. Giơ rìu lấy đà (Windup)
            Quaternion windupRot = Quaternion.Euler(-25f, 50f, -20f);
            Vector3 windupPos = basePos + new Vector3(0.04f, 0.08f, -0.06f);
            float t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                float f = t / 0.12f;
                fpsAxe.transform.localPosition = Vector3.Lerp(basePos, windupPos, f);
                fpsAxe.transform.localRotation = Quaternion.Slerp(baseRot, windupRot, f);
                yield return null;
            }

            // 2. Chém bổ mạnh về phía trước (Chop)
            Quaternion strikeRot = Quaternion.Euler(65f, -20f, 30f);
            Vector3 strikePos = basePos + new Vector3(-0.12f, -0.15f, 0.15f);
            t = 0f;
            while (t < 0.10f)
            {
                t += Time.deltaTime;
                float f = t / 0.10f;
                fpsAxe.transform.localPosition = Vector3.Lerp(windupPos, strikePos, f);
                fpsAxe.transform.localRotation = Quaternion.Slerp(windupRot, strikeRot, f);
                yield return null;
            }

            // 3. Thu rìu về vị trí cầm chuẩn bị (Recover)
            t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float f = t / 0.25f;
                fpsAxe.transform.localPosition = Vector3.Lerp(strikePos, basePos, f);
                fpsAxe.transform.localRotation = Quaternion.Slerp(strikeRot, baseRot, f);
                yield return null;
            }

            fpsAxe.transform.localPosition = basePos;
            fpsAxe.transform.localRotation = baseRot;
        }

        public void SetCarrying(bool isCarrying)
        {
            if (animator != null)
            {
                animator.SetBool("IsCarrying", isCarrying);
            }
        }

        public void SetExhausted(bool isExhausted)
        {
            if (animator != null)
            {
                animator.SetBool("IsExhausted", isExhausted);
            }
        }
    }
}
