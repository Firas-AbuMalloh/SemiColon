using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SemiColon.Models;
using SemiColon.Models.ViewModel;

namespace SemiColon.Controllers
{
    public class ProductsController : Controller
    {
        private readonly MyDbContext _Db;

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
            Int64 userId;
            if (Int64.TryParse(HttpContext.Session.GetString("UserId"), out userId))
            {
                // Fix: Use nameof(Card) to specify the navigation property as a string
                var cartItems = _Db.CartItems.Where(id => id.UserId == userId).Include(nameof(Card)).ToList();

                if (cartItems.Any())
                {
                    var temporaryCartItems = cartItems.Select(cartItem => new temporaryCart
                    {
                        CardID = cartItem.CardId, // Accessing CardId from individual CartItem
                        CardName = cartItem.Card.CardName, // Accessing CardName from the related Card entity
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Price
                    }).ToList();

                    return View(temporaryCartItems);
                }
                return RedirectToAction("shop", "Products");
            }
            else
                return RedirectToAction("profile", "User");
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
        [Route("Products/addToCart/{id}")]
        public async Task<IActionResult> addToCart(int id)
        {
            var product = await _Db.Cards.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            temporaryCart cartItemForAnon = new temporaryCart // تعريف لعناصر الكوكيز
            {
                Id = new Random().Next(1, int.MaxValue),
                CardID = product.Id,
                Quantity = 1,
                Price = product.Price,
                ImageUrl =product.ImageUrl,
                CardName = product.CardName,
                CreatedAt = DateTime.Now
            };

            if (HttpContext.Session.GetString("UserId") != null)
            {
                string userId = HttpContext.Session.GetString("UserId");
                int parsedUserId;
                if (int.TryParse(userId, out parsedUserId))
                {
                    var cart = await _Db.Carts.FirstOrDefaultAsync(c => c.UserId == parsedUserId);
                    if (cart == null)
                    {
                        cart = new Cart { UserId = parsedUserId, CreatedAt = DateTime.Now };
                        _Db.Carts.Add(cart);
                        await _Db.SaveChangesAsync();
                    }

                    var existingCartItem = await _Db.CartItems.FirstOrDefaultAsync(item => item.CartId == cart.Id && item.CardId == id);

                    if (existingCartItem != null)
                    {
                        //البرودكت موجود وبدنا نزيد واحد

                        existingCartItem.Quantity++;
                        existingCartItem.UpdatedAt = DateTime.Now;
                        _Db.Update(existingCartItem);
                    }
                    else
                    {
                        // إضافة عنصر جديد

                        var newCartItem = new CartItem
                        {
                            CartId = cart.Id,
                            CardId = id,
                            Quantity = 1,
                            Price = product.Price, // تأكد من وجود هذا الحقل
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            UserId = parsedUserId
                        };
                        _Db.CartItems.Add(newCartItem);
                    }
                    await _Db.SaveChangesAsync();
                }
                else
                {
                    // معالجة حالة عدم صلاحية UserId في الجلسة (تسجيل الدخول غير صحيح جزئيًا)
                    return BadRequest("Invalid User ID in session.");
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

                var existingItemInCookie = cartList.FirstOrDefault(item => item.CardID == cartItemForAnon.CardID);

                if (existingItemInCookie != null)
                {
                    existingItemInCookie.Quantity++;
                }
                else
                {
                    cartList.Add(cartItemForAnon);
                }

                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(30), // صلاحية 30 يوم

                    //from program.cs : ( CookiePolicyOptions )
                    //    HttpOnly = true,
                    //    IsEssential = true,
                    //    Secure = true, 
                    //    SameSite = SameSiteMode.Lax
                    //
                    };

                    Response.Cookies.Append(cartCookie, JsonSerializer.Serialize(cartList), cookieOptions);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult removeFromCart(int? id)
        {
            Int64 userId;

            if (Int64.TryParse(HttpContext.Session.GetString("UserId"), out userId))
            {
                if (id.HasValue && id != 0)
                {
                    var cartItem = _Db.CartItems.FirstOrDefault(item => item.UserId == userId && item.Id == id);
                    if (cartItem != null)
                    {
                        _Db.CartItems.Remove(cartItem); 
                        _Db.SaveChanges();
                        return RedirectToAction("cart");
                    }
                }
                else if (id == null || id == 0)
                {
                    var cartItems = _Db.CartItems.Where(item => item.UserId == userId).ToList(); 
                    if (cartItems.Any())
                    {
                        _Db.CartItems.RemoveRange(cartItems); 
                        _Db.SaveChanges();
                        return RedirectToAction("cart");
                    }
                }
            }
            else
            {
                if(id != null)
                {

                }
            }

            return RedirectToAction("cart");
        }
    }
    }
