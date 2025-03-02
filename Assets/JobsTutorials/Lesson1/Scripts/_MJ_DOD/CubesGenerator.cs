using Jobs.Common;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Pool;
using UnityEngine.Rendering.UI;
using Random = UnityEngine.Random;
namespace Jobs.OOD
{
    [RequireComponent(typeof(BoxCollider))]
    public class CubesGenerator_MJ : MonoBehaviour
    {
        public GameObject cubeArchetype = null;
        public GameObject targetArea = null;
        [Range(10, 10000)] public int generationTotalNum = 500;
        [Range(1, 60)] public int generationNumPerTicktime = 5;
        [Range(0.1f, 1.0f)] public float tickTime = 0.2f;
        [HideInInspector]
        public Vector3 generatorAreaSize;
        [HideInInspector]
        public Vector3 targetAreaSize;

        public float rotateSpeed = 180.0f;
        public float moveSpeed = 5.0f;        
        private NativeArray<Vector3> randTargetPosArray;
        private TransformAccessArray transformsAccessArray;
        
        //开启collectionChecks时，当外部尝试销毁池内对象时，会触发异常报错
        public bool collectionChecks = true;
        // 对象池
        private ObjectPool<GameObject> pool = null;
        private float timer = 0.0f;
        private Transform[] transforms;

        void Start()
        {
            ///创建对象池
            if (pool == null)
                pool = new ObjectPool<GameObject>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool,
                    OnDestroyPoolObject, collectionChecks, 10, generationTotalNum);

            generatorAreaSize = GetComponent<BoxCollider>().size;
            if (targetArea != null)
                targetAreaSize = targetArea.GetComponent<BoxCollider>().size;

            timer = 0.0f;

            randTargetPosArray = new NativeArray<Vector3>(generationTotalNum, Allocator.TempJob);
            transforms = new Transform[generationTotalNum];
            for (int i = 0; i < generationTotalNum; i++)
            {
                GameObject cube = pool.Get();
                var component = cube.AddComponent<AutoReturnToPool>();
                component.pool = pool;

                Vector3 randomGenerationPos = new Vector3(Random.Range(-generatorAreaSize.x * 0.5f, generatorAreaSize.x * 0.5f),
                    0,Random.Range(-generatorAreaSize.z * 0.5f, generatorAreaSize.z * 0.5f));
                component.generationPos = transform.position + randomGenerationPos;
                cube.transform.position = transform.position + randomGenerationPos;

                Vector3 randomTargetPos = GetRandomTargetPos();
                randTargetPosArray[i] = randomTargetPos;
                component.targetPos = randomTargetPos;
                transforms[i] = cube.transform;
            }
            transformsAccessArray = new TransformAccessArray(transforms);
            for (int i = generationTotalNum-1; i >=0; i--)
            {
                pool.Release(transforms[i].gameObject);
            }
        }

        void Update()
        {
            AutoRotateAndMoveJob_MJ job = new AutoRotateAndMoveJob_MJ()
            {
                deltaTime = Time.deltaTime,
                moveSpeed = moveSpeed,
                targetPosList = randTargetPosArray,
                rotateSpeed = rotateSpeed,
            };
            JobHandle jobHandle = job.Schedule(transformsAccessArray);
            jobHandle.Complete();


            if (timer >= tickTime)
            {
                GenerateCubes();
                timer -= tickTime;
            }

            timer += Time.deltaTime;
        }

        private void OnDestroy()
        {
            if(transformsAccessArray.isCreated)
                transformsAccessArray.Dispose();
            randTargetPosArray.Dispose();

            if (pool != null)
                pool.Dispose();
        }
        
        private void GenerateCubes()
        {
            if (!cubeArchetype  || pool == null)
                return;
            for (int i = 0; i < generationNumPerTicktime; ++i)
            {
                if (pool.CountActive < generationTotalNum)
                {
                    pool.Get();
                }
                else
                {
                    timer = 0;
                    return;
                }
            }
        }
        private Vector3 GetRandomTargetPos()
        {
            return targetArea.transform.position + new Vector3(Random.Range(-targetAreaSize.x * 0.5f, targetAreaSize.x * 0.5f),
                0,
                Random.Range(-targetAreaSize.z * 0.5f, targetAreaSize.z * 0.5f));
        }

        GameObject CreatePooledItem()
        {
            return Instantiate(cubeArchetype, transform);
        }

        void OnReturnedToPool(GameObject gameObject)
        {
            gameObject.SetActive(false);
        }

        void OnTakeFromPool(GameObject gameObject)
        {
            gameObject.SetActive(true);
        }

        void OnDestroyPoolObject(GameObject gameObject)
        {
            Destroy(gameObject);
        }
    }
}
