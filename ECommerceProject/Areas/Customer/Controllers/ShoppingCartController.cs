using ECommerceProject.Data;
using ECommerceProject.Models;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
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

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCartViewModel = new ShoppingCartViewModel()
            {
                OrderHeader = new Models.OrderHeader(),
                ListOfShoppingCards = _applicationDbContext.ShoppingCards.Where(i => i.ApplicationUserId == claim.Value).Include(i => i.Product)
            };

            foreach (var item in shoppingCartViewModel.ListOfShoppingCards)
            {
                item.Price = item.Product.Price;
                shoppingCartViewModel.OrderHeader.OrderTotal += (item.Count * item.Product.Price);
            }

            return View(shoppingCartViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Summary(ShoppingCartViewModel model)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCartViewModel.ListOfShoppingCards = _applicationDbContext.ShoppingCards.Where(i => i.ApplicationUserId == claim.Value).Include(i => i.Product);
            shoppingCartViewModel.OrderHeader.OrderStatus = RoleOrderStatusSessionOperations.Order_Status_Pending;
            shoppingCartViewModel.OrderHeader.ApplicationUserId = claim.Value;
            shoppingCartViewModel.OrderHeader.OrderDate = DateTime.Now;
            _applicationDbContext.OrderHeaders.Add(shoppingCartViewModel.OrderHeader);
            _applicationDbContext.SaveChanges();
            foreach (var item in shoppingCartViewModel.ListOfShoppingCards)
            {
                item.Price = item.Product.Price;
                OrderDetail orderDetails = new OrderDetail()
                {
                    ProductId = item.ProductId,
                    OrderId = shoppingCartViewModel.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count
                };
                shoppingCartViewModel.OrderHeader.OrderTotal += item.Count * item.Product.Price;
                model.OrderHeader.OrderTotal += item.Count * item.Product.Price;
                _applicationDbContext.OrderDetails.Add(orderDetails);
            }
            var Payment = PaymentProcess(model);
            _applicationDbContext.ShoppingCards.RemoveRange(shoppingCartViewModel.ListOfShoppingCards);
            _applicationDbContext.SaveChanges();
            HttpContext.Session.SetInt32(RoleOrderStatusSessionOperations.SessionShoppingCard, 0);
            return RedirectToAction("OrderCompleted");
        }

        private Payment PaymentProcess(ShoppingCartViewModel model)
        {
            Options options = new Options();
            options.ApiKey = "sandbox-M3HT9Zutm1Uzodt5AXQ4BF6bDV5Pdhbz";
            options.SecretKey = "sandbox-GpPKSxgWlES6bk0txbUL3nJ6Hwj23VMV";
            options.BaseUrl = "https://sandbox-api.iyzipay.com";

            CreatePaymentRequest request = new CreatePaymentRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = new Random().Next(1111, 9999).ToString();
            request.Price = model.OrderHeader.OrderTotal.ToString();
            request.PaidPrice = model.OrderHeader.OrderTotal.ToString();
            request.Currency = Currency.TRY.ToString();
            request.Installment = 1;
            request.BasketId = "B67832";
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

            //PaymentCard paymentCard = new PaymentCard();
            //paymentCard.CardHolderName = "John Doe";
            //paymentCard.CardNumber = "5528790000000008";
            //paymentCard.ExpireMonth = "12";
            //paymentCard.ExpireYear = "2030";
            //paymentCard.Cvc = "123";
            //paymentCard.RegisterCard = 0;
            //request.PaymentCard = paymentCard;

            PaymentCard paymentCard = new PaymentCard();
            paymentCard.CardHolderName = model.OrderHeader.CardName;
            paymentCard.CardNumber = model.OrderHeader.CardNumber;
            paymentCard.ExpireMonth = model.OrderHeader.ExpirationMonth;
            paymentCard.ExpireYear = model.OrderHeader.ExpirationYear;
            paymentCard.Cvc = model.OrderHeader.Cvc;
            paymentCard.RegisterCard = 0;
            request.PaymentCard = paymentCard;

            Buyer buyer = new Buyer();
            buyer.Id = model.OrderHeader.Id.ToString();
            buyer.Name = model.OrderHeader.Name;
            buyer.Surname = model.OrderHeader.Surname;
            buyer.GsmNumber = model.OrderHeader.PhoneNumber;
            buyer.Email = "email@email.com";
            buyer.IdentityNumber = "74300864791";
            buyer.LastLoginDate = "2015-10-05 12:43:35";
            buyer.RegistrationDate = "2013-04-21 15:12:09";
            buyer.RegistrationAddress = model.OrderHeader.Address;
            buyer.Ip = "85.34.78.112";
            buyer.City = model.OrderHeader.City;
            buyer.Country = "Turkey";
            buyer.ZipCode = model.OrderHeader.PostCode;
            request.Buyer = buyer;

            Address shippingAddress = new Address();
            shippingAddress.ContactName = "Jane Doe";
            shippingAddress.City = "Istanbul";
            shippingAddress.Country = "Turkey";
            shippingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
            shippingAddress.ZipCode = "34742";
            request.ShippingAddress = shippingAddress;

            Address billingAddress = new Address();
            billingAddress.ContactName = "Jane Doe";
            billingAddress.City = "Istanbul";
            billingAddress.Country = "Turkey";
            billingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
            billingAddress.ZipCode = "34742";
            request.BillingAddress = billingAddress;

            List<BasketItem> basketItems = new List<BasketItem>();
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            foreach (var item in _applicationDbContext.ShoppingCards.Where(i => i.ApplicationUserId == claim.Value).Include(i => i.Product))
            {
                basketItems.Add(new BasketItem()
                {
                    Id = item.Id.ToString(),
                    Name = item.Product.Title,
                    Category1 = item.Product.CategoryId.ToString(),
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = (item.Price * item.Count).ToString()
                });
            }
            request.BasketItems = basketItems;

            return Payment.Create(request, options);
        }

        public IActionResult OrderCompleted()
        {
            return View();
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
