using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SemiColon.Models;
using SemiColon.Models.ViewModel;
using System.Text.Json; // تأكد من وجود هذا السطر في الأعلى

using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Collections.Generic;

namespace SemiColon.Controllers
{
    public class ProductsController : Controller
    {
        private readonly MyDbContext _Db;

        // مجموعة لتخزين الطلبات النشطة وتجنب التكرار
        private static readonly Dictionary<string, DateTime> _activeRequests = new Dictionary<string, DateTime>();
        private static readonly Dictionary<string, DateTime> _activeFavRequests = new Dictionary<string, DateTime>();
        private static readonly object _lockObj = new object();
        private static readonly object _favLockObj = new object();

        public ProductsController(MyDbContext Db)
        {
            _Db = Db;
        }
        public async Task<IActionResult> cart()
        {

            string userIdString = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
            {
                var thiscartItems = await _Db.CartItems
                    .Where(x => x.UserId == userId)
                    .Include(x => x.Card)
                    .ToListAsync();

                // Fix: Correct the type name to match the actual class definition
                if (thiscartItems.Count == 0)
                {
                    cartViewModel cartItemsViewModel = new cartViewModel
                    {
                        _cartItem = null,
                        _tempCart = null
                    };
                    return View(cartItemsViewModel);
                }
                else
                {
                    cartViewModel cartItemsViewModel = new cartViewModel
                    {
                        _cartItem = thiscartItems,
                        _tempCart = null
                    };
                    return View(cartItemsViewModel);
                }
                
               
            }
            else
            {
               
                const string cartCookie = "temporaryCart";

                string cartCookieValue = Request.Cookies[cartCookie];

                

                List<temporaryCart> cartListFromCookie = new List<temporaryCart>();



                // التحقق إذا كانت قيمة الكوكي غير فارغة أو null



                if (!string.IsNullOrEmpty(cartCookieValue))

                {

                    

                    try

                    {

                        cartListFromCookie = JsonSerializer.Deserialize<List<temporaryCart>>(cartCookieValue);

                        // إذا تم فك التسلسل بنجاح، يتم إرجاع عرض مع قائمة العناصر من الكوكي

                        cartViewModel listFromCookie = new cartViewModel
                        {
                            _tempCart = cartListFromCookie,
                            _cartItem = null
                        };
                        return View(listFromCookie);

                    }

                    // التقاط أي استثناء قد يحدث أثناء فك تسلسل JSON (مثل تنسيق غير صحيح)

                    catch (JsonException ex)

                    {

                        // تسجيل الخطأ في وحدة التحكم (لأغراض التصحيح)

                        Console.WriteLine($"Error deserializing cart cookie for anonymous user: {ex.Message}");

                        // في حالة حدوث خطأ في فك التسلسل، يتم إرجاع عرض مع قائمة فارغة لتجنب الأعطال

                        return View(new cartViewModel());

                    }

                }

                else

                {

                    // إذا كانت قيمة الكوكي فارغة أو null (أي لا توجد سلة تسوق مؤقتة)،

                    // يتم إرجاع عرض مع قائمة فارغة

                    return View(new cartViewModel());

                }

            }

        }
        public IActionResult checkout()
        {
            string userIdString = HttpContext.Session.GetString("UserId");

            if (int.TryParse(userIdString, out int userId)) // Changed Int64 to long for consistency
            {
                decimal sub = 0;
                decimal total = 0;
                var cartItems = _Db.CartItems.Where(id => id.UserId == (int)userId) // Explicitly cast userId to int
                                             .Include(nameof(Card))
                                             .ToList();

                if (cartItems.Any())
                {
                    var temporaryCartItems = cartItems.Select(cartItem => new temporaryCart
                    {
                        CardID = cartItem.CardId,
                        CardName = cartItem.Card.CardName,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Price
                    }).ToList();

                    foreach (var temp in temporaryCartItems)
                    {
                        sub += temp.Quantity * temp.Price;
                    }
                    total = sub;

                    temporaryCartItems[0].Subtotal = sub;
                    temporaryCartItems[0].Total = total;

                    return View(temporaryCartItems);
                }
                return RedirectToAction("shop", "Products");
            }
            else
            {
                return RedirectToAction("profile", "User");
            }
        }

