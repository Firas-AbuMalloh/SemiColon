using Microsoft.AspNetCore.Mvc;
using SemiColon.Models;
using SemiColon.Controllers;
using Microsoft.AspNetCore.Http;
using SemiColon.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace SemiColon.Controllers
{
    public class UserController : Controller
    {
        private readonly MyDbContext _Db ;

        public UserController(MyDbContext db)
        {
            _Db = db;
        }
        public IActionResult signIn()
        {

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> checkSignIn(string email, string password)
        {

            var user = _Db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid email or password";
                return View("SignIn");
            }

            bool isValid = PasswordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
            if (!isValid)
            {
                ViewBag.Error = "Invalid email or password.";
                return View("SignIn");
            }

            user.LastLogin = DateTime.Now;
            _Db.SaveChanges();

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Useremail", user.Email);

            // نقل بيانات سلة التسوق من الكوكيز إلى قاعدة البيانات
            const string cartCookie = "temporaryCart";
            string cartCookieValue = Request.Cookies[cartCookie];

            if (!string.IsNullOrEmpty(cartCookieValue))
            {
                List<temporaryCart> cartListFromCookie = new List<temporaryCart>();
                try
                {
                    cartListFromCookie = JsonSerializer.Deserialize<List<temporaryCart>>(cartCookieValue);

                    if (cartListFromCookie != null && cartListFromCookie.Any())
                    {
                        // التأكد من وجود كارت للمستخدم أو إنشائه إذا لم يكن موجودًا
                        var cart = await _Db.Carts.FirstOrDefaultAsync(c => c.UserId == user.Id);
                        if (cart == null)
                        {
                            cart = new Cart 
                            { 
                                UserId = user.Id,
                                CreatedAt = DateTime.Now,
                                
                            };
                            _Db.Carts.Add(cart);
                            await _Db.SaveChangesAsync();
                        }

                        foreach (var cookieItem in cartListFromCookie)
                        {
                            // التحقق إذا كان المنتج موجودًا بالفعل في سلة التسوق
                            var existingCartItem = await _Db.CartItems.FirstOrDefaultAsync(
                                item => item.CartId == cart.Id && item.CardId == cookieItem.CardID);

                            if (existingCartItem != null)
                            {
                                // زيادة الكمية إذا كان المنتج موجودًا
                                existingCartItem.Quantity += cookieItem.Quantity;
                                existingCartItem.UpdatedAt = DateTime.Now;
                                _Db.Update(existingCartItem);
                            }
                            else
                            {
                                // إضافة عنصر جديد إلى سلة التسوق
                                var newCartItem = new CartItem
                                {
                                    CartId = cart.Id,
                                    CardId = cookieItem.CardID,
                                    Quantity = cookieItem.Quantity,
                                    Price = cookieItem.Price,
                                    CreatedAt = cookieItem.CreatedAt, // الاحتفاظ بتاريخ الإنشاء الأصلي من الكوكي
                                    UpdatedAt = DateTime.Now,
                                    UserId = user.Id // يجب تعيين UserId هنا أيضًا
                                };
                                _Db.CartItems.Add(newCartItem);
                            }
                        }
                        await _Db.SaveChangesAsync();

                        // مسح الكوكي بعد نقل البيانات
                        Response.Cookies.Delete(cartCookie);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error deserializing cart cookie during login: {ex.Message}");
                    // يمكنك هنا إضافة منطق للتعامل مع خطأ فك تسلسل الكوكي، مثل مسحه
                    Response.Cookies.Delete(cartCookie);
                }
            }

            return RedirectToAction(nameof(HomeController.index), "Home");
        }
        public IActionResult signUp()
        {
           
            return View();
        }
        [HttpPost]
        public IActionResult CheckSignUp(User user, string Password)
        {
            if (ModelState.IsValid)
            {

                // تشفير كلمة المرور  Password
                (byte[] passwordHash, byte[] passwordSalt) = PasswordHasher.HashPassword(Password);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                user.CreatedAt = DateTime.Now; 
                user.IsVerified = false;       
                user.Role = "User";            
                user.ProfileImageUrl = "profileImg.png";
                _Db.Users.Add(user);
                _Db.SaveChanges();
                return RedirectToAction("SignIn"); 
            }
            return View();
        }
        public async Task<IActionResult> profile()
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));

            var profileDetails = await _Db.Users.FindAsync(userId);

            var orders = await _Db.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Card) // تحميل تفاصيل الطلبات والبطاقات المرتبطة بها
                .ToListAsync();

            if (profileDetails != null)
            {
                var profileDetailsViewModel = new profileViewModel
                {
                    _user = profileDetails,
                    _orders = orders,
                    _ordersDetails = orders.SelectMany(o => o.OrderDetails).ToList()
                };
                return View(profileDetailsViewModel);
            }
            else
            {
                return View("No User Found");
            }
        }
        public IActionResult resetPassword()
        {
            return View();
        }
        public IActionResult forgotPassword()
        {
            return View();
        }
        public IActionResult signOut() {
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("Useremail");

            return RedirectToAction(nameof(signIn));
        }

    }
}
