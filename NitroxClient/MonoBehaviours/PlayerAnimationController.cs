using RootMotion.FinalIK;
using System.Collections;
using UnityEngine;

namespace NitroxClient.MonoBehaviours
{
    //Copy of ArmsController
    public class PlayerAnimationController : MonoBehaviour
    {
        public float smoothSpeedUnderWater = 4f;
        public float smoothSpeedAboveWater = 8f;
        [AssertNotNull]
        public Transform leftAimIKTransform;
        [AssertNotNull]
        public Transform rightAimIKTransform;
        [AssertNotNull]
        public Transform lookTargetTransform;
        [AssertNotNull]
        public Transform attachedLeftHandTarget;
        [AssertNotNull]
        public Transform leftHandElbow;
        [AssertNotNull]
        public BleederAttachTarget bleederAttackTarget;
        public float ikToggleTime = 0.5f;
        [AssertNotNull]
        public Transform leftHandAttach;
        [AssertNotNull]
        public Transform rightHandAttach;
        [AssertNotNull]
        public Transform leftHand;
        [AssertNotNull]
        public Transform rightHand;
        private Animator animator;
        //private Player player;
        //private GUIHand guiHand;
        private Vector3 smoothedVelocity = Vector3.zero;
        //private PDA pda;
        private Vector3 lookTargetResetPos;
        private InspectOnFirstPickup inspectObject;
        private int restoreQuickSlot = -1;
        private string inspectObjectParam;
        private bool inspecting;
        private ArmAiming leftAim = new ArmAiming();
        private ArmAiming rightAim = new ArmAiming();
        private PlayerTool lastTool;
        private FullBodyBipedIK ik;
        private bool isPuttingAwayTool;
        private float diveObstaclesScanInterval = 0.5f;
        private bool obstaclesBelow = true;
        private float nextDiveObstaclesScanTime;
        private Transform leftWorldTarget;
        private Transform rightWorldTarget;
        private bool reconfigureWorldTarget;
        private bool wasBleederAttached;
        private bool wasPdaInUse;
        public Quaternion AimingRotation { get; set; }
        public Vector3 Velocity { get; set; }
        public TechType ItemHeld { get; set; }
        public Quaternion BodyRotation { get; set; }
        public bool UpdatePlayerAnimations { get; set; }
        public AnimChangeType PlayerFlags { get; set; }

        private void Start()
        {
            animator = gameObject.GetComponent<Animator>();
            Utils.Assert(animator != null, "see log", null);
            //player = Utils.GetLocalPlayerComp();
            //guiHand = player.GetComponent<GUIHand>();
            InstallAnimationRules();
            leftAim.FindAimer(gameObject, leftAimIKTransform);
            rightAim.FindAimer(gameObject, rightAimIKTransform);
            ik = GetComponent<FullBodyBipedIK>();
            bleederAttackTarget = GetComponentInChildren<BleederAttachTarget>();
            Utils.Assert(bleederAttackTarget != null, "see log", null);
            lookTargetResetPos = lookTargetTransform.transform.localPosition;
            //pda = player.GetPDA();
        }

        public void StartInspectObjectAsync(InspectOnFirstPickup obj)
        {
            StartCoroutine(InspectObjectAsync(obj));
        }

        //used for cinematic reasons such as the kharaa or the upcoming timecapsule open animation
        public float StartHolsterTime(float time)
        {
            return 1f;
        }

        private IEnumerator HolsterTimeAsync(int quickslot, float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            if (quickslot != -1)
            {
                Inventory.main.quickSlots.Select(quickslot);
            }
            Inventory.main.quickSlots.SetIgnoreHotkeyInput(false);
            Player.main.GetPDA().SetIgnorePDAInput(false);
            yield break;
        }

