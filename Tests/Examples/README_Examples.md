# Unit Test Examples - Japanese Standards (日本基準サンプル)

このフォルダには、日本のソフトウェア開発基準に従った Unit Test の実例が含まれています。

## 📁 ファイル構成

### 🧪 テストファイル
- **`AuthenticationServiceTests_Japanese.cs`** - サービス層テストの完全なサンプル
- **`ControllerTests_Japanese.cs`** - コントローラー層テストの完全なサンプル  
- **`IntegrationTests_Japanese.cs`** - 統合テストの完全なサンプル

### 📚 ドキュメント
- **`README_Examples.md`** - このファイル（サンプルの説明）

---

## 🎯 各サンプルの特徴

### 1. **AuthenticationServiceTests_Japanese.cs**

#### **📋 含まれるテストパターン**
- **正常系テスト** (Happy Path Tests)
  - 有効なユーザー認証
  - 新規ユーザー登録
  - セッション管理

- **異常系テスト** (Error Path Tests)
  - 無効なパスワード
  - 存在しないユーザー
  - 非アクティブユーザー
  - 重複メールアドレス

- **境界値テスト** (Boundary Tests)
  - 最小パスワード長
  - RememberMe機能の有効期限

- **パフォーマンステスト** (Performance Tests)
  - レスポンス時間測定
  - 負荷テスト

#### **🔧 技術的特徴**
```csharp
// 日本語コメントによる詳細な説明
/// <summary>
/// ログイン機能 - 正常系テスト
/// 【シナリオ】有効なユーザー認証情報でログイン
/// 【期待結果】認証成功、JWTトークン生成、セッション作成
/// </summary>

// AAA パターンの明確な分離
// ■ 準備 (Arrange)
// ■ 実行 (Act)  
// ■ 検証 (Assert)

// 詳細な検証とコメント
Assert.True(result.IsSuccess, "認証が成功すること");
_jwtServiceMock.Verify(x => x.GenerateJwtToken(validUser), Times.Once, 
    "JWTトークンが生成されること");
```

### 2. **ControllerTests_Japanese.cs**

#### **📋 含まれるテストパターン**
- **ログイン機能テスト**
  - GETリクエスト（ビュー表示）
  - POSTリクエスト（認証処理）
  - ReturnURL処理
  - エラーハンドリング

- **ユーザー登録機能テスト**
  - 新規登録処理
  - バリデーションエラー
  - 重複チェック

- **ログアウト機能テスト**
  - セッション無効化
  - リダイレクト処理

- **セキュリティ機能テスト**
  - アクセス拒否
  - パスワード忘れ機能

#### **🔧 技術的特徴**
```csharp
// HTTPコンテキストのモック設定
private void SetupControllerContext()
{
    var httpContext = new DefaultHttpContext();
    _controller.ControllerContext = new ControllerContext
    {
        HttpContext = httpContext
    };
}

// MVC特有の検証
var redirectResult = Assert.IsType<RedirectToActionResult>(result);
Assert.Equal("Index", redirectResult.ActionName);
Assert.Equal("Home", redirectResult.ControllerName);
```

### 3. **IntegrationTests_Japanese.cs**

#### **📋 含まれるテストパターン**
- **完全なユーザーフローテスト**
  - 登録→ログイン→認証ページアクセス→ログアウト

- **セキュリティテスト**
  - CSRF攻撃防止
  - SQLインジェクション防止

- **パフォーマンステスト**
  - ページレスポンス時間
  - 同時アクセス負荷テスト

- **データベース整合性テスト**
  - データ作成確認
  - 重複防止確認

#### **🔧 技術的特徴**
```csharp
// WebApplicationFactoryを使用した実際のHTTPテスト
public IntegrationTests_Japanese(WebApplicationFactory<Program> factory)
{
    _factory = factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            // テスト用インメモリDB設定
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestDb");
            });
        });
    });
}

// 実際のHTTPリクエスト/レスポンステスト
var response = await _client.PostAsync("/Account/Login", loginData);
Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
```

---

## 🚀 サンプルの使用方法

### **1. 基本的な使用方法**

```bash
# サンプルファイルを実際のテストプロジェクトにコピー
cp Tests/Examples/*.cs Tests/

# 必要に応じてnamespaceを修正
# AuthenticationModule.Tests.Examples → AuthenticationModule.Tests

# テスト実行
dotnet test --filter "Category=正常系"
dotnet test --filter "Category=異常系"  
dotnet test --filter "Category=性能テスト"
```

