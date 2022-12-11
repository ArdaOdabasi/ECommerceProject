using ECommerceProject.Data;
using ECommerceProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ECommerceProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;
        [BindProperty]
        public OrderDetailsViewModel OrderDetailsViewModel { get; set; }
        public OrderController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        [HttpPost]
        [Authorize(Roles = RoleOrderStatusSessionOperations.Role_Admin)]
        public IActionResult Approved()
        {
            OrderHeader orderHeader = _applicationDbContext.OrderHeaders.FirstOrDefault(i => i.Id == OrderDetailsViewModel.OrderHeader.Id);
            orderHeader.OrderStatus = RoleOrderStatusSessionOperations.Order_Status_Confirmed;
            _applicationDbContext.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = RoleOrderStatusSessionOperations.Role_Admin)]
        public IActionResult SendByCargo()
        {
            OrderHeader orderHeader = _applicationDbContext.OrderHeaders.FirstOrDefault(i => i.Id == OrderDetailsViewModel.OrderHeader.Id);
            orderHeader.OrderStatus = RoleOrderStatusSessionOperations.Order_Status_In_Shipping;
            _applicationDbContext.SaveChanges();
            return RedirectToAction("Index");
        }
        public IActionResult Details(int id)
        {
            OrderDetailsViewModel = new OrderDetailsViewModel
            {
                OrderHeader = _applicationDbContext.OrderHeaders.FirstOrDefault(i => i.Id == id),
                ListOfOrderDetails = _applicationDbContext.OrderDetails.Where(x => x.OrderId == id).Include(x => x.Product)
            };
            return View(OrderDetailsViewModel);
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            IEnumerable<OrderHeader> orderHeadersList;
            if (User.IsInRole(RoleOrderStatusSessionOperations.Role_Admin))
            {
                orderHeadersList = _applicationDbContext.OrderHeaders.ToList();
            }
            else
            {
                orderHeadersList = _applicationDbContext.OrderHeaders.Where(i => i.ApplicationUserId == claim.Value).Include(i => i.ApplicationUser);
            }
            return View(orderHeadersList);
        }
        public IActionResult Pended()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            IEnumerable<OrderHeader> orderHeadersList;
            if (User.IsInRole(RoleOrderStatusSessionOperations.Role_Admin))
            {
                orderHeadersList = _applicationDbContext.OrderHeaders.Where(i => i.OrderStatus == RoleOrderStatusSessionOperations.Order_Status_Pending);
            }
            else
            {
                orderHeadersList = _applicationDbContext.OrderHeaders.Where(i => i.ApplicationUserId == claim.Value && i.OrderStatus == RoleOrderStatusSessionOperations.Order_Status_Pending)
                    .Include(i => i.ApplicationUser);
            }
            return View(orderHeadersList);
        }

        public IActionResult Confirmed()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            IEnumerable<OrderHeader> orderHeadersList;
            if (User.IsInRole(RoleOrderStatusSessionOperations.Role_Admin))
            {
                orderHeadersList = _applicationDbContext.OrderHeaders.Where(i => i.OrderStatus == RoleOrderStatusSessionOperations.Order_Status_Confirmed);
            }
            else
            {
                orderHeadersList = _applicationDbContext.OrderHeaders.Where(i => i.ApplicationUserId == claim.Value && i.OrderStatus == RoleOrderStatusSessionOperations.Order_Status_Confirmed)
                    .Include(i => i.ApplicationUser);
            }
            return View(orderHeadersList);
        }

        public IActionResult Shipped()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            IEnumerable<OrderHeader> orderHeadersList;
            if (User.IsInRole(RoleOrderStatusSessionOperations.Role_Admin))
            {
                orderHeadersList = _applicationDbContext.OrderHeaders.Where(i => i.OrderStatus == RoleOrderStatusSessionOperations.Order_Status_In_Shipping);
            }
            else
            {
                orderHeadersList = _applicationDbContext.OrderHeaders.Where(i => i.ApplicationUserId == claim.Value && i.OrderStatus == RoleOrderStatusSessionOperations.Order_Status_In_Shipping)
                    .Include(i => i.ApplicationUser);
            }
            return View(orderHeadersList);
        }
    }
}
