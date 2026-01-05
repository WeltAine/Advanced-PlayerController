using UnityEngine;


namespace AdvancedPlayerController 
{
    public class RaycastSensor
    {
        //方向枚举
        public enum CastDirection
        {
            Forward,
            Up,
            Right,
            Backward,
            Down,
            Left
        }

        Transform transform;

        Vector3 origin = Vector3.zero;
        CastDirection castDirection;
        public float castLength = 1f;

        public LayerMask layerMask = 255;

        RaycastHit hitInformation;


        public RaycastSensor(Transform transform)
        {
            this.transform = transform;
            castDirection = CastDirection.Forward;
        }

        public void Cast()
        {
            Vector3 worldOrigin = transform.TransformPoint(origin);
            Vector3 worldCastDirection = GetCastDirection();

            Ray ray = new Ray(worldOrigin, worldCastDirection);
            Physics.Raycast(ray, out hitInformation, castLength, layerMask, QueryTriggerInteraction.Ignore);
        }


        public void SetCastOrigin(Vector3 pos)
        {
            origin = transform.InverseTransformPoint(pos);
        }

        public Vector3 GetCastDirection()
        {
            return castDirection switch {
                CastDirection.Forward => transform.forward,
                CastDirection.Up => transform.up,
                CastDirection.Right => transform.right,

                CastDirection.Backward => -transform.forward,
                CastDirection.Down => -transform.up,
                CastDirection.Left => -transform.right
            };
        }

        public void SetCastDirection(CastDirection direction)
        {
            castDirection = direction;
        }



        public bool HasDetectedHit()
        {
            return hitInformation.collider != null;
        }

        public Transform GetTransform()
        {
            return hitInformation.transform;
        }

        public Vector3 GetNormal()
        {
            return hitInformation.normal;
        }

        public Vector3 GetPosition()
        {
            return hitInformation.point;
        }

        public float GetDistance()
        {
            return hitInformation.distance;
        }

        public Collider GetCollider()
        {
            return hitInformation.collider;
        }
    }

}

