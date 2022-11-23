namespace Nebula.Images;
static class GlobalImage
{
    private static Sprite MeetingButtonLeft = null;
    private static Sprite MeetingButtonRight = null;

    public static Sprite GetMeetingButtonLeft()
    {
        if (MeetingButtonLeft) return MeetingButtonLeft;
        MeetingButtonLeft = Helpers.loadSpriteFromResources("Nebula.Resources.MeetingButtonLeft.png", 100f);
        return MeetingButtonLeft;
    }

    public static Sprite GetMeetingButtonRight()
    {
        if (MeetingButtonRight) return MeetingButtonRight;
        MeetingButtonRight = Helpers.loadSpriteFromResources("Nebula.Resources.MeetingButtonRight.png", 100f);
        return MeetingButtonRight;
    }
}
