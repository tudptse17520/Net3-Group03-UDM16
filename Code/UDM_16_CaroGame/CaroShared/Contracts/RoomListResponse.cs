using System.Collections.Generic;

namespace CaroShared.Contracts
{
    // Phản hồi danh sách phòng đang hoạt động
    public class RoomListResponse
    {
        public List<RoomDto> Rooms { get; set; } = new();
    }
}
