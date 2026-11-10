namespace MovementTemplate
{
    /// <summary>Vertical-only climbing. Place a trigger box in front of the ladder, forward axis pointing into it.</summary>
    public class LadderVolume : ClimbVolume
    {
        protected override void Reset()
        {
            base.Reset();
            freeStrafe = false;
        }
    }
}
