using UnityEngine;
using HorrorGame.Story;

namespace HorrorGame.Story
{
    /// <summary>
    /// Điểm đánh dấu nhiệm vụ (hổ phách / vàng dịu) hướng dẫn người chơi khi cần thiết.
    /// Mặc định ẨN khi khởi động game, TUYỆT ĐỐI không sáng trong menu hoặc cutscene.
    /// Chỉ sáng lên đúng vật phẩm theo từng giai đoạn nhiệm vụ cụ thể.
    /// </summary>
    public class QuestWaypointMarker : MonoBehaviour
    {
        [Header("Marker Settings")]
        public Vector3 offset = new Vector3(0f, 0.45f, 0f);
        public float baseScale = 0.28f;
        public Color markerColor = new Color(1.0f, 0.82f, 0.12f, 0.90f);
        public float maxVisibleDistance = 14f;
        public bool requireLineOfSight = true;

        [Header("Animation")]
        public bool enableBobbing = true;
        public float bobSpeed = 3.0f;
        public float bobHeight = 0.04f;
        public bool enablePulse = true;
        public float pulseSpeed = 2.0f;

        [Header("Distance Display")]
        public bool showDistance = true;
        public string customLabel = "";
        public float textOffsetY = -0.28f;

        private GameObject markerVisual;
        private SpriteRenderer spriteRenderer;
        private TextMesh distanceTextMesh;
        private TextMesh shadowTextMesh;
        private Transform camTransform;
        private Vector3 initialOffset;
        private bool isExplicitlyVisible = false;

        private void Awake()
        {
            initialOffset = offset;
            CreateVisual();
        }

        private void Start()
        {
            FindCamera();
            // Mặc định luôn ẩn khi vừa load scene
            SetVisible(false);
        }

        private void FindCamera()
        {
            if (Camera.main != null) camTransform = Camera.main.transform;
            else
            {
                var cam = FindAnyObjectByType<Camera>();
                if (cam != null) camTransform = cam.transform;
            }
        }

        private void CreateVisual()
        {
            if (markerVisual != null) return;

            markerVisual = new GameObject("Waypoint_Visual");
            markerVisual.transform.SetParent(transform, false);
            markerVisual.transform.localPosition = initialOffset;
            markerVisual.transform.localScale = Vector3.one * baseScale;

            spriteRenderer = markerVisual.AddComponent<SpriteRenderer>();

            Sprite dotSprite = null;
#if UNITY_EDITOR
            dotSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Icon_Quest_Yellow_Dot.png");
#endif
            if (dotSprite != null)
            {
                spriteRenderer.sprite = dotSprite;
            }

            spriteRenderer.color = markerColor;
            spriteRenderer.sortingOrder = 50;

            CreateDistanceText();

            // Mặc định tắt ngay từ Awake
            markerVisual.SetActive(false);
        }

        private void CreateDistanceText()
        {
            Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (safeFont == null) safeFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 26);
            if (safeFont == null) safeFont = Font.CreateDynamicFontFromOSFont("Arial", 26);

            // Shadow text
            GameObject shadowObj = new GameObject("Distance_Shadow");
            shadowObj.transform.SetParent(markerVisual.transform, false);
            shadowObj.transform.localPosition = new Vector3(0.02f, textOffsetY - 0.02f, 0.01f);

            shadowTextMesh = shadowObj.AddComponent<TextMesh>();
            shadowTextMesh.font = safeFont;
            shadowTextMesh.fontSize = 24;
            shadowTextMesh.characterSize = 0.04f;
            shadowTextMesh.alignment = TextAlignment.Center;
            shadowTextMesh.anchor = TextAnchor.MiddleCenter;
            shadowTextMesh.color = new Color(0f, 0f, 0f, 0.85f);
            shadowTextMesh.fontStyle = FontStyle.Bold;

            var shadowMr = shadowObj.GetComponent<MeshRenderer>();
            if (shadowMr != null) shadowMr.sortingOrder = 51;

            // Main text
            GameObject textObj = new GameObject("Distance_Text");
            textObj.transform.SetParent(markerVisual.transform, false);
            textObj.transform.localPosition = new Vector3(0f, textOffsetY, 0f);

