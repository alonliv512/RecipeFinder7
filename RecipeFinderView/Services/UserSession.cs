using Microsoft.JSInterop;
using Model;

namespace RecipeFinderView.Services
{
    public class UserSession
    {
        // רכיב שמאפשר לנו תקשורת מול ה-JavaScript של הדפדפן (למשל כדי לגשת ל-localStorage)
        private readonly IJSRuntime _jsRuntime;

        public UserSession(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        // משתנים ששומרים את נתוני המשתמש המחובר בזיכרון בזמן שהאתר פתוח
        public int UserId { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public int RoleId { get; private set; }         // מזהה התפקיד (1 = מנהל, 2 = משתמש רגיל, 3 = יוצר תוכן)
        public string RoleName { get; private set; } = string.Empty; // שם התפקיד בטקסט

        // בדיקה מהירה האם יש משתמש מחובר (אם ה-UserId גדול מ-0)
        public bool IsLoggedIn => UserId > 0;

        // אירוע שמעדכן את שאר רכיבי האתר ברגע שהסטטוס של המשתמש משתנה (התחברות/התנתקות)
        public event Action? OnChange;

        // פונקציה שטוענת את הנתונים השמורים בדפדפן בעת טעינת האתר (כדי שהמשתמש לא ינותק ברענון F5)
        public async Task InitializeAsync()
        {
            try
            {
                // שליפת שם המשתמש מזיכרון ה-localStorage של הדפדפן
                var username = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "username");
                if (!string.IsNullOrEmpty(username))
                {
                    Username = username;

                    // שליפת מזהה המשתמש (UserId) והמרתו ממחרוזת למספר שלם
                    var userIdStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userId");
                    if (int.TryParse(userIdStr, out int parsedId))
                    {
                        UserId = parsedId;
                    }

                    // שליפת האימייל
                    Email = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "email") ?? "";

                    // שליפת מזהה התפקיד (RoleId) והמרתו למספר
                    var roleIdStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "roleId");
                    if (int.TryParse(roleIdStr, out int parsedRoleId))
                    {
                        RoleId = parsedRoleId;
                    }

                    // שליפת שם התפקיד
                    RoleName = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "roleName") ?? "";

                    // עדכון הרכיבים שמאזינים לשינויים בסשן
                    NotifyStateChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing session: " + ex.Message);
            }
        }

        // פונקציה שמופעלת מיד כשהמשתמש מתחבר בהצלחה במערכת
        public async Task Login(User user)
        {
            // שומר את הנתונים במשתנים של המחלקה בזיכרון
            UserId = user.Id;
            Username = user.Username;
            Email = user.Email;
            RoleId = user.RoleID;
            RoleName = user.RoleName;

            // שמירת הנתונים ב-LocalStorage של הדפדפן כדי שישמרו גם אחרי רענון או סגירת הדפדפן
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "username", user.Username);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userId", user.Id);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "email", user.Email ?? "");
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "roleId", user.RoleID);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "roleName", user.RoleName ?? "");

            // עדכון השdאים במערכת
            NotifyStateChanged();
        }

        // פונקציה שמופעלת כאשר המשתמש בוחר להתנתק מהמערכת
        public async Task Logout()
        {
            // איפוס כל המשתנים לערכי ברירת המחדל שלהם
            UserId = 0;
            Username = string.Empty;
            Email = string.Empty;
            RoleId = 0;
            RoleName = string.Empty;

            // מחיקת הנתונים מזיכרון ה-localStorage של הדפדפן
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "username");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userId");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "email");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "roleId");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "roleName");

            // עדכון המערכת שהמשתמש התנתק
            NotifyStateChanged();
        }

        // פונקציית עזר שבונה אובייקט User מהנתונים הקיימים בסשן (נוח לבדיקות בדפים המוגנים)
        public Task<User> GetCurrentUser()
        {
            var user = new User
            {
                Id = UserId,
                Username = Username,
                Email = Email,
                RoleID = RoleId,
                RoleName = RoleName
            };
            return Task.FromResult(user);
        }

        // פונקציית עזר פרטית שמפעילה את האירוע שמעדכן את המסכים
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}