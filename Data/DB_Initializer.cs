using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using webserver.Utils;
using webserver.Enums;
using MySqlConnector;

namespace webserver.Data
{
    public class DB_Initializer
    {
        private readonly ILogger<DB_Initializer> _logger;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IConfiguration _configuration;

        public DB_Initializer(
            ILogger<DB_Initializer> logger,
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _configuration = configuration;
        }


        public async Task<DBResult<bool>> InitializeDatabase()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Connection string is null or empty");
                    return DBResult<bool>.Fail(DBErrorCode.DatabaseConnectionError, "Connection string is null or empty");
                }

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                _logger.LogInformation("DB Connection Success {connectionString}", connectionString);

                return DBResult<bool>.Success(true);
                
                
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB Connection Error");
                return DBResult<bool>.Fail(DBErrorCode.DatabaseConnectionError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB Unknown Error");
                return DBResult<bool>.Fail(DBErrorCode.UnknownError, ex.Message);
            }
        }   

        /// <summary>
        /// 데이터베이스 작업을 수행하는 람다 함수를 실행합니다.
        /// </summary>
        /// <typeparam name="T">반환 타입</typeparam>
        /// <param name="operation">실행할 데이터베이스 작업</param>
        /// <returns>작업 결과</returns>
        public async Task<DBResult<T>> ExecuteLambda<T>(Func<ApplicationDbContext, Task<T>> operation)
        {
            // using 문을 사용하여 컨텍스트가 자동으로 닫히게 하자. 세션을 물고있을 필요 없음.
            using var context = await _contextFactory.CreateDbContextAsync();
            try
            {
                var result = await operation(context);
                return DBResult<T>.Success(result);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency Error");
                return DBResult<T>.Fail(DBErrorCode.ConcurrencyError, ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB Update Error");
                return DBResult<T>.Fail(DBErrorCode.UpdateError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB Unknown Error");
                return DBResult<T>.Fail(DBErrorCode.UnknownError, ex.Message);
            }
            // using 블록을 벗어나면 context는 자동으로 Dispose.
        }

        /// <summary>
        /// 다중 쿼리 트랜잭션 처리 - 여러 쿼리를 하나의 트랜잭션으로 묶어 실행하며, 중간에 오류 발생 시 롤백합니다.
        /// </summary>
        /// <typeparam name="T">반환될 데이터 타입</typeparam>
        /// <param name="actions">트랜잭션으로 실행할 작업들</param>
        /// <returns>작업 결과와 데이터</returns>
        public async Task<DBResult<T>> ExecuteLambdaTransaction<T>(Func<ApplicationDbContext, IDbContextTransaction, Task<T>> actions)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();
            
            try
            {
                // 트랜잭션 내에서 작업 실행
                var result = await actions(context, transaction);
                
                // 트랜잭션 커밋
                await transaction.CommitAsync();
                
                return DBResult<T>.Success(result);
            }
            catch (DbUpdateException ex)
            {
                // 롤백
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction Query Error - Rollback");
                return DBResult<T>.Fail(DBErrorCode.QueryError, ex.Message);
            }
            catch (DbException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transactioning DB Connection Error - Rollback");
                return DBResult<T>.Fail(DBErrorCode.ConnectionError, ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transactioning Unknown Error - Rollback");
                return DBResult<T>.Fail(DBErrorCode.UnknownError, ex.Message);
            }
        }
    }
}
