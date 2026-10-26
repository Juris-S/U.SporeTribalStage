namespace SporeTribal.Buildings
{
    public class MainHut : Building
    {
        protected override void Collapse()
        {
            base.Collapse();
            if (Owner != null) Owner.Surrender();
        }
    }
}
