using ECommerceProject.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerceProject.ViewComponents
{
    public class CategoryList : ViewComponent
    {
        private readonly ApplicationDbContext _applicationDbContext;
        public CategoryList(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }
        public IViewComponentResult Invoke()
        {
            var category = _applicationDbContext.Categories.ToList();
            return View(category);
        }
    }
}
