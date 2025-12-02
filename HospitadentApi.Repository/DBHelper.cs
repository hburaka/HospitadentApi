using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.SqlClient;

namespace HospitadentApi.Repository
{
    public class DBHelper : IDisposable
    {
        private MySqlConnection _connection;
        private MySqlCommand _command;
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

        private void BaglantiAc()
        {
            if (_connection.State == ConnectionState.Closed)
                _connection.Open();
        }

        private void BaglantiKapat()
        {
            if (_connection.State == ConnectionState.Open && _transaction == null)
                _connection.Close();
        }

        public void BeginTransaction()
        {
            BaglantiAc();
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
                finally { _transactionCompleted = true; BaglantiKapat(); }
            }
        }

        public void RollbackTransaction()
        {
            if (_transaction != null && !_transactionCompleted)
            {
                try { _transaction.Rollback(); }
                catch (Exception ex) { throw new Exception("Error rolling back transaction", ex); }
                finally { _transactionCompleted = true; BaglantiKapat(); }
            }
        }

        public void ParametreEkle(string ParametreAdi, object ParametreDegeri)
        {
            try
            {
                _command.Parameters.AddWithValue(ParametreAdi, ParametreDegeri ?? DBNull.Value);
            }
            catch (Exception ex) { throw new Exception("Error adding parameter", ex); }
        }

        public void ParametreEkle(string ParametreAdi, object ParametreDegeri, MySqlDbType ParametreTipi, int ParametreBoyut)
        {
            try
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
            catch (Exception ex) { throw new Exception("Error adding parameter", ex); }
        }

        public void ParametreEkle(string ParametreAdi, object ParametreDegeri, MySqlDbType ParametreTipi)
        {
            try
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
            catch (Exception ex) { throw new Exception("Error adding parameter", ex); }
        }

        public void ParametreEkleOut(string ParametreAdi, MySqlDbType ParametreTipi, int ParametreBoyut)
        {
            try
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
            catch (Exception ex) { throw new Exception("Error adding output parameter", ex); }
        }

        public string ParemetreDegeriniGetir(string ParametreAdi)
        {
            return _command.Parameters[ParametreAdi]?.Value?.ToString() ?? string.Empty;
        }

        public void ParametreleriTemizle() => _command.Parameters.Clear();

        #region ExecuteReader

        public MySqlDataReader ExecuteReaderSp(string ProsedurAdi)
        {
            try
            {
                _command.CommandType = CommandType.StoredProcedure;
                _command.CommandText = ProsedurAdi;
                BaglantiAc();
                _reader = _command.ExecuteReader(CommandBehavior.CloseConnection);
                return _reader;
            }
            catch (Exception ex) { throw new Exception("Error executing reader", ex); }
        }

        public MySqlDataReader ExecuteReaderSql(string SqlString)
        {
            try
            {
                _command.CommandType = CommandType.Text;
                _command.CommandText = SqlString;
                BaglantiAc();
                _reader = _command.ExecuteReader(CommandBehavior.CloseConnection);
                return _reader;
            }
            catch (Exception ex) { throw new Exception("Error executing reader", ex); }
        }

        #endregion

        #region GetDataSet

        public DataSet GetDataSetBySp(string ProsedurAdi)
        {
            try
            {
                _command.CommandType = CommandType.StoredProcedure;
                _command.CommandText = ProsedurAdi;
                BaglantiAc();
                using var adpt = new MySqlDataAdapter(_command);
                var ds = new DataSet();
                adpt.Fill(ds);
                return ds;
            }
            catch (Exception ex) { throw new Exception("Error getting dataset", ex); }
            finally { BaglantiKapat(); }
        }

        public DataSet GetDataSetBySql(string SqlString)
        {
            try
            {
                _command.CommandType = CommandType.Text;
                _command.CommandText = SqlString;
                BaglantiAc();
                using var adpt = new MySqlDataAdapter(_command);
                var ds = new DataSet();
                adpt.Fill(ds);
                return ds;
            }
            catch (Exception ex) { throw new Exception("Error getting dataset", ex); }
            finally { BaglantiKapat(); }
        }

        #endregion

        #region ExecuteNonQuery

        public int ExecuteNonQuerySp(string ProsedurAdi)
        {
            object etkilenenSatirSayisi = 0;
            try
            {
                _command.CommandType = CommandType.StoredProcedure;
                _command.CommandText = ProsedurAdi;
                BaglantiAc();
                etkilenenSatirSayisi = _command.ExecuteNonQuery();
            }
            catch (Exception ex) { throw new Exception("Error executing non-query", ex); }
            finally { BaglantiKapat(); }
            return Convert.ToInt32(etkilenenSatirSayisi);
        }

        public int ExecuteNonQuerySql(string SqlString)
        {
            object etkilenenSatirSayisi = 0;
            try
            {
                _command.CommandType = CommandType.Text;
                _command.CommandText = SqlString;
                BaglantiAc();
                etkilenenSatirSayisi = _command.ExecuteNonQuery();
            }
            catch (Exception ex) { throw new Exception("Error executing non-query", ex); }
            finally { BaglantiKapat(); }
            return Convert.ToInt32(etkilenenSatirSayisi);
        }

        #endregion

        #region ExecuteScalar

        public int ExecuteScalarSp(string ProsedurAdi)
        {
            object identity = 0;
            try
            {
                _command.CommandType = CommandType.StoredProcedure;
                _command.CommandText = ProsedurAdi;
                BaglantiAc();
                identity = _command.ExecuteScalar();
            }
            catch (Exception ex) { throw new Exception("Error executing scalar", ex); }
            finally { BaglantiKapat(); }
            return Convert.ToInt32(identity);
        }

        public int ExecuteScalarSql(string SqlString)
        {
            object identity = 0;
            try
            {
                _command.CommandType = CommandType.Text;
                _command.CommandText = SqlString;
                BaglantiAc();
                identity = _command.ExecuteScalar();
            }
            catch (Exception ex) { throw new Exception("Error executing scalar", ex); }
            finally { BaglantiKapat(); }
            return Convert.ToInt32(identity);
        }

        #endregion

        public void ClearParameters() => _command.Parameters.Clear();
    }
}