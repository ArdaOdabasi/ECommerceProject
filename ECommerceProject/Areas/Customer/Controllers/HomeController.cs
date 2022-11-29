using ECommerceProject.Data;
using ECommerceProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ECommerceProject.Areas.Customer.Controllers
{
    [Area("Customer")]

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _applicationDbContext;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext applicationDbContext)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
        }

        public IActionResult Index()
        {
            var Products = _applicationDbContext.Products.Where(p => p.IsHome).ToList();
            return View(Products);
        }

        public IActionResult Details(int id)
        {
            var product = _applicationDbContext.Products.FirstOrDefault(i => i.Id == id);
            ShoppingCard shoppingCard = new ShoppingCard()
            {
                Product = product,
                ProductId = product.Id
            };
            return View(shoppingCard);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCard Scard)
        {
            Scard.Id = 0;
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                Scard.ApplicationUserId = claim.Value;
                ShoppingCard card = _applicationDbContext.ShoppingCards.FirstOrDefault(
                    u => u.ApplicationUserId == Scard.ApplicationUserId && u.ProductId == Scard.ProductId);
                if (card == null)
                {
                    _applicationDbContext.ShoppingCards.Add(Scard);
                }
                else
                {
                    card.Count += Scard.Count;
                }
                _applicationDbContext.SaveChanges();
                var count = _applicationDbContext.ShoppingCards.Where(i => i.ApplicationUserId == Scard.ApplicationUserId).ToList().Count();
                HttpContext.Session.SetInt32(RoleOrderStatusSessionOperations.SessionShoppingCard, count);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var product = _applicationDbContext.Products.FirstOrDefault(i => i.Id == Scard.Id);
                ShoppingCard card = new ShoppingCard()
                {
                    Product = product,
                    ProductId = product.Id
                };
            }

            return View(Scard);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
