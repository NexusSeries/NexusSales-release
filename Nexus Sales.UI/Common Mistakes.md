```markdown
## Common Mistakes

### 1. Passing an Empty or Null Connection String to LoginPage

**Mistake:**  
Instantiating `LoginPage` using a constructor that does not provide a valid connection string, such as:
```
new LoginPage(this, isForward)
```
or
```
new LoginPage(this, string.Empty)
```
This results in runtime errors like:
```
System.ArgumentException: 'Host can't be null'
```
when attempting to connect to the database.

**Correct Approach:**  
Always instantiate `LoginPage` with a valid connection string:
```
new LoginPage(this, decryptedConnectionString)
```
or
```
MainFrame.Navigate(new LoginPage(this, decryptedConnectionString));
```

**Tip:**  
Remove or make private any constructors for `LoginPage` that do not require a connection string to prevent this mistake.

---

### 2. Using Multiple Constructors for Pages That Require a Connection String

**Mistake:**  
Having multiple public constructors for pages like `LoginPage`, `ForgotPasswordPage`, or `ResetPasswordPage` that do not require a connection string can lead to accidental usage of the wrong constructor, resulting in runtime errors.

**Correct Approach:**  
Only expose constructors that require all necessary parameters (such as the connection string). Make parameterless or incomplete constructors private or remove them entirely.

---

### 3. Navigating to LoginPage Without the Connection String

**Mistake:**  
Using navigation methods that instantiate `LoginPage` without passing the connection string, for example:
```
public void NavigateToLogin(bool isForward = true)
{
    MainFrame.Navigate(new LoginPage(this, isForward));
}
```
This will cause the "Host can't be null" error.

**Correct Approach:**  
Always pass the connection string:
```
public void NavigateToLogin(bool isForward = true)
{
    MainFrame.Navigate(new LoginPage(this, decryptedConnectionString));
}




## 2025-08-03: Using Entity Framework 6 with Npgsql in .NET Framework 4.7.2 for PostgreSQL

### **The Mistake:**
Attempting to use Entity Framework 6 (EF6) with the Npgsql provider for PostgreSQL on .NET Framework 4.7.2, resulting in runtime errors such as:

System.Data.DataException: 'An exception occurred while initializing the database. See the InnerException for details.' MissingMethodException: Method not found: 'Void Npgsql.NpgsqlCommand.set_ExpectedTypes(System.Type[])'


This occurs when calling EF6 async methods (e.g., `FindAsync`, `ToListAsync`) on a `DbSet` backed by Npgsql, due to a version mismatch or lack of full support for EF6 async APIs in the Npgsql provider.

### **The Correct Approach:**
Avoid using Entity Framework 6 for PostgreSQL operations in .NET Framework 4.7.2 projects.  
Instead, use **ADO.NET with Npgsql** directly for all database operations:
- Replace all EF6-based queries and updates with `NpgsqlConnection` and `NpgsqlCommand`.
- Manually map query results to your C# models.

**Example:**

using (var conn = new NpgsqlConnection(_connectionString)) { await conn.OpenAsync(); using (var cmd = new NpgsqlCommand("DELETE FROM notifications WHERE id = @id", conn)) { cmd.Parameters.AddWithValue("id", notificationId); await cmd.ExecuteNonQueryAsync(); } }



### **Why it's Important:**
- Prevents runtime errors and application crashes due to incompatible library versions.
- Ensures reliable, maintainable, and future-proof data access in .NET Framework projects using PostgreSQL.
- Avoids wasted time troubleshooting EF6/Npgsql integration issues that cannot be resolved without migrating to .NET Core or later.

---

What was changed:
•	Added a new entry documenting the EF6/Npgsql incompatibility issue, the error message, and the correct approach (use ADO.NET/Npgsql directly).
•	Provided a code example and clear reasoning to prevent recurrence.


## 2025-08-06: Using Base ViewModel to Access Derived Class Members (Repository, UserEmail, Collections)

