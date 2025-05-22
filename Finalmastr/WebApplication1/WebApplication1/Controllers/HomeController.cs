using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemiColon.Models;
using WebApplication1.Models;
using Microsoft.AspNetCore.Http;
using SemiColon.Models.ViewModel;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Net;

namespace SemiColon.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;


        private readonly MyDbContext _Db;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, MyDbContext dbContext, IConfiguration configuration)
        {
            _logger = logger;
            _Db = dbContext;
            _configuration = configuration;
        }

        public async Task<IActionResult> index()
        {
            var products = await _Db.Cards.ToListAsync();
            var categories = await _Db.Categories.ToListAsync();
            var main = await _Db.MainCategories.ToListAsync();

            var mostFrequentCards = await _Db.OrderDetails
                .GroupBy(od => od.CardId)
                .Select(group => new { CardId = group.Key, Count = group.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            var frequentCardIds = mostFrequentCards.Select(mfc => mfc.CardId).ToList();

            // تحسين الأداء باستخدام Dictionary
            var frequentCardDict = mostFrequentCards.ToDictionary(mfc => mfc.CardId, mfc => mfc.Count);

            var bestSellingCardsInfo = _Db.Cards
                .Where(c => frequentCardIds.Contains(c.Id))
                .AsEnumerable()
                .Select(c =>
                {
                    frequentCardDict.TryGetValue(c.Id, out int count);
                    return new bestOrderSellerViewModels
                    {
                        CardId = c.Id,
                        CardName = c.CardName,
                        CardPrice = c.Price,
                        CardImage = c.ImageUrl,
                        CardCount = count
                    };
                })
                .OrderByDescending(vm => vm.CardCount)
                .ToList();
            var testimonials = await _Db.Testimonials.Include(t => t.User).ToListAsync();
            var viewModelIndex = new indexViewModel
            {
                Products = products,
                Categories = categories,
                MainCategories = main,
                BestOrderSellers = bestSellingCardsInfo,
                Testimonials = testimonials
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactUs(ContactFeedBack dataForm)
        {
            if (ModelState.IsValid)
            {
                var recaptchaResponse = Request.Form["g-recaptcha-response"];
                var recaptchaSecretKey = _configuration["Recaptcha:SecretKey"];

                // 1. التحقق من مفتاح ReCaptcha
                if (string.IsNullOrEmpty(recaptchaSecretKey))
                {
                    // Log error: ReCaptcha secret key not configured! (Use a logging framework)
                    ModelState.AddModelError("", "ReCaptcha is not configured correctly.");
                    return View(dataForm);
                }

                if (string.IsNullOrEmpty(recaptchaResponse))
                {
                    ModelState.AddModelError("", "Please complete the CAPTCHA.");
                    return View(dataForm);
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
                            return View(dataForm);
                        }
                        else
                        {
                            // 2. Process the form data (Database interaction, etc.)
                            try
                            {
                                _Db.ContactFeedBacks.Add(dataForm);
                                dataForm.CreatedAt = DateOnly.FromDateTime(DateTime.Now);
                                await _Db.SaveChangesAsync();

                                // Success!
                                TempData["SuccessMessage"] = "Your message has been sent successfully!";
                                return RedirectToAction("ContactUs");
                            }
                            catch (Exception dbEx)
                            {
                                // Log the database exception (Use a logging framework)
                                ModelState.AddModelError("", "An error occurred while saving your message. Please try again.");
                                return View(dataForm);
                            }
                        }
                    }
                    else
                    {
                        // Log error: ReCaptcha verification failed (HTTP error)
                        ModelState.AddModelError("", "Error verifying ReCaptcha. Please try again.");
                        return View(dataForm);
                    }
                }
            }
            else
            {
                return View(dataForm);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }



        public async Task<IActionResult> blog()
        {

            return View();
        }
        public async Task<IActionResult> blogDetails1()
        {

            return View();
        }
        public async Task<IActionResult> blogDetails2()
        {

            return View();
        }
        public async Task<IActionResult> blogDetails3()
        {

            return View();
        } public async Task<IActionResult> blogDetails4()
        {

            return View();
        }
    }
}