            distanceTextMesh = textObj.AddComponent<TextMesh>();
            distanceTextMesh.font = safeFont;
            distanceTextMesh.fontSize = 24;
            distanceTextMesh.characterSize = 0.04f;
            distanceTextMesh.alignment = TextAlignment.Center;
            distanceTextMesh.anchor = TextAnchor.MiddleCenter;
            distanceTextMesh.color = new Color(1.0f, 0.94f, 0.65f, 1.0f);
            distanceTextMesh.fontStyle = FontStyle.Bold;

            var mr = textObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 52;
        }

        private void LateUpdate()
        {
            // 1. Tuyệt đối ẩn khi đang ở menu Di chúc hoặc đang chạy Intro
            if (TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen)
            {
                if (markerVisual != null && markerVisual.activeSelf) markerVisual.SetActive(false);
                return;
            }

            if (CinematicIntroManager.Instance != null && CinematicIntroManager.Instance.IsPlayingIntro)
            {
                if (markerVisual != null && markerVisual.activeSelf) markerVisual.SetActive(false);
                return;
            }

            if (!isExplicitlyVisible)
            {
                if (markerVisual != null && markerVisual.activeSelf) markerVisual.SetActive(false);
                return;
            }

            if (camTransform == null)
            {
                FindCamera();
                if (camTransform == null) return;
            }

            float dist = Vector3.Distance(camTransform.position, transform.position);

            // Quá xa so với tầm hiển thị thì ẩn
            if (dist > maxVisibleDistance)
            {
                if (markerVisual.activeSelf) markerVisual.SetActive(false);
                return;
            }

            // Kiểm tra vật cản tầm nhìn (tường, cửa đóng) khi ở cự ly > 3.5m để không hiện xuyên tường vô lý
            if (requireLineOfSight && dist > 3.5f)
            {
                Vector3 rayOrigin = camTransform.position;
                Vector3 targetPos = transform.position + initialOffset;
                Vector3 dir = targetPos - rayOrigin;
                if (Physics.Raycast(rayOrigin, dir.normalized, out RaycastHit hit, dist - 0.25f))
                {
                    if (hit.transform != transform && !hit.transform.IsChildOf(transform))
                    {
                        if (markerVisual.activeSelf) markerVisual.SetActive(false);
                        return;
                    }
                }
            }

            if (!markerVisual.activeSelf) markerVisual.SetActive(true);

            // Cập nhật số mét khoảng cách
            if (showDistance && distanceTextMesh != null)
            {
                string distStr;
                if (dist < 1.2f)
                {
                    distStr = string.IsNullOrEmpty(customLabel) ? "< 1m" : $"{customLabel}\n< 1m";
                }
                else
                {
                    distStr = string.IsNullOrEmpty(customLabel) ? $"{Mathf.RoundToInt(dist)}m" : $"{customLabel}\n{Mathf.RoundToInt(dist)}m";
                }

                distanceTextMesh.text = distStr;
                if (shadowTextMesh != null)
                {
                    shadowTextMesh.text = distStr;
                }
            }

            // Billboard xoay về camera
            markerVisual.transform.LookAt(markerVisual.transform.position + camTransform.rotation * Vector3.forward, camTransform.rotation * Vector3.up);

            // Bồng bềnh nhẹ
            float bob = enableBobbing ? Mathf.Sin(Time.time * bobSpeed) * bobHeight : 0f;
            markerVisual.transform.localPosition = initialOffset + Vector3.up * bob;

            // Co giãn theo cự ly
            float distanceScale = Mathf.Clamp(dist * 0.12f, 0.7f, 1.8f);

            if (enablePulse)
            {
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.08f;
                markerVisual.transform.localScale = Vector3.one * (baseScale * distanceScale * pulse);

                float alpha = Mathf.Lerp(0.7f, 0.95f, Mathf.PingPong(Time.time * pulseSpeed, 1f));
                Color c = markerColor;
                c.a = alpha;
                if (spriteRenderer != null) spriteRenderer.color = c;
            }
            else
            {
                markerVisual.transform.localScale = Vector3.one * (baseScale * distanceScale);
            }
        }

        public void SetVisible(bool visible)
        {
            isExplicitlyVisible = visible;
            if (markerVisual != null)
            {
                markerVisual.SetActive(visible);
            }
        }
    }
}
