using MiniFTPClient_WPF.Models;
using MiniFTPClient_WPF.thungrac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MiniFTPClient_WPF.Services
{
    public class FtpClientService
    {
        // Singleton
        private static FtpClientService _instance;
        public static FtpClientService Instance => _instance ??= new FtpClientService();

        // Biến kết nối
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;

        public bool IsConnected => _client != null && _client.Connected;
        public string CurrentUsername { get; private set; }
        public int CurrentUserId { get; private set; }
        public string CurrentFullName { get; private set; }
        public string CurrentEmail { get; private set; }
        public string CurrentDescription { get; private set; }

        private const string SERVER_IP = "127.0.0.1";
        private const int SERVER_PORT = 9999;

        // Hàm gửi lệnh cơ bản
        public async Task<string> SendCommandAsync(string command)
        {
            if (!IsConnected) return "ERROR|Mất kết nối";
            try
            {
                await _writer.WriteLineAsync(command);
                await _writer.FlushAsync();
                return await _reader.ReadLineAsync();
            }
            catch (Exception ex)
            {
                return "ERROR|" + ex.Message;
            }
        }

        // 1. ĐĂNG NHẬP (Cập nhật lấy tên hiển thị)
        public async Task<string> LoginAsync(string username, string password)
        {
            try
            {
                if (_client == null || !_client.Connected)
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(SERVER_IP, SERVER_PORT);

                    _stream = _client.GetStream();
                    _reader = new StreamReader(_stream, Encoding.UTF8);
                    _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                    await _reader.ReadLineAsync(); // Đọc WELCOME
                }

                await _writer.WriteLineAsync($"LOGIN|{username}|{password}");
                string response = await _reader.ReadLineAsync();

                if (response != null && response.StartsWith("LOGIN_SUCCESS"))
                {
                    
                    CurrentUsername = username;
                    // Lấy tên hiển thị: LOGIN_SUCCESS|Nguyễn Lan Anh
                    var parts = response.Split('|');
                    // 1. Lấy User ID (Index 1)
                    if (parts.Length > 1)
                    {
                        int.TryParse(parts[1], out int uid);
                        CurrentUserId = uid;
                    }

                    // 2. Lấy Tên hiển thị (Index 2) -> SỬA DÒNG NÀY
                    // (Lúc trước bạn để parts[1] nên nó lấy nhầm ID)
                    CurrentFullName = parts.Length > 2 ? parts[2] : username;

                    // 3. Lấy Email (Index 3)
                    CurrentEmail = parts.Length > 3 ? parts[3] : "";

                    // 4. Lấy Mô tả (Index 4)
                    CurrentDescription = parts.Length > 4 ? parts[4] : " ";
                    return "OK";
                }
                return response?.Split('|')[1] ?? "Lỗi không xác định";
            }
            catch (Exception ex)
            {
                return "Lỗi kết nối: " + ex.Message;
            }
        }

        // 2. LẤY DANH SÁCH FILE
        public async Task<List<FileItem>> GetListingAsync()
        {
            var list = new List<FileItem>();
            if (!IsConnected) return list;

            try
            {
                await _writer.WriteLineAsync("LIST");
                string response = await _reader.ReadLineAsync();

                if (response != null && response.StartsWith("LIST_SUCCESS"))
                {
                    string data = response.Substring(13);
                    if (!string.IsNullOrEmpty(data))
                    {
                        string[] items = data.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var item in items)
                        {
                            var parts = item.Split(':');
                            if (parts[0] == "FOLDER")
                                list.Add(new FileItem(int.Parse(parts[2]), parts[1], true, 0));
                            else if (parts[0] == "FILE")
                                list.Add(new FileItem(int.Parse(parts[3]), parts[1], false, long.Parse(parts[2])));
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        // 3. UPLOAD FILE
        public async Task<string> UploadFileAsync(string filePath)
        {
            if (!IsConnected) return "Mất kết nối server.";
            try
            {
                FileInfo fi = new FileInfo(filePath);
                string fileName = fi.Name;
                long fileSize = fi.Length;

                await _writer.WriteLineAsync($"UPLOAD|{fileName}|{fileSize}");
                await _writer.FlushAsync();

                string response = await _reader.ReadLineAsync();
                if (response != "UPLOAD_READY") return "Server từ chối: " + response;

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await _stream.WriteAsync(buffer, 0, bytesRead);
                    }
                    await _stream.FlushAsync();
                }

                string result = await _reader.ReadLineAsync();
                return result != null && result.StartsWith("UPLOAD_SUCCESS") ? "OK" : "Lỗi Server: " + result;
            }
            catch (Exception ex)
            {
                return "Lỗi Upload: " + ex.Message;
            }
        }

        // 4. DOWNLOAD FILE
        public async Task<bool> DownloadFileAsync(int fileId, string savePath, long fileSize)
        {
            if (!IsConnected) return false;
            try
            {
                await _writer.WriteLineAsync($"DOWNLOAD|{fileId}");
                await _writer.FlushAsync();

                string resp = await _reader.ReadLineAsync();
                if (resp.StartsWith("DOWNLOAD_READY"))
                {
                    using (var fs = new FileStream(savePath, FileMode.Create))
                    {
                        byte[] buffer = new byte[8192];
                        long totalRead = 0;
                        while (totalRead < fileSize)
                        {
                            int read = await _stream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, fileSize - totalRead));
                            if (read == 0) break;
                            await fs.WriteAsync(buffer, 0, read);
                            totalRead += read;
                        }
                    }
                    return true;
                }
                return false;
            }
            catch { return false; }
        }


        // 5. DELETE FILE
        public async Task<bool> DeleteFileAsync(int fileId)
        {
            string result = await SendCommandAsync($"DELETE|{fileId}");
            return result != null && result.StartsWith("DELETE_SUCCESS");
        }

        // 6. LẤY DANH SÁCH USER (Cho tính năng Share)
        public async Task<List<string>> GetUsersAsync()
        {
            var list = new List<string>();
            if (!IsConnected) return list;

            await _writer.WriteLineAsync("GET_USERS");
            string resp = await _reader.ReadLineAsync();

            if (resp != null && resp.StartsWith("USERS_LIST"))
            {
                string data = resp.Substring(11);
                var users = data.Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (var user in users)
                {
                    // Tách "1:Nguyễn Văn A" thành chỉ lấy "Nguyễn Văn A"
                    var parts = user.Split(':');
                    if (parts.Length > 1)
                    {
                        string fullName = parts[1];

                        // CHỈ THÊM NẾU KHÔNG PHẢI LÀ NGƯỜI DÙNG HIỆN TẠI
                        if (!string.Equals(fullName, CurrentFullName, StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(fullName);
                        }
                    }
                }
            }
            return list;
        }

        // 7. NGẮT KẾT NỐI
        public void Disconnect()
        {
            try
            {
                if (IsConnected) { _writer.WriteLine("QUIT"); _writer.Flush(); }
                _client?.Close();
            }
            catch { }
            _client = null;
            _stream = null;
            _reader = null;
            _writer = null;
        }

        // ==================== THÊM HÀM ChangePasswordAsync (TODO) ====================
        public async Task<string> ChangePasswordAsync(string currentPwd, string newPwd)
        {
            try
            {
                if (!IsConnected) return "Chưa kết nối server";

                // Gửi lệnh CHANGE_PASSWORD|currentPwd|newPwd
                await _writer.WriteLineAsync($"CHANGE_PASSWORD|{currentPwd}|{newPwd}");
                string response = await _reader.ReadLineAsync();

                if (response.StartsWith("CHANGE_PASSWORD_SUCCESS"))
                {
                    return "OK";
                }
                else if (response.StartsWith("ERROR"))
                {
                    return response.Split('|')[1];
                }

                return "Lỗi không xác định";
            }
            catch (Exception ex)
            {
                return $"Lỗi: {ex.Message}";
            }
        }

        // ==================== THÊM HÀM UpdateProfileAsync (TODO) ====================
        public async Task<string> UpdateProfileAsync(string username, string email, string fullName)
        {
            try
            {
                if (!IsConnected) return "Chưa kết nối server";

                // Gửi lệnh UPDATE_PROFILE|username|email|fullName
                await _writer.WriteLineAsync($"UPDATE_PROFILE|{username}|{email}|{fullName}");
                string response = await _reader.ReadLineAsync();

                if (response.StartsWith("UPDATE_SUCCESS"))
                {
                    CurrentFullName = fullName;
                    return "OK";
                }
                else if (response.StartsWith("ERROR"))
                {
                    return response.Split('|')[1];
                }

                return "Lỗi không xác định";
            }
            catch (Exception ex)
            {
                return $"Lỗi: {ex.Message}";
            }
        }

        // 8. LẤY DANH SÁCH THÙNG RÁC
        public async Task<List<TrashItem>> GetTrashAsync()
        {
            var list = new List<TrashItem>();
            if (!IsConnected) return list;

            try
            {
                await _writer.WriteLineAsync("GET_TRASH");
                string resp = await _reader.ReadLineAsync();

                if (resp != null && resp.StartsWith("TRASH_LIST"))
                {
                    string data = resp.Substring(11);
                    if (!string.IsNullOrEmpty(data))
                    {
                        var items = data.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var item in items)
                        {
                            var parts = item.Split('|');
                            // FILE:id:name:size:date
                            if (parts.Length >= 5)
                            {
                                list.Add(new TrashItem
                                {
                                    FileId = int.Parse(parts[1]),
                                    FileName = parts[2],
                                    Size = FormatFileSize(long.Parse(parts[3])),
                                    DeletedDate = DateTime.Parse(parts[4]),
                                    OriginalPath = "/Home" // Tạm thời
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        // 9. KHÔI PHỤC FILE
        public async Task<bool> RestoreFileAsync(int fileId)
        {
            string result = await SendCommandAsync($"RESTORE_FILE|{fileId}");
            return result != null && result.StartsWith("RESTORE_SUCCESS");
        }

        // 10. XÓA VĨNH VIỄN
        public async Task<bool> PermanentDeleteAsync(int fileId)
        {
            string result = await SendCommandAsync($"PERMANENT_DELETE|{fileId}");
            return result != null && result.StartsWith("PERMANENT_DELETE_SUCCESS");
        }

        // 11. DỌN DẸP THÙNG RÁC
        public async Task<bool> EmptyTrashAsync()
        {
            string result = await SendCommandAsync("EMPTY_TRASH");
            return result != null && result.StartsWith("EMPTY_TRASH_SUCCESS");
        }

        // Helper
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        //=========================share file======================
        public async Task<string> ShareFileAsync(int fileId, string targetUserFullName, string accessLevel = "READ")
        {
            try
            {
                if (!IsConnected) return "Chưa kết nối server";

                // 1. Lấy danh sách user để tìm user_id từ full_name
                var users = await GetUsersAsync();

                // GetUsersAsync() hiện tại trả về List<string> (chỉ có full_name)
                // Ta cần sửa lại để lấy cả user_id
                // Tạm thời gửi lệnh SHARE_FILE_BY_NAME để server tự tìm

                await _writer.WriteLineAsync($"SHARE_FILE_BY_NAME|{fileId}|{targetUserFullName}|{accessLevel}");
                string response = await _reader.ReadLineAsync();

                if (response != null && response.StartsWith("SHARE_SUCCESS"))
                {
                    return "OK";
                }
                else if (response != null && response.StartsWith("ERROR"))
                {
                    return response.Split('|')[1];
                }

                return "Lỗi không xác định";
            }
            catch (Exception ex)
            {
                return $"Lỗi: {ex.Message}";
            }
        }

        // ==================== LẤY DANH SÁCH USER (CẢI TIẾN) ====================
        /// <summary>
        /// Lấy danh sách user với cả ID và tên
        /// </summary>
        public async Task<List<Tuple<int, string>>> GetUsersWithIdAsync()
        {
            var list = new List<Tuple<int, string>>();
            if (!IsConnected) return list;

            await _writer.WriteLineAsync("GET_USERS");
            string resp = await _reader.ReadLineAsync();

            if (resp != null && resp.StartsWith("USERS_LIST"))
            {
                string data = resp.Substring(11);
                var users = data.Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (var user in users)
                {
                    // Format từ server: "1:Nguyễn Văn A"
                    var parts = user.Split(':');
                    if (parts.Length > 1)
                    {
                        int userId = int.Parse(parts[0]);
                        string fullName = parts[1];

                        // Không thêm người dùng hiện tại
                        if (!string.Equals(fullName, CurrentFullName, StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(new Tuple<int, string>(userId, fullName));
                        }
                    }
                }
            }
            return list;
        }

        // ==================== LẤY FILE ĐƯỢC SHARE ====================
        /// <summary>
        /// Lấy danh sách file mà người khác đã share cho mình
        /// </summary>
        public async Task<List<SharedFileItem>> GetSharedFilesAsync()
        {
            var list = new List<SharedFileItem>();
            if (!IsConnected) return list;

            try
            {
                await _writer.WriteLineAsync("GET_SHARED_FILES");
                string resp = await _reader.ReadLineAsync();

                if (resp != null && resp.StartsWith("SHARED_FILES_LIST"))
                {
                    string data = resp.Substring(18);
                    if (!string.IsNullOrEmpty(data))
                    {
                        var items = data.Split(';', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var item in items)
                        {
                            // Format: id|name|size|access|owner;
                            var parts = item.Split('|');
                            if (parts.Length >= 5)
                            {
                                list.Add(new SharedFileItem
                                {
                                    FileId = int.Parse(parts[0]),
                                    FileName = parts[1],
                                    FileSize = long.Parse(parts[2]),
                                    AccessLevel = parts[3],
                                    OwnerName = parts[4]
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lấy shared files: {ex.Message}");
            }

            return list;
        }

        // ==================== HỦY CHIA SẺ ====================
        /// <summary>
        /// Hủy chia sẻ file với user
        /// </summary>
        public async Task<bool> UnshareFileAsync(int fileId, int userId)
        {
            if (!IsConnected) return false;

            try
            {
                await _writer.WriteLineAsync($"UNSHARE_FILE|{fileId}|{userId}");
                string response = await _reader.ReadLineAsync();

                return response != null && response.StartsWith("UNSHARE_SUCCESS");
            }
            catch
            {
                return false;
            }
        }

        // ==================== KIỂM TRA QUYỀN ====================
        /// <summary>
        /// Kiểm tra quyền truy cập file
        /// </summary>
        public async Task<string> CheckFileAccessAsync(int fileId)
        {
            if (!IsConnected) return "NONE";

            try
            {
                await _writer.WriteLineAsync($"CHECK_ACCESS|{fileId}");
                string response = await _reader.ReadLineAsync();

                if (response != null && response.StartsWith("ACCESS_LEVEL"))
                {
                    return response.Split('|')[1];
                }
            }
            catch { }

            return "NONE";
        }
      

        // ==================== LẤY DANH SÁCH FILE ĐÃ GỬI (SENT) ====================
        public async Task<List<SharedFileItem>> GetSentFilesAsync()
        {
            var list = new List<SharedFileItem>();
            if (!IsConnected) return list;

            try
            {
                await _writer.WriteLineAsync("GET_SENT_FILES"); // Gửi lệnh mới
                string resp = await _reader.ReadLineAsync();

                if (resp != null && resp.StartsWith("SENT_FILES_LIST"))
                {
                    string data = resp.Substring(16); // Bỏ chữ SENT_FILES_LIST| (độ dài 16)
                    if (!string.IsNullOrEmpty(data))
                    {
                        var items = data.Split(';', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var item in items)
                        {
                            // Format: id|name|size|access|receiverName
                            var parts = item.Split('|');
                            if (parts.Length >= 5)
                            {
                                list.Add(new SharedFileItem
                                {
                                    FileId = int.Parse(parts[0]),
                                    FileName = parts[1],
                                    FileSize = long.Parse(parts[2]),
                                    AccessLevel = parts[3],
                                    OwnerName = parts[4] // Trong trường hợp này, OwnerName đóng vai trò là "Người nhận"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lấy sent files: {ex.Message}");
            }

            return list;
        }

        public async Task<bool> CreateDirectoryAsync(string folderName)
        {
            // Gửi lệnh MKDIR|TenThuMuc
            string result = await SendCommandAsync($"MKDIR|{folderName}");
            return result != null && result.StartsWith("MKDIR_SUCCESS");
        }

        // Chuyển thư mục
        public async Task<bool> ChangeDirectoryAsync(string folderName)
        {
            // folderName có thể là tên thư mục con hoặc ".."
            string result = await SendCommandAsync($"CWD|{folderName}");
            return result != null && result.StartsWith("CWD_SUCCESS");
        }     
        
    }
}