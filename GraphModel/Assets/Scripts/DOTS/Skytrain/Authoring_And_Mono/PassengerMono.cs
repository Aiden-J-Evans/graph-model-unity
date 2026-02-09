using UnityEngine;
using Unity.Entities;

public class PassengerMono : MonoBehaviour
{
    public string name;
    public class PassengerBaker : Baker<PassengerMono>
    {
        public override void Bake(PassengerMono authoring)
        {
            AddComponent(new PassengerComponent { 
                PassengerName = authoring.name
            });
        }
    }
}
