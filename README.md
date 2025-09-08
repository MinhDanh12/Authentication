# Authentication Module

Một module xác thực toàn diện được xây dựng bằng ASP.NET MVC với các tính năng bảo mật hiện đại.

## 🚀 Tính năng chính

### 🔐 Xác thực và Ủy quyền
- **Đăng nhập/Đăng ký** với email hoặc username
- **Đa loại người dùng**: End User, Admin, Partner, Moderator
- **Remember Me** với token an toàn (30 ngày)
- **Đăng xuất an toàn** với cleanup session

### 🛡️ Bảo mật
- **JWT Token** cho API authentication
- **Cookie Authentication** cho web interface
- **Session Management** với thời gian hết hạn
- **Password Encryption** với BCrypt
- **CSRF Protection**
- **Input Validation**

### 🎨 Giao diện người dùng
- **Responsive Design** với Bootstrap 5
- **Modern UI** với Font Awesome icons
- **Smooth Animations** và transitions
- **Mobile-friendly** interface

## 🏗️ Kiến trúc

### Models
- `ApplicationUser`: User entity với Identity
- `UserType`: Enum cho các loại người dùng
- `UserSession`: Quản lý phiên đăng nhập
- `LoginViewModel`/`RegisterViewModel`: View models

### Services
- `IAuthenticationService`: Interface cho authentication logic
- `AuthenticationService`: Implementation chính
- `IJwtService`: Interface cho JWT operations
- `JwtService`: JWT token generation và validation

### Controllers
- `AccountController`: Xử lý login, register, logout
- `HomeController`: Trang chủ với authorization

### Data
- `ApplicationDbContext`: Entity Framework context
- **In-Memory Database** cho development
- **SQL Server** cho production

## 🛠️ Công nghệ sử dụng

- **.NET 8.0**
- **ASP.NET MVC**
- **Entity Framework Core**
- **ASP.NET Identity**
- **JWT Bearer Authentication**
- **Bootstrap 5**
- **Font Awesome**
- **xUnit** cho Unit Testing
- **Moq** cho Mocking

## 📦 Cài đặt và Chạy

### Yêu cầu hệ thống
- .NET 8.0 SDK
- Visual Studio 2022 hoặc VS Code
- SQL Server (tùy chọn, có thể dùng In-Memory)

### Cài đặt
```bash
# Clone repository
git clone <repository-url>
cd Authentication

# Restore packages
dotnet restore

# Update database (nếu dùng SQL Server)
dotnet ef database update

# Chạy ứng dụng
dotnet run
```

### Cấu hình
Chỉnh sửa `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AuthenticationModuleDb;Trusted_Connection=true"
  },
  "Jwt": {
    "Key": "YourSecretKeyHere",
    "Issuer": "AuthenticationModule",
    "Audience": "AuthenticationModuleUsers",
    "ExpiryInMinutes": 60
  }
}
```

## 🧪 Testing

### Chạy Unit Tests
```bash
# Chạy tất cả tests
dotnet test

# Chạy với coverage
dotnet test --collect:"XPlat Code Coverage"

# Chạy specific test class
dotnet test --filter "AuthenticationServiceTests"
```

### Test Coverage
- **AuthenticationService**: 95%+ coverage
- **JwtService**: 90%+ coverage
- **Controllers**: 85%+ coverage

## 🔧 API Endpoints

### Authentication
- `POST /Account/Login` - Đăng nhập
- `POST /Account/Register` - Đăng ký
- `POST /Account/Logout` - Đăng xuất
- `GET /Account/AccessDenied` - Truy cập bị từ chối

### Application
- `GET /Home/Index` - Trang chủ (yêu cầu authentication)
- `GET /Home/Privacy` - Chính sách bảo mật

## 🔒 Bảo mật

### Password Policy
- Tối thiểu 6 ký tự
- Hỗ trợ chữ hoa, chữ thường, số
- Mã hóa với BCrypt

### Session Security
- JWT token với expiration
- Refresh token mechanism
- Secure cookie settings
- Session cleanup on logout

### Input Validation
- Server-side validation
- Client-side validation
- CSRF protection
- XSS prevention

## 📱 Responsive Design

- **Mobile-first** approach
- **Bootstrap 5** grid system
- **Flexible layouts** cho mọi screen size
- **Touch-friendly** interface

## 🚀 Deployment

### Development
```bash
dotnet run --environment Development
```

### Production
```bash
dotnet publish -c Release
# Deploy to IIS, Azure, hoặc Docker
```

## 📋 TODO / Roadmap

- [ ] **Password Reset** functionality
- [ ] **Email Verification** cho registration
- [ ] **Two-Factor Authentication** (2FA)
- [ ] **OAuth Integration** (Google, Facebook)
- [ ] **Role-based Authorization**
- [ ] **Audit Logging**
- [ ] **Rate Limiting**
- [ ] **API Documentation** với Swagger

## 🤝 Contributing

1. Fork repository
2. Tạo feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Tạo Pull Request

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

## 👨‍💻 Author

**Minh Danh** - [GitHub](https://github.com/MinhDanh12)

## 🙏 Acknowledgments

- ASP.NET Core team cho framework tuyệt vời
- Bootstrap team cho UI components
- Font Awesome cho icons
- Community cho các packages và libraries

---

**Lưu ý**: Đây là một module xác thực mẫu được phát triển cho mục đích học tập và demo. Trong môi trường production, cần thêm các biện pháp bảo mật bổ sung.
