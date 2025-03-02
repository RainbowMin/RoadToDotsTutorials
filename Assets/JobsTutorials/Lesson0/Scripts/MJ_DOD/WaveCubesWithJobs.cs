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
    public class WaveCubesWithJobs : MonoBehaviour
    {
        public GameObject cubeAchetype = null;
        [Range(10, 100)] public int xHalfCount = 40;
        [Range(10, 100)] public int zHalfCount = 40;
        private NativeArray<Vector3> m_WorldPositions;
        private NativeArray<Vector3> m_LocalPositions;
        private List<Transform> cubesList = new(40*40);
        static readonly ProfilerMarker<int> profilerMarker = new ProfilerMarker<int>("WaveCubes UpdateTransform", "Objects Count");
        void Start()
        {           

            for (var z = -zHalfCount; z <= zHalfCount; z++)
            {
                for (var x = -xHalfCount; x <= xHalfCount; x++)
                {
                    var cube = Instantiate(cubeAchetype);
                    cube.transform.position = new Vector3(x * 1.1f, 0, z * 1.1f);
                    cubesList.Add(cube.transform);
                }
            }          
            m_WorldPositions = new NativeArray<Vector3>(cubesList.Count, Allocator.Persistent);
            m_LocalPositions = new NativeArray<Vector3>(cubesList.Count, Allocator.Persistent);
        }

        void OnDestroy()
        {
            m_WorldPositions.Dispose();
            m_LocalPositions.Dispose();
        }

        void Update()
        {
            using (profilerMarker.Auto(cubesList.Count))
            {
                for(int i = 0; i < cubesList.Count; i++)
                {
                    m_WorldPositions[i] = cubesList[i].position;
                    m_LocalPositions[i] = cubesList[i].localPosition;                
                }

                WaveCubesJob waveCubesJob = new WaveCubesJob();
                waveCubesJob.TimeValue = Time.time;
                waveCubesJob.WorldPositions = m_WorldPositions;
                waveCubesJob.LocalPositions = m_LocalPositions;
                JobHandle jobHandle = waveCubesJob.Schedule(cubesList.Count, 64);
                jobHandle.Complete();

                for(int i = 0; i < cubesList.Count; i++)
                {
                    cubesList[i].localPosition = m_LocalPositions[i];
                }
            }
  
        }
    }

    [BurstCompile]
    struct WaveCubesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> WorldPositions;
        public NativeArray<Vector3> LocalPositions;
        [ReadOnly] public float TimeValue;

        public void Execute(int index)
        {
            var distance = Vector3.Distance(WorldPositions[index], Vector3.zero);
            LocalPositions[index] += Vector3.up * Mathf.Sin(TimeValue * 3f + distance * 0.2f);
        }
    }
}