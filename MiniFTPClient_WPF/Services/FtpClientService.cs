using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MiniFTPClient_WPF.Models;

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

        // 👉 ĐÂY LÀ BIẾN BẠN ĐANG THIẾU
        public string CurrentFullName { get; private set; }

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
                    CurrentFullName = parts.Length > 1 ? parts[1] : username;
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

        // 👉 ĐÂY LÀ HÀM BẠN ĐANG THIẾU
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
                list.AddRange(users);
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
    }
}