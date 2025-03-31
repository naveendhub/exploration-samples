using System;
using System.Data;
using System.Data.SQLite;

namespace FastRepository {
    internal class SqliteRepositoryAccess {

        const string ConnectionString = "Data Source=InMemorySample;Mode=Memory;Cache=Shared";
        private readonly SQLiteConnection sqlConnection = null;

        public SqliteRepositoryAccess() {

            sqlConnection = new SQLiteConnection(ConnectionString);
        }

        internal void StoreKeyToSqlite(string sopInstanceUid, string mmfKey) {
            try {
                sqlConnection.Open();

                using (var sqLiteCommand = new SQLiteCommand(sqlConnection)) {

                    sqLiteCommand.CommandText =
                        "INSERT INTO images (" +
                        "sopInstanceUid, HeaderKey, PixelKey,tags4,tags5,tags6,tags7,tags8,tags9,tags10," +
                        "tags11,tags12,tags13,tags14,tags15,tags16,tags17,tags18,tags19,tags20 )" +
                        " VALUES (@sopInstanceUid, @HeaderKey, @PixelKey, @tags4,@tags5,@tags6,@tags7,@tags8,@tags9," +
                        "@tags10,@tags11,@tags12,@tags13,@tags14,@tags15,@tags16,@tags17,@tags18,@tags19,@tags20)";


                    AddParameter(sqLiteCommand, "@sopInstanceUid", DbType.String, sopInstanceUid);
                    AddParameter(sqLiteCommand, "@HeaderKey", DbType.String, mmfKey);
                    AddParameter(sqLiteCommand, "@PixelKey", DbType.String, "pixel_" + mmfKey);

                    AddParameter(sqLiteCommand, "@tags4", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags5", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags6", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags7", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags8", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags9", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags10", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags11", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags12", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags13", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags14", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags15", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags16", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags17", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags18", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags19", DbType.String, Guid.NewGuid().ToString());
                    AddParameter(sqLiteCommand, "@tags20", DbType.String, Guid.NewGuid().ToString());

                    sqLiteCommand.ExecuteNonQuery();
                }

            } catch (Exception e) {
                Console.WriteLine(e);

            } finally {
                sqlConnection.Close();
            }

        }

        internal string ReadARecordFromRepository(string mmfkey) {
            string sopInstanceUid = String.Empty;
            try {
                sqlConnection.Open();
                using (var sqLiteCommand = new SQLiteCommand(sqlConnection)) {
                    string stm = "SELECT * FROM images where [HeaderKey] = @HeaderKey";

                    sqLiteCommand.CommandText = stm;
                    var parameter = sqLiteCommand.CreateParameter();
                    parameter.ParameterName = "@HeaderKey";
                    parameter.Value = mmfkey;
                    sqLiteCommand.Parameters.Add(parameter);

                    using (var rdr = sqLiteCommand.ExecuteReader()) {
                        while (rdr.Read()) {
                            sopInstanceUid = rdr.GetString(0);
                        }
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e);

            } finally {
                sqlConnection.Close();
            }

            return sopInstanceUid;
        }

        internal void CreateTables() {
            try {
                sqlConnection.Open();
                using (var sqLiteCommand = new SQLiteCommand(sqlConnection)) {
                    sqLiteCommand.CommandText = "DROP TABLE IF EXISTS images";
                    sqLiteCommand.ExecuteNonQuery();
                    sqLiteCommand.CommandText = @"CREATE TABLE images(
                        sopInstanceUid TEXT PRIMARY KEY, HeaderKey TEXT, PixelKey TEXT, tags4 TEXT, tags5 TEXT, tags6 TEXT, tags7 TEXT, tags8 TEXT, tags9 TEXT, tags10 TEXT,tags11 TEXT, tags12 TEXT, 
                        tags13 TEXT, tags14 TEXT, tags15 TEXT, tags16 TEXT, tags17 TEXT ,tags18 TEXT, tags19 TEXT, tags20 TEXT )";
                    sqLiteCommand.ExecuteNonQuery();
                }
            } catch (SQLiteException ex) {
                Console.WriteLine("Exception is creating the table " + ex);
            } finally {
                sqlConnection.Close();
            }
        }

        private static IDbDataParameter AddParameter(IDbCommand command, string paramName, DbType type, object value) {
            IDbDataParameter parameter = command.CreateParameter();
            parameter.ParameterName = paramName;
            parameter.DbType = type;
            if (value != null)
                parameter.Value = value;
            else
                parameter.Value = DBNull.Value;
            command.Parameters.Add(parameter);
            return parameter;
        }
    }
}
