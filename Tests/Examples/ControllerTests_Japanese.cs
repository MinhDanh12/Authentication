using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using AuthenticationModule.Controllers;
using AuthenticationModule.Models;
using AuthenticationModule.Services;

namespace AuthenticationModule.Tests.Examples
{
    /// <summary>
    /// AccountController のテストクラス - 日本基準サンプル
    /// 【対象】アカウント管理コントローラーの全機能
    /// 【責任】HTTP リクエスト処理、ビューレンダリング、リダイレクト処理
    /// 【作成者】Development Team
    /// 【作成日】2025年12月
    /// </summary>
    public class AccountControllerTests_Japanese : IDisposable
    {
        #region テストデータ (Test Data)
        private readonly Mock<IAuthenticationService> _authServiceMock;
        private readonly Mock<ILogger<AccountController>> _loggerMock;
        private readonly AccountController _controller;
        #endregion

        #region セットアップ (Setup)
        public AccountControllerTests_Japanese()
        {
            // モック初期化
            _authServiceMock = new Mock<IAuthenticationService>();
            _loggerMock = new Mock<ILogger<AccountController>>();
            
            // コントローラー初期化
            _controller = new AccountController(_authServiceMock.Object, _loggerMock.Object);
        }
        #endregion

        #region ログイン機能テスト (Login Tests)

        /// <summary>
        /// ログインページ表示 - 正常系テスト
        /// 【シナリオ】ログインページへのGETリクエスト
        /// 【期待結果】ログインビューとモデルが返される
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Component", "Login")]
        public void Login_Get_ログインビューを返す()
        {
            // ■ 準備 (Arrange)
            // 【目的】ログインページの表示テスト
            // 【前提条件】認証されていないユーザー
            
            // ■ 実行 (Act)
            var result = _controller.Login();

            // ■ 検証 (Assert)
            // 【期待結果】ViewResultが返される
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
            
            // 【期待結果】LoginViewModelが設定されている
            var model = Assert.IsType<LoginViewModel>(viewResult.Model);
            Assert.NotNull(model);
            Assert.False(model.RememberMe); // デフォルト値確認
        }

        /// <summary>
        /// ログインページ表示 - ReturnURL付き
        /// 【シナリオ】リダイレクト先URLを指定してログインページにアクセス
        /// 【期待結果】ReturnURLがViewDataに設定される
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Component", "Login")]
        public void Login_Get_ReturnURL付き_ViewDataに設定()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】リダイレクト先URL
            var returnUrl = "/Home/Dashboard";

            // ■ 実行 (Act)
            var result = _controller.Login(returnUrl);

            // ■ 検証 (Assert)
            // 【期待結果】ViewResultが返される
            var viewResult = Assert.IsType<ViewResult>(result);
            
            // 【期待結果】ReturnURLがViewDataに設定されている
            Assert.Equal(returnUrl, viewResult.ViewData["ReturnUrl"]);
            
            // 【期待結果】モデルが正しく設定されている
            var model = Assert.IsType<LoginViewModel>(viewResult.Model);
            Assert.NotNull(model);
        }

        /// <summary>
        /// ログイン処理 - 正常系テスト
        /// 【シナリオ】有効な認証情報でログイン
        /// 【期待結果】ホーム画面にリダイレクト
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Component", "Login")]
        public async Task Login_Post_有効な認証情報_ホーム画面にリダイレクト()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】有効なログイン情報
            var loginModel = new LoginViewModel
            {
                EmailOrUsername = "test@example.com",
                Password = "ValidPassword123!",
                RememberMe = false
            };

