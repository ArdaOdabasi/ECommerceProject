using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerceProject.Models
{
    public class RoleOrderStatusSessionOperations
    {
        public const string Role_Person = "Person";
        public const string Role_Admin = "Admin";
        public const string Role_User = "User";
        public const string SessionShoppingCard = "Shopping Card Session";
        public const string Order_Status_Confirmed = "Sipariş Onaylandı";
        public const string Order_Status_Pending = "Sipariş Beklemede";
        public const string Order_Status_In_Shipping = "Sipariş Kargoda";
    }
}