        public async Task<IActionResult> shop(int? id)
        {
            if (id != null)
            {
                var products = await _Db.Cards.Where(p => p.CategoryId == id).ToListAsync();

                return View(products);
            }
            else
            {
                var products = await _Db.Cards.ToListAsync();

                return View(products);
            }
                
              
            }
           
        
        public async Task<IActionResult> categoryShop(int? id)
        {

            if (id != null)
            {
                var products = await _Db.Categories.Where(p => p.MainCategoryId == id).ToListAsync();
                return View(products);
            }
            else
            {
                var products = await _Db.Categories.ToListAsync();
                return View(products);
            }
           
        }

        [HttpGet]
        public IActionResult productDetails(int id)
        {
            var product = _Db.Cards.Find(id);
            if (product != null)
            {
                var products = _Db.Cards.Where(CategoryItems => CategoryItems.CategoryId == product.CategoryId).ToList();


                var Single_Cards = new SingleDetailsViewModel
                {
                    singleCard = product,
                    cards = products
                };

                return View(Single_Cards);
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            int totalCount = 0;
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdStr, out int userId))
            {
                var cart = await _Db.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
                if (cart != null)
                {
                    totalCount = await _Db.CartItems
                        .Where(c => c.CartId == cart.Id)
                        .SumAsync(item => item.Quantity);
                }
            }
            else
            {
                var cookie = HttpContext.Request.Cookies["temporaryCart"];
                if (!string.IsNullOrEmpty(cookie))
                {
                    try
                    {
                        var items = JsonSerializer.Deserialize<List<temporaryCart>>(cookie);
                        totalCount = items?.Sum(i => i.Quantity) ?? 0;
                    }
                    catch
                    {
                        totalCount = 0;
                    }
                }
            }

            return Json(new { cartCount = totalCount });
        }


