using UnityEngine;

public enum WeatherKind { None, Fog, Snow, Rain, Leaves, Sparkles, Blizzard }

// Put on a checkpoint: the weather that starts when the player reaches it.
public class CheckpointEffect : MonoBehaviour
{
    public WeatherKind weather = WeatherKind.Fog;
}
