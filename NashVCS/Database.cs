using System;
using System.Data.SQLite;
using System.IO;

namespace NashVCS
{
    /// <summary>
    /// Created by Nahom on 6/26/2016
    /// </summary>
    class Database : IDisposable
    {
        private string DB_NAME = "";
        private SQLiteConnection m_dbconnection = new SQLiteConnection();

        public Database(string db_name, bool hidden = true)
        {
            DB_NAME = db_name;

            //for registration
            if (!File.Exists(DB_NAME))
            {
                SQLiteConnection.CreateFile(DB_NAME);
            }

            if (hidden)
            {
                File.SetAttributes(DB_NAME, FileAttributes.Hidden);
            }

            m_dbconnection = new SQLiteConnection(string.Format("{0}={1};{2}={3};", "Data Source", DB_NAME, "Version", "3"));
            m_dbconnection.Open();
        }

        public void ExecuteNonQuery(string sql)
        {
            SQLiteCommand command = new SQLiteCommand(sql, m_dbconnection);
            command.ExecuteNonQuery();
        }

        public SQLiteDataReader ExecuteQuery(string sql)
        {
            return new SQLiteCommand(sql, m_dbconnection).ExecuteReader();
        }

        public void CloseDatabase()
        {
            m_dbconnection.Close();
        }


        public void Dispose()
        {
            CloseDatabase();
        }

    }
}
