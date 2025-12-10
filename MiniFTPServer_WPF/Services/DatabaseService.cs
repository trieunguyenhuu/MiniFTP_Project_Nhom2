using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
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
            // Chỉ trỏ đường dẫn, KHÔNG chạy lệnh CREATE TABLE nữa
            _dbFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ftp_db.sqlite");
            _connectionString = $"Data Source={_dbFilePath};Version=3;";

            if (!File.Exists(_dbFilePath))
            {
                MessageBox.Show("LỖI: Không tìm thấy file Database! Vui lòng tạo thủ công.");
            }
        }

        // --- HÀM 1: LOGIN MỚI (Lấy thêm Tên hiển thị) ---
        // Trả về: Tuple<User_ID, Full_Name>
        public Tuple<int, string> CheckLoginGetInfo(string username, string password)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                // Query lấy cả id và full_name
                string sql = "SELECT user_id, full_name FROM Users WHERE username = @u AND password = @p";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", password);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            // Nếu full_name null thì lấy tạm username
                            string name = reader["full_name"] is DBNull ? username : reader["full_name"].ToString();
                            return new Tuple<int, string>(id, name);
                        }
                    }
                }
            }
            return null; // Đăng nhập sai
        }

        // --- HÀM 2: LẤY ID FOLDER GỐC (Giữ nguyên) ---
        public int GetUserRootFolderId(int userId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT file_id FROM Files WHERE owner_user_id = @uid AND parent_id IS NULL";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    object result = cmd.ExecuteScalar();
                    if (result != null) return Convert.ToInt32(result);

                    // Nếu chưa có thư mục gốc thì tạo mới
                    string insertSql = "INSERT INTO Files (owner_user_id, parent_id, is_folder, file_name) VALUES (@uid, NULL, 1, 'ROOT'); SELECT last_insert_rowid();";
                    using (var cmd2 = new SQLiteCommand(insertSql, conn))
                    {
                        cmd2.Parameters.AddWithValue("@uid", userId);
                        return Convert.ToInt32(cmd2.ExecuteScalar());
                    }
                }
            }
        }

        // --- HÀM 3: LẤY LIST FILE (Sửa để lọc file rác) ---
        public string GetFileList(int userId, int parentFolderId)
        {
            StringBuilder sb = new StringBuilder();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                // THÊM: AND is_deleted = 0
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
            return sb.ToString();
        }

        // --- HÀM 4: THÊM FILE (Giữ nguyên) ---
        public void AddFile(int userId, int parentId, string fileName, long size, string storagePath)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                // CHÚ Ý: Đảm bảo tên cột khớp với lệnh CREATE TABLE/ALTER TABLE bạn đã chạy
                // Cột is_deleted mặc định là 0, is_folder là 0
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

        // --- HÀM 5: XÓA MỀM (Cho vào thùng rác) ---
        public void SoftDeleteFile(int fileId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                // Chỉ update is_deleted thành 1 chứ không xóa hẳn
                string sql = "UPDATE Files SET is_deleted = 1 WHERE file_id = @id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", fileId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // --- HÀM 6: LẤY DANH SÁCH USER (Cho tính năng Share) ---
        public string GetAllUsers()
        {
            StringBuilder sb = new StringBuilder();
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
            return sb.ToString();
        }

        // --- HÀM 7: Lấy info download (Giữ nguyên) ---
        public Tuple<string, string> GetFileInfo(int fileId)
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
                            return new Tuple<string, string>(reader["storage_path"].ToString(), reader["file_name"].ToString());
                        }
                    }
                }
            }
            return null;
        }

        // --- HÀM TẠO THƯ MỤC (MKDIR) ---
        public void AddFolder(int userId, int parentId, string folderName)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                // is_folder = 1 nghĩa là Thư mục
                // is_deleted mặc định là 0 nên không cần điền
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

    }
}