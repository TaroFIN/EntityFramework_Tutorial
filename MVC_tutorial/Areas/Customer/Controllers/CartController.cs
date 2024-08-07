using FluentEcpay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Pelican.DataAccess.Repository.IRepository;
using Pelican.Models;
using Pelican.Models.ViewModels;
using Pelican.Utility;
using System.Security.Claims;

namespace MVC_tutorial.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private static IPayment _payment;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            IEnumerable<ProductImage> productImages = _unitOfWork.ProductImage.GetAll();

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Product.ProductImages = productImages.Where(u => u.ProductId == cart.Product.Id).ToList();
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
			
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
			ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
            ShoppingCartVM.OrderHeader.PaymentIntentId = "";

			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
			}

            if(applicationUser.CompanyId.GetValueOrDefault()==0)
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}
            else
            {
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();
            
            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }
            
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
                var service = new
                {
                    Url = "https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5",
                    MerchantId = "3002599",
                    HashKey = ECPay.HashKey,
                    HashIV = ECPay.HashIV,
                    ServerUrl = Request.Scheme + "://" + Request.Host.Value + "/api/payment/callback",
                    ClientUrl = Request.Scheme + "://" + Request.Host.Value + $"/Customer/Cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.id}"
                };
                var transaction = new
                {
                    No = "test01",
                    Description = "測試購物系統",
                    Date = DateTime.Now,
                    Method = EPaymentMethod.Credit,
                    Items = new List<Item>()
                };
                foreach (var cart in ShoppingCartVM.ShoppingCartList)
                {
                    transaction.Items.Add(new Item()
                    {
                        Name = cart.Product.Title,
                        Price = (int)cart.Product.Price,
                        Quantity = cart.Count
                    });
                }

                _payment = new PaymentConfiguration()
                    .Send.ToApi(
                        url: service.Url)
                    .Send.ToMerchant(
                        service.MerchantId)
                    .Send.UsingHash(
                        key: service.HashKey,
                        iv: service.HashIV)
                    .Return.ToServer(
                        url: service.ServerUrl)
                    .Return.ToClient(
                        url: service.ClientUrl)
                    .Transaction.New(
                        no: transaction.No,
                        description: transaction.Description,
                        date: transaction.Date)
                    .Transaction.UseMethod(
                        method: transaction.Method)
                    .Transaction.WithItems(
                        items: transaction.Items)
                    .Generate();
                return View("ECPay", _payment);
            }
            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.id });
        }

        [HttpGet]
        public IActionResult OrderConfirmation(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.id == id);
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == orderHeader.ApplicationUserId);
            IEnumerable<ShoppingCart> ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId);

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                orderHeader.PaymentIntentId = _payment.MerchantTradeNo;
                orderHeader.PaymentDate = DateTime.Now;
            }
            else
            {
                orderHeader.OrderStatus = SD.StatusShipped;
            }

            _unitOfWork.ShoppingCart.RemoveRange(ShoppingCartList);
            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();

            HttpContext.Session.Clear();

            //_emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Pelican",
            //    $"<p>New Order Created - {orderHeader.id}</p>");

            return View(id);
        }

		public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cartFromDb.Count++;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
            _unitOfWork.Save();
            HttpContext.Session.SetInt32(SD.SessionCart,
                _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count());
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            if (cartFromDb.Count <= 1)
            {
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count--;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
            _unitOfWork.Save();
            HttpContext.Session.SetInt32(SD.SessionCart, 
                _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count());
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
            HttpContext.Session.SetInt32(SD.SessionCart,
                _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count());
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else if (shoppingCart.Count > 50 && shoppingCart.Count <= 100)
            {
                return shoppingCart.Product.Price50;
            }
            return shoppingCart.Product.Price100;
        }
    }
}
