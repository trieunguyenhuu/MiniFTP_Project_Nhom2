using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace MiniFtpServer_WPF.Services
{
    public class DatabaseService
    {
        private string _connectionString;
        private string _dbFilePath;

        public DatabaseService()
        {
            _dbFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ftp_db.sqlite");
            _connectionString = $"Data Source={_dbFilePath};Version=3;";

            if (!File.Exists(_dbFilePath))
            {
                MessageBox.Show("LỖI: Không tìm thấy file Database! Ứng dụng sẽ đóng.",
                    "Lỗi nghiêm trọng", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            // Kiểm tra kết nối
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối Database: {ex.Message}\nỨng dụng sẽ đóng.",
                    "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        // ==================== HÀM MÃ HÓA MẬT KHẨU ====================
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // ==================== LOGIN MỚI (Hash password) ====================
        public Tuple<int, string> CheckLoginGetInfo(string username, string password)
        {
            try
            {
                string hashedPassword = HashPassword(password);

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT user_id, full_name FROM Users WHERE username = @u AND password = @p";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@p", hashedPassword);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string name = reader["full_name"] is DBNull ? username : reader["full_name"].ToString();
                                return new Tuple<int, string>(id, name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kiểm tra đăng nhập: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        // ==================== LẤY ROOT FOLDER ====================
        public int GetUserRootFolderId(int userId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT file_id FROM Files WHERE owner_user_id = @uid AND parent_id IS NULL AND is_folder = 1";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        object result = cmd.ExecuteScalar();
                        if (result != null) return Convert.ToInt32(result);

                        // Tạo mới folder gốc
                        string insertSql = "INSERT INTO Files (owner_user_id, parent_id, is_folder, file_name) VALUES (@uid, NULL, 1, 'ROOT'); SELECT last_insert_rowid();";
                        using (var cmd2 = new SQLiteCommand(insertSql, conn))
                        {
                            cmd2.Parameters.AddWithValue("@uid", userId);
                            return Convert.ToInt32(cmd2.ExecuteScalar());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lấy thư mục gốc: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return -1;
            }
        }

        // ==================== LẤY FILE LIST ====================
        public string GetFileList(int userId, int parentFolderId)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT file_id, file_name, is_folder, file_size FROM Files WHERE parent_id = @pid AND is_deleted = 0";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pid", parentFolderId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string name = reader.GetString(1);
                                bool isFolder = reader.GetInt32(2) == 1;
                                long size = reader.GetInt64(3);

                                if (isFolder) sb.Append($"FOLDER:{name}:{id};");
                                else sb.Append($"FILE:{name}:{size}:{id};");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lấy danh sách file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return sb.ToString();
        }

        // ==================== THÊM FILE ====================
        public void AddFile(int userId, int parentId, string fileName, long size, string storagePath)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO Files (owner_user_id, parent_id, is_folder, file_name, file_size, storage_path, is_deleted) 
                                   VALUES (@u, @p, 0, @n, @s, @path, 0)";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", userId);
                        cmd.Parameters.AddWithValue("@p", parentId);
                        cmd.Parameters.AddWithValue("@n", fileName);
                        cmd.Parameters.AddWithValue("@s", size);
                        cmd.Parameters.AddWithValue("@path", storagePath);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi thêm file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== XÓA MỀM ====================
        public void SoftDeleteFile(int fileId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE Files SET is_deleted = 1 WHERE file_id = @id";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", fileId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== LẤY DANH SÁCH USER ====================
        public string GetAllUsers()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT user_id, full_name FROM Users";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sb.Append($"{reader.GetInt32(0)}:{reader.GetString(1)};");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lấy danh sách user: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return sb.ToString();
        }

        // ==================== LẤY FILE INFO ====================
        public Tuple<string, string> GetFileInfo(int fileId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT storage_path, file_name FROM Files WHERE file_id = @fid";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fid", fileId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Tuple<string, string>(
                                    reader["storage_path"].ToString(),
                                    reader["file_name"].ToString()
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lấy thông tin file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        // ==================== TẠO FOLDER ====================
        public void AddFolder(int userId, int parentId, string folderName)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO Files (owner_user_id, parent_id, is_folder, file_name)
                                   VALUES (@uid, @pid, 1, @name)";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@pid", parentId);
                        cmd.Parameters.AddWithValue("@name", folderName);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}