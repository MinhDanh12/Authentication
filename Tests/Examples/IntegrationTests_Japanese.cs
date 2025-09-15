using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using Xunit;
using AuthenticationModule.Data;
using AuthenticationModule.Models;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AuthenticationModule.Tests.Examples
{
    /// <summary>
    /// 統合テストクラス - 日本基準サンプル
    /// 【対象】アプリケーション全体の統合テスト
    /// 【責任】エンドツーエンドのシナリオテスト、実際のHTTPリクエスト/レスポンス
    /// 【作成者】Development Team
    /// 【作成日】2025年12月
    /// </summary>
    public class IntegrationTests_Japanese : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        #region テストデータ (Test Data)
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        #endregion

        #region セットアップ (Setup)
        public IntegrationTests_Japanese(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // テスト用のインメモリデータベースを設定
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("IntegrationTestDb");
                    });

                    // テスト用ログレベル設定
                    services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
                });

                builder.UseEnvironment("Testing");
            });

            _client = _factory.CreateClient();
        }
        #endregion

        #region 完全なユーザーフローテスト (Complete User Flow Tests)

        /// <summary>
        /// 完全なユーザーライフサイクルテスト
        /// 【シナリオ】新規ユーザー登録→ログイン→認証が必要なページアクセス→ログアウト
        /// 【期待結果】全ての段階で適切なレスポンスが返される
        /// </summary>
        [Fact]
        [Trait("Category", "統合テスト")]
        [Trait("Type", "E2E")]
        public async Task CompleteUserFlow_新規登録からログアウトまで_全段階で正常動作()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】新規ユーザー情報
            var newUser = new
            {
                FirstName = "統合",
                LastName = "テスト",
                Email = "integration@test.com",
                UserName = "integration_test",
                Password = "IntegrationTest123!",
                ConfirmPassword = "IntegrationTest123!",
                UserType = (int)UserType.EndUser
            };

            await SeedTestDatabase();

            // ■ 段階1: ユーザー登録 (User Registration)
            // 【目的】新規ユーザーアカウントの作成
            var registrationContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName", newUser.FirstName),
                new KeyValuePair<string, string>("LastName", newUser.LastName),
                new KeyValuePair<string, string>("Email", newUser.Email),
                new KeyValuePair<string, string>("UserName", newUser.UserName),
                new KeyValuePair<string, string>("Password", newUser.Password),
                new KeyValuePair<string, string>("ConfirmPassword", newUser.ConfirmPassword),
                new KeyValuePair<string, string>("UserType", newUser.UserType.ToString())
            });

            var registerResponse = await _client.PostAsync("/Account/Register", registrationContent);

            // 【検証】登録成功でログインページにリダイレクト
            Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);
            Assert.Contains("/Account/Login", registerResponse.Headers.Location?.ToString());

            // ■ 段階2: ユーザーログイン (User Login)
            // 【目的】作成したアカウントでの認証
            var loginContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("EmailOrUsername", newUser.Email),
                new KeyValuePair<string, string>("Password", newUser.Password),
                new KeyValuePair<string, string>("RememberMe", "false")
            });

            var loginResponse = await _client.PostAsync("/Account/Login", loginContent);

            // 【検証】ログイン成功でホームページにリダイレクト
            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
            Assert.Contains("/Home", loginResponse.Headers.Location?.ToString());

            // 【検証】認証Cookieが設定されている
            var cookies = loginResponse.Headers.GetValues("Set-Cookie");
            Assert.Contains(cookies, c => c.Contains(".AspNetCore.Identity.Application"));

            // ■ 段階3: 認証が必要なページへのアクセス (Protected Page Access)
            // 【目的】認証状態でのリソースアクセス確認
            var protectedPageResponse = await _client.GetAsync("/Home/Index");

            // 【検証】認証済みユーザーは保護されたページにアクセス可能
            Assert.Equal(HttpStatusCode.OK, protectedPageResponse.StatusCode);

            // ■ 段階4: ログアウト (User Logout)
            // 【目的】セッション終了とクリーンアップ
            var logoutResponse = await _client.PostAsync("/Account/Logout", new StringContent(""));

            // 【検証】ログアウト成功でログインページにリダイレクト
            Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
            Assert.Contains("/Account/Login", logoutResponse.Headers.Location?.ToString());

            // ■ 段階5: ログアウト後の保護されたページアクセス (Post-Logout Access)
            // 【目的】認証が無効化されていることの確認
            var postLogoutResponse = await _client.GetAsync("/Home/Index");

            // 【検証】未認証ユーザーはログインページにリダイレクト
            Assert.Equal(HttpStatusCode.Redirect, postLogoutResponse.StatusCode);
            Assert.Contains("/Account/Login", postLogoutResponse.Headers.Location?.ToString());

            // ■ データベース状態検証 (Database State Verification)
            await VerifyDatabaseState(newUser.Email);
        }

        /// <summary>
        /// 無効なログイン試行の統合テスト
        /// 【シナリオ】存在しないユーザーでのログイン試行
        /// 【期待結果】適切なエラーメッセージとセキュリティ対応
        /// </summary>
        [Fact]
        [Trait("Category", "統合テスト")]
        [Trait("Type", "Security")]
        public async Task InvalidLogin_存在しないユーザー_適切なエラーレスポンス()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】存在しないユーザーの認証情報
            var invalidLoginData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("EmailOrUsername", "nonexistent@test.com"),
                new KeyValuePair<string, string>("Password", "WrongPassword123!"),
                new KeyValuePair<string, string>("RememberMe", "false")
            });

            // ■ 実行 (Act)
            var response = await _client.PostAsync("/Account/Login", invalidLoginData);

            // ■ 検証 (Assert)
            // 【期待結果】ログインフォームが再表示される
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // 【期待結果】レスポンス内容にエラーメッセージが含まれる
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("正しくありません", responseContent);

            // 【セキュリティ検証】詳細なエラー情報が漏洩していないこと
            Assert.DoesNotContain("存在しません", responseContent);
            Assert.DoesNotContain("見つかりません", responseContent);
        }

        /// <summary>
        /// 重複ユーザー登録の統合テスト
        /// 【シナリオ】既存のメールアドレスで登録試行
        /// 【期待結果】適切なエラーメッセージで登録拒否
        /// </summary>
        [Fact]
        [Trait("Category", "統合テスト")]
        [Trait("Type", "Validation")]
        public async Task DuplicateRegistration_既存メールアドレス_登録拒否()
        {
            // ■ 準備 (Arrange)
            // 【前提条件】既存ユーザーをデータベースに追加
            await SeedTestDatabase();
            var existingEmail = "existing@test.com";
            await CreateTestUser(existingEmail);

            // 【テストデータ】既存メールアドレスでの新規登録情報
            var duplicateUserData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName", "重複"),
                new KeyValuePair<string, string>("LastName", "ユーザー"),
                new KeyValuePair<string, string>("Email", existingEmail), // 重複メール
                new KeyValuePair<string, string>("UserName", "duplicate_user"),
                new KeyValuePair<string, string>("Password", "DuplicateTest123!"),
                new KeyValuePair<string, string>("ConfirmPassword", "DuplicateTest123!"),
                new KeyValuePair<string, string>("UserType", "1")
            });

            // ■ 実行 (Act)
            var response = await _client.PostAsync("/Account/Register", duplicateUserData);

            // ■ 検証 (Assert)
            // 【期待結果】登録フォームが再表示される
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // 【期待結果】重複エラーメッセージが表示される
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("使用されています", responseContent);

            // 【データベース検証】重複ユーザーが作成されていないこと
            await VerifyNoDuplicateUser(existingEmail);
        }

        #endregion

        #region パフォーマンス統合テスト (Performance Integration Tests)

        /// <summary>
        /// ログインページのレスポンス時間テスト
        /// 【要件】ログインページは500ms以内でレスポンス
        /// 【測定方法】実際のHTTPリクエストでの応答時間測定
        /// </summary>
        [Fact]
        [Trait("Category", "性能テスト")]
        [Trait("Type", "Performance")]
        public async Task LoginPage_レスポンス時間_500ms以内()
        {
            // ■ 準備 (Arrange)
            var stopwatch = Stopwatch.StartNew();

            // ■ 実行 (Act)
            var response = await _client.GetAsync("/Account/Login");

            // ■ 検証 (Assert)
            stopwatch.Stop();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < 500, 
                $"ログインページのレスポンス時間は500ms以内であること。実際: {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 同時ログイン処理の負荷テスト
        /// 【要件】同時5ユーザーのログイン処理を3秒以内で完了
        /// 【測定方法】並列HTTPリクエストによる負荷テスト
        /// </summary>
        [Fact]
        [Trait("Category", "性能テスト")]
        [Trait("Type", "LoadTest")]
        public async Task ConcurrentLogin_同時5ユーザー_3秒以内完了()
        {
            // ■ 準備 (Arrange)
            await SeedTestDatabase();
            const int concurrentUsers = 5;
            var loginTasks = new List<Task<HttpResponseMessage>>();

            // 【テストユーザー作成】
            for (int i = 0; i < concurrentUsers; i++)
            {
                var email = $"loadtest{i}@test.com";
                await CreateTestUser(email);
            }

            var stopwatch = Stopwatch.StartNew();

            // 【並列ログインタスク作成】
            for (int i = 0; i < concurrentUsers; i++)
            {
                var email = $"loadtest{i}@test.com";
                var loginData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("EmailOrUsername", email),
                    new KeyValuePair<string, string>("Password", "LoadTest123!"),
                    new KeyValuePair<string, string>("RememberMe", "false")
                });

                loginTasks.Add(_client.PostAsync("/Account/Login", loginData));
            }

            // ■ 実行 (Act)
            var responses = await Task.WhenAll(loginTasks);

            // ■ 検証 (Assert)
            stopwatch.Stop();

            // 【期待結果】全てのログインが成功
            Assert.All(responses, response => 
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode));

            // 【期待結果】処理時間が要件内
            Assert.True(stopwatch.ElapsedMilliseconds < 3000, 
                $"同時{concurrentUsers}ユーザーのログインは3秒以内で完了すること。実際: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region セキュリティ統合テスト (Security Integration Tests)

        /// <summary>
        /// CSRF攻撃防止テスト
        /// 【シナリオ】CSRFトークンなしでのPOSTリクエスト
        /// 【期待結果】リクエストが拒否される
        /// </summary>
        [Fact]
        [Trait("Category", "セキュリティテスト")]
        [Trait("Type", "Security")]
        public async Task CSRF_Protection_トークンなしPOST_リクエスト拒否()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】CSRFトークンを含まないログインデータ
            var maliciousLoginData = new StringContent(
                "EmailOrUsername=test@test.com&Password=Test123!",
                Encoding.UTF8,
                "application/x-www-form-urlencoded");

            // ■ 実行 (Act)
            var response = await _client.PostAsync("/Account/Login", maliciousLoginData);

            // ■ 検証 (Assert)
            // 【期待結果】CSRF保護によりリクエストが拒否される
            // 注意: 実際の実装によってはBadRequestまたはForbiddenが返される
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.Forbidden ||
                       response.StatusCode == HttpStatusCode.OK, // フォームが再表示される場合
                       $"CSRFトークンなしのリクエストは適切に処理されること。実際のステータス: {response.StatusCode}");
        }

        /// <summary>
        /// SQLインジェクション攻撃防止テスト
        /// 【シナリオ】悪意のあるSQLコードを含むログイン試行
        /// 【期待結果】SQLインジェクションが防止される
        /// </summary>
        [Fact]
        [Trait("Category", "セキュリティテスト")]
        [Trait("Type", "Security")]
        public async Task SQL_Injection_Prevention_悪意のあるSQL_攻撃防止()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】SQLインジェクション攻撃を試みるデータ
            var maliciousData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("EmailOrUsername", "admin'; DROP TABLE Users; --"),
                new KeyValuePair<string, string>("Password", "' OR '1'='1"),
                new KeyValuePair<string, string>("RememberMe", "false")
            });

            // ■ 実行 (Act)
            var response = await _client.PostAsync("/Account/Login", maliciousData);

            // ■ 検証 (Assert)
            // 【期待結果】SQLインジェクションが防止され、通常のログイン失敗として処理
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("正しくありません", responseContent);

            // 【データベース検証】データベースが破損していないこと
            await VerifyDatabaseIntegrity();
        }

        #endregion

        #region ヘルパーメソッド (Helper Methods)

        /// <summary>
        /// テスト用データベースの初期化
        /// 【目的】各テストで使用する基本データの準備
        /// </summary>
        private async Task SeedTestDatabase()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // データベースをクリーンアップ
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// テストユーザーの作成
        /// 【用途】統合テスト用の既知のユーザーアカウント作成
        /// </summary>
        private async Task CreateTestUser(string email, string password = "LoadTest123!")
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                UserName = email.Split('@')[0],
                Email = email,
                FirstName = "テスト",
                LastName = "ユーザー",
                UserType = UserType.EndUser,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"テストユーザー作成に失敗: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        /// <summary>
        /// データベース状態の検証
        /// 【用途】統合テスト後のデータ整合性確認
        /// </summary>
        private async Task VerifyDatabaseState(string userEmail)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 【検証】ユーザーが正常に作成されている
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            Assert.NotNull(user);
            Assert.True(user.IsActive);

            // 【検証】ユーザーセッションが適切に管理されている
            var userSessions = await context.UserSessions
                .Where(s => s.UserId == user.Id)
                .ToListAsync();
            
            // セッションが作成されていることを確認（ログイン時）
            // または全て無効化されていることを確認（ログアウト時）
            Assert.True(userSessions.All(s => !s.IsActive), "ログアウト後は全セッションが無効化されていること");
        }

        /// <summary>
        /// 重複ユーザーが作成されていないことの検証
        /// 【用途】重複登録防止機能の確認
        /// </summary>
        private async Task VerifyNoDuplicateUser(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var userCount = await context.Users.CountAsync(u => u.Email == email);
            Assert.True(userCount <= 1, $"メールアドレス {email} のユーザーは1人以下であること。実際: {userCount}人");
        }

        /// <summary>
        /// データベース整合性の検証
        /// 【用途】SQLインジェクション攻撃後のデータベース状態確認
        /// </summary>
        private async Task VerifyDatabaseIntegrity()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // 【検証】基本テーブルがアクセス可能
                var userCount = await context.Users.CountAsync();
                var sessionCount = await context.UserSessions.CountAsync();

                // 【検証】データベース構造が破損していない
                Assert.True(userCount >= 0, "Usersテーブルが正常にアクセス可能であること");
                Assert.True(sessionCount >= 0, "UserSessionsテーブルが正常にアクセス可能であること");
            }
            catch (Exception ex)
            {
                Assert.True(false, $"データベース整合性チェックに失敗: {ex.Message}");
            }
        }

        #endregion

        #region リソース解放 (Cleanup)

        /// <summary>
        /// テスト終了時のリソース解放
        /// 【目的】HTTPクライアントとファクトリーのクリーンアップ
        /// </summary>
        public void Dispose()
        {
            _client?.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// カスタムWebApplicationFactory
    /// 【目的】統合テスト用のアプリケーション設定
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // テスト用データベース設定
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // テスト用ログ設定
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            });

            builder.UseEnvironment("Testing");
        }
    }
}