        public IEnumerator InspectObjectAsync(InspectOnFirstPickup obj)
        {
            if (!inspecting)
            {
                restoreQuickSlot = ((!obj.restoreQuickSlot) ? -1 : Inventory.main.quickSlots.activeSlot);
                InventoryItem heldItem = Inventory.main.quickSlots.heldItem;
                float holsterTime = 0f;
                float inspectDuration = obj.inspectDuration;
                PlayerTool tool = (heldItem == null || !(heldItem.item != null)) ? null : heldItem.item.GetComponent<PlayerTool>();
                if (tool)
                {
                    holsterTime = tool.holsterTime;
                }
                inspectObject = obj;
                if (Inventory.main.ReturnHeld(true))
                {
                    inspecting = true;
                    Inventory.main.quickSlots.SetIgnoreHotkeyInput(true);
                    Player.main.GetPDA().SetIgnorePDAInput(true);
                    inspectObjectParam = ((!string.IsNullOrEmpty(inspectObject.animParam)) ? inspectObject.animParam : ("holding_" + inspectObject.pickupAble.GetTechType().AsString(true)));
                    yield return new WaitForSeconds(holsterTime);
                    inspectObject.OnInspectObjectBegin();
                    inspectObject.gameObject.SetActive(true);
                    animator.SetBool(inspectObjectParam, true);
                    animator.SetBool("using_tool_first", true);
                    yield return new WaitForSeconds(inspectDuration);
                    Inventory.main.quickSlots.SetIgnoreHotkeyInput(false);
                    Player.main.GetPDA().SetIgnorePDAInput(false);
                    animator.SetBool(inspectObjectParam, false);
                    animator.SetBool("using_tool_first", false);
                    inspectObject.gameObject.SetActive(false);
                    inspectObject.OnInspectObjectDone();
                    if (restoreQuickSlot != -1)
                    {
                        Inventory.main.quickSlots.Select(restoreQuickSlot);
                    }
                    inspectObject = null;
                    inspectObjectParam = null;
                    restoreQuickSlot = -1;
                    inspecting = false;
                }
                else
                {
                    inspectObject = null;
                    restoreQuickSlot = -1;
                }
            }
            yield break;
        }

        private void OnInspectObjectDone()
        {
        }

        public void ResetLookTargetToInitialPos()
        {
            lookTargetTransform.localPosition = lookTargetResetPos;
        }

        private void InstallAnimationRules()
        {
        }

        public Vector3 GetSmoothedVelocity()
        {
            return smoothedVelocity;
        }

        private Vector3 GetRelativeVelocity()
        {
            Vector3 velocity = Velocity;//player.gameObject.GetComponent<PlayerController>().velocity;
            //camPivot>camRoot is beside body, so use the Player.head = diveSuit_head_geo
            //if anyone could figure out how to use AimingRotation that would be great but they depend on position too
            Transform aimingTransform = transform.Find("player_view/male_geo/diveSuit/diveSuit_head_geo");//player.camRoot.GetAimingTransform();
            Vector3 result = Vector3.zero;
            if (HasAnimationFlag(AnimChangeType.UNDERWATER) || !HasAnimationFlag(AnimChangeType.STANDING))
            {
                result = aimingTransform.InverseTransformDirection(velocity);
            }
            else
            {
                Vector3 forward = aimingTransform.forward;
                forward.y = 0f;
                forward.Normalize();
                result.z = Vector3.Dot(forward, velocity);
                Vector3 right = aimingTransform.right;
                right.y = 0f;
                right.Normalize();
                result.x = Vector3.Dot(right, velocity);
            }
            return result;
        }

        private void SetPlayerSpeedParameters()
        {
            Vector3 relativeVelocity = gameObject.transform.rotation.GetInverse() * Velocity;
            float d = (!Player.main.IsUnderwater()) ? smoothSpeedAboveWater : smoothSpeedUnderWater;
            smoothedVelocity = UWE.Utils.SlerpVector(smoothedVelocity, relativeVelocity, Vector3.Normalize(relativeVelocity - smoothedVelocity) * d * Time.deltaTime);
            SafeAnimator.SetFloat(animator, "move_speed", smoothedVelocity.magnitude);
            SafeAnimator.SetFloat(animator, "move_speed_x", smoothedVelocity.x);
            SafeAnimator.SetFloat(animator, "move_speed_y", smoothedVelocity.y);
            SafeAnimator.SetFloat(animator, "move_speed_z", smoothedVelocity.z);
            float viewPitch = AimingRotation.eulerAngles.x;
            if (viewPitch > 180f)
            {
                viewPitch -= 360f;
            }
            viewPitch = -viewPitch;
            SafeAnimator.SetFloat(animator, "view_pitch", viewPitch);
        }

        public void TriggerAnimParam(string paramName, float duration = 0f)
        {
            animator.SetBool(paramName, true);
            StartSetAnimParam(paramName, duration);
        }

        private void StartSetAnimParam(string paramName, float duration)
        {
            StartCoroutine(SetAnimParamAsync(paramName, false, duration));
        }

        private IEnumerator SetAnimParamAsync(string paramName, bool value, float duration)
        {
            yield return new WaitForSeconds(duration);
            animator.SetBool(paramName, value);
            yield break;
        }

