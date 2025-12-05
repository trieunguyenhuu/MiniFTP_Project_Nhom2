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
            // 1. Xác định đường dẫn file database
            _dbFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ftp_db.sqlite");
            _connectionString = $"Data Source={_dbFilePath};Version=3;";

            if (!File.Exists(_dbFilePath))
            {
                MessageBox.Show($"LỖI: Không tìm thấy file CSDL tại:\n{_dbFilePath}\nCopy file vào bin/Debug rồi chạy lại.",
                                "Thiếu Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- HÀM 1: Kiểm tra Đăng nhập ---
        public int CheckLogin(string username, string password)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT user_id FROM Users WHERE username = @u AND password = @p";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", password);
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : -1;
                }
            }
        }

        // --- HÀM 2: Lấy (hoặc Tạo) ID thư mục gốc ---
        public int GetUserRootFolderId(int userId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                // Tìm thư mục gốc (parent_id IS NULL)
                string sql = "SELECT file_id FROM Files WHERE owner_user_id = @uid AND parent_id IS NULL";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    object result = cmd.ExecuteScalar();
                    if (result != null) return Convert.ToInt32(result);
                }

                // Nếu chưa có, tạo mới
                string insertSql = @"INSERT INTO Files (owner_user_id, parent_id, is_folder, file_name) 
                                     VALUES (@uid, NULL, 1, 'ROOT'); SELECT last_insert_rowid();";
                using (var cmd = new SQLiteCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        // --- HÀM 3: Lấy danh sách File (Cho lệnh LIST) ---
        // Trả về chuỗi: "FOLDER:Ten:Id;FILE:Ten:Size:Id;"
        public string GetFileList(int userId, int parentFolderId)
        {
            StringBuilder sb = new StringBuilder();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                // Lấy tất cả file/folder nằm trong thư mục cha này
                string sql = "SELECT file_id, file_name, is_folder, file_size FROM Files WHERE parent_id = @pid";

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

                            if (isFolder)
                                sb.Append($"FOLDER:{name}:{id};");
                            else
                                sb.Append($"FILE:{name}:{size}:{id};");
                        }
                    }
                }
            }
            return sb.ToString();
        }

        // --- HÀM 4: Thêm File Mới (Cho lệnh UPLOAD) ---
        public void AddFile(int userId, int parentId, string fileName, long size, string storagePath)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = @"INSERT INTO Files (owner_user_id, parent_id, is_folder, file_name, file_size, storage_path)
                               VALUES (@uid, @pid, 0, @name, @size, @path)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@pid", parentId);
                    cmd.Parameters.AddWithValue("@name", fileName);
                    cmd.Parameters.AddWithValue("@size", size);
                    cmd.Parameters.AddWithValue("@path", storagePath);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // --- HÀM 5: Tạo Thư mục Mới (Cho lệnh MKDIR) ---
        public void AddFolder(int userId, int parentId, string folderName)
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

        // --- HÀM 6: Lấy thông tin file để Download ---
        // Trả về tuple (StoragePath, FileName)
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
    }
}