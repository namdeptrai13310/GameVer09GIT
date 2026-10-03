using UnityEngine;

namespace HorrorGame.Story
{
    /// <summary>
    /// Tạo hiệu ứng bánh xe xoay và làm nhòe chuyển động (Wheel Spin & Motion Blur) cho xe bán tải
    /// khi xe đang chạy trong đoạn cutscene mở đầu.
    /// </summary>
    public class CarWheelSpinner : MonoBehaviour
    {
        [Header("Wheel Wheelbase Offsets (Local to Prop_Car_A)")]
        public Vector3 frontLeftOffset = new Vector3(1.68f, 0.54f, 0.95f);
        public Vector3 frontRightOffset = new Vector3(1.68f, 0.54f, -1.05f);
        public Vector3 rearLeftOffset = new Vector3(-1.95f, 0.54f, 0.98f);
        public Vector3 rearRightOffset = new Vector3(-1.95f, 0.54f, -1.08f);

        [Header("Wheel Settings")]
        public float wheelRadius = 0.45f;
        public float wheelThickness = 0.28f;
        public float currentSpinSpeed = 0f; // degrees / sec
        public float maxSpinSpeed = 950f;

        [Header("Spinning Visuals")]
        public Transform[] wheelTransforms = new Transform[4];
        public MeshRenderer[] wheelBlurRenderers = new MeshRenderer[4];

        private Vector3 lastCarPosition;
        private bool isInitialized = false;

        private void Start()
        {
            SetupWheelVisuals();
            lastCarPosition = transform.position;
        }

        private void SetupWheelVisuals()
        {
            if (isInitialized) return;
            isInitialized = true;

            Vector3[] offsets = new Vector3[] { frontLeftOffset, frontRightOffset, rearLeftOffset, rearRightOffset };
            string[] wheelNames = new string[] { "Wheel_FL_Spin", "Wheel_FR_Spin", "Wheel_RL_Spin", "Wheel_RR_Spin" };

            // Tạo chất liệu đĩa căm xoay nhòe (Motion Blur Rim Material)
            Material blurMat = CreateWheelBlurMaterial();

            for (int i = 0; i < 4; i++)
            {
                Transform existing = transform.Find(wheelNames[i]);
                if (existing != null)
                {
                    wheelTransforms[i] = existing;
                    wheelBlurRenderers[i] = existing.GetComponent<MeshRenderer>();
                    continue;
                }

                // Tạo hình trụ đĩa bánh xe xoay nhòe
                GameObject wheelObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheelObj.name = wheelNames[i];
                wheelObj.transform.SetParent(transform, false);
                wheelObj.transform.localPosition = offsets[i];

                // Đĩa bánh xe: Trong xe bán tải, trục ngang là trục Z
                wheelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wheelObj.transform.localScale = new Vector3(wheelRadius * 2f, wheelThickness * 0.5f, wheelRadius * 2f);

                // Hủy collider để tránh cản trở vật lý
                var col = wheelObj.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var mr = wheelObj.GetComponent<MeshRenderer>();
                if (mr != null && blurMat != null)
                {
                    mr.sharedMaterial = blurMat;
                }

                wheelTransforms[i] = wheelObj.transform;
                wheelBlurRenderers[i] = mr;
            }
        }

        private Material CreateWheelBlurMaterial()
        {
            Shader standardShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(standardShader);
            mat.name = "Mat_Wheel_MotionBlur";
            mat.color = new Color(0.12f, 0.12f, 0.13f, 0.95f);

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.45f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.35f);

            return mat;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            // Tính vận tốc thực tế của xe
            float speed = (transform.position - lastCarPosition).magnitude / dt;
            lastCarPosition = transform.position;

            bool isDriving = (CinematicIntroManager.Instance != null && CinematicIntroManager.Instance.IsPlayingIntro && speed > 0.15f);

            float targetSpeed = isDriving ? (speed / (2f * Mathf.PI * wheelRadius) * 360f) : 0f;
            currentSpinSpeed = Mathf.Lerp(currentSpinSpeed, targetSpeed, dt * 10f);

            if (currentSpinSpeed > 5f)
            {
                // Xoay các bánh xe quanh trục ngang (trục Y của hình trụ đã được quay Euler 90 độ)
                float angleDelta = currentSpinSpeed * dt;
                for (int i = 0; i < wheelTransforms.Length; i++)
                {
                    if (wheelTransforms[i] != null)
                    {
                        wheelTransforms[i].gameObject.SetActive(true);
                        wheelTransforms[i].Rotate(0f, angleDelta, 0f, Space.Self);
                    }
                }
            }
            else
            {
                // Khi xe dừng hẳn, ẩn đĩa xoay mờ để lộ bánh xe tĩnh nguyên bản
                for (int i = 0; i < wheelTransforms.Length; i++)
                {
                    if (wheelTransforms[i] != null && wheelTransforms[i].gameObject.activeSelf)
                    {
                        wheelTransforms[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
