namespace Nebula.Events.Variation;

class BlackOut : GlobalEvent
{
    public float VisionRate = 1f;
    public float MinRate = 1f;

    public BlackOut(float duration, ulong option) : base(GlobalEvent.Type.BlackOut, duration, option)
    {
        MinRate = 1f * ((float)option / 100f);
        if (MinRate < 0f) MinRate = 0f;
        if (MinRate > 1f) MinRate = 1f;
    }

    public override void Update(float left)
    {
        if (left < 2f)
            VisionRate += (1f - MinRate) * 0.5f * Time.deltaTime;
        else
        {
            VisionRate -= (1f - MinRate) * 0.5f * Time.deltaTime;
            if (VisionRate < MinRate) VisionRate = MinRate;
        }
    }
}
