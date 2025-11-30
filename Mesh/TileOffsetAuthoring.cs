using Unity.Entities;
using UnityEngine;

// A MonoBehaviour to create our singleton entity in the scene
public class TileOffsetAuthoring : MonoBehaviour
{
    // The class Baker will run at build time and create the entity
    class Baker : Baker<TileOffsetAuthoring>
    {
        public override void Bake(TileOffsetAuthoring authoring)
        {
            // We don't need to add any components here, as we will manage the data at runtime.
            // We just need an entity to exist. We will add the component in TileManager.
            // Adding a tag component makes it easy to find.
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new TileOffsetSingletonTag());
        }
    }
}

// A simple tag to identify our singleton entity
public struct TileOffsetSingletonTag : IComponentData { }