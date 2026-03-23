using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

/// <summary>
/// Passenger component tag
/// </summary>
public struct Passenger : IComponentData
{
    public float TimeWaiting;
}

/// <summary>
/// Holds the value of movement speed for the passengers
/// </summary>
public struct MoveSpeed : IComponentData
{
    public float Value;
}

/// <summary>
/// Destination component for passengers
/// </summary>
public struct Destination : IComponentData
{
    public float3 Value;
}

/// <summary>
/// Handles the time of fade in for passengers
/// </summary>
public struct FadeIn : IComponentData
{
    public float Duration;
    public float Elapsed;
}

/// <summary>
/// Holds the colour value of the passenger while fading
/// </summary>
[MaterialProperty("_BaseColor")]
public struct URPMaterialPropertyBaseColor : IComponentData
{
    public float4 Value; // rgb = color, a = fade
}

/// <summary>
/// Radius component for crowd simulation system
/// </summary>
public struct Radius : IComponentData
{
    public float Value;
}

/// <summary>
/// Tag for skytrain stations
/// </summary>
public struct StationTag : IComponentData { }

/// <summary>
/// Station entered tag for passengers
/// </summary>
public struct StationEntered : IComponentData { }


