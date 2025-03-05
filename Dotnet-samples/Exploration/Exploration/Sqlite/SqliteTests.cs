using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exploration.Sqlite {

    internal static class SqliteTests {
        public static void Run() {
            string dbPath = Path.Combine(Path.GetTempPath(),"test_database.db");

            // Create a test database with multiple tables
            CreateTestDatabase(dbPath);

            // Corrupt a specific table (KeyStore) by damaging its b-tree page
            //CorruptSpecificTable(dbPath, "KeyStore");

            CorruptByRemovingFromSqliteMaster(dbPath, "KeyStore");

            TableIntegrityCheck(dbPath);

            

            // Verify corruption: Try to read from tables
            VerifyCorruption(dbPath);
        }

        private static void TableIntegrityCheck(string dbPath)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand("PRAGMA quick_check('KeyStore');", connection)) {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        /* Expected to possibly fail */
                        Console.WriteLine($"Table corrupted");

                    }
                }
            }
        }

        static void CreateTestDatabase(string dbPath) {
            // Delete existing database if it exists
            if (File.Exists(dbPath))
                File.Delete(dbPath);

            // Create and populate the database
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;")) {
                connection.Open();

                // Create KeyStore table (the one we'll corrupt)
                using (var command = new SQLiteCommand(@"
                    CREATE TABLE KeyStore (
                        KeyId INTEGER PRIMARY KEY AUTOINCREMENT,
                        EncryptedKey TEXT NOT NULL,
                        InitializationVector TEXT NOT NULL,
                        CreationTime DATETIME DEFAULT CURRENT_TIMESTAMP
                    );", connection)) {
                    command.ExecuteNonQuery();
                }

                // Insert sample data
                using (var command = new SQLiteCommand(@"
                    INSERT INTO KeyStore (EncryptedKey, InitializationVector) VALUES 
                    ('encrypted_data_1', 'iv_1'),
                    ('encrypted_data_2', 'iv_2'),
                    ('encrypted_data_3', 'iv_3');", connection)) {
                    command.ExecuteNonQuery();
                }

                // Create another table (that will remain intact)
                using (var command = new SQLiteCommand(@"
                    CREATE TABLE UserSettings (
                        SettingId INTEGER PRIMARY KEY AUTOINCREMENT,
                        SettingName TEXT NOT NULL,
                        SettingValue TEXT,
                        LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
                    );", connection)) {
                    command.ExecuteNonQuery();
                }

                // Insert sample data into the second table
                using (var command = new SQLiteCommand(@"
                    INSERT INTO UserSettings (SettingName, SettingValue) VALUES 
                    ('Theme', 'Dark'),
                    ('Language', 'English'),
                    ('NotificationsEnabled', 'true');", connection)) {
                    command.ExecuteNonQuery();
                }

                Console.WriteLine("Test database created successfully.");
            }
        }

        static void CorruptSpecificTable(string dbPath, string tableName) {
            // First, get the page number for the table
            int pageNumber = GetTablePageNumber(dbPath, tableName);

            if (pageNumber <= 0) {
                Console.WriteLine($"Could not find page for table {tableName}");
                return;
            }

            Console.WriteLine($"Table {tableName} located at page {pageNumber}");

            // Now corrupt that specific page in the database file
            using (FileStream fs = new FileStream(dbPath, FileMode.Open, FileAccess.ReadWrite)) {
                // SQLite pages are typically 4096 bytes
                int pageSize = 4096;

                // Seek to the beginning of our target page
                fs.Seek(pageSize * (pageNumber - 1) + 100, SeekOrigin.Begin);

                // Write some garbage data to corrupt the b-tree structure
                byte[] garbage = new byte[20];
                new Random().NextBytes(garbage);
                fs.Write(garbage, 0, garbage.Length);

                Console.WriteLine($"Corrupted page {pageNumber} for table {tableName}");
            }
        }

        static int GetTablePageNumber(string dbPath, string tableName) {
            // This is a hacky way to find the page - in a real scenario, you would use the sqlite_master table
            // But for simplicity and since we're just corrupting for testing, we'll use PRAGMA
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;")) {
                connection.Open();

                using (var command = new SQLiteCommand($"PRAGMA table_info({tableName});", connection)) {
                    try {
                        // Just execute a query against the table to make sure it's in the cache
                        command.ExecuteNonQuery();
                    } catch { }
                }

                // Get the page number from sqlite_master
                using (var command = new SQLiteCommand($"SELECT rootpage FROM sqlite_master WHERE type='table' AND name='{tableName}';", connection)) {
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value) {
                        return Convert.ToInt32(result);
                    }
                }
            }

            return -1;
        }

        // APPROACH 1: Delete the table entry from sqlite_master
        static void CorruptByRemovingFromSqliteMaster(string dbPath, string tableName) {
            string tempDbPath = Path.GetTempFileName();

            try {
                // Make a copy of the database to work with
                File.Copy(dbPath, tempDbPath, true);

                // Connect to the temporary database
                using (var connection = new SQLiteConnection($"Data Source={tempDbPath};Version=3;")) {
                    connection.Open();

                    // Enable writing to sqlite_master
                    using (var command = new SQLiteCommand("PRAGMA writable_schema = 1;", connection)) {
                        command.ExecuteNonQuery();
                    }

                    //// Delete the table entry from sqlite_master
                    //using (var command = new SQLiteCommand(
                    //    $"DELETE FROM sqlite_master WHERE type='table' AND name='{tableName}';",
                    //    connection)) {
                    //    command.ExecuteNonQuery();
                    //}

                    // Replace the table definition with a corrupted SQL statement
                    using (var command = new SQLiteCommand(
                               $"UPDATE sqlite_master SET sql = 'CREATE TABLE {tableName} (CORRUPTED_INVALID_SCHEMA)' WHERE type='table' AND name='{tableName}';",
                               connection)) {
                        command.ExecuteNonQuery();
                    }

                    // Disable writing to sqlite_master
                    using (var command = new SQLiteCommand("PRAGMA writable_schema = 0;", connection)) {
                        command.ExecuteNonQuery();
                    }

                    // Force a database reload
                    using (var command = new SQLiteCommand("PRAGMA integrity_check;", connection)) {
                        try {
                            command.ExecuteNonQuery();
                        } catch { /* Expected to possibly fail */ }
                    }
                }

                // Replace original with modified database
                File.Copy(tempDbPath, dbPath, true);

                Console.WriteLine($"Successfully corrupted {tableName} by removing from sqlite_master");
            } finally {
                // Clean up temp file
                try {
                    if (File.Exists(tempDbPath))
                        File.Delete(tempDbPath);
                } catch { /* Ignore cleanup errors */ }
            }
        }

        static void VerifyCorruption(string dbPath) {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;")) {
                connection.Open();

                // Try to read from the corrupted table
                Console.WriteLine("\nAttempting to read from corrupted KeyStore table:");
                try {
                    using (var command = new SQLiteCommand("SELECT * FROM KeyStore;", connection))
                    using (var reader = command.ExecuteReader()) {
                        while (reader.Read()) {
                            Console.WriteLine($"  KeyId: {reader["KeyId"]}, Key: {reader["EncryptedKey"]}");
                        }
                    }
                    Console.WriteLine("  (If you see this, corruption failed!)");
                } catch (Exception ex) {
                    Console.WriteLine($"  Error accessing KeyStore table: {ex.Message}");
                    Console.WriteLine("  KeyStore table is corrupted as expected.");
                }

                // Try to read from the intact table
                Console.WriteLine("\nAttempting to read from intact UserSettings table:");
                try {
                    using (var command = new SQLiteCommand("SELECT * FROM UserSettings;", connection))
                    using (var reader = command.ExecuteReader()) {
                        while (reader.Read()) {
                            Console.WriteLine($"  SettingId: {reader["SettingId"]}, Name: {reader["SettingName"]}, Value: {reader["SettingValue"]}");
                        }
                    }
                    Console.WriteLine("  UserSettings table is intact as expected.");
                } catch (Exception ex) {
                    Console.WriteLine($"  Error accessing UserSettings table: {ex.Message}");
                    Console.WriteLine("  (This is unexpected - both tables are corrupted!)");
                }
            }
        }
    }
}

