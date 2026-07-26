namespace D12Canvas.History;

public interface ICommand
{
    void Apply();

    void Undo();
}