        [HttpPost]
        public async Task<IActionResult> addToCart(int id, int quantity = 1, string _requestId = null)
        {
            // تنظيف الطلبات القديمة (أكثر من 5 ثوان)
            lock (_lockObj)
            {
                var now = DateTime.Now;
                var keysToRemove = _activeRequests.Where(kvp => (now - kvp.Value).TotalSeconds > 5)
                    .Select(kvp => kvp.Key).ToList();
                
                foreach (var key in keysToRemove)
                {
                    _activeRequests.Remove(key);
                }
                
                // التحقق من وجود طلب مماثل
                string requestKey = $"cart_{id}";
                
                if (_activeRequests.ContainsKey(requestKey))
                {
                    // طلب مكرر، تجاهله
                    return Json(new { success = true, message = "Request already processed.", cartCount = GetCurrentCartCount() });
                }
                
                // تسجيل الطلب كنشط
                _activeRequests[requestKey] = now;
            }
            
            try 
            {
                var product = await _Db.Cards.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                // Validate quantity
                if (quantity <= 0)
                {
                    quantity = 1; // Default to 1 if invalid quantity
                }

                // Ensure quantity doesn't exceed stock
                if (quantity > product.StockQuantity)
                {
                    quantity = product.StockQuantity;
                    if (quantity <= 0)
                    {
                        return Json(new { success = false, message = "Product is out of stock." });
                    }
                }

                temporaryCart cartItemForAnon = new temporaryCart
                {
                    Id = new Random().Next(1, int.MaxValue),
                    CardID = product.Id,
                    Quantity = quantity,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    CardName = product.CardName,
                    CreatedAt = DateTime.Now
                };

                int totalCount = 0; // لتخزين عدد العناصر في السلة

                if (HttpContext.Session.GetString("UserId") != null)
                {
                    string userId = HttpContext.Session.GetString("UserId");
                    if (int.TryParse(userId, out int parsedUserId))
                    {
                        // استخدام معاملة للتأكد من عدم حدوث إضافات متكررة
                        using (var transaction = await _Db.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                // تحقق من وجود سلة للمستخدم أو إنشاء واحدة جديدة
                                var cart = await _Db.Carts
                                    .FirstOrDefaultAsync(c => c.UserId == parsedUserId);
                                
                                if (cart == null)
                                {
                                    cart = new Cart { 
                                        UserId = parsedUserId, 
                                        CreatedAt = DateTime.Now 
                                    };
                                    _Db.Carts.Add(cart);
                                    await _Db.SaveChangesAsync();
                                }

                                // تحقق من وجود المنتج في السلة
                                var existingCartItem = await _Db.CartItems
                                    .FirstOrDefaultAsync(item => item.CartId == cart.Id && item.CardId == id);

                                if (existingCartItem != null)
                                {
                                    existingCartItem.Quantity += quantity;
                                    existingCartItem.UpdatedAt = DateTime.Now;
                                    _Db.Update(existingCartItem);
                                }
                                else
                                {
                                    var newCartItem = new CartItem
                                    {
                                        CartId = cart.Id,
                                        CardId = id,
                                        Quantity = quantity,
                                        Price = product.Price,
                                        CreatedAt = DateTime.Now,
                                        UpdatedAt = DateTime.Now,
                                        UserId = parsedUserId
                                    };
                                    _Db.CartItems.Add(newCartItem);
                                }

                                await _Db.SaveChangesAsync();
                                
                                // إتمام المعاملة بنجاح
                                await transaction.CommitAsync();

                                // حساب عدد العناصر في السلة بعد الإضافة
                                totalCount = await _Db.CartItems
                                    .Where(c => c.UserId == parsedUserId)
                                    .SumAsync(item => item.Quantity);
                            }
                            catch (Exception ex)
                            {
                                // إلغاء المعاملة في حالة حدوث خطأ
                                await transaction.RollbackAsync();
                                Console.WriteLine($"Error adding to cart: {ex.Message}");
                                return Json(new { success = false, message = "Error adding product to cart." });
                            }
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "Invalid user session." });
                    }
                }
                else
                {
                    const string cartCookie = "temporaryCart";
                    string cartCookieValue = Request.Cookies[cartCookie];
                    List<temporaryCart> cartList = new List<temporaryCart>();

                    if (!string.IsNullOrEmpty(cartCookieValue))
                    {
                        try
                        {
                            cartList = JsonSerializer.Deserialize<List<temporaryCart>>(cartCookieValue);
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"Error deserializing cart cookie: {ex.Message}");
                            cartList = new List<temporaryCart>();
                        }
                    }

                    // التأكد إن المنتج مش متكرر
                    var existingItemInCookie = cartList.FirstOrDefault(item => item.CardID == cartItemForAnon.CardID);

