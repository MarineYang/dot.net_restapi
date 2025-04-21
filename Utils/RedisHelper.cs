using StackExchange.Redis;

namespace webserver.Utils
{
    public class RedisHelper
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisHelper(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }
        public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(key, value, expiry);
        }
        public async Task<string> GetStringAsync(string key)
        {
            var db = _redis.GetDatabase();
            return await db.StringGetAsync(key);
        }
        public async Task<bool> KeyExistsAsync(string key)
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        public async Task<bool> DeleteAsync(string key)
        {
            var db = _redis.GetDatabase();
            return await db.KeyDeleteAsync(key);
        }

        // 방 추가
        public async Task AddRoomToStateAsync(int roomId, string state, int currentPlayers)
        {
            var db = _redis.GetDatabase();
            string key = $"rooms:{state}";

            // 상태별 Sorted Set에 방 추가
            await db.SortedSetAddAsync(key, roomId.ToString(), currentPlayers);
        }

        // 방 상태 변경
        public async Task UpdateRoomStateAsync(int roomId, string oldState, string newState, int currentPlayers)
        {
            var db = _redis.GetDatabase();

            // 기존 상태에서 제거
            if (!string.IsNullOrEmpty(oldState))
            {
                string oldKey = $"rooms:{oldState}";
                await db.SortedSetRemoveAsync(oldKey, roomId.ToString());
            }

            // 새로운 상태에 추가
            string newKey = $"rooms:{newState}";
            await db.SortedSetAddAsync(newKey, roomId.ToString(), currentPlayers);

        }
        
        // 특정 상태의 방 목록 조회 
        public async Task<List<(int roomId, int currentPlayers)>> GetRoomsByStateAsync(int state, int page, int pageSize)
        {
            string stateString = state switch
            {
                1 => "waiting",
                2 => "playing",
                3 => "finished",
                _ => throw new ArgumentException("Invalid state")
            };



            var db = _redis.GetDatabase();
            string key = $"rooms:{stateString}";

            // 페이지네이션 계산
            int start = (page - 1) * pageSize;
            int stop = start + pageSize - 1;

            // 상태별 Sorted Set에서 방 ID 조회
            var entries = await db.SortedSetRangeByRankWithScoresAsync(key, start, stop, Order.Ascending);

            return entries.Select(entry => (roomId: int.Parse(entry.Element), currentPlayers: (int)entry.Score)).ToList();

        }

        public async Task CreateRoomAsync(int roomId)
        {
            var db = _redis.GetDatabase();
            string key = $"rooms:waiting";
            // 방 ID를 Sorted Set에 추가
            await db.SortedSetAddAsync(key, roomId.ToString(), 1);
        }

        // 모든 상태에서 특정 방 제거
        public async Task RemoveRoomFromAllStatesAsync(int roomId)
        {
            var db = _redis.GetDatabase();

            // 모든 상태에서 방 ID 제거
            await db.SortedSetRemoveAsync("rooms:waiting", roomId.ToString());
            await db.SortedSetRemoveAsync("rooms:playing", roomId.ToString());
            await db.SortedSetRemoveAsync("rooms:finished", roomId.ToString());
        }

    }
}
