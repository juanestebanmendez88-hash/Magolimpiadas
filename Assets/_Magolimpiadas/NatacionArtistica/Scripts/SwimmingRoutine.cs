using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Magolimpiadas/Natación artística/Rutina", fileName = "RutinaNatacion")]
public class SwimmingRoutine : ScriptableObject
{
    public AudioClip song;
    public float bpm = 100f;
    public float songOffset;
    public List<RoutineFigure> figures = new List<RoutineFigure>();

    public float SecondsPerBeat => 60f / Mathf.Max(30f, bpm);
}
