using D12Canvas.Model;

namespace D12Canvas.Persistence;

public interface IBoardSerializer
{
    string Serialize(Board board);
    Board Deserialize(string json);
    PartialBoardDeserializeResult DeserializePartial(string json);
}
