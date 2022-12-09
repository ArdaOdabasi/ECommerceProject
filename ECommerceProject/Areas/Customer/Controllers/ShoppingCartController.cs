using ECommerceProject.Data;
using ECommerceProject.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace ECommerceProject.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ShoppingCartController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;
        [BindProperty]
        public ShoppingCartViewModel shoppingCartViewModel { get; set; }

        public ShoppingCartController(UserManager<IdentityUser> userManager, IEmailSender emailSender, ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCartViewModel = new ShoppingCartViewModel
            {
                OrderHeader = new Models.OrderHeader(),
                ListOfShoppingCards = _applicationDbContext.ShoppingCards.Where(i => i.ApplicationUserId == claim.Value).Include(i => i.Product)
            };

            shoppingCartViewModel.OrderHeader.OrderTotal = 0;
            shoppingCartViewModel.OrderHeader.ApplicationUser = _applicationDbContext.ApplicationUsers.FirstOrDefault(i => i.Id == claim.Value);
            foreach (var item in shoppingCartViewModel.ListOfShoppingCards)
            {
                shoppingCartViewModel.OrderHeader.OrderTotal += (item.Count * item.Product.Price);
            }
            return View(shoppingCartViewModel);
        }

        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = _applicationDbContext.ApplicationUsers.FirstOrDefault(i => i.Id == claim.Value);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Doğrulama Emaili Boş!");
            }
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
            ModelState.AddModelError(string.Empty, "Email Doğrulama Kodu Gönder.");
            return RedirectToAction("Success");
        }
        public IActionResult Success()
        {
            return View();
        }
        public IActionResult Increase(int CartId)
        {
            var ShoppingCart = _applicationDbContext.ShoppingCards.FirstOrDefault(i => i.Id == CartId);
            ShoppingCart.Count += 1;
            _applicationDbContext.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Decrease(int CartId)
        {
            var ShoppingCart = _applicationDbContext.ShoppingCards.FirstOrDefault(i => i.Id == CartId);
            if (ShoppingCart.Count == 1)
            {
                var Count = _applicationDbContext.ShoppingCards.Where(u => u.ApplicationUserId == ShoppingCart.ApplicationUserId).ToList().Count();
                _applicationDbContext.ShoppingCards.Remove(ShoppingCart);
                _applicationDbContext.SaveChanges();
                HttpContext.Session.SetInt32(RoleOrderStatusSessionOperations.SessionShoppingCard, Count - 1);
            }
            else
            {
                ShoppingCart.Count -= 1;
                _applicationDbContext.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Delete(int CartId)
        {
            var ShoppingCart = _applicationDbContext.ShoppingCards.FirstOrDefault(i => i.Id == CartId);

            var Count = _applicationDbContext.ShoppingCards.Where(u => u.ApplicationUserId == ShoppingCart.ApplicationUserId).ToList().Count();
            _applicationDbContext.ShoppingCards.Remove(ShoppingCart);
            _applicationDbContext.SaveChanges();
            HttpContext.Session.SetInt32(RoleOrderStatusSessionOperations.SessionShoppingCard, Count - 1);

            return RedirectToAction(nameof(Index));
        }
    }
}