        private void Reconfigure(PlayerTool tool)
        {
            ik.solver.GetBendConstraint(FullBodyBipedChain.LeftArm).bendGoal = leftHandElbow;
            ik.solver.GetBendConstraint(FullBodyBipedChain.LeftArm).weight = 1f;
            if (tool == null)
            {
                leftAim.shouldAim = false;
                rightAim.shouldAim = false;
                ik.solver.leftHandEffector.target = null;
                ik.solver.rightHandEffector.target = null;
                if (!HasAnimationFlag(AnimChangeType.PDAEQUIPPED))
                {
                    if (leftWorldTarget)
                    {
                        ik.solver.leftHandEffector.target = leftWorldTarget;
                        ik.solver.GetBendConstraint(FullBodyBipedChain.LeftArm).bendGoal = null;
                        ik.solver.GetBendConstraint(FullBodyBipedChain.LeftArm).weight = 0f;
                    }
                    if (rightWorldTarget)
                    {
                        ik.solver.rightHandEffector.target = rightWorldTarget;
                    }
                }
            }
            else
            {
                if (!IsBleederAttached())
                {
                    leftAim.shouldAim = tool.ikAimLeftArm;
                    if (tool.useLeftAimTargetOnPlayer)
                    {
                        ik.solver.leftHandEffector.target = attachedLeftHandTarget;
                    }
                    else
                    {
                        ik.solver.leftHandEffector.target = tool.leftHandIKTarget;
                    }
                }
                else
                {
                    leftAim.shouldAim = tool.ikAimRightArm;
                    ik.solver.leftHandEffector.target = null;
                }
                rightAim.shouldAim = tool.ikAimRightArm;
                ik.solver.rightHandEffector.target = tool.rightHandIKTarget;
            }
        }

        private void UpdateHandIKWeights()
        {
            float num = ik.solver.rightHandEffector.positionWeight;
            float num2 = (!(ik.solver.rightHandEffector.target != null)) ? 0f : 1f;
            float num3 = ik.solver.leftHandEffector.positionWeight;
            float num4 = (!(ik.solver.leftHandEffector.target != null)) ? 0f : 1f;
            if (ikToggleTime == 0f)
            {
                num = num2;
                num3 = num4;
            }
            else
            {
                num = UWE.Utils.Slerp(num, num2, Time.deltaTime / ikToggleTime);
                num3 = UWE.Utils.Slerp(num3, num4, Time.deltaTime / ikToggleTime);
            }
            ik.solver.rightHandEffector.positionWeight = num;
            ik.solver.rightHandEffector.rotationWeight = num;
            ik.solver.leftHandEffector.positionWeight = num3;
            ik.solver.leftHandEffector.rotationWeight = num3;
            if (num3 > 0f || num > 0f)
            {
                ik.solver.IKPositionWeight = 1f;
            }
            else
            {
                ik.solver.IKPositionWeight = 0f;
            }
        }

        public bool IsBleederAttached()
        {
            return bleederAttackTarget.attached;
        }

