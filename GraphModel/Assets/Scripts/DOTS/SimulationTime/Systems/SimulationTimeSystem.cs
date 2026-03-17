using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct SimulationTimeSystem : ISystem
{
    //EntityQuery _TextMeshQuery;
    //EntityQuery _SentEntitiesQuery;

    public void OnCreate(ref SystemState state)
    {
        //k_TextOffset = new float3(0, 1.5f, 0);
        //_TextMeshQuery = state.GetEntityQuery(typeof(DisplayCollisionText), typeof(TextMesh));
        //_SentEntitiesQuery = state.GetEntityQuery(typeof(SentEntity));
    }
}
