using Microsoft.AspNetCore.Http.HttpResults;
using webserver.DTOs;
using webserver.Enums;
using webserver.Models;
using webserver.Repositories.RoomRepository;
using webserver.Utils;

namespace webserver.Services.RoomService
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly RedisHelper redisHelper;

        public RoomService(IRoomRepository roomRepository, RedisHelper redisHelper)
        {
            _roomRepository = roomRepository;
            this.redisHelper = redisHelper;
        }
        public async Task<ResponseWrapper<Res_GetRoomsDto>> GetRoomsAsync(int page, int pageSize)
        {
            //int skip = (page - 1) * pageSize;
            List<RoomDTO> temp_rooms = new List<RoomDTO>();

            var redis_rooms = await redisHelper.GetRoomsByStateAsync((int)RoomType.Waiting, page, pageSize);
            if (redis_rooms == null)
            {
                return ResponseWrapper<Res_GetRoomsDto>.Failure(ErrorType.BadRequest, "No rooms found");
            }
            foreach (var room in redis_rooms) {
                var temp_room = new RoomDTO();
                temp_room.Id = room.roomId;
                temp_room.Status = 1;
                temp_room.CurrentPlayers = room.currentPlayers;
                temp_rooms.Add(temp_room);
            }
            

            var rooms = await _roomRepository.GetRoomsAsync(temp_rooms.Select(room => room.Id).ToList());
            if (rooms.ErrorCode != Enums.DBErrorCode.Success)
            {
                return ResponseWrapper<Res_GetRoomsDto>.Failure(ErrorType.BadRequest, rooms.ErrorMessage);
            }
            foreach (var room in rooms.Data)
            {
                var temp_room = temp_rooms.FirstOrDefault(r => r.Id == room.Id);
                if (temp_room != null)
                {
                    temp_room.Name = room.RoomName;
                    temp_room.MaxPlayers = room.MaxPlayers;
                    temp_room.CreatedAt = room.CreatedAt;
                }
            }

            var res = new Res_GetRoomsDto
            {
                Rooms = temp_rooms
            };

            return ResponseWrapper<Res_GetRoomsDto>.Success(res, "Room list successfully");
        }
        public async Task<ResponseWrapper<Res_CreateRoomDto>> CreateRoomAsync(Req_CreateRoomDto req)
        {
            if (req == null)
            {
                return ResponseWrapper<Res_CreateRoomDto>.Failure(ErrorType.BadRequest, "Invalid request");
            }
            var room = new Room
            {
                RoomName = req.RoomName,
                MaxPlayers = 2,
                CreatedAt = DateTime.UtcNow
            };
            var result = await _roomRepository.CreateRoomAsync(room);
            if (result.ErrorCode != DBErrorCode.Success)
            {
                return ResponseWrapper<Res_CreateRoomDto>.Failure(ErrorType.BadRequest, result.ErrorMessage);
            }
            // Redis에 방 정보 저장
            await redisHelper.CreateRoomAsync(room.Id);

            var res = new Res_CreateRoomDto
            {
                Room = new RoomDTO
                {
                    Id = room.Id,
                    Name = room.RoomName,
                    MaxPlayers = room.MaxPlayers,
                    CreatedAt = room.CreatedAt
                }
            };

            return ResponseWrapper<Res_CreateRoomDto>.Success(res, "Room list successfully");
        }
    }
}
