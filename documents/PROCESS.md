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

**後續補記（2026-07-31）：重現這一步的執行者換人了**
練習2時三個bug是我自己在瀏覽器上手動點過重現（見第1題），才把觀察轉述給agent去找程式；當時agent沒有能力自己開瀏覽器操作，重現這一步的成本全落在我身上。
今天請agent「建立一筆新訂單，截圖給我看結果頁」，agent直接用Playwright自己開瀏覽器（navigate到`/Orders/Create`）、選客戶、選商品、填數量、按送出、再截圖驗證`/Orders/Details/209`的結果頁，整個「操作+重現+驗證」流程agent自己跑完，我只要看截圖核對金額/折扣算得對不對。
心得：練習2的教訓「重現這一步不能省」還是對的，只是執行者換了——以前這一步只能我做，現在可以交給agent自己做，我的角色從「跑一遍再轉述」變成「核對agent跑出來的結果」。

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

### 第二階段 — 自建 MCP Server（活動2）

練習 3 — before/after 對照（2026-07-31）

問題：「哪些商品庫存低於5？」

- **Before**（`orderhub` MCP 關閉）：我自己組 T-SQL，用 `sqlcmd -S localhost -d OrderHubTraining -E -C -Q "SELECT Sku, Name, StockQuantity FROM Products WHERE StockQuantity < 5 AND IsActive = 1 ORDER BY StockQuantity ASC;"` 直接查資料庫——連線字串（`localhost`/`OrderHubTraining`）要自己記得，還踩到終端機編碼問題，中文商品名全變亂碼，得回頭比對稍早 Playwright 抓到的商品清單才能還原正確名稱。
- **After**（`orderhub` MCP 開啟）：一次呼叫 `low_stock(threshold=5)`，直接拿到乾淨 JSON，中文正常顯示，不用碰連線字串也不用處理編碼。
- 兩次結果完全一致：SKU-1048（晨光 行動電源，庫存2）、SKU-1005（極光 筆電支架，3）、SKU-1023（雲峰 27吋螢幕，3）、SKU-1014（星河 USB-C 集線器，4）、SKU-1032（曜石 機械鍵盤，4）。

心得：MCP 省下來的不是「查得到查不到」，是「怎麼查」這段路——連線字串、SQL 語法、編碼問題全部被包進工具的 description 和 JSON serializer 裡，agent 不用每次重新發明一次查詢邏輯。

練習 4 — 會改資料的工具:cancel_order（2026-07-31）

- 標註：`cancel_order` 標 `Destructive = true, Idempotent = false`；回頭把練習1的三個唯讀工具都補上 `ReadOnly = true`。
- 權限確認：對 agent 說「幫我取消訂單209」，實際呼叫 `cancel_order` 工具前跳出權限確認提示，按允許後才真的執行——沒有繞過確認直接動資料。
- 成功案例：訂單209（SKU-1001 x2）、訂單210（SKU-1001 x1）依序取消，回傳「訂單 209 已取消,庫存已回補」「訂單 210 已取消,庫存已回補」。
- 失敗案例：對已取消的訂單209再取消一次，回傳「取消失敗:狀態為 Cancelled 的訂單不可取消」，是清楚的拒絕訊息,不是 exception dump。
- 驗證：取消完後去 `/Products` 頁面確認 SKU-1001 現有庫存 = 23，跟建立訂單209/210之前的原始庫存數字完全一致，證明兩筆取消的庫存回補都算對了。

心得：這題跟前三題最大的不同是「授權從此是設計的一部分」——唯讀工具答錯了大不了重問，但會改資料的工具下手前一定要有人工確認這一關,而且錯誤訊息要讓agent能懂「為什麼不行」而不是丟一坨 exception,這樣它才不會瞎猜重試。

練習 5 — Resources 與 Prompts（2026-07-31）

