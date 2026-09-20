using System;

namespace CaroShared.Contracts
{
    public class DrawOfferRequestDto
    {
        public string RoomId { get; set; } = string.Empty;
        public Guid MatchIdentity { get; set; }
    }

    public class DrawOfferEventDto
    {
        public string RoomId { get; set; } = string.Empty;
        public Guid MatchIdentity { get; set; }
        public Guid OfferIdentity { get; set; }
        public string OfferedByPlayerId { get; set; } = string.Empty;
        public string OfferedByPlayerName { get; set; } = string.Empty;
    }

    public class DrawResponseRequestDto
    {
        public string RoomId { get; set; } = string.Empty;
        public Guid MatchIdentity { get; set; }
        public Guid OfferIdentity { get; set; }
        public bool Accept { get; set; }
    }

    public class DrawOfferResolvedDto
    {
        public string RoomId { get; set; } = string.Empty;
        public Guid MatchIdentity { get; set; }
        public Guid OfferIdentity { get; set; }
        public bool Accepted { get; set; }
        public bool Cancelled { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
