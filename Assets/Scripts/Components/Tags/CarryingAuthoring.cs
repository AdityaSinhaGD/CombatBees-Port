using Unity.Entities;

[GenerateAuthoringComponent]
public struct IsCarryingResource : IComponentData
{
    public Entity Value;
}