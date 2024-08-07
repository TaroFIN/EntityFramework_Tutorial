using FluentEcpay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Pelican.DataAccess.Repository.IRepository;
using Pelican.Models;
using Pelican.Models.ViewModels;
using Pelican.Utility;
using System.Diagnostics;
using System.Security.Claims;

namespace MVC_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private static IPayment _payment;
        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index(string status)
        {
            List<OrderHeader> objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            return View();
        }

        public IActionResult Details(int orderid)
        {
            OrderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.id == orderid, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderid, includeProperties: "Product")
            };
            return View(OrderVM);
        }

        [Authorize(Roles = SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetail(int orderid)
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.id == OrderVM.OrderHeader.id);
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode= OrderVM.OrderHeader.PostalCode;
            if(!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier)) orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber)) orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;

            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Details), new {orderId = orderHeaderFromDb.id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.id == OrderVM.OrderHeader.id);
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }
            

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["Success"] = "Order Shipped Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u=>u.id==OrderVM.OrderHeader.id);
            if(orderHeader.PaymentStatus==SD.PaymentStatusApproved)
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork.Save();
            TempData["Success"] = "Order Cancelled Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.id });
        }

        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            OrderVM.OrderHeader = _unitOfWork.OrderHeader.Get(u => u.id == OrderVM.OrderHeader.id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.id, includeProperties: "Product");

            var service = new
            {
                Url = "https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5",
                MerchantId = "3002599",
                HashKey = ECPay.HashKey,
                HashIV = ECPay.HashIV,
                ServerUrl = Request.Scheme + "://" + Request.Host.Value + "/api/payment/callback",
                ClientUrl = Request.Scheme + "://" + Request.Host.Value + $"/Admin/Order/PaymentConfirmation?id={OrderVM.OrderHeader.id}"
            };
            var transaction = new
            {
                No = "test01",
                Description = "測試購物系統",
                Date = DateTime.Now,
                Method = EPaymentMethod.Credit,
                Items = new List<Item>()
            };
            foreach (var items in OrderVM.OrderDetail)
            {
                transaction.Items.Add(new Item()
                {
                    Name = items.Product.Title,
                    Price = (int)items.Product.Price,
                    Quantity = items.Count
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

            //return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.id });
        }

        [HttpGet]
        public IActionResult PaymentConfirmation(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.id == id);
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == orderHeader.ApplicationUserId);

            
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.PaymentStatus = SD.PaymentStatusApproved;
            orderHeader.PaymentIntentId = _payment.MerchantTradeNo;
            orderHeader.PaymentDate = DateTime.Now;
            
            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            return View(id);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();

            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(u=>u.ApplicationUserId == userId, includeProperties: "ApplicationUser");

            }

            switch (status)
            {
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusPending);
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusApproved);
                    break;
                default:
                    break;
            }


            return Json(new { data = objOrderHeaders });
        }
        #endregion
    }
}
