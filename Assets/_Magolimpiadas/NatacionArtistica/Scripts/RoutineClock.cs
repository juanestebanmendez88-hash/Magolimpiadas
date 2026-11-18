using UnityEngine;

public class RoutineClock
{
    double dspStart;
    float secondsPerBeat = 0.6f;
    float latency;
    double lastDsp = -1, lastReal;

    public float SecondsPerBeat => secondsPerBeat;

    public void Setup(double dspStart, float secondsPerBeat, float latency)
    {
        this.dspStart = dspStart;
        this.secondsPerBeat = Mathf.Max(0.05f, secondsPerBeat);
        this.latency = latency;
    }

    public double Now()
    {
        double dsp = AudioSettings.dspTime;
        double real = Time.unscaledTimeAsDouble;
        if (dsp != lastDsp) { lastDsp = dsp; lastReal = real; }
        return lastDsp + (real - lastReal);
    }

    public float Beat => (float)((Now() - dspStart - latency) / secondsPerBeat);

    public double BeatDsp(float beat) => dspStart + beat * secondsPerBeat;
}
