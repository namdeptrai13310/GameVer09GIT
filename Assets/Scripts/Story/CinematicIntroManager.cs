using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace HorrorGame.Story
{
    public class CinematicIntroManager : MonoBehaviour
    {
        public static CinematicIntroManager Instance { get; private set; }

        [Header("Cinematic Camera")]
        public Camera cinematicCamera;
        public float introDuration = 15.0f;

        [Header("Letterbox Bars")]
        public RectTransform topBar;
        public RectTransform bottomBar;

        [Header("Dialogue / Subtitle UI")]
        public CanvasGroup subtitleCanvasGroup;
        public Text speakerNameText;
        public Text subtitleText;
        public GameObject skipHintText;

        [Header("Story Dialogue Lines")]
        [TextArea(2, 4)]
        public string line1 = "Căn biệt thự này... to quá...";
        [TextArea(2, 4)]
        public string line2 = "Nơi đây thật sự là tài sản cha để lại cho mình sao? Trông hoang tàn và u ám quá...";
        [TextArea(2, 4)]
        public string line3 = "Phải vào trong xem thử tình hình thế nào đã.";

        [Header("References")]
        public Transform playerTransform;
        public Transform playerCameraTransform;
        public PlayerController playerController;

        [Header("Car Driving Intro Setup (Chạy trên mặt đường Pavement)")]
        public Transform carTransform;

        // Spline Waypoints on the actual road in the map:
        // Dirt Road from the East (z ≈ -16.5, Y = 10.04 ground contact) -> Cruising West -> Pulling up against curb
        private static readonly Vector3[] roadSpline = new Vector3[]
        {
            new Vector3(115.0f, 10.00f, -16.5f),  // Dirt road from East
            new Vector3(98.0f, 10.00f, -16.5f),   // Cruising West along open road
            new Vector3(86.0f, 10.00f, -16.2f),   // Passing near estate approach
            new Vector3(78.0f, 10.00f, -15.5f),   // Easing over toward curb
            new Vector3(71.24f, 10.00f, -14.6f)   // Parked cleanly along curb in front of Villa 1
        };

        public Vector3 carParkedPosition = new Vector3(71.24f, 10.00f, -14.6f);
        public Quaternion carParkedRotation = Quaternion.Euler(0f, 180f, 0f);

        [Header("Outside Player Spawn Point")]
        public Vector3 outsideSpawnPosition = new Vector3(71.8f, 10.04f, -16.5f);
        public Quaternion outsideSpawnRotation = Quaternion.Euler(0f, 20f, 0f);

        [Header("Car Headlights")]
        public Light leftHeadlight;
        public Light rightHeadlight;

        [Header("Audio")]
        public AudioSource dialogueAudioSource;
        public AudioSource carAudioSource;
        public AudioClip sfxCarEngine;
        public AudioClip sfxCarBrake;
        public AudioClip sfxCarDoor;
        public AudioClip introWhooshSFX;

        public bool isPlayingIntro = false;
        public bool IsPlayingIntro => isPlayingIntro;
        private Action onIntroCompleteCallback;
        private Coroutine introCoroutine;

        // Cinematic Flight Waypoints
        private struct CameraWaypoint
        {
            public Vector3 position;
            public Quaternion rotation;
            public float time; // normalized [0..1]
        }

        private List<CameraWaypoint> waypoints = new List<CameraWaypoint>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Parked rotation: front of truck (+X) pointing West along curb
            carParkedRotation = Quaternion.Euler(0f, 180f, 0f);

            SetupCinematicCamera();
            FindReferences();
            SetupCarAndHeadlights();
            SetupAudio();
        }

        private void Start()
        {
            // Position player outside immediately
            PositionPlayerAtOutsideSpawn();
            if (isPlayingIntro)
            {
                SetPlayerVisualVisible(false);
            }
            else
            {
                SetPlayerVisualVisible(true);
            }
        }

        public void SetPlayerVisualVisible(bool visible)
        {
            if (playerTransform == null)
            {
                var p = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform != null)
            {
                if (!playerTransform.gameObject.activeSelf)
                    playerTransform.gameObject.SetActive(true);

                Transform model = playerTransform.Find("civilian_girl");
                if (model != null)
                {
                    model.gameObject.SetActive(visible);
                }
                else
                {
                    Renderer[] rends = playerTransform.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in rends) r.enabled = visible;
                }
            }
        }

        public void PositionPlayerAtOutsideSpawn()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform != null)
            {
                if (!playerTransform.gameObject.activeSelf)
                    playerTransform.gameObject.SetActive(true);

                CharacterController cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                playerTransform.position = outsideSpawnPosition;
                playerTransform.rotation = outsideSpawnRotation;

                if (cc != null) cc.enabled = true;
            }
        }

        private void FindReferences()
        {
            if (playerTransform == null)
            {
                var p = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform != null)
            {
                if (playerController == null)
                    playerController = playerTransform.GetComponent<PlayerController>();

                if (playerCameraTransform == null)
                {
                    Camera cam = playerTransform.GetComponentInChildren<Camera>();
                    if (cam != null) playerCameraTransform = cam.transform;
                }
            }

            if (carTransform == null)
            {
                var carObj = GameObject.Find("Prop_Car_A");
                if (carObj != null) carTransform = carObj.transform;
            }

            // Position toolbox on car rear bed
            var tb = GameObject.Find("Toolbox_Pickup");
            if (tb == null)
            {
                var allObjs = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var o in allObjs) { if (o.name == "Toolbox_Pickup") { tb = o; break; } }
            }
            if (tb != null && carTransform != null)
            {
                tb.SetActive(true);
                tb.transform.SetParent(carTransform, false);
                tb.transform.localPosition = new Vector3(-1.8f, 1.68f, 0f);
                tb.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                tb.transform.localScale = new Vector3(1.0f, 0.6f, 0.7f);
            }

            var axe = GameObject.Find("Axe_Pickup");
            if (axe == null)
            {
                var allObjs = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var o in allObjs) { if (o.name == "Axe_Pickup") { axe = o; break; } }
            }
            if (axe != null && carTransform != null)
            {
                axe.SetActive(true);
                axe.transform.SetParent(carTransform, false);
                axe.transform.localPosition = new Vector3(-1.8f, 1.70f, -0.4f);
                axe.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                axe.transform.localScale = Vector3.one * 0.9f;
            }
        }

        private void SetupCarAndHeadlights()
        {
            if (carTransform == null) return;

            // In Prop_Car_A local space: Front of car is along +X, rear is -X
            if (leftHeadlight == null)
            {
                Transform existingLeft = carTransform.Find("Car_Headlight_Left");
                if (existingLeft != null) leftHeadlight = existingLeft.GetComponent<Light>();
                else
                {
                    GameObject lObj = new GameObject("Car_Headlight_Left");
                    lObj.transform.SetParent(carTransform, false);
                    lObj.transform.localPosition = new Vector3(2.6f, 1.05f, -0.65f);
                    lObj.transform.localRotation = Quaternion.Euler(4f, 90f, 0f); // Shines along +X (front)

                    leftHeadlight = lObj.AddComponent<Light>();
                    leftHeadlight.type = LightType.Spot;
                    leftHeadlight.range = 35f;
                    leftHeadlight.spotAngle = 55f;
                    leftHeadlight.intensity = 2.5f;
                    leftHeadlight.color = new Color(1.0f, 0.94f, 0.82f);
                    leftHeadlight.shadows = LightShadows.Soft;
                }
            }
            else
            {
                leftHeadlight.transform.localPosition = new Vector3(2.6f, 1.05f, -0.65f);
                leftHeadlight.transform.localRotation = Quaternion.Euler(4f, 90f, 0f);
            }

            if (rightHeadlight == null)
            {
                Transform existingRight = carTransform.Find("Car_Headlight_Right");
                if (existingRight != null) rightHeadlight = existingRight.GetComponent<Light>();
                else
                {
                    GameObject rObj = new GameObject("Car_Headlight_Right");
                    rObj.transform.SetParent(carTransform, false);
                    rObj.transform.localPosition = new Vector3(2.6f, 1.05f, 0.65f);
                    rObj.transform.localRotation = Quaternion.Euler(4f, 90f, 0f);

                    rightHeadlight = rObj.AddComponent<Light>();
                    rightHeadlight.type = LightType.Spot;
                    rightHeadlight.range = 35f;
                    rightHeadlight.spotAngle = 55f;
                    rightHeadlight.intensity = 2.5f;
                    rightHeadlight.color = new Color(1.0f, 0.94f, 0.82f);
                    rightHeadlight.shadows = LightShadows.Soft;
                }
            }
            else
            {
                rightHeadlight.transform.localPosition = new Vector3(2.6f, 1.05f, 0.65f);
                rightHeadlight.transform.localRotation = Quaternion.Euler(4f, 90f, 0f);
            }

            if (leftHeadlight != null) leftHeadlight.enabled = true;
            if (rightHeadlight != null) rightHeadlight.enabled = true;
        }

        private void SetupAudio()
        {
            if (carAudioSource == null)
            {
                GameObject aObj = new GameObject("Intro_CarAudioSource");
                aObj.transform.SetParent(transform, false);
                carAudioSource = aObj.AddComponent<AudioSource>();
                carAudioSource.playOnAwake = false;
                carAudioSource.spatialBlend = 0f;
            }

            if (dialogueAudioSource == null)
            {
                GameObject dObj = new GameObject("Intro_DialogueAudioSource");
                dObj.transform.SetParent(transform, false);
                dialogueAudioSource = dObj.AddComponent<AudioSource>();
                dialogueAudioSource.playOnAwake = false;
                dialogueAudioSource.spatialBlend = 0f;
            }

            if (sfxCarEngine == null)
                sfxCarEngine = Resources.Load<AudioClip>("SFX_Car_Engine_Drive");
            if (sfxCarBrake == null)
                sfxCarBrake = Resources.Load<AudioClip>("SFX_Car_Brake_Stop");
            if (sfxCarDoor == null)
                sfxCarDoor = Resources.Load<AudioClip>("SFX_Car_Door_Shut");
        }

        private void SetupCinematicCamera()
        {
            if (cinematicCamera == null)
            {
                GameObject camObj = GameObject.Find("Intro_CinematicCamera");
                if (camObj == null)
                {
                    camObj = new GameObject("Intro_CinematicCamera");
                    camObj.transform.SetParent(transform, false);
                }
                cinematicCamera = camObj.GetComponent<Camera>();
                if (cinematicCamera == null) cinematicCamera = camObj.AddComponent<Camera>();
                cinematicCamera.nearClipPlane = 0.05f;
                cinematicCamera.farClipPlane = 400f;
                cinematicCamera.fieldOfView = 65f;
                if (camObj.GetComponent<AudioListener>() == null)
                    camObj.AddComponent<AudioListener>();
                cinematicCamera.gameObject.SetActive(false);
            }
        }

        private void SetupWaypoints()
        {
            waypoints.Clear();

            // Waypoint 0 (0.00): High road establishing shot - viewing the truck cruising West down the dirt road
            waypoints.Add(new CameraWaypoint
            {
                position = new Vector3(122.0f, 12.8f, -20.5f),
                rotation = Quaternion.Euler(14f, -70f, 0f),
                time = 0.0f
            });

            // Waypoint 1 (0.28): Dynamic tracking shot following the truck as it cruises towards the estate
            waypoints.Add(new CameraWaypoint
            {
                position = new Vector3(88.0f, 11.5f, -20.0f),
                rotation = Quaternion.Euler(10f, -42f, 0f),
                time = 0.28f
            });

            // Waypoint 2 (0.48): Low angle curb camera watching the truck brake and pull up to park
            waypoints.Add(new CameraWaypoint
            {
                position = new Vector3(64.5f, 11.2f, -17.5f),
                rotation = Quaternion.Euler(5f, 65f, 0f),
                time = 0.48f
            });

            // Waypoint 3 (0.64): Wide full-body shot showing the whole character An stepping out beside the truck
            waypoints.Add(new CameraWaypoint
            {
                position = new Vector3(67.5f, 11.2f, -19.5f),
                rotation = Quaternion.Euler(2.1f, 50.9f, 0f),
                time = 0.64f
            });

            // Waypoint 4 (0.82): Sweeping crane view tilting up to showcase the grand front facade of Villa 1
            waypoints.Add(new CameraWaypoint
            {
                position = new Vector3(79.5f, 13.8f, -16.0f),
                rotation = Quaternion.Euler(-10.1f, 6.1f, 0f), // Tilting up towards mansion front facade & roof
                time = 0.82f
            });

            // Waypoint 5 (1.00): Seamless glide directly down into An's first-person eyes
            Vector3 camOffset = (playerController != null) ? playerController.firstPersonCamOffset : new Vector3(0f, 1.819f, 0.254f);
            Vector3 fpsEyePos = (playerCameraTransform != null) 
                ? playerCameraTransform.position 
                : (outsideSpawnPosition + outsideSpawnRotation * camOffset);
            Quaternion fpsRot = (playerCameraTransform != null) 
                ? playerCameraTransform.rotation 
                : outsideSpawnRotation;

            waypoints.Add(new CameraWaypoint
            {
                position = fpsEyePos,
                rotation = fpsRot,
                time = 1.0f
            });
        }

        private GameObject FindCanvasChild(string childName)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                Transform t = canvas.transform.Find(childName);
                if (t != null) return t.gameObject;
            }
            return GameObject.Find(childName);
        }

        public void SetHUDVisibility(bool visible)
        {
            string[] hudNames = new string[] { "StaminaUI", "Hotbar_QuickSlots", "CrosshairDot", "PubgDoorUI" };
            foreach (string name in hudNames)
            {
                GameObject go = FindCanvasChild(name);
                if (go != null) go.SetActive(visible);
            }
        }

        public void PlayIntroCutscene(Action onComplete)
        {
            onIntroCompleteCallback = onComplete;
            if (introCoroutine != null) StopCoroutine(introCoroutine);
            introCoroutine = StartCoroutine(RoutineIntroCutscene());
        }

        private IEnumerator RoutineIntroCutscene()
        {
            isPlayingIntro = true;
            SetHUDVisibility(false);
            FindReferences();
            PositionPlayerAtOutsideSpawn();

            // Hide the player character model during driving phase so she only appears when car stops
            SetPlayerVisualVisible(false);

            SetupWaypoints();

            // Disable player control & cursor
            if (playerController != null) playerController.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Place car at start of road spline
            if (carTransform != null)
            {
                carTransform.position = roadSpline[0];
                Vector3 startTangent = (roadSpline[1] - roadSpline[0]).normalized;
                // Prop_Car_A front is along +X (positive local X)
                carTransform.rotation = Quaternion.LookRotation(startTangent, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
            }

            // Activate Cinematic Camera
            if (cinematicCamera != null)
            {
                cinematicCamera.transform.position = waypoints[0].position;
                cinematicCamera.transform.rotation = waypoints[0].rotation;
                cinematicCamera.gameObject.SetActive(true);
            }

            // Disable player camera audio listener during intro
            AudioListener playerListener = playerCameraTransform != null ? playerCameraTransform.GetComponent<AudioListener>() : null;
            if (playerListener != null) playerListener.enabled = false;

            // Animate letterbox bars sliding in
            StartCoroutine(AnimateLetterbox(true, 1.0f));

            if (skipHintText != null) skipHintText.SetActive(true);

            // Subtitle state
            if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = 0f;

            string playerName = PlayerPrefs.GetString("PlayerName", "An");
            if (speakerNameText != null) speakerNameText.text = playerName;

            // Audio cues: Start engine drive
            if (carAudioSource != null && sfxCarEngine != null)
            {
                carAudioSource.clip = sfxCarEngine;
                carAudioSource.loop = true;
                carAudioSource.volume = 0.85f;
                carAudioSource.Play();
            }

            float elapsed = 0f;
            int currentLine = 0;
            bool brakePlayed = false;
            bool doorPlayed = false;

            while (elapsed < introDuration)
            {
                // Ensure gameplay HUD remains hidden throughout cinematic
                SetHUDVisibility(false);

                // Allow skipping with Space or Left Click or F
                if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame) ||
                    (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
                {
                    break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / introDuration);

                // Update Car along the road spline (t = 0 to 0.48 is drive phase)
                UpdateCarDrive(t);

                // Sound triggers
                if (t >= 0.40f && !brakePlayed)
                {
                    brakePlayed = true;
                    if (carAudioSource != null)
                    {
                        carAudioSource.Stop();
                        if (sfxCarBrake != null)
                            carAudioSource.PlayOneShot(sfxCarBrake, 0.9f);
                    }
                }

                if (t >= 0.60f && !doorPlayed)
                {
                    doorPlayed = true;
                    if (carAudioSource != null && sfxCarDoor != null)
                    {
                        carAudioSource.PlayOneShot(sfxCarDoor, 1.0f);
                    }

                    // Car has parked and door opened: Reveal player stepping out beside the truck
                    PositionPlayerAtOutsideSpawn();
                    SetPlayerVisualVisible(true);
                }

                // Keep player visual active once stepped out
                if (t >= 0.60f)
                {
                    SetPlayerVisualVisible(true);
                }

                // Update Camera Position & Rotation along waypoints
                UpdateCameraSpline(t);

                // Story dialogue lines timing (delivered once character steps out)
                if (t >= 0.68f && currentLine == 0)
                {
                    currentLine = 1;
                    StartCoroutine(DisplaySubtitleRoutine(line1, 3.0f));
                }
                else if (t >= 0.80f && currentLine == 1)
                {
                    currentLine = 2;
                    StartCoroutine(DisplaySubtitleRoutine(line2, 3.5f));
                }
                else if (t >= 0.92f && currentLine == 2)
                {
                    currentLine = 3;
                    StartCoroutine(DisplaySubtitleRoutine(line3, 2.2f));
                }

                yield return null;
            }

            // Ensure car and player are strictly in final parked & outside spawn positions
            SnapToFinalState();

            // Smooth handoff to player
            yield return StartCoroutine(EndCutsceneRoutine(playerListener));
        }

        private void UpdateCarDrive(float t)
        {
            if (carTransform == null) return;

            // Normalized car drive phase: 0.0 to 0.48
            float driveT = Mathf.Clamp01(t / 0.48f);
            float smoothT = Mathf.SmoothStep(0f, 1f, driveT);

            if (smoothT < 1.0f)
            {
                Vector3 currentPos = EvaluateSpline(roadSpline, smoothT);
                Vector3 nextPos = EvaluateSpline(roadSpline, Mathf.Min(smoothT + 0.015f, 1f));
                Vector3 forwardDir = (nextPos - currentPos).normalized;

                if (forwardDir.sqrMagnitude < 0.001f)
                    forwardDir = Vector3.left;

                // Prop_Car_A front is +Vector3.right (positive local X)
                Quaternion targetRot = Quaternion.LookRotation(forwardDir, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);

                carTransform.position = currentPos;
                carTransform.rotation = targetRot;

                // Subtle chassis suspension vibration while rolling on gravel
                float bounce = Mathf.Sin(t * 22f) * 0.01f * (1.0f - smoothT);
                carTransform.position += Vector3.up * bounce;
            }
            else
            {
                carTransform.position = carParkedPosition;
                carTransform.rotation = carParkedRotation;
            }
        }

        private Vector3 EvaluateSpline(Vector3[] pts, float t)
        {
            int count = pts.Length;
            if (count < 2) return pts[0];

            float scaledT = t * (count - 1);
            int i = Mathf.Clamp(Mathf.FloorToInt(scaledT), 0, count - 2);
            float localT = scaledT - i;

            Vector3 p0 = i > 0 ? pts[i - 1] : pts[0] + (pts[0] - pts[1]);
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = i < count - 2 ? pts[i + 2] : pts[count - 1] + (pts[count - 1] - pts[count - 2]);

            // Catmull-Rom formulation
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * localT +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * (localT * localT) +
                (-p0 + 3f * p1 - 3f * p2 + p3) * (localT * localT * localT)
            );
        }

        public void SnapToFinalState()
        {
            if (carAudioSource != null && carAudioSource.isPlaying)
            {
                carAudioSource.Stop();
            }

            if (carTransform != null)
            {
                carTransform.position = carParkedPosition;
                carTransform.rotation = carParkedRotation;
            }

            PositionPlayerAtOutsideSpawn();
            SetPlayerVisualVisible(true);

            SetHUDVisibility(true);
        }

        private void UpdateCameraSpline(float t)
        {
            if (cinematicCamera == null || waypoints.Count < 2) return;

            // Find segment
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (t >= waypoints[i].time && t <= waypoints[i + 1].time)
                {
                    float segT = (t - waypoints[i].time) / (waypoints[i + 1].time - waypoints[i].time);
                    float smoothT = Mathf.SmoothStep(0, 1, segT);

                    cinematicCamera.transform.position = Vector3.Lerp(waypoints[i].position, waypoints[i + 1].position, smoothT);
                    cinematicCamera.transform.rotation = Quaternion.Slerp(waypoints[i].rotation, waypoints[i + 1].rotation, smoothT);
                    break;
                }
            }
        }

        public IEnumerator DisplaySubtitleRoutine(string text, float duration)
        {
            if (subtitleText != null) subtitleText.text = text;
            if (subtitleCanvasGroup != null)
            {
                // Fade in
                float fElapsed = 0f;
                while (fElapsed < 0.35f)
                {
                    fElapsed += Time.deltaTime;
                    subtitleCanvasGroup.alpha = fElapsed / 0.35f;
                    yield return null;
                }
                subtitleCanvasGroup.alpha = 1f;

                yield return new WaitForSeconds(duration);

                // Fade out
                fElapsed = 0f;
                while (fElapsed < 0.4f)
                {
                    fElapsed += Time.deltaTime;
                    subtitleCanvasGroup.alpha = 1f - (fElapsed / 0.4f);
                    yield return null;
                }
                subtitleCanvasGroup.alpha = 0f;
            }
        }

        public IEnumerator AnimateLetterbox(bool show, float duration)
        {
            if (topBar == null || bottomBar == null) yield break;

            float targetHeight = show ? 72f : 0f;
            float initialTop = topBar.sizeDelta.y;
            float initialBot = bottomBar.sizeDelta.y;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, Mathf.Lerp(initialTop, targetHeight, t));
                bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, Mathf.Lerp(initialBot, targetHeight, t));
                yield return null;
            }

            topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, targetHeight);
            bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, targetHeight);
        }

        private IEnumerator EndCutsceneRoutine(AudioListener playerListener)
        {
            if (skipHintText != null) skipHintText.SetActive(false);
            if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = 0f;

            // Turn off cinematic camera and hand off immediately to player camera
            if (cinematicCamera != null)
            {
                cinematicCamera.gameObject.SetActive(false);
            }
            if (playerListener != null) playerListener.enabled = true;
            SetPlayerVisualVisible(true);

            // Animate letterbox bars retracting
            yield return StartCoroutine(AnimateLetterbox(false, 0.8f));

            isPlayingIntro = false;

            // Re-enable player controller
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // Restore HUD visibility
            SetHUDVisibility(true);

            // Trigger complete callback
            onIntroCompleteCallback?.Invoke();

            // Announce initial exploration objective
            if (VillaSurveyAndRepairQuest.Instance != null)
            {
                VillaSurveyAndRepairQuest.Instance.UpdateObjectiveDisplay();
            }
            else
            {
                StoryObjectiveBanner.ShowObjective("KHẢO SÁT BIỆT THỰ", "Bước vào bên trong căn biệt thự để kiểm tra tình trạng ngôi nhà.");
            }
        }
    }
}
