using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BookShop.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _productRepo;
        private readonly IShoppingCartRepository _shoppingCartRepo;

        public HomeController(ILogger<HomeController> logger, IProductRepository productRepo, IShoppingCartRepository shoppingCartRepo)
        {
            _logger = logger;
            _productRepo = productRepo;
            _shoppingCartRepo = shoppingCartRepo;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _productRepo.GetAll(includeProperties: "Category").ToList();

            // لو المستخدم مسجل دخول، يتم تحميل عدد عناصر السلة
            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                var cartItems = _shoppingCartRepo.GetAll(u => u.ApplicationUserId == userId);
                ViewBag.CartCount = cartItems.Sum(item => item.Count);
            }
            else
            {
                ViewBag.CartCount = 0;
            }

            return View(productList);
        }

        public IActionResult Details(Guid productId)
        {
            ShoppingCart shoppingCart = new ShoppingCart()
            {
                Product = _productRepo.Get(u => u.Id == productId, includeProperties: "Category"),
                Count = 1,
                ProductId = productId
            };
            return View(shoppingCart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId = userId;

            var cartFromDb = _shoppingCartRepo.Get(u => u.ApplicationUserId == userId && u.ProductId == shoppingCart.ProductId);
            var product = _productRepo.Get(u => u.Id == shoppingCart.ProductId);

            if (cartFromDb == null)
            {
                _shoppingCartRepo.Add(shoppingCart);
            }
            else
            {
                cartFromDb.Count += shoppingCart.Count;
                _shoppingCartRepo.Update(cartFromDb);
            }

            _shoppingCartRepo.Save();

            // تحديث عدد العناصر في السلة
            var cartItems = _shoppingCartRepo.GetAll(u => u.ApplicationUserId == userId);
            HttpContext.Session.SetInt32("SessionCart", cartItems.Sum(item => item.Count));

            TempData["success"] = $"{product.Title} is added to the cart";
            return RedirectToAction(nameof(Index));
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
