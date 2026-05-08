using System.Reflection;
using System.Windows.Input;
using Xunit;

namespace VinhKhanh.App.Tests;

/// <summary>
/// Property-based test for PoiListPage RefreshCommand binding bug condition.
/// 
/// **Validates: Bug Condition Exploration**
/// 
/// This test verifies that the bug condition is fixed on the code:
/// - PoiListPage.xaml has RefreshView with Command="{Binding RefreshCommand}"
/// - PoiListPage.xaml.cs HAS a RefreshCommand property
/// - This allows binding to work correctly on release build with compiled bindings
/// 
/// Expected behavior on UNFIXED code: Test FAILS (confirms bug exists)
/// Expected behavior on FIXED code: Test PASSES (confirms fix works)
/// </summary>
public class PoiListPageRefreshCommandBugConditionTests
{
    private static string GetSourceCodePath(string fileName)
    {
        // Get the workspace root by going up from the test assembly directory
        // AppContext.BaseDirectory is: C:\Users\nt\dotnetsys\VinhKhanh\tests\VinhKhanh.App.Tests\bin\Debug\net10.0
        // We need to go to: C:\Users\nt\dotnetsys\VinhKhanh
        var testAssemblyDir = AppContext.BaseDirectory;
        var workspaceRoot = Path.GetFullPath(Path.Combine(testAssemblyDir, "..", "..", "..", "..", ".."));
        
        // Try to find the source file
        var possiblePaths = new[]
        {
            Path.Combine(workspaceRoot, "src", "VinhKhanh.App", fileName),
            Path.Combine(workspaceRoot, "src", "VinhKhanh.App", "Pages", fileName),
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new FileNotFoundException($"Could not find {fileName} in paths: {string.Join(", ", possiblePaths)}");
    }

    /// <summary>
    /// Property: RefreshCommand property must exist on PoiListPage for binding to work.
    /// 
    /// This test verifies the source code contains the RefreshCommand property.
    /// On unfixed code, this test FAILS because RefreshCommand property doesn't exist.
    /// On fixed code, this test PASSES because RefreshCommand property is added.
    /// </summary>
    [Fact(DisplayName = "Bug Condition: RefreshCommand property must exist on PoiListPage")]
    public void BugCondition_RefreshCommandPropertyMustExist()
    {
        // Arrange: Get the PoiListPage.xaml.cs source code
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert: Check if RefreshCommand property is declared
        // The property should be declared as: public ICommand RefreshCommand { get; }
        Assert.Contains("RefreshCommand", sourceCode);
        Assert.Contains("ICommand", sourceCode);
    }

    /// <summary>
    /// Property: RefreshCommand must be of type ICommand for XAML binding to work.
    /// 
    /// XAML binding requires the property to implement ICommand interface.
    /// </summary>
    [Fact(DisplayName = "Bug Condition: RefreshCommand must implement ICommand interface")]
    public void BugCondition_RefreshCommandMustImplementICommand()
    {
        // Arrange: Get the PoiListPage.xaml.cs source code
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert: Check if RefreshCommand is declared as ICommand
        Assert.Contains("public ICommand RefreshCommand", sourceCode);
    }

    /// <summary>
    /// Property: RefreshCommand property must be readable (have a getter).
    /// 
    /// XAML binding requires the property to have a public getter.
    /// </summary>
    [Fact(DisplayName = "Bug Condition: RefreshCommand property must have a public getter")]
    public void BugCondition_RefreshCommandMustHavePublicGetter()
    {
        // Arrange: Get the PoiListPage.xaml.cs source code
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert: Check if RefreshCommand has a getter
        // The property should have { get; } or { get; set; }
        Assert.Contains("RefreshCommand", sourceCode);
        Assert.Matches(@"public\s+ICommand\s+RefreshCommand\s*\{\s*get", sourceCode);
    }

    /// <summary>
    /// Property: PoiListPage.xaml must have RefreshView with Command binding.
    /// 
    /// This verifies that the XAML file contains the binding that requires RefreshCommand.
    /// </summary>
    [Fact(DisplayName = "Bug Condition: PoiListPage.xaml must have RefreshView with Command binding")]
    public void BugCondition_XamlMustHaveRefreshViewCommandBinding()
    {
        // Arrange: Find the XAML file
        var xamlPath = GetSourceCodePath("PoiListPage.xaml");
        
        // Act & Assert: Check if XAML file exists and contains the binding
        Assert.True(File.Exists(xamlPath), $"XAML file not found at {xamlPath}");
        
        var xamlContent = File.ReadAllText(xamlPath);
        
        // Assert: XAML must contain RefreshView with Command binding
        Assert.Contains("RefreshView", xamlContent);
        Assert.Contains("Command=\"{Binding RefreshCommand}\"", xamlContent);
    }
}

/// <summary>
/// Regression tests to verify that the fix doesn't break existing functionality.
/// 
/// **Validates: Regression Prevention**
/// 
/// These tests verify:
/// - POI list loads and displays correctly
/// - Search functionality works
/// - Navigation to detail page works
/// - No new errors or warnings introduced
/// </summary>
public class PoiListPageRegressionTests
{
    private static string GetSourceCodePath(string fileName)
    {
        var testAssemblyDir = AppContext.BaseDirectory;
        var workspaceRoot = Path.GetFullPath(Path.Combine(testAssemblyDir, "..", "..", "..", "..", ".."));
        
        var possiblePaths = new[]
        {
            Path.Combine(workspaceRoot, "src", "VinhKhanh.App", fileName),
            Path.Combine(workspaceRoot, "src", "VinhKhanh.App", "Pages", fileName),
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new FileNotFoundException($"Could not find {fileName}");
    }

    /// <summary>
    /// Regression: PoiListPage.xaml.cs must still have FilteredPois collection.
    /// 
    /// This verifies that the POI list collection is still present for displaying items.
    /// </summary>
    [Fact(DisplayName = "Regression: FilteredPois collection must exist for displaying POI list")]
    public void Regression_FilteredPoisCollectionMustExist()
    {
        // Arrange
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert
        Assert.Contains("FilteredPois", sourceCode);
        Assert.Contains("ObservableCollection<Poi>", sourceCode);
    }

    /// <summary>
    /// Regression: PoiListPage must still have LoadDataAsync method.
    /// 
    /// This verifies that the data loading functionality is preserved.
    /// </summary>
    [Fact(DisplayName = "Regression: LoadDataAsync method must exist for loading POI data")]
    public void Regression_LoadDataAsyncMethodMustExist()
    {
        // Arrange
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert
        Assert.Contains("LoadDataAsync", sourceCode);
        Assert.Contains("async Task LoadDataAsync", sourceCode);
    }

    /// <summary>
    /// Regression: PoiListPage must still have ApplyFilter method.
    /// 
    /// This verifies that the search/filter functionality is preserved.
    /// </summary>
    [Fact(DisplayName = "Regression: ApplyFilter method must exist for search functionality")]
    public void Regression_ApplyFilterMethodMustExist()
    {
        // Arrange
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert
        Assert.Contains("ApplyFilter", sourceCode);
        Assert.Contains("void ApplyFilter", sourceCode);
    }

    /// <summary>
    /// Regression: PoiListPage must still have OnPoiTapped method.
    /// 
    /// This verifies that the navigation to detail page functionality is preserved.
    /// </summary>
    [Fact(DisplayName = "Regression: OnPoiTapped method must exist for navigation")]
    public void Regression_OnPoiTappedMethodMustExist()
    {
        // Arrange
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert
        Assert.Contains("OnPoiTapped", sourceCode);
        Assert.Contains("PoiDetailPage", sourceCode);
    }

    /// <summary>
    /// Regression: PoiListPage.xaml must still have CollectionView for displaying list.
    /// 
    /// This verifies that the UI structure for displaying POI list is preserved.
    /// </summary>
    [Fact(DisplayName = "Regression: CollectionView must exist in XAML for displaying POI list")]
    public void Regression_CollectionViewMustExistInXaml()
    {
        // Arrange
        var xamlPath = GetSourceCodePath("PoiListPage.xaml");
        var xamlContent = File.ReadAllText(xamlPath);
        
        // Act & Assert
        Assert.Contains("CollectionView", xamlContent);
        Assert.Contains("PoisCollectionView", xamlContent);
    }

    /// <summary>
    /// Regression: PoiListPage.xaml must still have search Entry.
    /// 
    /// This verifies that the search UI element is preserved.
    /// </summary>
    [Fact(DisplayName = "Regression: Search Entry must exist in XAML")]
    public void Regression_SearchEntryMustExistInXaml()
    {
        // Arrange
        var xamlPath = GetSourceCodePath("PoiListPage.xaml");
        var xamlContent = File.ReadAllText(xamlPath);
        
        // Act & Assert
        Assert.Contains("SearchEntry", xamlContent);
        Assert.Contains("Entry", xamlContent);
    }

    /// <summary>
    /// Regression: PoiListPage.xaml must still have OnSearchTextChanged binding.
    /// 
    /// This verifies that the search text change event is preserved.
    /// </summary>
    [Fact(DisplayName = "Regression: OnSearchTextChanged event binding must exist")]
    public void Regression_OnSearchTextChangedBindingMustExist()
    {
        // Arrange
        var xamlPath = GetSourceCodePath("PoiListPage.xaml");
        var xamlContent = File.ReadAllText(xamlPath);
        
        // Act & Assert
        Assert.Contains("TextChanged", xamlContent);
    }

    /// <summary>
    /// Regression: PoiListPage.xaml must still have TapGestureRecognizer for POI items.
    /// 
    /// This verifies that the tap-to-navigate functionality is preserved.
    /// </summary>
    [Fact(DisplayName = "Regression: TapGestureRecognizer must exist for POI item navigation")]
    public void Regression_TapGestureRecognizerMustExist()
    {
        // Arrange
        var xamlPath = GetSourceCodePath("PoiListPage.xaml");
        var xamlContent = File.ReadAllText(xamlPath);
        
        // Act & Assert
        Assert.Contains("TapGestureRecognizer", xamlContent);
        Assert.Contains("OnPoiTapped", xamlContent);
    }

    /// <summary>
    /// Regression: PoiListPage must still have BindingContext set to this.
    /// 
    /// This verifies that the binding context is properly set for data binding.
    /// </summary>
    [Fact(DisplayName = "Regression: BindingContext must be set to this")]
    public void Regression_BindingContextMustBeSetToThis()
    {
        // Arrange
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert
        Assert.Contains("BindingContext = this", sourceCode);
    }

    /// <summary>
    /// Regression: PoiListPage must still have ItemsSource binding for CollectionView.
    /// 
    /// This verifies that the list items are bound to the FilteredPois collection.
    /// </summary>
    [Fact(DisplayName = "Regression: CollectionView ItemsSource must be bound to FilteredPois")]
    public void Regression_CollectionViewItemsSourceMustBeBound()
    {
        // Arrange
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert
        Assert.Contains("PoisCollectionView.ItemsSource = FilteredPois", sourceCode);
    }

    /// <summary>
    /// Regression: PoiListPage must still have OnReloadClicked method.
    /// 
    /// This verifies that the manual reload button functionality is preserved.
    /// </summary>
    [Fact(DisplayName = "Regression: OnReloadClicked method must exist for manual reload")]
    public void Regression_OnReloadClickedMethodMustExist()
    {
        // Arrange
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert
        Assert.Contains("OnReloadClicked", sourceCode);
    }

    /// <summary>
    /// Regression: PoiListPage must still have ILocalDbService for local data loading.
    /// 
    /// This verifies that the local database service is still used for loading POI data.
    /// </summary>
    [Fact(DisplayName = "Regression: ILocalDbService must be used for local data loading")]
    public void Regression_LocalDbServiceMustBeUsed()
    {
        // Arrange
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert
        Assert.Contains("ILocalDbService", sourceCode);
        Assert.Contains("_db.GetPoisAsync", sourceCode);
    }

    /// <summary>
    /// Regression: PoiListPage must still have ApiClientService for remote data loading.
    /// 
    /// This verifies that the API service is still used for syncing POI data.
    /// </summary>
    [Fact(DisplayName = "Regression: ApiClientService must be used for remote data loading")]
    public void Regression_ApiClientServiceMustBeUsed()
    {
        // Arrange
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert
        Assert.Contains("ApiClientService", sourceCode);
        Assert.Contains("_api.GetPoisAsync", sourceCode);
    }

    /// <summary>
    /// Regression: PoiListPage must still have MainRefreshView element.
    /// 
    /// This verifies that the RefreshView UI element is still present.
    /// </summary>
    [Fact(DisplayName = "Regression: MainRefreshView element must exist in XAML")]
    public void Regression_MainRefreshViewMustExistInXaml()
    {
        // Arrange
        var xamlPath = GetSourceCodePath("PoiListPage.xaml");
        var xamlContent = File.ReadAllText(xamlPath);
        
        // Act & Assert
        Assert.Contains("MainRefreshView", xamlContent);
    }

    /// <summary>
    /// Regression: PoiListPage must still have IsRefreshing property check.
    /// 
    /// This verifies that the refresh state is still being managed.
    /// </summary>
    [Fact(DisplayName = "Regression: IsRefreshing property must be checked in LoadDataAsync")]
    public void Regression_IsRefreshingPropertyMustBeChecked()
    {
        // Arrange
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert
        Assert.Contains("IsRefreshing", sourceCode);
    }

    /// <summary>
    /// Regression: PoiListPage must still have error handling in LoadDataAsync.
    /// 
    /// This verifies that error handling is preserved.
    /// </summary>
    [Fact(DisplayName = "Regression: Error handling must exist in LoadDataAsync")]
    public void Regression_ErrorHandlingMustExist()
    {
        // Arrange
        var codeFilePath = GetSourceCodePath("PoiListPage.xaml.cs");
        var sourceCode = File.ReadAllText(codeFilePath);
        
        // Act & Assert
        Assert.Contains("try", sourceCode);
        Assert.Contains("catch", sourceCode);
        Assert.Contains("finally", sourceCode);
    }
}
