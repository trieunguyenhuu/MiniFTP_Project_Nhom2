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
        private static FtpClientService _instance;
        public static FtpClientService Instance => _instance ??= new FtpClientService();

        // --- KHAI BÁO BIẾN TOÀN CỤC (Để hàm nào cũng dùng được) ---
        private TcpClient _client;
        private NetworkStream _stream; // <--- Đây là cái bạn đang thiếu
        private StreamReader _reader;
        private StreamWriter _writer;

        public bool IsConnected => _client != null && _client.Connected;
        public string CurrentUsername { get; private set; }

        private const string SERVER_IP = "127.0.0.1";
        private const int SERVER_PORT = 9999;

        // --- 1. ĐĂNG NHẬP ---
        public async Task<string> LoginAsync(string username, string password)
        {
            try
            {
                if (_client == null || !_client.Connected)
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(SERVER_IP, SERVER_PORT);

                    // Gán giá trị cho biến toàn cục
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
                    return "OK";
                }
                return response?.Split('|')[1] ?? "Lỗi không xác định";
            }
            catch (Exception ex)
            {
                return "Lỗi kết nối: " + ex.Message;
            }
        }

        // --- 2. LẤY DANH SÁCH FILE ---
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
                    if (string.IsNullOrEmpty(data)) return list;

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
            catch { }
            return list;
        }

        // --- 3. UPLOAD FILE (Đã sửa lỗi _stream) ---
        public async Task<string> UploadFileAsync(string filePath)
        {
            if (!IsConnected) return "Mất kết nối server.";

            try
            {
                FileInfo fi = new FileInfo(filePath);
                string fileName = fi.Name;
                long fileSize = fi.Length;

                // Gửi lệnh
                await _writer.WriteLineAsync($"UPLOAD|{fileName}|{fileSize}");
                await _writer.FlushAsync();

                // Chờ xác nhận
                string response = await _reader.ReadLineAsync();
                if (response != "UPLOAD_READY") return "Server từ chối: " + response;

                // Gửi dữ liệu (Dùng biến _stream toàn cục)
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

                // Chờ kết quả
                string result = await _reader.ReadLineAsync();
                return result != null && result.StartsWith("UPLOAD_SUCCESS") ? "OK" : "Lỗi Server: " + result;
            }
            catch (Exception ex)
            {
                return "Lỗi Upload: " + ex.Message;
            }
        }

        // --- 4. NGẮT KẾT NỐI ---
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