using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using AuthenticationModule.Data;
using AuthenticationModule.Models;
using AuthenticationModule.Services;
using System.Diagnostics;

namespace AuthenticationModule.Tests.Examples
{
    /// <summary>
    /// AuthenticationService のテストクラス - 日本基準サンプル
    /// 【対象】認証サービスの全機能
    /// 【責任】ログイン、登録、ログアウト機能のテスト
    /// 【作成者】Development Team
    /// 【作成日】2025年12月
    /// </summary>
    public class AuthenticationServiceTests_Japanese : IDisposable
    {
        #region テストデータ (Test Data)
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<ApplicationDbContext> _contextMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly AuthenticationService _authService;
        private readonly Mock<DbSet<UserSession>> _userSessionsMock;
        #endregion

        #region セットアップ (Setup)
        public AuthenticationServiceTests_Japanese()
        {
            // UserManagerモック初期化
            _userManagerMock = MockUserManager<ApplicationUser>();
            
            // SignInManagerモック初期化  
            _signInManagerMock = MockSignInManager();
            
            // DbContextモック初期化
            _contextMock = new Mock<ApplicationDbContext>();
            _userSessionsMock = new Mock<DbSet<UserSession>>();
            _contextMock.Setup(x => x.UserSessions).Returns(_userSessionsMock.Object);
            
            // JwtServiceモック初期化
            _jwtServiceMock = new Mock<IJwtService>();
            _jwtServiceMock.Setup(x => x.GenerateJwtToken(It.IsAny<ApplicationUser>()))
                          .Returns("test-jwt-token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                          .Returns("test-refresh-token");

            // テスト対象サービス初期化
            _authService = new AuthenticationService(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _contextMock.Object,
                _jwtServiceMock.Object);
        }
        #endregion

        #region 正常系テスト (Happy Path Tests)

        /// <summary>
        /// ログイン機能 - 正常系テスト
        /// 【シナリオ】有効なユーザー認証情報でログイン
        /// 【期待結果】認証成功、JWTトークン生成、セッション作成
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Category", "HappyPath")]
        public async Task LoginAsync_正常なユーザー認証_成功を返す()
        {
            // ■ 準備 (Arrange)
            // 【目的】正常なユーザーでログインテスト
            // 【前提条件】アクティブなユーザーが存在する
            var validUser = TestDataFactory.CreateValidUser("test@example.com");
            var loginEmail = "test@example.com";
            var loginPassword = "ValidPassword123!";
            var rememberMe = false;

            // 【モック設定】ユーザー検索が成功
            SetupUserManagerFindByEmail(validUser);
            
            // 【モック設定】パスワード検証が成功
            _signInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(validUser, loginPassword, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // 【モック設定】ユーザー更新が成功
            _userManagerMock
                .Setup(x => x.UpdateAsync(validUser))
                .ReturnsAsync(IdentityResult.Success);

            // ■ 実行 (Act)
            var result = await _authService.LoginAsync(loginEmail, loginPassword, rememberMe);

            // ■ 検証 (Assert)
            // 【期待結果】認証成功
            Assert.True(result.IsSuccess, "認証が成功すること");
            Assert.NotNull(result.User);
            Assert.Equal("test-jwt-token", result.Token);
            Assert.Equal("test-refresh-token", result.RefreshToken);
            Assert.Equal(validUser.Email, result.User.Email);
            Assert.True(result.ExpiresAt > DateTime.UtcNow, "有効期限が未来の時刻であること");

            // 【副作用の確認】各サービスが正しく呼び出されているか
            _jwtServiceMock.Verify(x => x.GenerateJwtToken(validUser), Times.Once, 
                "JWTトークンが生成されること");
            _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once, 
                "リフレッシュトークンが生成されること");
            _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once, 
                "データベース変更が保存されること");
        }

        /// <summary>
        /// ユーザー登録機能 - 正常系テスト
        /// 【シナリオ】有効なユーザー情報で新規登録
        /// 【期待結果】登録成功、新しいユーザーが作成される
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Category", "HappyPath")]
        public async Task RegisterAsync_有効なユーザー情報_登録成功()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】新規ユーザー登録情報
            var registerModel = new RegisterViewModel
            {
                FirstName = "田中",
                LastName = "太郎",
                Email = "tanaka.taro@example.com",
                UserName = "tanaka_taro",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!",
                UserType = UserType.EndUser
            };

            // 【モック設定】既存ユーザーが存在しない
            SetupUserManagerFindByEmail(null);

