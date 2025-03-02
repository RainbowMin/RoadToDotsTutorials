using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Jobs;

namespace MJ.Jobs.OOD
{    
    public class WaveCubesWithJobs_IJobParallelForTransform : MonoBehaviour
    {
        public GameObject cubeAchetype = null;
        [Range(10, 100)] public int xHalfCount = 40;
        [Range(10, 100)] public int zHalfCount = 40;
        private TransformAccessArray transformsAccessArray;
        static readonly ProfilerMarker<int> profilerMarker = new ProfilerMarker<int>("WaveCubes UpdateTransform", "Objects Count");
        void Start()
        {
            transformsAccessArray = new TransformAccessArray(4 * xHalfCount * zHalfCount);            
            for (var z = -zHalfCount; z <= zHalfCount; z++)
            {
                for (var x = -xHalfCount; x <= xHalfCount; x++)
                {
                    var cube = Instantiate(cubeAchetype);
                    cube.transform.position = new Vector3(x * 1.1f, 0, z * 1.1f);
                    transformsAccessArray.Add(cube.transform);
                }
            }       
        }

        void OnDestroy()
        {
            if (transformsAccessArray.isCreated)
                transformsAccessArray.Dispose();
        }

        void Update()
        {
            using (profilerMarker.Auto(transformsAccessArray.length))
            {
                WaveCubesJobForTransform waveCubesJob = new WaveCubesJobForTransform();
                waveCubesJob.TimeValue = Time.time; 
                JobHandle jobHandle = waveCubesJob.Schedule(transformsAccessArray);
                jobHandle.Complete();
            }
        }
    }

    [BurstCompile]
    struct WaveCubesJobForTransform : IJobParallelForTransform
    {
        [ReadOnly] public float TimeValue;
        public void Execute(int index, TransformAccess transform)
        {
            var distance = Vector3.Distance(transform.position, Vector3.zero);
            transform.localPosition += Vector3.up * Mathf.Sin(TimeValue * 3f + distance * 0.2f);
        }
    }
}