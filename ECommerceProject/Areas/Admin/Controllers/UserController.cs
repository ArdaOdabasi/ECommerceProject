using ECommerceProject.Data;
using ECommerceProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerceProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleOrderStatusSessionOperations.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;
        public UserController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }
        public IActionResult Index()
        {
            var users = _applicationDbContext.ApplicationUsers.ToList();
            var role = _applicationDbContext.Roles.ToList();
            var userRole = _applicationDbContext.UserRoles.ToList();
            foreach (var item in users)
            {
                var roleId = userRole.FirstOrDefault(i => i.UserId == item.Id).RoleId;
                item.Role = role.FirstOrDefault(u => u.Id == roleId).Name;
            }
            return View(users);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _applicationDbContext.ApplicationUsers
                .FirstOrDefaultAsync(m => m.Id == id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _applicationDbContext.ApplicationUsers.FindAsync(id);
            _applicationDbContext.ApplicationUsers.Remove(user);
            await _applicationDbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
