using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemiColon.Models; // استبدله بمسار DbContext الصحيح
using System.Text.Json;
using SemiColon.Models.ViewModel;

public class CartCountViewComponent : ViewComponent
{
    private readonly MyDbContext _context;

    public CartCountViewComponent(MyDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        int totalCount = 0;

        var userIdStr = HttpContext.Session.GetString("UserId");
        if (int.TryParse(userIdStr, out int userId))
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart != null)
            {
                totalCount = await _context.CartItems
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

        return View(totalCount);
    }
}
