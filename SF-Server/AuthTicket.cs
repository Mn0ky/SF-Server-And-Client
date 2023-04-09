using System.Text;

namespace SF_Server;

public readonly struct AuthTicket : IEquatable<AuthTicket>
{
    public readonly byte[] Ticket;
    public readonly string TicketString;

    public AuthTicket(byte[] ticket)
    {
        Ticket = ticket;
        
        var authTicketString = new StringBuilder();
        foreach (var b in Ticket)
            authTicketString.Append($"{b:x2}");

        TicketString = authTicketString.ToString();
    }
    
    public bool Equals(AuthTicket other) => TicketString == other.TicketString;

    public override int GetHashCode() => TicketString != null ? TicketString.GetHashCode() : 0;

    public override string ToString() => TicketString;
}