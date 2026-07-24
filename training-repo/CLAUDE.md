# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案簡介

OrderHub 是一套公司內部訂單管理系統（培訓用專案）：業務可建立/查詢訂單、管理商品與客戶。
單一 SQL Server 資料庫、內部使用，不需要考慮多租戶、高併發或微服務架構。

更完整的操作說明（啟動步驟、資料庫連線設定、頁面路由、疑難排解）請看 `../documents/README.md`（位於本 repo 的上層目錄，因為此 repo 是培訓套件裡的一個子專案）。

## 技術棧

- .NET 8 / ASP.NET Core MVC（Razor Views + Bootstrap 5，前端資源皆為 `wwwroot/lib` 下的本地檔案，不依賴 CDN）
- EF Core 8 + SQL Server（本機安裝，非 Docker）
- xUnit + EF Core InMemory（測試不需要 SQL Server，也不會動到真實資料庫）

## 常用指令

```powershell
dotnet build                          # 建置整個 solution
dotnet run --project src/OrderHub.Web # 啟動網站（預設 http://localhost:5150）
dotnet test                           # 跑全部測試
dotnet test --filter "FullyQualifiedName~OrderServicePricingTests"  # 只跑單一測試類別
```

第一次啟動（或每次啟動）都會在 `Program.cs` 自動執行 `db.Database.Migrate()` 再跑 `DbSeeder.SeedAsync()`，不需要手動建庫；種子資料使用固定 random seed，每次重建內容一致。

## 分層與慣例

三層式架構，相依方向為 `OrderHub.Web` → `OrderHub.Core` → `OrderHub.Infrastructure`（Web 專案同時參照 Core 與 Infrastructure 以完成 DI 註冊）：

- **`OrderHub.Web`**：`Controllers` / `ViewModels` / `Views`。Controller 保持薄，只轉接 service 呼叫結果並做 ViewModel 對應；`Helpers/DisplayHelper.cs` 集中管理狀態／會員等級的顯示文字、badge class 與金額格式化，Controller 和 View 都用它，不要各自重寫一套。
- **`OrderHub.Core`**：`Domain`（entity）、`Interfaces`（repository 介面）、`Services`（商業邏輯）。所有商業邏輯（折扣、庫存增減、狀態轉移、驗證）都放在這一層的 service，不進 Controller。
- **`OrderHub.Infrastructure`**：`Data`（`OrderHubDbContext`、`DbSeeder`）、`Migrations`、`Repositories`（實作 Core 定義的介面）。只有這一層直接碰 `DbContext`；Controller / Service 一律透過 repository 介面操作資料。

其他慣例：

- Service 方法回傳 `ServiceResult<T>`（見 `OrderHub.Core/Common/ServiceResult.cs`）表達預期內的失敗（如驗證不通過、找不到資源），不要用例外處理業務規則；Controller 依 `result.Success` 決定導向或把 `result.Errors` 塞進 `ModelState`。
- View 一律綁專屬 ViewModel（`OrderHub.Web/ViewModels`），不要把 Domain model 直接傳給 View；Controller 手寫 mapping。
- 使用者輸入用 DataAnnotations（`[Required]`、`[Range]`…）+ `ModelState.IsValid` 驗證，驗證失敗要回表單顯示錯誤，不能讓使用者輸入導致 500。
- 金額一律用 `decimal`。折扣邏輯集中在 `OrderService`（`GetDiscountRate` / `CalculateSubtotal` / `CalculateTotal`），新增與金額相關的功能時沿用這幾個方法，不要在別處重算。
- 操作結果訊息走 `TempData["Success"]` / `TempData["Error"]`（`Views/Shared/_Layout.cshtml` 有共用的 alert 顯示區塊），不要另外設計提示機制。
- POST action 都要加 `[ValidateAntiForgeryToken]`。
- 新增 Controller / Service 時可參照既有的 `ProductsController` + `ProductService`（最簡單的一組）或 `OrdersController` + `OrderService`（含驗證、ViewModel mapping、TempData 較完整的一組）。

## 測試慣例

- 測試專案 `tests/OrderHub.Tests` 用 `TestSetup.cs` 建立 InMemory `OrderHubDbContext` 與已注入真實 repository 的 service 實例（`CreateOrderService` / `CreateProductService`），以及 `AddCustomer` / `AddProduct` 測試資料 helper。新增 service 測試時沿用這套 helper，不要重新手刻 DbContext 設定。
- 測試檔以 `{Service}{行為}Tests.cs` 命名（如 `OrderServiceCreateTests`、`OrderServicePricingTests`），一個行為維度一個檔案。

## 重要 / 危險檔案

- `src/OrderHub.Infrastructure/Migrations/**`：EF migration 是歷史紀錄，不要手動修改；schema 變更請新增 migration。
- `src/OrderHub.Web/appsettings*.json`：含資料庫連線字串，修改前先確認。

## 不要做的事

- 不要未經同意就加新的 NuGet 套件。
- 不要在 Controller 或 Service 直接使用 `DbContext`，一律透過 repository 介面。
- 不要把商業邏輯（折扣計算、庫存判斷、狀態轉移規則）寫進 Controller 或 View。
- 不要為了「順手」重構與當前任務無關的程式碼。
