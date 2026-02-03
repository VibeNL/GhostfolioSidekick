# Comprehensive Smoke Test - Visual Walkthrough

## Test: `ComprehensiveSmokeTest_LoginSyncAndViewTransactions`

This test validates the complete end-to-end user workflow for the Portfolio Viewer application.

---

## 📸 Test Flow with Screenshots

### Step 1: Login
**Screenshot:** `comprehensive-01-login.png`

```
┌─────────────────────────────────┐
│   Portfolio Viewer Login       │
│                                 │
│   ┌───────────────────────┐    │
│   │ Access Token:         │    │
│   │ [test-token-12345]    │    │
│   └───────────────────────┘    │
│                                 │
│   [ Sign In ]                   │
│                                 │
└─────────────────────────────────┘
```

**Actions:**
- Navigate to application URL
- Fill in access token field
- Click "Sign In" button
- Wait for redirect

**Validations:**
- ✅ Successfully redirected from `/login`
- ✅ Now on home page `/`

---

### Step 2: Verify Home Page
**Screenshot:** `comprehensive-02-homepage.png`

```
┌──────────────────────────────────────┐
│ Portfolio Viewer        Dashboard    │
├──────────────────────────────────────┤
│                                      │
│  Start page                          │
│                                      │
│  Before use, please sync the data    │
│                                      │
│  ⚠ No sync yet: Please perform      │
│     your first sync to get started   │
│                                      │
│  Current Action: Idle                │
│  Progress: [         ] 0%            │
│                                      │
│  [ 🔄 Sync Data ]                    │
│                                      │
└──────────────────────────────────────┘
```

**Actions:**
- Wait for home page to fully load
- Verify sync button is visible

**Validations:**
- ✅ Sync button is visible
- ✅ Sync button is enabled

---

### Step 3: Start Sync
**Screenshot:** `comprehensive-03-sync-started.png`

```
┌──────────────────────────────────────┐
│ Portfolio Viewer        Dashboard    │
├──────────────────────────────────────┤
│                                      │
│  Current Action: Starting sync...    │
│  Progress: [███       ] 15%          │
│                                      │
│  [ ⏳ Syncing... ]                   │
│  (Button disabled)                   │
│                                      │
└──────────────────────────────────────┘
```

**Actions:**
- Click sync button
- Wait for sync to start

**Validations:**
- ✅ Sync in progress (button disabled)
- ✅ Progress bar appears

---

### Step 4: Monitor Sync Progress
**Console Output Example:**

```
=== Step 4: Monitor Sync Progress ===
  Progress: 0% - Starting sync...
  Progress: 5% - Fetching accounts...
  Progress: 15% - Loading portfolio data...
  Progress: 35% - Processing transactions...
  Progress: 60% - Calculating holdings...
  Progress: 85% - Updating database...
  Progress: 100% - Sync complete!
✓ Sync completed in 23.4 seconds
```

**Actions:**
- Monitor progress every second
- Log progress changes
- Wait for completion (max 2 minutes)

**Validations:**
- ✅ Progress reaches 100% OR button becomes enabled
- ✅ Completed within timeout

---

### Step 5: Verify Sync Complete
**Screenshot:** `comprehensive-04-sync-complete.png`

```
┌──────────────────────────────────────┐
│ Portfolio Viewer        Dashboard    │
├──────────────────────────────────────┤
│                                      │
│  ℹ Last sync: 15/01/2025 14:32:45   │
│    (just now)                        │
│    ⚡ Next sync will be fast         │
│                                      │
│  Current Action: Idle                │
│  Progress: [██████████] 100%         │
│                                      │
│  [ 🔄 Sync Data ]    [ ⚙ Options ]  │
│                                      │
└──────────────────────────────────────┘
```

**Validations:**
- ✅ "Last sync time" alert is displayed
- ✅ Sync button is re-enabled
- ✅ Progress shows 100%

---

### Step 6: Navigate to Transactions
**Screenshot:** `comprehensive-05-transactions-loaded.png`

```
┌──────────────────────────────────────────┐
│ Portfolio Viewer                         │
├──────────────────────────────────────────┤
│ Dashboard  Portfolio  [Transactions ▼]   │
│                         │                │
│                         ├─ Transaction   │
│                         │  History       │
│                         └─ Upcoming      │
│                            Dividends     │
└──────────────────────────────────────────┘
```

**Actions:**
- Click "Transactions" dropdown menu
- Click "Transaction History" link
- Wait for page navigation

**Validations:**
- ✅ Successfully navigated to `/transactions`

---

### Step 7: Verify Transaction Data
**Screenshot:** `comprehensive-06-transactions-verified.png`

