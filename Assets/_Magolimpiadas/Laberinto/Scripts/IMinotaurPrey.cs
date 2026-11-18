using UnityEngine;

public interface IMinotaurPrey
{
    Transform Body { get; }
    Vector3 Position { get; }
    bool CanBeHunted { get; }
    void Catch(float respawnSeconds);
}
