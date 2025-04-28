using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemiColon.Models;
using WebApplication1.Models;
using Microsoft.AspNetCore.Http;
using SemiColon.Models.ViewModel;
using Newtonsoft.Json;

namespace SemiColon.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;


        private readonly MyDbContext _Db;

        public HomeController(ILogger<HomeController> logger, MyDbContext dbContext)
        {
            _logger = logger;
            _Db = dbContext;
        }

        public async Task<IActionResult> index()
        {
            var products = await _Db.Cards.ToListAsync();
            var categories = await _Db.Categories.ToListAsync();

            var viewModelIndex = new indexViewModel
            {
                Products = products, 
                Categories = categories 
            };

            if (viewModelIndex.Products.Any() || viewModelIndex.Categories.Any())
            {
                return View(viewModelIndex);
            }
            else
            {
                ViewBag.Message = "No products or categories found in the database.";
                return View();
            }
        }
        public IActionResult aboutUs()
        {
            return View();
        }

        public IActionResult contactUs()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> contactUs(ContactFeedBack dataForm)
        {

            if (ModelState.IsValid)
            {
                var recaptchaResponse = Request.Form["g-recaptcha-response"];
                var secretKey = "6LdpQCIrAAAAACT0zYj5o7LrzIwzgAVJck-BLGbK";
                var client = new HttpClient();
                var result = await client.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={recaptchaResponse}", null);
                var responseString = await result.Content.ReadAsStringAsync();
                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);

                if (json.success != true)
                {
                    ModelState.AddModelError("", "CAPTCHA validation failed. Please try again.");
                    return View(dataForm); // أو ممكن ترجع نفس View مع البيانات إذا تحب
                }
                else
                {
                    if (dataForm != null)
                    {
                        dataForm.UserId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));
                        if (dataForm.Email == null)
                        {
                            dataForm.Email = HttpContext.Session.GetString("Useremail");
                        }

                        dataForm.CreatedAt = DateOnly.FromDateTime(DateTime.Now);  // فقط التاريخ اتذكر يا فراس لا تنساها
                        _Db.ContactFeedBacks.Add(dataForm);
                        _Db.SaveChanges();

                        return RedirectToAction("contactUs");
                    }
                    else
                    {
                        return RedirectToAction("contactUs");
                    }
                }
            }
            else
                return View(dataForm);
        }
           
                


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
