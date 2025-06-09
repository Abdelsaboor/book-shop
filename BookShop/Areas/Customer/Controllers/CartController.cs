using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using BookShop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookShop.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IShoppingCartRepository _shoppingCartRepo;
        private readonly IProductRepository _productRepo;

        private ShoppingCartViewModel _shoppingCartVM { get; set; }

        public CartController(IShoppingCartRepository shoppingCartRepo, IProductRepository productRepo)
        {
            _shoppingCartRepo = shoppingCartRepo;
            _productRepo = productRepo;
        }

        public IActionResult Index()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            _shoppingCartVM = new ShoppingCartViewModel()
            {
                ShoppingCartList = _shoppingCartRepo.GetAll(
                    u => u.ApplicationUserId == userId,
                    includeProperties: "Product").ToList()
            };

            foreach (var cart in _shoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                _shoppingCartVM.OrderTotal += (cart.Price * cart.Count);
            }

            UpdateCartCountInSession();

            return View(_shoppingCartVM);
        }

        public IActionResult Summary()
        {
            return View();
        }

        public IActionResult Plus(Guid cartId)
        {
            var cartFromDb = _shoppingCartRepo.Get(u => u.Id == cartId);
            cartFromDb.Count += 1;
            _shoppingCartRepo.Update(cartFromDb);
            _shoppingCartRepo.Save();

            UpdateCartCountInSession();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(Guid cartId)
        {
            var cartFromDb = _shoppingCartRepo.Get(u => u.Id == cartId);
            if (cartFromDb.Count == 1)
            {
                _shoppingCartRepo.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count -= 1;
                _shoppingCartRepo.Update(cartFromDb);
            }

            _shoppingCartRepo.Save();

            UpdateCartCountInSession();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(Guid cartId)
        {
            var cartFromDb = _shoppingCartRepo.Get(u => u.Id == cartId);
            _shoppingCartRepo.Remove(cartFromDb);
            _shoppingCartRepo.Save();

            UpdateCartCountInSession();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            shoppingCart.ApplicationUserId = userId;

            var cartFromDb = _shoppingCartRepo.Get(
                u => u.ApplicationUserId == userId && u.ProductId == shoppingCart.ProductId);

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

            UpdateCartCountInSession();

            TempData["success"] = $"{product.Title} has been added to the cart";
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else if (shoppingCart.Count <= 100)
            {
                return shoppingCart.Product.Price50;
            }
            else
            {
                return shoppingCart.Product.Price100;
            }
        }

        private string GetUserId()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            return claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private void UpdateCartCountInSession()
        {
            var userId = GetUserId();
            if (userId == null) return;

            var cartItems = _shoppingCartRepo.GetAll(u => u.ApplicationUserId == userId);
            var count = cartItems.Sum(item => item.Count);

            HttpContext.Session.SetInt32("SessionCart", count);
        }
    }
}