### **The Mistake:**
Implementing logic in a base ViewModel class (such as `ViewModelBase`) that directly accesses members (`_repository`, `_userEmail`, `UserBookmarks`, `Bookmarks`) which are only defined in derived classes (e.g., `NotificationsViewModel`).  
This leads to compile-time errors like:


CS0103: The name '_repository' does not exist in the current context CS0103: The name '_userEmail' does not exist in the current context CS0103: The name 'UserBookmarks' does not exist in the current context CS0103: The name 'Bookmarks' does not exist in the current context



and runtime issues if not caught.

### **The Correct Approach:**
- The base ViewModel should only define shared logic and abstract or virtual methods for functionality that depends on derived class members.
- Implement all logic that requires access to derived class fields (such as repository, user email, or collections) in the derived class itself.
- In the base class, declare such methods as `protected abstract` or `protected virtual` and throw `NotImplementedException` if not overridden.

**Example:**



// In ViewModelBase protected virtual async Task UnpinNotificationAsync(NotificationItem item) { throw new NotImplementedException("UnpinNotificationAsync must be implemented in the derived ViewModel."); }
// In NotificationsViewModel private async Task UnpinNotificationAsync(NotificationItem item) { // Access _repository, _userEmail, UserBookmarks, Bookmarks here }




### **Why it's Important:**
- Prevents compile-time and runtime errors due to missing context in the base class.
- Enforces clean MVVM architecture and separation of concerns.
- Makes the codebase more maintainable and easier to extend, as shared logic is in the base class and context-specific logic is in derived classes.




## 2025-08-08: Deleting Bookmarked Notifications Removes Bookmarks (Bookmark Data Loss on Notification Deletion)

### **The Mistake:**
When implementing the deletion (collapse) of private notifications, the code in the data access layer (`RemoveNotificationAsync` in `SqlDbRepository`) was written to not only delete the notification from the `notifications` table, but also to remove all related entries from the `notification_bookmarks` and `user_bookmarked_notifications` tables:
// BAD: This deletes bookmarks when deleting a notification! using (var cmd = new NpgsqlCommand("DELETE FROM notifications WHERE id = @id", conn)) { ... } using (var cmd = new NpgsqlCommand("DELETE FROM notification_bookmarks WHERE notification_id = @id", conn)) { ... } using (var cmd = new NpgsqlCommand("DELETE FROM user_bookmarked_notifications WHERE notification_id = @id", conn)) { ... }


As a result, when a user collapsed (deleted) a private notification from the notification panel, any bookmarks associated with that notification were also deleted. This caused the notification to disappear from the bookmarks panel as well, even though the user only intended to remove it from the main notification list, not from their saved bookmarks.

### **The Correct Approach:**
**Only delete the notification itself when collapsing/removing it from the notification panel.**  
Do **not** delete any associated bookmarks or user bookmark records. Bookmarks should only be removed when the user explicitly chooses to "unpin" or remove the bookmark.

**Correct code:**

// GOOD: Only delete the notification itself! using (var cmd = new NpgsqlCommand("DELETE FROM notifications WHERE id = @id", conn)) { cmd.Parameters.AddWithValue("id", notificationId); await cmd.ExecuteNonQueryAsync(); } // Do NOT delete from notification_bookmarks or user_bookmarked_notifications here!


### **Why it's Important:**
- **User Experience:** Users expect that deleting a notification from the notification panel will not affect their saved bookmarks. Accidentally removing bookmarks leads to frustration and loss of important saved information.
- **Data Integrity:** Bookmarks are a separate user-driven feature. Deleting them as a side effect of another action violates the principle of least surprise and can result in unintended data loss.
- **Separation of Concerns:** Each user action (delete notification vs. unpin bookmark) should have a clear, isolated effect in the codebase and database. This makes the application more maintainable and predictable.
- **Future-proofing:** As features grow (e.g., cross-device sync, audit logs), tightly coupling notification deletion with bookmark deletion can introduce subtle bugs and make future enhancements more difficult.

**Lesson:**  
**Never delete bookmarks or user bookmark records as a side effect of notification deletion.**  
Always ensure that bookmarks are only removed when the user explicitly requests it (e.g., by clicking "Unpin" in the bookmarks panel).

---