        private void UpdateDiving()
        {
            bool falling = !HasAnimationFlag(AnimChangeType.PRECURSORWATER) &&
                (Player.main.GetMode() == Player.Mode.Normal &&
                !HasAnimationFlag(AnimChangeType.UNDERWATER) &&
                !HasAnimationFlag(AnimChangeType.STANDING)) &&
                Velocity.y < 0f;
            if (falling && Time.time >= nextDiveObstaclesScanTime)
            {
                nextDiveObstaclesScanTime = Time.time + diveObstaclesScanInterval;
                Vector3 direction = Velocity.normalized + Vector3.down;
                int layerMask = ~(1 << LayerMask.NameToLayer("Player"));
                float maxDistance = 5f;
                //body has same local position as player so this works the same as player.transform.position
                if (transform.position.y > 0f)
                {
                    maxDistance = (transform.position.y + 1.5f) / Vector3.Dot(direction.normalized, Vector3.down);
                }
                obstaclesBelow = Physics.Raycast(transform.position, direction, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                obstaclesBelow = (obstaclesBelow || !falling);
            }
            SafeAnimator.SetBool(animator, "diving", falling && !obstaclesBelow);
            SafeAnimator.SetBool(animator, "diving_land", falling && obstaclesBelow);
        }

        public void SetWorldIKTarget(Transform leftTarget, Transform rightTarget)
        {
            leftWorldTarget = leftTarget;
            rightWorldTarget = rightTarget;
            reconfigureWorldTarget = true;
        }

        private void Update()
        {
            if (!UpdatePlayerAnimations)
            {
                return;
            }
            bool jumping = !HasAnimationFlag(AnimChangeType.PRECURSORWATER) &&
                (Player.main.GetMode() == Player.Mode.Normal &&
                !HasAnimationFlag(AnimChangeType.UNDERWATER) &&
                !HasAnimationFlag(AnimChangeType.STANDING));
            //bool bleederAttached = IsBleederAttached();
            SetPlayerSpeedParameters();
            leftAim.Update(ikToggleTime);
            rightAim.Update(ikToggleTime);
            UpdateHandIKWeights();
            if (bleederAttackTarget.attached)
            {
                SafeAnimator.SetBool(animator, "using_tool", HasAnimationFlag(AnimChangeType.USINGTOOL));
            }
            else
            {
                SafeAnimator.SetBool(animator, "using_tool", HasAnimationFlag(AnimChangeType.USINGTOOL));
                SafeAnimator.SetBool(animator, "using_tool", HasAnimationFlag(AnimChangeType.USINGTOOLALT));
            }
            SafeAnimator.SetBool(animator, "holding_tool", ItemHeld != TechType.None);
            //SafeAnimator.SetBool(animator, "bleeder", bleederAttackTarget.attached);
            SafeAnimator.SetBool(animator, "jump", jumping);
            //SafeAnimator.SetFloat(animator, "verticalOffset", MainCameraControl.main.GetImpactBob());
            //wasBleederAttached = bleederAttached;
            UpdateDiving();
        }

        public bool IsInAnimationState(string layer, string state)
        {
            Utils.Assert(!string.IsNullOrEmpty(layer), "see log", null);
            Utils.Assert(!string.IsNullOrEmpty(state), "see log", null);
            int num = Animator.StringToHash(layer + "." + state);
            return animator.GetCurrentAnimatorStateInfo(0).nameHash == num;
        }

        //unused - deleted to prevent errors
        public void OnToolBleederHitAnim(AnimationEvent e)
        {
        }

        //unused
        public void OnToolUseAnim(AnimationEvent e)
        {
        }

        public void OnToolReloadBeginAnim(AnimationEvent e)
        {
        }

        public void OnToolReloadEndAnim(AnimationEvent e)
        {
        }

        public void OnToolReloadAnim(AnimationEvent e)
        {
        }

        public void OnSwimFastStartAnim(AnimationEvent e)
        {
            Player localPlayerComp = Utils.GetLocalPlayerComp();
            localPlayerComp.OnSwimFastStartAnim(e);
        }

        public void OnSwimFastEndAnim(AnimationEvent e)
        {
            Player localPlayerComp = Utils.GetLocalPlayerComp();
            localPlayerComp.OnSwimFastEndAnim(e);
        }

        //unused
        private void FireExSpray(AnimationEvent e)
        {
        }

        //unused
        public void BashHit(AnimationEvent e)
        {
        }

        public void SetUsingBuilder(bool isUsingBuilder)
        {
            if (animator)
            {
                SafeAnimator.SetBool(animator, "using_builder", isUsingBuilder);
            }
        }

        private void spawn_pda()
        {
        }

        private void kill_pda()
        {
        }

        public void OnToolAnimDraw(AnimationEvent e)
        {
        }

        private class ArmAiming
        {
            public AimIK aimer;

            public bool shouldAim;

            private float weightVelocity;

            public void FindAimer(GameObject ikObj, Transform aimedXform)
            {
                foreach (AimIK aimIK in ikObj.GetComponentsInChildren<AimIK>())
                {
                    if (aimIK.solver.transform == aimedXform)
                    {
                        if (aimer != null)
                        {
                            UWE.Utils.LogReport("Found multiple AimIK components on " + ikObj.GetFullHierarchyPath() + " that manipulate the transform " + aimedXform.gameObject.GetFullHierarchyPath(), null);
                        }
                        else
                        {
                            aimer = aimIK;
                        }
                    }
                }
                if (aimer == null)
                {
                    UWE.Utils.LogReport("Could not find AimIK comp on " + ikObj.GetFullHierarchyPath() + " for transform " + aimedXform.gameObject.GetFullHierarchyPath(), null);
                }
            }

            public void Update(float smoothTime)
            {
                if (aimer == null)
                {
                    return;
                }
                aimer.solver.IKPositionWeight = Mathf.SmoothDamp(aimer.solver.IKPositionWeight, (!shouldAim) ? 0f : 1f, ref weightVelocity, smoothTime);
            }
        }

        private bool HasAnimationFlag(AnimChangeType value)
        {
            return (PlayerFlags & value) != 0;
        }

        public bool this[string name]
        {
            get { return animator.GetBool(name); }
            set { animator.SetBool(name, value); }
        }

        public void SetFloat(string name, float value)
        {
            animator.SetFloat(name, value);
        }
    }
}