### **2. カスタマイズ方法**

#### **テストデータの変更**
```csharp
// TestDataFactoryクラスでテストデータをカスタマイズ
public static ApplicationUser CreateValidUser(string email = "your@email.com")
{
    return new ApplicationUser
    {
        // あなたのプロジェクトに合わせてプロパティを調整
        Email = email,
        UserName = email.Split('@')[0],
        // ...
    };
}
```

#### **モック設定の調整**
```csharp
// あなたのサービスインターフェースに合わせてモックを調整
private readonly Mock<IYourService> _yourServiceMock;

// セットアップでモック動作を定義
_yourServiceMock
    .Setup(x => x.YourMethod(It.IsAny<string>()))
    .ReturnsAsync(expectedResult);
```

### **3. 新しいテストの追加**

#### **新しいテストメソッドの作成**
```csharp
/// <summary>
/// 新機能のテスト
/// 【シナリオ】あなたの新機能のテストシナリオ
/// 【期待結果】期待される結果の説明
/// </summary>
[Fact]
[Trait("Category", "正常系")]
[Trait("Component", "YourNewFeature")]
public async Task YourNewMethod_条件_期待結果()
{
    // ■ 準備 (Arrange)
    // テストデータとモックの設定
    
    // ■ 実行 (Act)
    // テスト対象の実行
    
    // ■ 検証 (Assert)
    // 結果の検証
}
```

---

## 📊 品質指標

### **カバレッジ目標**
- **サービス層**: 95%以上
- **コントローラー層**: 85%以上
- **統合テスト**: 主要フロー100%

### **パフォーマンス目標**
- **単体テスト**: 100ms以内
- **統合テスト**: 500ms以内
- **負荷テスト**: 同時10ユーザー、3秒以内

### **品質チェックポイント**
- ✅ AAA構造の遵守
- ✅ 適切な日本語コメント
- ✅ モックの正しい使用
- ✅ 境界値テストの実装
- ✅ エラーハンドリングのテスト

---

## 🔧 実行環境要件

### **必要なパッケージ**
```xml
<PackageReference Include="xunit" Version="2.6.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="Moq" Version="4.20.69" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
```

### **実行コマンド**
```bash
# 全テスト実行
dotnet test

# カテゴリ別実行
dotnet test --filter "Category=正常系"
dotnet test --filter "Category=異常系"
dotnet test --filter "Category=境界値"
dotnet test --filter "Category=性能テスト"

# コンポーネント別実行
dotnet test --filter "Component=Login"
dotnet test --filter "Component=Register"

# カバレッジ付き実行
dotnet test --collect:"XPlat Code Coverage"
```

---

## 📝 学習ポイント

### **1. 日本基準の特徴**
- **詳細なコメント**: なぜそのテストが必要かを明記
- **5W1H原則**: テストの目的と方法を明確化
- **品質重視**: 単なるPassではなく、品質保証の観点

### **2. テスト設計原則**
- **独立性**: 各テストは他に依存しない
- **再現性**: 何度実行しても同じ結果
- **明確性**: テストの意図が明確

### **3. 保守性の考慮**
- **ヘルパーメソッド**: 共通処理の再利用
- **テストデータファクトリー**: 一貫したテストデータ
- **適切な抽象化**: 変更に強い設計

---

## 🎓 研修での活用方法

### **段階的学習**
1. **Week 1**: `AuthenticationServiceTests_Japanese.cs`で基本的な単体テスト
2. **Week 2**: `ControllerTests_Japanese.cs`でMVCテスト
3. **Week 3**: `IntegrationTests_Japanese.cs`で統合テスト
4. **Week 4**: 実際のプロジェクトでのテスト作成

### **ハンズオン演習**
1. サンプルコードの動作確認
2. 既存テストの修正・拡張
3. 新機能のテスト作成
4. コードレビューの実践

### **チェックリスト**
- [ ] AAA構造を理解している
- [ ] Mockの使い方を理解している
- [ ] 正常系・異常系・境界値テストを書ける
- [ ] 統合テストの重要性を理解している
- [ ] パフォーマンステストを実装できる

---

**📚 参考資料**: メインの`JAPANESE_UNITTEST_GUIDELINE.md`と併せてご活用ください。

**👥 作成者**: Development Team  
**📅 作成日**: 2025年12月  
**🔄 バージョン**: 1.0

---

*これらのサンプルは実際のプロジェクトで即座に使用できるよう設計されています。チームの学習と品質向上にお役立てください。*
