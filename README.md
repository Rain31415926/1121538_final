# 📝 極簡數位管家 (Minimalist Digital Butler)

> **課程名稱：** 視窗程式設計 (II) 期末專題  
> **開發者：** [你的學號] [你的姓名]  
> **開發語言與框架：** C# / Windows Forms (.NET Framework 4.7.2)

## 📌 專案簡介 (Project Overview)

市面上的代辦事項軟體往往過於複雜，光是設定分類、日期與優先級就會打斷使用者的思緒。**「極簡數位管家」** 是一款專為追求效率與專注力所開發的桌面應用程式。

它的核心理念是：**「打字即分類」**。結合了自訂的語法解析器 (Parser)、無感存檔機制、階層式子任務連動，以及獨創的全螢幕「沉浸式專注模式」，幫助使用者以最直覺的方式管理生活瑣事，並透過儀表板看見自己的努力軌跡。

---

## ✨ 核心功能 (Key Features)

### 1. ⌨️ 魔法輸入法 (Smart Text Parser)
透過自訂的正規表達式 (Regex) 解析引擎，在文字框輸入特定符號即可瞬間賦予任務屬性，無需滑鼠點擊繁瑣的下拉選單：
* `#標籤`：自動分類任務（例：`#工作`、`#理財`）。
* `!優先級`：設定輕重緩急（支援 `!high`, `!medium`, `!low`）。
* `&顏色`：替任務背景上色，支援 Hex 色碼（例：`&Gold`, `&#FF5733`）。
* `~重複`：支援週期性任務（`~daily`, `~weekly`, `~monthly`），完成後自動生成下一期。

### 2. 🗂️ 專案目標拆解 (Hierarchical Subtasks)
* 支援無限層級的子任務拆解。
* **智慧雙向連動：** 主畫面上可一鍵展開/收合子任務。當子項目全部勾選時，父任務自動標記完成；勾選父任務也能一鍵完成所有子項目。

### 3. 🧘 沉浸式專注模式 (Zen/Focus Mode)
* 專為進入心流狀態設計。選定任務後進入此模式，視窗將切換為**無邊框全螢幕深色 UI**，隱藏系統工具列與其他干擾。
* 內建 25 分鐘番茄鐘倒數計時器，時間結束自動提示並完成任務。

### 4. 📊 數據視覺化儀表板 (Dashboard)
利用 `System.Windows.Forms.DataVisualization.Charting` 與自訂繪圖 `Graphics` 實作：
* **每日完成熱力圖 (Heatmap)：** 仿造 GitHub Contribution Graph，過去 12 週的產能一目了然。
* **任務積壓警告圖 (Burndown Chart)：** 追蹤過去 14 天的任務新增與消化速度，自動偵測並警告任務過載。

### 5. 💾 狀態持久化與極致防呆 (Persistence & Robustness)
* **0 秒等待：** 系統利用 `Application Settings` 自動記憶上次開啟的檔案，下次啟動時瞬間還原畫面。
* **無感儲存：** 提供「建立新檔」功能，任何狀態更動（如勾選完成）皆會在背景自動安全寫入 `.txt` 檔，不怕資料遺失。
* 內建「新手教學指南」視窗，無痛上手。

---

## 📸 畫面截圖 (Screenshots)

*(💡 提示：請將專案截圖上傳至 GitHub 倉庫內的 `images` 資料夾，並將下方路徑替換成實際檔名)*

| 主畫面與子任務展開 | 魔法輸入與任務編輯 |
| :---: | :---: |
| <img src="./images/main.png" width="400" alt="主畫面"> | <img src="./images/edit.png" width="400" alt="任務編輯"> |

| 沉浸式專注模式 (全螢幕) | 數據視覺化儀表板 |
| :---: | :---: |
| <img src="./images/focus.png" width="400" alt="專注模式"> | <img src="./images/dashboard.png" width="400" alt="儀表板"> |

---

## 🚀 執行說明 (How to Run)

本專案無需安裝額外的第三方資料庫，所有資料皆以輕量化的 `.txt` 格式儲存。

1. **環境需求：**
   * Windows 10 / 11 作業系統
   * 安裝 Visual Studio 2022
   * .NET Framework 4.7.2 或以上版本
2. **開啟專案：**
   * 將此儲存庫 Clone 至本地端，或下載 ZIP 檔後解壓縮。
   * 點擊開啟資料夾內的 `1121538_徐霈綺_final.sln` 方案檔。
3. **建置與執行：**
   * 在 Visual Studio 中，按下 `F5` 或是點擊上方選單的「開始」按鈕建置並執行專案。
4. **開始使用：**
   * 首次開啟時，清單為空。您可以點擊左下角的 **「New File (建立新檔)」** 創建一個新的文字檔，或是點擊 **「Open File」** 載入現有的任務文字檔。
   * 點擊「❓ 教學指南」即可查看完整操作說明。

---

## 📂 專案架構 (Project Structure)

本專案嚴格遵守 Git 版控規範，已套用 `.gitignore` 排除 `bin/`, `obj/`, `.vs/` 等編譯與暫存檔案。

```text
├── Form1.cs           // 主畫面視窗 (清單渲染、排序、過濾、拖曳排序)
├── DashboardForm.cs   // 儀表板視窗 (熱力圖與折線圖繪製)
├── FocusForm.cs       // 專注模式視窗 (無邊框全螢幕、Timer 邏輯)
├── TaskDialogForm.cs  // 新增/編輯任務視窗
├── HelpForm.cs        // 新手教學分頁視窗
├── TaskModels.cs      // 包含 TaskItem, SubTask 等資料結構
└── TaskParser.cs      // 負責將純文字解析為物件的 Regex 引擎
