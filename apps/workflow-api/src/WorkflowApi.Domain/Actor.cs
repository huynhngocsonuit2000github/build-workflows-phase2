namespace WorkflowApi.Domain;

/// <summary>Loại bên kích hoạt một chuyển trạng thái.</summary>
public enum ActorType
{
    N8n,
    Agent,
    Human,
    Ci,
    System
}

/// <summary>Ai kích hoạt: loại (để kiểm tra quyền) và định danh (để ghi lịch sử).</summary>
public sealed record Actor
{
    public ActorType Type { get; }
    public string Id { get; }

    public Actor(ActorType type, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Actor id is required.", nameof(id));
        }

        Type = type;
        Id = id.Trim();
    }

    public override string ToString() => $"{Type}:{Id}";
}
