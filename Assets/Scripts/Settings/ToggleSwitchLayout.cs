namespace BattleRoyale.Settings
{
    public static class ToggleSwitchLayout
    {
        public static float KnobX(float width, float knobSize, float padding, bool isOn)
        {
            float half = width * 0.5f;
            float innerLeft = -half + padding;
            float innerRight = half - padding;
            float radius = knobSize * 0.5f;
            float knobOff = innerLeft + radius;
            float knobOn = innerRight - radius;
            return isOn ? knobOn : knobOff;
        }
    }
}
