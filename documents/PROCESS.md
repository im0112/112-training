# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：

Claude Code（Sonnet 5）

---

## 通用四問

### 1. 我的任務拆解

（開工前你把任務拆成哪幾步？實際做的時候順序有變嗎？為什麼變？）

- 順序基本沒變：重現/理解現況 → 讓 agent 提方案 → 我核可 → 動手 → 跑測試 → commit。唯一調整是練習4我把 code review 提前到跟 agent 一起看 diff，而不是等它說完成再回頭補查——省了一輪來回。

### 2. AI 幫上大忙的地方

（哪件事 agent 做得又快又好？**貼上當時的提問原文**，說明為什麼這樣問有效。）

- 「請 agent 提案並執行一次小型重構，重構前先說明計畫，我確認後再動手」——這句好用在於逼它先交出可審查的東西（哪個方法、放什麼邏輯），而不是直接動手改完再讓我猜它想幹什麼。方案幾分鐘看完，比事後 review 整個 diff 再打回去快。

### 3. AI 誤導我的地方，與我如何發現

（agent 說錯／改錯／過度自信的時刻。你靠什麼抓到——對照程式碼？頁面實測？跑測試？）

- 沒有典型的「講錯話」，但練習2那三個 bug 剛開始給的描述都偏模糊（沒有具體頁碼/金額/庫存數字），agent 一開始的定位方向也偏泛用性假設。後來補上具體數字才收斂到根因——問題不在 agent 誤導，是我一開始給的輸入不夠精確，agent 只是照單全收。

### 4. 我會帶回日常工作的一招

（一個具體、可複製的做法，不要寫「要多驗證」這種口號——寫出**操作步驟**。）

- 大改動一律先要 agent 給「計畫+改動範圍」再動手，不要讓它邊寫邊想。操作步驟：prompt 裡明講「先提案，我確認後才動手」，方案裡要求列出「動什麼檔案/方法、行為會不會變」，我用幾十秒看完再回覆再動手。跑起來的成本幾乎是 0，抓錨定/範圍跑掉的效果很好。

## 自我驗證（做到哪個階段答哪題）

### 第一階段 — Agentic Coding

練習 1

1. 我能不看筆記說出三個專案（Web/Core/Infrastructure）各自的職責
   → Web=Controllers/ViewModels/Views，Controller盡量薄；Core=Domain/Interfaces/Services，商業邏輯集中在Service；Infrastructure=DbContext/Migrations/Repositories，唯一直接碰DbContext的一層
2. 我核對過 agent 描述的建單流程，且至少找出一處不精確或過度簡化的說法
   → agent一開始說「建立訂單時依序驗證、扣庫存、儲存」，但沒點出扣庫存這一步其實發生在SaveChanges之前——驗證失敗時前面已扣的庫存不會真的落地，因為整批都還沒SaveChanges，這個細節是我自己看CreateOrderAsync原始碼才確認的
3. 我知道商業邏輯應該放在哪一層、新增頁面要動哪些地方
   → 折扣/庫存判斷/狀態轉移一律放Core的Service，不進Controller或View；新增頁面：Web加Controller action+ViewModel+View，有新規則加Core Service方法，需要新查詢就在Infrastructure的Repository補方法

練習 2

1. 三個 bug 我都先在頁面上重現過，才開始找程式
   → 是，三個bug都先復測過再動code
2. 我給 agent 的資訊包含具體觀察（頁碼／金額數字／庫存數字），而不是只貼客訴原文
   → 不夠，三個描述都是線索但沒有頁碼/金額/庫存數字，等於把「找根因」這步丟給agent去猜，這是可以改進的地方
3. 每個修復都回到頁面驗證過症狀消失
   → 是，三個都補測過
4. 每個 bug 都補了一個回歸測試，`dotnet test` 全綠
   → 是，`dotnet test`全綠
5. 三個獨立 commit，message 說明症狀與根因
   → 是
6. （思考題）為什麼原本的測試沒抓到這三個 bug？
   → 舊測試只驗單一函式的表面輸出（例如直接測CalculateTotal的結果），沒有測到「多個方法組合起來、或邊界數值」的情境，bug剛好躲在組合路徑上，單元測試各自綠燈但兜起來是錯的

補一句給未來自己：下次報bug直接附「復現路徑+具體數字」，別只丟症狀描述，能少一輪來回。

練習 3

1. `/Products/LowStock` 不帶參數 → 門檻 10 的結果；帶 `?threshold=3` → 結果隨之改變
   → 瀏覽器實測，正確
2. `?threshold=0`、`?threshold=-1` → 頁面顯示驗證錯誤，不是 500
   → 瀏覽器實測，正確
3. 售出數量欄位排除了 Cancelled 訂單（可用一筆已取消的訂單驗證）
   → 單元測試驗證（含近30天邊界情境）
4. 停售（已停售 badge）商品不出現在列表
   → 瀏覽器實測，正確
5. 程式分層與命名跟既有的 Products 功能一致（請 agent 自我 review 一次，並自己確認）
   → Controller只接參數→驗證→呼叫service→map ViewModel，沒算邏輯，跟Index同款；門檻過濾/銷量聚合都在ProductService，Controller/View不碰；EF查詢都在Repository層；View綁專屬LowStockViewModel；命名沿用既有Get{條件}Async風格
6. 至少 3 個新測試，`dotnet test` 全綠
   → 3個新測試，35/35全綠

一句話：這題比較像是「照著既有骨架長一個新功能」，agent自我review那步比自己肉眼抓分層問題快，但最後還是我自己核對過一次才算數。

練習 4

1. 重構後 `dotnet test` 全綠
   → 35/35全綠
2. 我能說出這次重構「改善了什麼、沒有改變什麼」
   → 改善——CreateOrderAsync原本混了「前置驗證」和「逐項處理」兩種職責，拆成ValidateRequest（純函式，回傳第一個錯誤或null）和BuildOrderItemsAsync（跑迴圈建items+errors），CreateOrderAsync只剩協調流程；沒變——驗證順序、錯誤訊息文字、扣庫存時機（仍在SaveChanges前）、IOrderService介面、外部呼叫方式
3. 我有在 code review 的角度看過 diff（不是 agent 說好就好）
   → customer!.Id用null-forgiving，核對過ValidateRequest的邏輯保證這裡非null，成立；order.Items.Add改foreach不用AddRange，因為ICollection<OrderItem>沒有AddRange，這是編譯期會爆的錯，agent自己在跑測試前就修掉了；沒有夾帶跟本次任務無關的「順手」改動

一句話：這題的重點不是重構技巧本身（Extract Method誰都會），是「先出計畫、我確認、再動手」這個順序本身把review成本壓到最低。

---

## 附錄：值得留下的對話片段

（貼 1–2 段最有代表性的 prompt 與回應**摘要**——不用貼全文，重點是「我怎麼問」和「它怎麼答」。）

**片段一：先出計畫、我確認、再動手**
我：「請agent提案並執行一次小型重構，重構前先說明計畫，我確認後再動手」
agent：先回計畫（拆ValidateRequest/BuildOrderItemsAsync），我看完回「gogogo」才動手。
心得：確認成本幾秒鐘，換來的是不用等改完再打回去重來。

**片段二：編譯期的錯agent自己就抓了**
計畫寫的是`order.Items.AddRange(items)`，但`Order.Items`是`ICollection<OrderItem>`沒這方法。動手時agent自己發現、自己改成foreach，我完全沒插手。
心得：型別/語法這類錯不用我盯，我該把注意力留給「邏輯搬過去語意有沒有變」。
