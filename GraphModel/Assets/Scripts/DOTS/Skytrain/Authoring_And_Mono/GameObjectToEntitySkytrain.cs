using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Stateful;
using Unity.Rendering;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class GameObjectToEntitySkytrain : MonoBehaviour
{
    public SkytrainLoadingZoneVariables _loadingZones;
    public SkytrainVisibleSkytrainVariables _visibleSkytrain;
    public SkytrainMotionDetectionVariables _motionDetection;
    public SkytrainPassengerVariables _passengers;
    public float _skytrainEntityScale = 1.5f;
    public bool _loadingZoneInTransit = false; // For testing purposes, remove after testing if "in transit" will stop loading zones from functioning

    private EntityManager _entityManager;
    private Entity _skytrainEntity;
    private List<Entity> _loadingZoneEntities = new List<Entity>();
    private bool _loadingZoneErrorHappened = false;
    

    void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        InstantiateSkytrainEntity();

        // create the loading zone entities
        if (_loadingZones._loadingZoneOffsets.Count > 0)
        {
            InstantiateLoadingZoneEntities();
        }
        

        Debug.Log(gameObject.name + " skytrain entity is [" + _skytrainEntity.Index + "]");

    }

    void Update()
    {
        UpdateVisibleSkytrainEntityTransform();

        UpdateLoadingZonesTransforms();
    }

    void OnDestroy()
    {
        // Destroy the entity when the GameObject is destroyed
        if (_skytrainEntity == null)
        {
            return;
        }

        if (World.DefaultGameObjectInjectionWorld.EntityManager.Exists(_skytrainEntity))
        {

            World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(_skytrainEntity);
        }
        
    }

    private void InstantiateSkytrainEntity()
    {
        // Create an entity for the skytrain and add the position component
        _skytrainEntity = _entityManager.CreateEntity(typeof(SkytrainProperties));
        _entityManager.SetComponentData(_skytrainEntity, new SkytrainProperties {
            SkytrainName = "SkytrainCoolio",
            MaxCapacity = 500,
            CurrentCapacity = 0
        });
        _entityManager.AddComponentData(_skytrainEntity, new LocalTransform
        {
            Position = this.transform.position,
            Scale = _skytrainEntityScale,
            Rotation = this.transform.rotation
        });
        _entityManager.AddComponent<LocalToWorld>(_skytrainEntity);

        // Add physics stuff to enable physics
        //PhysicsFilter
         Unity.Physics.CollisionFilter colFilter = Unity.Physics.CollisionFilter.Default;
        // Would change what the trigger can collide with here
        //Material (for physics, not colour)
        Unity.Physics.Material colMaterial = Unity.Physics.Material.Default;
        colMaterial.CollisionResponse = Unity.Physics.CollisionResponsePolicy.CollideRaiseCollisionEvents; // This makes it a trigger
                                                                                                  //PhysicsCollider
        BlobAssetReference<Unity.Physics.Collider> boxColliderBlob = Unity.Physics.BoxCollider.Create(new Unity.Physics.BoxGeometry
        {
            Center = float3.zero,
            BevelRadius = 0.05f,
            Orientation = quaternion.identity,
            Size = new float3(1, 1, 1)
        },
            colFilter,
            colMaterial
        );;
        _entityManager.AddComponentData(_skytrainEntity, new Unity.Physics.PhysicsCollider { Value = boxColliderBlob });
        //PhysicsVelocity
        _entityManager.AddComponentData(_skytrainEntity, new Unity.Physics.PhysicsVelocity
        { });
        //PhysicsMass
        //PhysicsWorldIndex
        _entityManager.AddSharedComponentManaged(_skytrainEntity, new Unity.Physics.PhysicsWorldIndex
        {
            Value = 0
        });


        float3 skytrainPosition = this.transform.position;

        // Add skytrain motion state
        _entityManager.AddComponentData(_skytrainEntity, new SkytrainMotionState { 
            Threshold = _motionDetection._movementThreshold,
            TotalMovement = 0,
            PreviousPosition = skytrainPosition,
            CurrentBufferPosition = 0
        });

        DynamicBuffer<SkytrainMotionMagnitude> motionMagnitudesBuffer = _entityManager.AddBuffer<SkytrainMotionMagnitude>(_skytrainEntity);

        for (int i = 0; i < motionMagnitudesBuffer.Capacity; i++)
        {
            motionMagnitudesBuffer.Add(new SkytrainMotionMagnitude
            { 
                Value = 0
            });
        }

        // If set to create visible entity, add relevant components to make it visible
        if (_visibleSkytrain._createVisibleSkytrainEntity && _visibleSkytrain._skytrainVisibleEntityPrefab != null)
        {
            InstantiateVisibleSkytrainEntity();
        }
        // If was supposed to make a visible entity, but did not set the prefab
        else if (_visibleSkytrain._createVisibleSkytrainEntity) 
        {
            Debug.LogError("Was supposed to create a visible skytrain, but the skytrain lacked a necessary prefab");
        }

        if (_passengers._passengerPrefab != null)
        {
            Entity passengerPrototype = CreatePassengerPrototypeEntity();
            _entityManager.AddComponentData<PassengerPrototype>(
                _skytrainEntity,
                new PassengerPrototype
                {
                    passengerEntity = passengerPrototype,
                    distanceBetweenEntities = _passengers._distanceBetweenPassengers
                });
        }
        else
        {
            Debug.LogError("Was supposed to create a passenger prototype, but the skytrain lacked a necessary prefab");
        }
    }

    private Entity CreatePassengerPrototypeEntity()
    {
        Entity passengerPrototype = _entityManager.CreateEntity();
        _entityManager.AddComponent<LocalToWorld>(passengerPrototype);

        // Add physics stuff to enable physics
        //PhysicsFilter
        Unity.Physics.CollisionFilter colFilter = Unity.Physics.CollisionFilter.Default;
        // Would change what the trigger can collide with here
        //Material (for physics, not colour)
        Unity.Physics.Material colMaterial = Unity.Physics.Material.Default;
        colMaterial.CollisionResponse = Unity.Physics.CollisionResponsePolicy.CollideRaiseCollisionEvents; // This makes it a trigger
                                                                                                           //PhysicsCollider
        BlobAssetReference<Unity.Physics.Collider> boxColliderBlob = Unity.Physics.BoxCollider.Create(new Unity.Physics.BoxGeometry
        {
            Center = float3.zero,
            BevelRadius = 0.05f,
            Orientation = quaternion.identity,
            Size = new float3(1, 1, 1)
        },
            colFilter,
            colMaterial
        );
        _entityManager.AddComponentData(passengerPrototype, new Unity.Physics.PhysicsCollider { Value = boxColliderBlob });
        //PhysicsVelocity
        _entityManager.AddComponentData(passengerPrototype, new Unity.Physics.PhysicsVelocity
        { });
        //PhysicsMass
        //PhysicsWorldIndex
        _entityManager.AddSharedComponentManaged(passengerPrototype, new Unity.Physics.PhysicsWorldIndex
        {
            Value = 0
        });
        // Create a RenderMeshDescription using the convenience constructor
        // with named parameters.
        var desc = new RenderMeshDescription(
            shadowCastingMode: ShadowCastingMode.Off,
            receiveShadows: false);

        // Create an array of mesh and material required for runtime rendering.
        //From variables
        //var renderMeshArray = new RenderMeshArray(new Material[] { Material }, new Mesh[] { Mesh });

        //var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        //From prefab
        var renderMeshArray = new RenderMeshArray(new Material[] {
           _passengers._passengerPrefab.GetComponent<Renderer>().sharedMaterial
        }, new Mesh[] {
           _passengers._passengerPrefab.GetComponent<MeshFilter>().sharedMesh
        });

        // Call AddComponents to populate base entity with the components required
        // by Entities Graphics
        RenderMeshUtility.AddComponents(
            passengerPrototype,
            _entityManager,
            desc,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

        _entityManager.AddComponent<Passenger>(passengerPrototype);

        return passengerPrototype;
        
    }

    private void InstantiateVisibleSkytrainEntity()
    {
        // Create a RenderMeshDescription using the convenience constructor
        // with named parameters.
        var desc = new RenderMeshDescription(
            shadowCastingMode: ShadowCastingMode.Off,
            receiveShadows: false);

        // Create an array of mesh and material required for runtime rendering.
        //From variables
        //var renderMeshArray = new RenderMeshArray(new Material[] { Material }, new Mesh[] { Mesh });
        //From prefab
        var renderMeshArray = new RenderMeshArray(new Material[] {
           _visibleSkytrain._skytrainVisibleEntityPrefab.GetComponent<Renderer>().sharedMaterial
        }, new Mesh[] {
            _visibleSkytrain._skytrainVisibleEntityPrefab.GetComponent<MeshFilter>().sharedMesh
        });

        // Call AddComponents to populate base entity with the components required
        // by Entities Graphics
        RenderMeshUtility.AddComponents(
            _skytrainEntity,
            _entityManager,
            desc,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        
    }
    private void UpdateVisibleSkytrainEntityTransform()
    {
        _entityManager.SetComponentData(_skytrainEntity, new LocalTransform
        {
            Position = this.transform.position,
            Scale = _skytrainEntityScale,
            Rotation = this.transform.rotation
        });
        
    }

    private void InstantiateLoadingZoneEntities()
    {
        if (_loadingZones._loadingZonePrefab != null) {
            // Create a RenderMeshDescription using the convenience constructor
            // with named parameters.
            var desc = new RenderMeshDescription(
                shadowCastingMode: ShadowCastingMode.Off,
                receiveShadows: false);

            // Create an array of mesh and material required for runtime rendering.
            //From prefab
            var renderMeshArray = new RenderMeshArray(new Material[] {
                _loadingZones._loadingZonePrefab.GetComponent<Renderer>().sharedMaterial
            }, new Mesh[] {
                _loadingZones._loadingZonePrefab.GetComponent<MeshFilter>().sharedMesh
            });

            List<string> entityMessages = new List<string>();
            entityMessages.Add("One message");
            entityMessages.Add("Two message");
            entityMessages.Add("Three message");

            for (int i = 0; i < _loadingZones._loadingZoneOffsets.Count; i++)
            {
                Vector3 offset = _loadingZones._loadingZoneOffsets[i];

                Entity loadingZoneEntity = _entityManager.CreateEntity(typeof(LocalToWorld));

                // Call AddComponents to populate base entity with the components required
                // by Entities Graphics
                RenderMeshUtility.AddComponents(
                    loadingZoneEntity,
                    _entityManager,
                    desc,
                    renderMeshArray,
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
                _entityManager.AddComponentData(loadingZoneEntity, new LocalTransform
                {
                    Position = this.transform.position + this.transform.rotation * offset,
                    Scale = _loadingZones._loadingZoneScale,
                    Rotation = this.transform.rotation
                });

                _entityManager.AddComponentData(loadingZoneEntity, new LoadingZoneComponent
                {
                    SkytrainEntity = _skytrainEntity
                });

                // Add physics stuff to enable physics
                //PhysicsFilter
                Unity.Physics.CollisionFilter colFilter = Unity.Physics.CollisionFilter.Default;
                // Would change what the trigger can collide with here
                //Material (for physics, not colour)
                Unity.Physics.Material colMaterial = Unity.Physics.Material.Default;
                colMaterial.CollisionResponse = Unity.Physics.CollisionResponsePolicy.RaiseTriggerEvents; // This makes it a trigger
                                                                                                          //PhysicsCollider
                BlobAssetReference<Unity.Physics.Collider> boxColliderBlob = Unity.Physics.BoxCollider.Create(new Unity.Physics.BoxGeometry
                {
                    Center = float3.zero,
                    BevelRadius = 0.05f,
                    Orientation = quaternion.identity,
                    Size = new float3(1, 1, 1)
                    
                },
                    colFilter,
                    colMaterial
                );
                _entityManager.AddComponentData(loadingZoneEntity, new Unity.Physics.PhysicsCollider { Value = boxColliderBlob });
                //PhysicsVelocity
                _entityManager.AddComponentData(loadingZoneEntity, new Unity.Physics.PhysicsVelocity
                { });
                //PhysicsMass
                //PhysicsWorldIndex
                _entityManager.AddSharedComponentManaged(loadingZoneEntity, new Unity.Physics.PhysicsWorldIndex
                {
                    Value = 0
                });

                // Add a message to identify the loading zone
                _entityManager.AddComponentData(loadingZoneEntity, new MessageComponent
                {
                    Message = entityMessages[i % entityMessages.Count]
                });

                //_entityManager.AddBuffer<StatefulCollisionEvent>(loadingZoneEntity); // For Collisions
                _entityManager.AddBuffer<StatefulTriggerEvent>(loadingZoneEntity); // For Triggers

                // If loading zone is in transit, add the "in transit" component
                if (_loadingZoneInTransit)
                {
                    _entityManager.AddComponent<LoadingZoneInTransitComponent>(loadingZoneEntity);
                }

                _loadingZoneEntities.Add(loadingZoneEntity);
                
            }

            DynamicBuffer<LoadingZoneReferenceOnSkytrain> loadingZoneBuffer = _entityManager.AddBuffer<LoadingZoneReferenceOnSkytrain>(_skytrainEntity);
            for (int i = 0; i < _loadingZoneEntities.Count; i++)
            {
                loadingZoneBuffer.Add(new LoadingZoneReferenceOnSkytrain
                {
                    LoadingZone = _loadingZoneEntities[i],
                });
            }
        }
        else {
            Debug.LogError("Was supposed to create a loading zone entity, but the skytrain lacked a necessary prefab");
            _loadingZoneErrorHappened = true;
        }
    }

    private void UpdateLoadingZonesTransforms()
    {
        if (!_loadingZoneErrorHappened)
        {
            for (int i = 0; i < _loadingZones._loadingZoneOffsets.Count; i++)
            {
                var offset = _loadingZones._loadingZoneOffsets[i];

                _entityManager.SetComponentData(_loadingZoneEntities[i], new LocalTransform
                {
                    Position = this.transform.position + this.transform.rotation * offset,
                    Scale = _loadingZones._loadingZoneScale,
                    Rotation = this.transform.rotation
                });
            }
        }
    }
}

[System.Serializable]
public class SkytrainLoadingZoneVariables
{
    public GameObject _loadingZonePrefab;
    public float _loadingZoneScale = 1.0f;
    public List<Vector3> _loadingZoneOffsets;
}

[System.Serializable]
public class SkytrainVisibleSkytrainVariables
{
    public bool _createVisibleSkytrainEntity = true;
    public GameObject _skytrainVisibleEntityPrefab;
}

[System.Serializable]
public class SkytrainMotionDetectionVariables
{
    public float _movementThreshold = 50;
    //public float _totalMovement = 0;
    //public int _currentBufferPosition = 0;
}

[System.Serializable]
public class SkytrainPassengerVariables
{
    public GameObject _passengerPrefab;
    public float _distanceBetweenPassengers;
}