using Unity.VisualScripting;
using UnityEngine;


//保证在地面状态下的平滑上下移动（平滑贴地），调整胶囊体位置，投射传感器设置，贴地状态下垂直速度设置

namespace AdvancedPlayerController
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMover : MonoBehaviour
    {
        [Range(0f, 1f)]
        [SerializeField] float stepHeightRatio = 0.2f;

        [SerializeField] float baseColliderHeight = 2f;
        [SerializeField] float baseColliderThickness = 1f;
        [SerializeField] float baseColliderOffset = 0f;
        CapsuleCollider capsuleCollider;
        Transform aimTransform;//一般默认本体

        RaycastSensor sensor;
        float baseSensorRange;

        private void Awake()
        {
            Setup();
        }

        private void OnValidate()
        {
            if(gameObject.activeInHierarchy)
            {
                RecalculateCapsuleColliderDimension();
                RecalibrateSensor();
            }
        }


        void Setup()
        {
            capsuleCollider = gameObject.GetComponent<CapsuleCollider>();

            rb = gameObject.GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.useGravity = false;


            aimTransform = this.transform;

            RecalculateCapsuleColliderDimension();
            RecalibrateSensor();
        }



        /// <summary>
        /// 碰撞体尺寸调整，高度，厚度，偏移
        /// </summary>
        void RecalculateCapsuleColliderDimension()
        {
            capsuleCollider.height = baseColliderHeight * (1 - stepHeightRatio);
            capsuleCollider.radius = (baseColliderThickness / 2);
            capsuleCollider.center = (baseColliderOffset + baseColliderHeight * 0.5f * stepHeightRatio) * transform.up;

            if(capsuleCollider.height / 2  < capsuleCollider.radius)
            {
                capsuleCollider.radius = capsuleCollider.height / 2;
            }
        }

        /// <summary>
        /// 重新校准投射器，调整投射起点，长度
        /// </summary>
        void RecalibrateSensor()
        {
            sensor ??= new RaycastSensor(aimTransform);

            //设置传感器起点和方向
            sensor.SetCastOrigin(capsuleCollider.bounds.center);
            sensor.SetCastDirection(RaycastSensor.CastDirection.Down);


            //设置投射长度（这里只是算一个基础，在调用检测方法时再决定具体长度）
            const float safetyDistanceFactor = 0.001f;

            float length = baseColliderHeight * 0.5f + baseColliderHeight * 0.5f * stepHeightRatio;
            baseSensorRange = length * (1f + safetyDistanceFactor) * aimTransform.localScale.x;
            sensor.castLength = length * aimTransform.localScale.x;
            
            //检测掩码设置（和组件所在对象的物理掩码一致）
            RecalculateSensorLayerMask();
        }


        /// <summary>
        /// 传感器掩码设置（和组件所在对象的物理掩码一致）
        /// </summary>
        void RecalculateSensorLayerMask()
        {
            int objectLayer = gameObject.layer;
            int layermask = Physics.AllLayers;
            for (int i = 0; i < 32; i++)
            {
                if (Physics.GetIgnoreLayerCollision(objectLayer, i))
                {
                    layermask &= ~(1 << i);
                }
            }
        }


        bool isGrounded = false;
        public bool IsGrounded() => isGrounded;
        public void SetExtendSensorRange(bool isExtended) => isUsingExtendedSensorRange = isExtended;
        bool isUsingExtendedSensorRange = false; 
        Vector3 currentGroundAdjustmentVelocity = Vector3.zero;//垂直速度

        public void CheckForGround()
        {

            currentGroundAdjustmentVelocity = Vector3.zero;

            sensor.castLength = isUsingExtendedSensorRange
                ? baseSensorRange + baseColliderHeight * stepHeightRatio
                : baseSensorRange;

            sensor.Cast();

            isGrounded = sensor.HasDetectedHit();

            if (!isGrounded) return;

            float distance = sensor.GetDistance();
            float baseDistance = (baseColliderHeight - capsuleCollider.height * 0.5f) * aimTransform.localScale.x;
            currentGroundAdjustmentVelocity = aimTransform.up * (baseDistance - distance) / Time.fixedDeltaTime;//我们不是希望一秒内完成完成矫正，而是一帧内

        }


        Rigidbody rb;

        public void SetVelocity(Vector3 velocity) => rb.linearVelocity = velocity + currentGroundAdjustmentVelocity;


        public Vector3 GetGroundNormal() => sensor.GetNormal();
    }

}
