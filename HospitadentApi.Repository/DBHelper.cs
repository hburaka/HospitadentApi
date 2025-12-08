using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace HospitadentApi.Repository
{
    public class DBHelper : IDisposable
    {
        private readonly MySqlConnection _connection;
        private readonly MySqlCommand _command;
        private MySqlDataReader? _reader;
        private MySqlTransaction? _transaction;
        private bool _transactionCompleted;

        public DBHelper(string connectionString)
        {
            _connection = new MySqlConnection(connectionString);
            _command = _connection.CreateCommand();
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _command?.Dispose();
            _transaction?.Dispose();
            _connection?.Dispose();
        }

        private void EnsureOpen()
        {
            if (_connection.State == ConnectionState.Closed)
                _connection.Open();
        }

        private async Task EnsureOpenAsync()
        {
            if (_connection.State == ConnectionState.Closed)
                await _connection.OpenAsync();
        }

        private void EnsureClose()
        {
            if (_connection.State == ConnectionState.Open && _transaction == null)
                _connection.Close();
        }

        public void BeginTransaction()
        {
            EnsureOpen();
            _transaction = _connection.BeginTransaction();
            _command.Transaction = _transaction;
            _transactionCompleted = false;
        }

        public void CommitTransaction()
        {
            if (_transaction != null && !_transactionCompleted)
            {
                try { _transaction.Commit(); }
                catch (Exception ex) { throw new Exception("Error committing transaction", ex); }
                finally { _transactionCompleted = true; EnsureClose(); }
            }
        }

        public void RollbackTransaction()
        {
            if (_transaction != null && !_transactionCompleted)
            {
                try { _transaction.Rollback(); }
                catch (Exception ex) { throw new Exception("Error rolling back transaction", ex); }
                finally { _transactionCompleted = true; EnsureClose(); }
            }
        }

        public void ParametreEkle(string ParametreAdi, object ParametreDegeri)
        {
            _command.Parameters.AddWithValue(ParametreAdi, ParametreDegeri ?? DBNull.Value);
        }

        public void ParametreEkle(string ParametreAdi, object ParametreDegeri, MySqlDbType ParametreTipi, int ParametreBoyut)
        {
            var p = new MySqlParameter
            {
                ParameterName = ParametreAdi,
                MySqlDbType = ParametreTipi,
                Size = ParametreBoyut,
                Direction = ParameterDirection.Input,
                Value = ParametreDegeri ?? DBNull.Value
            };
            _command.Parameters.Add(p);
        }

        public void ParametreEkle(string ParametreAdi, object ParametreDegeri, MySqlDbType ParametreTipi)
        {
            var p = new MySqlParameter
            {
                ParameterName = ParametreAdi,
                MySqlDbType = ParametreTipi,
                Direction = ParameterDirection.Input,
                Value = ParametreDegeri ?? DBNull.Value
            };
            _command.Parameters.Add(p);
        }

        public void ParametreEkleOut(string ParametreAdi, MySqlDbType ParametreTipi, int ParametreBoyut)
        {
            var p = new MySqlParameter
            {
                ParameterName = ParametreAdi,
                MySqlDbType = ParametreTipi,
                Size = ParametreBoyut,
                Direction = ParameterDirection.Output
            };
            _command.Parameters.Add(p);
        }

        public string ParemetreDegeriniGetir(string ParametreAdi)
            => _command.Parameters[ParametreAdi]?.Value?.ToString() ?? string.Empty;

        public void ParametreleriTemizle() => _command.Parameters.Clear();

        #region Sync Methods (kept for compatibility)

        public MySqlDataReader ExecuteReaderSp(string ProsedurAdi)
        {
            _command.CommandType = CommandType.StoredProcedure;
            _command.CommandText = ProsedurAdi;
            EnsureOpen();
            _reader = _command.ExecuteReader(CommandBehavior.CloseConnection);
            return _reader;
        }

        public MySqlDataReader ExecuteReaderSql(string SqlString)
        {
            _command.CommandType = CommandType.Text;
            _command.CommandText = SqlString;
            EnsureOpen();
            _reader = _command.ExecuteReader(CommandBehavior.CloseConnection);
            return _reader;
        }

        public DataSet GetDataSetBySp(string ProsedurAdi)
        {
            _command.CommandType = CommandType.StoredProcedure;
            _command.CommandText = ProsedurAdi;
            EnsureOpen();
            using var adpt = new MySqlDataAdapter(_command);
            var ds = new DataSet();
            adpt.Fill(ds);
            EnsureClose();
            return ds;
        }

        public DataSet GetDataSetBySql(string SqlString)
        {
            _command.CommandType = CommandType.Text;
            _command.CommandText = SqlString;
            EnsureOpen();
            using var adpt = new MySqlDataAdapter(_command);
            var ds = new DataSet();
            adpt.Fill(ds);
            EnsureClose();
            return ds;
        }

        public int ExecuteNonQuerySp(string ProsedurAdi)
        {
            _command.CommandType = CommandType.StoredProcedure;
            _command.CommandText = ProsedurAdi;
            EnsureOpen();
            var res = _command.ExecuteNonQuery();
            EnsureClose();
            return Convert.ToInt32(res);
        }

        public int ExecuteNonQuerySql(string SqlString)
        {
            _command.CommandType = CommandType.Text;
            _command.CommandText = SqlString;
            EnsureOpen();
            var res = _command.ExecuteNonQuery();
            EnsureClose();
            return Convert.ToInt32(res);
        }

        public int ExecuteScalarSp(string ProsedurAdi)
        {
            _command.CommandType = CommandType.StoredProcedure;
            _command.CommandText = ProsedurAdi;
            EnsureOpen();
            var identity = _command.ExecuteScalar();
            EnsureClose();
            return Convert.ToInt32(identity);
        }

        public int ExecuteScalarSql(string SqlString)
        {
            _command.CommandType = CommandType.Text;
            _command.CommandText = SqlString;
            EnsureOpen();
            var identity = _command.ExecuteScalar();
            EnsureClose();
            return Convert.ToInt32(identity);
        }

        #endregion

        #region Async Methods (recommended for scalability)

        public async Task<MySqlDataReader> ExecuteReaderSqlAsync(string sql)
        {
            _command.CommandType = CommandType.Text;
            _command.CommandText = sql;
            await EnsureOpenAsync();
            _reader = await _command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            return _reader;
        }

        public async Task<int> ExecuteNonQuerySqlAsync(string sql)
        {
            _command.CommandType = CommandType.Text;
            _command.CommandText = sql;
            await EnsureOpenAsync();
            var res = await _command.ExecuteNonQueryAsync();
            EnsureClose();
            return Convert.ToInt32(res);
        }

        public async Task<object?> ExecuteScalarSqlAsync(string sql)
        {
            _command.CommandType = CommandType.Text;
            _command.CommandText = sql;
            await EnsureOpenAsync();
            var res = await _command.ExecuteScalarAsync();
            EnsureClose();
            return res;
        }

        #endregion

        public void ClearParameters() => _command.Parameters.Clear();
    }
}