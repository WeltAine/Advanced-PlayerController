using AdvancedController;
using ImprovedTimers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityUtils;
using UnityUtils.StateMachine;


namespace AdvancedPlayerController
{
    public class PlayerController : MonoBehaviour
    {

        Transform tr;//空间
        Transform cameraTransform;
        PlayerMover mover;//贴地垂直移动器

        PlayerInput _input;//输入组件
        [SerializeField, Required] InputReader input;//！！！等待替换

        #region 参数

        #region 物理参数
        float movementSpeed;//水平面移动速度（既是期望的上限也是默认输入速度）
        float jumpSpeed;//垂直速度
        float jumpDuration;//跳跃持续时间
        float airControllerRate;//空中控制系数（玩家输入对空中速度的影响程度）
        float slopeLimit;//坡度限制

        float gravity;//重力
        float slideGravity;//滑动重力（是的跟坡度没关系，被设为了一个定值）

        float airFriction;//空气阻力
        float groundFriction;//地面阻力

        Vector3 momentum;//动量
        Vector3 savedMovementVelocity;//输入速度

        #endregion

        bool useLocalMomentum;

        StateMachine stateMachine;//！！！等待替换
        CountdownTimer jumpTimer;//！！！等待替换

        #endregion

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        void FixedUpdate()
        {
            mover.CheckForGround();
            stateMachine.FixedUpdate();

            HandleMomentum();


            momentum = GetWorldMomentum();

            mover.SetExtendSensorRange(IsGrounded());
            mover.SetVelocity(momentum);//mover再调整一次动量
            momentum = mover.GetVelocity();
            momentum = useLocalMomentum ? tr.worldToLocalMatrix * momentum : momentum;

            savedMovementVelocity = CalculateMovementVelocity();
        }

        /// <summary>
        /// 获取世界坐标系下动量
        /// </summary>
        /// <returns></returns>
        Vector3 GetWorldMomentum() => useLocalMomentum ? tr.localToWorldMatrix * momentum : momentum;





        void HandleMomentum()
        {
            //分解
            momentum = GetWorldMomentum();
            Vector3 horizontalMomentum = Vector3.ProjectOnPlane(momentum, tr.up);
            //Vector3 verticalMomentum = Vector3.Project(momentum, tr.up);
            Vector3 verticalMomentum = momentum - horizontalMomentum;


            //水平动量处理
            if(stateMachine.CurrentState is GroundedState)
            {
                horizontalMomentum += CalculateMovementVelocity() * Time.fixedDeltaTime;
            }

            if(stateMachine.CurrentState is SlidingState)
            {
                HandleSliding(ref horizontalMomentum);
            }

            if (!IsGrounded())
            {
                HandleNonGrounding(ref horizontalMomentum);
            }

            //摩擦力
            float friction = stateMachine.CurrentState is GroundedState ? groundFriction : airFriction;
            horizontalMomentum = Vector3.MoveTowards(horizontalMomentum, Vector3.zero, friction * Time.fixedDeltaTime);



            //速度约束
            horizontalMomentum = Vector3.ClampMagnitude(horizontalMomentum, movementSpeed);


            //垂直动量处理
            verticalMomentum -= tr.up * gravity;

            if(stateMachine.CurrentState is GroundedState && VectorMath.GetDotProduct(verticalMomentum, tr.up) < 0f)
            {
                verticalMomentum = Vector3.zero;
            }


            //合并
            momentum = horizontalMomentum + verticalMomentum;

            //特殊动量处理
            if(stateMachine.CurrentState is SlidingState)
            {
                Vector3 gradientDirection = Vector3.ProjectOnPlane(-tr.up, mover.GetGroundNormal()).normalized;
                momentum += gradientDirection * slideGravity;
            }

            momentum = useLocalMomentum ? tr.worldToLocalMatrix * momentum : momentum;
        }

        bool IsGrounded() => stateMachine.CurrentState is GroundedState or SlidingState;

        Vector3 CalculateMovementVelocity() => CalculateMovementDirection() * movementSpeed;

        Vector3 CalculateMovementDirection()
        {
            Vector3 direction = input.Direction.x * tr.transform.right + input.Direction.y * tr.forward;

            return direction;
        }

        void HandleSliding(ref Vector3 horizontalMomentum)
        {
            //获取输入
            Vector3 movementVelocity = CalculateMovementVelocity();


            //抹消可能导致上下的操作
            Vector3 gradientDirection = Vector3.ProjectOnPlane(-tr.up, mover.GetGroundNormal());//相对于玩家空间的梯度
            Vector3 prehibitedMovementVelocity = Vector3.ProjectOnPlane(gradientDirection, tr.up).normalized;//梯度方向在水平面上的投影
            //上述两个操作可以直接用   Vector3.ProjectOnPlane(mover.GetGroundNormal(), tr.up).normalized;代替
            movementVelocity = VectorMath.RemoveDotVector(movementVelocity, prehibitedMovementVelocity);

            //追加速度
            horizontalMomentum += movementVelocity * Time.fixedDeltaTime;

        }



        void HandleNonGrounding(ref Vector3 horizontalMomentum)
        {
            //获取输入
            Vector3 movementVelocity = CalculateMovementVelocity();

            //当前向速度已经最大时，我们会更加注重垂直输入
            if(horizontalMomentum.sqrMagnitude > Mathf.Sqrt(movementSpeed))
            {
                if(VectorMath.GetDotProduct(horizontalMomentum, movementVelocity) > 0)
                {
                    movementVelocity = VectorMath.RemoveDotVector(movementVelocity, horizontalMomentum.normalized);
                }

                //空中高速惩罚机制，转向更加困难
                horizontalMomentum += movementVelocity * airControllerRate * Time.fixedDeltaTime * 0.25f;
            }
            else
            {
                horizontalMomentum += movementVelocity * airControllerRate * Time.fixedDeltaTime;
            }
        }
    }

}