```
┌───────────────────────────────────────────────────────────────────┐
│ Transactions (247 total)                                          │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│ Date       │ Type   │ Symbol  │ Name           │ Account │ Value │
│────────────┼────────┼─────────┼────────────────┼─────────┼───────│
│ 01/15/2025 │ BUY    │ AAPL    │ Apple Inc.     │ Main    │ $500  │
│ 01/14/2025 │ BUY    │ MSFT    │ Microsoft Corp │ Main    │ $750  │
│ 01/13/2025 │ BUY    │ GOOGL   │ Alphabet Inc.  │ Main    │ $350  │
│ 01/12/2025 │ DIV    │ AAPL    │ Apple Inc.     │ Main    │ $2.50 │
│ 01/11/2025 │ BUY    │ TSLA    │ Tesla Inc.     │ Main    │ $950  │
│                                                                   │
│ [ 1 ] 2  3  4  5  ... 10                                         │
└───────────────────────────────────────────────────────────────────┘
```

**Actions:**
- Wait for transactions to load
- Read transaction data from table
- Verify data quality

**Console Output:**
```
=== Step 7: Verify Transaction Data ===
  Has transactions: True
  Is empty state: False
  Has error: False
✓ Found 25 transactions on current page
  Total records info: Transactions (247 total) - Showing 25 on this page
✓ Transaction table is visible
✓ Retrieved 5 transaction details:
  [01/15/2025] BUY - AAPL (Apple Inc.) - $500.00
  [01/14/2025] BUY - MSFT (Microsoft Corp) - $750.00
  [01/13/2025] BUY - GOOGL (Alphabet Inc.) - $350.00
  [01/12/2025] DIV - AAPL (Apple Inc.) - $2.50
  [01/11/2025] BUY - TSLA (Tesla Inc.) - $950.00
✓ Transaction data is valid
```

**Validations:**
- ✅ No error message displayed
- ✅ Either transactions OR empty state shown
- ✅ If transactions exist:
  - Table is visible
  - Rows have valid data (date, type, symbol not empty)
  - Transaction count matches display
- ✅ If empty state:
  - "No Transactions Found" message shown
  - This is acceptable (no data synced)

---

## 🎯 Test Summary

### What This Test Validates

1. **Authentication** - Login functionality works
2. **UI Rendering** - Home page renders correctly
3. **Data Sync** - Sync process completes successfully
4. **Progress Tracking** - Real-time sync progress updates
5. **Navigation** - Menu navigation works correctly
6. **Data Loading** - Transactions page loads data
7. **Data Integrity** - Transaction data is properly formatted

### Test Duration

- **Minimum:** ~30 seconds (fast sync with no data)
- **Typical:** 30-90 seconds (partial sync)
- **Maximum:** 2-3 minutes (first full sync)

### Screenshots Captured

1. `comprehensive-01-login.png` - After successful login
2. `comprehensive-02-homepage.png` - Home page loaded
3. `comprehensive-03-sync-started.png` - Sync in progress
4. `comprehensive-04-sync-complete.png` - Sync completed
5. `comprehensive-05-transactions-loaded.png` - Transactions page loaded
6. `comprehensive-06-transactions-verified.png` - Transaction data verified
   OR `comprehensive-06-transactions-empty.png` - Empty state (if no data)

### On Failure

If the test fails, additional artifacts are captured:
- `comprehensive-smoketest-error-[timestamp].png` - Screenshot at failure point
- `comprehensive-smoketest-error-[timestamp].html` - Full HTML dump
- Console logs with detailed error information

---

## 🚀 Running This Test

### Run only the comprehensive test:

```bash
dotnet test --filter "FullyQualifiedName~ComprehensiveSmokeTest"
```

### Run with detailed output:

```bash
dotnet test --filter "FullyQualifiedName~ComprehensiveSmokeTest" --logger "console;verbosity=detailed"
```

### Expected Output:

```
=== Step 1: Login ===
✓ Login successful
=== Step 2: Verify Home Page ===
✓ Home page loaded
=== Step 3: Start Sync ===
  First sync - will download all data
✓ Sync started
✓ Sync in progress
=== Step 4: Monitor Sync Progress ===
  Progress: 15% - Fetching accounts...
  Progress: 35% - Loading portfolio data...
  Progress: 60% - Processing transactions...
  Progress: 100% - Sync complete!
✓ Sync completed in 42.7 seconds
✓ Last sync time displayed
=== Step 5: Navigate to Transactions ===
✓ Navigated to transactions page
=== Step 6: Load Transactions ===
✓ Transactions page loaded
=== Step 7: Verify Transaction Data ===
✓ Found 247 transactions on current page
✓ Transaction table is visible
✓ Transaction data is valid
=== Comprehensive Smoke Test Completed Successfully ===
✓ Total test duration: 48.3 seconds

Test Passed
```

---

## 💡 Tips

- **First run is slowest** - Initial sync downloads all data
- **Subsequent runs faster** - Partial sync only fetches new data
- **Check screenshots** - Visual confirmation of each step
- **Read console output** - Detailed progress information
- **Increase timeout** - Modify `timeout: 120000` in code if needed for slower connections
