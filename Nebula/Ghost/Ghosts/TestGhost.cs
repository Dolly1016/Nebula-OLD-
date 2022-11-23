namespace Nebula.Ghost.Ghosts;

class TestGhost : Ghost
{
    public TestGhost()
    {
        AddAI(new AI.AI_FarthestSabotageForPlayers(0, 1f));
        AddAI(new AI.AI_RedNoise(64, 0.5f, NoiseDestination.SabotageMood, false));

        AddAI(new AI.AI_HideDeadBodyDoorMood(0, 0.8f, 1));
        AddAI(new AI.AI_PlayerDoorMood(0, 0.9f, 2));
        AddAI(new AI.AI_RedNoise(64, 0.7f, NoiseDestination.DoorMood, false));
    }
}