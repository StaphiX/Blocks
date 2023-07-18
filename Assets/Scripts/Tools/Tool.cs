public enum ToolState
{
    None,
    Active,
    Finished,
}

public interface ITool
{
    public void Update();
    public void Finish();
}

public class Tool : ITool
{
    protected ToolState state = ToolState.Active;
    public virtual void Update() { }
    public virtual void Finish() { state = ToolState.Finished; }
    public virtual void UpdateUI() { }
    public virtual void HandleInput() { }
}

