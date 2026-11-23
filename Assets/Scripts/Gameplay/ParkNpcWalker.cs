using UnityEngine;

namespace EcoSort.Gameplay
{
    /// <summary>
    /// Guides a friendly low-poly park NPC along the outer park footpath loop,
    /// always keeping a comfortable safe distance away from the player's sorting area.
    /// Drives the Blender-baked Walk animation clip via Animator.
    /// </summary>
    public class ParkNpcWalker : MonoBehaviour
    {
        [Header("Park Path Loop")]
        [SerializeField] private Vector3 pathCenter = new Vector3(0f, 0f, 0.40f);
        [SerializeField] private float orbitRadius = 4.15f;
        [SerializeField] private bool clockwise = true;
        [SerializeField] private float startAngleDegrees = 0f;
        [SerializeField] private float groundY = 0f;

        [Header("Movement & Comfort")]
        [SerializeField] private float walkSpeed = 0.85f;
        [SerializeField] private float turnSmoothSpeed = 6f;
        [SerializeField] private float minSafeDistanceFromOrigin = 3.60f;
        [SerializeField] private float walkAnimSpeed = 1.05f;

        [Header("Ambient Stroll Pauses")]
        [SerializeField] private bool allowScenicPauses = true;
        [SerializeField] private float minWalkSeconds = 12f;
        [SerializeField] private float maxWalkSeconds = 22f;
        [SerializeField] private float pauseSeconds = 2.2f;

        [Header("Animation")]
        [SerializeField] private Animator npcAnimator;

        private float _currentAngleRad;
        private float _stateTimer;
        private bool _isPaused;
        private Vector3 _pauseLookDir;

        public void Configure(
            Vector3 center,
            float radius,
            bool isClockwise,
            float startDeg,
            float speed)
        {
            pathCenter = center;
            orbitRadius = Mathf.Max(radius, minSafeDistanceFromOrigin);
            clockwise = isClockwise;
            startAngleDegrees = startDeg;
            walkSpeed = speed;
            ApplyPositionForAngle(Mathf.Deg2Rad * startAngleDegrees, true);
        }

        private void Awake()
        {
            if (npcAnimator == null)
                npcAnimator = GetComponentInChildren<Animator>();

            _currentAngleRad = Mathf.Deg2Rad * startAngleDegrees;
            _stateTimer = Random.Range(minWalkSeconds * 0.5f, maxWalkSeconds);
            _isPaused = false;
            ApplyPositionForAngle(_currentAngleRad, true);
        }

        private void OnEnable()
        {
            if (npcAnimator == null)
                npcAnimator = GetComponentInChildren<Animator>();

            if (npcAnimator != null)
            {
                npcAnimator.enabled = true;
                npcAnimator.speed = walkAnimSpeed;
            }
        }

        private void Update()
        {
            if (allowScenicPauses)
            {
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    _isPaused = !_isPaused;
                    if (_isPaused)
                    {
                        _stateTimer = pauseSeconds;
                        // Gently look outward toward the park trees/benches during brief pause
                        Vector3 outward = (transform.position - pathCenter);
                        outward.y = 0f;
                        _pauseLookDir = outward.sqrMagnitude > 0.001f ? outward.normalized : transform.forward;
                    }
                    else
                    {
                        _stateTimer = Random.Range(minWalkSeconds, maxWalkSeconds);
                    }
                }
            }

            if (_isPaused)
            {
                if (npcAnimator != null)
                    npcAnimator.speed = Mathf.MoveTowards(npcAnimator.speed, 0.0f, Time.deltaTime * 3.5f);

                if (_pauseLookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(_pauseLookDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 2.5f);
                }
                return;
            }

            if (npcAnimator != null)
                npcAnimator.speed = Mathf.MoveTowards(npcAnimator.speed, walkAnimSpeed, Time.deltaTime * 3.5f);

            float effectiveRadius = Mathf.Max(orbitRadius, minSafeDistanceFromOrigin);
            float angularSpeed = walkSpeed / Mathf.Max(0.5f, effectiveRadius);
            float dir = clockwise ? 1f : -1f;
            _currentAngleRad += dir * angularSpeed * Time.deltaTime;

            ApplyPositionForAngle(_currentAngleRad, false);
        }

        private void ApplyPositionForAngle(float angleRad, bool snapRotation)
        {
            float effectiveRadius = Mathf.Max(orbitRadius, minSafeDistanceFromOrigin);

            // Angle 0 at +Z: x = cx + r*sin(a), z = cz + r*cos(a)
            float sinA = Mathf.Sin(angleRad);
            float cosA = Mathf.Cos(angleRad);

            Vector3 pos = new Vector3(
                pathCenter.x + effectiveRadius * sinA,
                groundY,
                pathCenter.z + effectiveRadius * cosA
            );

            // Strict safety guard: never approach closer than minSafeDistanceFromOrigin to player (0, 0, 0)
            Vector2 horiz = new Vector2(pos.x, pos.z);
            if (horiz.magnitude < minSafeDistanceFromOrigin)
            {
                horiz = horiz.normalized * minSafeDistanceFromOrigin;
                pos.x = horiz.x;
                pos.z = horiz.y;
            }

            transform.position = pos;

            // Tangent direction along the ring
            float dir = clockwise ? 1f : -1f;
            Vector3 tangent = new Vector3(dir * cosA, 0f, -dir * sinA).normalized;
            if (tangent.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(tangent, Vector3.up);
                transform.rotation = snapRotation
                    ? targetRot
                    : Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSmoothSpeed);
            }
        }
    }
}
