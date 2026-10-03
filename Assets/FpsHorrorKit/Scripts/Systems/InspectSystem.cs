namespace FpsHorrorKit
{
    using System.Collections;
    using UnityEngine;

    public class InspectSystem : MonoBehaviour, IInteractable
    {
        [Header("For Rotation")]
        [Tooltip("Object rotation speed")] public float rotationSpeed;
        [Tooltip("Speed for returning to the starting position")] public float returnSpeed = 5f;

        [Header("For Camera")]
        [Tooltip("Distance to the camera during inspection")] public float distanceToCamera;

        [Header("Higlight UI")]
        public string interactText = "Press [E] to Inspect";

        [Header("For Lantern")]
        public Light lanternLight;
        public float lightInspectIntensity;

        private float _startLightIntensity;

        private FpsAssetsInputs _input;
        private FpsController _fpsController;
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        private bool _isInteracting;
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _input = FindAnyObjectByType<FpsAssetsInputs>();
            _fpsController = FindAnyObjectByType<FpsController>();
        }

        private void Start()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;

            if (lanternLight != null)
                _startLightIntensity = lanternLight.intensity;
        }

        private void LateUpdate()
        {
            if (_isInteracting)
            {
                HandleInspection();
            }
        }

        public void Interact()
        {
            if (_fpsController == null || Camera.main == null) return;

            PlayerInteract.Instance.sendRaycast = false;

            _fpsController.isInteracting = true;
            Vector3 targetPosition = Camera.main.transform.position + Camera.main.transform.forward * distanceToCamera;

            StopAllCoroutines();
            StartCoroutine(SmoothTransition(targetPosition, _startRotation, disableCollider: false));

            ToggleCollider(false);
            ToggleLantern(false);
        }

        private void HandleInspection()
        {
            InteractCameraSettings.Instance?.Interacting(distanceToCamera);

            float rotationX = _input.look.x * rotationSpeed * Time.deltaTime;
            float rotationY = _input.look.y * rotationSpeed * Time.deltaTime;

            transform.Rotate(Camera.main.transform.up, -rotationX, Space.World);
            transform.Rotate(Camera.main.transform.right, -rotationY, Space.World);

            if (_input.stopInteract)
            {
                _input.stopInteract = false;
                StopInspection();
            }
        }

        private void StopInspection()
        {
            StopAllCoroutines();
            StartCoroutine(SmoothTransition(_startPosition, _startRotation, disableCollider: true));

            _isInteracting = false;
            _fpsController.isInteracting = false;

            InteractCameraSettings.Instance?.NotInteracting();

            ToggleLantern(true);

            PlayerInteract.Instance.sendRaycast = true;
        }

        private IEnumerator SmoothTransition(Vector3 targetPosition, Quaternion targetRotation, bool disableCollider)
        {
            float elapsed = 0f;
            float duration = 0.5f; // Tối đa 0.5s để tới đích

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, targetPosition, t);
                transform.rotation = Quaternion.Lerp(startRot, targetRotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPosition;
            transform.rotation = targetRotation;

            _isInteracting = !disableCollider;
            ToggleCollider(disableCollider);
        }

        private void ToggleCollider(bool isActive)
        {
            if (_collider != null)
            {
                _collider.enabled = isActive;
            }
            else
            {
                Debug.LogError("Collider not found!");
            }
        }

        private void ToggleLantern(bool isActive)
        {
            if (lanternLight != null)
            {
                lanternLight.intensity = isActive ? _startLightIntensity : lightInspectIntensity;
            }
        }

        public void Highlight()
        {
            PlayerInteract.Instance.ChangeInteractText(interactText);
        }

        public void HoldInteract() { }
        public void UnHighlight() { }
    }
}