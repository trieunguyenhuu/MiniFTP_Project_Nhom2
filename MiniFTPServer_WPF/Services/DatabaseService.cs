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
        public Tuple<int, string, string, string> CheckLoginGetInfo(string username, string password)
        {
            try
            {
                string hashedPassword = HashPassword(password);

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT user_id, full_name, Email, Description FROM Users WHERE username = @u AND password = @p";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@p", hashedPassword);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int id = reader.GetInt32(0);

                                string fullName = reader["full_name"] is DBNull ? username : reader["full_name"].ToString();
                                string email = reader["Email"] is DBNull ? "" : reader["Email"].ToString();
                                string description = reader["Description"] is DBNull ? "" : reader["Description"].ToString();

                                return new Tuple<int, string, string, string>(id, fullName, email, description);
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

        // ==================== LẤY DANH SÁCH FILE ĐÃ XÓA ====================
        public List<Tuple<int, string, long, DateTime>> GetDeletedFiles(int userId)
        {
            var list = new List<Tuple<int, string, long, DateTime>>();
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT file_id, file_name, file_size, created_at 
                          FROM Files 
                          WHERE owner_user_id = @uid AND is_deleted = 1 AND is_folder = 0
                          ORDER BY created_at DESC";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string name = reader.GetString(1);
                                long size = reader.GetInt64(2);
                                DateTime date = reader.GetDateTime(3);

                                list.Add(new Tuple<int, string, long, DateTime>(id, name, size, date));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lấy file đã xóa: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return list;
        }

        // ==================== KHÔI PHỤC FILE ====================
        public bool RestoreFile(int fileId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE Files SET is_deleted = 0 WHERE file_id = @id";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", fileId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khôi phục file: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ==================== XÓA VĨNH VIỄN FILE ====================
        public bool PermanentDeleteFile(int fileId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    // Lấy storage_path để xóa file vật lý
                    string getPath = "SELECT storage_path FROM Files WHERE file_id = @id";
                    string storagePath = null;

                    using (var cmd = new SQLiteCommand(getPath, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", fileId);
                        storagePath = cmd.ExecuteScalar()?.ToString();
                    }

                    // Xóa record trong DB
                    string deleteSql = "DELETE FROM Files WHERE file_id = @id";
                    using (var cmd = new SQLiteCommand(deleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", fileId);
                        cmd.ExecuteNonQuery();
                    }

                    // Xóa file vật lý nếu tồn tại
                    if (!string.IsNullOrEmpty(storagePath))
                    {
                        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            "MASTER_STORAGE", storagePath);
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa vĩnh viễn: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ==================== XÓA TẤT CẢ FILE TRONG THÙNG RÁC ====================
        public bool EmptyTrash(int userId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    // Lấy danh sách file cần xóa vật lý
                    string getFiles = "SELECT storage_path FROM Files WHERE owner_user_id = @uid AND is_deleted = 1";
                    var paths = new List<string>();

                    using (var cmd = new SQLiteCommand(getFiles, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                paths.Add(reader.GetString(0));
                            }
                        }
                    }

                    // Xóa records trong DB
                    string deleteSql = "DELETE FROM Files WHERE owner_user_id = @uid AND is_deleted = 1";
                    using (var cmd = new SQLiteCommand(deleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }

                    // Xóa files vật lý
                    foreach (var path in paths)
                    {
                        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            "MASTER_STORAGE", userId.ToString(), path);
                        if (File.Exists(fullPath))
                        {
                            try { File.Delete(fullPath); } catch { }
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi dọn dẹp thùng rác: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
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

        // Lấy ID thư mục con dựa vào tên và thư mục cha hiện tại
        public int? GetFolderIdByName(int userId, int parentId, string folderName)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    // is_folder = 1 và is_deleted = 0
                    string sql = "SELECT file_id FROM Files WHERE owner_user_id = @uid AND parent_id = @pid AND file_name = @name AND is_folder = 1 AND is_deleted = 0";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@pid", parentId);
                        cmd.Parameters.AddWithValue("@name", folderName);
                        var result = cmd.ExecuteScalar();
                        if (result != null) return Convert.ToInt32(result);
                    }
                }
            }
            catch { }
            return null;
        }

        // Lấy ID thư mục cha (để Back lại)
        public int? GetParentId(int currentFolderId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT parent_id FROM Files WHERE file_id = @fid";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fid", currentFolderId);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value) return Convert.ToInt32(result);
                    }
                }
            }
            catch { }
            return null;
        }

        // ==================== KIỂM TRA MẬT KHẨU ====================
        public bool VerifyPassword(int userId, string password)
        {
            try
            {
                string hashedPassword = HashPassword(password);

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM Users WHERE user_id = @uid AND password = @pwd";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@pwd", hashedPassword);

                        long count = (long)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kiểm tra mật khẩu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ==================== CẬP NHẬT MẬT KHẨU ====================
        public bool UpdatePassword(int userId, string newPassword)
        {
            try
            {
                string hashedPassword = HashPassword(newPassword);

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE Users SET password = @pwd WHERE user_id = @uid";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pwd", hashedPassword);
                        cmd.Parameters.AddWithValue("@uid", userId);

                        int rows = cmd.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi cập nhật mật khẩu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        //================================chia sẻ file==========================================
        public bool ShareFile(int fileId, int ownerId, int sharedWithUserId, string accessLevel = "READ")
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Kiểm tra file có tồn tại và thuộc về owner không
                    string checkSql = "SELECT owner_user_id FROM Files WHERE file_id = @fid";
                    using (var cmd = new SQLiteCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fid", fileId);
                        var result = cmd.ExecuteScalar();

                        if (result == null || Convert.ToInt32(result) != ownerId)
                        {
                            return false; // File không tồn tại hoặc không phải owner
                        }
                    }

                    // 2. Kiểm tra đã share cho user này chưa
                    string checkExistSql = @"SELECT perm_id FROM Permissions 
                                    WHERE file_id = @fid AND shared_with_user_id = @uid";

                    using (var cmd = new SQLiteCommand(checkExistSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fid", fileId);
                        cmd.Parameters.AddWithValue("@uid", sharedWithUserId);

                        var existingPerm = cmd.ExecuteScalar();

                        if (existingPerm != null)
                        {
                            // Đã share rồi → Update access level
                            string updateSql = @"UPDATE Permissions 
                                        SET access_level = @level 
                                        WHERE perm_id = @pid";

                            using (var updateCmd = new SQLiteCommand(updateSql, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@level", accessLevel);
                                updateCmd.Parameters.AddWithValue("@pid", existingPerm);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Chưa share → Insert mới
                            string insertSql = @"INSERT INTO Permissions 
                                        (file_id, shared_with_user_id, access_level) 
                                        VALUES (@fid, @uid, @level)";

                            using (var insertCmd = new SQLiteCommand(insertSql, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@fid", fileId);
                                insertCmd.Parameters.AddWithValue("@uid", sharedWithUserId);
                                insertCmd.Parameters.AddWithValue("@level", accessLevel);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi chia sẻ file: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ==================== LẤY DANH SÁCH FILE ĐƯỢC CHIA SẺ ====================
        /// <summary>
        /// Lấy danh sách file mà user này được người khác share
        /// </summary>
        public List<Tuple<int, string, long, string, string>> GetSharedFiles(int userId)
        {
            var list = new List<Tuple<int, string, long, string, string>>();

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                SELECT 
                    f.file_id,
                    f.file_name,
                    f.file_size,
                    p.access_level,
                    u.full_name as owner_name
                FROM Permissions p
                JOIN Files f ON p.file_id = f.file_id
                JOIN Users u ON f.owner_user_id = u.user_id
                WHERE p.shared_with_user_id = @uid 
                    AND f.is_deleted = 0
                ORDER BY f.created_at DESC";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string name = reader.GetString(1);
                                long size = reader.GetInt64(2);
                                string access = reader.GetString(3);
                                string owner = reader.GetString(4);

                                list.Add(new Tuple<int, string, long, string, string>(
                                    id, name, size, access, owner));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lấy file được chia sẻ: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return list;
        }

        // ==================== HỦY CHIA SẺ ====================
        /// <summary>
        /// Hủy chia sẻ file với user
        /// </summary>
        public bool UnshareFile(int fileId, int sharedWithUserId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"DELETE FROM Permissions 
                          WHERE file_id = @fid AND shared_with_user_id = @uid";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fid", fileId);
                        cmd.Parameters.AddWithValue("@uid", sharedWithUserId);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hủy chia sẻ: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ==================== KIỂM TRA QUYỀN TRUY CẬP ====================
        /// <summary>
        /// Kiểm tra user có quyền truy cập file không
        /// </summary>
        public string CheckFileAccess(int fileId, int userId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Kiểm tra là owner không
                    string ownerSql = "SELECT owner_user_id FROM Files WHERE file_id = @fid";
                    using (var cmd = new SQLiteCommand(ownerSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fid", fileId);
                        var ownerId = cmd.ExecuteScalar();

                        if (ownerId != null && Convert.ToInt32(ownerId) == userId)
                        {
                            return "OWNER"; // Là chủ sở hữu
                        }
                    }

                    // 2. Kiểm tra trong bảng Permissions
                    string permSql = @"SELECT access_level FROM Permissions 
                              WHERE file_id = @fid AND shared_with_user_id = @uid";

                    using (var cmd = new SQLiteCommand(permSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fid", fileId);
                        cmd.Parameters.AddWithValue("@uid", userId);

                        var result = cmd.ExecuteScalar();
                        return result?.ToString() ?? "NONE";
                    }
                }
            }
            catch
            {
                return "NONE";
            }
        }

        // ==================== LẤY DANH SÁCH USER ĐÃ SHARE ====================
        /// <summary>
        /// Lấy danh sách user đã được share file này
        /// </summary>
        public List<Tuple<int, string, string>> GetSharedWithUsers(int fileId)
        {
            var list = new List<Tuple<int, string, string>>();

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                SELECT 
                    u.user_id,
                    u.full_name,
                    p.access_level
                FROM Permissions p
                JOIN Users u ON p.shared_with_user_id = u.user_id
                WHERE p.file_id = @fid";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fid", fileId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                string fullName = reader.GetString(1);
                                string access = reader.GetString(2);

                                list.Add(new Tuple<int, string, string>(userId, fullName, access));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lấy danh sách: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return list;
        }

        public int? GetUserIdByFullName(string fullName)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    string sql = "SELECT user_id FROM Users WHERE full_name = @name COLLATE NOCASE";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", fullName);

                        var result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : (int?)null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tìm user: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}