思考(5c第3點)：
- 折扣規則用 Resource 給 vs 讓 agent 自己讀 `OrderService.cs`：Resource 是團隊共用、進版控的一份真相，規則改版只要改 server 這一處，所有連上這個 MCP 的人/agent 都同步拿到新版；讓 agent 自己讀程式碼則每次都要重新解析、還要求對方有原始碼存取權（不是每個想問折扣的人都該有 repo 權限），而且程式碼重構後 agent 找不找得到規則所在位置也沒保證。
- prompt 範本放 server vs 每個人自己打一段話：放 server 讓「先查 low_stock、再查訂單狀況、最後輸出固定格式的表」這套固定流程只寫一次、進版控，每個人打同一個 slash command 拿到完全一致的執行步驟；換成每個人各自打一段話，措辭不同容易漏步驟(例如忘記帶 threshold、忘記要輸出補貨理由)，而且沒有共用機制——有人發現更好的問法，其他人不會自動受益，除非手動口耳相傳。

### 第三階段 — n8n 自動化（活動4）

練習 2 — 退單巡檢日報

思考題：如果「查什麼、怎麼查」也交給 AI Agent 自由發揮，會失去什麼？

- 補測 false 分支時，我把 HTTP Request 節點的 body 從 `{"text":"過去 30 天取消的訂單"}` 換成 `{"text":"昨天取消的訂單"}`，可預期地讓查詢回傳 0 筆走到 Data Table 那條路；驗完再換回原字串，又可預期地拿回一樣範圍的資料。這時候才意識到：查詢條件現在是我能操控的固定輸入，所以我能靠「換一行字串」重現 true/false 兩種分支。如果查什麼、怎麼查都讓 AI 臨場決定，我沒辦法保證同一個測試腳本重跑兩次會給我一樣的分支結果。
- 為了回答這題我回頭翻了 `OrderSearchService.cs`，看到「白名單防線」那段註解，才確認活動 3 那邊的 LLM 其實從來沒機會碰 SQL——它只能吐出 `OrderSearchQuery` 那組固定欄位（Status/MemberTier/DateFrom/DateTo），真正的查詢是 EF Core 照這組參數生的。n8n 這邊打的正是這道牆後面的固定端點。如果把這層拿掉、讓 AI Agent 自己想查詢邏輯，等於是繞過白名單直接讓模型決定要撈哪些資料。
- 取消訂單 #204 之後，我自己開 `/Orders?status=Cancelled` 肉眼數了一次：近 30 天 8 筆、加總 17,499 元，跟 AI Agent 吐出來的「近 30 天累計取消 8 筆訂單，總金額達 17,499 元」對上了。這個核對動作能成立，前提是 AI 只負責摘要「查詢範圍已經固定」的資料。如果連查什麼都是 AI 決定，日報數字出錯時我會分不清是牠查錯範圍、還是摘要算錯——兩層不確定性混在一起沒辦法拆開查。

練習 3 — MCP 合體：讓流程裡的 AI 會用你的工具

思考題：對照練習 2，同一批退單，有深挖 vs 沒深挖的日報差異？

- 我把 AI Agent 的 Tool 掛上 MCP Client（endpoint `http://localhost:3001`，只勾 `get_order`）之後按 Execute Workflow，跑完點開 Logs 一看，樹狀圖裡排了 8 個 `MCP Client` 節點——數了一下正好對上這批 8 筆退單，才確定 agent 真的一筆一筆去查了明細，不是拿彙總 JSON 自己腦補品項。
- 我拿這次的日報（issue #5）跟練習 2 那份（issue #4）並排看：同樣是訂單 4、徐若瑄取消的那筆，練習 2 只寫得出「金額 6,920 元（本期最高金額）」，這次卻列出「曜石 HDMI 傳輸線 x2、曜石 桌上麥克風 x2、晨光 無線滑鼠 x2」三個品項各自的小計，連 208/207/204 這幾筆 Silver 會員的 5% 折扣都寫出來了。
- 這才意識到差別不是「多寫幾行」：練習 2 那份我只能信任總金額 17,499 元是對的，沒辦法自己核對；這次每筆的單價、數量、小計、折扣都攤開來，我可以自己拿計算機加回去對應付總額，算錯了也能一眼看出是哪個品項或折扣出的問題。

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