            // 【期待される認証結果】
            var expectedAuthResult = new AuthenticationResult
            {
                IsSuccess = true,
                User = new ApplicationUser
                {
                    Id = "user123",
                    UserName = "testuser",
                    Email = "test@example.com",
                    FirstName = "テスト",
                    LastName = "ユーザー",
                    UserType = UserType.EndUser,
                    IsActive = true
                },
                Token = "valid-jwt-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            // 【モック設定】認証サービスが成功を返す
            _authServiceMock
                .Setup(x => x.LoginAsync(loginModel.EmailOrUsername, loginModel.Password, loginModel.RememberMe))
                .ReturnsAsync(expectedAuthResult);

            // 【モック設定】HttpContextとAuthentication
            SetupControllerContext();

            // ■ 実行 (Act)
            var result = await _controller.Login(loginModel);

            // ■ 検証 (Assert)
            // 【期待結果】リダイレクト結果
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);

            // 【副作用検証】認証サービスが正しく呼び出されている
            _authServiceMock.Verify(
                x => x.LoginAsync(loginModel.EmailOrUsername, loginModel.Password, loginModel.RememberMe),
                Times.Once,
                "認証サービスが正確なパラメータで呼び出されること");
        }

        /// <summary>
        /// ログイン処理 - 異常系テスト
        /// 【シナリオ】無効なモデル状態でログイン試行
        /// 【期待結果】ビューが返され、エラーが表示される
        /// </summary>
        [Fact]
        [Trait("Category", "異常系")]
        [Trait("Component", "Login")]
        public async Task Login_Post_無効なモデル_ビューとエラーを返す()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】無効なログイン情報
            var invalidModel = new LoginViewModel
            {
                EmailOrUsername = "", // 必須項目が空
                Password = "",        // 必須項目が空
                RememberMe = false
            };

            // 【モデル状態】バリデーションエラーを追加
            _controller.ModelState.AddModelError("EmailOrUsername", "Email/Username は必須です");
            _controller.ModelState.AddModelError("Password", "パスワードは必須です");

            // ■ 実行 (Act)
            var result = await _controller.Login(invalidModel);

            // ■ 検証 (Assert)
            // 【期待結果】ViewResultが返される
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(invalidModel, viewResult.Model);

            // 【期待結果】ModelStateにエラーがある
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains("必須", _controller.ModelState["EmailOrUsername"]?.Errors[0].ErrorMessage);
            Assert.Contains("必須", _controller.ModelState["Password"]?.Errors[0].ErrorMessage);

            // 【副作用検証】認証サービスが呼ばれていないこと
            _authServiceMock.Verify(
                x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never,
                "無効なモデルの場合は認証サービスを呼び出さないこと");
        }

        /// <summary>
        /// ログイン処理 - 異常系テスト
        /// 【シナリオ】認証失敗（無効な認証情報）
        /// 【期待結果】ビューが返され、認証エラーメッセージが表示される
        /// </summary>
        [Fact]
        [Trait("Category", "異常系")]
        [Trait("Component", "Login")]
        public async Task Login_Post_認証失敗_ビューとエラーメッセージを返す()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】認証に失敗するログイン情報
            var loginModel = new LoginViewModel
            {
                EmailOrUsername = "test@example.com",
                Password = "wrongpassword",
                RememberMe = false
            };

            // 【期待される認証結果】認証失敗
            var failedAuthResult = new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "Email/Username または パスワードが正しくありません"
            };

            // 【モック設定】認証サービスが失敗を返す
            _authServiceMock
                .Setup(x => x.LoginAsync(loginModel.EmailOrUsername, loginModel.Password, loginModel.RememberMe))
                .ReturnsAsync(failedAuthResult);

            // ■ 実行 (Act)
            var result = await _controller.Login(loginModel);

            // ■ 検証 (Assert)
            // 【期待結果】ViewResultが返される
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(loginModel, viewResult.Model);

            // 【期待結果】ModelStateにエラーが追加されている
            Assert.False(_controller.ModelState.IsValid);
            var errorMessages = _controller.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            Assert.Contains(errorMessages, msg => msg.Contains("正しくありません"));

            // 【副作用検証】認証サービスが呼び出されている
            _authServiceMock.Verify(
                x => x.LoginAsync(loginModel.EmailOrUsername, loginModel.Password, loginModel.RememberMe),
                Times.Once,
                "認証試行のためサービスが呼び出されること");
        }

        #endregion

        #region ユーザー登録機能テスト (Register Tests)

        /// <summary>
        /// 登録ページ表示 - 正常系テスト
        /// 【シナリオ】ユーザー登録ページへのGETリクエスト
        /// 【期待結果】登録ビューとモデルが返される
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Component", "Register")]
        public void Register_Get_登録ビューを返す()
        {
            // ■ 準備 (Arrange)
            // 【目的】登録ページの表示テスト
            
            // ■ 実行 (Act)
            var result = _controller.Register();

            // ■ 検証 (Assert)
            // 【期待結果】ViewResultが返される
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);

            // 【期待結果】RegisterViewModelが設定されている
            var model = Assert.IsType<RegisterViewModel>(viewResult.Model);
            Assert.NotNull(model);
            Assert.Equal(UserType.EndUser, model.UserType); // デフォルト値確認
        }

        /// <summary>
        /// ユーザー登録処理 - 正常系テスト
        /// 【シナリオ】有効な登録情報でユーザー登録
        /// 【期待結果】ログインページにリダイレクト、成功メッセージ表示
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Component", "Register")]
        public async Task Register_Post_有効な登録情報_ログインページにリダイレクト()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】有効な登録情報
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

            // 【期待される登録結果】
            var successAuthResult = new AuthenticationResult
            {
                IsSuccess = true,
                User = new ApplicationUser
                {
                    Id = "newuser123",
                    UserName = registerModel.UserName,
                    Email = registerModel.Email,
                    FirstName = registerModel.FirstName,
                    LastName = registerModel.LastName,
                    UserType = registerModel.UserType,
                    IsActive = true
                },
                ErrorMessage = "ユーザー登録が正常に完了しました"
            };

            // 【モック設定】登録サービスが成功を返す
            _authServiceMock
                .Setup(x => x.RegisterAsync(registerModel))
                .ReturnsAsync(successAuthResult);

            // ■ 実行 (Act)
            var result = await _controller.Register(registerModel);

            // ■ 検証 (Assert)
            // 【期待結果】ログインページへのリダイレクト
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Null(redirectResult.ControllerName); // 同一コントローラー内

            // 【期待結果】成功メッセージがTempDataに設定されている
            Assert.True(_controller.TempData.ContainsKey("SuccessMessage"));
            Assert.Contains("完了", _controller.TempData["SuccessMessage"]?.ToString());

            // 【副作用検証】登録サービスが正しく呼び出されている
            _authServiceMock.Verify(
                x => x.RegisterAsync(It.Is<RegisterViewModel>(r => 
                    r.Email == registerModel.Email && 
                    r.UserName == registerModel.UserName)),
                Times.Once,
                "登録サービスが正確なパラメータで呼び出されること");
        }

        /// <summary>
        /// ユーザー登録処理 - 異常系テスト
        /// 【シナリオ】無効なモデル状態で登録試行
        /// 【期待結果】ビューが返され、バリデーションエラーが表示される
        /// </summary>
        [Fact]
        [Trait("Category", "異常系")]
        [Trait("Component", "Register")]
        public async Task Register_Post_無効なモデル_ビューとエラーを返す()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】無効な登録情報
            var invalidModel = new RegisterViewModel
            {
                FirstName = "",     // 必須項目が空
                LastName = "",      // 必須項目が空
                Email = "invalid-email", // 無効なメール形式
                UserName = "",      // 必須項目が空
                Password = "123",   // 短すぎるパスワード
                ConfirmPassword = "456", // パスワード不一致
                UserType = UserType.EndUser
            };

            // 【モデル状態】バリデーションエラーを追加
            _controller.ModelState.AddModelError("FirstName", "名前は必須です");
            _controller.ModelState.AddModelError("LastName", "姓は必須です");
            _controller.ModelState.AddModelError("Email", "有効なメールアドレスを入力してください");
            _controller.ModelState.AddModelError("UserName", "ユーザー名は必須です");
            _controller.ModelState.AddModelError("Password", "パスワードは6文字以上である必要があります");
            _controller.ModelState.AddModelError("ConfirmPassword", "パスワードが一致しません");

            // ■ 実行 (Act)
            var result = await _controller.Register(invalidModel);

            // ■ 検証 (Assert)
            // 【期待結果】ViewResultが返される
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(invalidModel, viewResult.Model);

            // 【期待結果】ModelStateに複数のエラーがある
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ErrorCount >= 6);

            // 【期待結果】各フィールドにエラーメッセージがある
            Assert.True(_controller.ModelState["FirstName"]?.Errors.Count > 0);
            Assert.True(_controller.ModelState["Email"]?.Errors.Count > 0);
            Assert.True(_controller.ModelState["Password"]?.Errors.Count > 0);

            // 【副作用検証】登録サービスが呼ばれていないこと
            _authServiceMock.Verify(
                x => x.RegisterAsync(It.IsAny<RegisterViewModel>()),
                Times.Never,
                "無効なモデルの場合は登録サービスを呼び出さないこと");
        }

        /// <summary>
        /// ユーザー登録処理 - 異常系テスト
        /// 【シナリオ】登録失敗（重複メールアドレスなど）
        /// 【期待結果】ビューが返され、登録エラーメッセージが表示される
        /// </summary>
        [Fact]
        [Trait("Category", "異常系")]
        [Trait("Component", "Register")]
        public async Task Register_Post_登録失敗_ビューとエラーメッセージを返す()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】登録に失敗する情報（重複メール）
            var registerModel = new RegisterViewModel
            {
                FirstName = "田中",
                LastName = "太郎",
                Email = "duplicate@example.com", // 既存のメールアドレス
                UserName = "tanaka_taro",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!",
                UserType = UserType.EndUser
            };

            // 【期待される登録結果】登録失敗
            var failedAuthResult = new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "このメールアドレスは既に使用されています"
            };

            // 【モック設定】登録サービスが失敗を返す
            _authServiceMock
                .Setup(x => x.RegisterAsync(registerModel))
                .ReturnsAsync(failedAuthResult);

            // ■ 実行 (Act)
            var result = await _controller.Register(registerModel);

            // ■ 検証 (Assert)
            // 【期待結果】ViewResultが返される
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(registerModel, viewResult.Model);

            // 【期待結果】ModelStateにエラーが追加されている
            Assert.False(_controller.ModelState.IsValid);
            var errorMessages = _controller.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            Assert.Contains(errorMessages, msg => msg.Contains("使用されています"));

            // 【副作用検証】登録サービスが呼び出されている
            _authServiceMock.Verify(
                x => x.RegisterAsync(registerModel),
                Times.Once,
                "登録試行のためサービスが呼び出されること");
        }

        #endregion

        #region ログアウト機能テスト (Logout Tests)

        /// <summary>
        /// ログアウト処理 - 正常系テスト
        /// 【シナリオ】認証済みユーザーのログアウト
        /// 【期待結果】ログインページにリダイレクト、セッション無効化
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Component", "Logout")]
        public async Task Logout_Post_認証済みユーザー_ログインページにリダイレクト()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】認証済みユーザー
            var userId = "user123";
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Email, "test@example.com")
            }, "TestAuthentication"));

            // 【モック設定】HttpContextに認証済みユーザーを設定
            var httpContext = new DefaultHttpContext
            {
                User = user
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // 【モック設定】ログアウトサービスが成功を返す
            _authServiceMock
                .Setup(x => x.LogoutAsync(userId))
                .ReturnsAsync(true);

            // ■ 実行 (Act)
            var result = await _controller.Logout();

            // ■ 検証 (Assert)
            // 【期待結果】ログインページへのリダイレクト
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Null(redirectResult.ControllerName); // 同一コントローラー内

            // 【副作用検証】ログアウトサービスが正しいユーザーIDで呼び出されている
            _authServiceMock.Verify(
                x => x.LogoutAsync(userId),
                Times.Once,
                "ログアウトサービスが正確なユーザーIDで呼び出されること");
        }

        /// <summary>
        /// ログアウト処理 - 異常系テスト
        /// 【シナリオ】未認証ユーザーのログアウト試行
        /// 【期待結果】ログインページにリダイレクト（サービス呼び出しなし）
        /// </summary>
        [Fact]
        [Trait("Category", "異常系")]
        [Trait("Component", "Logout")]
        public async Task Logout_Post_未認証ユーザー_ログインページにリダイレクト()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】未認証ユーザー（ClaimsPrincipalなし）
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()) // 未認証
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // ■ 実行 (Act)
            var result = await _controller.Logout();

            // ■ 検証 (Assert)
            // 【期待結果】ログインページへのリダイレクト
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);

            // 【副作用検証】ログアウトサービスが呼ばれていないこと
            _authServiceMock.Verify(
                x => x.LogoutAsync(It.IsAny<string>()),
                Times.Never,
                "未認証ユーザーの場合はログアウトサービスを呼び出さないこと");
        }

        #endregion

        #region アクセス拒否機能テスト (Access Denied Tests)

        /// <summary>
        /// アクセス拒否ページ表示 - 正常系テスト
        /// 【シナリオ】権限不足でアクセス拒否された場合
        /// 【期待結果】アクセス拒否ビューが表示される
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Component", "AccessDenied")]
        public void AccessDenied_権限不足_アクセス拒否ビューを返す()
        {
            // ■ 準備 (Arrange)
            // 【目的】アクセス拒否ページの表示テスト
            
            // ■ 実行 (Act)
            var result = _controller.AccessDenied();

            // ■ 検証 (Assert)
            // 【期待結果】ViewResultが返される
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult);

            // 【期待結果】ビュー名が正しく設定されている（明示的でない場合はnull）
            Assert.Null(viewResult.ViewName); // デフォルトビュー名を使用
        }

        #endregion

        #region パスワード忘れ機能テスト (Forgot Password Tests)

        /// <summary>
        /// パスワード忘れページ表示 - 正常系テスト
        /// 【シナリオ】パスワード忘れページへのGETリクエスト
        /// 【期待結果】パスワード忘れビューが表示される
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Component", "ForgotPassword")]
        public void ForgotPassword_Get_パスワード忘れビューを返す()
        {
            // ■ 準備 (Arrange)
            // 【目的】パスワード忘れページの表示テスト
            
            // ■ 実行 (Act)
            var result = _controller.ForgotPassword();

            // ■ 検証 (Assert)
            // 【期待結果】ViewResultが返される
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult);
        }

        /// <summary>
        /// パスワード忘れ処理 - 正常系テスト
        /// 【シナリオ】有効なメールアドレスでパスワードリセット要求
        /// 【期待結果】ログインページにリダイレクト、成功メッセージ表示
        /// </summary>
        [Fact]
        [Trait("Category", "正常系")]
        [Trait("Component", "ForgotPassword")]
        public async Task ForgotPassword_Post_有効なメール_ログインページにリダイレクト()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】有効なメールアドレス
            var email = "test@example.com";
            var existingUser = new ApplicationUser
            {
                Id = "user123",
                Email = email,
                UserName = "testuser",
                IsActive = true
            };

            // 【モック設定】ユーザーが存在する
            _authServiceMock
                .Setup(x => x.GetUserByEmailOrUsernameAsync(email))
                .ReturnsAsync(existingUser);

            // ■ 実行 (Act)
            var result = await _controller.ForgotPassword(email);

            // ■ 検証 (Assert)
            // 【期待結果】ログインページへのリダイレクト
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);

            // 【期待結果】成功メッセージがTempDataに設定されている
            Assert.True(_controller.TempData.ContainsKey("InfoMessage"));
            Assert.Contains("送信", _controller.TempData["InfoMessage"]?.ToString());

            // 【副作用検証】ユーザー検索サービスが呼び出されている
            _authServiceMock.Verify(
                x => x.GetUserByEmailOrUsernameAsync(email),
                Times.Once,
                "ユーザー検索サービスが呼び出されること");
        }

        /// <summary>
        /// パスワード忘れ処理 - 異常系テスト
        /// 【シナリオ】空のメールアドレスでリセット要求
        /// 【期待結果】ビューが返され、バリデーションエラーが表示される
        /// </summary>
        [Fact]
        [Trait("Category", "異常系")]
        [Trait("Component", "ForgotPassword")]
        public async Task ForgotPassword_Post_空のメール_ビューとエラーを返す()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】空のメールアドレス
            var emptyEmail = "";

            // ■ 実行 (Act)
            var result = await _controller.ForgotPassword(emptyEmail);

            // ■ 検証 (Assert)
            // 【期待結果】ViewResultが返される
            var viewResult = Assert.IsType<ViewResult>(result);

            // 【期待結果】ModelStateにエラーがある
            Assert.False(_controller.ModelState.IsValid);
            var errorMessages = _controller.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            Assert.Contains(errorMessages, msg => msg.Contains("必須"));

            // 【副作用検証】ユーザー検索サービスが呼ばれていないこと
            _authServiceMock.Verify(
                x => x.GetUserByEmailOrUsernameAsync(It.IsAny<string>()),
                Times.Never,
                "空のメールアドレスの場合はユーザー検索を行わないこと");
        }

        /// <summary>
        /// パスワード忘れ処理 - 異常系テスト
        /// 【シナリオ】存在しないメールアドレスでリセット要求
        /// 【期待結果】セキュリティのため成功と同じレスポンス（情報漏洩防止）
        /// </summary>
        [Fact]
        [Trait("Category", "異常系")]
        [Trait("Component", "ForgotPassword")]
        public async Task ForgotPassword_Post_存在しないメール_セキュリティのため成功レスポンス()
        {
            // ■ 準備 (Arrange)
            // 【テストデータ】存在しないメールアドレス
            var nonExistentEmail = "nonexistent@example.com";

            // 【モック設定】ユーザーが存在しない
            _authServiceMock
                .Setup(x => x.GetUserByEmailOrUsernameAsync(nonExistentEmail))
                .ReturnsAsync((ApplicationUser?)null);

            // ■ 実行 (Act)
            var result = await _controller.ForgotPassword(nonExistentEmail);

            // ■ 検証 (Assert)
            // 【期待結果】ログインページへのリダイレクト（セキュリティのため成功と同じ）
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);

            // 【期待結果】成功メッセージがTempDataに設定されている（セキュリティのため）
            Assert.True(_controller.TempData.ContainsKey("InfoMessage"));
            Assert.Contains("送信", _controller.TempData["InfoMessage"]?.ToString());

            // 【副作用検証】ユーザー検索サービスが呼び出されている
            _authServiceMock.Verify(
                x => x.GetUserByEmailOrUsernameAsync(nonExistentEmail),
                Times.Once,
                "存在確認のためユーザー検索サービスが呼び出されること");
        }

        #endregion

        #region ヘルパーメソッド (Helper Methods)

        /// <summary>
        /// コントローラーのHttpContextをモック設定
        /// 【用途】認証処理やHttpContextが必要なテスト用
        /// </summary>
        private void SetupControllerContext()
        {
            var httpContext = new DefaultHttpContext();
            
            // HttpContextにAuthenticationServiceをモック設定
            var authServiceMock = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
            
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(Microsoft.AspNetCore.Authentication.IAuthenticationService)))
                .Returns(authServiceMock.Object);
            
            httpContext.RequestServices = serviceProvider.Object;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // TempDataの初期化
            _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext, 
                new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>().Object);
        }

        #endregion

        #region リソース解放 (Cleanup)

        /// <summary>
        /// テスト終了時のリソース解放
        /// 【目的】メモリリーク防止
        /// </summary>
        public void Dispose()
        {
            // コントローラーの解放
            _controller?.Dispose();
            
            // GCによる自動解放を許可
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