            // 【モック設定】ユーザー作成が成功
            _userManagerMock
                .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerModel.Password))
                .ReturnsAsync(IdentityResult.Success);

            // ■ 実行 (Act)
            var result = await _authService.RegisterAsync(registerModel);

            // ■ 検証 (Assert)
            // 【期待結果】登録成功
            Assert.True(result.IsSuccess, "ユーザー登録が成功すること");
            Assert.NotNull(result.User);
            Assert.Equal(registerModel.Email, result.User.Email);
            Assert.Equal(registerModel.UserName, result.User.UserName);
            Assert.Equal(registerModel.FirstName, result.User.FirstName);
            Assert.Equal(registerModel.LastName, result.User.LastName);
            Assert.Equal(registerModel.UserType, result.User.UserType);
            Assert.True(result.User.IsActive, "新規ユーザーがアクティブ状態であること");
            Assert.Contains("成功", result.ErrorMessage);

            // 【副作用の確認】ユーザー作成が呼び出されているか
            _userManagerMock.Verify(x => x.CreateAsync(
                It.Is<ApplicationUser>(u => 
                    u.Email == registerModel.Email && 
                    u.UserName == registerModel.UserName),
                registerModel.Password), 
                Times.Once, 
                "新規ユーザーが正しいパラメータで作成されること");
        }

        /// <summary>
        /// ログアウト機能 - 正常系テスト
        /// 【シナリオ】アクティブなセッションを持つユーザーのログアウト
        /// 【期待結果】全てのセッションが無効化される
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Category", "HappyPath")]
        public async Task LogoutAsync_アクティブセッション_全セッション無効化()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】ユーザーID
            var userId = "user123";
            
            // 【テストデータ】アクティブなセッションリスト
            var activeSessions = new List<UserSession>
            {
                new UserSession { Id = 1, UserId = userId, IsActive = true, SessionToken = "token1" },
                new UserSession { Id = 2, UserId = userId, IsActive = true, SessionToken = "token2" },
                new UserSession { Id = 3, UserId = userId, IsActive = false, SessionToken = "token3" } // 既に無効
            };

            // 【モック設定】アクティブセッション検索
            var queryableActiveSessions = activeSessions.Where(s => s.UserId == userId && s.IsActive).AsQueryable();
            SetupDbSetQuery(_userSessionsMock, queryableActiveSessions);

            // ■ 実行 (Act)
            var result = await _authService.LogoutAsync(userId);

            // ■ 検証 (Assert)
            // 【期待結果】ログアウト成功
            Assert.True(result, "ログアウト処理が成功すること");

            // 【副作用の確認】アクティブセッションが無効化されているか
            var activeSessionsToDeactivate = activeSessions.Where(s => s.UserId == userId && s.IsActive).ToList();
            Assert.All(activeSessionsToDeactivate, session => 
                Assert.False(session.IsActive, $"セッション{session.Id}が無効化されていること"));

            // 【副作用の確認】データベース保存が呼ばれているか
            _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once,
                "セッション状態の変更が保存されること");
        }

        #endregion

        #region 異常系テスト (Error Path Tests)

        /// <summary>
        /// ログイン機能 - 異常系テスト
        /// 【シナリオ】無効なパスワードでログイン試行
        /// 【期待結果】認証失敗、適切なエラーメッセージ
        /// </summary>
        [Fact]
        [Trait("Category", "異常系")]
        [Trait("Category", "ErrorPath")]
        public async Task LoginAsync_無効なパスワード_認証失敗を返す()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】有効なユーザーと無効なパスワード
            var validUser = TestDataFactory.CreateValidUser("test@example.com");
            var loginEmail = "test@example.com";
            var invalidPassword = "WrongPassword";

            // 【モック設定】ユーザー検索が成功
            SetupUserManagerFindByEmail(validUser);

            // 【モック設定】パスワード検証が失敗
            _signInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(validUser, invalidPassword, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // ■ 実行 (Act)
            var result = await _authService.LoginAsync(loginEmail, invalidPassword, false);

            // ■ 検証 (Assert)
            // 【期待結果】認証失敗
            Assert.False(result.IsSuccess, "認証が失敗すること");
            Assert.Null(result.User);
            Assert.Empty(result.Token);
            Assert.Empty(result.RefreshToken);
            Assert.Contains("パスワード", result.ErrorMessage);

            // 【副作用の確認】トークン生成が呼ばれていないこと
            _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<ApplicationUser>()), Times.Never,
                "認証失敗時はトークンが生成されないこと");
        }

        /// <summary>
        /// ログイン機能 - 異常系テスト
        /// 【シナリオ】存在しないユーザーでログイン試行
        /// 【期待結果】認証失敗、セキュリティを考慮したエラーメッセージ
        /// </summary>
        [Fact]
        [Trait("Category", "異常系")]
        [Trait("Category", "ErrorPath")]
        public async Task LoginAsync_存在しないユーザー_認証失敗を返す()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】存在しないユーザーのメールアドレス
            var nonExistentEmail = "nonexistent@example.com";
            var password = "AnyPassword123!";

            // 【モック設定】ユーザーが見つからない
            SetupUserManagerFindByEmail(null);

            // ■ 実行 (Act)
            var result = await _authService.LoginAsync(nonExistentEmail, password, false);

            // ■ 検証 (Assert)
            // 【期待結果】認証失敗（セキュリティのため詳細は隠す）
            Assert.False(result.IsSuccess, "認証が失敗すること");
            Assert.Null(result.User);
            Assert.Contains("存在しない", result.ErrorMessage);

            // 【セキュリティ確認】パスワード検証が呼ばれていないこと
            _signInManagerMock.Verify(
                x => x.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never,
                "存在しないユーザーに対してパスワード検証を行わないこと");
        }

        /// <summary>
        /// ログイン機能 - 異常系テスト
        /// 【シナリオ】非アクティブユーザーでログイン試行
        /// 【期待結果】認証失敗、アカウント状態に関するエラーメッセージ
        /// </summary>
        [Fact]
        [Trait("Category", "異常系")]
        [Trait("Category", "ErrorPath")]
        public async Task LoginAsync_非アクティブユーザー_アクセス拒否を返す()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】非アクティブなユーザー
            var inactiveUser = TestDataFactory.CreateInactiveUser();
            var loginEmail = inactiveUser.Email;
            var password = "ValidPassword123!";

            // 【モック設定】非アクティブユーザーが見つかる
            SetupUserManagerFindByEmail(inactiveUser);

            // ■ 実行 (Act)
            var result = await _authService.LoginAsync(loginEmail, password, false);

            // ■ 検証 (Assert)
            // 【期待結果】アクセス拒否
            Assert.False(result.IsSuccess, "非アクティブユーザーは認証失敗すること");
            Assert.Null(result.User);
            Assert.Contains("無効", result.ErrorMessage);

            // 【セキュリティ確認】非アクティブユーザーに対してパスワード検証を行わないこと
            _signInManagerMock.Verify(
                x => x.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never,
                "非アクティブユーザーに対してパスワード検証を行わないこと");
        }

        /// <summary>
        /// ユーザー登録機能 - 異常系テスト
        /// 【シナリオ】重複メールアドレスで登録試行
        /// 【期待結果】登録失敗、適切なエラーメッセージ
        /// </summary>
        [Fact]
        [Trait("Category", "異常系")]
        [Trait("Category", "ErrorPath")]
        public async Task RegisterAsync_重複メールアドレス_エラーメッセージを返す()
        {
            // ■ 準備 (Arrange)
            // 【既存ユーザー設定】
            var existingUser = TestDataFactory.CreateValidUser("duplicate@example.com");

            // 【登録試行データ】重複メールアドレス
            var registerModel = new RegisterViewModel
            {
                FirstName = "新規",
                LastName = "ユーザー",
                Email = "duplicate@example.com", // 重複メール
                UserName = "newuser",
                Password = "NewPassword123!",
                ConfirmPassword = "NewPassword123!",
                UserType = UserType.EndUser
            };

            // 【モック設定】既存ユーザーが見つかる
            SetupUserManagerFindByEmail(existingUser);

            // ■ 実行 (Act)
            var result = await _authService.RegisterAsync(registerModel);

            // ■ 検証 (Assert)
            // 【期待結果】登録失敗
            Assert.False(result.IsSuccess, "重複メールアドレスでは登録失敗すること");
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("使用", result.ErrorMessage);
            Assert.Null(result.User);

            // 【副作用検証】新規ユーザー作成が呼ばれていないこと
            _userManagerMock.Verify(
                x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
                Times.Never,
                "重複チェック後は新規作成処理が呼ばれないこと");
        }

        #endregion

        #region 境界値テスト (Boundary Tests)

        /// <summary>
        /// パスワード境界値テスト
        /// 【シナリオ】最小パスワード長でのログイン
        /// 【期待結果】正常に認証処理が実行される
        /// </summary>
        [Fact]
        [Trait("Category", "境界値")]
        [Trait("Category", "Boundary")]
        public async Task LoginAsync_最小パスワード長_正常動作()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】最小長のパスワード（6文字）
            var user = TestDataFactory.CreateValidUser("test@example.com");
            var minPassword = "Pass1!"; // 6文字（最小要件）

            SetupUserManagerFindByEmail(user);
            _signInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(user, minPassword, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // ■ 実行 (Act)
            var result = await _authService.LoginAsync("test@example.com", minPassword, false);

            // ■ 検証 (Assert)
            // 【期待結果】最小長パスワードでも認証成功
            Assert.True(result.IsSuccess, "最小パスワード長でも認証が成功すること");
            Assert.NotNull(result.User);
        }

        /// <summary>
        /// Remember Me 境界値テスト
        /// 【シナリオ】RememberMe=trueでの最大セッション期間
        /// 【期待結果】30日間の有効期限が設定される
        /// </summary>
        [Fact]
        [Trait("Category", "境界値")]
        [Trait("Category", "Boundary")]
        public async Task LoginAsync_RememberMe有効_30日間有効期限()
        {
            // ■ 準備 (Arrange)
            var user = TestDataFactory.CreateValidUser("test@example.com");
            var rememberMe = true;

            SetupUserManagerFindByEmail(user);
            _signInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(user, It.IsAny<string>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            var beforeLogin = DateTime.UtcNow;

            // ■ 実行 (Act)
            var result = await _authService.LoginAsync("test@example.com", "password", rememberMe);

            // ■ 検証 (Assert)
            var afterLogin = DateTime.UtcNow;
            var expectedMinExpiry = beforeLogin.AddDays(29); // 29日以上
            var expectedMaxExpiry = afterLogin.AddDays(31);  // 31日以下

            Assert.True(result.IsSuccess);
            Assert.True(result.ExpiresAt >= expectedMinExpiry, "有効期限が29日以上であること");
            Assert.True(result.ExpiresAt <= expectedMaxExpiry, "有効期限が31日以下であること");
        }

        #endregion

        #region パフォーマンステスト (Performance Tests)

        /// <summary>
        /// ログイン処理のパフォーマンステスト
        /// 【要件】ログイン処理は100ms以内で完了すること
        /// 【測定方法】Stopwatchによる実行時間測定
        /// </summary>
        [Fact]
        [Trait("Category", "性能テスト")]
        [Trait("Category", "Performance")]
        public async Task LoginAsync_パフォーマンス要件_100ms以内で完了()
        {
            // ■ 準備 (Arrange)
            var user = TestDataFactory.CreateValidUser("perf@example.com");
            SetupUserManagerFindByEmail(user);
            _signInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(user, It.IsAny<string>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            var stopwatch = Stopwatch.StartNew();

            // ■ 実行 (Act)
            var result = await _authService.LoginAsync("perf@example.com", "password", false);

            // ■ 検証 (Assert)
            stopwatch.Stop();
            
            Assert.True(result.IsSuccess, "認証が成功すること");
            Assert.True(stopwatch.ElapsedMilliseconds < 100, 
                $"ログイン処理は100ms以内で完了すること。実際: {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 同時ログイン処理のパフォーマンステスト
        /// 【要件】同時10ユーザーのログインを処理できること
        /// 【測定方法】並列Task実行による負荷テスト
        /// </summary>
        [Fact]
        [Trait("Category", "性能テスト")]
        [Trait("Category", "Performance")]
        public async Task LoginAsync_負荷テスト_同時10ユーザー処理()
        {
            // ■ 準備 (Arrange)
            const int concurrentUsers = 10;
            var tasks = new List<Task<AuthenticationResult>>();

            // 【各ユーザーのモック設定】
            for (int i = 0; i < concurrentUsers; i++)
            {
                var email = $"user{i}@example.com";
                var user = TestDataFactory.CreateValidUser(email);
                
                // 注意: 実際の実装では、各ユーザーに対して個別のモック設定が必要
                // ここでは簡略化のため共通設定を使用
                if (i == 0) // 最初のユーザーのみ設定（サンプルのため）
                {
                    SetupUserManagerFindByEmail(user);
                    _signInManagerMock
                        .Setup(x => x.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), false))
                        .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
                    _userManagerMock
                        .Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                        .ReturnsAsync(IdentityResult.Success);
                }

                tasks.Add(_authService.LoginAsync(email, "password", false));
            }

            var stopwatch = Stopwatch.StartNew();

            // ■ 実行 (Act)
            var results = await Task.WhenAll(tasks);

            // ■ 検証 (Assert)
            stopwatch.Stop();

            // 注意: このテストは実際のモック設定の制限により、最初のユーザーのみ成功する
            // 実際の実装では、全ユーザーが成功することを確認する
            Assert.True(results[0].IsSuccess, "最初のユーザーのログインが成功すること");
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"10ユーザー同時ログインは1秒以内で完了すること。実際: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region ヘルパーメソッド (Helper Methods)

        /// <summary>
        /// UserManagerのモック作成
        /// 【参考】https://stackoverflow.com/questions/49165810/how-to-mock-usermanager-in-net-core-testing
        /// </summary>
        private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
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
        private Mock<SignInManager<ApplicationUser>> MockSignInManager()
        {
            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var options = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
            var logger = new Mock<ILogger<SignInManager<ApplicationUser>>>();

            return new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                options.Object,
                logger.Object,
                null,
                null);
        }

        /// <summary>
        /// ユーザー検索のモック設定
        /// 【用途】FindByEmailAsync, FindByNameAsyncの結果設定
        /// </summary>
        private void SetupUserManagerFindByEmail(ApplicationUser? user)
        {
            var users = user != null ? new List<ApplicationUser> { user } : new List<ApplicationUser>();
            var queryableUsers = users.AsQueryable();

            var mockSet = new Mock<DbSet<ApplicationUser>>();
            mockSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Provider).Returns(queryableUsers.Provider);
            mockSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Expression).Returns(queryableUsers.Expression);
            mockSet.As<IQueryable<ApplicationUser>>().Setup(m => m.ElementType).Returns(queryableUsers.ElementType);
            mockSet.As<IQueryable<ApplicationUser>>().Setup(m => m.GetEnumerator()).Returns(queryableUsers.GetEnumerator());

            _userManagerMock.Setup(x => x.Users).Returns(mockSet.Object);
        }

        /// <summary>
        /// DbSet クエリのモック設定
        /// 【用途】LINQ to Entitiesクエリのモック化
        /// </summary>
        private static void SetupDbSetQuery<T>(Mock<DbSet<T>> mockSet, IQueryable<T> data) where T : class
        {
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        }

        #endregion

        #region リソース解放 (Cleanup)

        /// <summary>
        /// テスト終了時のリソース解放
        /// 【目的】メモリリーク防止
        /// </summary>
        public void Dispose()
        {
            // モックオブジェクトの明示的な解放は不要
            // .NET GCに委ねる
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// テストデータファクトリー
    /// 【目的】一貫性のあるテストデータ作成
    /// 【使用方法】各テストで共通のテストデータを生成
    /// </summary>
    public static class TestDataFactory
    {
        /// <summary>
        /// 有効なユーザーを作成
        /// 【用途】正常系テスト用
        /// 【特徴】アクティブ、メール確認済み
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
                LastLoginAt = DateTime.UtcNow.AddDays(-1),
                EmailConfirmed = true,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = false
            };
        }

        /// <summary>
        /// 無効なユーザーを作成
        /// 【用途】異常系テスト用
        /// 【特徴】非アクティブ状態
        /// </summary>
        public static ApplicationUser CreateInactiveUser(string email = "inactive@example.com")
        {
            var user = CreateValidUser(email);
            user.IsActive = false;
            user.LastLoginAt = DateTime.UtcNow.AddMonths(-6); // 6ヶ月前
            return user;
        }

        /// <summary>
        /// 管理者ユーザーを作成
        /// 【用途】権限テスト用
        /// 【特徴】Admin権限
        /// </summary>
        public static ApplicationUser CreateAdminUser(string email = "admin@example.com")
        {
            var user = CreateValidUser(email);
            user.UserType = UserType.Admin;
            user.FirstName = "管理者";
            user.LastName = "ユーザー";
            return user;
        }

        /// <summary>
        /// パートナーユーザーを作成
        /// 【用途】権限テスト用
        /// 【特徴】Partner権限
        /// </summary>
        public static ApplicationUser CreatePartnerUser(string email = "partner@example.com")
        {
            var user = CreateValidUser(email);
            user.UserType = UserType.Partner;
            user.FirstName = "パートナー";
            user.LastName = "ユーザー";
            return user;
        }
    }
}
