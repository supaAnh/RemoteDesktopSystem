# REMOTE DESKTOP SYSTEM
RemoteDesktopSystem
RemoteDesktopSystem là một ứng dụng điều khiển máy tính từ xa đơn giản được xây dựng trên nền tảng .NET 10. Hệ thống cho phép người dùng truyền hình ảnh màn hình theo thời gian thực, gửi tin nhắn chat, chuyển tập tin và điều khiển chuột/bàn phím giữa máy chủ (Server) và máy khách (Client).

# TÍNH NĂNG CHÍNH
Truyền hình ảnh màn hình (Screen Streaming): Chụp và nén ảnh màn hình định dạng JPEG để truyền tải với tốc độ cao (khoảng 10 FPS).
Điều khiển từ xa (Remote Control): Cho phép máy khách gửi các sự kiện chuột (di chuyển, click) để điều khiển máy chủ.
Chat đa điểm: Hệ thống nhắn tin thời gian thực giữa Server và tất cả các Client đang kết nối.
Chuyển tập tin (File Transfer): Gửi và nhận tệp tin giữa các máy thông qua mạng.
Bảo mật: Toàn bộ dữ liệu truyền gói tin (Packet) đều được mã hóa bằng thuật toán AES (16-bit key) trước khi gửi qua NetworkStream.
Quản lý người dùng: Xác thực đăng nhập thông qua cơ sở dữ liệu SQL Server.

# CÔNG NGHỆ SỬ DỤNG
Ngôn ngữ: C#

Framework: .NET 10.0, Windows Forms (WinForms)
Giao thức mạng: TCP/IP (TcpClient, TcpListener)
Bảo mật: Cryptography (AES Encryption)
Cơ sở dữ liệu: Microsoft SQL Server
Dữ liệu: JSON Serialization (System.Text.Json)

# CẤU TRÚC CÂY THƯ MỤC:

RemoteDesktopSystem/
├── RemoteDesktop.Client/              # Dự án phía người điều khiển (Client)
│   ├── Networking/
│   │   └── ClientHandler.cs           # Xử lý kết nối và gửi gói tin TCP
│   ├── frmConnect.cs                  # Giao diện nhập IP/Port để kết nối
│   ├── frmLogin.cs                    # Giao diện đăng nhập hệ thống
│   ├── frmRemote.cs                   # Giao diện chính để xem màn hình và điều khiển
│   ├── Program.cs                     # Điểm khởi chạy ứng dụng Client
│   └── RemoteDesktop.Client.csproj
├── RemoteDesktop.Server/              # Dự án phía máy bị điều khiển (Server)
│   ├── Database/
│   │   └── DatabaseManager.cs         # Quản lý kết nối SQL Server và xác thực người dùng
│   ├── Networking/
│   │   └── ServerHandler.cs           # Quản lý danh sách Client và điều phối gói tin
│   ├── Services/
│   │   ├── MouseHelper.cs             # Thư viện giả lập sự kiện chuột (Win32 API)
│   │   └── ScreenCapturer.cs          # Xử lý chụp và nén ảnh màn hình
│   ├── frmConnect.cs                  # Giao diện khởi động Server (mở Port)
│   ├── frmRemote.cs                   # Giao diện theo dõi trạng thái tại Server
│   ├── Program.cs                     # Điểm khởi chạy ứng dụng Server
│   └── RemoteDesktop.Server.csproj
├── RemoteDesktop.Common/              # Thư viện dùng chung cho cả Client và Server
│   ├── DTOs/                          # Các đối tượng chuyển đổi dữ liệu (Data Transfer Objects)
│   │   ├── FilePacketDTO.cs           # Cấu trúc gói tin chứa file
│   │   ├── InputDTO.cs                # Cấu trúc lệnh điều khiển chuột/phím
│   │   └── LoginDTO.cs                # Cấu trúc thông tin tài khoản
│   ├── Helpers/
│   │   ├── DataHelper.cs              # Serialize/Deserialize đối tượng sang Byte[] (JSON)
│   │   └── NetworkHelper.cs           # Hỗ trợ gửi/nhận gói tin có độ dài header
│   ├── Models/
│   │   ├── CommandType.cs             # Enum định nghĩa các loại lệnh (Login, Chat, v.v.)
│   │   └── Packet.cs                  # Cấu trúc gói tin cơ bản để truyền qua mạng
│   ├── Security/
│   │   └── AESEncryption.cs           # Xử lý mã hóa và giải mã dữ liệu
│   └── RemoteDesktop.Common.csproj
├── RemoteDesktopSystem.slnx           # Tệp quản lý Solution
└── README.md                          # Tài liệu hướng dẫn dự án

# CÀI ĐẶT CƠ BẢN
Cơ sở dữ liệu: Tạo Database có tên RemoteDesktopDB trong SQL Server. Cấu hình chuỗi kết nối (connection string) trong file DatabaseManager.cs.
Chạy Server: Mở project Server, chọn Port (mặc định 8000) và nhấn "Khởi động Server".
Chạy Client: Mở project Client, nhập IP của máy Server và kết nối.
