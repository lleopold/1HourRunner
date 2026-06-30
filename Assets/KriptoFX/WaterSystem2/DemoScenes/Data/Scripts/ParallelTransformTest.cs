using KWS;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

using UnityEngine.Assertions;
using UnityEngine.Jobs;

public class WaterSurfaceTest : MonoBehaviour
{
    
    public struct ApplyVelocityJobParallelForTransform : IJobParallelForTransform
    {
        [ReadOnly]
        public NativeArray<Vector3> velocity;
        public NativeArray<SurfaceData> dataToSend;
        [ReadOnly] public NativeArray<SurfaceData> result;

        public float deltaTime;

      
        public void Execute(int index, TransformAccess transform)
        {

            var data =  dataToSend[index];
            data.Position =  transform.position;
            data.Position += velocity[index] * deltaTime;

            dataToSend[index] = data;
            
            var transformPosition = transform.position;
            transformPosition.y = result[index].Position.y;
            transform.position = transformPosition;
        }
    }

    public void ApplyVelocity_ParallelForTransform()
    { 
        WaterSurfaceJobs.EnableWaterJobSystem    = true;
        WaterSurfaceJobs.DataToSend = dataToSend;
        
        if (!transformAccessArray.isCreated || transformAccessArray.length == 0) return;
        if (!velocity.IsCreated             || velocity.Length             != transformAccessArray.length) return;
        if (!dataToSend.IsCreated           || dataToSend.Length           != transformAccessArray.length) return;
        if (!WaterSurfaceJobs.Result.IsCreated || WaterSurfaceJobs.Result.Length != transformAccessArray.length) return;
       
        var job = new ApplyVelocityJobParallelForTransform()
        {
            deltaTime  = Time.deltaTime,
            velocity   = velocity,
            dataToSend = dataToSend,
            result = WaterSurfaceJobs.Result
        };

 
        var handle = job.ScheduleByRef(transformAccessArray);
        handle.Complete();

    }
    
    
    NativeArray<Vector3> velocity;
    TransformAccessArray transformAccessArray;
    GameObject[]         gameObjects;
    Transform[]          transforms;
    NativeArray<SurfaceData> dataToSend;
    
    void OnEnable()
    {
        gameObjects = new GameObject[1000];
        transforms = new Transform[1000];
        
        int   gridSize = Mathf.CeilToInt(Mathf.Sqrt(gameObjects.Length));
        float spacing  = 1.0f; 
        
        float offsetX = (gridSize - 1) * spacing * 0.5f;
        float offsetZ = (gridSize - 1) * spacing * 0.5f;

        for (int i = 0; i < gameObjects.Length; i++)
        {
            int x = i % gridSize;
            int z = i / gridSize;

            gameObjects[i]      = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObjects[i].name = $"Test Object {i}";

            gameObjects[i].transform.position = new Vector3(
                x * spacing - offsetX,
                0,
                z * spacing - offsetZ
            );

            gameObjects[i].transform.localScale = Vector3.one * 0.25f;
            transforms[i]                       = gameObjects[i].transform;
        }

       
        transformAccessArray        = new TransformAccessArray(transforms);
        velocity                    = new NativeArray<Vector3>(transforms.Length, Allocator.Persistent);
        dataToSend                  = new NativeArray<SurfaceData>(transforms.Length, Allocator.Persistent);
        
        for (var i = 0; i < velocity.Length; ++i)
        {
            velocity[i] = new Vector3(Random.Range(-2, 2), 0, Random.Range(-2, 2));
        }

    }

    void OnDisable()
    { 
        WaterSurfaceJobs.EnableWaterJobSystem = false;
        WaterSurfaceJobs.DataToSend           = default;
        
        if (velocity.IsCreated) velocity.Dispose();
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        if (dataToSend.IsCreated) dataToSend.Dispose();
        
        for (var index = 0; index < gameObjects.Length; index++)
        {
            Destroy(gameObjects[index]);
        }

    }

    void Update()
    {
        ApplyVelocity_ParallelForTransform();
        
        
    }
}