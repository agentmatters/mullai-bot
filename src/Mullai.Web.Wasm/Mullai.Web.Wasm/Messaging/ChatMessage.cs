using System;
using System.Collections.Generic;

namespace Mullai.Web.Wasm.Messaging;

public class ChatMessage
{
    public string Content { get; set; } = "";
    public bool IsUser { get; set; }
    public List<string> TaskUpdates { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
