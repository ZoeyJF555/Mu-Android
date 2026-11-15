namespace Mu.Core.Game;

/// <summary>De dónde viene un mensaje del chat, para pintarlo de un color u otro.</summary>
public enum ChatKind
{
    /// <summary>Avisos del juego (subida de nivel, muerte...).</summary>
    System,

    /// <summary>Lo que dice el jugador.</summary>
    Player,

    /// <summary>Lo que dicen los bots.</summary>
    Bot,

    /// <summary>Lo que dicen los NPC.</summary>
    Npc,
}

/// <summary>Una línea del chat.</summary>
public readonly record struct ChatMessage(ChatKind Kind, string Text);

/// <summary>
/// Guarda las últimas líneas del chat. Es una cola con tope: al llegar al
/// límite se tira la más vieja, que es lo que hace un chat de verdad.
/// </summary>
public sealed class ChatLog
{
    /// <summary>Cuántas líneas se conservan.</summary>
    public const int Capacity = 40;

    private readonly Queue<ChatMessage> _messages = new();

    /// <summary>Las líneas guardadas, de la más vieja a la más nueva.</summary>
    public IReadOnlyCollection<ChatMessage> Messages => _messages;

    /// <summary>Escribe una línea.</summary>
    public void Add(ChatKind kind, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _messages.Enqueue(new ChatMessage(kind, text));
        while (_messages.Count > Capacity)
        {
            _messages.Dequeue();
        }
    }

    /// <summary>Las últimas líneas que caben en una ventana de un alto dado.</summary>
    /// <param name="maxLines">Cuántas líneas se quieren.</param>
    public IReadOnlyList<ChatMessage> Tail(int maxLines)
    {
        if (maxLines <= 0)
        {
            return [];
        }

        var all = _messages.ToArray();
        if (all.Length <= maxLines)
        {
            return all;
        }

        return all[^maxLines..];
    }
}
