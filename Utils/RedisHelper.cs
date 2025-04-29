using StackExchange.Redis;

namespace webserver.Utils
{
    public class RedisHelper
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger _logger;

        public RedisHelper(IConnectionMultiplexer redis, ILogger<RedisHelper> logger)
        {
            _redis = redis;
            _logger = logger;
        }
        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                var redis = _redis.GetDatabase();
                return await redis.StringSetAsync(key, value, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis string set error - key: {Key}", key);
                return false;
            }

        }
        public async Task<string> StringGetAsync(string key)
        {
            try
            {
                var redis = _redis.GetDatabase();
                return await redis.StringGetAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis string get error - key: {Key}", key);
                return null;
            }

        }
        public async Task<bool> KeyExistsAsync(string key)
        {
            try
            {
                var redis = _redis.GetDatabase();
                return await redis.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis key exists error - key: {Key}", key);
                return false;
            }
        }
        public async Task<bool> KeyDeleteAsync(string key)
        {
            try
            {
                var redis = _redis.GetDatabase();
                return await redis.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis key delete error - key: {Key}", key);
                return false;
            }

        }
        public async Task<bool> KeyExpireAsync(string key, TimeSpan expiry)
        {
            try
            {
                var db = _redis.GetDatabase();
                return await db.KeyExpireAsync(key, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis KeyExpire 오류 발생 - 키: {Key}", key);
                return false;
            }
        }


        // 방 추가
        public async Task AddRoomToStateAsync(int roomId, string state, int currentPlayers)
        {
            var redis = _redis.GetDatabase();
            string key = $"rooms:{state}";

            // 상태별 Sorted Set에 방 추가
            await redis.SortedSetAddAsync(key, roomId.ToString(), currentPlayers);
        }

        // 방 상태 변경
        public async Task UpdateRoomStateAsync(int roomId, string oldState, string newState, int currentPlayers)
        {
            var redis = _redis.GetDatabase();

            // 기존 상태에서 제거
            if (!string.IsNullOrEmpty(oldState))
            {
                string oldKey = $"rooms:{oldState}";
                await redis.SortedSetRemoveAsync(oldKey, roomId.ToString());
            }

            // 새로운 상태에 추가
            string newKey = $"rooms:{newState}";
            await redis.SortedSetAddAsync(newKey, roomId.ToString(), currentPlayers);

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



            var redis = _redis.GetDatabase();
            string key = $"rooms:{stateString}";

            // 페이지네이션 계산
            int start = (page - 1) * pageSize;
            int stop = start + pageSize - 1;

            // 상태별 Sorted Set에서 방 ID 조회
            var entries = await redis.SortedSetRangeByRankWithScoresAsync(key, start, stop, Order.Ascending);

            return entries.Select(entry => (roomId: int.Parse(entry.Element), currentPlayers: (int)entry.Score)).ToList();

        }
        public async Task<int> GetCurrentPlayersAsync(int roomId)
        {
            var redis = _redis.GetDatabase();
            string key = $"room:{roomId}:currentPlayers";
            var value = await redis.StringGetAsync(key);
            return value.HasValue ? (int)value : 0;
        }

        public async Task UpdateCurrentPlayersAsync(int roomId, int currentPlayers)
        {
            var redis = _redis.GetDatabase();
            string key = $"room:{roomId}:currentPlayers";
            await redis.StringSetAsync(key, currentPlayers);
        }

        public async Task CreateRoomAsync(int roomId)
        {
            var redis = _redis.GetDatabase();
            string key = $"rooms:waiting";
            // 방 ID를 Sorted Set에 추가
            await redis.SortedSetAddAsync(key, roomId.ToString(), 1);
        }

        // 모든 상태에서 특정 방 제거
        public async Task RemoveRoomFromAllStatesAsync(int roomId)
        {
            var redis = _redis.GetDatabase();

            // 모든 상태에서 방 ID 제거
            await redis.SortedSetRemoveAsync("rooms:waiting", roomId.ToString());
            await redis.SortedSetRemoveAsync("rooms:playing", roomId.ToString());
            await redis.SortedSetRemoveAsync("rooms:finished", roomId.ToString());
        }

    }
}
