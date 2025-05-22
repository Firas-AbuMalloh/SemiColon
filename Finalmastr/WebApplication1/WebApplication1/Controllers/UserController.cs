using Microsoft.AspNetCore.Mvc;
using SemiColon.Models;
using SemiColon.Controllers;
using Microsoft.AspNetCore.Http;
using SemiColon.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.IdentityModel.Tokens;

namespace SemiColon.Controllers
{
    public class UserController : Controller
    {
        private readonly MyDbContext _Db;
        private readonly IConfiguration _configuration;
        public UserController(MyDbContext db, IConfiguration configuration)
        {
            _Db = db;
            _configuration = configuration;
        }
        public IActionResult signIn()
        {
            return View(new SemiColon.Models.ViewModel.LoginViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> checkSignIn(SemiColon.Models.ViewModel.LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("SignIn", model);
            }

            var user = _Db.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View("SignIn", model);
            }

            bool isValid = PasswordHasher.VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt);
            if (!isValid)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View("SignIn", model);
            }

            // التحقق من حالة الحساب
            if (!user.IsVerified.GetValueOrDefault())
            {
                ModelState.AddModelError("", "Your account has been blocked. Please contact support for assistance.");
                return View("SignIn", model);
            }

            user.LastLogin = DateTime.Now;
            _Db.SaveChanges();

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Useremail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);

            // نقل بيانات سلة التسوق من الكوكيز إلى قاعدة البيانات

            const string cartCookie = "temporaryCart";
            string cartCookieValue = Request.Cookies[cartCookie];

            if (!string.IsNullOrEmpty(cartCookieValue))
            {
                List<temporaryCart> cartListFromCookie = new List<temporaryCart>();
                try
                {
                    cartListFromCookie = JsonConvert.DeserializeObject<List<temporaryCart>>(cartCookieValue);

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
                catch (Newtonsoft.Json.JsonException ex)
                {
                    Console.WriteLine($"Error deserializing cart cookie during login: {ex.Message}");
                    // يمكنك هنا إضافة منطق للتعامل مع خطأ فك تسلسل الكوكي، مثل مسحه
                    Response.Cookies.Delete(cartCookie);
                }
            }

            // نقل المفضلات من الكوكيز إلى قاعدة البيانات
            const string favoriteCookie = "temporaryFavorites";
            string favoriteCookieValue = Request.Cookies[favoriteCookie];

            if (!string.IsNullOrEmpty(favoriteCookieValue))
            {
                try
                {
                    List<temporaryFavorite> favoritesList = System.Text.Json.JsonSerializer.Deserialize<List<temporaryFavorite>>(favoriteCookieValue);

                    if (favoritesList != null && favoritesList.Any())
                    {
                        foreach (var cookieFavorite in favoritesList)
                        {
                            // التحقق إذا كان المنتج موجودًا بالفعل في المفضلات
                            var existingFavorite = await _Db.Favorites.FirstOrDefaultAsync(
                                f => f.UserId == user.Id && f.CardId == cookieFavorite.CardID);

                            if (existingFavorite == null)
                            {
                                // إضافة منتج جديد إلى المفضلات
                                var newFavorite = new Favorite
                                {
                                    UserId = user.Id,
                                    CardId = cookieFavorite.CardID,
                                    CreatedAt = cookieFavorite.CreatedAt ?? DateTime.Now
                                };
                                _Db.Favorites.Add(newFavorite);
                            }
                        }
                        await _Db.SaveChangesAsync();

                        // مسح الكوكي بعد نقل البيانات
                        Response.Cookies.Delete(favoriteCookie);
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    Console.WriteLine($"Error deserializing favorites cookie during login: {ex.Message}");
                    // مسح الكوكي في حالة حدوث خطأ
                    Response.Cookies.Delete(favoriteCookie);
                }
            }

            // تحويل المستخدم إلى الصفحة المناسبة حسب دوره
            if (user.Role == "Admin")
            {
                return RedirectToAction(nameof(adminProfile));
            }
            else
            {
                return RedirectToAction("index","Home");
            }
        }
        public IActionResult signUp()
        {

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckSignUp(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                if (_Db.Users.Any(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "This username is already taken");
                    return View("SignUp", model);
                }

                // Check if email already exists
                if (_Db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered");
                    return View("SignUp", model);
                }

                // Password hashing
                (byte[] passwordHash, byte[] passwordSalt) = PasswordHasher.HashPassword(model.Password);

                // Create new User object and assign values
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Country = model.Country,
                    PhoneNumber = model.PhoneNumber,
                    CreatedAt = DateTime.Now,
                    IsVerified = true,
                    Role = "User",
                    ProfileImageUrl = "profileImg.png"
                };

                _Db.Users.Add(user);
                _Db.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! Please sign in with your new account.";
                return RedirectToAction("SignIn");
            }

            // If we reach here, model validation failed
            return View("SignUp", model);
        }

        public async Task<IActionResult> profile()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Please log in to access your profile.";
                return RedirectToAction("signIn", "User");
            }

            // Load user with favorites included
            var profileDetails = await _Db.Users
                .Include(u => u.Favorites)
                    .ThenInclude(f => f.Card)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (profileDetails == null)
            {
                TempData["ErrorMessage"] = "User profile not found. Please log in again.";
                return RedirectToAction("signIn", "User");
            }

            var orders = await _Db.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Card)
                .ToListAsync();


            var profileDetailsViewModel = new profileViewModel
            {
                _user = profileDetails,
                _orders = orders,
            };
            if(HttpContext.Session.GetString("UserRole") !="Admin")
            return View(profileDetailsViewModel);
            else
            {
                return RedirectToAction("adminProfile", profileDetailsViewModel);

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
        public IActionResult signOut()
        {
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("Useremail");

            return RedirectToAction(nameof(signIn));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> forgotPassword(string email)
        {
            var recaptchaResponse = Request.Form["g-recaptcha-response"];

            var recaptchaSecretKey = _configuration["Recaptcha:SecretKey"];

            // 1. التحقق من مفتاح ReCaptcha
            if (string.IsNullOrEmpty(recaptchaSecretKey))
            {
                // Log error: ReCaptcha secret key not configured! (Use a logging framework)
                ModelState.AddModelError("", "ReCaptcha is not configured correctly.");
                return View();
            }

            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                ModelState.AddModelError("", "Please complete the CAPTCHA.");
                return View();
            }

            using (HttpClient client = new HttpClient())
            {
                var recaptchaResult = await client.PostAsync(
                    $"https://www.google.com/recaptcha/api/siteverify?secret={recaptchaSecretKey}&response={recaptchaResponse}", null);

                if (recaptchaResult.IsSuccessStatusCode)
                {
                    var recaptchaResponseString = await recaptchaResult.Content.ReadAsStringAsync();
                    dynamic recaptchaJson = JsonConvert.DeserializeObject(recaptchaResponseString);

                    if (recaptchaJson.success != true)
                    {
                        ModelState.AddModelError("", "CAPTCHA validation failed. Please try again.");
                        return View();
                    }
                    else if (!string.IsNullOrEmpty(email))
                    {
                        string _email = email;

                        var user = _Db.Users.FirstOrDefault(u => u.Email == _email);

                        if (user != null)
                        {
                            // هنا يمكنك إضافة منطق لإرسال رابط إعادة تعيين كلمة المرور إلى البريد الإلكتروني
                            // على سبيل المثال، يمكنك استخدام خدمة البريد الإلكتروني لإرسال رسالة تحتوي على رابط إعادة تعيين كلمة المرور
                            TempData["ErrorMessage"] = "Check your email for the password reset link.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Email not found.";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Please enter a valid email address.";
                    }
                    return View(nameof(forgotPassword));
                }
                else
                {
                    // Log error: Error verifying ReCaptcha (Use a logging framework)
                    ModelState.AddModelError("", "Error verifying ReCaptcha. Please try again.");
                    return View();
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> updateProfile(profileViewModel model)
        {
            var update = model._update; // هكذا تصل لبيانات الفورم
            int userId;
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out userId))
            {
                return RedirectToAction("signIn", "User");
            }
            var orders = await _Db.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Card)
                .ToListAsync();

            var user = await _Db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = update.FirstName;
            user.LastName = update.LastName;
            user.Username = update.Username;
            user.PhoneNumber = update.PhoneNumber;



            if (update.ChangePassword == true)
            {
                if (string.IsNullOrEmpty(update.OldPassword) || string.IsNullOrEmpty(update.ConfirmPassword) || string.IsNullOrEmpty(update.NewPassword))
                {
                    ModelState.AddModelError("", "Please fill all password fields.");
                    return View("Profile", new profileViewModel { _user = user, _update = model._update, _orders = orders }); // أو حسب اسم ViewModel اللي تستخدمه


                }
                if (update.NewPassword != update.ConfirmPassword)
                {
                    ModelState.AddModelError("", "New password and confirm password do not match.");
                    return View("Profile", new profileViewModel { _user = user, _update = model._update, _orders = orders }); // أو حسب اسم ViewModel اللي تستخدمه
                }
                else
                {
                    bool isValid = PasswordHasher.VerifyPassword(update.OldPassword, user.PasswordHash, user.PasswordSalt);
                    if (!isValid)
                    {
                        ModelState.AddModelError("", "Invalid old password.");
                        return View("Profile", new profileViewModel { _user = user, _update = model._update, _orders = orders }); // أو حسب اسم ViewModel اللي تستخدمه
                    }
                    (byte[] passwordHash, byte[] passwordSalt) = PasswordHasher.HashPassword(update.NewPassword);
                    user.PasswordHash = passwordHash;
                    user.PasswordSalt = passwordSalt;

                    _Db.Users.Update(user);
                    _Db.SaveChanges();
                }
            }
            _Db.Users.Update(user);
            _Db.SaveChanges();


            return RedirectToAction("profile");
        }


    [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            // Get user role to check if admin
            var userRole = HttpContext.Session.GetString("UserRole");
            var userIdStr = HttpContext.Session.GetString("UserId");

            // Define base query first
            IQueryable<Order> baseQuery = _Db.Orders.AsQueryable();

            // Apply filters based on role
            if (userRole != "Admin" && !string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                // Non-admin users can only see their own orders
                baseQuery = baseQuery?.Where(o => o.UserId == userId);
            }
            
            // Apply includes and get the order
            var order = await baseQuery
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Card)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            var result = order.OrderDetails.Select(od => new
            {
                id = od.Id,
                cardName = od.Card.CardName,
                quantity = od.Quantity,
                price = od.Price,
                sku = $"CARD-{od.CardId}",
                imageUrl = od.Card.ImageUrl,
                order = new {
                    id = order.Id,
                    discountValue = order.DiscountValue,
                    subtotal = order.Subtotal,
                    totalAmount = order.TotalAmount
                }
            });

            return Json(result);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetOrderMetadata(int orderId)
        {
            // Get the current user
            var userIdStr = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            // Define base query first
            IQueryable<Order> baseQuery = _Db.Orders.AsQueryable();

            // Apply user filter if not admin
            if (userRole != "Admin")
            {
                baseQuery = baseQuery.Where(o => o.UserId == userId);
            }
            
            // Apply remaining parts of the query
            var order = await baseQuery
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Create a generic shipping address since it's not in database
            var shippingAddress = new
            {
                name = "Shipping Information",
                street = "Please contact customer service",
                city = "",
                state = "",
                zip = "",
                country = ""
            };

            // Get customer information (order user or current user if admin)
            var customerId = order.UserId;
            var customer = await _Db.Users.FindAsync(customerId);

            var result = new
            {
                orderId = order.Id,
                orderDate = order.CreatedAt,
                status = order.Status,
                paymentMethod = order.PaymentMethod ?? "Credit Card",
                totalAmount = order.TotalAmount,
                subtotal = order.Subtotal,
                discountValue = order.DiscountValue,
                shippingAddress = shippingAddress,
                customer = new {
                    name = $"{customer?.FirstName} {customer?.LastName}",
                    email = order.Email ?? customer?.Email,
                    phone = order.PhoneNumber ?? customer?.PhoneNumber
                },
                itemCount = order.OrderDetails?.Count ?? 0
            };

            return Json(result);
        }
        
        [HttpPost]
        public async Task<IActionResult> ReorderItems(int orderId)
        {
            try
            {
                // Get the current user
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Get the order with items
                var order = await _Db.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Card)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                // Add items to cart
                var cartItems = new List<CartItem>();
                foreach (var item in order.OrderDetails)
                {
                    // Check if the product is still available
                    var product = await _Db.Cards.FindAsync(item.CardId);
                    if (product != null && product.StockQuantity >= item.Quantity)
                    {
                        // Create or update cart item
                        var cartItem = await _Db.CartItems.FirstOrDefaultAsync(c => 
                            c.UserId == userId && c.CardId == item.CardId);
                            
                        if (cartItem == null)
                        {
                            // Create new cart item
                            cartItem = new CartItem
                            {
                                UserId = userId,
                                CardId = item.CardId,
                                Quantity = item.Quantity,
                                CreatedAt = DateTime.Now
                            };
                            _Db.CartItems.Add(cartItem);
                        }
                        else
                        {
                            // Update existing cart item quantity
                            cartItem.Quantity += item.Quantity;
                            cartItem.UpdatedAt = DateTime.Now;
                            _Db.CartItems.Update(cartItem);
                        }
                        
                        cartItems.Add(cartItem);
                    }
                }

                // Save changes to database
                await _Db.SaveChangesAsync();
                
                // Return success
                return Json(new { 
                    success = true, 
                    message = $"Added {cartItems.Count} items to cart", 
                    itemCount = cartItems.Sum(i => i.Quantity) 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error adding items to cart: {ex.Message}" });
            }
        }

        public async Task<IActionResult> adminProfile(string viewName = null, int? showDetails = null)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Please log in to access your profile.";
                return RedirectToAction("signIn", "User");
            }

            // Check if the user has admin role
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "You don't have permission to access the admin profile.";
                return RedirectToAction("profile", "User");
            }
            
            // Store details of the user to show in ViewBag if showDetails parameter is provided
            if (showDetails.HasValue)
            {
                var userDetails = await _Db.Users.FindAsync(showDetails.Value);
                if (userDetails != null)
                {
                    ViewBag.ShowUserDetails = true;
                    ViewBag.UserDetailsId = userDetails.Id;
                    ViewBag.UserDetailsName = $"{userDetails.FirstName} {userDetails.LastName}";
                    ViewBag.UserDetailsUsername = userDetails.Username;
                    ViewBag.UserDetailsEmail = userDetails.Email;
                    ViewBag.UserDetailsRole = userDetails.Role;
                    ViewBag.UserDetailsIsVerified = userDetails.IsVerified;
                    ViewBag.UserDetailsProfileImageUrl = userDetails.ProfileImageUrl;
                    
                    // Set the viewName to Users if not already set, so we display the users tab
                    if (string.IsNullOrEmpty(viewName))
                    {
                        viewName = "Users";
                    }
                }
            }

            var profileDetails = await _Db.Users.FindAsync(userId);

            if (profileDetails == null)
            {
                TempData["ErrorMessage"] = "User profile not found. Please log in again.";
                return RedirectToAction("signIn", "User");
            }

            // Get all orders for all users (since this is an admin view)
            var orders = await _Db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Card)
                .Include(o => o.User)  // Include user information
                .ToListAsync();

            var allCards = await _Db.Cards.Include(c=>c.Category).ToListAsync();
            
            // تأكد من وجود بائع افتراضي
            await EnsureDefaultSellerExists();
            
            // جلب قائمة البائعين
            var allSellers = await _Db.Sellers
                .Include(s => s.User)
                .Select(s => new { 
                    Id = s.Id, 
                    Name = !string.IsNullOrEmpty(s.CompanyName) ? s.CompanyName : s.User.Username 
                })
                .ToListAsync();
            
            // جلب قائمة الفئات
            var allCategories = await _Db.Categories
                .Select(c => new { 
                    Id = c.Id, 
                    Name = c.CategoryName 
                })
                .ToListAsync();
            
            // جلب قائمة المستخدمين
            var allUsers = await _Db.Users.ToListAsync();
            
            // جلب قائمة أكواد الخصم
            var allDiscounts = await _Db.Discounts.ToListAsync();
            
            // جلب قائمة المدونات
            var allBlogs = await _Db.Blogs.Include(b => b.Author).ToListAsync();
            
            // جلب قائمة الملاحظات وآراء المستخدمين
            var allFeedbacks = await _Db.ContactFeedBacks.ToListAsync();

            // جلب قائمة الشهادات
            var allTestimonials = await _Db.Testimonials.Include(t => t.User).ToListAsync();
            
            // تمرير البيانات إلى العرض
            ViewBag.AllSellers = allSellers;
            ViewBag.AllCategories = allCategories;
            
            // Set the active tab based on viewName parameter
            if (!string.IsNullOrEmpty(viewName))
            {
                ViewBag.ActiveView = viewName;
            }
            
            var profileDetailsViewModel = new adminProfileViewModel
            {
                _user = profileDetails,
                _orders = orders,
                _allCards = allCards,
                _allUsers = allUsers,
                _allDiscounts = allDiscounts,
                _allBlogs = allBlogs,
                _allFeedbacks = allFeedbacks,
                Testimonials = allTestimonials
            };

            return View(profileDetailsViewModel);
        }

        public async Task<IActionResult> deleteCard(int id)
        {
            var card = await _Db.Cards.FindAsync(id);
            if (card == null)
            {
                return Json(new { success = false, message = "Card not found." });
            }

            _Db.Cards.Remove(card);
            await _Db.SaveChangesAsync();

            return Json(new { success = true, message = "Card deleted successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> getCardDetails(int id)
        {
            var card = await _Db.Cards
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (card == null)
            {
                return Json(new { success = false, message = "Card not found." });
            }

            var result = new
            {
                id = card.Id,
                cardName = card.CardName,
                sellerId = card.SellerId,
                categoryId = card.CategoryId,
                categoryName = card.Category?.CategoryName,
                description = card.Description,
                price = card.Price,
                stockQuantity = card.StockQuantity,
                imageUrl = card.ImageUrl,
                success = true
            };

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> updateCard(int id, string cardName, double price, int stockQuantity, int categoryId, int sellerId, string description, string imageUrl)
        {
            if (string.IsNullOrEmpty(cardName))
            {
                return Json(new { success = false, message = "Card name cannot be empty." });
            }

            try
            {
                var card = await _Db.Cards.FindAsync(id);
                if (card == null)
                {
                    return Json(new { success = false, message = "Card not found." });
                }

                // Update card details
                card.CardName = cardName;
                card.Price = (decimal)price; // Ensure the price is cast to decimal as per the Card class definition
                card.StockQuantity = stockQuantity;
                card.CategoryId = categoryId;
                card.SellerId = sellerId;
                card.Description = description;

                // Update the image if it has been changed
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    card.ImageUrl = imageUrl;
                }

                _Db.Cards.Update(card);
                await _Db.SaveChangesAsync();

                return Json(new { success = true, message = "Card updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating card: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> addCard(string cardName, double price, int stockQuantity, int categoryId, int sellerId, string description, string imageUrl)
        {
            if (string.IsNullOrEmpty(cardName))
            {
                return Json(new { success = false, message = "Card name cannot be empty." });
            }

            try
            {
                // Validate required fields
                if (categoryId <= 0)
                {
                    return Json(new { success = false, message = "Please select a valid category." });
                }
                
                // جلب قائمة البائعين المتاحين
                var availableSellers = await GetAllSellers();
                
                if (!availableSellers.Any())
                {
                    return Json(new { success = false, message = "No sellers available in the database. Please contact the administrator." });
                }
                
                // التحقق من صحة معرف البائع
                if (sellerId <= 0 || !availableSellers.Any(s => s.Id == sellerId))
                {
                    // استخدام أول بائع متاح
                    sellerId = availableSellers.First().Id;
                    Console.WriteLine($"Using first available seller with ID: {sellerId}");
                }
                else
                {
                    // التأكد من أن البائع المختار موجود
                    var seller = availableSellers.FirstOrDefault(s => s.Id == sellerId);
                    if (seller == null)
                    {
                        sellerId = availableSellers.First().Id;
                        Console.WriteLine($"Selected seller not found. Using first available seller with ID: {sellerId}");
                    }
                    else
                    {
                        Console.WriteLine($"Using selected seller with ID: {sellerId}");
                    }
                }

                // Create new card
                var newCard = new Card
                {
                    CardName = cardName,
                    Price = (decimal)price,
                    StockQuantity = stockQuantity,
                    CategoryId = categoryId,
                    SellerId = sellerId,
                    Description = string.IsNullOrEmpty(description) ? null : description,
                    ImageUrl = !string.IsNullOrEmpty(imageUrl) ? imageUrl : "default-card.jpg",
                    CreatedAt = DateTime.Now
                    // No need to set UpdatedAt as it's not in the model
                };

                _Db.Cards.Add(newCard);
                await _Db.SaveChangesAsync();

                return Json(new { success = true, message = "Card added successfully." });
            }
            catch (Exception ex)
            {
                // Log the full exception details
                var innerExMessage = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";
                Console.WriteLine($"Error adding card: {ex.Message}, Inner exception: {innerExMessage}");
                
                return Json(new { 
                    success = false, 
                    message = $"Error adding card: {ex.Message}",
                    details = innerExMessage
                });
            }
        }

        public async Task<IActionResult> getCategoryList()
        {
            try
            {
                var categories = await _Db.Categories.Select(c => new { id = c.Id, name = c.CategoryName }).ToListAsync();
                return Json(new { success = true, categories });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error fetching categories: {ex.Message}" });
            }
        }

                // First method has been removed as it was causing conflicts with the overloaded version below

        [HttpPost]
        public async Task<IActionResult> updateUserRole(int userId, string role, string isVerified)
        {
            try
            {
                // Parse isVerified string to boolean
                bool isVerifiedBool = isVerified?.ToLower() == "true";
                
                Console.WriteLine($"Updating user {userId} - Role: {role}, IsVerified: {isVerified} (parsed to: {isVerifiedBool})");
                
                // التحقق من وجود المستخدم
                var user = await _Db.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // التحقق من صحة الدور المطلوب
                if (string.IsNullOrEmpty(role) || (role != "Admin" && role != "User"))
                {
                    return Json(new { success = false, message = "Invalid role. Role must be Admin or User" });
                }

                // تحديث دور المستخدم وحالة التحقق
                user.Role = role;
                user.IsVerified = isVerifiedBool;
                _Db.Users.Update(user);
                await _Db.SaveChangesAsync();

                string statusMessage = isVerifiedBool ? "verified" : "blocked";
                return Json(new { success = true, message = $"User role updated to {role} and status set to {statusMessage} successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                return Json(new { success = false, message = $"Error updating user: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> getUserDetails(int id)
        {
            try
            {
                var user = await _Db.Users.FindAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var result = new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    phoneNumber = user.PhoneNumber,
                    country = user.Country,
                    role = user.Role,
                    isVerified = user.IsVerified,
                    profileImageUrl = user.ProfileImageUrl,
                    createdAt = user.CreatedAt,
                    lastLogin = user.LastLogin,
                    success = true
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error fetching user details: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> getSellerList()
        {
            try
            {
                // تأكد من وجود بائع افتراضي
                await EnsureDefaultSellerExists();
                
                var sellers = await _Db.Sellers
                    .Include(s => s.User)
                    .Select(s => new { 
                        id = s.Id, 
                        name = !string.IsNullOrEmpty(s.CompanyName) ? s.CompanyName : s.User.Username 
                    })
                    .ToListAsync();
                    
                return Json(new { success = true, sellers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error fetching sellers: {ex.Message}" });
            }
        }

        private async Task<List<Seller>> GetAllSellers()
        {
            // هل هناك بائعون في قاعدة البيانات؟
            var sellers = await _Db.Sellers.ToListAsync();
            
            if (!sellers.Any())
            {
                // طباعة رسالة في وحدة التحكم للتأكد من أن القيمة الافتراضية يتم استخدامها
                Console.WriteLine("No sellers found. Creating default seller...");
                
                // تأكد من وجود بائع افتراضي
                await EnsureDefaultSellerExists();
                
                // جلب البائعين مرة أخرى بعد إنشاء واحد افتراضي
                sellers = await _Db.Sellers.ToListAsync();
            }
            
            return sellers;
        }

        private async Task EnsureDefaultSellerExists()
        {
            // تحقق من وجود بائعين
            var sellerExists = await _Db.Sellers.AnyAsync();
            
            if (!sellerExists)
            {
                // تحقق من وجود مستخدم أدمن
                var adminUser = await _Db.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                
                if (adminUser == null)
                {
                    // استخدم أي مستخدم موجود
                    adminUser = await _Db.Users.FirstOrDefaultAsync();
                    
                    if (adminUser == null)
                    {
                        // لا يوجد مستخدمين، لا يمكن إنشاء بائع
                        return;
                    }
                }
                
                // طباعة معلومات المستخدم للتأكد من صحتها
                Console.WriteLine($"Creating default seller with UserId: {adminUser.Id}");
                
                // إنشاء بائع افتراضي باستخدام المستخدم الحالي
                var defaultSeller = new Seller
                {
                    UserId = adminUser.Id,
                    CompanyName = "Default Seller",
                    ContactEmail = adminUser.Email,
                    ContactPhone = adminUser.PhoneNumber,
                    Balance = 0,
                    CreatedAt = DateTime.Now
                };
                
                _Db.Sellers.Add(defaultSeller);
                await _Db.SaveChangesAsync();
                
                // طباعة معرف البائع الجديد للتأكد
                Console.WriteLine($"Default seller created with ID: {defaultSeller.Id}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> getDiscountDetails(int id)
        {
            var discount = await _Db.Discounts.FindAsync(id);

            if (discount == null)
            {
                return Json(new { success = false, message = "Discount not found." });
            }

            var result = new
            {
                id = discount.Id,
                code = discount.DiscountCode,
                description = discount.Description,
                value = discount.DiscountValue,
                isPercentage = discount.IsPercentage,
                startDate = discount.StartDate?.ToString("yyyy-MM-ddTHH:mm"),
                endDate = discount.EndDate?.ToString("yyyy-MM-ddTHH:mm"),
                usageCount = discount.UsageCount,
                usageLimit = discount.UsageLimit,
                isActive = discount.IsActive,
                applicableToUser = discount.ApplicableToUser,
                applicableToCart = discount.ApplicableToCart,
                createdAt = discount.CreatedAt,
                success = true
            };

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> deleteDiscount(int id)
        {
            var discount = await _Db.Discounts.FindAsync(id);
            if (discount == null)
            {
                return Json(new { success = false, message = "Discount not found." });
            }

            _Db.Discounts.Remove(discount);
            await _Db.SaveChangesAsync();

            return Json(new { success = true, message = "Discount deleted successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> updateDiscount(int id, string discountCode, string description, 
            decimal discountValue, bool isPercentage, string startDate, string endDate,
            int? usageLimit, bool isActive, int? applicableToUser, bool applicableToCart)
        {
            if (string.IsNullOrEmpty(discountCode))
            {
                return Json(new { success = false, message = "Discount code cannot be empty." });
            }

            try
            {
                var discount = await _Db.Discounts.FindAsync(id);
                if (discount == null)
                {
                    return Json(new { success = false, message = "Discount not found." });
                }

                // Update discount details
                discount.DiscountCode = discountCode;
                discount.Description = description;
                discount.DiscountValue = discountValue;
                discount.IsPercentage = isPercentage;
                
                // Parse dates if provided
                if (!string.IsNullOrEmpty(startDate))
                {
                    discount.StartDate = DateTime.Parse(startDate);
                }
                
                if (!string.IsNullOrEmpty(endDate))
                {
                    discount.EndDate = DateTime.Parse(endDate);
                }
                
                discount.UsageLimit = usageLimit;
                discount.IsActive = isActive;
                discount.ApplicableToUser = applicableToUser;
                discount.ApplicableToCart = applicableToCart;
                discount.UpdatedAt = DateTime.Now;

                _Db.Discounts.Update(discount);
                await _Db.SaveChangesAsync();

                return Json(new { success = true, message = "Discount updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating discount: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> addDiscount(string discountCode, string description, 
            decimal discountValue, bool isPercentage, string startDate, string endDate,
            int? usageLimit, bool isActive, int? applicableToUser, bool applicableToCart)
        {
            if (string.IsNullOrEmpty(discountCode))
            {
                return Json(new { success = false, message = "Discount code cannot be empty." });
            }

            try
            {
                // Check if discount code already exists
                if (await _Db.Discounts.AnyAsync(d => d.DiscountCode == discountCode))
                {
                    return Json(new { success = false, message = "This discount code already exists." });
                }

                // Create new discount
                var newDiscount = new Discount
                {
                    DiscountCode = discountCode,
                    Description = description,
                    DiscountValue = discountValue,
                    IsPercentage = isPercentage,
                    IsActive = isActive,
                    UsageCount = 0,
                    UsageLimit = usageLimit,
                    ApplicableToUser = applicableToUser,
                    ApplicableToCart = applicableToCart,
                    CreatedAt = DateTime.Now
                };

                // Parse dates if provided
                if (!string.IsNullOrEmpty(startDate))
                {
                    newDiscount.StartDate = DateTime.Parse(startDate);
                }
                
                if (!string.IsNullOrEmpty(endDate))
                {
                    newDiscount.EndDate = DateTime.Parse(endDate);
                }

                _Db.Discounts.Add(newDiscount);
                await _Db.SaveChangesAsync();

                return Json(new { success = true, message = "Discount added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error adding discount: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> getBlogDetails(int id)
        {
            try
            {
                var blog = await _Db.Blogs
                    .Include(b => b.Author)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (blog == null)
                {
                    return Json(new { success = false, message = "Blog not found." });
                }

                var result = new
                {
                    id = blog.Id,
                    title = blog.Title,
                    content = blog.Content,
                    authorId = blog.AuthorId,
                    authorName = $"{blog.Author.FirstName} {blog.Author.LastName}",
                    authorUsername = blog.Author.Username,
                    createdAt = blog.CreatedAt,
                    success = true
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error fetching blog details: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> deleteBlog(int id)
        {
            try
            {
                var blog = await _Db.Blogs.FindAsync(id);
                if (blog == null)
                {
                    return Json(new { success = false, message = "Blog not found." });
                }

                _Db.Blogs.Remove(blog);
                await _Db.SaveChangesAsync();

                return Json(new { success = true, message = "Blog deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting blog: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> updateBlog(int id, string title, string content)
        {
            try
            {
                if (string.IsNullOrEmpty(title))
                {
                    return Json(new { success = false, message = "Blog title cannot be empty." });
                }

                if (string.IsNullOrEmpty(content))
                {
                    return Json(new { success = false, message = "Blog content cannot be empty." });
                }

                var blog = await _Db.Blogs.FindAsync(id);
                if (blog == null)
                {
                    return Json(new { success = false, message = "Blog not found." });
                }

                blog.Title = title;
                blog.Content = content;
                
                _Db.Blogs.Update(blog);
                await _Db.SaveChangesAsync();

                return Json(new { success = true, message = "Blog updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating blog: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> addBlog(string title, string content)
        {
            try
            {
                if (string.IsNullOrEmpty(title))
                {
                    return Json(new { success = false, message = "Blog title cannot be empty." });
                }

                if (string.IsNullOrEmpty(content))
                {
                    return Json(new { success = false, message = "Blog content cannot be empty." });
                }

                // Get current user ID from session
                int currentUserId;
                if (!int.TryParse(HttpContext.Session.GetString("UserId"), out currentUserId))
                {
                    return Json(new { success = false, message = "User not authenticated." });
                }

                var newBlog = new Blog
                {
                    Title = title,
                    Content = content,
                    AuthorId = currentUserId,
                    CreatedAt = DateTime.Now
                };

                _Db.Blogs.Add(newBlog);
                await _Db.SaveChangesAsync();

                return Json(new { success = true, message = "Blog added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error adding blog: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> toggleFeedbackStatus(int id)
        {
            try
            {
                // Find the feedback item
                var feedback = await _Db.ContactFeedBacks.FindAsync(id);
                if (feedback == null)
                {
                    return Json(new { success = false, message = "Feedback not found." });
                }

                // Toggle the status
                feedback.IsPublished = !feedback.IsPublished;

                // If the feedback is now published, copy it to the Testimonials table
                if (feedback.IsPublished)
                {
                    // Create new Testimonial from the feedback
                    var testimonial = new Testimonial
                    {
                        UserId = feedback.UserId,
                        UserName = feedback.Name,
                        UserEmail = feedback.Email,
                        Subject = feedback.Subject,
                        Message = feedback.Message,
                        CreatedAt = DateTime.Now,
                        IsPublished = true
                    };

                    // Add to Testimonials
                    _Db.Testimonials.Add(testimonial);
                    
                    // Delete from ContactFeedBack
                    _Db.ContactFeedBacks.Remove(feedback);
                    
                    await _Db.SaveChangesAsync();
                    
                    return Json(new { 
                        success = true, 
                        message = "Feedback published as testimonial and removed from feedback list.",
                        isPublished = true
                    });
                }
                else
                {
                    // Just update the status
                    _Db.ContactFeedBacks.Update(feedback);
                    await _Db.SaveChangesAsync();
                    
                    return Json(new { 
                        success = true, 
                        message = "Feedback status updated to unpublished.",
                        isPublished = false
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating feedback status: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> getFeedbackDetails(int id)
        {
            try
            {
                var feedback = await _Db.ContactFeedBacks.FindAsync(id);
                if (feedback == null)
                {
                    return Json(new { success = false, message = "Feedback not found." });
                }

                var result = new
                {
                    id = feedback.Id,
                    name = feedback.Name,
                    email = feedback.Email,
                    phone = feedback.Phone,
                    subject = feedback.Subject,
                    message = feedback.Message,
                    createdAt = feedback.CreatedAt.HasValue ? feedback.CreatedAt.Value.ToString("yyyy-MM-dd") : null,
                    isPublished = feedback.IsPublished,
                    success = true
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error fetching feedback details: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> deleteFeedback(int id)
        {
            try
            {
                var feedback = await _Db.ContactFeedBacks.FindAsync(id);
                if (feedback == null)
                {
                    return Json(new { success = false, message = "Feedback not found." });
                }

                _Db.ContactFeedBacks.Remove(feedback);
                await _Db.SaveChangesAsync();

                return Json(new { success = true, message = "Feedback deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting feedback: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> deleteTestimonial(int id)
        {
            try
            {
                var testimonial = await _Db.Testimonials.FindAsync(id);
                if (testimonial == null)
                {
                    return Json(new { success = false, message = "Testimonial not found." });
                }

                _Db.Testimonials.Remove(testimonial);
                await _Db.SaveChangesAsync();

                return Json(new { success = true, message = "Testimonial deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting testimonial: {ex.Message}" });
            }
        }
    }
}