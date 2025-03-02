using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Jobs.OOD
{
    [BurstCompile]
    public struct AutoRotateAndMoveJob_MJ : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<Vector3> targetPosList;
        [ReadOnly] public float moveSpeed;
        [ReadOnly] public float rotateSpeed;
        [ReadOnly] public float deltaTime;
        public void Execute(int index, TransformAccess transform)
        {
            transform.rotation *= Quaternion.AngleAxis(rotateSpeed * deltaTime, Vector3.up);
            Vector3 targetPos = targetPosList[index];
            Vector3 dist = targetPos - transform.position;
            Vector3 moveDir = dist.normalized;
            transform.position += moveDir * (moveSpeed * deltaTime);
        }
    }
}