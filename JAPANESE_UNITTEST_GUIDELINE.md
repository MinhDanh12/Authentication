# Unit Test Guideline

## 📋 Mục lục (目次)
1. [Giới thiệu](#giới-thiệu)
2. [Nguyên tắc cơ bản](#nguyên-tắc-cơ-bản)
3. [Cấu trúc test ](#cấu-trúc-test)
4. [Quy tắc đặt tên](#quy-tắc-đặt-tên)
5. [Mẫu test cases](#mẫu-test-cases)
6. [Test Coverage Requirements](#test-coverage-requirements)
7. [Code Review Checklist](#code-review-checklist)
8. [Best Practices](#best-practices)

---

## 🎯 Giới thiệu

Guideline này được thiết kế dựa trên chuẩn phát triển phần mềm của Nhật Bản, tập trung vào:
- **Chất lượng cao** (高品質 - Kōhinshitsu)
- **Chi tiết và tỉ mỉ** (詳細 - Shōsai)
- **Dễ bảo trì** (保守性 - Hoshūsei)
- **Tài liệu hóa đầy đủ** (文書化 - Bunsho-ka)

---

## 🏗️ Nguyên tắc cơ bản

### 1. **5W1H Principle** (5W1H原則)
Mỗi test case phải trả lời được:
- **Who** (誰): Ai là người dùng/actor
- **What** (何): Chức năng gì được test
- **When** (いつ): Điều kiện nào xảy ra
- **Where** (どこ): Môi trường/context nào
- **Why** (なぜ): Mục đích của test
- **How** (どのように): Cách thức thực hiện

### 2. **Kaizen Mindset** (改善思考)
- Liên tục cải thiện test coverage
- Refactor test code thường xuyên
- Học hỏi từ bugs để viết test tốt hơn

### 3. **Monozukuri Spirit** (ものづくり精神)
- Chú trọng vào chất lượng từng chi tiết nhỏ
- Test không chỉ để pass mà để đảm bảo chất lượng
- Tư duy long-term maintenance

---

## 📐 Cấu trúc test

### **AAA Pattern với Japanese Documentation**

```csharp
[Fact]
public async Task LoginAsync_正常なユーザー認証_成功を返す()
{
    // ■ 準備 (Arrange) - Chuẩn bị
    // 【目的】正常なユーザーでログインテスト
    // 【前提条件】アクティブなユーザーが存在する
    var validUser = CreateValidUser();
    var loginRequest = new LoginViewModel
    {
        EmailOrUsername = "test@example.com",
        Password = "ValidPassword123!",
        RememberMe = false
    };
    
    SetupMockUserManager(validUser);
    
    // ■ 実行 (Act) - Thực hiện
    var result = await _authService.LoginAsync(
        loginRequest.EmailOrUsername, 
        loginRequest.Password, 
        loginRequest.RememberMe);
    
    // ■ 検証 (Assert) - Kiểm tra
    // 【期待結果】認証成功
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.User);
    Assert.NotEmpty(result.Token);
    Assert.Equal(validUser.Email, result.User.Email);
    
    // 【副作用の確認】セッションが作成されているか
    VerifySessionCreated(validUser.Id);
}
```

### **Test Class Structure**

```csharp
/// <summary>
/// AuthenticationService のテストクラス
/// 【対象】認証サービスの全機能
/// 【責任】ログイン、登録、ログアウト機能のテスト
/// </summary>
[TestClass]
public class AuthenticationServiceTests : TestBase
{
    #region テストデータ (Test Data)
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<ApplicationDbContext> _contextMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly AuthenticationService _authService;
    #endregion

    #region セットアップ (Setup)
    public AuthenticationServiceTests()
    {
        // Initialize mocks and service
    }
    #endregion

    #region 正常系テスト (Happy Path Tests)
    [TestMethod]
    public async Task LoginAsync_正常なユーザー認証_成功を返す() { }
    
    [TestMethod] 
    public async Task RegisterAsync_有効なユーザー情報_登録成功() { }
    #endregion

    #region 異常系テスト (Error Path Tests)
    [TestMethod]
    public async Task LoginAsync_無効なパスワード_認証失敗を返す() { }
    
    [TestMethod]
    public async Task LoginAsync_存在しないユーザー_認証失敗を返す() { }
    #endregion

    #region 境界値テスト (Boundary Tests)
    [TestMethod]
    public async Task LoginAsync_最小パスワード長_正常動作() { }
    #endregion

    #region パフォーマンステスト (Performance Tests)
    [TestMethod]
    public async Task LoginAsync_大量同時アクセス_性能要件満足() { }
    #endregion

    #region ヘルパーメソッド (Helper Methods)
    private ApplicationUser CreateValidUser() { }
    private void SetupMockUserManager(ApplicationUser user) { }
    private void VerifySessionCreated(string userId) { }
    #endregion
}
```

---

## 📝 Quy tắc đặt tên

### **Method Naming Convention**

**Format**: `[MethodName]_[Condition]_[ExpectedResult]`

**Tiếng Nhật**: `[メソッド名]_[条件]_[期待結果]`

#### Ví dụ:
```csharp
// ✅ Good - Japanese Style
LoginAsync_正常なユーザー認証_成功を返す()
LoginAsync_無効なパスワード_認証失敗を返す()
LoginAsync_非アクティブユーザー_アクセス拒否を返す()
RegisterAsync_重複メール_エラーメッセージを返す()

// ✅ Good - English Style (Alternative)
LoginAsync_WithValidCredentials_ReturnsSuccess()
LoginAsync_WithInvalidPassword_ReturnsAuthFailure()
LoginAsync_WithInactiveUser_ReturnsAccessDenied()
RegisterAsync_WithDuplicateEmail_ReturnsErrorMessage()
```

### **Test Categories**

```csharp
// 正常系 (Happy Path)
[Category("正常系")]
[Category("HappyPath")]

// 異常系 (Error Path)  
[Category("異常系")]
[Category("ErrorPath")]

// 境界値 (Boundary)
[Category("境界値")]
[Category("Boundary")]

// 統合テスト (Integration)
[Category("統合テスト")]
[Category("Integration")]

// パフォーマンス (Performance)
[Category("性能テスト")]
[Category("Performance")]
```

---

## 🧪 Mẫu test cases

### **1. Controller Test Example**

```csharp
/// <summary>
/// AccountController のログイン機能テスト
/// 【シナリオ】正常なログイン処理
/// </summary>
[Fact]
public async Task Login_Post_正常なログイン情報_ホーム画面にリダイレクト()
{
    // ■ 準備 (Arrange)
    // 【テストデータ】
    var loginModel = new LoginViewModel
    {
        EmailOrUsername = "test@example.com",
        Password = "ValidPassword123!",
        RememberMe = false
    };

    var expectedAuthResult = new AuthenticationResult
    {
        IsSuccess = true,
        User = new ApplicationUser
        {
            Id = "user123",
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UserType = UserType.EndUser,
            IsActive = true
        },
        Token = "valid-jwt-token",
        ExpiresAt = DateTime.UtcNow.AddHours(1)
    };

    // 【モック設定】
    _authServiceMock
        .Setup(x => x.LoginAsync(loginModel.EmailOrUsername, loginModel.Password, loginModel.RememberMe))
        .ReturnsAsync(expectedAuthResult);

    SetupControllerContext();

    // ■ 実行 (Act)
    var result = await _controller.Login(loginModel);

    // ■ 検証 (Assert)
    // 【主要検証】リダイレクト結果
    var redirectResult = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("Index", redirectResult.ActionName);
    Assert.Equal("Home", redirectResult.ControllerName);

    // 【副作用検証】認証サービス呼び出し確認
    _authServiceMock.Verify(
        x => x.LoginAsync(loginModel.EmailOrUsername, loginModel.Password, loginModel.RememberMe),
        Times.Once,
        "認証サービスが正確に呼び出されること");
}
```

### **2. Service Test Example**

```csharp
/// <summary>
/// AuthenticationService のユーザー登録テスト
/// 【シナリオ】重複メールアドレスでの登録試行
/// </summary>
[Fact]
public async Task RegisterAsync_重複メールアドレス_エラーメッセージを返す()
{
    // ■ 準備 (Arrange)
    // 【既存ユーザー設定】
    var existingUser = new ApplicationUser
    {
        Id = "existing123",
        Email = "duplicate@example.com",
        UserName = "existing",
        IsActive = true
    };

    // 【登録試行データ】
    var registerModel = new RegisterViewModel
    {
        FirstName = "New",
        LastName = "User",
        Email = "duplicate@example.com", // 重複メール
        UserName = "newuser",
        Password = "NewPassword123!",
        ConfirmPassword = "NewPassword123!",
        UserType = UserType.EndUser
    };

    // 【モック設定】既存ユーザーが見つかる
    _userManagerMock
        .Setup(x => x.Users)
        .Returns(new List<ApplicationUser> { existingUser }.AsQueryable());

    // ■ 実行 (Act)
    var result = await _authService.RegisterAsync(registerModel);

    // ■ 検証 (Assert)
    // 【期待結果】登録失敗
    Assert.False(result.IsSuccess);
    Assert.NotNull(result.ErrorMessage);
    Assert.Contains("既に使用", result.ErrorMessage);
    Assert.Null(result.User);

    // 【副作用検証】新規ユーザー作成が呼ばれていないこと
    _userManagerMock.Verify(
        x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
        Times.Never,
        "重複チェック後は新規作成処理が呼ばれないこと");
}
```

### **3. Integration Test Example**

```csharp
/// <summary>
/// エンドツーエンドログインテスト
/// 【シナリオ】実際のDBを使用した完全なログインフロー
/// </summary>
[Fact]
public async Task IntegrationTest_完全なログインフロー_成功()
{
    // ■ 準備 (Arrange)
    // 【テスト用DB設定】
    using var scope = _factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SeedTestUser(context);

    var client = _factory.CreateClient();

    // 【ログインデータ】
    var loginData = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("EmailOrUsername", "integration@test.com"),
        new KeyValuePair<string, string>("Password", "IntegrationTest123!"),
        new KeyValuePair<string, string>("RememberMe", "false")
    });

    // ■ 実行 (Act)
    var response = await client.PostAsync("/Account/Login", loginData);

    // ■ 検証 (Assert)
    // 【HTTPレスポンス検証】
    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Equal("/Home/Index", response.Headers.Location?.ToString());

    // 【データベース状態検証】
    var userSessions = await context.UserSessions
        .Where(s => s.User.Email == "integration@test.com" && s.IsActive)
        .ToListAsync();
    
    Assert.Single(userSessions);
    Assert.True(userSessions[0].ExpiresAt > DateTime.UtcNow);

    // 【認証Cookie検証】
    var cookies = response.Headers.GetValues("Set-Cookie");
    Assert.Contains(cookies, c => c.Contains(".AspNetCore.Identity.Application"));
}
```

---

## 📊 Test Coverage Requirements

### **Minimum Coverage Standards**

| Component | Coverage | 説明 |
|-----------|----------|------|
| Controllers | 85% | UI層の主要パス |
| Services | 95% | ビジネスロジック層 |
| Models | 80% | データ検証ロジック |
| Utilities | 90% | 共通機能 |
| **Overall** | **90%** | **プロジェクト全体** |

### **Coverage Verification**

```bash
# テスト実行とカバレッジ測定
dotnet test --collect:"XPlat Code Coverage"

# レポート生成
reportgenerator -reports:"TestResults\*\coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html

# カバレッジ確認
start CoverageReport\index.html
```

### **Quality Gates**

```csharp
// テストプロジェクトの設定
[assembly: AssemblyMetadata("MinimumCoverage", "90")]
[assembly: AssemblyMetadata("CoverageExclusions", "*.Designer.cs;*AssemblyInfo.cs")]
```

---

## ✅ Code Review Checklist

### **Test Code Review Points**

#### **🔍 基本チェック項目**
- [ ] **命名規則**: メソッド名が5W1Hを満たしているか
- [ ] **AAA構造**: Arrange/Act/Assertが明確に分離されているか  
- [ ] **単一責任**: 1つのテストで1つの機能のみをテストしているか
- [ ] **独立性**: テストが他のテストに依存していないか
- [ ] **再現性**: 何度実行しても同じ結果が得られるか

#### **📝 ドキュメンテーション**
- [ ] **日本語コメント**: 複雑なロジックに適切な説明があるか
- [ ] **テスト目的**: なぜこのテストが必要かが明確か
- [ ] **前提条件**: テスト実行の前提が文書化されているか
- [ ] **期待結果**: 何を検証しているかが明確か

#### **🧪 テスト品質**
- [ ] **正常系**: Happy pathがカバーされているか
- [ ] **異常系**: Error pathがカバーされているか  
- [ ] **境界値**: Edge caseがテストされているか
- [ ] **Mock使用**: 適切にモックが使用されているか
- [ ] **Assertion**: 適切な検証が行われているか

#### **⚡ パフォーマンス**
- [ ] **実行時間**: テストが高速に実行されるか（<100ms）
- [ ] **リソース使用**: メモリリークがないか
- [ ] **並列実行**: 並列実行時に問題がないか

### **Review Comment Templates**

```csharp
// ✅ Good Review Comments
"// 💡 提案: テスト名に期待結果を含めることで、より明確になります"
"// ✨ 良い点: AAA構造が明確で読みやすいです"  
"// 🔍 質問: この境界値テストの根拠は何でしょうか？"
"// 🚨 問題: Mockの設定が不完全で、NullReferenceExceptionの可能性があります"

// ❌ Poor Review Comments  
"直して"
"これダメ"
"なんで？"
```

---

## 🎯 Best Practices

### **1. Test Data Management (テストデータ管理)**

```csharp
/// <summary>
/// テストデータファクトリー
/// 【目的】一貫性のあるテストデータ作成
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// 有効なユーザーを作成
    /// 【用途】正常系テスト用
    /// </summary>
    public static ApplicationUser CreateValidUser(string email = "test@example.com")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email.Split('@')[0],
            Email = email,
            FirstName = "テスト",
            LastName = "ユーザー",
            UserType = UserType.EndUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };
    }

    /// <summary>
    /// 無効なユーザーを作成
    /// 【用途】異常系テスト用
    /// </summary>
    public static ApplicationUser CreateInactiveUser()
    {
        var user = CreateValidUser("inactive@example.com");
        user.IsActive = false;
        return user;
    }
}
```

### **2. Custom Assertions (カスタム検証)**

```csharp
/// <summary>
/// 認証結果専用のアサーション
/// 【目的】テストコードの可読性向上
/// </summary>
public static class AuthenticationAssertions
{
    /// <summary>
    /// 認証成功の検証
    /// </summary>
    public static void ShouldBeSuccessfulLogin(this AuthenticationResult result, string expectedEmail)
    {
        Assert.True(result.IsSuccess, "認証が成功すること");
        Assert.NotNull(result.User);
        Assert.NotEmpty(result.Token);
        Assert.Equal(expectedEmail, result.User.Email);
        Assert.True(result.ExpiresAt > DateTime.UtcNow, "有効期限が未来の時刻であること");
    }

    /// <summary>
    /// 認証失敗の検証
    /// </summary>
    public static void ShouldBeFailedLogin(this AuthenticationResult result, string expectedErrorMessage)
    {
        Assert.False(result.IsSuccess, "認証が失敗すること");
        Assert.Null(result.User);
        Assert.Empty(result.Token);
        Assert.Contains(expectedErrorMessage, result.ErrorMessage);
    }
}

// 使用例
[Fact]
public async Task LoginAsync_正常認証_成功結果を返す()
{
    // Arrange & Act
    var result = await _authService.LoginAsync("test@example.com", "password", false);
    
    // Assert - カスタムアサーション使用
    result.ShouldBeSuccessfulLogin("test@example.com");
}
```

### **3. Test Base Classes (テストベースクラス)**

```csharp
/// <summary>
/// 認証関連テストの基底クラス
/// 【目的】共通セットアップの提供
/// </summary>
public abstract class AuthenticationTestBase : IDisposable
{
    protected readonly Mock<UserManager<ApplicationUser>> UserManagerMock;
    protected readonly Mock<SignInManager<ApplicationUser>> SignInManagerMock;
    protected readonly Mock<ApplicationDbContext> ContextMock;
    protected readonly Mock<IJwtService> JwtServiceMock;
    protected readonly AuthenticationService AuthService;

    protected AuthenticationTestBase()
    {
        // 共通モック初期化
        UserManagerMock = MockUserManager<ApplicationUser>();
        SignInManagerMock = MockSignInManager();
        ContextMock = new Mock<ApplicationDbContext>();
        JwtServiceMock = new Mock<IJwtService>();

        AuthService = new AuthenticationService(
            UserManagerMock.Object,
            SignInManagerMock.Object, 
            ContextMock.Object,
            JwtServiceMock.Object);
    }

    /// <summary>
    /// UserManagerのモック作成
    /// 【参考】https://stackoverflow.com/questions/49165810/how-to-mock-usermanager-in-net-core-testing
    /// </summary>
    protected static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
        mgr.Object.UserValidators.Add(new UserValidator<TUser>());
        mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());
        return mgr;
    }

    /// <summary>
    /// SignInManagerのモック作成
    /// </summary>
    protected Mock<SignInManager<ApplicationUser>> MockSignInManager()
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        var logger = new Mock<ILogger<SignInManager<ApplicationUser>>>();

        return new Mock<SignInManager<ApplicationUser>>(
            UserManagerMock.Object,
            contextAccessor.Object,
            userPrincipalFactory.Object,
            options.Object,
            logger.Object,
            null,
            null);
    }

    public virtual void Dispose()
    {
        // リソースクリーンアップ
        GC.SuppressFinalize(this);
    }
}
```

### **4. Performance Testing (性能テスト)**

```csharp
/// <summary>
/// パフォーマンステスト
/// 【要件】ログイン処理は100ms以内で完了すること
/// </summary>
[Fact]
public async Task LoginAsync_パフォーマンス要件_100ms以内で完了()
{
    // Arrange
    var user = TestDataFactory.CreateValidUser();
    SetupSuccessfulLogin(user);

    var stopwatch = Stopwatch.StartNew();

    // Act
    var result = await _authService.LoginAsync("test@example.com", "password", false);

    // Assert
    stopwatch.Stop();
    
    Assert.True(result.IsSuccess);
    Assert.True(stopwatch.ElapsedMilliseconds < 100, 
        $"ログイン処理は100ms以内で完了すること。実際: {stopwatch.ElapsedMilliseconds}ms");
}

/// <summary>
/// 負荷テスト
/// 【要件】同時100ユーザーのログインを処理できること
/// </summary>
[Fact]
public async Task LoginAsync_負荷テスト_同時100ユーザー処理()
{
    // Arrange
    const int concurrentUsers = 100;
    var tasks = new List<Task<AuthenticationResult>>();

    // Act
    for (int i = 0; i < concurrentUsers; i++)
    {
        var email = $"user{i}@example.com";
        var user = TestDataFactory.CreateValidUser(email);
        SetupSuccessfulLogin(user);
        
        tasks.Add(_authService.LoginAsync(email, "password", false));
    }

    var results = await Task.WhenAll(tasks);

    // Assert
    Assert.All(results, result => Assert.True(result.IsSuccess));
    Assert.Equal(concurrentUsers, results.Count(r => r.IsSuccess));
}
```

---

## 📚 参考資料

### **Japanese Software Testing Standards**
- [JIS X 0129-1](https://www.jisc.go.jp/) - ソフトウェアテスト技法
- [JSTQB](https://jstqb.jp/) - ソフトウェアテスト技術者資格認定
- [SEC](https://www.ipa.go.jp/sec/) - ソフトウェア・エンジニアリング・センター

### **Tools & Frameworks**
- **xUnit**: Primary testing framework
- **Moq**: Mocking framework  
- **FluentAssertions**: Better assertions
- **Coverlet**: Code coverage
- **ReportGenerator**: Coverage reports

### **Books (推奨書籍)**
- 『xUnit Test Patterns』- Gerard Meszaros
- 『単体テストの考え方/使い方』- Vladimir Khorikov  
- 『Clean Code』- Robert C. Martin
- 『ソフトウェアテスト293の鉄則』- Cem Kaner

---

## 🚀 Getting Started

### **1. プロジェクトセットアップ**
```bash
# テストプロジェクト作成
dotnet new xunit -n YourProject.Tests
cd YourProject.Tests

# 必要パッケージ追加
dotnet add package Moq
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package FluentAssertions
dotnet add package coverlet.collector

# プロジェクト参照追加
dotnet add reference ../YourProject/YourProject.csproj
```

### **2. 初回テスト作成**
```csharp
// 最初のテストクラス作成
public class SampleServiceTests : AuthenticationTestBase
{
    [Fact]
    public void FirstTest_基本動作確認_正常終了()
    {
        // この guideline に従って最初のテストを作成
        Assert.True(true, "テスト環境が正常にセットアップされていること");
    }
}
```
---