namespace MovementTemplate
{
    /// <summary>Climbable wall: move up, down and sideways along the surface. Forward axis points into the wall.</summary>
    public class ClimbSurface : ClimbVolume
    {
        protected override void Reset()
        {
            base.Reset();
            freeStrafe = true;
        }
    }
}