                    if (existingItemInCookie != null)
                    {
                        existingItemInCookie.Quantity += quantity;
                    }
                    else
                    {
                        // التأكد إن Id المنتج الجديد مش متكرر
                        while (cartList.Any(x => x.Id == cartItemForAnon.Id))
                        {
                            cartItemForAnon.Id = new Random().Next(1, int.MaxValue);
                        }
                        cartList.Add(cartItemForAnon);
                    }

                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(30)
                    };

                    Response.Cookies.Append(cartCookie, JsonSerializer.Serialize(cartList), cookieOptions);

                    totalCount = cartList.Sum(i => i.Quantity);
                }

                return Json(new { success = true, message = "Product added to cart successfully.", cartCount = totalCount });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in addToCart: {ex.Message}");
                return Json(new { success = false, message = "An unexpected error occurred." });
            }
            finally
            {
                // إزالة الطلب من الطلبات النشطة بعد الانتهاء من المعالجة
                lock (_lockObj)
                {
                    string requestKey = $"cart_{id}";
                    _activeRequests.Remove(requestKey);
                }
            }
        }
        
        // Helper method for getting current cart count
        private int GetCurrentCartCount()
        {
            int totalCount = 0;
            var userIdStr = HttpContext.Session.GetString("UserId");
            
            if (int.TryParse(userIdStr, out int userId))
            {
                totalCount = _Db.CartItems
                    .Where(c => c.UserId == userId)
                    .Sum(item => item.Quantity);
            }
            else
            {
                var cookie = HttpContext.Request.Cookies["temporaryCart"];
                if (!string.IsNullOrEmpty(cookie))
                {
                    try
                    {
                        var items = JsonSerializer.Deserialize<List<temporaryCart>>(cookie);
                        totalCount = items?.Sum(i => i.Quantity) ?? 0;
                    }
                    catch
                    {
                        totalCount = 0;
                    }
                }
            }
            
            return totalCount;
        }

        public async Task<IActionResult> removeFromCart(int? id, bool isAjax = false)
        {
            int totalCount = 0;
            int userId;

            // إذا كان المستخدم مسجل دخول
            if (int.TryParse(HttpContext.Session.GetString("UserId"), out userId))
            {
                if (id.HasValue && id != 0)
                {
                    var cartItem = _Db.CartItems.FirstOrDefault(item => item.UserId == userId && item.Id == id);
                    if (cartItem != null)
                    {
                        _Db.CartItems.Remove(cartItem);
                        await _Db.SaveChangesAsync();
                    }
                }
                else
                {
                    var cartItems = _Db.CartItems.Where(item => item.UserId == userId).ToList();
                    if (cartItems.Any())
                    {
                        _Db.CartItems.RemoveRange(cartItems);
                        await _Db.SaveChangesAsync();
                    }
                }
                
                // Get updated cart count
                var cart = await _Db.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
                if (cart != null)
                {
                    totalCount = await _Db.CartItems
                        .Where(c => c.CartId == cart.Id)
                        .SumAsync(item => item.Quantity);
                }
            }
            else // المستخدم غير مسجل دخول (زائر)
            {
                // استرجاع الكوكي
                const string cartCookie = "temporaryCart";
                string cartCookieValue = Request.Cookies[cartCookie];

                if (!string.IsNullOrEmpty(cartCookieValue))
                {
                    // فك الكوكيز إلى ليست
                    var tempCartList = JsonSerializer.Deserialize<List<temporaryCart>>(cartCookieValue);
                   
                    if (tempCartList != null)
                    {
                        if (id.HasValue && id != 0)
                        {
                            var itemToRemove = tempCartList.FirstOrDefault(x => x.Id == id.Value);
                            if (itemToRemove != null)
                            {
                                tempCartList.Remove(itemToRemove);
                            }
                        }
                        else
                        {
                            // حذف جميع العناصر
                            tempCartList.Clear();
                        }

                        // إعادة كتابة الكوكيز بعد التعديل أو الحذف
                        CookieOptions options = new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(1)
                        };

                        if (tempCartList.Any())
                        {
                            Response.Cookies.Append(cartCookie, JsonSerializer.Serialize(tempCartList), options);
                            totalCount = tempCartList.Sum(i => i.Quantity);
                        }
                        else
                        {
                            Response.Cookies.Delete(cartCookie);
                            totalCount = 0;
                        }
                    }
                }
                else
                {
                    // لا يوجد أي بيانات بالكوكيز - حذف نهائي احتياطي
                    Response.Cookies.Delete("Cart");
                    totalCount = 0;
                }
            }
            
            if (isAjax)
            {
                return Json(new { success = true, message = "Item removed from cart", cartCount = totalCount });
            }
            else
            {
                return RedirectToAction("cart");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult checkDiscountCheckout(string discount)
        {

            int userId;
            if (int.TryParse(HttpContext.Session.GetString("UserId"), out userId))
            {
                decimal discountValue = 0;
                decimal sub = 0;
                decimal total = 0;
                // Fix: Use nameof(Card) to specify the navigation property as a string
                var cartItems = _Db.CartItems.Where(id => id.UserId == userId).Include(nameof(Card)).ToList();
                var discountCode = _Db.Discounts.FirstOrDefault(x => x.DiscountCode == discount);

                if(discountCode != null )
                {
                    discountValue = discountCode.DiscountValue;
                }
                else
                {
                    discountValue = 0;
                }

                if (cartItems.Any())
                {
                    var temporaryCartItems = cartItems.Select(cartItem => new temporaryCart
                    {
                        CardID = cartItem.CardId, // Accessing CardId from individual CartItem
                        CardName = cartItem.Card.CardName, // Accessing CardName from the related Card entity
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Price,


                    }).ToList();

                    foreach(var temp in temporaryCartItems)
                    {
                        sub += temp.Quantity * temp.Price;
                    }
                    total =sub-( sub * discountValue / 100);

                    temporaryCartItems[0].Subtotal = sub;
                    temporaryCartItems[0].Total = total;
                    temporaryCartItems[0].discount = sub * discountValue / 100;

                    return View("checkout", temporaryCartItems);
                }
                return RedirectToAction("checkout");
            }
            else
            return RedirectToAction(nameof(checkout));
        }




        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<ActionResult> CheckoutBtn(
          string phone,
          string email,
          bool sendAsGift,
          string firstNameGift,
          string lastNameGift,
          string phoneGift,
          string emailGift,
          string massegeGift,
          string payment,
          decimal discount,
          decimal subtotal,
          decimal total

      )
        {
            int userId ;
            if(int.TryParse(HttpContext.Session.GetString("UserId"), out userId))
            {
                
                var cart = await _Db.Carts.FirstOrDefaultAsync(uId => uId.UserId == userId);
                var cartItems = await _Db.CartItems.Where(x => x.CartId == cart.Id).ToListAsync();

                if (cartItems != null)
                {
                    var order = new Order
                    {
                        UserId = userId,
                        OrderDate = DateTime.Now,
                        TotalAmount = total,

                        Status = "Completed", // أو أي حالة أخرى تناسب تطبيقك
                       
                        CreatedAt = DateTime.Now,


                        PhoneNumber = phone,

                        Email = email,
                        IsSendAsGift = sendAsGift,
                        FirstNameGift = firstNameGift,
                        LastNameGift = lastNameGift,
                        PhoneNumberGift = phoneGift,
                        EmailGift = emailGift,
                        MassegeGift = massegeGift,
                        PaymentMethod = payment,
                        DiscountValue = discount,
                        Subtotal = subtotal,
                    };
                    _Db.Orders.Add(order);
                    await _Db.SaveChangesAsync();
                    foreach (var item in cartItems)
                    {
                        var orderDetail = new OrderDetail
                        {
                            OrderId = order.Id,
                            CardId = item.CardId,
                            Quantity = item.Quantity,
                            Price = item.Price
                        };
                        _Db.OrderDetails.Add(orderDetail);
                    }
                    await _Db.SaveChangesAsync();
                }

                if (cartItems.Any())
                {
                    _Db.CartItems.RemoveRange(cartItems);
                    await _Db.SaveChangesAsync();
                }
                _Db.Remove(cart);
                await _Db.SaveChangesAsync();


               

            }
            return RedirectToAction("index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> inc(int id)
        {
            int newQuantity = 0;
            bool success = false;
            string message = "";

            if (HttpContext.Session.GetString("UserId") != null)
            {
                // المستخدم مسجل
                var cart = await _Db.CartItems.FindAsync(id);
                if (cart != null)
                {
                    cart.Quantity++;
                    _Db.CartItems.Update(cart);
                    await _Db.SaveChangesAsync();
                    newQuantity = cart.Quantity;
                    success = true;
                    message = "تم زيادة الكمية.";
                }
                else
                {
                    message = "العنصر غير موجود.";
                }
            }
            else
            {
                // المستخدم غير مسجل
                const string cartCookie = "temporaryCart";
                string cartCookieValue = Request.Cookies[cartCookie];
                List<temporaryCart> cartList = new List<temporaryCart>();

                if (!string.IsNullOrEmpty(cartCookieValue))
                {
                    try
                    {
                        cartList = JsonSerializer.Deserialize<List<temporaryCart>>(cartCookieValue);
                        var item = cartList.FirstOrDefault(x => x.Id == id);
                        if (item != null)
                        {
                            item.Quantity++;
                            newQuantity = item.Quantity;
                            success = true;
                            message = "تم زيادة الكمية.";
                        }
                        else
                        {
                            message = "العنصر غير موجود.";
                        }

                        Response.Cookies.Append(cartCookie, JsonSerializer.Serialize(cartList), new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(30)
                        });
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine("Error in inc cookie: " + ex.Message);
                        message = "خطأ في معالجة البيانات.";
                    }
                }
                else
                {
                    message = "السلة فارغة.";
                }
            }

            return Json(new { success, newQuantity, message });
        }

        [HttpPost]
        public async Task<IActionResult> dec(int id)
        {
            int newQuantity = 0;
            bool success = false;
            string message = "";

            if (HttpContext.Session.GetString("UserId") != null)
            {
                // المستخدم مسجل
                var cart = await _Db.CartItems.FindAsync(id);
                if (cart != null)
                {
                    cart.Quantity--;
                    if (cart.Quantity <= 0)
                    {
                        _Db.CartItems.Remove(cart);
                        message = "تم حذف العنصر.";
                    }
                    else
                    {
                        _Db.CartItems.Update(cart);
                        newQuantity = cart.Quantity;
                        success = true;
                        message = "تم تقليل الكمية.";
                    }
                    await _Db.SaveChangesAsync();
                }
                else
                {
                    message = "العنصر غير موجود.";
                }
            }
            else
            {
                // المستخدم غير مسجل
                const string cartCookie = "temporaryCart";
                string cartCookieValue = Request.Cookies[cartCookie];
                List<temporaryCart> cartList = new List<temporaryCart>();

                if (!string.IsNullOrEmpty(cartCookieValue))
                {
                    try
                    {
                        cartList = JsonSerializer.Deserialize<List<temporaryCart>>(cartCookieValue);
                        var item = cartList.FirstOrDefault(x => x.Id == id);
                        if (item != null)
                        {
                            item.Quantity--;
                            if (item.Quantity <= 0)
                            {
                                cartList.Remove(item);
                                message = "تم حذف العنصر.";
                            }
                            else
                            {
                                newQuantity = item.Quantity;
                                success = true;
                                message = "تم تقليل الكمية.";
                            }
                        }
                        else
                        {
                            message = "العنصر غير موجود.";
                        }

                        Response.Cookies.Append(cartCookie, JsonSerializer.Serialize(cartList), new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(30)
                        });
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine("Error in dec cookie: " + ex.Message);
                        message = "خطأ في معالجة البيانات.";
                    }
                }
                else
                {
                    message = "السلة فارغة.";
                }
            }

            return Json(new { success, newQuantity, message });
        }

        [HttpPost]
        public async Task<IActionResult> addToFavorites(int id)
        {
            // منع تنفيذ نفس الطلب مرتين
            lock (_favLockObj)
            {
                var now = DateTime.Now;
                var keysToRemove = _activeFavRequests.Where(kvp => (now - kvp.Value).TotalSeconds > 5)
                    .Select(kvp => kvp.Key).ToList();
                
                foreach (var key in keysToRemove)
                {
                    _activeFavRequests.Remove(key);
                }
                
                // التحقق من وجود طلب مماثل
                string requestKey = $"fav_{id}";
                
                if (_activeFavRequests.ContainsKey(requestKey))
                {
                    // طلب مكرر، إرجاع حالة معالجة
                    return Json(new { success = true, message = "Request already processed." });
                }
                
                // تسجيل الطلب كنشط
                _activeFavRequests[requestKey] = now;
            }

            try
            {
                // Check if product exists
                var product = await _Db.Cards.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                bool isAdded = false;
                
                // Check if user is logged in
                string userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    // Handle non-logged-in users using cookies
                    const string favoriteCookie = "temporaryFavorites";
                    string favoriteCookieValue = Request.Cookies[favoriteCookie];
                    List<temporaryFavorite> favoritesList = new List<temporaryFavorite>();

                    if (!string.IsNullOrEmpty(favoriteCookieValue))
                    {
                        try
                        {
                            favoritesList = System.Text.Json.JsonSerializer.Deserialize<List<temporaryFavorite>>(favoriteCookieValue);
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            Console.WriteLine($"Error deserializing favorites cookie: {ex.Message}");
                            favoritesList = new List<temporaryFavorite>();
                        }
                    }

                    // Check if the product is already in favorites
                    var existingItem = favoritesList.FirstOrDefault(item => item.CardID == id);

                    if (existingItem != null)
                    {
                        // Product is already in favorites, so remove it
                        favoritesList.Remove(existingItem);
                        isAdded = false;
                    }
                    else
                    {
                        // Add product to favorites
                        var favoriteItem = new temporaryFavorite
                        {
                            Id = new Random().Next(1, int.MaxValue),
                            CardID = product.Id,
                            CardName = product.CardName,
                            ImageUrl = product.ImageUrl,
                            Price = product.Price,
                            CreatedAt = DateTime.Now
                        };
                        
                        // Make sure the ID is unique
                        while (favoritesList.Any(x => x.Id == favoriteItem.Id))
                        {
                            favoriteItem.Id = new Random().Next(1, int.MaxValue);
                        }
                        
                        favoritesList.Add(favoriteItem);
                        isAdded = true;
                    }

                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(30)
                    };

                    Response.Cookies.Append(favoriteCookie, System.Text.Json.JsonSerializer.Serialize(favoritesList), cookieOptions);
                    
                    return Json(new { 
                        success = true, 
                        added = isAdded, 
                        message = isAdded ? "Product added to wishlist." : "Product removed from wishlist." 
                    });
                }

                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Json(new { success = false, message = "Invalid user session." });
                }

                // Use transaction to prevent race conditions
                using (var transaction = await _Db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Check if the item is already in favorites
                        var existingFavorite = await _Db.Favorites
                            .FirstOrDefaultAsync(f => f.UserId == userId && f.CardId == id);

                        if (existingFavorite != null)
                        {
                            // Item already in favorites, so remove it
                            _Db.Favorites.Remove(existingFavorite);
                            var result = await _Db.SaveChangesAsync();
                            if (result <= 0)
                            {
                                throw new Exception("Failed to remove item from favorites.");
                            }
                            
                            // Update favorite count in the Card table
                            product.FavoriteCount = Math.Max(0, (product.FavoriteCount ?? 0) - 1);
                            _Db.Cards.Update(product);
                            result = await _Db.SaveChangesAsync();
                            if (result <= 0)
                            {
                                throw new Exception("Failed to update product favorite count.");
                            }
                            
                            isAdded = false;
                        }
                        else
                        {
                            // Add new favorite
                            var favorite = new Favorite
                            {
                                UserId = userId,
                                CardId = id,
                                CreatedAt = DateTime.Now
                            };

                            _Db.Favorites.Add(favorite);
                            
                            // Update favorite count in the Card table
                            product.FavoriteCount = (product.FavoriteCount ?? 0) + 1;
                            _Db.Cards.Update(product);
                            
                            var result = await _Db.SaveChangesAsync();
                            if (result <= 0)
                            {
                                throw new Exception("Failed to add item to favorites.");
                            }
                            
                            isAdded = true;
                        }
                        
                        // Commit the transaction
                        await transaction.CommitAsync();
                        
                        return Json(new { 
                            success = true, 
                            added = isAdded, 
                            message = isAdded ? "Product added to wishlist." : "Product removed from wishlist." 
                        });
                    }
                    catch (Exception ex)
                    {
                        // Rollback if any error occurs
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Error updating wishlist: {ex.Message}");
                        return Json(new { success = false, message = $"Error: Failed to update wishlist." });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in addToFavorites: {ex.Message}");
                return Json(new { success = false, message = $"Unexpected error occurred." });
            }
            finally
            {
                // إزالة الطلب من الطلبات النشطة بعد الانتهاء من المعالجة
                lock (_favLockObj)
                {
                    string requestKey = $"fav_{id}";
                    _activeFavRequests.Remove(requestKey);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserFavorites()
        {
            try
            {
                // Check if user is logged in
                string userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    // For non-logged in users, return favorites from cookies
                    const string favoriteCookie = "temporaryFavorites";
                    string favoriteCookieValue = Request.Cookies[favoriteCookie];
                    List<temporaryFavorite> favoritesList = new List<temporaryFavorite>();
                    
                    if (!string.IsNullOrEmpty(favoriteCookieValue))
                    {
                        try
                        {
                            favoritesList = System.Text.Json.JsonSerializer.Deserialize<List<temporaryFavorite>>(favoriteCookieValue);
                            var favoriteIds = favoritesList.Select(f => f.CardID).ToList();
                            return Json(new { success = true, favorites = favoriteIds });
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            Console.WriteLine($"Error deserializing favorites cookie: {ex.Message}");
                            return Json(new { success = true, favorites = new List<int>() });
                        }
                    }
                    
                    return Json(new { success = true, favorites = new List<int>() });
                }

                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Json(new { success = false, message = "Invalid user session." });
                }

                // Get user's favorite products
                var favorites = await _Db.Favorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.CardId)
                    .ToListAsync();

                return Json(new { success = true, favorites });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            try
            {
                // Check if user is logged in
                string userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    // For non-logged in users, show favorites from cookies
                    const string favoriteCookie = "temporaryFavorites";
                    string favoriteCookieValue = Request.Cookies[favoriteCookie];
                    List<temporaryFavorite> favoritesList = new List<temporaryFavorite>();
                    
                    if (!string.IsNullOrEmpty(favoriteCookieValue))
                    {
                        try
                        {
                            favoritesList = System.Text.Json.JsonSerializer.Deserialize<List<temporaryFavorite>>(favoriteCookieValue);
                            
                            if (favoritesList.Count > 0)
                            {
                                // Get the product IDs from the cookie
                                var productIds = favoritesList.Select(f => f.CardID).ToList();
                                
                                // Fetch actual product data from the database
                                var products = await _Db.Cards
                                    .Where(c => productIds.Contains(c.Id))
                                    .Include(c => c.Category)
                                    .ToListAsync();
                                
                                // Create a list of favorites with the same structure as database favorites
                                var cookieFavorites = products.Select(p => new Favorite
                                {
                                    CardId = p.Id,
                                    Card = p,
                                    CreatedAt = DateTime.Now
                                }).ToList();
                                
                                // Pass the list to the view
                                ViewBag.IsAnonymous = true;
                                return View(cookieFavorites);
                            }
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            Console.WriteLine($"Error deserializing favorites cookie: {ex.Message}");
                        }
                    }
                    
                    // If no favorites found or error occurred, show empty list
                    ViewBag.IsAnonymous = true;
                    return View(new List<Favorite>());
                }

                if (!int.TryParse(userIdStr, out int userId))
                {
                    TempData["ErrorMessage"] = "Invalid session. Please log in again.";
                    return RedirectToAction("SignIn", "User");
                }

                // Get user's favorite products with details
                var favorites = await _Db.Favorites
                    .Where(f => f.UserId == userId)
                    .Include(f => f.Card)
                    .ThenInclude(c => c.Category)
                    .ToListAsync();

                ViewBag.IsAnonymous = false;
                return View(favorites);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFavoritesCount()
        {
            try
            {
                // Check if user is logged in
                string userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    // For non-logged in users, count favorites from cookies
                    const string favoriteCookie = "temporaryFavorites";
                    string favoriteCookieValue = Request.Cookies[favoriteCookie];
                    
                    if (!string.IsNullOrEmpty(favoriteCookieValue))
                    {
                        try
                        {
                            var favoritesList = System.Text.Json.JsonSerializer.Deserialize<List<temporaryFavorite>>(favoriteCookieValue);
                            return Json(new { success = true, favoritesCount = favoritesList.Count });
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            Console.WriteLine($"Error deserializing favorites cookie: {ex.Message}");
                            return Json(new { success = true, favoritesCount = 0 });
                        }
                    }
                    
                    return Json(new { success = true, favoritesCount = 0 });
                }

                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Json(new { success = false, favoritesCount = 0 });
                }

                // Count user's favorite products
                int favoritesCount = await _Db.Favorites
                    .Where(f => f.UserId == userId)
                    .CountAsync();

                return Json(new { success = true, favoritesCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}", favoritesCount = 0 });
            }
        }

    }
}
