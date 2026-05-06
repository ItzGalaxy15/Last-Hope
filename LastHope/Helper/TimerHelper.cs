namespace Last_Hope.Helpers;

public static class TimerHelper
{
    public static float DecreaseTimer(float value, float deltaTime)
    {
        if (value > 0f)
        {
            return value -= deltaTime;
        }
        return 0f;
    }
}