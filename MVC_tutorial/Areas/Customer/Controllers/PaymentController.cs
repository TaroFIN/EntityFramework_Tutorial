using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Pelican.Utility;

namespace FluentEcpay.Web.Controllers
{
    [Area("Customer")]
    [Route("[controller]")]
    [ApiController]
    public class PaymentController : Controller
    {
        public PaymentController()
        {
        }

        // POST api/payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult New()
        {
            return RedirectToAction("checkout");
        }

        [HttpGet("checkout")]
        public IActionResult CheckOut()
        {
            var service = new
            {
                Url = "https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5",
                MerchantId = "3002599",
                HashKey = ECPay.HashKey,
                HashIV = ECPay.HashIV,
                ServerUrl = "https://test.com/api/payment/callback",
                ClientUrl = "https://test.com/payment/success"
            };
            var transaction = new
            {
                No = "test00003",
                Description = "�����ʪ��t��",
                Date = DateTime.Now,
                Method = EPaymentMethod.Credit,
                Items = new List<Item>{
                    new Item{
                        Name = "���",
                        Price = 14000,
                        Quantity = 2
                    },
                    new Item{
                        Name = "�H����",
                        Price = 900,
                        Quantity = 10
                    }
                }
            };
            IPayment payment = new PaymentConfiguration()
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

            return View(payment);
        }

        [HttpPost("callback")]
        public IActionResult Callback(PaymentResult result)
        {
            var hashKey = ECPay.HashKey;
            var hashIV = ECPay.HashIV;

            // �ȥ��P�_�ˬd�X�O�_���T�C
            if (!CheckMac.PaymentResultIsValid(result, hashKey, hashIV)) return BadRequest();

            // �B�z����q�檬�A����ʵ���...�C

            return Ok("1|OK");
        }
    }
}