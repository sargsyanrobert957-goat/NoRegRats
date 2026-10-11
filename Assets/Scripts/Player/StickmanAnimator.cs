using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Draws the player as a stick figure made of LineRenderers and animates it procedurally from
    /// the player's velocity: a walk cycle that blends into a run cycle as speed rises, a jump/fall
    /// pose in the air, and a slight breathing motion when standing still.
    /// Local +x is "forward"; PlayerMovement mirrors this object to face left.
    /// Angles are measured from straight down, positive = rotated forward.
    /// </summary>
    public class StickmanAnimator : MonoBehaviour
    {
        [System.Serializable]
        struct Gait
        {
            [Tooltip("Distance covered by one full cycle (two steps). Match it to the leg swing so feet don't slide.")]
            public float strideLength;
            [Tooltip("How far each leg swings forward and back, in degrees.")]
            public float legSwing;
            [Tooltip("Knee bend of the leg swinging forward, in degrees.")]
            public float kneeBend;
            [Tooltip("How far each arm swings, in degrees.")]
            public float armSwing;
            [Tooltip("Elbow bend, in degrees.")]
            public float elbowBend;
            [Tooltip("Forward lean of the upper body, in degrees.")]
            public float lean;

            public static Gait Lerp(Gait a, Gait b, float t) => new Gait
            {
                strideLength = Mathf.Lerp(a.strideLength, b.strideLength, t),
                legSwing = Mathf.Lerp(a.legSwing, b.legSwing, t),
                kneeBend = Mathf.Lerp(a.kneeBend, b.kneeBend, t),
                armSwing = Mathf.Lerp(a.armSwing, b.armSwing, t),
                elbowBend = Mathf.Lerp(a.elbowBend, b.elbowBend, t),
                lean = Mathf.Lerp(a.lean, b.lean, t),
            };
        }

        // Joint angles in radians, plus how far the hips sink while the legs are spread.
        struct Pose
        {
            public float lean, hipDrop;
            public float frontThigh, frontKnee, backThigh, backKnee;
            public float frontArm, frontElbow, backArm, backElbow;

            public static Pose Lerp(Pose a, Pose b, float t) => new Pose
            {
                lean = Mathf.Lerp(a.lean, b.lean, t),
                hipDrop = Mathf.Lerp(a.hipDrop, b.hipDrop, t),
                frontThigh = Mathf.Lerp(a.frontThigh, b.frontThigh, t),
                frontKnee = Mathf.Lerp(a.frontKnee, b.frontKnee, t),
                backThigh = Mathf.Lerp(a.backThigh, b.backThigh, t),
                backKnee = Mathf.Lerp(a.backKnee, b.backKnee, t),
                frontArm = Mathf.Lerp(a.frontArm, b.frontArm, t),
                frontElbow = Mathf.Lerp(a.frontElbow, b.frontElbow, t),
                backArm = Mathf.Lerp(a.backArm, b.backArm, t),
                backElbow = Mathf.Lerp(a.backElbow, b.backElbow, t),
            };
        }

        const int HeadSegments = 24;

        [SerializeField] PlayerMovement movement;

        [Header("Lines")]
        [SerializeField] LineRenderer head;
        [SerializeField] LineRenderer torso;
        [SerializeField] LineRenderer frontArm;
        [SerializeField] LineRenderer backArm;
        [SerializeField] LineRenderer frontLeg;
        [SerializeField] LineRenderer backLeg;

        [Header("Proportions")]
        [SerializeField] float headRadius = 0.16f;
        [SerializeField] float torsoLength = 0.55f;
        [SerializeField] float upperArmLength = 0.3f;
        [SerializeField] float forearmLength = 0.28f;
        [SerializeField] float thighLength = 0.42f;
        [SerializeField] float shinLength = 0.42f;
        [Tooltip("Height of the hips relative to this object when standing. With straight legs the feet should touch the bottom of the player's collider.")]
        [SerializeField] float hipHeight = -0.025f;

        [Header("Standing")]
        [Tooltip("How far apart the legs are when standing still, in degrees from vertical.")]
        [SerializeField] float idleLegSpread = 10f;
        [Tooltip("How far the arms hang away from the body when standing still, in degrees.")]
        [SerializeField] float idleArmSpread = 14f;

        [Header("Gaits")]
        [SerializeField] Gait walk = new Gait { strideLength = 1.5f, legSwing = 26f, kneeBend = 45f, armSwing = 24f, elbowBend = 20f, lean = 4f };
        [SerializeField] Gait run = new Gait { strideLength = 2.8f, legSwing = 46f, kneeBend = 100f, armSwing = 55f, elbowBend = 95f, lean = 16f };
        [Tooltip("How quickly the figure blends into and out of its jump pose.")]
        [SerializeField] float airBlendSpeed = 10f;

        Rigidbody2D body;
        float cyclePhase;
        float airBlend;

        void Awake()
        {
            if (movement == null)
                movement = GetComponentInParent<PlayerMovement>();
            body = movement.GetComponent<Rigidbody2D>();
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector2 velocity = body.linearVelocity;
            float speed = Mathf.Abs(velocity.x);
            bool grounded = movement.IsGrounded;

            float moving = Mathf.Clamp01(speed / movement.WalkSpeed);
            Gait gait = Gait.Lerp(walk, run, Mathf.InverseLerp(movement.WalkSpeed, movement.RunSpeed, speed));
            if (grounded)
                cyclePhase = Mathf.Repeat(cyclePhase + 2f * Mathf.PI * speed * dt / gait.strideLength, 2f * Mathf.PI);
            airBlend = Mathf.MoveTowards(airBlend, grounded ? 0f : 1f, airBlendSpeed * dt);

            Pose pose = Pose.Lerp(GroundPose(gait, moving), AirPose(velocity, speed), airBlend);
            float breath = Mathf.Sin(Time.time * 2.5f) * 0.012f * (1f - moving) * (1f - airBlend);
            Draw(pose, breath);
        }

        /// <summary>Puts the figure in its standing pose, e.g. after changing proportions in the Inspector.</summary>
        [ContextMenu("Snap To Idle Pose")]
        public void SnapToIdlePose()
        {
            cyclePhase = 0f;
            Draw(GroundPose(walk, 0f), 0f);
        }

        Pose GroundPose(Gait gait, float moving)
        {
            float sin = Mathf.Sin(cyclePhase);
            float cos = Mathf.Cos(cyclePhase);
            // The standing stance fades out as the walk cycle takes over.
            float swing = (gait.legSwing * moving * sin + idleLegSpread * (1f - moving)) * Mathf.Deg2Rad;
            float knee = gait.kneeBend * Mathf.Deg2Rad * moving;
            float arm = (gait.armSwing * moving * sin - idleArmSpread * (1f - moving)) * Mathf.Deg2Rad;
            float elbow = Mathf.Lerp(12f, gait.elbowBend, moving) * Mathf.Deg2Rad;

            return new Pose
            {
                lean = gait.lean * Mathf.Deg2Rad * moving,
                // The planted leg stays straight, so the hips sink as it tilts away from vertical.
                hipDrop = (thighLength + shinLength) * (1f - Mathf.Cos(swing)),
                // A leg bends its knee while it swings forward and straightens while planted.
                frontThigh = swing,
                frontKnee = knee * Mathf.Max(0f, cos),
                backThigh = -swing,
                backKnee = knee * Mathf.Max(0f, -cos),
                // Each arm swings opposite to the leg on the same side.
                frontArm = -arm,
                frontElbow = elbow,
                backArm = arm,
                backElbow = elbow,
            };
        }

        Pose AirPose(Vector2 velocity, float speed)
        {
            // Tuck the legs and throw the arms up while rising, reach down for the landing while falling.
            float falling = Mathf.InverseLerp(3f, -6f, velocity.y);
            float lean = Mathf.Lerp(0f, 10f, speed / movement.RunSpeed);
            return new Pose
            {
                lean = lean * Mathf.Deg2Rad,
                hipDrop = 0f,
                frontThigh = Mathf.Lerp(75f, 35f, falling) * Mathf.Deg2Rad,
                frontKnee = Mathf.Lerp(105f, 35f, falling) * Mathf.Deg2Rad,
                backThigh = Mathf.Lerp(-15f, -10f, falling) * Mathf.Deg2Rad,
                backKnee = Mathf.Lerp(75f, 25f, falling) * Mathf.Deg2Rad,
                frontArm = Mathf.Lerp(150f, 110f, falling) * Mathf.Deg2Rad,
                frontElbow = 20f * Mathf.Deg2Rad,
                backArm = Mathf.Lerp(125f, 60f, falling) * Mathf.Deg2Rad,
                backElbow = 25f * Mathf.Deg2Rad,
            };
        }

        void Draw(Pose pose, float breath)
        {
            var hip = new Vector2(0f, hipHeight - pose.hipDrop);
            var up = new Vector2(Mathf.Sin(pose.lean), Mathf.Cos(pose.lean));
            Vector2 neck = hip + up * (torsoLength + breath);
            Vector2 shoulder = hip + up * (torsoLength * 0.85f + breath);
            Vector2 headCenter = neck + up * (headRadius + head.widthMultiplier * 0.5f);

            torso.positionCount = 2;
            torso.SetPosition(0, hip);
            torso.SetPosition(1, neck);
            SetLimb(frontLeg, hip, pose.frontThigh, thighLength, pose.frontThigh - pose.frontKnee, shinLength);
            SetLimb(backLeg, hip, pose.backThigh, thighLength, pose.backThigh - pose.backKnee, shinLength);
            SetLimb(frontArm, shoulder, pose.frontArm, upperArmLength, pose.frontArm + pose.frontElbow, forearmLength);
            SetLimb(backArm, shoulder, pose.backArm, upperArmLength, pose.backArm + pose.backElbow, forearmLength);

            head.positionCount = HeadSegments;
            for (int i = 0; i < HeadSegments; i++)
            {
                float angle = i * 2f * Mathf.PI / HeadSegments;
                head.SetPosition(i, headCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * headRadius);
            }
        }

        static void SetLimb(LineRenderer line, Vector2 root, float upperAngle, float upperLength, float lowerAngle, float lowerLength)
        {
            Vector2 joint = root + Direction(upperAngle) * upperLength;
            Vector2 end = joint + Direction(lowerAngle) * lowerLength;
            line.positionCount = 3;
            line.SetPosition(0, root);
            line.SetPosition(1, joint);
            line.SetPosition(2, end);
        }

        static Vector2 Direction(float angleFromDown) => new Vector2(Mathf.Sin(angleFromDown), -Mathf.Cos(angleFromDown));
    }
}
