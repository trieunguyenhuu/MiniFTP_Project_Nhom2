using MiniFTPClient_WPF.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static MiniFTPClient_WPF.home.Page1;

namespace MiniFTPClient_WPF.Services
{
    public class FtpClientService
    {
        // Singleton: Giúp truy cập kết nối từ bất kỳ đâu (Login, Home, Trash...)
        private static FtpClientService _instance;
        public static FtpClientService Instance => _instance ??= new FtpClientService();

        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;

        public bool IsConnected => _client != null && _client.Connected;
        public string CurrentUsername { get; private set; }
        public string CurrentFullName { get; private set; }

        // Cấu hình Server (Nếu test cùng máy thì để 127.0.0.1)
        private const string SERVER_IP = "127.0.0.1";
        private const int SERVER_PORT = 9999;

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

        // --- 1. XỬ LÝ ĐĂNG NHẬP ---
        public async Task<string> LoginAsync(string username, string password)
        {
            try
            {
                // Kết nối mới nếu chưa có
                if (_client == null || !_client.Connected)
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(SERVER_IP, SERVER_PORT);
                    var stream = _client.GetStream();
                    _reader = new StreamReader(stream, Encoding.UTF8);
                    _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                    // Đọc dòng "WELCOME" từ server
                    await _reader.ReadLineAsync();
                }

                // Gửi lệnh LOGIN
                await _writer.WriteLineAsync($"LOGIN|{username}|{password}");
                string response = await _reader.ReadLineAsync();

                if (response != null && response.StartsWith("LOGIN_SUCCESS"))
                {
                    CurrentUsername = username;
                    // Server trả về: LOGIN_SUCCESS|
                    var parts = response.Split('|');
                    CurrentFullName = parts.Length > 1 ? parts[1] : username;
                    return "OK";
                }
                return response;
            }
            catch (Exception ex)
            {
                return "Lỗi kết nối: " + ex.Message;
            }
        }

        // --- 2. LẤY DANH SÁCH FILE (Home) ---
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
                    // Format Server gửi: LIST_SUCCESS|FOLDER:Ten:Id;FILE:Ten:Size:Id;...
                    string data = response.Substring(13); // Cắt bỏ "LIST_SUCCESS|"
                    if (string.IsNullOrEmpty(data)) return list;

                    string[] items = data.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in items)
                    {
                        var parts = item.Split(':');
                        if (parts[0] == "FOLDER")
                        {
                            // FOLDER:Name:Id
                            list.Add(new FileItem(int.Parse(parts[2]), parts[1], true, 0));
                        }
                        else if (parts[0] == "FILE")
                        {
                            // FILE:Name:Size:Id
                            list.Add(new FileItem(int.Parse(parts[3]), parts[1], false, long.Parse(parts[2])));
                        }
                    }
                }
            }
            catch { /* Xử lý lỗi ngầm hoặc ghi log */ }
            return list;
        }

        // --- 3. TẠO THƯ MỤC (MKDIR) ---
        public async Task<bool> CreateFolderAsync(string folderName)
        {
            if (!IsConnected) return false;
            await _writer.WriteLineAsync($"MKDIR|{folderName}");
            string response = await _reader.ReadLineAsync();
            return response != null && response.StartsWith("MKDIR_SUCCESS");
        }

        // Hàm Download
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

        // Hàm Delete
        public async Task<bool> DeleteFileAsync(int fileId)
        {
            return (await SendCommandAsync($"DELETE|{fileId}")).StartsWith("DELETE_SUCCESS");
        }

        // Hàm Lấy Users
        public async Task<List<UserItem>> GetUsersAsync()
        {
            var list = new List<UserItem>();
            await _writer.WriteLineAsync("GET_USERS");
            string resp = await _reader.ReadLineAsync();
            if (resp.StartsWith("USERS_LIST"))
            {
                // Format: USERS_LIST|1:LanAnh;2:Trieu;
                string data = resp.Substring(11);
                foreach (var u in data.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var p = u.Split(':');
                    list.Add(new UserItem { Name = p[1] }); // Bạn có thể thêm ID vào UserItem nếu cần
                }
            }
            return list;
        }

        // --- 4. NGẮT KẾT NỐI ---
        public void Disconnect()
        {
            try { _writer?.WriteLine("QUIT"); _client?.Close(); } catch { }
            _client = null;
        }
    }
}